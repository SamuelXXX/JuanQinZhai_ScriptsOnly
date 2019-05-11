using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum UIContentType
{
    Text,
    Image,
    OnOffCommand,
    Texture2D
}

public enum UIComponentType
{
    Text,
    Image,
    Canvas,
    GameObject//not used really
}
/// <summary>
/// Get ui content data from ui resources database and apply it for self ui component
/// </summary>
public class UIContentGetter : MonoBehaviour
{
    #region Settings and run-time data
    public string UIContentLocator;
    public UIComponentType componentType = UIComponentType.Text;

    /// <summary>
    /// Target resources accessor
    /// </summary>
    protected UIResourceImage resourceImage;

    protected Text mText;
    protected Image mImage;
    protected Canvas mCanvas;
    protected Material mMaterial;
    protected Texture2D currentPicture;
    #endregion

    #region Mono Events
    // Use this for initialization
    void Start()
    {
        resourceImage = UISystem.Singleton.GetResourceImage(UIContentLocator);

        mText = GetComponent<Text>();
        mImage = GetComponent<Image>();
        mCanvas = GetComponent<Canvas>();
        var renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            mMaterial = renderer.material;
            currentPicture = mMaterial.mainTexture as Texture2D;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (resourceImage == null)
            return;

        switch (resourceImage.ContentType)
        {
            case UIContentType.Text:
                if (mText != null)
                    mText.text = resourceImage.Text;
                break;
            case UIContentType.Image:
                if (mImage != null)
                    mImage.sprite = resourceImage.Image;

                break;
            case UIContentType.Texture2D:
                if (mMaterial != null)//has mesh renderer,for special non ui elements
                {
                    if (currentPicture != resourceImage.texture)
                    {
                        currentPicture = resourceImage.texture;
                        mMaterial.mainTexture = currentPicture;
                    }
                }
                break;
            case UIContentType.OnOffCommand:
                switch (componentType)
                {
                    case UIComponentType.Text:
                        if (mText != null)
                        {
                            mText.enabled = resourceImage.OnOffCommand;
                        }
                        break;
                    case UIComponentType.Image:
                        if (mImage != null)
                        {
                            mImage.enabled = resourceImage.OnOffCommand;
                        }
                        break;
                    case UIComponentType.Canvas:
                        if (mCanvas != null)
                        {
                            mCanvas.enabled = resourceImage.OnOffCommand;
                        }
                        break;
                    case UIComponentType.GameObject:
                        gameObject.SetActive(resourceImage.OnOffCommand);
                        break;
                    default:
                        break;
                }

                //if (IsSwitch)
                //{
                //    if (resourceImage.OnOffCommand)
                //    {
                //        LoadAllSprites();
                //    }
                //    else
                //    {
                //        UnloadAllSprites();
                //    }
                //}

                break;
            default: break;
        }
    }
    #endregion


    #region Image Mapper Tools
    [System.Serializable]
    public class SubImageMapper
    {
        public Image targetComponent;
        public string resourceLocator;
    }

    public List<SubImageMapper> subImageMappers = new List<SubImageMapper>();
    public bool IsSwitch
    {
        get
        {
            if (componentType == UIComponentType.Text || componentType == UIComponentType.GameObject)
                return false;

            UIResourceImage resourceImage = UISystem.Singleton.GetResourceImage(UIContentLocator);
            if (resourceImage.ContentType == UIContentType.OnOffCommand)
            {
                return true;
            }

            return false;

        }
    }

    void LoadAllSprites()
    {
        foreach (var i in subImageMappers)
        {
            if (i.targetComponent.sprite != null)
                continue;
            i.targetComponent.sprite = Resources.Load<Sprite>(i.resourceLocator);
        }
    }

    void UnloadAllSprites()
    {
        foreach (var i in subImageMappers)
        {
            if (i.targetComponent.sprite == null)
                continue;
            var s = i.targetComponent.sprite;
            i.targetComponent.sprite = null;
            Resources.UnloadAsset(s);
        }
    }


