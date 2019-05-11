using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum AssetBundleType
{
    None = 0,
    MaterialTexturePair,
    UISprite
}

[DisallowMultipleComponent]
public class AssetBundleAgent : MonoBehaviour
{
    #region Settings
    [SerializeField]
    AssetBundleType bundleType = AssetBundleType.None;
    [SerializeField]
    string bundleName = "";
    [SerializeField]
    string mainAssetName;
    [SerializeField]
    string relatedAssetName;

    public string MainAssetName
    {
        get
        {
            return mainAssetName;
        }
    }

    public string BundleName
    {
        get
        {
            return bundleName;
        }
    }

    public AssetBundleType BundleType
    {
        get
        {
            return bundleType;
        }
    }

    [Header("Back-Up Data")]
    [SerializeField]
    Object backupDirectRelatedAsset;
    [SerializeField]
    Shader materialShader;

    #endregion

    #region Asset Bundle Path Management
    const string AssetBundleBuildOutputPath = "_AssetBundleOutput";
    /// <summary>
    /// Get href from javascript codes
    /// </summary>
    /// <returns></returns>
    [DllImport("__Internal")]
    private static extern string GetHref();
#if UNITY_EDITOR
    static string GetBundlePathFromEditor()
    {
        var str = AssetBundleBuildOutputPath + "/" + SceneManager.GetActiveScene().name;
        return str;
    }
#endif

    static string GetSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    static string GetAssetBundleLoadingPath()
    {
#if UNITY_EDITOR      
        return GetBundlePathFromEditor();
#else
        var hrefStr = GetHref();
        hrefStr.Replace("index.html", "");
        hrefStr += AssetBundleBuildOutputPath;
        hrefStr += "/" + GetSceneName();
        return hrefStr;
#endif
    }

    public string GetOwnBundlePath()
    {
        
#if UNITY_EDITOR
        return "http://localhost/" + AssetBundleBuildOutputPath + "/" + GetSceneName() + "/" + bundleName;
#else
        return GetAssetBundleLoadingPath() + "/" + bundleName;
#endif
    }
    #endregion

    #region Mono Events
    // Use this for initialization
    void Start()
    {
    }

    public void OnBundleDownloadingFinished(AssetBundle bundle)
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Image image = GetComponent<Image>();
        switch (bundleType)
        {
            case AssetBundleType.MaterialTexturePair:
                if (renderer != null)
                {
                    renderer.material = bundle.LoadAsset<Material>(relatedAssetName);
                    renderer.material.shader = materialShader;
                }
                break;
            case AssetBundleType.UISprite:
                if (image != null)
                {
                    image.sprite = bundle.LoadAsset<Sprite>(relatedAssetName);
                }
                break;
            default:
                break;
        }
    }
    #endregion

    #region Building Tools
