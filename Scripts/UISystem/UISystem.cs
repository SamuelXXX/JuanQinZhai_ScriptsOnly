using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UISystem : MonoBehaviour
{
    #region singleton
    protected static UISystem singleton;
    public static UISystem Singleton
    {
        get
        {
#if UNITY_EDITOR
            if (singleton == null)
            {
                singleton = FindObjectOfType<UISystem>();
            }
#endif
            return singleton;
        }
        private set
        {
            singleton = value;
        }
    }
    #endregion
    public List<UIResource> UIResourceDatabase = new List<UIResource>();
    /// <summary>
    /// Get href from javascript codes
    /// </summary>
    /// <returns></returns>
    [DllImport("__Internal")]
    private static extern string GetHref();
    Hashtable uiResourceDBCache = new Hashtable();

    protected Hashtable UIResourceDBCache
    {
        get
        {
            if (uiResourceDBCache == null)
            {
                uiResourceDBCache = new Hashtable();
            }
            BuildHashTblCache();
            return uiResourceDBCache;
        }
    }

    void BuildHashTblCache()
    {
        uiResourceDBCache.Clear();
        foreach (var d in UIResourceDatabase)
        {
            if (string.IsNullOrEmpty(d.resourceLocator))
            {
                continue;
            }
            uiResourceDBCache.Add(d.resourceLocator, d);
        }
    }

    void Awake()
    {
#if !UNITY_EDITOR
        singleton = this;
#endif
        BuildHashTblCache();
    }

    private void Start()
    {
        Singleton.SetContent("Txt_WebSource", "运行网址：" + GetHref());
    }


    private void LateUpdate()
    {
        //Resources.UnloadUnusedAssets();
    }

    #region UI DBMS
    public string[] GetLocators()
    {
        List<string> locatorList = new List<string>();
        foreach (var r in UIResourceDatabase)
        {
            locatorList.Add(r.resourceLocator);
        }
        return locatorList.ToArray();
    }

    public void SetContent(string resourceLocator, string par)
    {
        if (!UIResourceDBCache.Contains(resourceLocator))
        {
            Debug.Log("Rersource with locator '" + resourceLocator + "' not found!");
            return;
        }

        UIResource resource = UIResourceDBCache[resourceLocator] as UIResource;
        if (resource.contentType != UIContentType.Text)
        {
            Debug.Log("Rersource content type '" + resourceLocator + "' not matched!");
            return;
        }

        resource.text = par;
    }

    public void SetContent(string resourceLocator, Sprite par)
    {
        if (!UIResourceDBCache.Contains(resourceLocator))
        {
            Debug.Log("Rersource with locator '" + resourceLocator + "' not found!");
            return;
        }

        UIResource resource = UIResourceDBCache[resourceLocator] as UIResource;
        if (resource.contentType != UIContentType.Image)
        {
            Debug.Log("Rersource content type '" + resourceLocator + "' not matched!");
            return;
        }

        resource.image = par;
    }

    public void SetContent(string resourceLocator, bool par)
    {
        if (!UIResourceDBCache.Contains(resourceLocator))
        {
            Debug.Log("Rersource with locator '" + resourceLocator + "' not found!");
            return;
        }

        UIResource resource = UIResourceDBCache[resourceLocator] as UIResource;
        if (resource.contentType != UIContentType.OnOffCommand)
        {
            Debug.Log("Rersource content type '" + resourceLocator + "' not matched!");
            return;
        }

        resource.onOffCommand = par;
    }

    public void SetContent(string resourceLocator, Texture2D par)
    {
        if (!UIResourceDBCache.Contains(resourceLocator))
        {
            Debug.Log("Rersource with locator '" + resourceLocator + "' not found!");
            return;
        }

        UIResource resource = UIResourceDBCache[resourceLocator] as UIResource;
        if (resource.contentType != UIContentType.Texture2D)
        {
            Debug.Log("Rersource content type '" + resourceLocator + "' not matched!");
            return;
        }

        resource.texture = par;
    }


    public string GetTextContent(string resourceLocator)
    {
        if (!UIResourceDBCache.Contains(resourceLocator))
        {
            Debug.Log("Rersource with locator '" + resourceLocator + "' not found!");
            return null;
        }

        UIResource resource = UIResourceDBCache[resourceLocator] as UIResource;
        if (resource.contentType != UIContentType.Text)
        {
            Debug.Log("Rersource content type '" + resourceLocator + "' not matched!");
            return null;
        }
        return resource.text;
    }

    public Sprite GetImageContent(string resourceLocator)
    {
        if (!UIResourceDBCache.Contains(resourceLocator))
        {
            Debug.Log("Rersource with locator '" + resourceLocator + "' not found!");
            return null;
        }

        UIResource resource = UIResourceDBCache[resourceLocator] as UIResource;
        if (resource.contentType != UIContentType.Image)
        {
            Debug.Log("Rersource content type '" + resourceLocator + "' not matched!");
            return null;
        }
        return resource.image;
    }

    public Texture2D GetTextureContent(string resourceLocator)
    {
        if (!UIResourceDBCache.Contains(resourceLocator))
        {
            Debug.Log("Rersource with locator '" + resourceLocator + "' not found!");
            return null;
        }

        UIResource resource = UIResourceDBCache[resourceLocator] as UIResource;
        if (resource.contentType != UIContentType.Texture2D)
        {
            Debug.Log("Rersource content type '" + resourceLocator + "' not matched!");
            return null;
        }
        return resource.texture;
    }

    public bool GetBoolContent(string resourceLocator)
    {
        if (!UIResourceDBCache.Contains(resourceLocator))
        {
            Debug.Log("Rersource with locator '" + resourceLocator + "' not found!");
            return false;
        }

        UIResource resource = UIResourceDBCache[resourceLocator] as UIResource;
        if (resource.contentType != UIContentType.OnOffCommand)
        {
            Debug.Log("Rersource content type '" + resourceLocator + "' not matched!");
            return false;
        }
        return resource.onOffCommand;
    }

    public UIResourceImage GetResourceImage(string resourceLocator)
    {
        if (!UIResourceDBCache.Contains(resourceLocator))
        {
            Debug.Log("Rersource with locator '" + resourceLocator + "' not found!");
            return null;
        }

        UIResource resource = UIResourceDBCache[resourceLocator] as UIResource;
        return new UIResourceImage(resource);
    }
    #endregion
}

