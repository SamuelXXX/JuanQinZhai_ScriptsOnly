using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The handler of a special scene
/// </summary>
[System.Serializable]
public class SpecialScene : MonoBehaviour
{
    #region Settings and run-time data
    //basic settings
    public string specialSceneName;//name of this block,usually set as object's name
    public Transform sceneViewCenter;//the view center of this cell
    public CameraViewMode sceneViewMode;//the view mode of this special scene

    public ViewContentType sceneViewContentType = ViewContentType.Model3DView;
    public PanoramaBlock viewContentDelegation = null;
    public bool isIndoor = true;

    //position info,built automatically
    [HideInInspector]
    public BlockManager manager;
    #endregion

    #region Mono Events
    void OnValidate()
    {
        if (viewContentDelegation == null)
        {
            viewContentDelegation = GetComponentInChildren<PanoramaBlock>();
        }

        if (viewContentDelegation)
        {
            sceneViewCenter = viewContentDelegation.transform;
            sceneViewContentType = ViewContentType.PanoramaView;
        }

        if (sceneViewCenter == null)
            sceneViewCenter = transform;
        if (string.IsNullOrEmpty(specialSceneName))
        {
            specialSceneName = name;
        }
    }
    #endregion

    public void OnEnterSpecialScene()
    {
        LoadAllMaterial();
    }

    public void OnExitSpecialScene()
    {
        StartCoroutine("DelayUnloadAsset");
    }

    IEnumerator DelayUnloadAsset()
    {
        yield return new WaitForSeconds(2f);
        UnloadAllMaterial();
    }

    #region Material Dynamic Loader
    [System.Serializable]
    public class MaterialMapper
    {
        public MeshRenderer targetRenderer;
        public string resourceLocator;
    }

    public List<MaterialMapper> subMatMappers = new List<MaterialMapper>();


    void LoadAllMaterial()
    {
        if (sceneViewContentType != ViewContentType.Model3DView)
            return;

        foreach (var i in subMatMappers)
        {
            if (i.targetRenderer.sharedMaterial != null)
                continue;
            i.targetRenderer.sharedMaterial = Resources.Load<Material>(i.resourceLocator);
        }
    }

    void UnloadAllMaterial()
    {
        if (sceneViewContentType != ViewContentType.Model3DView)
            return;

        foreach (var i in subMatMappers)
        {
            if (i.targetRenderer.sharedMaterial == null)
                continue;
            var s = i.targetRenderer.sharedMaterial;
            
            i.targetRenderer.sharedMaterial = null;
            Resources.UnloadAsset(s);
        }
        
    }


    void BuildMaterialMapper()
    {
        if (subMatMappers.Count != 0)
        {
            Debug.Log(name + ":Has Material Content, Cannot rebuild,please kill imageMapper first");
            return;
        }

        foreach (var i in GetComponentsInChildren<MeshRenderer>())
        {
            if (i.sharedMaterial != null)
            {
                //sprite is in resources
                if (Resources.Load<Material>(i.sharedMaterial.name) == i.sharedMaterial)
                {
                    var mapper = new MaterialMapper();
                    mapper.targetRenderer = i;
                    mapper.resourceLocator = i.sharedMaterial.name;
                    subMatMappers.Add(mapper);
                    i.sharedMaterial = null;
                }
            }
        }
    }

    void KillMaterialMapper()
    {
        foreach (var i in subMatMappers)
        {
            i.targetRenderer.sharedMaterial = Resources.Load<Material>(i.resourceLocator);
        }

        subMatMappers.Clear();
    }

#if UNITY_EDITOR
    //[MenuItem("Optimize Tools/UI:Build Sub ImageManager")]
    public static void BuildSubMaterialManager()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<SpecialScene> contentSwitcher = new List<SpecialScene>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            contentSwitcher.AddRange(go.GetComponentsInChildren<SpecialScene>(false));
        }

        contentSwitcher.RemoveAll(p => p.sceneViewContentType != ViewContentType.Model3DView);
        foreach (var s in contentSwitcher)
        {
            s.BuildMaterialMapper();
        }

        Debug.Log(contentSwitcher.Count + " Switcher");

        Resources.UnloadUnusedAssets();
    }

    //[MenuItem("Optimize Tools/UI:Kill Sub ImageManager")]
    public static void KillSubMaterialManager()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<SpecialScene> contentSwitcher = new List<SpecialScene>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            contentSwitcher.AddRange(go.GetComponentsInChildren<SpecialScene>(false));
        }

        contentSwitcher.RemoveAll(p => p.sceneViewContentType != ViewContentType.Model3DView);
        foreach (var s in contentSwitcher)
        {
            s.KillMaterialMapper();
        }

        Resources.UnloadUnusedAssets();
    }
#endif
    #endregion
}
