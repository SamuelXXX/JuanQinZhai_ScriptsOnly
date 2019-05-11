using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PanoramaBlock : MonoBehaviour
{
    //[DllImport("__Internal")]
    //private static extern string GetHref();
    public enum BuildingStatus
    {
        NotBuilding = 0,
        Built,
        CleanBuilt
    }

    #region Definations
    [System.Serializable]
    public class PanoramaSide
    {
        public MeshRenderer renderer;
        [SerializeField]
        public Object mainAsset;
        public string mainAssetName;
        public bool hasTexture = true;

        public Material material
        {
            get
            {
                if (renderer)
                {
                    return renderer.sharedMaterial;
                }
                return null;
            }
            set
            {
                if (renderer)
                {
                    renderer.sharedMaterial = value;
                }
            }
        }

        public void PrepareBundleData()
        {
            if (mainAsset != null)
                return;

            if (renderer == null)
                return;

            if (renderer.sharedMaterial == null)
                return;

            mainAsset = renderer.sharedMaterial;

            if (mainAsset == null)
            {
                hasTexture = false;
                return;
            }

            if (((Material)mainAsset).GetTexture("_MainTex") == null)
            {
                hasTexture = false;
                mainAsset = null;
                return;
            }

            mainAssetName = mainAsset.name;
            renderer.sharedMaterial = Resources.Load<Material>("Empty");

        }

        public void RecoverPanorama()
        {
            if (mainAsset == null)
                return;

            if (renderer == null)
                return;


            //prepare direct related asset
            if (renderer != null)
            {
                renderer.sharedMaterial = (Material)mainAsset;
            }

            mainAsset = null;
            mainAssetName = null;
        }

        public void BreakAssetReference()
        {
            mainAsset = null;
        }

        public void LoadAsset(AssetBundle bundle)
        {
            if (bundle == null)
                return;

            if (!string.IsNullOrEmpty(mainAssetName))
            {
                Material material = (Material)bundle.LoadAssetAsync<Material>(mainAssetName).asset;
                renderer.sharedMaterial = material;
            }
        }

        public void UnloadAsset(AssetBundle bundle)
        {
            //Resources.UnloadAsset(renderer.material);
            //DestroyImmediate(renderer.sharedMaterial, true);
            renderer.material = Resources.Load<Material>("Empty");
            renderer.material.SetTexture("_MainTex", Resources.Load<Texture>(mainAssetName));
        }
    }
    #endregion

    #region Settings
    [Header("Six Sides")]
    public PanoramaSide front;
    public PanoramaSide back;
    public PanoramaSide left;
    public PanoramaSide right;
    public PanoramaSide up;
    public PanoramaSide down;

    public MeshRenderer replacableContent;

    [Header("Other Settings")]
    public Transform dualDebugCamera;
    public float viewingScaleSize = 3;
    public bool allowDynamicUpdating = false;

    [Header("Asset Bundles Settings")]
    public string PanoramaAssetBundleName = "";
    public BuildingStatus buildingStatus = BuildingStatus.NotBuilding;
    #endregion

    #region DownloadingPath Management
    const string PanoramaAssetBundleOutputPath = "_PanoramaAssetBundle";
    const string PanoramaAssetBundleNamePrefix = "panorama";


    /// <summary>
    /// Get href from javascript codes
    /// </summary>
    /// <returns></returns>
    [DllImport("__Internal")]
    private static extern string GetHref();
#if UNITY_EDITOR
    static string GetBundlePathFromEditor()
    {
        var str = PanoramaAssetBundleOutputPath + "/" + SceneManager.GetActiveScene().name;
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
        hrefStr=hrefStr.Replace("/index.html", "/");
        hrefStr += PanoramaAssetBundleOutputPath;
        hrefStr += "/" + GetSceneName();
        return hrefStr;
#endif
    }

    public string GetOwnBundlePath()
    {

#if UNITY_EDITOR
        return "http://www.shaderealm.com/test/JuanQinZhai/" + PanoramaAssetBundleOutputPath + "/" + GetSceneName() + "/" + PanoramaAssetBundleName;
#else
        return GetAssetBundleLoadingPath() + "/" + PanoramaAssetBundleName;
        //return "http://www.shaderealm.com/test/JuanQinZhai/" + PanoramaAssetBundleOutputPath + "/" + GetSceneName() + "/" + PanoramaAssetBundleName;
#endif
    }
    #endregion

    #region Asset Loading
    public bool hasMaterials = false;
    static int PanoramaIndex = 0;

#if UNITY_EDITOR
    [MenuItem("Optimize Tools/Panorama:Build Mapper")]
    static void BuildPanoramaMapper()
    {
        var path = GetAssetBundleLoadingPath();
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<PanoramaBlock> panoramaBlocks = new List<PanoramaBlock>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            panoramaBlocks.AddRange(go.GetComponentsInChildren<PanoramaBlock>(false));
        }

        List<AssetBundleBuild> buildMaps = new List<AssetBundleBuild>();
        PanoramaIndex = 0;

        foreach (var mc in panoramaBlocks)
        {
            if (mc.gameObject.activeInHierarchy == false)
                continue;
            var c = mc.BuildBundle();
            buildMaps.Add(c);
            PanoramaIndex++;
        }


        BuildPipeline.BuildAssetBundles(path, buildMaps.ToArray(), BuildAssetBundleOptions.DeterministicAssetBundle, BuildTarget.WebGL);

        EditorApplication.MarkSceneDirty();
        EditorApplication.SaveScene();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Optimize Tools/Panorama:Cancel Mapper")]
    static void CancelMapper()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<PanoramaBlock> panoramaBlocks = new List<PanoramaBlock>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            panoramaBlocks.AddRange(go.GetComponentsInChildren<PanoramaBlock>(false));
        }

        foreach (var mc in panoramaBlocks)
        {
            if (mc.gameObject.activeInHierarchy == false)
                continue;
            mc.CancelBundle();
        }

        var path = GetAssetBundleLoadingPath();
        DeleteDirectory(path);

        EditorApplication.MarkSceneDirty();
        EditorApplication.SaveScene();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Optimize Tools/Panorama:Clean Built")]
    static void CleanBuilt()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<PanoramaBlock> panoramaBlocks = new List<PanoramaBlock>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            panoramaBlocks.AddRange(go.GetComponentsInChildren<PanoramaBlock>(false));
        }

        foreach (var mc in panoramaBlocks)
        {
            if (mc.gameObject.activeInHierarchy == false)
                continue;
            mc.BreakLink();
        }

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

    void BreakLink()
    {
        if (buildingStatus != BuildingStatus.Built)
            return;
        front.BreakAssetReference();
        back.BreakAssetReference();
        up.BreakAssetReference();
        down.BreakAssetReference();
        left.BreakAssetReference();
        right.BreakAssetReference();
        buildingStatus = BuildingStatus.CleanBuilt;
    }

    void CancelBundle()
    {
        if (buildingStatus != BuildingStatus.Built)
            return;
        front.RecoverPanorama();
        back.RecoverPanorama();
        up.RecoverPanorama();
        down.RecoverPanorama();
        left.RecoverPanorama();
        right.RecoverPanorama();
        hasMaterials = true;
        buildingStatus = BuildingStatus.NotBuilding;
    }

    AssetBundleBuild BuildBundle()
    {
        if (buildingStatus != BuildingStatus.NotBuilding)
        {
            var c = new AssetBundleBuild();
            c.assetBundleName = null;
            c.assetNames = null;
            return c;
        }

        front.PrepareBundleData();
        back.PrepareBundleData();
        up.PrepareBundleData();
        down.PrepareBundleData();
        left.PrepareBundleData();
        right.PrepareBundleData();

        PanoramaAssetBundleName = PanoramaAssetBundleNamePrefix + PanoramaIndex.ToString();

        AssetBundleBuild buildMap = new AssetBundleBuild();
        buildMap.assetBundleName = PanoramaAssetBundleName;
        List<string> assetNames = new List<string>();

        if (front.mainAsset)
            assetNames.Add(AssetDatabase.GetAssetPath(front.mainAsset));
        if (back.mainAsset)
            assetNames.Add(AssetDatabase.GetAssetPath(back.mainAsset));
        if (up.mainAsset)
            assetNames.Add(AssetDatabase.GetAssetPath(up.mainAsset));
        if (down.mainAsset)
            assetNames.Add(AssetDatabase.GetAssetPath(down.mainAsset));
        if (left.mainAsset)
            assetNames.Add(AssetDatabase.GetAssetPath(left.mainAsset));
        if (right.mainAsset)
            assetNames.Add(AssetDatabase.GetAssetPath(right.mainAsset));

        buildMap.assetNames = assetNames.ToArray();
        hasMaterials = false;
        buildingStatus = BuildingStatus.Built;
        return buildMap;
    }
