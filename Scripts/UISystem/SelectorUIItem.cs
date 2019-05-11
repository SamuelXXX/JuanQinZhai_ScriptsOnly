using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Image selector ui item
/// </summary>
public class SelectorUIItem : MonoBehaviour
{
    public string imageName = "Dummy Image";
    public Texture2D extendedImage;
    public string resourceLocator = null;

    [HideInInspector]
    public Image displayImage;
    protected SelectorUI manager;
    protected Image selectedIcon;
    protected Button button;
    // Use this for initialization
    void Start()
    {
        selectedIcon = transform.Find("SelectedIcon").GetComponent<Image>();
        displayImage = GetComponent<Image>();
        selectedIcon.enabled = false;
        button = GetComponentInChildren<Button>();
        manager = GetComponentInParent<SelectorUI>();
        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnClicked()
    {
        selectedIcon.enabled = true;
        var c = selectedIcon.color;
        c.a = 0.8f;
        selectedIcon.color = c;
        LoadAsset();
        UISystem.Singleton.SetContent(manager.imageContentLocator, extendedImage);
        manager.SwitchSelectingItem(this);
    }

    public void DeSelect()
    {
        UnloadAsset();
        selectedIcon.enabled = false;
        var c = selectedIcon.color;
        c.a = 1f;
        selectedIcon.color = c;
    }

    #region resource managing
    void UnloadAsset()
    {
        if (Resources.Load<Texture2D>(extendedImage.name) == extendedImage)
        {
            resourceLocator = extendedImage.name;
            extendedImage = null;
        }
    }

    void LoadAsset()
    {
        if (extendedImage == null && resourceLocator != null)
        {
            extendedImage = Resources.Load<Texture2D>(resourceLocator);
        }
    }
#if UNITY_EDITOR
    public static void AssetToName()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<SelectorUIItem> selectorUIs = new List<SelectorUIItem>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            selectorUIs.AddRange(go.GetComponentsInChildren<SelectorUIItem>(false));
        }

        foreach (var l in selectorUIs)
        {
            if (l.gameObject.activeInHierarchy == false)
                continue;
            l.UnloadAsset();
        }

        EditorApplication.MarkSceneDirty();
        EditorApplication.SaveScene();
        AssetDatabase.SaveAssets();
    }

    public static void NameToAsset()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<SelectorUIItem> selectorUIs = new List<SelectorUIItem>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            selectorUIs.AddRange(go.GetComponentsInChildren<SelectorUIItem>(false));
        }

        foreach (var l in selectorUIs)
        {
            if (l.gameObject.activeInHierarchy == false)
                continue;
            l.LoadAsset();
        }

        EditorApplication.MarkSceneDirty();
        EditorApplication.SaveScene();
        AssetDatabase.SaveAssets();
    }
    
#endif
    #endregion
}
