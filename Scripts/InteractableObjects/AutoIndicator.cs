using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoIndicator : MonoBehaviour
{
    public Transform focusTarget;
    public float lerpSpeed = 4f;
    public BlockCombinationStatus enablingStatus = BlockCombinationStatus.Divide;
    public Vector3 positionOffset = Vector3.zero;
    public bool showInGuideMode = true;

    RectTransform rectTransform;
    // Use this for initialization
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!showInGuideMode && CameraCharacter.Singleton.GameModeC == GameMode.GuideMode)
            return;

        if (focusTarget == null)
            return;

        if (BlockManager.Singleton.status != enablingStatus || CameraCharacter.Singleton.InViewTransitioning)
        {
            GetComponent<Canvas>().enabled = false;
            return;
        }




        Vector3 v = focusTarget.position;
        v += positionOffset;

        v = CameraCharacter.Singleton.ownedCamera.WorldToScreenPoint(v);

        if (GetComponent<Canvas>().enabled == false)
        {
            GetComponent<Canvas>().enabled = true;
            rectTransform.position = v;
        }
        else
        {
            rectTransform.position = Vector3.Lerp(rectTransform.position, v, Time.deltaTime * lerpSpeed);
        }


    }
}
