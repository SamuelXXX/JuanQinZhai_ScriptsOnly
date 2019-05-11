using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinPeeler : MonoBehaviour
{
    public Transform slideStartPostion;
    public Transform slideStopPosition;
    public float tubeRadius;
    public float maxProgressTimes = 3f;
    public string slideDoneEvent = "SlideFinished";

    protected GestureOperationStatus gestureOperationStatus = null;
    public float currentProgress = 0;
    public bool SlidingIsDone
    {
        get
        {
            return currentProgress >= maxProgressTimes;
        }
    }
    [Header("Open for debug")]
    public bool readyToInteract = false;

    // Use this for initialization
    void Start()
    {
        GlobalEventManager.RegisterHandler("Unlock-" + name, EnableSlideInteraction);
        GlobalEventManager.RegisterHandler("Lock-" + name, DisableSlideInteraction);
    }

    void OnDestroy()
    {
        GlobalEventManager.UnregisterHandler("Unlock-" + name, EnableSlideInteraction);
        GlobalEventManager.UnregisterHandler("Lock-" + name, DisableSlideInteraction);
    }

    // Update is called once per frame
    void Update()
    {
        if (readyToInteract && !SlidingIsDone)
        {
            if (gestureOperationStatus == null || gestureOperationStatus.isDone)
            {
                SetReadyToSlide();
            }

            var progress = gestureOperationStatus.progress;
            if (progress > currentProgress)
            {
                currentProgress = progress;
                OnUpdateProgress(currentProgress);           
            }
        }

        if (SlidingIsDone)
        {
            OnUpdateAfterSlideDone();           
        }

        if (SlidingIsDone && !slideDoneEventCalled)
        {
            slideDoneEventCalled = true;
            OnSlideDone();
        }
    }

    protected virtual void OnUpdateProgress(float newProgress)
    {

    }

    protected virtual void OnUpdateAfterSlideDone()
    {

    }

    bool slideDoneEventCalled = false;
    void OnSlideDone()
    {
        GlobalEventManager.SendEvent(slideDoneEvent);
        readyToInteract = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawLine(slideStartPostion.position, slideStopPosition.position);
        Gizmos.DrawWireSphere((slideStartPostion.position + slideStopPosition.position) / 2f, tubeRadius);
    }

    public void EnableSlideInteraction(GlobalEvent evt)
    {
        readyToInteract = true;
    }

    public void DisableSlideInteraction(GlobalEvent evt)
    {
        readyToInteract = false;
    }

    void SetReadyToSlide()
    {
        if (gestureOperationStatus != null)
        {
            InputPlatform.Singleton.RemoveGestureCommand(gestureOperationStatus.id);
            gestureOperationStatus = null;
        }
        Vector3 startPosition = Camera.main.WorldToScreenPoint(slideStartPostion.position);
        Vector3 stopPosition = Camera.main.WorldToScreenPoint(slideStopPosition.position);
        startPosition.z = 0f;
        stopPosition.z = 0f;
        float roundDis = 100f / InputPlatform.Singleton.ScreenSizeRatio;
        gestureOperationStatus = InputPlatform.Singleton.PushLiningGestureCommand(startPosition, stopPosition, roundDis);
    }
}
