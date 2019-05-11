using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The handler of a model block
/// </summary>
[System.Serializable]
public class BlockInfo : MonoBehaviour
{
    #region Settings and run-time data
    //basic settings
    public bool locked = true;//check is this block is accessible
    public bool highlighted = false;
    public bool hideWhenCombine = false;
    //public Canvas descriptionUI = null;
    public PlayMakerFSM descriptionFSM = null;
    public string blockName
    {
        get
        {
            return name;
        }
    }//name of this block,usually set as object's name
    [HideInInspector]
    public GameObject block;//block gameobject entity

    public List<CellBlock> subCells = new List<CellBlock>();

    //position info,built automatically
    [HideInInspector]
    public Vector3 focusedPosition;//local position when focused
    [HideInInspector]
    public Vector3 combiningPosition;//local position when combine
    [HideInInspector]
    public Vector3 dividingPosition;//local position when divide
    [HideInInspector]
    public Vector3 combineInsideViewPosition;//local position when divide
    public BlockManager manager;


    //running data
    protected bool isMoving = false;
    public bool IsMoving
    {
        get
        {
            return isMoving;
        }
    }

    protected Quaternion originalLocalRotation;
    protected Vector3 targetPosition;
    //protected Material mainMaterial;
    protected Collider mainCollider;
    protected List<Material> modelMaterials = new List<Material>();
    protected Transform rooms;
    protected string roomsMeshName;
    protected string roomsMaterialName;
    protected MeshRenderer roomsMeshRenderer;
    protected MeshFilter roomsMeshFilter;
    protected MeshRenderer RoomsMeshRenderer
    {
        get
        {
            if (rooms == null)
                return null;
            if (roomsMeshRenderer == null)
                roomsMeshRenderer = rooms.GetComponent<MeshRenderer>();

            return roomsMeshRenderer;
        }
    }
    protected MeshFilter RoomsMeshFilter
    {
        get
        {
            if (rooms == null)
                return null;
            if (roomsMeshFilter == null)
                roomsMeshFilter = rooms.GetComponent<MeshFilter>();

            return roomsMeshFilter;
        }
    }
    #endregion

    #region Moving Control
    float lerpSpeed = 8f;
    public void StartMoving(BlockCombinationStatus status, float lerpSpeed = 8f, Vector3? positionOffset = null, bool isCentered = false)
    {
        Vector3 offset = Vector3.zero;
        if (positionOffset != null)
            offset = positionOffset.Value;

        HideDescription();
        switch (status)
        {
            case BlockCombinationStatus.Combine:
                targetPosition = combiningPosition;
                break;
            case BlockCombinationStatus.CombineInsidePreview:
                targetPosition = combineInsideViewPosition;
                break;
            case BlockCombinationStatus.Divide:
                targetPosition = dividingPosition;
                break;
            case BlockCombinationStatus.FocusOnOneBlock:
                if (isCentered)
                {
                    targetPosition = focusedPosition;
                    ShowDescription();
                }
                else
                {
                    Vector3 diff = block.transform.localPosition - focusedPosition;
                    diff = diff.normalized * 300f;
                    targetPosition = focusedPosition + diff;
                }
                break;
            case BlockCombinationStatus.Indoor:
                targetPosition = combiningPosition;
                break;
            default:
                break;
        }
        this.lerpSpeed = lerpSpeed;
        targetPosition += offset;
        isMoving = true;
    }

