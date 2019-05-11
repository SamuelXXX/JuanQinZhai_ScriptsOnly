// (c) Copyright HutongGames, LLC 2010-2013. All rights reserved.

using UnityEngine;
using UnityEngine.UI;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("SpriteRenderer")]
    public class SpriteAlphaTurning : FsmStateAction
    {
        [RequiredField]
        public FsmFloat time;
        public FsmColor targetColor;
        public FsmEvent finishEvent;
        protected Image image;


        private float timer;
        private Color originalColor;


        public override void Reset()
        {
            time = 1f;
            finishEvent = null;
        }

        public override void OnEnter()
        {
            image = Fsm.GameObject.GetComponent<Image>();
            if (image == null)
            {
                Finish();
                return;
            }

            originalColor = image.color;
            if(originalColor.a==0)
            {
                image.enabled = true;
            }
            if (time.Value <= 0)
            {
                Fsm.Event(finishEvent);
                image.color = targetColor.Value;
                Finish();
                return;
            }

            timer = 0f;
        }

        public override void OnUpdate()
        {
            // update time


            timer += Time.deltaTime;



            if (timer >= time.Value)
            {
                image.color = targetColor.Value;
                if(image.color.a==0f)
                {
                    image.enabled = false;
                }
                Finish();
                if (finishEvent != null)
                {
                    Fsm.Event(finishEvent);
                }
            }
            else
            {
                float normalize = timer / time.Value;
                image.color = Color.Lerp(originalColor, targetColor.Value, normalize);

            }
        }

    }
}
