using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LandscapePainting : MonoBehaviour
{
    public enum LandscapePaintingStatus
    {
        Hide = 0,
        CombineDisplay,
        SplitDisplay,
        HideAfterDisplay
    }

    public LandscapePaintingStatus landscapePaintingStatus = LandscapePaintingStatus.Hide;
    public Transform hidePoint;
    public Transform displayPoint;

    public List<SplitingBlock> splitingBlocks = new List<SplitingBlock>();



    public float scaleFactor = 0.1f;


    public string finishEvent;
    [HideInInspector]
    public bool isDisplay = false;

    protected float lerpSpeed = 7f;
    public Vector3 originalScale;
    protected Vector3 displayScale = new Vector3(1, 1, 1);

    [System.Serializable]
    public class SplitingBlock
    {
        public Transform targetBlock;
        public Transform splitLocalPoint;
        public Vector3 combineLocalPosition;
        public Quaternion combineLocalRotation;

        LandscapePaintingStatus lastStatus = LandscapePaintingStatus.Hide;
        bool positionStable = false;
        bool rotationStable = false;
        public void UpdatePosition(LandscapePaintingStatus status, float lerpSpeed, bool shouldHide)
        {
            if (lastStatus != status)
            {
                lastStatus = status;
                positionStable = false;
                rotationStable = false;
            }
            switch (status)
            {
                case LandscapePaintingStatus.Hide:
                    if (!positionStable)
                    {
                        targetBlock.localPosition = Vector3.Lerp(targetBlock.localPosition, combineLocalPosition, Time.deltaTime * lerpSpeed);
                        if (Vector3.Distance(targetBlock.localPosition, combineLocalPosition) < 0.001f)
                        {
                            positionStable = true;
                        }
                    }
                    if (!rotationStable)
                    {
                        targetBlock.localRotation = Quaternion.Lerp(targetBlock.localRotation, combineLocalRotation, Time.deltaTime * lerpSpeed);
                        Quaternion v = targetBlock.localRotation * Quaternion.Inverse(combineLocalRotation);
                        Vector3 e = v.eulerAngles;
                        if (e.magnitude < 0.001f)
                        {
                            rotationStable = true;
                        }
                    }
                    if (shouldHide)
                        Hide();
                    else
                        Show();
                    break;
                case LandscapePaintingStatus.CombineDisplay:
                    if (!positionStable)
                    {
                        targetBlock.localPosition = Vector3.Lerp(targetBlock.localPosition, combineLocalPosition, Time.deltaTime * lerpSpeed);
                        if (Vector3.Distance(targetBlock.localPosition, combineLocalPosition) < 0.001f)
                        {
                            positionStable = true;
                        }
                    }
                    if (!rotationStable)
                    {
                        targetBlock.localRotation = Quaternion.Lerp(targetBlock.localRotation, combineLocalRotation, Time.deltaTime * lerpSpeed);
                        Quaternion v = targetBlock.localRotation * Quaternion.Inverse(combineLocalRotation);
                        Vector3 e = v.eulerAngles;
                        if (e.magnitude < 0.001f)
                        {
                            rotationStable = true;
                        }
                    }
                    Show();
                    break;
                case LandscapePaintingStatus.SplitDisplay:
                    if (!positionStable)
                    {
                        targetBlock.localPosition = Vector3.Lerp(targetBlock.localPosition, splitLocalPoint.localPosition, Time.deltaTime * lerpSpeed);
                        if (Vector3.Distance(targetBlock.localPosition, splitLocalPoint.localPosition) < 0.001f)
                        {
                            positionStable = true;
                        }
                    }
                    if (!rotationStable)
                    {
                        targetBlock.localRotation = Quaternion.Lerp(targetBlock.localRotation, splitLocalPoint.localRotation, Time.deltaTime * lerpSpeed);
                        Quaternion v = targetBlock.localRotation * Quaternion.Inverse(splitLocalPoint.localRotation);
                        Vector3 e = v.eulerAngles;
                        if (e.magnitude < 0.001f)
                        {
                            rotationStable = true;
                        }
                    }
                    Show();
                    break;
                case LandscapePaintingStatus.HideAfterDisplay:
                    if (!positionStable)
                    {
                        targetBlock.localPosition = Vector3.Lerp(targetBlock.localPosition, combineLocalPosition, Time.deltaTime * lerpSpeed);
                        if (Vector3.Distance(targetBlock.localPosition, combineLocalPosition) < 0.001f)
                        {
                            positionStable = true;
                        }
                    }
                    if (!rotationStable)
                    {
                        targetBlock.localRotation = Quaternion.Lerp(targetBlock.localRotation, combineLocalRotation, Time.deltaTime * lerpSpeed);
                        Quaternion v = targetBlock.localRotation * Quaternion.Inverse(combineLocalRotation);
                        Vector3 e = v.eulerAngles;
                        if (e.magnitude < 0.001f)
                        {
                            rotationStable = true;
                        }
                    }
                    if (shouldHide)
                    {
                        Hide();
                    }
                    else
                        Show();
                    break;
                default: break;
            }
        }

        public SplitingBlock()
        {
            if (targetBlock == null)
                return;

            combineLocalPosition = targetBlock.localPosition;
            combineLocalRotation = targetBlock.localRotation;
        }

        public void SaveCombineLocalTransform()
        {
            if (targetBlock == null)
                return;

            combineLocalPosition = targetBlock.localPosition;
            combineLocalRotation = targetBlock.localRotation;
        }

        public void LoadCombineLocalTransform()
        {
            if (targetBlock == null)
                return;

            targetBlock.localPosition = combineLocalPosition;
            targetBlock.localRotation = combineLocalRotation;
        }

        public void SaveSplitLocalTransform()
        {
            if (targetBlock == null)
                return;

            splitLocalPoint.localPosition = targetBlock.localPosition;
            splitLocalPoint.localRotation = targetBlock.localRotation;
        }


        public void LoadSplitLocalTransform()
        {
            if (targetBlock == null)
                return;

            targetBlock.localPosition = splitLocalPoint.localPosition;
            targetBlock.localRotation = splitLocalPoint.localRotation;
        }

        MeshRenderer renderer = null;
        void Hide()
        {
            if (renderer == null)
                renderer = targetBlock.GetComponent<MeshRenderer>();

            renderer.enabled = false;
        }

        void Show()
        {
            if (renderer == null)
                renderer = targetBlock.GetComponent<MeshRenderer>();

            renderer.enabled = true;
        }
    }

    #region Mono Events
    private void Awake()
    {
        originalScale = transform.localScale;
        displayScale = originalScale * scaleFactor;
    }

    private void Start()
    {
        GlobalEventManager.RegisterHandler("HideBack-" + name, Hide);
        GlobalEventManager.RegisterHandler("ScaleView-" + name, Display);
        GlobalEventManager.RegisterHandler("Split-" + name, Split);
        GlobalEventManager.RegisterHandler("Combine-" + name, Combine);
    }

    private void OnDestroy()
    {
        GlobalEventManager.UnregisterHandler("HideBack-" + name, Hide);
        GlobalEventManager.UnregisterHandler("ScaleView-" + name, Display);
        GlobalEventManager.UnregisterHandler("Split-" + name, Split);
        GlobalEventManager.UnregisterHandler("Combine-" + name, Combine);
    }

    private void OnValidate()
    {
        finishEvent = "PaintingViewFinished";
    }

    private void LateUpdate()
    {
        UpdateState();
        //Debug.Log(Application.persistentDataPath);
    }

    LandscapePaintingStatus lastStatus;
    bool positionStable = false;
    bool rotationStable = false;
    bool scaleStable = false;
    bool childHide = false;

    void UpdateState()
    {
        if (lastStatus != landscapePaintingStatus)
        {
            lastStatus = landscapePaintingStatus;
            positionStable = false;
            rotationStable = false;
            scaleStable = false;
        }
        switch (landscapePaintingStatus)
        {
            case LandscapePaintingStatus.Hide:
                if (!positionStable)
                {
                    transform.position = Vector3.Lerp(transform.position, hidePoint.position, Time.deltaTime * lerpSpeed);
                    if (Vector3.Distance(transform.position, hidePoint.position) < 0.001f)
                    {
                        positionStable = true;
                        transform.position = hidePoint.position;
                    }
                }
                if (!rotationStable)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, hidePoint.rotation, Time.deltaTime * lerpSpeed);
                    Quaternion v = transform.rotation * Quaternion.Inverse(hidePoint.rotation);
                    Vector3 e = v.eulerAngles;
                    if (e.magnitude < 0.001f)
                    {
                        rotationStable = true;
                        transform.rotation = hidePoint.rotation;
                    }
                }
                if (!scaleStable)
                {
                    transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * lerpSpeed);
                    if (Vector3.Distance(transform.localScale, originalScale) < 0.001f)
                    {
                        scaleStable = true;
                        transform.localScale = originalScale;
                    }
                }
                if(positionStable&&rotationStable)
                {
                    childHide = true;
                }
                break;
            case LandscapePaintingStatus.CombineDisplay:
                if (!positionStable)
                {
                    transform.position = Vector3.Lerp(transform.position, displayPoint.position, Time.deltaTime * lerpSpeed);
                    if (Vector3.Distance(transform.position, displayPoint.position) < 0.001f)
                    {
                        positionStable = true;
                        transform.position = displayPoint.position;
                    }
                }
                if (!rotationStable)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, displayPoint.rotation, Time.deltaTime * lerpSpeed);
                    Quaternion v = transform.rotation * Quaternion.Inverse(displayPoint.rotation);
                    Vector3 e = v.eulerAngles;
                    if (e.magnitude < 0.001f)
                    {
                        rotationStable = true;
                        transform.rotation = displayPoint.rotation;
                    }

                }
                if (!scaleStable)
                {
                    transform.localScale = Vector3.Lerp(transform.localScale, displayScale, Time.deltaTime * lerpSpeed);
                    if (Vector3.Distance(transform.localScale, displayScale) < 0.001f)
                    {
                        scaleStable = true;
                        transform.localScale = displayScale;
                    }
                }

                if (positionStable && rotationStable && scaleStable)
                {
                    var p = InputPlatform.Singleton.GetMoveVector() * InputPlatform.Singleton.ScreenSizeRatio;
                    float deltaX = p.x * 0.2f;
                    //float deltaY = p.y * 0.2f;
                    Vector3 eulerAngles = transform.rotation.eulerAngles;
                    eulerAngles += new Vector3(0, -deltaX, 0);
                    transform.rotation = Quaternion.Euler(eulerAngles);
                }
                childHide = false;
                break;
            case LandscapePaintingStatus.SplitDisplay:
                if (!positionStable)
                {
                    transform.position = Vector3.Lerp(transform.position, displayPoint.position, Time.deltaTime * lerpSpeed);
                    if (Vector3.Distance(transform.position, displayPoint.position) < 0.001f)
                    {
                        positionStable = true;
                        transform.position = displayPoint.position;
                    }
                }
                if (!rotationStable)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, displayPoint.rotation, Time.deltaTime * lerpSpeed);
                    Quaternion v = transform.rotation * Quaternion.Inverse(displayPoint.rotation);
                    Vector3 e = v.eulerAngles;
                    if (e.magnitude < 0.001f)
                    {
                        rotationStable = true;
                        transform.rotation = displayPoint.rotation;
                    }

                }

                if (!scaleStable)
                {
                    transform.localScale = Vector3.Lerp(transform.localScale, displayScale, Time.deltaTime * lerpSpeed);
                    if (Vector3.Distance(transform.localScale, displayScale) < 0.001f)
                    {
                        scaleStable = true;
                        transform.localScale = displayScale;
                    }
                }
                childHide = false;
                break;
            case LandscapePaintingStatus.HideAfterDisplay:
                if (!positionStable)
                {
                    transform.position = Vector3.Lerp(transform.position, hidePoint.position, Time.deltaTime * lerpSpeed);
                    if (Vector3.Distance(transform.position, hidePoint.position) < 0.001f)
                    {
                        positionStable = true;
                        transform.position = hidePoint.position;
                    }
                }
                if (!rotationStable)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, hidePoint.rotation, Time.deltaTime * lerpSpeed);
                    Quaternion v = transform.rotation * Quaternion.Inverse(hidePoint.rotation);
                    Vector3 e = v.eulerAngles;
                    if (e.magnitude < 0.001f)
                    {
                        rotationStable = true;
                        transform.rotation = hidePoint.rotation;
                    }

                }
                if (!scaleStable)
                {
                    transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * lerpSpeed);
                    if (Vector3.Distance(transform.localScale, originalScale) < 0.001f)
                    {
                        scaleStable = true;
                        transform.localScale = originalScale;
                    }
                }

                if (positionStable && rotationStable)
                    childHide = true;
                else
                    childHide = false;
                break;
            default: break;
        }

        foreach (var sb in splitingBlocks)
        {
            sb.UpdatePosition(landscapePaintingStatus, lerpSpeed, childHide);
        }
        lastStatus = landscapePaintingStatus;
    }
    #endregion

    public void Display(GlobalEvent evt)
    {
        if (landscapePaintingStatus == LandscapePaintingStatus.Hide)
            landscapePaintingStatus = LandscapePaintingStatus.CombineDisplay;
    }

    public void Split(GlobalEvent evt)
    {
        if (landscapePaintingStatus == LandscapePaintingStatus.CombineDisplay)
            landscapePaintingStatus = LandscapePaintingStatus.SplitDisplay;
    }

    public void Combine(GlobalEvent evt)
    {
        if (landscapePaintingStatus == LandscapePaintingStatus.SplitDisplay)
            landscapePaintingStatus = LandscapePaintingStatus.CombineDisplay;
    }

    public void Hide(GlobalEvent evt)
    {
        if (landscapePaintingStatus == LandscapePaintingStatus.CombineDisplay)
        {
            landscapePaintingStatus = LandscapePaintingStatus.HideAfterDisplay;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LandscapePainting))]
