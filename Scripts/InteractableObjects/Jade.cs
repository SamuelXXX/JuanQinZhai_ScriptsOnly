using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Jade controller, get moved by hand moving position
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class Jade : MonoBehaviour
{
    [HideInInspector]
    public Transform targetPoint;
    public float constraintRange = 5f;

    protected float targetJudgeRadius = 0.05f;
    protected Vector3 moveCenter;
    protected Vector3 holdPosOffset;
    protected Plane constraintPlane;
    protected InlayItemState jadeState = InlayItemState.Free;

    public delegate bool InlayEventHandler();
    public InlayEventHandler OnInlayHandler = null;

    // Need to reset basic data when enabled
    void OnEnable()
    {
        constraintPlane = new Plane(transform.forward, transform.position);
    }

    public void SetMoveCenter(Vector3 center)
    {
        moveCenter = center;
    }
    // Update is called once per frame
    void Update()
    {
        UpdateState();
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
        switch (jadeState)
        {
            case InlayItemState.Free:
                if (ray != null)
                {
                    RaycastHit hitInfo;
                    if (Physics.Raycast(ray.Value, out hitInfo))
                    {
                        if (hitInfo.collider == GetComponent<Collider>())
                        {
                            jadeState = InlayItemState.Holding;
                            float rayDis;
                            constraintPlane.Raycast(ray.Value, out rayDis);
                            holdPosOffset = ray.Value.GetPoint(rayDis) - transform.position;
                        }
                    }
                }
                break;
            case InlayItemState.Holding:
                if (ray == null)
                {
                    jadeState = InlayItemState.Free;
                    break;
                }

                if (Vector3.Distance(targetPoint.position, transform.position) < targetJudgeRadius)
                {
                    if (OnInlayHandler != null && !OnInlayHandler())
                    {
                        break;
                    }
                    jadeState = InlayItemState.ReachTarget;
                    transform.position = targetPoint.position;
                    break;
                }

                break;
            case InlayItemState.ReachTarget:
                break;
            default: break;
        }

        //process based on state
        if (jadeState == InlayItemState.Holding)
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
        else if (jadeState == InlayItemState.Free)
        {
            transform.position = Vector3.Lerp(transform.position, moveCenter, Time.deltaTime * 10f);
        }
    }
}
