using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("UISystem")]
    [Tooltip("Operate camera focusing.")]
    public class CameraFocusOperation : FsmStateAction
    {
        public FsmFloat focusDistance = 1.2f;
        public bool focus = true;

        public override void OnEnter()
        {
            if (CameraCharacter.Singleton != null)
            {
                if (focus)
                {
                    CameraCharacter.Singleton.Focus(focusDistance.Value);
                }
                else
                {
                    CameraCharacter.Singleton.CancelFocus();
                }
            }
            Finish();
        }
    }
}