    void BuildImageMapper()
    {
        if (!IsSwitch)
            return;

        if (subImageMappers.Count != 0)
        {
            Debug.Log(name + ":Has Image Content, Cannot rebuild,please kill imageMapper first");
            return;
        }


        foreach (var i in GetComponentsInChildren<Image>())
        {
            if (i.GetComponent<UIContentGetter>() != null)
            {
                var p = i.GetComponent<UIContentGetter>();
                var ri = UISystem.Singleton.GetResourceImage(p.UIContentLocator);
                if (ri.ContentType == UIContentType.Image)
                    continue;
            }
            if (i.sprite != null)
            {
                //sprite is in resources
                if (Resources.Load<Sprite>(i.sprite.name) == i.sprite)
                {
                    var mapper = new SubImageMapper();
                    mapper.targetComponent = i;
                    mapper.resourceLocator = i.sprite.name;
                    subImageMappers.Add(mapper);
                    i.sprite = null;
                }
            }
        }
    }

    void KillImageMapper()
    {
        if (!IsSwitch)
            return;

        foreach (var i in subImageMappers)
        {
            i.targetComponent.sprite = Resources.Load<Sprite>(i.resourceLocator);
        }

        subImageMappers.Clear();
    }

#if UNITY_EDITOR
    //[MenuItem("Optimize Tools/UI:Build Sub ImageManager")]
    public static void BuildSubImageManager()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<UIContentGetter> contentSwitcher = new List<UIContentGetter>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            contentSwitcher.AddRange(go.GetComponentsInChildren<UIContentGetter>(false));
        }

        contentSwitcher.RemoveAll(p => p.IsSwitch == false);
        foreach (var s in contentSwitcher)
        {
            //s.BuildImageMapper();
        }

        Debug.Log(contentSwitcher.Count + " Switcher");

        Resources.UnloadUnusedAssets();
    }

    //[MenuItem("Optimize Tools/UI:Kill Sub ImageManager")]
    public static void KillSubImageManager()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<UIContentGetter> contentSwitcher = new List<UIContentGetter>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            contentSwitcher.AddRange(go.GetComponentsInChildren<UIContentGetter>(false));
        }

        contentSwitcher.RemoveAll(p => p.IsSwitch == false);
        foreach (var s in contentSwitcher)
        {
            //s.KillImageMapper();
        }

        Resources.UnloadUnusedAssets();
    }
#endif
    #endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(UIContentGetter))]
public class ContentGetterEditor : Editor
{
    UIContentGetter Target
    {
        get
        {
            return target as UIContentGetter;
        }
    }

    public override void OnInspectorGUI()
    {
        string[] avaliableLocators = UISystem.Singleton.GetLocators();
        List<string> locatorsList = new List<string>();
        locatorsList.AddRange(avaliableLocators);

        if (locatorsList.Count == 0)
        {
            EditorGUILayout.LabelField("UI Content Locators List is Empty!!!");
        }
        else
        {
            int index = 0;

            if (!locatorsList.Contains(Target.UIContentLocator))
            {
                Target.UIContentLocator = avaliableLocators[0];
                index = 0;
            }
            else
            {
                index = locatorsList.IndexOf(Target.UIContentLocator);
            }

            int[] popUpList = new int[avaliableLocators.Length];
            for (int i = 0; i < popUpList.Length; i++)
            {
                popUpList[i] = i;
            }

            index = EditorGUILayout.IntPopup("UIContentLocator", index, avaliableLocators, popUpList);
            Target.UIContentLocator = avaliableLocators[index];
        }

        UIResourceImage ri = UISystem.Singleton.GetResourceImage(Target.UIContentLocator);
        EditorGUILayout.LabelField("Content Type:" + ri.ContentType.ToString());

        EditorGUILayout.Space();
        if (ri.ContentType == UIContentType.OnOffCommand)
        {
            Target.componentType = (UIComponentType)EditorGUILayout.EnumPopup("TargetComponentType", Target.componentType);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

}
#endif