/// <summary>
/// UI resource contents holder
/// </summary>
[System.Serializable]
public class UIResource
{
    public string resourceLocator;
    public UIContentType contentType = UIContentType.Text;

    public string text;
    public Sprite image;
    public Texture2D texture;
    public bool onOffCommand;

    public bool editorFolded = true;
}

/// <summary>
/// Read-only accessor to UI Resources
/// </summary>
public class UIResourceImage
{
    UIResource resource;
    public UIResourceImage(UIResource resource)
    {
        this.resource = resource;
    }

    public UIContentType ContentType
    {
        get
        {
            return resource.contentType;
        }
    }

    public string Text
    {
        get
        {
            return resource.text;
        }
    }

    public Sprite Image
    {
        get
        {
            return resource.image;
        }
    }

    public Texture2D texture
    {
        get
        {
            return resource.texture;
        }
    }

    public bool OnOffCommand
    {
        get
        {
            return resource.onOffCommand;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UISystem))]
public class UIServerDatabaseEditor : Editor
{
    protected UISystem Target
    {
        get
        {
            return target as UISystem;
        }
    }

    bool listFolded = true;

    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(listFolded ? "+" : "-", GUILayout.Height(15f), GUILayout.Width(18f)))
        {
            listFolded = !listFolded;
        }
        EditorGUILayout.LabelField("UIResourceDataBase");
        EditorGUILayout.EndHorizontal();

        List<UIResource> removeList = new List<UIResource>();
        if (!listFolded)
        {
            foreach (var r in Target.UIResourceDatabase)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("->", GUILayout.Width(20f));
                if (GUILayout.Button(r.editorFolded ? "+" : "-", GUILayout.Height(15f), GUILayout.Width(18f)))
                {
                    r.editorFolded = !r.editorFolded;
                }
                EditorGUILayout.LabelField(r.resourceLocator + "-------------------------------------------------------------------------------------------------------------");
                if (r.editorFolded)
                {
                    switch (r.contentType)
                    {
                        case UIContentType.Text:
                            r.text = EditorGUILayout.TextField(r.text);
                            break;
                        case UIContentType.Image:
                            r.image = (Sprite)EditorGUILayout.ObjectField(r.image, typeof(Sprite));
                            break;
                        case UIContentType.Texture2D:
                            r.texture = (Texture2D)EditorGUILayout.ObjectField(r.texture, typeof(Texture2D));
                            break;
                        case UIContentType.OnOffCommand:
                            r.onOffCommand = EditorGUILayout.Toggle(r.onOffCommand);
                            break;
                        default: break;
                    }
                }
                else
                {
                    if (GUILayout.Button("del", GUILayout.Height(15f), GUILayout.Width(48f)))
                    {
                        removeList.Add(r);
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (!r.editorFolded)
                {
                    r.resourceLocator = EditorGUILayout.TextField("Locator", r.resourceLocator);
                    r.contentType = (UIContentType)EditorGUILayout.EnumPopup("Content Type", r.contentType);
                    switch (r.contentType)
                    {
                        case UIContentType.Text:
                            r.text = EditorGUILayout.TextField("Text", r.text);
                            break;
                        case UIContentType.Image:
                            r.image = (Sprite)EditorGUILayout.ObjectField("Image", r.image, typeof(Sprite));
                            break;
                        case UIContentType.Texture2D:
                            r.texture = (Texture2D)EditorGUILayout.ObjectField(r.texture, typeof(Texture2D));
                            break;
                        case UIContentType.OnOffCommand:
                            r.onOffCommand = EditorGUILayout.Toggle("OnOff Cmd", r.onOffCommand);
                            break;
                        default: break;
                    }


                    EditorGUILayout.Space();
                }
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                var r = new UIResource();
                r.resourceLocator = "www...";
                r.contentType = UIContentType.Text;
                Target.UIResourceDatabase.Add(r);
            }
            EditorGUILayout.EndHorizontal();
        }

        foreach (var r in removeList)
        {
            Target.UIResourceDatabase.Remove(r);
        }
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
