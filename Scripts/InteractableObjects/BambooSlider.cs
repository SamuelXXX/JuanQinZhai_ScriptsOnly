using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BambooSlider : MonoBehaviour
{

    public List<BambooPeelingSide> allPeelingSides = new List<BambooPeelingSide>();
    public string finishEvent = "BambooTubeSlided";

    protected int currentPeelingSideIndex = -1;
    protected bool currentPeelingSideActivated;
    bool ready = false;
    bool finished = false;
    bool bodyFreezed = false;

    float minRotateAngle;
    float maxRotateAngle;
    public BambooPeelingSide CurrentSide
    {
        get
        {
            if (currentPeelingSideIndex < 0 || currentPeelingSideIndex >= allPeelingSides.Count)
                return null;

            return allPeelingSides[currentPeelingSideIndex];
        }
    }
    // Use this for initialization
    void Start()
    {
        foreach (var p in allPeelingSides)
        {
            p.bambooSlider = this;
        }

        GlobalEventManager.RegisterHandler("Enable-" + name, EnableSlide);
    }

    private void OnDestroy()
    {
        GlobalEventManager.UnregisterHandler("Enable-" + name, EnableSlide);
    }

    // Update is called once per frame
    void Update()
    {
        if (!ready)
            return;

        if (finished)
            return;

        if (!bodyFreezed)
        {
            var p = InputPlatform.Singleton.GetMoveVector() * InputPlatform.Singleton.ScreenSizeRatio;
            var dx = p.x < 0 ? p.x : 0;
            float deltaX = dx * 0.2f;
            //float deltaY = p.y * 0.2f;
            Vector3 eulerAngles = transform.rotation.eulerAngles;
            eulerAngles += new Vector3(0, -deltaX, 0);
            transform.rotation = Quaternion.Euler(eulerAngles);

            Transform curForward = CurrentSide.slideMainDirection;
            float dot = Vector3.Dot(curForward.forward, -CameraCharacter.Singleton.ownedCamera.transform.forward);
            float cross = Vector3.Cross(curForward.forward, -CameraCharacter.Singleton.ownedCamera.transform.forward).y;

            if (dot > 0.9f || cross < 0f)
            {
                Vector3 lookDir = -CameraCharacter.Singleton.ownedCamera.transform.forward;

                Quaternion targetRotation = Quaternion.LookRotation(lookDir, transform.up);
                Quaternion deltaRotation = targetRotation * Quaternion.Inverse(curForward.rotation);
                transform.rotation = deltaRotation * transform.rotation;
                ActivateCurrentSide();
                GlobalEventManager.SendEvent("BambooPositionDone");
            }

        }
    }



    /// <summary>
    /// Called when a side finished peeling
    /// </summary>
    /// <param name="side"></param>
    public void NotifyFinished(BambooPeelingSide side)
    {
        if (side == CurrentSide)
            SetNextSideAsCurrentSide();
    }

    void EnableSlide(GlobalEvent evt)
    {
        ready = true;
        SetNextSideAsCurrentSide();
    }

    void SetNextSideAsCurrentSide()
    {
        currentPeelingSideIndex++;
        if(currentPeelingSideIndex==1)
        {
            GlobalEventManager.SendEvent("BambooFirstPieceSlided");
        }
        if (currentPeelingSideIndex >= allPeelingSides.Count)
        {
            GlobalEventManager.SendEvent(finishEvent);
            finished = true;
            bodyFreezed = true;
        }
        else
        {
            bodyFreezed = false;
        }
    }

    void ActivateCurrentSide()
    {
        if (currentPeelingSideIndex < 0 || currentPeelingSideIndex >= allPeelingSides.Count)
        {
            return;
        }

        CurrentSide.EnableSlideInteraction(new GlobalEvent(""));
        bodyFreezed = true;
    }
}
