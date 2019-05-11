// (c) Copyright HutongGames, LLC 2010-2013. All rights reserved.

using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("GlobalEvent")]
    [Tooltip("Wait global event.")]
    public class SendGlobalEvent : FsmStateAction
    {
        public FsmString eventName = GlobalEventManager.Back;

        public override void OnEnter()
        {
            GlobalEventManager.SendEvent(eventName.Value);
            Finish();
        }
    }
}