    public void UpdatePosition(float deltaTime, float lerpSpeed = 8f)
    {
        if (isMoving && block != null)
        {
            Vector3 pos = block.transform.localPosition;
            pos = Vector3.Lerp(pos, targetPosition, deltaTime * lerpSpeed);
            if (Vector3.Distance(pos, targetPosition) < 0.05f)
            {
                pos = targetPosition;
                isMoving = false;
            }
            block.transform.localPosition = pos;
        }

        if (BlockManager.Singleton.status == BlockCombinationStatus.Divide)
        {
            if (CameraCharacter.Singleton.allowInteraction)
            {
                Vector3 eulerAngles = transform.localRotation.eulerAngles;
                var p = InputPlatform.Singleton.GetMoveVector() * InputPlatform.Singleton.ScreenSizeRatio * 0.5f;
                eulerAngles += new Vector3(-p.y, -p.x, 0);
                float x = eulerAngles.x;
                if (x > 270)
                    x -= 360f;
                x = Mathf.Clamp(x, -5f, 60f);
                eulerAngles.x = x;

                float y = eulerAngles.y;
                if (y < 0f)
                    y += 360f;
                y = Mathf.Clamp(y, 150f, 240f);
                eulerAngles.y = y;
                transform.localRotation = Quaternion.Euler(eulerAngles);
            }
        }
        else
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, originalLocalRotation, Time.deltaTime * lerpSpeed);
        }
    }
    #endregion

    #region Mono Events
    private void Awake()
    {
        originalLocalRotation = transform.localRotation;
    }
    void Start()
    {
        mainCollider = GetComponent<Collider>();
        //mainMaterial = GetComponent<MeshRenderer>().materials[0];
        rooms = transform.Find("Rooms");

        if (rooms != null)
        {
            roomsMaterialName = RoomsMeshRenderer.sharedMaterial.name;
            roomsMeshName = RoomsMeshFilter.sharedMesh.name;
        }

        CameraCharacter.Singleton.RegisterCollider(mainCollider, OnCameraCharacterEvent);
        //Defination of universal operation event
        GlobalEventManager.RegisterHandler("Unlock-" + blockName,
            (v) =>
            {
                UnlockBlock();
            }
            );

        GlobalEventManager.RegisterHandler("Lock-" + blockName,
            (v) =>
            {
                LockBlock();
            }
            );
        GlobalEventManager.RegisterHandler("Highlight-" + blockName,
            (v) =>
            {
                highlighted = true;
            }
            );

        GlobalEventManager.RegisterHandler("Dehighlight-" + blockName,
            (v) =>
            {
                highlighted = false;
            }
            );
        InitializeBlock();

    }

    void Update()
    {
        UpdatePosition(Time.deltaTime, lerpSpeed);
        UpdateClickableAttribute();
        UpdateAppearance();
        UpdateHighLightColor();
        UpdateDescription();
        if (BlockManager.Singleton.status != BlockCombinationStatus.FocusOnOneBlock)
        {
            HideDescription();
        }
    }

    void OnValidate()
    {
        subCells.Clear();
        if (subCells.Count == 0)
        {
            subCells.AddRange(GetComponentsInChildren<CellBlock>());
        }

        foreach (var c in subCells)
        {
            c.parentBlock = this;
            c.manager = manager;
        }
        block = gameObject;
    }
    #endregion

    #region Mesh Loading
    public class MeshMapper
    {
        public MeshFilter filter;
        public string meshName;
    }

    List<MeshMapper> subMesh = null;
    void InitializeSubMesh()
    {
        if (subMesh != null)
            return;
        subMesh = new List<MeshMapper>();

        foreach (var m in GetComponentsInChildren<MeshFilter>())
        {
            if (m == RoomsMeshFilter)
                continue;

            if (m.sharedMesh == null)
                continue;

            if (m.sharedMesh != Resources.Load<Mesh>(m.sharedMesh.name))
                continue;

            var p = new MeshMapper();
            p.filter = m;
            p.meshName = m.sharedMesh.name;
            subMesh.Add(p);
        }

    }
    public void LoadMesh()
    {
        InitializeSubMesh();
        if (RoomsMeshRenderer.sharedMaterial == null)
        {
            RoomsMeshRenderer.sharedMaterial = Resources.Load<Material>(roomsMaterialName);
        }

        if (RoomsMeshFilter.sharedMesh == null)
        {
            RoomsMeshFilter.sharedMesh = Resources.Load<Mesh>(roomsMeshName);
        }

        foreach(var p in subMesh)
        {
            p.filter.sharedMesh= Resources.Load<Mesh>(p.meshName);
        }
    }

    public void UnloadMesh()
    {
        InitializeSubMesh();
        if (RoomsMeshRenderer.sharedMaterial != null)
        {
            Resources.UnloadAsset(RoomsMeshRenderer.sharedMaterial);
            RoomsMeshRenderer.sharedMaterial = null;
        }

        if (RoomsMeshFilter.sharedMesh != null)
        {
            Resources.UnloadAsset(RoomsMeshFilter.sharedMesh);
            RoomsMeshFilter.sharedMesh = null;
        }

        foreach (var p in subMesh)
        {
            Resources.UnloadAsset(p.filter.sharedMesh);
            p.filter.sharedMesh = null;
        }
    }
    #endregion

    #region lock status control
    public delegate void OnLockStatusChanged(bool newStatus, GameObject block);
    public event OnLockStatusChanged onLockStatusChanged;

    public void UnlockBlock()
    {
        if (locked == true)
        {
            locked = false;
            if (onLockStatusChanged != null)
            {
                onLockStatusChanged(false, block);
            }
        }
    }

    public void LockBlock()
    {
        if (locked == true)
        {
            locked = true;
            if (onLockStatusChanged != null)
            {
                onLockStatusChanged(true, block);
            }
        }
    }
    #endregion

    #region  properties
    bool clickable;
    bool Clickable
    {
        get
        {
            return clickable;
        }
        set
        {
            if (clickable == value)
                return;
            clickable = value;
            if (clickable)
            {
                GetComponent<Collider>().enabled = true;
                //ShowAppearance();
            }
            else
            {
                GetComponent<Collider>().enabled = false;
                //HideAppearance();
            }
        }
    }
    #endregion

    #region Appearance Modify
    Color oriColor;
    Color lockColor = Color.gray;
    Color highLightColor;

    enum AppearanceType
    {
        None = 0,
        Locked,
        Normal,
        HoverIn,
        HighLight
    }

    AppearanceType appearanceType = AppearanceType.None;

    AppearanceType CurrentAppearanceType
    {
        get
        {
            return appearanceType;
        }
        set
        {
            if (appearanceType == value)
                return;

            appearanceType = value;
            if (!MeshOptimizer.allDynamicCombinerFinished)
                return;
            if (modelMaterials.Count == 0)
            {
                foreach (var m in GetComponentsInChildren<MeshRenderer>())
                {
                    if (m.GetComponentInParent<PanoramaBlock>() || m.GetComponentInParent<AlbedoRejector>() || m.gameObject.name == "Rooms")
                        continue;
                    foreach (var s in m.materials)
                    {
                        modelMaterials.Add(s);
                    }
                }
            }

            switch (value)
            {
                case AppearanceType.None:
                    break;
                case AppearanceType.Locked:
                    foreach (var m in modelMaterials)
                    {
                        if (m != null)
                            m.SetColor("_Color", lockColor);
                    }
                    if (RoomsMeshRenderer != null && RoomsMeshRenderer.sharedMaterial != null)
                    {
                        RoomsMeshRenderer.sharedMaterial.SetColor("_Color", lockColor);
                    }
                    curColor = lockColor;
                    //mainMaterial.SetColor("_Color", lockColor);
                    break;
                case AppearanceType.Normal:
                    foreach (var m in modelMaterials)
                    {
                        if (m != null)
                            m.SetColor("_Color", oriColor);
                    }
                    if (RoomsMeshRenderer != null && RoomsMeshRenderer.sharedMaterial != null)
                    {
                        RoomsMeshRenderer.sharedMaterial.SetColor("_Color", oriColor);
                    }
                    curColor = oriColor;
                    //mainMaterial.SetColor("_Color", oriColor);
                    break;
                case AppearanceType.HoverIn:
                    foreach (var m in modelMaterials)
                    {
                        if (m != null)
                            m.SetColor("_Color", oriColor);
                    }
                    if (RoomsMeshRenderer != null && RoomsMeshRenderer.sharedMaterial != null)
                    {
                        RoomsMeshRenderer.sharedMaterial.SetColor("_Color", oriColor);
                    }
                    curColor = oriColor;
                    //mainMaterial.SetColor("_Color", highLightColor);
                    break;
                case AppearanceType.HighLight:
                    //foreach (var m in modelMaterials)
                    //{
                    //    if (m != null)
                    //        m.SetColor("_Color", highLightColor);
                    //}
                    //if (RoomsMeshRenderer != null)
                    //{
                    //    RoomsMeshRenderer.material.SetColor("_Color", highLightColor);
                    //}
                    //if (ViewShellsMeshRenderer != null)
                    //{
                    //    ViewShellsMeshRenderer.material.SetColor("_Color", highLightColor);
                    //}

                    //mainMaterial.SetColor("_Color", highLightColor);
                    break;
                default: break;
            }

        }

    }


    bool descriptionOn = false;
    void ShowDescription()
    {
        descriptionOn = true;
    }

    void HideDescription()
    {
        descriptionOn = false;
    }

    bool up = true;
    Color curColor;
    Color colorDif;
    void UpdateHighLightColor()
    {
        if (CurrentAppearanceType == AppearanceType.HighLight)
        {
            if (up)
            {
                curColor.r += Time.deltaTime * 2f * colorDif.r;
                if (curColor.r > highLightColor.r)
                {
                    curColor.r = highLightColor.r;
                    up = false;
                }
            }
            else
            {
                curColor.r -= Time.deltaTime * 2f * colorDif.r;
                if (curColor.r < oriColor.r / 2f)
                {
                    curColor.r = oriColor.r / 2f;
                    up = true;
                }
            }

            curColor.g = curColor.r;
            curColor.b = curColor.r;

            foreach (var m in modelMaterials)
            {
                if (m != null)
                    m.SetColor("_Color", curColor);
            }
            if (RoomsMeshRenderer != null && RoomsMeshRenderer.sharedMaterial != null)
            {
                RoomsMeshRenderer.sharedMaterial.SetColor("_Color", curColor);
            }
        }
    }

    bool roomState = true;
    bool CurrentRoomState
    {
        get
        {
            return roomState;
        }
        set
        {
            if (roomState == value)
                return;
            roomState = value;
            if (value)
            {
                ShowRoom();
            }
            else
            {
                HideRoom();
            }
        }
    }

    void ShowRoom()
    {
        if (rooms == null)
            return;
        rooms.gameObject.SetActive(true);
    }

    void HideRoom()
    {
        if (rooms == null)
            return;
        rooms.gameObject.SetActive(false);
    }
    #endregion

    #region Inner call
    void InitializeBlock()
    {
        oriColor = Color.gray;
        highLightColor = new Color(0.7f, 0.7f, 0.7f);
        colorDif = new Color(highLightColor.r - oriColor.r, highLightColor.g - oriColor.g, highLightColor.b - oriColor.b);
    }

    bool lastFadeCmdIsIn = false;
    void UpdateDescription()
    {
        if (descriptionFSM == null)
        {
            return;
        }
        if (descriptionOn == false || BlockManager.Singleton.IsTransforming)
        {
            descriptionFSM.SendEvent("FadeOut");
        }
        else if (descriptionOn)
        {
            descriptionFSM.SendEvent("FadeIn");
        }
    }

    void UpdateAppearance()
    {
        if (locked)
        {
            if (manager.status == BlockCombinationStatus.Combine || manager.status == BlockCombinationStatus.CombineInsidePreview)
            {
                if (highlighted)
                    CurrentAppearanceType = AppearanceType.HighLight;
                else
                    CurrentAppearanceType = AppearanceType.Normal;
                if (hideWhenCombine)
                    CurrentRoomState = false;
                else
                    CurrentRoomState = true;

            }
            else
            {
                if (highlighted)
                    CurrentAppearanceType = AppearanceType.HighLight;
                else
                    CurrentAppearanceType = AppearanceType.Locked;
                CurrentRoomState = true;
            }
        }
        else
        {
            if (manager.status == BlockCombinationStatus.Combine || manager.status == BlockCombinationStatus.CombineInsidePreview)
            {
                if (highlighted)
                    CurrentAppearanceType = AppearanceType.HighLight;
                else
                    CurrentAppearanceType = AppearanceType.Normal;
                if (hideWhenCombine)
                    CurrentRoomState = false;
                else
                    CurrentRoomState = true;
            }
            else if (manager.status == BlockCombinationStatus.Divide)
            {
                CurrentRoomState = true;
                if (highlighted)
                {
                    CurrentAppearanceType = AppearanceType.HighLight;
                }
                else
                {
                    CurrentAppearanceType = AppearanceType.Normal;
                }
            }
            else
            {
                CurrentRoomState = true;
                if (highlighted)
                {
                    CurrentAppearanceType = AppearanceType.HighLight;
                }
                else
                {
                    CurrentAppearanceType = AppearanceType.Normal;
                }

            }
        }
    }

    bool mouseHovered = false;
    /// <summary> 
    /// Mouse event processing, will be override for new requirement
    /// </summary>
    /// <param name="eventType"></param>
    void OnCameraCharacterEvent(CameraCharacter.InteractionEventType eventType)
    {
        switch (eventType)
        {
            case CameraCharacter.InteractionEventType.MouseHoverOut:
                mouseHovered = false;
                break;
            case CameraCharacter.InteractionEventType.MouseHoverIn:
                mouseHovered = true;
                break;
            case CameraCharacter.InteractionEventType.MouseClickDown:
                if (manager == null)
                    break;
                if (locked == false && manager.status == BlockCombinationStatus.Divide)
                {
                    GlobalEventManager.SendEvent("BlockClicked-" + blockName);
                    manager.SetFocusBlockIndex(blockName);
                    manager.ForwardTransform();
                }
                else if (manager.status == BlockCombinationStatus.Combine || manager.status == BlockCombinationStatus.CombineInsidePreview)
                {
                    if (manager.CommandQueueLenght == 0)
                        manager.ForwardTransform();
                }
                break;
            default: break;
        }
    }



    void UpdateClickableAttribute()
    {
        switch (manager.status)
        {
            case BlockCombinationStatus.Combine:
            case BlockCombinationStatus.CombineInsidePreview:
            case BlockCombinationStatus.Divide:
                Clickable = true;
                break;
            case BlockCombinationStatus.FocusOnOneBlock:
            case BlockCombinationStatus.Indoor:
            case BlockCombinationStatus.SpecialScene:
                Clickable = false;
                break;
        }
    }
    #endregion
}
