// (c) Copyright HutongGames, LLC 2010-2013. All rights reserved.

using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("GlobalEvent")]
    [Tooltip("Load iamge resources.")]
    public class LoadImage : FsmStateAction
    {
        protected ImageLoader imageLoader;
        public override void OnEnter()
        {
            base.OnEnter();
            imageLoader = Fsm.GameObject.GetComponent<ImageLoader>();
            if (imageLoader == null)
                Finish();

            imageLoader.LoadResources();
        }

        public override void OnUpdate()
        {
            if (imageLoader == null)
                Finish();
            if(imageLoader.HasImage)
                Finish();
        }
    }
}