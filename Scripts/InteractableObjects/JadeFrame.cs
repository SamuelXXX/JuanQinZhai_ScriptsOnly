using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class JadeFrame : MonoBehaviour
{
    public enum InlayFrameStatus
    {
        Hide = 0,
        Display,
        Focused,
        Inlayed,
        HideAfterInlay
    }

    public InlayFrameStatus jadeFrameStatus = InlayFrameStatus.Hide;
    public Transform inlayCore;
    public Transform hidePoint;
    public Transform displayPoint;
    public Transform inlayPoint;
    public Jade targetJade;
    public string inlayEvent;
    public MeshRenderer coreHighlightingRenderer;

    protected float lerpSpeed = 7f;

    #region Mono Events
    private void Awake()
    {
        if (targetJade != null)
        {
            targetJade.OnInlayHandler = OnJadeReachCore;
            targetJade.targetPoint = inlayCore;
        }

        if (coreHighlightingRenderer != null)
            coreHighlightingRenderer.enabled = false;
    }

    private void Start()
    {
        GlobalEventManager.RegisterHandler("Hide-" + name, Hide);
        GlobalEventManager.RegisterHandler("Display-" + name, Display);
        GlobalEventManager.RegisterHandler("Highlight-" + name, HighlightInlayCore);
        GlobalEventManager.RegisterHandler("Dehighlight-" + name, DeHighlightInlayCore);
        GlobalEventManager.RegisterHandler("Focus-" + name, Focus);
    }

    private void OnDestroy()
    {
        GlobalEventManager.UnregisterHandler("Hide-" + name, Hide);
        GlobalEventManager.UnregisterHandler("Display-" + name, Display);
        GlobalEventManager.UnregisterHandler("Highlight-" + name, HighlightInlayCore);
        GlobalEventManager.UnregisterHandler("Dehighlight-" + name, DeHighlightInlayCore);
        GlobalEventManager.UnregisterHandler("Focus-" + name, Focus);
    }

    private void OnValidate()
    {
        inlayEvent = "JadeInlayed-" + name;
    }

    private void LateUpdate()
    {
        UpdateState();
    }


    void UpdateState()
    {
        switch (jadeFrameStatus)
        {
            case InlayFrameStatus.Hide:
                if (Vector3.Distance(transform.position, hidePoint.position) < 0.001f)
                {
                    transform.position = hidePoint.position;
                    transform.rotation = hidePoint.rotation;
                    if (targetJade != null && targetJade.gameObject.activeInHierarchy)
                        targetJade.gameObject.SetActive(false);
                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, hidePoint.position, Time.deltaTime * lerpSpeed);
                    transform.rotation = Quaternion.Lerp(transform.rotation, hidePoint.rotation, Time.deltaTime * lerpSpeed);
                }
                break;
            case InlayFrameStatus.Display:
                if (Vector3.Distance(transform.position, displayPoint.position) < 0.001f)
                {
                    transform.position = displayPoint.position;
                    transform.rotation = displayPoint.rotation;
                    if (targetJade != null && !targetJade.gameObject.activeInHierarchy)
                    {
                        targetJade.SetMoveCenter(targetJade.transform.position);
                        targetJade.transform.position += Vector3.up * 10f;
                        targetJade.gameObject.SetActive(true);
                    }
                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, displayPoint.position, Time.deltaTime * lerpSpeed);
                    transform.rotation = Quaternion.Lerp(transform.rotation, displayPoint.rotation, Time.deltaTime * lerpSpeed);
                }

                break;
            case InlayFrameStatus.Focused:
                //if (Vector3.Distance(transform.position, inlayPoint.position) < 0.001f)
                //{
                //    transform.position = inlayPoint.position;
                //    transform.rotation = inlayPoint.rotation;
                //}
                //else
                //{
                //    transform.position = Vector3.Lerp(transform.position, inlayPoint.position, Time.deltaTime * lerpSpeed);
                //    transform.rotation = Quaternion.Lerp(transform.rotation, inlayPoint.rotation, Time.deltaTime * lerpSpeed);
                //}

                break;
            case InlayFrameStatus.Inlayed:
                targetJade.transform.parent = gameObject.transform;
                DeHighlightInlayCore(new GlobalEvent(""));
                break;
            case InlayFrameStatus.HideAfterInlay:
                if (Vector3.Distance(transform.position, hidePoint.position) < 0.001f)
                {
                    Destroy(targetJade.gameObject);
                    Destroy(gameObject);
                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, hidePoint.position, Time.deltaTime * lerpSpeed);
                    transform.rotation = Quaternion.Lerp(transform.rotation, hidePoint.rotation, Time.deltaTime * lerpSpeed);
                }
                break;
            default: break;
        }
    }
    #endregion

    bool OnJadeReachCore()
    {
        //if (jadeFrameStatus != InlayFrameStatus.Focused)
        //    return false;
        GlobalEventManager.SendEvent(inlayEvent);
        jadeFrameStatus = InlayFrameStatus.Inlayed;
        return true;
    }

    void Display(GlobalEvent evt)
    {
        jadeFrameStatus = InlayFrameStatus.Display;
    }

    void Hide(GlobalEvent evt)
    {
        if (jadeFrameStatus != InlayFrameStatus.Inlayed)
            jadeFrameStatus = InlayFrameStatus.Hide;
        else
            jadeFrameStatus = InlayFrameStatus.HideAfterInlay;
    }

    void Focus(GlobalEvent evt)
    {
        jadeFrameStatus = InlayFrameStatus.Focused;
        HighlightInlayCore(new GlobalEvent(""));
    }

    void HighlightInlayCore(GlobalEvent evt)
    {
        if (coreHighlightingRenderer != null)
            coreHighlightingRenderer.enabled = true;
    }

    void DeHighlightInlayCore(GlobalEvent evt)
    {
        if (coreHighlightingRenderer != null)
            coreHighlightingRenderer.enabled = false;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(JadeFrame))]
public class JadeFrameEditor : Editor
{
    JadeFrame Target
    {
        get
        {
            return target as JadeFrame;
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("To Hiding Position"))
        {
            if (Target.hidePoint != null)
            {
                Target.transform.position = Target.hidePoint.position;
                Target.transform.rotation = Target.hidePoint.rotation;
                Target.transform.parent = Target.hidePoint.parent;
                if (Target.targetJade != null)
                    Target.targetJade.gameObject.SetActive(false);
            }
        }

        if (GUILayout.Button("To Display Position"))
        {
            if (Target.displayPoint != null)
            {
                Target.transform.position = Target.displayPoint.position;
                Target.transform.rotation = Target.displayPoint.rotation;
                Target.transform.parent = Target.displayPoint.parent;
                if (Target.targetJade != null)
                    Target.targetJade.gameObject.SetActive(true);
            }
        }
    }

}
#endif
