using UnityEngine;
using UnityEngine.UI;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("UISystem")]
    [Tooltip("Adjust camera mask.")]
    public class CameraMaskAdjust : FsmStateAction
    {
        Image maskImage;
        public FsmFloat adjustTime = 1f;
        public FsmColor target;
        public FsmInt sortOrder = 0;

        Color color;
        float timer = 0f;

        public override void OnEnter()
        {
            maskImage = CameraCharacter.Singleton.cameraMask;
            if (maskImage == null)
            {
                Finish();
                return;
            }
            color = maskImage.color;
            timer = 0f;
            maskImage.canvas.sortingOrder = sortOrder.Value;
        }

        public override void OnUpdate()
        {
            timer += Time.deltaTime;
            if (timer < adjustTime.Value)
            {
                maskImage.color = Color.Lerp(color, target.Value, timer / adjustTime.Value);
            }
            else
            {
                maskImage.color = target.Value;
                Finish();
            }
        }
    }
}