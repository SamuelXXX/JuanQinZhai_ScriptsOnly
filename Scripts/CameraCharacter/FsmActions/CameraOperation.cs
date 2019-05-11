using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("UISystem")]
    [Tooltip("Operate camera.")]
    public class CameraOperation : FsmStateAction
    {
        public FsmBool freezeOrNot;
        public FsmBool allowInteraction = true;

        public override void OnEnter()
        {
            if (CameraCharacter.Singleton != null)
            {
                if (freezeOrNot.Value)
                {
                    CameraCharacter.Singleton.FreezeView();
                }
                else
                {
                    CameraCharacter.Singleton.UnfreezeView();
                }
                CameraCharacter.Singleton.allowInteraction = allowInteraction.Value;
            }
            Finish();
        }
    }
}