public class LandscapePaintingEditor : Editor
{
    LandscapePainting Target
    {
        get
        {
            return target as LandscapePainting;
        }
    }

    bool IsDisplay
    {
        get
        {
            return Target.isDisplay;
        }
        set
        {
            Target.isDisplay = value;
        }
    }

    bool allowDataBuilding = false;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();


        if (GUILayout.Button("Display"))
        {
            Target.transform.position = Target.displayPoint.position;
            Target.transform.rotation = Target.displayPoint.rotation;
            Target.transform.localScale = Target.hidePoint.localScale * Target.scaleFactor;
            IsDisplay = true;
        }
        if (GUILayout.Button("Hide"))
        {
            Target.transform.position = Target.hidePoint.position;
            Target.transform.rotation = Target.hidePoint.rotation;
            Target.transform.localScale = Target.hidePoint.localScale;
            foreach (var sb in Target.splitingBlocks)
            {
                sb.LoadCombineLocalTransform();
            }
            IsDisplay = false;
        }

        EditorGUILayout.Space();

        if (IsDisplay)
        {
            if (GUILayout.Button("Split"))
            {
                foreach (var sb in Target.splitingBlocks)
                {
                    sb.LoadSplitLocalTransform();
                }
            }

            if (GUILayout.Button("Combine"))
            {
                foreach (var sb in Target.splitingBlocks)
                {
                    sb.LoadCombineLocalTransform();
                }
            }
        }


