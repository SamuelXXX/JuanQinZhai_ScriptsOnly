using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AirBoxHandler : MonoBehaviour
{
    [Header("Basic Settings")]
    public bool locked = true;
    public float maxPushSpeed = 3;
    public float maxPullSpeed = 6;
    public string reachMaxProgressEvent = "HotnessMax";
    [Header("Fire Flare Settings")]
    public GameObject fireFlare;
    public float maxFireSize = 3f;
    public float currentFireSize = 0f;
    public float fireCoolDelay = 0.8f;
    public float fireCoolRate = 2f;

    [HideInInspector]
    public Vector3 pullEndPosition;
    [HideInInspector]
    public Vector3 pushEndPosition;

    [Header("Progress Settings")]
    public ParticleSystem smokeParticle;
    public float progressRate = 0.3f;
    public float progress = 0f;
    public Image progressUI;
    public string progressUILocator = "SW_BambooBoilingProgress";


    public float pushDepth;
    protected Vector3 pushDir;
    protected Vector3 holdPosOffset;
    protected Plane constraintPlane;
    protected Vector3 targetPosition;
    protected Material flareMaterial;
    ParticleSystem.EmissionModule emission;
    bool isHolding;
    bool isCooling = false;
    float lastPushTime = 0f;
    public float RelativeFireSize
    {
        get
        {
            return currentFireSize / maxFireSize;
        }
    }

    private void Awake()
    {
        if (GetComponentInChildren<MeshRenderer>() != null)
        {
            var m = GetComponentInChildren<MeshRenderer>();
            if (m.materials.Length != 0)
            {
                mainMaterial = m.materials[0];
                oriColor = mainMaterial.GetColor("_Color");
            }
        }
    }

    // Use this for initialization
    void Start()
    {
        transform.position = pullEndPosition;
        constraintPlane = new Plane(transform.up, transform.position);
        pushDir = pushEndPosition - pullEndPosition;
        progress = 0f;
        flareMaterial = fireFlare.GetComponent<MeshRenderer>().materials[0];
        emission = smokeParticle.emission;

        GlobalEventManager.RegisterHandler("Unlock-" + name, a =>
           {
               locked = false;
           });
        GlobalEventManager.RegisterHandler("Lock-" + name, a =>
        {
            locked = true;
        });

        GlobalEventManager.RegisterHandler("Highlight-" + name, Highlight);
        GlobalEventManager.RegisterHandler("Dehighlight-" + name, Dehighlight);
    }

    bool highLighted = false;
    Color oriColor;
    Material mainMaterial;


    void Highlight(GlobalEvent evt)
    {
        highLighted = true;
    }

    void Dehighlight(GlobalEvent evt)
    {
        highLighted = false;
    }

    bool up = true;
    Color curColor = Color.gray;

    void UpdateAppearance()
    {
        if (highLighted)
        {
            if (mainMaterial)
            {
                if (up)
                {
                    curColor.r += Time.deltaTime * 0.5f;
                    if (curColor.r > 1)
                    {
                        curColor.r = 1;
                        up = false;
                    }
                }
                else
                {
                    curColor.r -= Time.deltaTime * 0.5f;
                    if (curColor.r < oriColor.r)
                    {
                        curColor.r = oriColor.r;
                        up = true;
                    }
                }

                curColor.g = curColor.r;
                curColor.b = curColor.r;
                mainMaterial.SetColor("_Color", curColor);
            }
        }
        else
        {
            if (mainMaterial)
            {
                mainMaterial.SetColor("_Color", oriColor);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAppearance();
        var c = flareMaterial.GetColor("_TintColor");
        float h, s, v;
        Color.RGBToHSV(c, out h, out s, out v);
        s = 1 - (currentFireSize / maxFireSize);
        c = Color.HSVToRGB(h, s, v);
        flareMaterial.SetColor("_TintColor", c);
        //flareMaterial.SetFloat("_InvFade", currentFireSize);

        if (locked)
            return;
        Vector3? tp = InputPlatform.Singleton.GetTouchPoint();
        Ray? ray = null;
        if (tp != null)
        {
            ray = Camera.main.ScreenPointToRay(tp.Value);
        }

        //update holding state
        if (!isHolding)
        {
            if (ray != null)
            {
                RaycastHit hitInfo;
                if (Physics.Raycast(ray.Value, out hitInfo))
                {
                    if (hitInfo.collider == GetComponent<Collider>())
                    {
                        float rayDis;
                        constraintPlane.Raycast(ray.Value, out rayDis);
                        holdPosOffset = ray.Value.GetPoint(rayDis) - transform.position;
                        isHolding = true;
                    }
                }
            }
        }
        else
        {
            if (ray == null)
            {
                isHolding = false;
            }
        }

        if (isHolding && progress < 1f)
        {
            highLighted = false;
            //Calculate target position
            float rayDis;
            constraintPlane.Raycast(ray.Value, out rayDis);
            Vector3 tPosition = ray.Value.GetPoint(rayDis) - holdPosOffset;
            Vector3 diff = tPosition - pullEndPosition;
            float tgtDepth = Vector3.Dot(diff, pushDir.normalized);
            tgtDepth /= pushDir.magnitude;
            tgtDepth = Mathf.Clamp(tgtDepth, 0f, 1f);
            targetPosition = pullEndPosition + pushDir * tgtDepth;

            //Move handler
            Vector3 movVec = targetPosition - transform.position;
            if (tgtDepth > pushDepth)//push
            {
                Vector3 deltaMovement = maxPushSpeed * Time.deltaTime * movVec.normalized;
                if (deltaMovement.magnitude < movVec.magnitude)
                    transform.position += deltaMovement;
                else
                    transform.position = targetPosition;
            }
            else//pull
            {
                Vector3 deltaMovement = maxPullSpeed * Time.deltaTime * movVec.normalized;
                if (deltaMovement.magnitude < movVec.magnitude)
                    transform.position += deltaMovement;
                else
                    transform.position = targetPosition;
            }

            //Update progress
            float newDepth = GetDepthByPosition();

            OnUpdateDepth(newDepth - pushDepth);
            pushDepth = newDepth;
        }

        if (isCooling)
        {
            currentFireSize -= fireCoolRate * Time.deltaTime;
            if (currentFireSize < 0f)
                currentFireSize = 0f;
        }
        else
        {
            if (Time.time > lastPushTime + fireCoolDelay)
            {
                isCooling = true;
            }
        }

        if (progress < 1f)
        {
            progress += RelativeFireSize * Time.deltaTime * progressRate;
            if (progressUI != null)
                progressUI.fillAmount = progress;
            if (!uiOn)
            {
                UISystem.Singleton.SetContent(progressUILocator, true);
                uiOn = true;
            }
            if (progress >= 1f)
            {
                progress = 1f;
                OnReachTargetProgress();
            }

        }

        emission.rateOverTime = 20 * progress;
    }


    bool uiOn = false;
    float GetDepthByPosition()
    {
        Vector3 diff = transform.position - pullEndPosition;
        float newDepth = Vector3.Dot(diff, pushDir.normalized);
        newDepth /= pushDir.magnitude;
        newDepth = Mathf.Clamp(newDepth, 0f, 1f);
        targetPosition = pullEndPosition + pushDir * newDepth;
        return newDepth;
    }

    void OnUpdateDepth(float deltaDepth)
    {
        if (currentFireSize >= maxFireSize)
        {
            currentFireSize = maxFireSize;
            return;
        }

        if (deltaDepth > 0f)
        {
            currentFireSize += deltaDepth;
            if (currentFireSize >= maxFireSize)
            {
                currentFireSize = maxFireSize;

            }
            lastPushTime = Time.time;
            isCooling = false;
        }
    }

    void OnReachTargetProgress()
    {
        UISystem.Singleton.SetContent(progressUILocator, false);
        uiOn = false;
        GlobalEventManager.SendEvent(reachMaxProgressEvent);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(pullEndPosition, pushEndPosition);
    }
#endif

}

#if UNITY_EDITOR
[CustomEditor(typeof(AirBoxHandler))]
public class AirBoxHandlerEditor : Editor
{
    public AirBoxHandler Target
    {
        get
        {
            return target as AirBoxHandler;
        }
    }
    bool allowDataBuilding = false;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        if (GUILayout.Button("To Pull End"))
        {
            Target.transform.position = Target.pullEndPosition;
        }
        if (GUILayout.Button("To Push End"))
        {
            Target.transform.position = Target.pushEndPosition;
        }
        EditorGUILayout.Space();

        allowDataBuilding = EditorGUILayout.Toggle("Allow Data Building", allowDataBuilding);

        if (allowDataBuilding)
        {
            if (GUILayout.Button("Build Pull End"))
            {
                Target.pullEndPosition = Target.transform.position;
                allowDataBuilding = false;
            }
            if (GUILayout.Button("Build Push End"))
            {
                Target.pushEndPosition = Target.transform.position;
                allowDataBuilding = false;
            }
        }
    }
}
#endif