#endif
    #endregion


    #region Mono Events
    private void Start()
    {
        front.UnloadAsset(m_AssetBundle);
        back.UnloadAsset(m_AssetBundle);
        up.UnloadAsset(m_AssetBundle);
        down.UnloadAsset(m_AssetBundle);
        left.UnloadAsset(m_AssetBundle);
        right.UnloadAsset(m_AssetBundle);
    }
    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(CameraCharacter.Singleton.ownedCamera.transform.position, transform.position) < 0.05f)
        {
            if (!isViewing)
            {
                isViewing = true;
                OnEnterPanorama();
            }
        }
        else
        {
            if (isViewing)
            {
                isViewing = false;
                OnExitPanorama();
            }
        }
    }


    #endregion

    #region Asset Bundle Handling
    public bool bundleDownloaded = false;
    public AssetBundle m_AssetBundle = null;
    public void DownloadAssetBundle()
    {
        StartCoroutine("DownloadingRoutine");
    }

    void UnloadAssetBundle()
    {
        StopAllCoroutines();
        if (!bundleDownloaded)
        {
            return;
        }

        front.UnloadAsset(m_AssetBundle);
        back.UnloadAsset(m_AssetBundle);
        up.UnloadAsset(m_AssetBundle);
        down.UnloadAsset(m_AssetBundle);
        left.UnloadAsset(m_AssetBundle);
        right.UnloadAsset(m_AssetBundle);

        if (m_AssetBundle != null)
            m_AssetBundle.Unload(true);
        m_AssetBundle = null;
        bundleDownloaded = false;
    }


    IEnumerator DownloadingRoutine()
    {
        string path = GetOwnBundlePath();
        WWW bundleRequest = new WWW(path);
        yield return bundleRequest;
        m_AssetBundle = bundleRequest.assetBundle;
        if (m_AssetBundle)
        {
            m_AssetBundle.LoadAllAssets();
            front.LoadAsset(m_AssetBundle);
            back.LoadAsset(m_AssetBundle);
            up.LoadAsset(m_AssetBundle);
            down.LoadAsset(m_AssetBundle);
            left.LoadAsset(m_AssetBundle);
            right.LoadAsset(m_AssetBundle);
            bundleDownloaded = true;
        }
    }
    #endregion

    public bool PanoramaContentReady()
    {
        //return bundleDownloaded;
        return true;
    }

    bool isViewing = true;

    public void OnEnterPanorama()
    {
        transform.localScale = new Vector3(viewingScaleSize, viewingScaleSize, viewingScaleSize);
        ShowContent();
    }

    public void OnExitPanorama()
    {
        transform.localScale = Vector3.one;
        UnloadAssetBundle();
        HideContent();
    }

    void HideContent()
    {
        if (front.renderer != null)
            front.renderer.enabled = false;

        if (back.renderer != null)
            back.renderer.enabled = false;

        if (left.renderer != null)
            left.renderer.enabled = false;

        if (right.renderer != null)
            right.renderer.enabled = false;

        if (up.renderer != null)
            up.renderer.enabled = false;

        if (down.renderer != null)
            down.renderer.enabled = false;

        if (replacableContent != null)
            replacableContent.enabled = false;
    }

    void ShowContent()
    {
        if (front.renderer != null)
            front.renderer.enabled = true;

        if (back.renderer != null)
            back.renderer.enabled = true;

        if (left.renderer != null)
            left.renderer.enabled = true;

        if (right.renderer != null)
            right.renderer.enabled = true;

        if (up.renderer != null)
            up.renderer.enabled = true;

        if (down.renderer != null)
            down.renderer.enabled = true;

        if (replacableContent != null)
            replacableContent.enabled = true;
    }
}
//#if UNITY_EDITOR
//[CustomEditor(typeof(PanoramaBlock))]
//public class PanoramaBoxEditor : Editor
//{
//    public PanoramaBlock Target
//    {
//        get
//        {
//            return target as PanoramaBlock;
//        }
//    }

//    public override void OnInspectorGUI()
//    {
//        base.OnInspectorGUI();

//        EditorGUILayout.Space();
//        EditorGUILayout.LabelField("Panorama Asset Downloading Path:");
//        if (GlobalGameSettings.Instance == null)
//        {
//            EditorGUILayout.LabelField("No game settings asset found!");
//        }
//        else
//        {
//            if (string.IsNullOrEmpty(GlobalGameSettings.Instance.panoramaDownloadPath))
//                EditorGUILayout.LabelField("No downloading path assigned");
//            else
//                EditorGUILayout.LabelField(GlobalGameSettings.Instance.panoramaDownloadPath);
//        }

//        if (GUILayout.Button("Open Game Settings"))
//        {
//            Selection.activeObject = GlobalGameSettings.Instance;
//        }
//    }
//}
//#endif