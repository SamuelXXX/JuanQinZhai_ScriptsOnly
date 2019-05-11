// (c) Copyright HutongGames, LLC 2010-2013. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("DrawUpControl")]
    [Tooltip("Show DrawUp conversation.")]
    public class ShowDrawUp : FsmStateAction
    {
        public FsmString roleName;
        public FsmString convSynopsis;

        public override void OnEnter()
        {
            DrawUpContentProvider.Singleton.SetDrawUpContent(roleName.Value, convSynopsis.Value);
            DrawUpUIControl.Singleton.ShowDrawUp();
        }

        public override void OnUpdate()
        {
            if (DrawUpUIControl.Singleton.isHiding)
                Finish();
        }
    }
}