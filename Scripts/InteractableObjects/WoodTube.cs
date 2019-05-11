using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodTube : SkinPeeler
{
    [Header("Animator Settings")]
    public string animationTrigger = "Cut";
    public int maxAnimationCount = 3;

    protected Animator m_Animator;
    protected Animator mAnimator
    {
        get
        {
            if (m_Animator == null)
                m_Animator = GetComponent<Animator>();

            return m_Animator;
        }
    }

    int playCount = 0;
    protected override void OnUpdateProgress(float newProgress)
    {
        if (newProgress > 0.2f && playCount <= 0)
        {
            mAnimator.SetTrigger(animationTrigger + "1");
            playCount++;
        }

        if (newProgress > 0.5f && playCount <= 1)
        {
            mAnimator.SetTrigger(animationTrigger + "2");
            playCount++;
        }

        if (newProgress > 0.7f && playCount <= 2)
        {
            mAnimator.SetTrigger(animationTrigger + "3");
            playCount++;
        }
    }
}
