using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BambooPeelingSide : SkinPeeler
{

    [Header("Animation Settings")]
    public Transform slideMainDirection;
    public AnimationClip firstStageAnimation;
    public AnimationClip secondStageAnimation;


    float animTimer = 0f;
    public BambooSlider bambooSlider = null;
    protected override void OnUpdateProgress(float newProgress)
    {
        firstStageAnimation.SampleAnimation(gameObject, firstStageAnimation.length * (currentProgress / maxProgressTimes));
    }
    protected override void OnUpdateAfterSlideDone()
    {
        bool shouldNotifySlider = true;
        if (animTimer > secondStageAnimation.length)
        {
            shouldNotifySlider = false;
        }
        animTimer += Time.deltaTime;

        if (animTimer < secondStageAnimation.length)
        {
            secondStageAnimation.SampleAnimation(gameObject, animTimer);
            shouldNotifySlider = false;
        }

        if (shouldNotifySlider && bambooSlider != null)
        {
            bambooSlider.NotifyFinished(this);
        }

    }
}
