// (c) Copyright HutongGames, LLC 2010-2013. All rights reserved.

using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("GlobalEvent")]
    [Tooltip("Wait dynamic mesh combining finished.")]
    public class WaitOptimizingFinished : FsmStateAction
    {
        public override void OnUpdate()
        {
#if !STATIC_OPTIMIZE
            if(MeshOptimizer.allDynamicCombinerFinished)
            {
                Finish();
            }
#else
            Finish();
#endif
        }
    }
}