#if UNITY_EDITOR
    static int bundleBuildIndex = 0;
    static List<BundleDescriptor> bundleDescriptors = new List<BundleDescriptor>();
    public class BundleDescriptor
    {
        public string bundleName;
        public Object mainAsset;//the texture of material or sprite asset
        public List<Object> relatedAssets = new List<Object>();//other related assets,mainly for material
    }

    BundleDescriptor SearchBundleDescriptor(Object asset)
    {
        return bundleDescriptors.Find(a => { return a.mainAsset == asset; });
    }

    //[MenuItem("Optimize Tools/AssetBundle:Start Building")]
    static void BuildAssetBundles()
    {
        var path = GetAssetBundleLoadingPath();
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        //Get downloading manager
        Scene scene = SceneManager.GetActiveScene();
        AssetBundleDownloadingManager manager = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            manager = go.GetComponentInChildren<AssetBundleDownloadingManager>();
            if (manager != null)
                break;
        }

        if (manager == null)
        {
            Debug.Log("No Downloading Manager found in current scene");
            return;
        }

        if (manager.buildStatus != AssetBundleDownloadingManager.AssetBundleBuiltStatus.NotBuilt)
        {
            Debug.Log("Has been built before");
            return;
        }


        //prepare downloading manager data
        manager.BuildAgency();
        bundleDescriptors.Clear();
        bundleBuildIndex = 0;

        List<AssetBundleBuild> buildMaps = new List<AssetBundleBuild>();
        foreach (var a in manager.assetBundleAgents)
        {
            a.BuildBundle();
        }

        foreach (var b in bundleDescriptors)
        {
            AssetBundleBuild buildMap = new AssetBundleBuild();
            buildMap.assetBundleName = b.bundleName;
            List<string> assetNames = new List<string>();
            assetNames.Add(AssetDatabase.GetAssetPath(b.mainAsset));
            foreach (var r in b.relatedAssets)
            {
                assetNames.Add(AssetDatabase.GetAssetPath(r));
            }
            buildMap.assetNames = assetNames.ToArray();
            buildMaps.Add(buildMap);
        }

        bundleDescriptors.Clear();

        BuildPipeline.BuildAssetBundles(path, buildMaps.ToArray(), BuildAssetBundleOptions.DeterministicAssetBundle, BuildTarget.WebGL);
        manager.buildStatus = AssetBundleDownloadingManager.AssetBundleBuiltStatus.BundleCreated;
        EditorApplication.MarkSceneDirty();
        EditorApplication.SaveScene();
        AssetDatabase.SaveAssets();
    }

    //[MenuItem("Optimize Tools/AssetBundle:Cancel Building")]
    static void CancelAssetBundlesBuild()
    {
        //Get downloading manager
        Scene scene = SceneManager.GetActiveScene();
        AssetBundleDownloadingManager manager = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            manager = go.GetComponentInChildren<AssetBundleDownloadingManager>();
            if (manager != null)
                break;
        }

        if (manager == null)
        {
            Debug.Log("No Downloading Manager found in current scene");
            return;
        }

        if (manager.buildStatus != AssetBundleDownloadingManager.AssetBundleBuiltStatus.BundleCreated)
        {
            Debug.Log("Not in right state to cancel built");
            return;
        }

        foreach (var a in manager.assetBundleAgents)
        {
            if (a == null)
                continue;
            a.CancelBundle();
        }

        var path = GetAssetBundleLoadingPath();
        DeleteDirectory(path);

        manager.assetBundleAgents.Clear();
        manager.buildStatus = AssetBundleDownloadingManager.AssetBundleBuiltStatus.NotBuilt;
        EditorApplication.MarkSceneDirty();
        EditorApplication.SaveScene();
        AssetDatabase.SaveAssets();
    }

    //[MenuItem("Optimize Tools/AssetBundle:Finish Building")]
    static void BreakAssetLink()
    {
        //Get downloading manager
        Scene scene = SceneManager.GetActiveScene();
        AssetBundleDownloadingManager manager = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            manager = go.GetComponentInChildren<AssetBundleDownloadingManager>();
            if (manager != null)
                break;
        }

        if (manager == null)
        {
            Debug.Log("No Downloading Manager found in current scene");
            return;
        }

        if (manager.buildStatus != AssetBundleDownloadingManager.AssetBundleBuiltStatus.BundleCreated)
        {
            Debug.Log("Not in right state to break asset link");
            return;
        }

        if (!manager.allowBreakAssetsLink)
        {
            Debug.Log("Are you certain!!!");
            return;
        }

        manager.allowBreakAssetsLink = false;

        foreach (var a in manager.assetBundleAgents)
        {
            if (a == null)
                continue;
            a.backupDirectRelatedAsset = null;
        }

        manager.buildStatus = AssetBundleDownloadingManager.AssetBundleBuiltStatus.CleanBuilt;
        EditorApplication.MarkSceneDirty();
        EditorApplication.SaveScene();
        AssetDatabase.SaveAssets();
    }

    static void DeleteDirectory(string path)
    {
        DirectoryInfo dir = new DirectoryInfo(path);
        if (dir.Exists)
        {
            DirectoryInfo[] childs = dir.GetDirectories();
            foreach (DirectoryInfo child in childs)
            {
                child.Delete(true);
            }
            dir.Delete(true);
        }
    }

    /// <summary>
    /// Build bundle for this gameobject texture or material
    /// </summary>
    /// <returns></returns>
    void BuildBundle()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Image image = GetComponent<Image>();

        //prepare direct related asset
        if (renderer != null)
        {
            bundleType = AssetBundleType.MaterialTexturePair;
            backupDirectRelatedAsset = renderer.sharedMaterial;
            renderer.sharedMaterial = Resources.Load<Material>("DefaultMaterial");
        }
        else if (image != null && image.sprite != null)
        {
            bundleType = AssetBundleType.UISprite;
            backupDirectRelatedAsset = image.sprite;
            image.sprite = null;
        }
        else
        {
            bundleType = AssetBundleType.None;
            return;
        }
        relatedAssetName = backupDirectRelatedAsset.name;

        //Get main asset name
        Object mainAsset = null;
        switch (bundleType)
        {
            case AssetBundleType.MaterialTexturePair:
                mainAsset = (backupDirectRelatedAsset as Material).mainTexture;
                break;
            case AssetBundleType.UISprite:
                mainAsset = backupDirectRelatedAsset;
                break;
            default:
                break;
        }

        if (mainAsset == null)
            return;


        var bundleDescriptor = SearchBundleDescriptor(mainAsset);
        mainAssetName = mainAsset.name;

        //Has been marked as bundle before
        if (bundleDescriptor != null)
        {
            bundleName = bundleDescriptor.bundleName;
            if (bundleType == AssetBundleType.MaterialTexturePair)
                materialShader = (bundleDescriptor.relatedAssets[0] as Material).shader;

            bundleDescriptor.relatedAssets.Add(backupDirectRelatedAsset);
            return;
        }
        else
        {
            // Create the array of bundle build details.
            List<string> assetNames = new List<string>();

            switch (bundleType)
            {
                case AssetBundleType.MaterialTexturePair:
                    bundleName = "materialtexturepair-" + mainAssetName + "-" + bundleBuildIndex.ToString();
                    assetNames.Add(AssetDatabase.GetAssetPath(backupDirectRelatedAsset));
                    materialShader = (backupDirectRelatedAsset as Material).shader;
                    break;
                case AssetBundleType.UISprite:
                    bundleName = "uisprite-" + mainAssetName + "-" + bundleBuildIndex.ToString();
                    assetNames.Add(AssetDatabase.GetAssetPath(backupDirectRelatedAsset));
                    break;
                default:
                    break;
            }

            bundleDescriptor = new BundleDescriptor();
            bundleDescriptor.bundleName = bundleName;
            bundleDescriptor.mainAsset = mainAsset;
            bundleDescriptor.relatedAssets.Add(backupDirectRelatedAsset);
            bundleDescriptors.Add(bundleDescriptor);
            bundleBuildIndex++;
        }
    }

    void CancelBundle()
    {
        bundleName = "";
        mainAssetName = "";
        relatedAssetName = "";
        materialShader = null;
        if (backupDirectRelatedAsset == null)
        {
            bundleType = AssetBundleType.None;
            return;
        }

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Image image = GetComponent<Image>();

        switch (bundleType)
        {
            case AssetBundleType.MaterialTexturePair:
                if (renderer != null)
                {
                    renderer.sharedMaterial = backupDirectRelatedAsset as Material;
                }
                break;
            case AssetBundleType.UISprite:
                if (image != null)
                {
                    image.sprite = backupDirectRelatedAsset as Sprite;
                }
                break;
            default:
                break;
        }

        backupDirectRelatedAsset = null;
        bundleType = AssetBundleType.None;
    }
#endif
    #endregion
}
