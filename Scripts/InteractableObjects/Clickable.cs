using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class Clickable : MonoBehaviour
{
    public enum HintType
    {
        Indicator = 0,
        Flashing,
        None
    }
    #region Basic Settings
    /// <summary>
    /// Check if this clickable object is interactable
    /// </summary>
    [Header("Basic Settings"), SerializeField]
    protected bool locked = false;
    public HintType hintType = HintType.Indicator;
    public PlayMakerFSM UIFsm;


    /// <summary>
    /// Check if kill this object when first clicked
    /// </summary>
    public bool killWhenClicked = true;
    /// <summary>
    /// Detectale range for this object to show indicator or not,we do not use physical engine for blocking
    /// </summary>
    public float detectableRange = 5f;

    public bool focusWhenClicked = false;

    /// <summary>
    /// Global event to send when clicked
    /// </summary>
    [SerializeField]
    protected string clickEvent;

    [Header("Working Mode Settings")]
    public bool activeInGuideMode = true;
    public bool activeInFreeMode = false;

    public bool ActiveInCurrentMode
    {
        get
        {
            if (CameraCharacter.Singleton.GameModeC == GameMode.GuideMode && activeInGuideMode)
            {
                return true;
            }

            if (CameraCharacter.Singleton.GameModeC == GameMode.FreeMode && activeInFreeMode)
            {
                return true;
            }

            return false;
        }
    }

    [Header("Preview Settings")]
    public float viewDistance = 0.3f;
    #endregion

    #region Run time data
    Color oriColor;
    protected List<MaterialMapper> allMaterials = new List<MaterialMapper>();

    protected GameObject clickableIndicatorPrefab;
    protected GameObject clickableIndicator;
    protected Image indicatorImage;
    protected RectTransform indicatorRect;
    protected bool hasClicked = false;

    protected bool isViewing = false;
    protected bool viewFinished = false;
    protected Vector3 localOriginalPosition;
    protected Quaternion localOriginalRotation;

    public class MaterialMapper
    {
        public Material material;
        public Color oriColor;
    }

    #endregion

    #region Clickable Attribute
    bool canClick = false;
    Collider mainCollider = null;
    bool CanClick
    {
        get
        {
            return canClick;
        }
        set
        {
            if (canClick == value)
                return;

            canClick = value;
            if (mainCollider == null)
                return;
            if (canClick)
                mainCollider.enabled = true;
            else
                mainCollider.enabled = false;
        }
    }

    private void UpdateClickableAttribute()
    {
        if (BlockManager.Singleton.status == BlockCombinationStatus.Indoor || BlockManager.Singleton.status == BlockCombinationStatus.SpecialScene)
        {
            CanClick = true;
        }
        else
        {
            CanClick = false;
        }

    }
    #endregion

    #region Mono Events
    private void Awake()
    {
        //Store local transform data for viewing usage
        localOriginalPosition = transform.localPosition;
        localOriginalRotation = transform.localRotation;
        mainCollider = GetComponent<Collider>();
        mainCollider.enabled = false;
        foreach (var m in GetComponentsInChildren<MeshRenderer>(false))
        {
            if (m.materials.Length != 0)
            {
                if (!m.materials[0].HasProperty("_Color"))
                    break;

                var c = m.materials[0].GetColor("_Color");
                m.materials[0].HasProperty("_Color");
                var mapper = new MaterialMapper();
                mapper.material = m.materials[0];
                mapper.oriColor = c;
                allMaterials.Add(mapper);
            }
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(clickEvent))
            clickEvent = "ItemClicked-" + name;
    }
    // Use this for initialization
    void Start()
    {
        CameraCharacter.Singleton.RegisterCollider(GetComponent<Collider>(), OnClickHandler);
        if (GetComponent<MeshRenderer>())
        {
            //mainMaterial = GetComponent<MeshRenderer>().materials[0];
            //oriColor = mainMaterial.color;
        }

        //Register global command handlers
        GlobalEventManager.RegisterHandler("Highlight-" + name,
            Highlight
            );

        GlobalEventManager.RegisterHandler("Dehighlight-" + name,
            Dehighlight
            );

        GlobalEventManager.RegisterHandler("Lock-" + name,
            Lock
            );

        GlobalEventManager.RegisterHandler("Unlock-" + name,
            Unlock
            );
        GlobalEventManager.RegisterHandler("LookAt-" + name,
            LookAt
            );

        GlobalEventManager.RegisterHandler("Hide-" + name, Hide);
        GlobalEventManager.RegisterHandler("Display-" + name, Display);
        colorDif = new Color(highlightColor.r - 0.3f, highlightColor.g - 0.3f, highlightColor.b - 0.3f);
    }

    private void LateUpdate()
    {
        if (!ActiveInCurrentMode)
        {
            return;
        }

        UpdateDisplay();
        UpdateIndicator();
        UpdateClickableAttribute();
        UpdateColor();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Selection.activeGameObject == gameObject)
            Gizmos.DrawWireSphere(transform.position, detectableRange);
    }
