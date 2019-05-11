using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("UISystem")]
    [Tooltip("Set on-off command UI content of ui system.")]
    public class SetOnOffContent : FsmStateAction
    {
        public FsmString resourceLocator;
        public FsmBool content;

        public override void OnEnter()
        {
            if (UISystem.Singleton != null)
            {
                UISystem.Singleton.SetContent(resourceLocator.Value, content.Value);
            }
            Finish();
        }
    }
}