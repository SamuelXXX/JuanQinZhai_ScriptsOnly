using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputPlatform : MonoBehaviour
{
    private static InputPlatform singleton;
    public static InputPlatform Singleton
    {
        get
        {
            return singleton;
        }
        private set
        {
            singleton = value;
        }
    }

    /// <summary>
    /// Used to normalize input in different resolution screens
    /// </summary>
    public float ScreenSizeRatio
    {
        get
        {
            return 1920f / Screen.width;
        }
    }

#if TOUCH_DEBUG
    public Image touchPointUI;
#endif


    //for Button UI component,because GlobalEventManager is not in scene
    #region Events sending
    public void SendBackEvent()
    {
        GlobalEventManager.SendEvent(GlobalEventManager.Back);
    }

    public void SendDrawUpClickEvent()
    {
        GlobalEventManager.SendEvent(GlobalEventManager.DrawUpClicked);
    }


    bool tableOpened = false;

    bool allowInteractionStack;
    bool viewFreezedStack;
    public void OpenInfoTbl()
    {
        if (tableOpened)
            return;
        UISystem.Singleton.SetContent("SW_Ref", true);
        viewFreezedStack = CameraCharacter.Singleton.ViewFreezed;
        allowInteractionStack = CameraCharacter.Singleton.allowInteraction;
        CameraCharacter.Singleton.allowInteraction = false;
        CameraCharacter.Singleton.FreezeView();
        tableOpened = true;
    }

    public void CloseInfoTbl()
    {
        if (!tableOpened)
            return;
        UISystem.Singleton.SetContent("SW_Ref", false);
        CameraCharacter.Singleton.allowInteraction = allowInteractionStack;
        if (viewFreezedStack)
            CameraCharacter.Singleton.FreezeView();
        else
            CameraCharacter.Singleton.UnfreezeView();
        tableOpened = false;
    }

    public void SendEvent(string evtName)
    {
        GlobalEventManager.SendEvent(evtName);
    }
    #endregion

    #region Mono Events
    private void Awake()
    {
        Singleton = this;
    }

    private void Update()
    {
        ProcessKeyboard();

        //Update delta position when holding,the built-in function of 'Input' does not work properly
        //Only for mobile
        if (Input.touches.Length != 0 && Input.touches[0].phase != TouchPhase.Ended)
        {
            if (lastTouchPosition == null)
            {
                deltaTouchedPosition = Vector2.zero;
            }
            else
            {
                deltaTouchedPosition = Input.touches[0].position - lastTouchPosition.Value;
            }
            lastTouchPosition = Input.touches[0].position;
        }
        else
        {
            lastTouchPosition = null;
            deltaTouchedPosition = null;
        }

        //only for pc
        if (Input.GetMouseButton(0))
        {
            if (lastMouseHoldPoint == null)
            {
                deltaMouseHoldPoint = Vector3.zero;
            }
            else
            {
                deltaMouseHoldPoint = Input.mousePosition - lastMouseHoldPoint.Value;
            }
            lastMouseHoldPoint = Input.mousePosition;
        }
        else
        {
            lastMouseHoldPoint = null;
            deltaMouseHoldPoint = null;
        }

        foreach (var go in gestureOperations)
        {
            go.UpdateOperation();
        }


    }

    private void LateUpdate()
    {
#if TOUCH_DEBUG
        var v = GetTouchPoint();
        if (touchPointUI)
        {
            if (v != null)
            {
                touchPointUI.rectTransform.position = v.Value;
                touchPointUI.enabled = true;
            }
            else
            {
                touchPointUI.enabled = false;
            }
        }
#endif
    }

    /// <summary>
    /// For pc debug only
    /// </summary>
    void ProcessKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SendBackEvent();

        if (Input.GetKeyDown(KeyCode.N))
            SendDrawUpClickEvent();
    }
    #endregion

    #region General Input Tool Methods
    public Vector3? GetTouchPoint()
    {
        if (Input.touches.Length != 0)
        {
            return Input.touches[0].position;
        }
        if (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
        {
            return Input.mousePosition;
        }
        return null;
    }

    public bool GetTouchDown()
    {
        bool pc = Input.GetMouseButtonDown(0);
        bool mobile = false;
        if (Input.touches.Length != 0)
        {
            mobile = (Input.touches[0].phase == TouchPhase.Began);
        }
        return pc || mobile;
    }

    public bool GetTouchUp()
    {
        bool pc = Input.GetMouseButtonUp(0);
        bool mobile = false;
        if (Input.touches.Length != 0)
        {
            mobile = (Input.touches[0].phase == TouchPhase.Ended);
        }
        return pc || mobile;
    }

    public bool GetTouching()
    {
        bool pc = Input.GetMouseButton(0);
        bool mobile = false;
        if (Input.touches.Length != 0 && Input.touches[0].phase != TouchPhase.Ended)
        {
            mobile = true;
        }
        return pc || mobile;
    }

    /// <summary>
    /// Double finger scale
    /// </summary>
    /// <returns></returns>
    public float GetScaleLength()
    {
        if (Input.GetMouseButton(1))
        {
            return Input.mousePosition.magnitude;
        }
        else if (Input.touches.Length >= 2)
        {
            if (Input.touches[0].phase != TouchPhase.Ended && Input.touches[1].phase != TouchPhase.Ended)
            {
                return Vector3.Distance(Input.touches[0].position, Input.touches[1].position);
            }
        }
        return 0f;
    }

    Vector2? lastTouchPosition;
    Vector2? deltaTouchedPosition;
    Vector3? lastMouseHoldPoint;
    Vector3? deltaMouseHoldPoint;
    public Vector3 GetMoveVector()
    {
        Vector3 ret = Vector3.zero;

        if (deltaTouchedPosition != null)//mobile
        {
            ret.x = deltaTouchedPosition.Value.x;
            ret.y = deltaTouchedPosition.Value.y;
            return ret;
        }

        if (deltaMouseHoldPoint != null)//pc
        {
            ret.x = deltaMouseHoldPoint.Value.x;
            ret.y = deltaMouseHoldPoint.Value.y;
            return ret;
        }
        return ret;
    }
    #endregion

    #region Gesture Control
    uint idPool = 1;
    /// <summary>
    /// Push a holding gesture operation command
    /// </summary>
    /// <param name="holdingPoint"></param>
    /// <param name="progressTime"></param>
    /// <param name="roundDistance"></param>
    /// <returns></returns>
    public GestureOperationStatus PushHoldingGestureCommand(Vector3 holdingPoint, float progressTime, float roundDistance)
    {
        var go = GestureOperation.CreateHoldingGestureCommand(holdingPoint, progressTime, roundDistance);
        go.id = idPool++;
        gestureOperations.Add(go);
        return GestureOperationStatus.CreateGestureStatus(go);
    }

    /// <summary>
    /// Push a lining gesture operation command
    /// </summary>
    /// <param name="originPoint"></param>
    /// <param name="endPoint"></param>
    /// <param name="roundDistance"></param>
    /// <returns></returns>
    public GestureOperationStatus PushLiningGestureCommand(Vector3 originPoint, Vector3 endPoint, float roundDistance)
    {
        var go = GestureOperation.CreateLiningGestureCommand(originPoint, endPoint, roundDistance);
        go.id = idPool++;
        gestureOperations.Add(go);
        return GestureOperationStatus.CreateGestureStatus(go);
    }

    public void RemoveGestureCommand(uint gestureId)
    {
        gestureOperations.RemoveAll(a =>
        {
            return a.id == gestureId;
        });
    }

    List<GestureOperation> gestureOperations = new List<GestureOperation>();
    #endregion

#if GUI_DEBUG
    private void OnGUI()
    {
        string[] evtArray = GlobalEventManager.GetAllEvents();
        GUILayout.Label("All Events");
        Color c = GUI.color;
        GUI.color = Color.blue;
        foreach (var s in evtArray)
        {
            GUILayout.Label(s);
        }

        foreach (var s in gestureOperations)
        {

            s.OnGUI();

        }

        Vector3? tp = GetTouchPoint();
        GUILayout.Label("TouchPoint:" + (tp == null ? "0,0,0" : tp.Value.ToString()));
        if (tp != null)
            GUI.Label(new Rect(tp.Value.x, Screen.height - tp.Value.y, 100, 100), "Touch Here");
        GUI.color = c;
    }
#endif
}