        EditorGUILayout.Space();
        allowDataBuilding = EditorGUILayout.Toggle("Allow Data Building", allowDataBuilding);
        if (allowDataBuilding)
        {
            //save current position status
            if (!IsDisplay)
            {
                if (GUILayout.Button("Build HidePoint"))
                {
                    Target.hidePoint.position = Target.transform.position;
                    Target.hidePoint.rotation = Target.transform.rotation;
                    Target.hidePoint.localScale = Target.transform.localScale;
                    allowDataBuilding = false;
                }
            }
            else
            {
                if (GUILayout.Button("Build DisplayPoint"))
                {
                    Target.displayPoint.position = Target.transform.position;
                    Target.displayPoint.rotation = Target.transform.rotation;
                    allowDataBuilding = false;
                }
            }


            EditorGUILayout.Space();

            if (IsDisplay)
            {
                if (GUILayout.Button("Build CombinePoints"))
                {
                    foreach (var sb in Target.splitingBlocks)
                    {
                        sb.SaveCombineLocalTransform();
                    }
                    allowDataBuilding = false;
                }

                if (GUILayout.Button("Build SplitPoints"))
                {
                    foreach (var sb in Target.splitingBlocks)
                    {
                        sb.SaveSplitLocalTransform();
                    }
                    allowDataBuilding = false;
                }
            }
        }
    }

}
#endif
