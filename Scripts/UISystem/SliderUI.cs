using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderUI : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnValueChanged(float v)
    {
        CameraCharacter.Singleton.ownedCamera.fieldOfView = -30 * (v - 2f);
    }
}
