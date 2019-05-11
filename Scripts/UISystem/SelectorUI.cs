using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image selector UI
/// </summary>
public class SelectorUI : MonoBehaviour
{
    public Button okButton;
    public Image nextImage;
    public string imageContentLocator = "Img_ZhuTai";
    public string okButtonEvent = "";

    protected ScrollRect scrollRect;
    protected Image okButtonImage;
    private void Start()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (okButton)
        {
            okButtonImage = okButton.GetComponent<Image>();
            if (okButtonImage)
            {
                var c = okButtonImage.color;
                c.a = 0.5f;
                okButtonImage.color = c;
            }
            okButton.onClick.AddListener(OnOkClicked);
        }

        if (scrollRect)
        {
            scrollRect.onValueChanged.AddListener(OnValueChanged);
        }
    }

    SelectorUIItem currentSelecting = null;
    public void SwitchSelectingItem(SelectorUIItem newItem)
    {
        if (newItem == currentSelecting)
            return;

        if (currentSelecting != null)
            currentSelecting.DeSelect();

        currentSelecting = newItem;
        if (newItem != null)
        {
            if (okButtonImage)
            {
                var c = okButtonImage.color;
                c.a = 1f;
                okButtonImage.color = c;
            }
            GlobalEventManager.SendEvent("ImageSelected-" + newItem.imageName);
        }
    }

    void OnOkClicked()
    {
        if (currentSelecting)
        {
            GlobalEventManager.SendEvent(okButtonEvent);
        }
    }

    void OnValueChanged(Vector2 pos)
    {
        //Debug.Log("Scroll Value:" + pos.ToString());
        if (pos.y < 0.1)
        {
            nextImage.enabled = false;
        }
        else
        {
            nextImage.enabled = true;
        }
    }


}
