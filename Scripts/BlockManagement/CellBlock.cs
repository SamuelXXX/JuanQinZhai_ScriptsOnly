using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The handler of the cell block
/// </summary>
public class CellBlock : MonoBehaviour
{
    #region Settings and run-time data
    //basic settings
    public bool locked = true;//check is this cell is accessible
    public bool highlighted = false;
    public string roomName = "书房";
    public string cellName
    {
        get
        {
            return name;
        }
    }//name of this block,usually set as object's name
    public Transform roomViewCenter;//the view center of this cell

    //position info,built automatically
    [HideInInspector]
    public Vector3 focusedPosition;//position
    [HideInInspector]
    public Vector3 combiningPosition;//position when
    [HideInInspector]
    public Vector3 dividingPosition;
    [HideInInspector]
    public BlockManager manager;
    [HideInInspector]
    public BlockInfo parentBlock;
    public PanoramaBlock panoramaContent;


    //running data
    protected bool isMoving = false;
    protected Vector3 targetPosition;
    protected GameObject roomIndicatorPrefab;
    protected GameObject roomIndicator;
    protected Image roomIndicatorImage;
    protected Text roomIndicatorText;
    protected Button roomIndicatorHotArea;
    protected RectTransform roomIndicatorRect;

    #endregion

    #region Mono Events
    void Start()
    {
        //Create room indicator
        roomIndicatorPrefab = (GameObject)Resources.Load("RoomIndicator");
        if (roomIndicatorPrefab)
        {
            roomIndicator = Instantiate(roomIndicatorPrefab);
            if (roomIndicator)
            {
                roomIndicatorText = roomIndicator.GetComponentInChildren<Text>();
                roomIndicatorImage = roomIndicator.GetComponentInChildren<Image>();
                roomIndicatorHotArea = roomIndicator.GetComponentInChildren<Button>();
                roomIndicatorHotArea.onClick.AddListener(OnIndicatorClick);
                roomIndicatorRect = roomIndicatorImage.GetComponent<RectTransform>();
                IndicatorStatus = false;
                IndicatorText = roomName;
            }
        }

        //Defination of global operation event
        GlobalEventManager.RegisterHandler("Unlock-" + cellName + "-" + parentBlock.name,
            (v) =>
            {
                UnlockCell();
            }
            );

        GlobalEventManager.RegisterHandler("Lock-" + cellName + "-" + parentBlock.name,
            (v) =>
            {
                LockCell();
            }
            );
        GlobalEventManager.RegisterHandler("Highlight-" + cellName + "-" + parentBlock.name,
            (v) =>
            {
                highlighted = true;
            }
            );

        GlobalEventManager.RegisterHandler("Dehighlight-" + cellName + "-" + parentBlock.name,
            (v) =>
            {
                highlighted = false;
            }
            );
        InitializeCell();
    }

    void LateUpdate()
    {
        //after camera
        UpdateIndicator();
    }

    void OnValidate()
    {
        if (roomViewCenter == null)
            roomViewCenter = transform;

        if (panoramaContent == null)
            panoramaContent = GetComponentInChildren<PanoramaBlock>();
    }
    #endregion

    #region Room indicator control
    bool indicatorStatus = true;
    bool IndicatorStatus
    {
        get
        {
            return indicatorStatus;
        }
        set
        {
            if (indicatorStatus == value)
                return;
            indicatorStatus = value;
            if (roomIndicatorImage)
            {
                if (value)
                {
                    roomIndicatorImage.gameObject.SetActive(true);
                }
                else
                {
                    roomIndicatorImage.gameObject.SetActive(false);
                }
            }

        }
    }

    string IndicatorText
    {
        get
        {
            if (roomIndicatorText)
                return roomIndicatorText.text;

            return null;
        }
        set
        {
            if (roomIndicatorText)
                roomIndicatorText.text = value;
        }
    }

    float IndicatorTextAlpha
    {
        get
        {
            if (roomIndicatorText)
                return roomIndicatorText.color.a;
            return 0f;
        }
        set
        {
            value = Mathf.Clamp(value, 0f, 1f);
            if (roomIndicatorText)
            {
                var c = roomIndicatorText.color;
                c.a = value;
                roomIndicatorText.color = c;
            }
        }
    }

    float IndicatorAlpha
    {
        get
        {
            if (roomIndicatorImage)
                return roomIndicatorImage.color.a;
            return 0f;
        }
        set
        {
            value = Mathf.Clamp(value, 0f, 1f);
            if (roomIndicatorImage)
            {
                var c = roomIndicatorImage.color;
                c.a = value;
                roomIndicatorImage.color = c;
            }
        }
    }

