using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("UISystem")]
    [Tooltip("Set image UI content of ui system.")]
    public class SetImageContent : FsmStateAction
    {
        public FsmString resourceLocator;
        public Sprite content;

        public override void OnEnter()
        {
            if (UISystem.Singleton != null)
            {
                UISystem.Singleton.SetContent(resourceLocator.Value, content);
            }
            Finish();
        }
    }
}