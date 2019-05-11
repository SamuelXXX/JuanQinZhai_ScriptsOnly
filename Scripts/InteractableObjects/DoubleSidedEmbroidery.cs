using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DoubleSidedEmbroidery : MonoBehaviour
{
    public enum EmbroideryStatus
    {
        Hide = 0,
        Display,
        Inlayed,
        Focus,
        HideAfterInlay
    }

    public enum StringColor
    {
        Red = 0,
        Blue,
        Gold
    }


    public EmbroideryStatus embroideryStatus = EmbroideryStatus.Hide;
    public Transform inlayCore;
    public Transform hidePoint;
    public Transform displayPoint;
    public List<EmbroideryColor> targetString = new List<EmbroideryColor>();
    public List<ColorLayer> colorLayers = new List<ColorLayer>();

    [System.Serializable]
    public class ColorLayer
    {
        public StringColor color;
        public MeshRenderer targetRenderer;
        public bool inlayFinished = false;
    }



    public string inlayEvent;
    public float focusDistance = 0.5f;

    protected float lerpSpeed = 7f;

    #region Mono Events
    private void Awake()
    {
        foreach (var s in targetString)
        {
            if (s != null)
            {
                s.OnInlayHandler = OnStringReachCore;
                s.targetPoint = inlayCore;
            }
        }
    }

    private void Start()
    {
        GlobalEventManager.RegisterHandler("Hide-" + name, Hide);
        GlobalEventManager.RegisterHandler("Display-" + name, Display);
        GlobalEventManager.RegisterHandler("Focus-" + name, Focus);
    }

    private void OnDestroy()
    {
        GlobalEventManager.UnregisterHandler("Hide-" + name, Hide);
        GlobalEventManager.UnregisterHandler("Display-" + name, Display);
        GlobalEventManager.UnregisterHandler("Focus-" + name, Focus);
    }

    private void OnValidate()
    {
        inlayEvent = "StringColorFilled";
    }

    private void LateUpdate()
    {
        UpdateState();
    }


    void UpdateState()
    {
        switch (embroideryStatus)
        {
            case EmbroideryStatus.Hide:
                if (Vector3.Distance(transform.position, hidePoint.position) < 0.01f)
                {
                    transform.position = hidePoint.position;
                    foreach (var s in targetString)
                    {
                        if (s != null && s.gameObject.activeInHierarchy)
                            s.gameObject.SetActive(false);
                    }

                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, hidePoint.position, Time.deltaTime * lerpSpeed);
                    transform.rotation = Quaternion.Lerp(transform.rotation, hidePoint.rotation, Time.deltaTime * lerpSpeed);
                }
                break;
            case EmbroideryStatus.Display:
                if (Vector3.Distance(transform.position, displayPoint.position) < 0.01f)
                {
                    transform.position = displayPoint.position;

                    foreach (var s in targetString)
                    {
                        if (s != null && !s.gameObject.activeInHierarchy)
                        {
                            s.SetMoveCenter(s.transform.position);
                            s.transform.position += Vector3.up * 10f;
                            s.gameObject.SetActive(true);
                        }
                    }

                }
                else
                    transform.position = Vector3.Lerp(transform.position, displayPoint.position, Time.deltaTime * lerpSpeed);

                var inlayCount = 0;
                foreach (var c in colorLayers)
                {
                    if (c.inlayFinished)
                        inlayCount++;
                }

                if (inlayCount >= colorLayers.Count)
                {
                    GlobalEventManager.SendEvent(inlayEvent);
                    embroideryStatus = EmbroideryStatus.Inlayed;
                }
                break;
            case EmbroideryStatus.Inlayed:
                break;
            case EmbroideryStatus.Focus:
                Vector3 viewPoint = CameraCharacter.Singleton.ownedCamera.transform.position + CameraCharacter.Singleton.transform.forward * focusDistance;
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
                    transform.position = Vector3.Lerp(transform.position, viewPoint, Time.deltaTime * lerpSpeed);
                }
                break;
            case EmbroideryStatus.HideAfterInlay:
                if (Vector3.Distance(transform.position, hidePoint.position) < 0.01f)
                {
                    foreach (var s in targetString)
                        Destroy(s.gameObject);
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

    void OnStringReachCore(StringColor color)
    {
        var t = targetString.Find(m => m.stringColor == color);
        if (t != null)
        {
            t.gameObject.SetActive(false);
            foreach (var c in colorLayers)
            {
                if (c.color == color && !c.inlayFinished)
                {
                    StartCoroutine(FillColorRoutine(c));
                }
            }
        }
    }

    IEnumerator FillColorRoutine(ColorLayer layer)
    {
        if (layer != null)
        {
            layer.targetRenderer.enabled = true;


            layer.inlayFinished = true;

            yield return null;
        }
    }

    void Display(GlobalEvent evt)
    {
        embroideryStatus = EmbroideryStatus.Display;
    }

    void Hide(GlobalEvent evt)
    {
        if (embroideryStatus != EmbroideryStatus.Inlayed && embroideryStatus != EmbroideryStatus.Focus)
            embroideryStatus = EmbroideryStatus.Hide;
        else
            embroideryStatus = EmbroideryStatus.HideAfterInlay;
    }

    void Focus(GlobalEvent evt)
    {
        if (embroideryStatus == EmbroideryStatus.Inlayed)
        {
            embroideryStatus = EmbroideryStatus.Focus;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(DoubleSidedEmbroidery))]
public class DoubleSidedEmbroideryEditor : Editor
{
    DoubleSidedEmbroidery Target
    {
        get
        {
            return target as DoubleSidedEmbroidery;
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
                foreach (var s in Target.targetString)
                {
                    if (s != null)
                        s.gameObject.SetActive(false);
                }

            }
        }

        if (GUILayout.Button("To Display Position"))
        {
            if (Target.displayPoint != null)
            {
                Target.transform.position = Target.displayPoint.position;
                Target.transform.rotation = Target.displayPoint.rotation;
                Target.transform.parent = Target.displayPoint.parent;
                foreach (var s in Target.targetString)
                {
                    if (s != null)
                        s.gameObject.SetActive(true);
                }

            }
        }
    }

}
#endif