    Vector3 IndicatorRect
    {
        get
        {
            if (roomIndicatorRect != null)
                return roomIndicatorRect.position;
            return Vector2.zero;
        }
        set
        {
            if (roomIndicatorRect != null)
            {
                roomIndicatorRect.position = value;
            }
        }
    }

    bool hasClicked = false;
    void OnIndicatorClick()
    {
        if (locked == false && !manager.IsTransforming && CameraCharacter.Singleton.allowInteraction)
        {
            hasClicked = true;
            GlobalEventManager.SendEvent("CellClicked-" + cellName + "-" + parentBlock.name);
            manager.ForwardTransform(roomViewCenter.position, roomViewCenter.rotation, panoramaContent);
            
        }
    }

    float offset = 0f;
    bool offsetGoUp = true;
    void UpdateIndicator()
    {
        if (roomIndicator == null)
            return;

        if (manager.status != BlockCombinationStatus.FocusOnOneBlock || CameraCharacter.Singleton.FadingStatus == CameraCharacter.MaskFadingStatus.FadingOut)
        {
            IndicatorStatus = false;
            offset = 0f;
        }
        else
        {
            Vector3 v = transform.position + transform.up * 2f;
            float dis = Vector3.Distance(CameraCharacter.Singleton.ownedCamera.transform.position, v);
            if (dis > 80f)
            {
                IndicatorStatus = false;
                return;
            }
            v = CameraCharacter.Singleton.ownedCamera.WorldToScreenPoint(v);
            if (!hasClicked && !locked)
                v.y += offset;
            //not in screen
            if (v.z < 0 || v.x < 0 || v.x > Screen.width || v.y < 0 || v.y > Screen.height)
            {
                IndicatorStatus = false;
                return;
            }

            IndicatorRect = v;
            switch (roomIndicatorText.text.Length)
            {
                case 2:
                    roomIndicatorText.lineSpacing = 2f;
                    break;
                case 3:
                    roomIndicatorText.lineSpacing = 1.2f;
                    break;
                default:
                    roomIndicatorText.lineSpacing = 1f;
                    break;
            }

            if (InputPlatform.Singleton.GetTouching() && CameraCharacter.Singleton.allowInteraction)
            {
                IndicatorAlpha = 0f;
                IndicatorTextAlpha = 0f;
            }
            else
            {
                IndicatorTextAlpha = 1f;
                if (locked)
                    IndicatorAlpha = 0.4f;
                else
                    IndicatorAlpha = 1f;
            }




            IndicatorStatus = true;
            if (offsetGoUp)
            {
                offset += Time.deltaTime * 20f;
            }
            else
            {
                offset -= Time.deltaTime * 20f;
            }

            if (offset > 20f)
            {
                offsetGoUp = false;
            }

            if (offset < 0f)
            {
                offsetGoUp = true;
            }


        }
    }
    #endregion

    #region lock status control
    public delegate void OnLockStatusChanged(bool newStatus, GameObject block);
    public event OnLockStatusChanged onLockStatusChanged;

    public void UnlockCell()
    {
        if (locked == true)
        {
            locked = false;
            if (onLockStatusChanged != null)
            {
                onLockStatusChanged(false, gameObject);
            }
        }
    }

    public void LockCell()
    {
        if (locked == true)
        {
            locked = true;
            if (onLockStatusChanged != null)
            {
                onLockStatusChanged(true, gameObject);
            }
        }
    }
    #endregion

    #region Appearance Modify
    Color oriColor;
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
            switch (value)
            {
                case AppearanceType.None:
                    break;
                case AppearanceType.Locked:
                    //mainMaterial.SetColor("_Color", lockColor);
                    break;
                case AppearanceType.Normal:
                    //mainMaterial.SetColor("_Color", oriColor);
                    break;
                case AppearanceType.HoverIn:
                    //mainMaterial.SetColor("_Color", highLightColor);
                    break;
                case AppearanceType.HighLight:
                    //mainMaterial.SetColor("_Color", highLightColor);
                    break;
                default: break;
            }

        }

    }

    void ShowAppearance()
    {
        GetComponent<MeshRenderer>().enabled = true;
    }

    void HideAppearance()
    {
        GetComponent<MeshRenderer>().enabled = false;
    }
    #endregion

    #region Inner call
    void InitializeCell()
    {
        //oriColor = mainMaterial.GetColor("_Color");
    }
    #endregion
}