public enum GestureType
{
    Holding = 0,
    DrawLine
}

/// <summary>
/// Gesture operation handler
/// </summary>
public class GestureOperation
{
    #region Basic Settings
    public GestureType type = GestureType.DrawLine;
    public Vector3 originalPoint;
    public Vector3 endPoint;
    public float progressTime = 1f;
    public uint id;
    #endregion

    float roundDistance = 10f;

    #region Running data
    //status
    public bool isValid;
    public bool isDone;
    public float progress;

    Vector3 boundPoint;

    public enum LiningGestureStatus
    {
        NoTouching = 0,
        HasTouched,
        GestureBegin,
        BadGesture,
        GestureFinished
    }
    LiningGestureStatus liningGestureStatus;

    PointPositionType positionType = PointPositionType.OutsideSqure;
    #endregion


    public void UpdateOperation()
    {
        if (isDone)
            return;
        Vector3? tp = InputPlatform.Singleton.GetTouchPoint();
        switch (type)
        {
            case GestureType.Holding:
                if (tp == null)
                {
                    progress = 0;
                }
                else
                {
                    if (Vector3.Distance(tp.Value, originalPoint) > roundDistance)
                    {
                        progress = 0;
                    }
                    else
                    {
                        progress += Time.deltaTime / progressTime;
                    }
                    if (progress >= 1f)
                    {
                        progress = 1f;
                        isDone = true;
                    }
                }
                break;
            case GestureType.DrawLine:

                if (tp != null)
                {
                    positionType = CalculatePositionType(tp.Value);
                }

                switch (liningGestureStatus)
                {
                    case LiningGestureStatus.NoTouching:
                        if (tp != null)
                        {
                            liningGestureStatus = LiningGestureStatus.HasTouched;
                        }
                        break;
                    case LiningGestureStatus.HasTouched:
                        if (tp == null)
                        {
                            liningGestureStatus = LiningGestureStatus.NoTouching;
                            progress = 0;
                        }
                        else
                        {
                            if (positionType == PointPositionType.InsideEdgeSqure)
                            {
                                liningGestureStatus = LiningGestureStatus.GestureBegin;
                            }
                        }
                        break;
                    case LiningGestureStatus.GestureBegin:
                        if (tp == null)
                        {
                            liningGestureStatus = LiningGestureStatus.NoTouching;
                            progress = 0;
                        }
                        else
                        {
                            if (positionType == PointPositionType.OutsideSqure)
                            {
                                liningGestureStatus = LiningGestureStatus.BadGesture;
                                break;
                            }
                            if (positionType == PointPositionType.OutTargetPoint)
                            {
                                liningGestureStatus = LiningGestureStatus.GestureFinished;
                                progress = 1f;
                                isDone = true;
                                break;
                            }
                            if (positionType == PointPositionType.InsideStrictSqure)
                                boundPoint = refBoundPoint;
                            Vector3 diff = tp.Value - originalPoint;
                            Vector3 n = endPoint - originalPoint;
                            float a = Vector3.Dot(diff, n.normalized);
                            float b = n.magnitude;
                            float newProgress = a / b;
                            newProgress = Mathf.Clamp(newProgress, 0f, 1f);
                            if (newProgress > progress)
                            {
                                progress = newProgress;
                            }
                        }
                        break;
                    case LiningGestureStatus.BadGesture:
                        if (tp == null)
                        {
                            liningGestureStatus = LiningGestureStatus.NoTouching;
                            progress = 0;
                        }
                        break;
                    case LiningGestureStatus.GestureFinished:
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }
    }

    public enum PointPositionType
    {
        InsideStrictSqure,
        InsideEdgeSqure,
        OutsideSqure,
        OutTargetPoint
    }

    PointPositionType CalculatePositionType(Vector3 p)
    {
        Vector3 norUp;
        Vector3 norDown;
        Vector3 norLeft;
        Vector3 norRight;

        bool underEdgeLine = true;
        bool underStrictEdgeLine = true;
        bool aboveBottemLine = true;
        bool rightLeftEdge = true;
        bool leftRightEdge = true;

        norUp = (endPoint - boundPoint).normalized;
        norDown = -norUp;
        norLeft = Vector3.Cross(norUp, Vector3.forward);
        norRight = -norLeft;

        float a = 0;

        a = Vector3.Dot(p, norDown) - Vector3.Dot(endPoint, norDown);
        if (a < 0)
        {
            aboveBottemLine = false;
        }

        a = Vector3.Dot(p, norRight) - Vector3.Dot(endPoint, norRight);
        if (a < -roundDistance)
        {
            rightLeftEdge = false;
        }
        else if (a > roundDistance)
        {
            leftRightEdge = false;
        }

        a = Vector3.Dot(p, norUp) + roundDistance - Vector3.Dot(boundPoint, norUp);
        if (a < 0)
        {
            underEdgeLine = false;
            underStrictEdgeLine = false;
        }
        else if (a < roundDistance)
        {
            underStrictEdgeLine = false;
        }



        if (leftRightEdge && rightLeftEdge && !aboveBottemLine)
        {
            return PointPositionType.OutTargetPoint;
        }
        else if (leftRightEdge && rightLeftEdge && underStrictEdgeLine)
        {
            a -= roundDistance;
            refBoundPoint = boundPoint + norUp * a;
            return PointPositionType.InsideStrictSqure;
        }
        else if (leftRightEdge && rightLeftEdge && underEdgeLine)
        {
            return PointPositionType.InsideEdgeSqure;
        }
        else
        {
            return PointPositionType.OutsideSqure;
        }


    }

    Vector3 refBoundPoint = Vector3.zero;


    public static GestureOperation CreateHoldingGestureCommand(Vector3 holdingPoint, float progressTime, float roundDistance)
    {
        GestureOperation go = new GestureOperation();
        go.originalPoint = holdingPoint;
        go.progressTime = progressTime;
        go.roundDistance = roundDistance;
        go.isValid = true;
        go.isDone = false;
        go.progress = 0;

        return go;
    }


    public static GestureOperation CreateLiningGestureCommand(Vector3 originPoint, Vector3 endPoint, float roundDistance)
    {
        GestureOperation go = new GestureOperation();
        go.originalPoint = originPoint;
        go.endPoint = endPoint;
        go.roundDistance = roundDistance;

        go.boundPoint = originPoint;


        go.isValid = true;
        go.isDone = false;
        return go;
    }

    private GestureOperation()
    {

    }

    public void OnGUI()
    {
        if (type == GestureType.DrawLine)
        {
            Color c = GUI.color;
            GUI.color = Color.red;
            GUILayout.Label("LiningStatus:" + liningGestureStatus.ToString());
            GUILayout.Label("PositionType:" + positionType.ToString());
            GUI.Label(new Rect(boundPoint.x, Screen.height - boundPoint.y, 100, 100), "X");
            GUI.Label(new Rect(originalPoint.x, Screen.height - originalPoint.y, 100, 100), "0");
            GUI.Label(new Rect(endPoint.x, Screen.height - endPoint.y, 100, 100), "1");
            GUI.color = c;
        }
    }

}

/// <summary>
/// Gesture operation status accessor
/// </summary>
public class GestureOperationStatus
{
    GestureOperation mOperation;
    public static GestureOperationStatus CreateGestureStatus(GestureOperation operation)
    {
        GestureOperationStatus s = new GestureOperationStatus();
        s.mOperation = operation;
        return s;
    }

    private GestureOperationStatus()
    {

    }

    #region Status
    public uint id
    {
        get
        {
            if (mOperation == null)
            {
                return 0;
            }

            return mOperation.id;
        }
    }

    public bool isValid
    {
        get
        {
            if (mOperation == null)
            {
                return true;
            }

            return mOperation.isValid;
        }
    }
    public bool isDone
    {
        get
        {
            if (mOperation == null)
            {
                return false;
            }
            return mOperation.isDone;
        }
    }

    public float progress
    {
        get
        {
            if (mOperation == null)
            {
                return 0;
            }
            return mOperation.progress;
        }
    }
    #endregion
}

