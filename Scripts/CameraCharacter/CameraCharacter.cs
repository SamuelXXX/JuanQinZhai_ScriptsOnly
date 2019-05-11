using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum CameraViewMode
{
    FreeView = 0,//Free view when view the block structure
    FixedView,
    HorizontalFreeView,//Free view when indoor  
    HorizontalSlideView,
    HorizontalHalfFreeView
}

public enum CameraScaleMode
{
    None = 0,
    ScaleFov,
    MoveCamera
}

public enum ViewContentType
{
    Model3DView = 0,
    PanoramaView
}

public enum GameMode
{
    None = 0,
    GuideMode,
    FreeMode
}

public class CameraCharacter : MonoBehaviour
{
    #region Basic Settings
    [Header("Basic Settings"), UnityEngine.Tooltip("Camera mask to fulfil fade in-out operation")]
    public Image cameraMask;
    //public LineRenderer rayLine;
    //public Image touchHint;
    //public GameObject hitHint;
    public LayerMask modelViewMask;
    public LayerMask panoramaMask;
    [UnityEngine.Tooltip("Camera mask to fulfil fade in-out operation in world layer")]
    public GameObject cameraFocusMask;
    public float slideViewRange = 10f;
    public float sensitivityHor = 9.0f;
    public float sensitivityVert = 9.0f;
    public float slideViewSensitivity = 1f;
    public float floatLamda = 0.1f;

    [Header("Predefined View Settings")]
    public Vector3 fixedViewPosition;
    public Quaternion fixedViewRotation;
    public Vector3 lookDownViewPosition;
    public Quaternion lookDownViewRotation;
    #endregion

    #region Singleton
    protected static CameraCharacter singleton;
    public static CameraCharacter Singleton
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
    #endregion

    #region Run-Time data
    bool focusOnCenter = false;
    bool autoRotating = false;
    bool positionDirty = false;
    bool isRotatingToTarget = false;
    Vector3 positionBeforeRotating;
    Quaternion rotationBeforeRotating;
    private Camera m_camera = null;

    GameMode gameMode = GameMode.None;

    public GameMode GameModeC
    {
        get
        {
            return gameMode;
        }
    }
    public Camera ownedCamera
    {
        get
        {
            return m_camera;
        }
        private set
        {
            m_camera = value;
        }
    }
    #endregion

