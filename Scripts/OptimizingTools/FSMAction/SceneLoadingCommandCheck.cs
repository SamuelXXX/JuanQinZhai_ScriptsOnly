// (c) Copyright HutongGames, LLC 2010-2013. All rights reserved.

using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("GlobalEvent")]
    [Tooltip("Wait dynamic mesh combining finished.")]
    public class SceneLoadingCommandCheck : FsmStateAction
    {
        public FsmString command;
        public FsmString fsmEvent;
        public override void OnEnter()
        {
            if (command.Value == SceneLoadingManager.sceneReentranceCommand)
            {
                Fsm.SendEventToFsmOnGameObject(Fsm.GameObject, Fsm.Name, fsmEvent.Value);
            }

            if (SceneLoadingManager.loadingOperation == null)
                Finish();
        }

        public override void OnUpdate()
        {
            if (SceneLoadingManager.loadingOperation.isDone)
                Finish();
        }
    }
}