using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("UISystem")]
    [Tooltip("Set text UI content of ui system.")]
    public class SetTextContent : FsmStateAction
    {
        public FsmString resourceLocator;
        public FsmString content;

        public override void OnEnter()
        {
            if(UISystem.Singleton!=null)
            {
                UISystem.Singleton.SetContent(resourceLocator.Value, content.Value);
            }
            Finish();
        }
    }
}