    #region Mono Events
    void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Singleton = this;
        }
        lnLamda = Mathf.Log(floatLamda);
        ownedCamera = GetComponentInChildren<Camera>();
        GlobalEventManager.RegisterHandler("StartCameraRotating", StartAutoRotating);
        GlobalEventManager.RegisterHandler("StopCameraRotating", StopAutoRotating);
        GlobalEventManager.RegisterHandler("StartCameraFocus", FocusOnCenter);
        GlobalEventManager.RegisterHandler("StopCameraFocus", CancelFocusOnCenter);
        //Call web javascript function
        Application.ExternalCall("Func", 0);
    }

    // Update is called once per frame
    void Update()
    {
        PrepareInputData();//Prepare input data
        ProcessPushPopCommand();
        ProcessViewInteraction();//Process interaction with some interactable colliders
        UpdateView();//Movement control
        UpdateAutoRotating();
        LateUpdateView();
        UpdateFPS();
    }

    //private void LateUpdate()
    //{
    //    var t = InputPlatform.Singleton.GetTouchPoint();
    //    hitHint.SetActive(false);
    //    if (t != null)
    //    {
    //        if (rayLine)
    //        {
    //            Ray ray = Camera.main.ScreenPointToRay(t.Value);

    //            rayLine.positionCount = 2;
    //            rayLine.SetPosition(0, ray.origin);
    //            rayLine.SetPosition(1, ray.origin + ray.direction * 150f);

    //            if (hitHint)
    //            {
    //                RaycastHit hit = new RaycastHit();
    //                if (Physics.Raycast(ray, out hit, 180f, -1))
    //                {
    //                    hitHint.transform.position = hit.point;
    //                    hitHint.SetActive(true);
    //                }
    //            }
    //        }

    //        if (touchHint)
    //        {
    //            touchHint.GetComponent<RectTransform>().position = t.Value;
    //            touchHint.enabled = true;
    //        }



    //    }
    //    else
    //    {
    //        if (rayLine)
    //            rayLine.positionCount = 0;

    //        if (touchHint)
    //        {
    //            touchHint.enabled = false;
    //        }
    //    }
    //}
    #endregion

    #region Focus Mask Operation
    public void Focus(float focusDistance)
    {
        if (cameraFocusMask != null)
        {
            StopCoroutine("CancelFocusRoutine");
            cameraFocusMask.transform.localPosition = new Vector3(0, 0, focusDistance);
            StartCoroutine(FocusRoutine());
        }
    }

    public void CancelFocus()
    {
        if (cameraFocusMask != null)
        {
            StopCoroutine("FocusRoutine");
            StartCoroutine(CancelFocusRoutine());
        }
    }

    IEnumerator FocusRoutine()
    {
        MeshRenderer renderer = cameraFocusMask.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            Color color = mat.GetColor("_Color");
            while (true)
            {
                color.a += Time.deltaTime * 0.8f;
                if (color.a >= 0.76f)
                {
                    color.a = 0.76f;
                    mat.SetColor("_Color", color);
                    break;
                }
                mat.SetColor("_Color", color);
                yield return null;
            }
        }

        yield return null;
    }

    IEnumerator CancelFocusRoutine()
    {
        MeshRenderer renderer = cameraFocusMask.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            Color color = mat.GetColor("_Color");
            while (true)
            {
                color.a -= Time.deltaTime * 0.8f;
                if (color.a <= 0)
                {
                    color.a = 0f;
                    mat.SetColor("_Color", color);
                    break;
                }
                mat.SetColor("_Color", color);
                yield return null;
            }
        }
        yield return null;
    }
    #endregion

    #region Interaction with colliders
    [Header("Interaction Settings")]
    public LayerMask interactionBlockLayer;
    public LayerMask blockLayer;

    public enum InteractionEventType
    {
        MouseHoverIn = 0,
        MouseHoverOut,
        MouseClickDown
    }

    public void RegisterCollider(Collider col, OnInteractionEvents handler)
    {
        if (col == null || handler == null)
            return;

        if (interactionEvents.ContainsKey(col))
            interactionEvents[col] = handler;
        else
            interactionEvents.Add(col, handler);
    }

    public void UnregisterCollider(Collider col)
    {
        if (col == null)
            return;

        interactionEvents.Remove(col);
    }


    public Dictionary<Collider, OnInteractionEvents> interactionEvents = new Dictionary<Collider, OnInteractionEvents>();

    public delegate void OnInteractionEvents(InteractionEventType eventType);

    void CallMouseInteractionEvents(Collider col, InteractionEventType eventType)
    {
        if (interactionEvents.ContainsKey(col) && interactionEvents[col] != null)
        {
            interactionEvents[col](eventType);
        }
    }

    public Collider currentHoveringCollider = null;
    protected Collider touchDownCollider = null;
    protected Vector3 mouseButtonDownPos;
    protected bool mouseClickInvalid = false;
    public bool allowInteraction = true;

    /// <summary>
    /// Event processing from character,will be override for new requirement
    /// </summary>
    void ProcessViewInteraction()
    {


        //In special condition,do not interact
        if (InViewTransitioning || !allowInteraction)
        {
            if (currentHoveringCollider != null)
            {
                CallMouseInteractionEvents(currentHoveringCollider, InteractionEventType.MouseHoverOut);
                currentHoveringCollider = null;
            }

            mouseClickInvalid = true;
            return;
        }

        //only for pc mouse hovering
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(mouseRay, out hit, 150f, interactionBlockLayer))
        {
            if (currentHoveringCollider != hit.collider)
            {
                if (currentHoveringCollider != null)
                {
                    CallMouseInteractionEvents(currentHoveringCollider, InteractionEventType.MouseHoverOut);
                }
                CallMouseInteractionEvents(hit.collider, InteractionEventType.MouseHoverIn);
                currentHoveringCollider = hit.collider;
            }
        }
        else
        {
            if (currentHoveringCollider != null)
            {
                CallMouseInteractionEvents(currentHoveringCollider, InteractionEventType.MouseHoverOut);
                currentHoveringCollider = null;
            }
        }


        //Get touch down information
        if (InputPlatform.Singleton.GetTouchDown())
        {
            var tp = InputPlatform.Singleton.GetTouchPoint();
            mouseButtonDownPos = tp.Value;
            mouseClickInvalid = false;
        }

        //Click valid check
        if (InputPlatform.Singleton.GetTouching())
        {
            if (Vector3.Distance(InputPlatform.Singleton.GetTouchPoint().Value, mouseButtonDownPos) > 3f)
            {
                mouseClickInvalid = true;
            }
        }

        //Click Check
        if (InputPlatform.Singleton.GetTouchUp())
        {
            if (!mouseClickInvalid)
            {
                Vector3? v = InputPlatform.Singleton.GetTouchPoint();
                if (v == null)
                    return;
                Ray mRay = Camera.main.ScreenPointToRay(v.Value);
                RaycastHit h = new RaycastHit();
                if (Physics.Raycast(mRay, out h, 150f, interactionBlockLayer))
                {
                    CallMouseInteractionEvents(h.collider, InteractionEventType.MouseClickDown);
                }
            }
        }
    }
    #endregion

    #region View Stack Operation
    public delegate bool FadeinReadyFlagRead();
    public struct ViewParameters
    {
        public CameraViewMode viewMode;//The mode of camera movement
        public ViewContentType viewContentType;//The view content type,3d model or panorama
        public CameraScaleMode cameraScaleMode;//Scale fov or z-offset when make double-fingers scale command
        public float cameraZOffset;//Initia camera z-offset to center
        public float cameraXOffset;
        public Vector3? initialPosition;//Initial position
        public Quaternion? initialRotation;//Initial rotation

        public FadeinReadyFlagRead FadeInFlag;//Fade in flag condition to commit fading in operation
        public bool forceFading;//should this flag be forced to fade in and fade out 
        //Macro view settings
        public static ViewParameters GeneralPreview
        {
            get
            {
                ViewParameters vp = new ViewParameters();
                vp.viewMode = CameraViewMode.FreeView;
                vp.viewContentType = ViewContentType.Model3DView;
                vp.cameraScaleMode = CameraScaleMode.None;
                vp.FadeInFlag = null;
                vp.cameraZOffset = 30f;
                vp.cameraXOffset = 0f;
                vp.initialPosition = null;
                vp.initialRotation = null;
                vp.forceFading = false;
                return vp;
            }
        }

        public static ViewParameters FocusPreview
        {
            get
            {
                ViewParameters vp = new ViewParameters();
                vp.viewMode = CameraViewMode.FreeView;
                vp.cameraZOffset = 30f;
                vp.cameraXOffset = 0f;
                vp.initialPosition = null;
                vp.initialRotation = null;
                return vp;
            }
        }
    }

    public class PushPopViewCommand
    {
        public ViewParameters? viewParameters;
        public bool isPushCommand;
        public float commandTime;

        public PushPopViewCommand(ViewParameters? par, bool isPushCommand, float commandTime)
        {
            viewParameters = par;
            this.isPushCommand = isPushCommand;
            this.commandTime = commandTime;
        }
    }

    public Stack<ViewParameters> viewStack = new Stack<ViewParameters>();
    public Queue<PushPopViewCommand> viewCommandQueue = new Queue<PushPopViewCommand>();

    public ViewParameters currentViewParameter;
    Vector3? initialPosition;
    bool viewFreezed = false;

    public bool ViewFreezed
    {
        get
        {
            return viewFreezed;
        }
    }

    public bool InViewTransitioning
    {
        get
        {
            return inViewTransitioning;
        }
    }

    int maxCommandBuffer = 2;
    /// <summary>
    /// Push a new view structure to change current view angle and position
    /// </summary>
    /// <param name="vp"></param>
    public void PushView(ViewParameters vp, bool forcePush = false)
    {
        if (forcePush)
        {
            var sourceParameters = currentViewParameter;
            currentViewParameter = vp;
            initialPosition = vp.initialPosition;
            inViewTransitioning = true;
            if (currentViewParameter.forceFading || sourceParameters.forceFading)
            {
                cameraTranstioningType = CameraTransitioningType.FadeoutFadeinTranstioning;
            }
            else if (sourceParameters.viewContentType == ViewContentType.Model3DView
                && currentViewParameter.viewContentType == ViewContentType.Model3DView)
            {
                cameraTranstioningType = CameraTransitioningType.MovementTransition;
            }
            else if (sourceParameters.viewContentType == ViewContentType.Model3DView
            && currentViewParameter.viewContentType == ViewContentType.PanoramaView)
            {
                cameraTranstioningType = CameraTransitioningType.MoveInFadeOutTransition;
            }
            else if (sourceParameters.viewContentType == ViewContentType.PanoramaView
            && currentViewParameter.viewContentType == ViewContentType.Model3DView)
            {
                cameraTranstioningType = CameraTransitioningType.FadeInMoveOutTranstion;
            }
            else
            {
                cameraTranstioningType = CameraTransitioningType.FadeoutFadeinTranstioning;
            }
            viewStack.Push(vp);
        }
        else if (viewCommandQueue.Count < maxCommandBuffer)
            viewCommandQueue.Enqueue(new PushPopViewCommand(vp, true, Time.time));
    }

    /// <summary>
    /// Pop view to previous view angle and position
    /// </summary>
    public void PopView(bool forcePop = false)
    {
        if (forcePop)
        {
            ViewParameters sourceParameters;
            if (viewStack.Count > 2)
                sourceParameters = viewStack.Pop();
            else
                sourceParameters = viewStack.Peek();

            currentViewParameter = viewStack.Peek();

            if (!inViewTransitioning)
            {
                if (currentViewParameter.forceFading || sourceParameters.forceFading)
                {
                    cameraTranstioningType = CameraTransitioningType.FadeoutFadeinTranstioning;
                }
                else if (sourceParameters.viewContentType == ViewContentType.Model3DView
                && currentViewParameter.viewContentType == ViewContentType.Model3DView)
                {
                    cameraTranstioningType = CameraTransitioningType.MovementTransition;
                }
                else if (sourceParameters.viewContentType == ViewContentType.Model3DView
                && currentViewParameter.viewContentType == ViewContentType.PanoramaView)
                {
                    cameraTranstioningType = CameraTransitioningType.MoveInFadeOutTransition;
                }
                else if (sourceParameters.viewContentType == ViewContentType.PanoramaView
                && currentViewParameter.viewContentType == ViewContentType.Model3DView)
                {
                    cameraTranstioningType = CameraTransitioningType.FadeInMoveOutTranstion;
                }
                else
                {
                    cameraTranstioningType = CameraTransitioningType.FadeoutFadeinTranstioning;
                }
            }
            inViewTransitioning = true;
        }
        else if (viewCommandQueue.Count < maxCommandBuffer)
            viewCommandQueue.Enqueue(new PushPopViewCommand(null, false, Time.time));
    }

    void ProcessPushPopCommand()
    {
        if (viewCommandQueue.Count == 0 || inViewTransitioning)
            return;

        var command = viewCommandQueue.Dequeue();
        if (command.isPushCommand)
        {
            var sourceParameters = currentViewParameter;
            var vp = command.viewParameters.Value;
            currentViewParameter = vp;
            initialPosition = vp.initialPosition;
            inViewTransitioning = true;
            if (currentViewParameter.forceFading || sourceParameters.forceFading)
            {
                cameraTranstioningType = CameraTransitioningType.FadeoutFadeinTranstioning;
            }
            else if (sourceParameters.viewContentType == ViewContentType.Model3DView
                && currentViewParameter.viewContentType == ViewContentType.Model3DView)
            {
                cameraTranstioningType = CameraTransitioningType.MovementTransition;
            }
            else if (sourceParameters.viewContentType == ViewContentType.Model3DView
            && currentViewParameter.viewContentType == ViewContentType.PanoramaView)
            {
                cameraTranstioningType = CameraTransitioningType.MoveInFadeOutTransition;
            }
            else if (sourceParameters.viewContentType == ViewContentType.PanoramaView
            && currentViewParameter.viewContentType == ViewContentType.Model3DView)
            {
                cameraTranstioningType = CameraTransitioningType.FadeInMoveOutTranstion;
            }
            else
            {
                cameraTranstioningType = CameraTransitioningType.FadeoutFadeinTranstioning;
            }
            viewStack.Push(vp);
        }
        else
        {
            ViewParameters sourceParameters;
            if (viewStack.Count > 2)
                sourceParameters = viewStack.Pop();
            else
                sourceParameters = viewStack.Peek();

            currentViewParameter = viewStack.Peek();

            if (!inViewTransitioning)
            {
                if (currentViewParameter.forceFading || sourceParameters.forceFading)
                {
                    cameraTranstioningType = CameraTransitioningType.FadeoutFadeinTranstioning;
                }
                else if (sourceParameters.viewContentType == ViewContentType.Model3DView
                && currentViewParameter.viewContentType == ViewContentType.Model3DView)
                {
                    cameraTranstioningType = CameraTransitioningType.MovementTransition;
                }
                else if (sourceParameters.viewContentType == ViewContentType.Model3DView
                && currentViewParameter.viewContentType == ViewContentType.PanoramaView)
                {
                    cameraTranstioningType = CameraTransitioningType.MoveInFadeOutTransition;
                }
                else if (sourceParameters.viewContentType == ViewContentType.PanoramaView
                && currentViewParameter.viewContentType == ViewContentType.Model3DView)
                {
                    cameraTranstioningType = CameraTransitioningType.FadeInMoveOutTranstion;
                }
                else
                {
                    cameraTranstioningType = CameraTransitioningType.FadeoutFadeinTranstioning;
                }
            }
            inViewTransitioning = true;
        }
    }

    public void FreezeView()
    {
        viewFreezed = true;
    }

    public void UnfreezeView()
    {
        viewFreezed = false;
    }


    public MaskFadingStatus FadingStatus
    {
        get
        {
            return maskFadingStatus;
        }
    }

    //View transitioning parameters
    bool inViewTransitioning = false;
    CameraTransitioningType cameraTranstioningType = CameraTransitioningType.MovementTransition;
    MaskFadingStatus maskFadingStatus = MaskFadingStatus.Normal;
    float fadeTimer = 0f;
    public enum MaskFadingStatus
    {
        Normal = 0,
        FadingOut,
        Dark,
        FadingIn
    }

    public enum CameraTransitioningType
    {
        MovementTransition = 0,
        MoveInFadeOutTransition,
        FadeInMoveOutTranstion,
        FadeoutFadeinTranstioning
    }

    public delegate void CameraMaskEvent();
    public CameraMaskEvent OnCameraDarkend = null;

    bool LerpCameraOneFrame(float lerpSpeed)
    {
        float posDiff = 0f;
        float rotDiff = 0f;
        float zDiff = 0f;
        float xDiff = 0f;
        float fovDiff = 0f;
        //camera lerp
        ownedCamera.transform.localPosition = Vector3.Lerp(ownedCamera.transform.localPosition, new Vector3(-currentViewParameter.cameraXOffset, 0f, -currentViewParameter.cameraZOffset), Time.deltaTime * lerpSpeed);
        zDiff = Mathf.Abs(ownedCamera.transform.localPosition.z + currentViewParameter.cameraZOffset);
        xDiff = Mathf.Abs(ownedCamera.transform.localPosition.x + currentViewParameter.cameraXOffset);
        //fov lerp
        ownedCamera.fieldOfView = Mathf.Lerp(ownedCamera.fieldOfView, 60f, Time.deltaTime * lerpSpeed);
        fovDiff = Mathf.Abs(ownedCamera.fieldOfView - 60f);

        //position lerp
        if (currentViewParameter.initialPosition != null)
        {
            transform.position = Vector3.Lerp(transform.position, currentViewParameter.initialPosition.Value, Time.deltaTime * lerpSpeed);
            posDiff = Vector3.Distance(transform.position, currentViewParameter.initialPosition.Value);
        }

        //rotation lerp
        if (currentViewParameter.initialRotation != null)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, currentViewParameter.initialRotation.Value, Time.deltaTime * lerpSpeed);
            rotDiff = Vector3.Distance(transform.rotation.eulerAngles, currentViewParameter.initialRotation.Value.eulerAngles);
        }
        if (posDiff + rotDiff + zDiff + fovDiff < 0.01f)
            return true;
        else
            return false;
    }

    float lnLamda = -0.69897f;//value of ln0.2
    float lastScaleInput = 0f;
    float currentScaleInput = 0f;
    void UpdateView()
    {
        if (inViewTransitioning)//The camera is sitll in transitioning
        {
            switch (cameraTranstioningType)
            {
                case CameraTransitioningType.MovementTransition:
                    if (LerpCameraOneFrame(10f))
                        inViewTransitioning = false;
                    else
                        return;
                    break;
                case CameraTransitioningType.MoveInFadeOutTransition:
                    switch (maskFadingStatus)
                    {
                        case MaskFadingStatus.Normal:
                            maskFadingStatus = MaskFadingStatus.FadingOut;
                            fadeTimer = 0f;
                            return;
                        case MaskFadingStatus.FadingOut:
                            fadeTimer += Time.deltaTime;
                            if (fadeTimer < 0.5f)
                            {
                                cameraMask.color = Color.Lerp(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), fadeTimer / 0.5f);
                                LerpCameraOneFrame(4f);
                            }
                            else
                            {
                                cameraMask.color = new Color(0, 0, 0, 1);
                                maskFadingStatus = MaskFadingStatus.Dark;
                                if (OnCameraDarkend != null)
                                    OnCameraDarkend();
                                if (currentViewParameter.initialPosition != null)
                                    transform.position = currentViewParameter.initialPosition.Value;
                                if (currentViewParameter.initialRotation != null)
                                    transform.rotation = currentViewParameter.initialRotation.Value;
                                ownedCamera.transform.localPosition = new Vector3(-currentViewParameter.cameraXOffset, 0f, -currentViewParameter.cameraZOffset);
                                if (currentViewParameter.viewContentType == ViewContentType.Model3DView)
                                {
                                    ownedCamera.cullingMask = modelViewMask;
                                }
                                else
                                {
                                    ownedCamera.cullingMask = panoramaMask;
                                }
                                ownedCamera.fieldOfView = 60f;
                            }
                            return;
                        case MaskFadingStatus.Dark:
                            if (currentViewParameter.FadeInFlag == null || currentViewParameter.FadeInFlag())
                            {
                                maskFadingStatus = MaskFadingStatus.FadingIn;
                                fadeTimer = 0f;
                            }
                            return;
                        case MaskFadingStatus.FadingIn:
                            fadeTimer += Time.deltaTime;
                            if (fadeTimer < 0.5f)
                                cameraMask.color = Color.Lerp(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0), fadeTimer / 0.5f);
                            else
                            {
                                cameraMask.color = new Color(0, 0, 0, 0);
                                maskFadingStatus = MaskFadingStatus.Normal;
                                inViewTransitioning = false;
                                break;
                            }
                            return;
                        default: break;
                    }
                    break;
                case CameraTransitioningType.FadeInMoveOutTranstion:
                    switch (maskFadingStatus)
                    {
                        case MaskFadingStatus.Normal:
                            maskFadingStatus = MaskFadingStatus.FadingOut;
                            fadeTimer = 0f;
                            return;
                        case MaskFadingStatus.FadingOut:
                            fadeTimer += Time.deltaTime;
                            if (fadeTimer < 0.5f)
                                cameraMask.color = Color.Lerp(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), fadeTimer / 0.5f);
                            else
                            {
                                cameraMask.color = new Color(0, 0, 0, 1);
                                maskFadingStatus = MaskFadingStatus.Dark;
                                if (OnCameraDarkend != null)
                                    OnCameraDarkend();
                                if (currentViewParameter.viewContentType == ViewContentType.Model3DView)
                                {
                                    ownedCamera.cullingMask = modelViewMask;
                                }
                                else
                                {
                                    ownedCamera.cullingMask = panoramaMask;
                                }

                            }
                            return;
                        case MaskFadingStatus.Dark:
                            if (currentViewParameter.FadeInFlag == null || currentViewParameter.FadeInFlag())
                            {
                                maskFadingStatus = MaskFadingStatus.FadingIn;
                                fadeTimer = 0f;
                            }
                            return;
                        case MaskFadingStatus.FadingIn:
                            fadeTimer += Time.deltaTime;
                            var lerpStable = LerpCameraOneFrame(8f);
                            if (fadeTimer < 0.5f)
                            {
                                cameraMask.color = Color.Lerp(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0), fadeTimer / 0.5f);
                            }
                            else if (lerpStable)
                            {
                                cameraMask.color = new Color(0, 0, 0, 0);
                                maskFadingStatus = MaskFadingStatus.Normal;
                                inViewTransitioning = false;
                                break;
                            }
                            return;
                        default: break;
                    }
                    break;
                case CameraTransitioningType.FadeoutFadeinTranstioning:
                    switch (maskFadingStatus)
                    {
                        case MaskFadingStatus.Normal:
                            maskFadingStatus = MaskFadingStatus.FadingOut;
                            fadeTimer = 0f;
                            return;
                        case MaskFadingStatus.FadingOut:
                            fadeTimer += Time.deltaTime;
                            if (fadeTimer < 0.5f)
                                cameraMask.color = Color.Lerp(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), fadeTimer / 0.5f);
                            else
                            {
                                cameraMask.color = new Color(0, 0, 0, 1);
                                maskFadingStatus = MaskFadingStatus.Dark;
                                if (OnCameraDarkend != null)
                                    OnCameraDarkend();
                                if (currentViewParameter.initialPosition != null)
                                    transform.position = currentViewParameter.initialPosition.Value;
                                if (currentViewParameter.initialRotation != null)
                                    transform.rotation = currentViewParameter.initialRotation.Value;

                                //Reset all camera status
                                ownedCamera.transform.localPosition = new Vector3(-currentViewParameter.cameraXOffset, 0f, -currentViewParameter.cameraZOffset);
                                if (currentViewParameter.viewContentType == ViewContentType.Model3DView)
                                {
                                    ownedCamera.cullingMask = modelViewMask;
                                }
                                else
                                {
                                    ownedCamera.cullingMask = panoramaMask;
                                }
                                ownedCamera.fieldOfView = 60f;
                            }
                            return;
                        case MaskFadingStatus.Dark:
                            if (currentViewParameter.FadeInFlag == null || currentViewParameter.FadeInFlag())
                            {
                                maskFadingStatus = MaskFadingStatus.FadingIn;

                                fadeTimer = 0f;
                            }
                            return;
                        case MaskFadingStatus.FadingIn:
                            fadeTimer += Time.deltaTime;
                            if (fadeTimer < 0.5f)
                                cameraMask.color = Color.Lerp(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0), fadeTimer / 0.5f);
                            else
                            {
                                cameraMask.color = new Color(0, 0, 0, 0);
                                maskFadingStatus = MaskFadingStatus.Normal;
                                inViewTransitioning = false;
                                break;
                            }
                            return;
                        default: break;
                    }
                    break;
                default: break;
            }
        }

        //is a scale operation
        if (currentViewParameter.cameraScaleMode != CameraScaleMode.None && !viewFreezed)
        {
            if (lastScaleInput > 1f || currentScaleInput > 1f)
            {
                moveSpeedX = 0f;
                moveSpeedY = 0f;
            }

            if (lastScaleInput > 1f && currentScaleInput > 1f)
            {
                float deltaScale = currentScaleInput - lastScaleInput;
                switch (currentViewParameter.cameraScaleMode)
                {
                    case CameraScaleMode.ScaleFov:
                        ownedCamera.fieldOfView -= 0.04f * deltaScale;
                        ownedCamera.fieldOfView = Mathf.Clamp(ownedCamera.fieldOfView, 30f, 60f);
                        break;
                    case CameraScaleMode.MoveCamera:
                        Vector3 t = ownedCamera.transform.localPosition;
                        t.z += 0.02f * deltaScale;
                        t.z = Mathf.Clamp(t.z, -currentViewParameter.cameraZOffset, -currentViewParameter.cameraZOffset * 0.65f);
                        ownedCamera.transform.localPosition = t;
                        break;
                    default: break;
                }
                return;
            }
            else if (lastScaleInput > 1f || currentScaleInput > 1f)
            {
                return;
            }

        }

        if (currentViewParameter.viewContentType == ViewContentType.PanoramaView)//Panorama view
        {
            if (touched)
            {
                float boost = 1.5f;
                moveX = -deltaXThisFrame * (ownedCamera.fieldOfView / 60f) * boost / 4;
                moveY = -deltaYThisFrame * (ownedCamera.fieldOfView / 60f) * boost / 4;
            }
            else
            {
                float delta = 1 + lnLamda * Time.deltaTime + (lnLamda * Time.deltaTime) * (lnLamda * Time.deltaTime) / 2f;//Taylor equation for f(x)=c^x
                moveSpeedX = delta * moveSpeedX;
                moveSpeedY = delta * moveSpeedY;
                if (Mathf.Abs(moveSpeedX) < 0.01f)
                {
                    moveSpeedX = 0;
                }

                if (Mathf.Abs(moveSpeedY) < 0.01f)
                {
                    moveSpeedY = 0f;
                }

                moveX = -moveSpeedX * Time.deltaTime / 4;
                moveY = -moveSpeedY * Time.deltaTime / 4;
            }

        }
        else//model view
        {
            if (deltaXThisFrame == 0f && deltaYThisFrame == 0f)
                return;
            moveX = deltaXThisFrame;
            moveY = deltaYThisFrame;
        }

        //Camera movement control
        Vector3 eulerAngles = transform.rotation.eulerAngles;
        if (!viewFreezed)
        {
            if (currentViewParameter.viewMode == CameraViewMode.HorizontalFreeView)
            {
                eulerAngles += new Vector3(0, moveX, 0);
            }
            if (currentViewParameter.viewMode == CameraViewMode.FreeView)
            {
                eulerAngles += new Vector3(-moveY, moveX, 0);
                float x = eulerAngles.x;
                if (x > 270)
                    x -= 360f;
                x = Mathf.Clamp(x, -60f, 60f);
                eulerAngles.x = x;
            }
            if (currentViewParameter.viewMode == CameraViewMode.HorizontalHalfFreeView)
            {
                eulerAngles += new Vector3(-moveY, moveX, 0);
                float t = eulerAngles.x;
                if (t > 270)
                    t -= 360f;
                t = Mathf.Clamp(t, 5f, 60f);
                eulerAngles.x = t;
            }
            if (currentViewParameter.viewMode == CameraViewMode.HorizontalSlideView)
            {
                Vector3 dif = transform.position - initialPosition.Value;
                dif += new Vector3(-moveX * slideViewSensitivity / 9f, 0, 0);
                dif.x = Mathf.Clamp(dif.x, -slideViewRange, slideViewRange);
                transform.position = initialPosition.Value + dif;
            }
            transform.rotation = Quaternion.Euler(eulerAngles);
        }
    }

    float deltaXThisFrame = 0f;
    float deltaYThisFrame = 0f;

    float moveX = 0f;
    float moveY = 0f;

    float moveSpeedX;
    float moveSpeedY;

    bool touched = false;
    void PrepareInputData()
    {
        //Make sure the rotation operation is screensize-independent
        Vector3 vec = InputPlatform.Singleton.GetMoveVector() * InputPlatform.Singleton.ScreenSizeRatio;
        currentScaleInput = InputPlatform.Singleton.GetScaleLength() * InputPlatform.Singleton.ScreenSizeRatio;
        touched = InputPlatform.Singleton.GetTouching();
        deltaXThisFrame = vec.x * sensitivityHor;
        deltaYThisFrame = vec.y * sensitivityVert;

        if (touched && currentScaleInput < 1f)
        {
            moveSpeedX = deltaXThisFrame / Time.deltaTime;
            moveSpeedY = deltaYThisFrame / Time.deltaTime;
        }

        //deltaXThisFrame = vec.x * sensitivityHor;
        //deltaYThisFrame = vec.y * sensitivityVert;
    }

    void LateUpdateView()
    {
        lastScaleInput = currentScaleInput;
        if (focusOnCenter)
        {
            var rot = Quaternion.LookRotation(transform.position - ownedCamera.transform.position);
            ownedCamera.transform.rotation = Quaternion.Lerp(ownedCamera.transform.rotation, rot, Time.deltaTime * 8f);
        }
        else
        {
            ownedCamera.transform.localRotation = Quaternion.Lerp(ownedCamera.transform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
        }
    }

    float updateInterval = 0.5f;
    float fps = 0f;
    float timeLeft = 0.5f;
    int frameCount = 0;
    void UpdateFPS()
    {
        if (timeLeft <= 0f)
        {
            fps = frameCount / updateInterval;
            timeLeft = updateInterval;
            frameCount = 0;
        }
        else
        {
            timeLeft -= Time.deltaTime / Time.timeScale;
            frameCount++;
        }
    }

#if GUI_DEBUG
    private void OnGUI()
    {
        GUI.color = Color.blue;
        GUILayout.Label("(DeltaX,DeltaY)=" + "(" + deltaXThisFrame + "," + deltaYThisFrame + ")");
        GUILayout.Label("DeltaX=" + deltaXThisFrame);
        GUILayout.Label("DeltaY=" + deltaYThisFrame);
        GUILayout.Label("FPS=" + fps);
    }
#endif
    #endregion

    #region Auto rotating control
    void StartAutoRotating(GlobalEvent evt)
    {
        autoRotating = true;
    }

    void StopAutoRotating(GlobalEvent evt)
    {
        autoRotating = false;
    }

    void UpdateAutoRotating()
    {
        if (inViewTransitioning)
            return;

        if (autoRotating && viewFreezed)
        {
            Quaternion deltaRot = Quaternion.Euler(0, Time.deltaTime * 10f, 0);
            transform.rotation = deltaRot * transform.rotation;
            positionDirty = true;
        }
        else if (positionDirty)
        {
            inViewTransitioning = true;
            positionDirty = false;
        }
    }
    #endregion

    #region Focus Point Control
    void FocusOnCenter(GlobalEvent evt)
    {
        focusOnCenter = true;
    }

    void CancelFocusOnCenter(GlobalEvent evt)
    {
        focusOnCenter = false;
    }
    #endregion

    #region
    public void RotateTo(Transform target)
    {
        if (target == null || isRotatingToTarget)
            return;

        isRotatingToTarget = true;
        StartCoroutine(RotateCoroutine(target));
    }

    IEnumerator RotateCoroutine(Transform target)
    {
        Quaternion lookRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);

        while (true)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * 8f);
            Vector3 diff = (Quaternion.Inverse(transform.rotation) * lookRotation).eulerAngles;
            if (Vector3.Distance(diff, Vector3.zero) < 0.05f)
            {
                break;
            }
            yield return null;
        }

        isRotatingToTarget = false;
        yield return null;
    }

    public void SetGuideMode()
    {
        gameMode = GameMode.GuideMode;
    }

    public void SetFreeMode()
    {
        gameMode = GameMode.FreeMode;
    }
    #endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(CameraCharacter))]
public class CameraCharacterEditor : Editor
{
    public CameraCharacter Target
    {
        get
        {
            return target as CameraCharacter;
        }
    }
    bool allowDataBuilding = false;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();

        if (GUILayout.Button("To Fixed View Angle"))
        {
            Target.transform.position = Target.fixedViewPosition;
            Target.transform.rotation = Target.fixedViewRotation;
        }

        if (GUILayout.Button("To Look Down View Angle"))
        {
            Target.transform.position = Target.lookDownViewPosition;
            Target.transform.rotation = Target.lookDownViewRotation;
        }

        allowDataBuilding = EditorGUILayout.Toggle("Allow Data Building", allowDataBuilding);

        if (allowDataBuilding)
        {
            if (GUILayout.Button("Build As Fixed View"))
            {
                Target.fixedViewPosition = Target.transform.position;
                Target.fixedViewRotation = Target.transform.rotation;
                allowDataBuilding = false;
            }

            if (GUILayout.Button("Build As Look Down View"))
            {
                Target.lookDownViewPosition = Target.transform.position;
                Target.lookDownViewRotation = Target.transform.rotation;
                allowDataBuilding = false;
            }
        }
    }
}
#endif


