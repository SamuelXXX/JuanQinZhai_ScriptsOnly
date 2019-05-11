using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InlayItemState
{
    Free = 0,
    Holding,
    ReachTarget
}

public class Lid : MonoBehaviour
{
    public Transform targetPoint;
    public float targetJudgeRadius = 0.5f;
    public float constraintRange = 5f;
    public string reachTargetEvent = "LidCovered";


    protected Vector3 moveCenter;
    protected Vector3 holdPosOffset;
    protected Plane constraintPlane;
    protected InlayItemState lidState = InlayItemState.Free;

    Color oriColor;
    Material mainMaterial;
    bool highLighted = false;

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
        constraintPlane = new Plane(transform.up, transform.position);
        moveCenter = transform.position;
        GlobalEventManager.RegisterHandler("Highlight-" + name, Highlight);
        GlobalEventManager.RegisterHandler("Dehighlight-" + name, Dehighlight);
    }

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
    // Update is called once per frame
    void Update()
    {
        UpdateState();
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


    void UpdateState()
    {
        Vector3? tp = InputPlatform.Singleton.GetTouchPoint();
        Ray? ray = null;
        if (tp != null)
        {
            ray = Camera.main.ScreenPointToRay(tp.Value);
        }
        //update state
        switch (lidState)
        {
            case InlayItemState.Free:
                if (ray != null)
                {
                    RaycastHit hitInfo;
                    if (Physics.Raycast(ray.Value, out hitInfo, 30f))
                    {
                        if (hitInfo.collider == GetComponent<Collider>())
                        {
                            lidState = InlayItemState.Holding;
                            float rayDis;
                            constraintPlane.Raycast(ray.Value, out rayDis);
                            holdPosOffset = ray.Value.GetPoint(rayDis) - transform.position;
                        }
                    }
                }
                break;
            case InlayItemState.Holding:
                highLighted = false;
                if (Vector3.Distance(targetPoint.position, transform.position) < targetJudgeRadius)
                {
                    lidState = InlayItemState.ReachTarget;
                    GlobalEventManager.SendEvent(reachTargetEvent);
                    transform.position = targetPoint.position;
                    break;
                }
                if (ray == null)
                    lidState = InlayItemState.Free;
                break;
            case InlayItemState.ReachTarget:
                break;
            default: break;
        }

        //process based on state
        if (lidState == InlayItemState.Holding)
        {
            float rayDis;
            constraintPlane.Raycast(ray.Value, out rayDis);
            Vector3 tPosition = ray.Value.GetPoint(rayDis) - holdPosOffset;
            if (Vector3.Distance(tPosition, moveCenter) > constraintRange)
            {
                Vector3 dir = tPosition - moveCenter;
                transform.position = moveCenter + dir.normalized * constraintRange;
            }
            else
            {
                transform.position = tPosition;
            }
        }

    }
}
