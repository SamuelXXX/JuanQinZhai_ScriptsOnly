// (c) Copyright HutongGames, LLC 2010-2013. All rights reserved.

using UnityEngine;
using System.Collections.Generic;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("GlobalEvent")]
    [Tooltip("peek global events.")]
    public class PeekGlobalEvents : FsmStateAction
    {
        public string eventNames="eventA,eventB,eventC";
        public bool waitUntillSucceed = true;
        public FsmString succeedFsmEvent = "Succeed";
        public FsmString failedFsmEvent = "Failed";

        List<string> events = new List<string>();
        public override void OnEnter()
        {
            eventNames.Trim(' ');
            events.AddRange(eventNames.Split(','));
            events.RemoveAll(a =>
            {
                return string.IsNullOrEmpty(a);
            });

            if (!waitUntillSucceed)
            {
                if (DoCheck())
                {
                    KillAllEvents();
                    Fsm.SendEventToFsmOnGameObject(Fsm.GameObject, Fsm.Name, succeedFsmEvent.Value);
                }
                else
                {
                    Fsm.SendEventToFsmOnGameObject(Fsm.GameObject, Fsm.Name, failedFsmEvent.Value);
                }
                Finish();
            }
        }

        bool DoCheck()
        {
            return events.Count == events.FindAll(a =>
              {
                  if (GlobalEventManager.PeekEvent(a, false))
                  {
                      return true;
                  }
                  return false;
              }
            ).Count;
        }

        void KillAllEvents()
        {
            events.RemoveAll(a =>
            {
                //So will clear events in events pool
                if (GlobalEventManager.PeekEvent(a))
                {
                    return true;
                }
                return false;
            }
            );
            events.Clear();
        }


        public override void OnUpdate()
        {
            if (!waitUntillSucceed)
            {
                Finish();
                return;
            }

            if (DoCheck())
            {
                KillAllEvents();
                Fsm.SendEventToFsmOnGameObject(Fsm.GameObject, Fsm.Name, succeedFsmEvent.Value);
                Finish();
            }
        }
    }
}