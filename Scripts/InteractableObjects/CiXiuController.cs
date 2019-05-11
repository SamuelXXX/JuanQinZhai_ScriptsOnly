using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CiXiuController : MonoBehaviour
{
    public enum SelectedColor
    {
        None = 0,
        Red,
        Gold
    }
    public RectTransform hoverArea;
    public Image goldImage;
    public Image redImage;
    public string finishEvent = "CixiuFinished";
    public SelectedColor selectedColor = SelectedColor.None;

    bool redFinished = false;
    bool goldFinished = false;
    protected Canvas canvas;

    // Use this for initialization
    void Start()
    {
        canvas = GetComponent<Canvas>();

    }

    // Update is called once per frame
    void Update()
    {
        if (canvas.enabled == false)
            return;
        if (selectedColor == SelectedColor.None)
            return;
        if (redFinished && goldFinished)
            return;
        if (Inside())
        {
            Vector3 v = InputPlatform.Singleton.GetMoveVector();
            float m = v.magnitude * InputPlatform.Singleton.ScreenSizeRatio * 0.0001f;
            switch (selectedColor)
            {
                case SelectedColor.Red:
                    if (redFinished)
                        break;
                    redImage.fillAmount += m;
                    if (redImage.fillAmount >= 1f)
                        redFinished = true;
                    break;
                case SelectedColor.Gold:
                    if (goldFinished)
                        break;
                    goldImage.fillAmount += m;
                    if (goldImage.fillAmount >= 1f)
                        goldFinished = true;
                    break;
                default: break;
            }

            if (redFinished && goldFinished)
            {
                GlobalEventManager.SendEvent(finishEvent);
            }
        }
    }


    bool Inside()
    {
        if (hoverArea == null)
            return false;
        Rect rect = hoverArea.rect;
        Vector3 position = hoverArea.position;
        rect.x += position.x;
        rect.y += position.y;
        Vector3? v = InputPlatform.Singleton.GetTouchPoint();

        if (v == null)
            return false;

        Vector2 point = new Vector2(v.Value.x, v.Value.y);


        if (point.x < rect.x)
            return false;

        if (point.y < rect.y)
            return false;

        if (point.x > rect.x + rect.width)
            return false;

        if (point.y > rect.y + rect.height)
            return false;

        //Debug.Log("inside");
        return true;
    }

    public void OnRedSelected(bool value)
    {
        if (value)
        {
            selectedColor = SelectedColor.Red;
        }
    }

    public void OnGoldSelected(bool value)
    {
        if (value)
        {
            selectedColor = SelectedColor.Gold;
        }
    }

}


