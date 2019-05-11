using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ImageLoader : MonoBehaviour
{
    public string resourceLocator;

    public Image GetImage()
    {
        return GetComponent<Image>();
    }

    public bool HasImage
    {
        get
        {
            return image.sprite != null;
        }
    }

    Image image;
    // Use this for initialization
    void Start()
    {
        image = GetImage();
    }



    public void LoadResources()
    {
        if (GetImage().sprite != null)
            return;

        GetImage().sprite = Resources.Load<Sprite>(resourceLocator);
        resourceLocator = null;
    }

    public void UnloadResources()
    {
        if (GetImage().sprite == null)
            return;

        resourceLocator = GetImage().sprite.name;
        Resources.UnloadAsset(GetImage().sprite);

        //Resources.UnloadAsset(GetImage().sprite);
        GetImage().sprite = null;

        
    }

    #region Asset Loading
#if UNITY_EDITOR
    //[MenuItem("Optimize Tools/ImageLoader:Build Mapper")]
    public static void BuildImageMapper()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<ImageLoader> imageLoaders = new List<ImageLoader>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            imageLoaders.AddRange(go.GetComponentsInChildren<ImageLoader>(false));
        }

        foreach (var l in imageLoaders)
        {
            if (l.gameObject.activeInHierarchy == false)
                continue;
            l.UnloadResources();
        }

        EditorApplication.MarkSceneDirty();
        EditorApplication.SaveScene();
        AssetDatabase.SaveAssets();
    }

    //[MenuItem("Optimize Tools/ImageLoader:Cancel Mapper")]
    public static void CancelMapper()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<ImageLoader> imageLoaders = new List<ImageLoader>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            imageLoaders.AddRange(go.GetComponentsInChildren<ImageLoader>(false));
        }

        foreach (var l in imageLoaders)
        {
            if (l.gameObject.activeInHierarchy == false)
                continue;
            l.LoadResources();
        }

        EditorApplication.MarkSceneDirty();
        EditorApplication.SaveScene();
        AssetDatabase.SaveAssets();
    }
#endif
    #endregion
}
