using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class AssetBundleDownloadingManager : MonoBehaviour
{
    public enum AssetBundleBuiltStatus
    {
        NotBuilt = 0,
        BundleCreated,
        CleanBuilt
    }

    #region Singlton in runtime
    protected static AssetBundleDownloadingManager singleton;
    public static AssetBundleDownloadingManager Singleton
    {
        get
        {
            return singleton;
        }
    }
    #endregion

    public List<AssetBundleAgent> assetBundleAgents = new List<AssetBundleAgent>();
    public AssetBundleBuiltStatus buildStatus = AssetBundleBuiltStatus.NotBuilt;
    public bool allowBreakAssetsLink = false;

    [Header("Run time data")]


    public List<AssetBundleDownloadingHandler> downloadingHandlers = new List<AssetBundleDownloadingHandler>();

    protected Dictionary<string, AssetBundleDownloadingHandler> handlersCache = new Dictionary<string, AssetBundleDownloadingHandler>();

    public enum AssetBundleDownloadingHandleStatus
    {
        WaitingDownloading,
        Downloading,
        Downloaded,
        AssetLoaded,
        Unloaded
    }

    [System.Serializable]
    public class AssetBundleDownloadingHandler
    {
        public string bundleName;
        public AssetBundleType type;
        public AssetBundleDownloadingHandleStatus status;
        public List<AssetBundleAgent> receivers = new List<AssetBundleAgent>();


        public AssetBundle bundle = null;

        public void UnloadAssetBundle()
        {
            if (bundle != null)
            {
                Resources.UnloadAsset(bundle);
                bundle = null;
                status = AssetBundleDownloadingHandleStatus.Unloaded;
            }
        }

        public void LoadAssetBundle()
        {
            if (bundle != null)
            {
                bundle.LoadAllAssets();
                status = AssetBundleDownloadingHandleStatus.AssetLoaded;
            }
        }

    }

    private void Awake()
    {
        if (singleton == null)
            singleton = this;
    }
    // Use this for initialization
    void Start()
    {
        StartCoroutine(DownloadingRoutine());
    }

    // Update is called once per frame
    void Update()
    {

    }



    IEnumerator DownloadingRoutine()
    {
        foreach (var dh in downloadingHandlers)
        {
            if (dh == null)
                continue;

            string path = dh.receivers[0].GetOwnBundlePath();
            WWW bundleRequest = new WWW(path);
            dh.status = AssetBundleDownloadingHandleStatus.Downloading;
            yield return bundleRequest;
            dh.status = AssetBundleDownloadingHandleStatus.Downloaded;
            dh.bundle = bundleRequest.assetBundle;

            //image.bundle.LoadAllAssets();
            dh.LoadAssetBundle();
            foreach (var a in dh.receivers)
            {
                a.OnBundleDownloadingFinished(dh.bundle);
                yield return null;
            }

            dh.UnloadAssetBundle();
            yield return null;
        }
    }


#if UNITY_EDITOR
    public void BuildAgency()
    {
        var scene = gameObject.scene;
        assetBundleAgents.Clear();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            assetBundleAgents.AddRange(go.GetComponentsInChildren<AssetBundleAgent>(false));
        }

        BuildDownloadingHandlers();
    }

    public void BuildDownloadingHandlers()
    {
        foreach (var a in assetBundleAgents)
        {
            if (!handlersCache.ContainsKey(a.BundleName))
            {
                var dh = new AssetBundleDownloadingHandler();
                dh.bundleName = a.BundleName;
                dh.receivers.Add(a);
                dh.status = AssetBundleDownloadingHandleStatus.WaitingDownloading;
                dh.type = a.BundleType;
                dh.bundle = null;
                handlersCache.Add(a.BundleName, dh);
            }
            else
            {
                handlersCache[a.BundleName].receivers.Add(a);
            }
        }

        downloadingHandlers.Clear();

        foreach (var k in handlersCache.Keys)
        {
            downloadingHandlers.Add(handlersCache[k]);
        }
        handlersCache.Clear();
    }
#endif
}