#endif

    private void OnDestroy()
    {
        Destroy(clickableIndicator);
        GlobalEventManager.UnregisterHandler("Highlight-" + name,
            Highlight
            );

        GlobalEventManager.UnregisterHandler("Dehighlight-" + name,
            Dehighlight
            );

        GlobalEventManager.UnregisterHandler("Lock-" + name,
            Lock
            );

        GlobalEventManager.UnregisterHandler("Unlock-" + name,
            Unlock
            );
        GlobalEventManager.UnregisterHandler("LookAt-" + name,
            LookAt
            );
        GlobalEventManager.UnregisterHandler("Hide-" + name, Hide);
        GlobalEventManager.UnregisterHandler("Display-" + name, Display);
    }
    #endregion

    #region Global Event Handlers
    void Lock(GlobalEvent evt)
    {
        locked = true;
    }

    void Unlock(GlobalEvent evt)
    {
        locked = false;
    }

    void Highlight(GlobalEvent evt)
    {
        HighLighted = true;
    }

    void Dehighlight(GlobalEvent evt)
    {
        HighLighted = false;
    }

    /// <summary>
    /// Put the object right in front of the camera for detailed looking
    /// </summary>
    /// <param name="evt"></param>
    void Display(GlobalEvent evt)
    {
        isViewing = true;
    }

    void LookAt(GlobalEvent evt)
    {
        CameraCharacter.Singleton.RotateTo(transform);
    }

    /// <summary>
    /// Put the object back
    /// </summary>
    /// <param name="evt"></param>
    void Hide(GlobalEvent evt)
    {
        isViewing = false;
    }
    #endregion

    #region Appearance Control
    bool highlighted = false;
    public bool HighLighted
    {
        get
        {
            return highlighted;
        }
        set
        {
            if (highlighted != value)
            {
                highlighted = value;
                if (value)
                {
                    SetHighlight();
                }
                else
                {
                    SetNormal();
                }
            }
        }
    }

    void SetHighlight()
    {
        //if (mainMaterial)
        //    mainMaterial.color = highlightColor;
    }

    void SetNormal()
    {
        //if (mainMaterial)
        //    mainMaterial.color = oriColor;
    }
    #endregion

    #region Indicator Control
    bool IndicatorStatus
    {
        get
        {
            return clickableIndicator != null;
        }
        set
        {
            bool hasIndicator = clickableIndicator != null;
            if (hasIndicator != value)
            {
                if (value)
                {
                    CreateIndicator();
                }
                else
                {
                    DestroyIndicator();
                }
            }
        }
    }

    //float IndicatorAlpha
    //{
    //    get
    //    {
    //        if (indicatorImage)
    //            return indicatorImage.color.a;
    //        return 0f;
    //    }
    //    set
    //    {
    //        if (clickableIndicator == null)
    //            return;

    //        value = Mathf.Clamp(value, 0f, 1f);
    //        if (indicatorImage)
    //        {
    //            var c = indicatorImage.color;
    //            c.a = value;
    //            indicatorImage.color = c;
    //        }
    //    }
    //}

    Vector3 IndicatorRect
    {
        get
        {
            if (indicatorRect != null)
                return indicatorRect.position;
            return Vector2.zero;
        }
        set
        {
            if (clickableIndicator == null)
                return;
            if (indicatorRect != null)
            {
                indicatorRect.position = value;
            }
        }
    }

    void CreateIndicator()
    {
        if (detectableRange <= 0f)
            return;
        if (hintType != HintType.Indicator)
            return;
        if (clickableIndicator != null)
            return;
        clickableIndicatorPrefab = (GameObject)Resources.Load("ClickableIndicator");
        if (clickableIndicatorPrefab)
        {
            clickableIndicator = Instantiate(clickableIndicatorPrefab);
            if (clickableIndicator)
            {
                indicatorImage = clickableIndicator.GetComponentInChildren<Image>();
                indicatorRect = indicatorImage.GetComponent<RectTransform>();
            }
        }
    }

    void DestroyIndicator()
    {
        if (clickableIndicator == null)
            return;

        Destroy(clickableIndicator);
    }

    /// <summary>
    /// High light color when receive highlight command
    /// </summary>
    Color highlightColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    bool detected = false;
    void UpdateIndicator()
    {
        detected = false;
        if (hintType != HintType.Indicator)
        {
            IndicatorStatus = false;
            return;
        }

        if (BlockManager.Singleton.status != BlockCombinationStatus.Indoor || CameraCharacter.Singleton.InViewTransitioning ||
            !CameraCharacter.Singleton.allowInteraction)
        {
            IndicatorStatus = false;
        }
        else
        {
            Vector3 v = transform.position;
            float dis = Vector3.Distance(CameraCharacter.Singleton.ownedCamera.transform.position, v);
            if (dis > detectableRange)
            {

                IndicatorStatus = false;
                return;
            }

            if (Physics.Raycast(CameraCharacter.Singleton.ownedCamera.transform.position, transform.position - CameraCharacter.Singleton.ownedCamera.transform.position, dis, CameraCharacter.Singleton.blockLayer))
            {
                IndicatorStatus = false;
                return;
            }

            v = CameraCharacter.Singleton.ownedCamera.WorldToScreenPoint(v);

            //not in screen
            if (v.z < 0 || v.x < 0 || v.x > Screen.width || v.y < 0 || v.y > Screen.height)
            {
                IndicatorStatus = false;
                return;
            }

            if (locked)
            {
                IndicatorStatus = false;
                return;
            }

            IndicatorStatus = true;
            IndicatorRect = v;
            detected = true;

        }
    }

    bool up = true;
    Color curColor = Color.gray;
    Color colorDif;
    void UpdateColor()
    {
        if (hintType != HintType.Flashing)
        {
            return;
        }
        if (BlockManager.Singleton.status == BlockCombinationStatus.Indoor && locked == false)
        {
            if (up)
            {
                curColor.r += Time.deltaTime * colorDif.r;
                if (curColor.r > highlightColor.r)
                {
                    curColor.r = highlightColor.r;
                    up = false;
                }
            }
            else
            {
                curColor.r -= Time.deltaTime * colorDif.r;
                if (curColor.r < 0.3f)
                {
                    curColor.r = 0.3f;
                    up = true;
                }
            }

            curColor.g = curColor.r;
            curColor.b = curColor.r;
            foreach (var m in allMaterials)
            {
                m.material.SetColor("_Color", curColor);
            }
        }
        else
        {
            foreach (var m in allMaterials)
            {
                m.material.SetColor("_Color", m.oriColor);
            }
        }


    }
    #endregion

    #region View Display Control
    void UpdateDisplay()
    {
        if (isViewing)
        {
            Vector3 viewPoint = CameraCharacter.Singleton.ownedCamera.transform.position + CameraCharacter.Singleton.transform.forward * viewDistance;
            if (Vector3.Distance(transform.position, viewPoint) < 0.01f)
            {
                var p = InputPlatform.Singleton.GetMoveVector() * InputPlatform.Singleton.ScreenSizeRatio;
                float deltaX = p.x * 0.2f;
                //float deltaY = p.y * 0.2f;
                Vector3 eulerAngles = transform.rotation.eulerAngles;
                eulerAngles += new Vector3(0, -deltaX, 0);
                transform.rotation = Quaternion.Euler(eulerAngles);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, viewPoint, Time.deltaTime * 10f);
            }
        }
        else
        {
            if (Vector3.Distance(transform.localPosition, localOriginalPosition) > 0.001f)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, localOriginalPosition, Time.deltaTime * 10f);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, localOriginalRotation, Time.deltaTime * 10f);
            }
            else
            {
                transform.localRotation = localOriginalRotation;
            }
        }
    }
    #endregion

    void OnClickHandler(CameraCharacter.InteractionEventType eventType)
    {
        if (locked || !ActiveInCurrentMode)
            return;

        if (hintType == HintType.Indicator && !IndicatorStatus && detectableRange != 0f)
            return;


        switch (eventType)
        {
            case CameraCharacter.InteractionEventType.MouseClickDown:
                GlobalEventManager.SendEvent(clickEvent);
                if (!hasClicked)
                {
                    hasClicked = true;
                    if (hintType == HintType.Indicator && indicatorImage != null)
                    {
                        indicatorImage.GetComponentInChildren<PlayMakerFSM>().enabled = false;
                        indicatorImage.rectTransform.localScale = new Vector3(1f, 1f, 1f);
                    }

                }
                if (focusWhenClicked)
                {
                    CameraCharacter.Singleton.RotateTo(transform);
                }
                if (UIFsm)
                {
                    UIFsm.SendEvent("FadeIn");
                }
                if (killWhenClicked)
                {
                    Destroy(gameObject);
                }

                break;
            case CameraCharacter.InteractionEventType.MouseHoverIn:
                //SetHighlight();
                break;
            case CameraCharacter.InteractionEventType.MouseHoverOut:
                SetNormal();
                break;
            default: break;
        }

    }
}
