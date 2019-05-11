// (c) Copyright HutongGames, LLC 2010-2013. All rights reserved.

using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("GlobalEvent")]
    [Tooltip("Wait global event.")]
    public class WaitGlobalEvent : FsmStateAction
    {
        public FsmString eventName = GlobalEventManager.DrawUpClicked;
        public FsmBool ignorePastEvent = true;

        public override void OnEnter()
        {
            if (ignorePastEvent.Value)
                GlobalEventManager.RegisterHandler(eventName.Value, EventHandler);
        }

        public override void OnUpdate()
        {
            if (!ignorePastEvent.Value && GlobalEventManager.PeekEvent(eventName.Value))
            {
                Finish();
            }
        }

        public override void OnExit()
        {
            if (ignorePastEvent.Value)
                GlobalEventManager.UnregisterHandler(eventName.Value, EventHandler);
        }

        void EventHandler(GlobalEvent evt)
        {
            Finish();
        }
    }
}