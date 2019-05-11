using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class MeshOptimizer : MonoBehaviour
{
    public int combineTextureSize = 1024;
    const int giantTextureSize = 1024;

    public static bool allDynamicCombinerFinished
    {
        get
        {
#if !STATIC_OPTIMIZE
            return unfinishedCombinerCount <= 0;
#else
            return true;
#endif
        }
    }

#if !STATIC_OPTIMIZE
    public int maxUVProcessingPerFrame = 100;
    static int unfinishedCombinerCount = 0;
#else

    [SerializeField, HideInInspector]
    int index = -1;

    [SerializeField, HideInInspector]
    MeshBuildingStatus buildingStatus = MeshBuildingStatus.NotBuilt;
    const string buildPath = "_MeshCombiningOutput";
    const string generatedAssetLabel = "generated";
    static string RelativeTexturePath
    {
        get
        {
            return RelativeSceneAssetDir + "/Textures/";
        }
    }
    static string AbsoluteTexturePath
    {
        get
        {
            return AbsoluteSceneAssetDir + "/Textures/";
        }
    }
    static string RelativeMeshPath
    {
        get
        {
            return RelativeSceneAssetDir + "/Meshes/";
        }
    }
    static string AbsoluteMeshPath
    {
        get
        {
            return AbsoluteSceneAssetDir + "/Meshes/";
        }
    }
    static string RelativeMaterialPath
    {
        get
        {
            return RelativeSceneAssetDir + "/Materials/";
        }
    }
    static string AbsoluteMaterialPath
    {
        get
        {
            return AbsoluteSceneAssetDir + "/Materials/";
        }
    }

    static string RelativeSceneAssetDir
    {
        get
        {
            return "Assets/" + buildPath + "/" + CurrentSceneName;
        }
    }

    static string CurrentSceneName
    {
        get
        {
            return SceneManager.GetActiveScene().name;
        }
    }

    static string AbsoluteSceneAssetDir
    {
        get
        {
            return Application.dataPath + "/" + buildPath + "/" + CurrentSceneName;
        }
    }

#if UNITY_EDITOR
    static SceneOptimizeManifest combinersManifest
    {
        get
        {
            var path = "Assets/" + buildPath + "/" + SceneManager.GetActiveScene().name + "/" + "SceneOptimizeManifest.asset";
            SceneOptimizeManifest manifest = AssetDatabase.LoadAssetAtPath<SceneOptimizeManifest>(path);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<SceneOptimizeManifest>();
                PrepareBuildingPath();
                AssetDatabase.CreateAsset(manifest, path);
            }
            return manifest;
        }
    }
#endif
#endif

#if STATIC_OPTIMIZE
    [SerializeField, HideInInspector]
#endif
    //Create renderer and filter components
    MeshRenderer generatedRenderer = null;
#if STATIC_OPTIMIZE
    [SerializeField, HideInInspector]
#endif
    MeshFilter generatedFilter = null;

#if STATIC_OPTIMIZE
    [SerializeField, HideInInspector]
#endif
    //Very important data,
    Material generatedMaterial = null;
#if STATIC_OPTIMIZE
    [SerializeField, HideInInspector]
#endif
    Texture2D localCombineClampTexture = null;
#if STATIC_OPTIMIZE
    [SerializeField, HideInInspector]
#endif
    Rect[] localCombineTexRects = null;

#if !STATIC_OPTIMIZE
    // Use this for initialization
    void Start()
    {
        unfinishedCombinerCount++;
        //preparing cooking ingredients
        PreparingMeshTarget();
        StartCoroutine(CombiningRoutine());
    }
#elif UNITY_EDITOR
    //[MenuItem("Optimize Tools/Build Combining Asset")]
    static void BuildCombiningAsset()
    {
        if (combinersManifest.buildingStatus != MeshBuildingStatus.NotBuilt)
        {
            Debug.Log("This scene has been built already");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();
        PrepareBuildingPath();
        int indexPool = 0;

        List<MeshOptimizer> meshCombiners = new List<MeshOptimizer>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            var mc = go.GetComponent<MeshOptimizer>();
            meshCombiners.AddRange(go.GetComponentsInChildren<MeshOptimizer>(false));
        }

        foreach (var mc in meshCombiners)
        {
            if (mc.gameObject.activeInHierarchy == false)
                continue;
            mc.index = indexPool;
            //prepare processing targer
            mc.PreparingMeshTarget();
            //process combine
            mc.CombiningProcess();

            indexPool++;
        }
        combinersManifest.buildingStatus = MeshBuildingStatus.AssetCreated;
        EditorUtility.SetDirty(combinersManifest);
        EditorApplication.MarkSceneDirty();
        EditorApplication.SaveScene();
        AssetDatabase.SaveAssets();
    }

    //[MenuItem("Texture Tools/Crunch Picture")]
    static void Crunch()
    {
        Object target = Selection.activeObject;
        if (target == null)
            return;
        if (target is Texture2D)
        {
            var Target = target as Texture2D;

            TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(Target));
            ti.crunchedCompression = true;
            ti.compressionQuality = 50;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(Target));
        }
    }

    //[MenuItem("Texture Tools/Convert Textures To JPG")]
    static void Convert()
    {
        Object target = Selection.activeObject;
        if (target == null)
            return;
        if (target is Texture2D)
        {
            var Target = target as Texture2D;
            string combindPath = "Assets";
            Texture2D rCombineTex = new Texture2D(Target.width, Target.height, TextureFormat.ARGB32, false);
            rCombineTex.SetPixels32(Target.GetPixels32());
            rCombineTex.Apply();
            byte[] bytes = rCombineTex.EncodeToJPG();
            File.WriteAllBytes(combindPath + "/combineTex.jpg", bytes);
            AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
            rCombineTex = AssetDatabase.LoadAssetAtPath<Texture2D>(combindPath + "/combineTex.jpg");
        }
    }

    //[MenuItem("Optimize Tools/Cancel Combining")]
    static void CancelCombining()
    {
        if (combinersManifest.buildingStatus != MeshBuildingStatus.AssetCreated)
        {
            Debug.Log("This scene was not in mode of AssetCreated");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();
        RemoveBuildingAsset();

        List<MeshOptimizer> meshCombiners = new List<MeshOptimizer>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            var mc = go.GetComponent<MeshOptimizer>();
            meshCombiners.AddRange(go.GetComponentsInChildren<MeshOptimizer>(false));
        }

        foreach (var mc in meshCombiners)
        {
            foreach (var t in mc.allCombiningTarget)
            {
                t.renderer.enabled = true;
            }
            DestroyImmediate(mc.generatedFilter);
            DestroyImmediate(mc.generatedRenderer);
            foreach (var go in mc.repeatObjects)
            {
                DestroyImmediate(go);
            }
            foreach (var go in mc.giantObjects)
            {
                DestroyImmediate(go);
            }
            mc.ClearBuildCache();
        }

        combinersManifest.buildingStatus = MeshBuildingStatus.NotBuilt;
        EditorUtility.SetDirty(combinersManifest);
        EditorApplication.MarkSceneDirty();
        EditorApplication.SaveScene();
        AssetDatabase.SaveAssets();
    }

    //[MenuItem("Optimize Tools/Finish Combine(Donnot Click This)")]
    static void CompleteBuilt()
    {
        if (combinersManifest.buildingStatus == MeshBuildingStatus.CleanBuilt)
        {
            Debug.Log("Already built");
            return;
        }
        if (combinersManifest.buildingStatus == MeshBuildingStatus.NotBuilt)
        {
            Debug.Log("This scene has not been built yet!");
            return;
        }
        if (!combinersManifest.allowFinalBuilt)
        {
            Debug.Log("Are you ready to make final built? This operation can not be undo! If ready,please tick the 'Allow Final Building' in manifest file at " + RelativeSceneAssetDir);
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        List<MeshOptimizer> meshCombiners = new List<MeshOptimizer>();
        foreach (var go in rootObjects)
        {
            if (go.activeInHierarchy == false)
                continue;
            var mc = go.GetComponent<MeshOptimizer>();
            meshCombiners.AddRange(go.GetComponentsInChildren<MeshOptimizer>(false));
        }

        foreach (var mc in meshCombiners)
        {
            foreach (var t in mc.allCombiningTarget)
            {
                DestroyImmediate(t.renderer.gameObject);
            }
            mc.ClearBuildCache();
        }


        combinersManifest.buildingStatus = MeshBuildingStatus.CleanBuilt;
        EditorUtility.SetDirty(combinersManifest);
        EditorApplication.MarkSceneDirty();
        AssetDatabase.SaveAssets();
    }

    void ClearBuildCache()
    {
        //break generated asset reference
        generatedMaterial = null;
        localCombineClampTexture = null;

        //Targets
        allCombiningTarget.Clear();
        clampTexTarget.Clear();
        repeatTexTarget.Clear();
        giantTexTarget.Clear();

        //Textures
        giantTextureList.Clear();
        repeatTextureList.Clear();
        clampTextureList.Clear();

        //GameObjects
        repeatObjects.Clear();
        giantObjects.Clear();
    }

    static void PrepareBuildingPath()
    {
        if (!Directory.Exists(AbsoluteMaterialPath))
        {
            Directory.CreateDirectory(AbsoluteMaterialPath);
        }

        if (!Directory.Exists(AbsoluteMeshPath))
        {
            Directory.CreateDirectory(AbsoluteMeshPath);
        }

        if (!Directory.Exists(AbsoluteTexturePath))
        {
            Directory.CreateDirectory(AbsoluteTexturePath);
        }
    }

    static void RemoveBuildingAsset()
    {
        if (Directory.Exists(AbsoluteMaterialPath))
        {
            string[] paths = AssetDatabase.FindAssets("l:" + generatedAssetLabel + " t:Material", new string[1] { RelativeMaterialPath.Substring(0, RelativeMaterialPath.Length - 1) });
            foreach (var p in paths)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(p));
            }
        }

        if (Directory.Exists(AbsoluteMeshPath))
        {
            string[] paths = AssetDatabase.FindAssets("l:" + generatedAssetLabel + " t:Mesh", new string[1] { RelativeMeshPath.Substring(0, RelativeMeshPath.Length - 1) });
            foreach (var p in paths)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(p));
            }
        }

        if (Directory.Exists(AbsoluteTexturePath))
        {
            string[] paths = AssetDatabase.FindAssets("l:" + generatedAssetLabel + " t:Texture2D", new string[1] { RelativeTexturePath.Substring(0, RelativeTexturePath.Length - 1) });
            foreach (var p in paths)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(p));
            }
        }
    }

    void CreateAsset<T>(T asset, string nameWithoutPostfix) where T : Object
    {
        if (asset == null)
            return;
        string relativePath = "";
        if (asset is Mesh)
        {
            relativePath = RelativeMeshPath;
        }
        else if (asset is Material)
        {
            relativePath = RelativeMaterialPath;
        }
        else
        {
            Debug.Log("Asset type not supported");
            return;
        }

        var assetPath = relativePath + nameWithoutPostfix + index.ToString() + ".asset";
        T t = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (t != null)
            AssetDatabase.DeleteAsset(assetPath);

        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SetLabels(asset, new string[1] { generatedAssetLabel });
    }

    void StoreLocalCombineTexture()
    {
        string storePath = RelativeTexturePath + "ClampCombineTexture" + index.ToString() + ".jpg";
        Texture2D tempTexture = new Texture2D(localCombineClampTexture.width, localCombineClampTexture.height, TextureFormat.ARGB32, false);
        tempTexture.SetPixels32(localCombineClampTexture.GetPixels32());
        tempTexture.Apply();
        byte[] bytes = tempTexture.EncodeToJPG();
        File.WriteAllBytes(storePath, bytes);
        AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
        localCombineClampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(storePath);
    }
#endif

    #region Combining Process
#if !STATIC_OPTIMIZE
    IEnumerator CombiningRoutine()
    {
        //create material and combined clamp texture and corresponding UV rect
        CookMaterialAndClampCombineTexture();

        //combine mesh that use clamp texture,contains great amount of uv operation,need to run in coroutine
        StartCoroutine(CombineClampTexMeshRoutine());

        //combine mesh that use repeat texture
        CombineRepeatTexMesh();

        //combine mesh that use giant texture
        CombineGiantTexMesh();

        while (!clampCombineRoutineFinished)
        {
            yield return null;
        }
        //unload all clamp texture and destroy all children mesh objects
        UnloadUnusedResources();
        unfinishedCombinerCount--;
    }

    bool clampCombineRoutineFinished = false;
    IEnumerator CombineClampTexMeshRoutine()
    {
        generatedRenderer = gameObject.AddComponent<MeshRenderer>();
        generatedFilter = gameObject.AddComponent<MeshFilter>();
        generatedRenderer.sharedMaterial = generatedMaterial;

        CombineInstance[] combine = new CombineInstance[clampTexTarget.Count];

        Matrix4x4 goMatrix = transform.localToWorldMatrix;

        for (int i = 0; i < clampTexTarget.Count; i++)
        {
            if (clampTexTarget[i].renderer.transform == transform)
            {
                continue;
            }
            Rect rect = localCombineTexRects[clampTexTarget[i].textureIndex];
            //Debug.Log("Rect->Position:" + rect.position.ToString("f3") + " ->Size:" + rect.size.ToString("f3"));

            Mesh meshCombine = clampTexTarget[i].filter.mesh;
            Vector2[] uvs = new Vector2[meshCombine.uv.Length];
            //Debug.Log("UV count:"+uvs.Length);
            int k = 0;

            for (int j = 0; j < uvs.Length; j++)
            {
                //Debug.Log("UV:" + meshCombine.uv[j].ToString("f3"));
                float x = meshCombine.uv[j].x;
                float y = meshCombine.uv[j].y;

                uvs[j].x = rect.x + x * rect.width;
                uvs[j].y = rect.y + y * rect.height;
                k++;
                if (k >= maxUVProcessingPerFrame)
                {
                    k = 0;
                    yield return null;
                }
            }

            meshCombine.uv = uvs;
            combine[i].mesh = meshCombine;


            Matrix4x4 meMatrix = clampTexTarget[i].renderer.transform.localToWorldMatrix;

            combine[i].transform = Matrix4x4.Inverse(goMatrix) * meMatrix;
            yield return null;
        }

        Mesh newMesh = new Mesh();
        newMesh.CombineMeshes(combine, true, true);//合并网格  
        generatedFilter.mesh = newMesh;
        clampCombineRoutineFinished = true;
        yield return null;
    }
#elif UNITY_EDITOR
    void CombiningProcess()
    {
        //create material and combined clamp texture and corresponding UV rect
        CookMaterialAndClampCombineTexture();

        //combine mesh that use clamp texture,contains great amount of uv operation,need to run in coroutine
        CombineClampTexMesh();

        //combine mesh that use repeat texture
        CombineRepeatTexMesh();

        //combine mesh that use giant texture
        CombineGiantTexMesh();

        HideChildrenMeshRenderers();
    }

    void CombineClampTexMesh()
    {
        generatedRenderer = gameObject.GetComponent<MeshRenderer>();
        generatedFilter = gameObject.GetComponent<MeshFilter>();
        if (generatedRenderer == null)
            generatedRenderer = gameObject.AddComponent<MeshRenderer>();
        if (generatedFilter == null)
            generatedFilter = gameObject.AddComponent<MeshFilter>();
        generatedRenderer.sharedMaterial = generatedMaterial;

        CombineInstance[] combine = new CombineInstance[clampTexTarget.Count];

        Matrix4x4 goMatrix = transform.localToWorldMatrix;

        for (int i = 0; i < clampTexTarget.Count; i++)
        {
            if (clampTexTarget[i].renderer.transform == transform)
            {
                continue;
            }
            Rect rect = localCombineTexRects[clampTexTarget[i].textureIndex];
            //Debug.Log("Rect->Position:" + rect.position.ToString("f3") + " ->Size:" + rect.size.ToString("f3"));
            
            Mesh meshCombine = Instantiate<Mesh>(clampTexTarget[i].filter.sharedMesh);

            //Vector2[] originalUVs = meshCombine.uv;

            Vector2[] uvs = new Vector2[meshCombine.uv.Length];
            //Debug.Log("UV count:"+uvs.Length);

            for (int j = 0; j < uvs.Length; j++)
            {
                //Debug.Log("UV:" + meshCombine.uv[j].ToString("f3"));
                float x = meshCombine.uv[j].x;
                float y = meshCombine.uv[j].y;

                uvs[j].x = rect.x + x * rect.width;
                uvs[j].y = rect.y + y * rect.height;
            }

            meshCombine.uv = uvs;
            combine[i].mesh = meshCombine;


            Matrix4x4 meMatrix = clampTexTarget[i].renderer.transform.localToWorldMatrix;

            combine[i].transform = Matrix4x4.Inverse(goMatrix) * meMatrix;
        }

        Mesh newMesh = new Mesh();
        newMesh.CombineMeshes(combine, true, true);//合并网格  
        generatedFilter.sharedMesh = newMesh;

        //localCombineClampTexture.Compress(false);

        Debug.Log(localCombineClampTexture.format.ToString());
#if UNITY_EDITOR

        CreateAsset<Material>(generatedMaterial, "ClampCombineMaterial");
        CreateAsset<Mesh>(generatedFilter.sharedMesh, "ClampCombineMesh");
#endif
    }
#endif
    #endregion

    #region run-time data
    /// <summary>
    /// The mesh and renderer info for those target whose mesh and texture need to be optimized
    /// </summary>
    [System.Serializable]
    public struct CombiningTargetInfo
    {
        public MeshRenderer renderer;//MeshRenderer component of target
        public MeshFilter filter;//MeshFilter component of target
        public TextureWrapMode wrapMode;//The wrap mode of target's material's main texture
        public int textureIndex;//Texture index on texture list
    }

    [Header("Build Cache Data")]
    [Header("Targets")]
    /// <summary>
    /// All targets that need to merge mesh and texture
    /// </summary>
    public List<CombiningTargetInfo> allCombiningTarget = new List<CombiningTargetInfo>();


    /// <summary>
    /// The mesh target that use clamp texture
    /// </summary>
    public List<CombiningTargetInfo> clampTexTarget = new List<CombiningTargetInfo>();

    /// <summary>
    /// The mesh target that use repeat texture
    /// </summary>
    public List<CombiningTargetInfo> repeatTexTarget = new List<CombiningTargetInfo>();

    public List<CombiningTargetInfo> giantTexTarget = new List<CombiningTargetInfo>();

    [Header("Textures")]
    /// <summary>
    /// Repeat texture,will not be merged,but the mesh will be merged and share same texture
    /// </summary>
    public List<Texture2D> repeatTextureList = new List<Texture2D>();

    public List<Texture2D> clampTextureList = new List<Texture2D>();

    public List<Texture2D> giantTextureList = new List<Texture2D>();

    [Header("GameObjects")]
    public List<GameObject> giantObjects = new List<GameObject>();

    public List<GameObject> repeatObjects = new List<GameObject>();
    #endregion

    #region Common pipe operations
    void PreparingMeshTarget()
    {
        //Get all mesh filter, each one is a target
        MeshFilter[] mfChildren = GetComponentsInChildren<MeshFilter>(false);

        foreach (var m in mfChildren)
        {
            if (m.GetComponent<CombineRejectMarker>() || m.GetComponentInParent<CombineRejectMarker>())
            {
                continue;
            }

            if(m.sharedMesh==null)
            {
                Debug.Log("No mesh found in " + m.gameObject.name);
                continue;
            }

            //Set basic data
            var mm = new CombiningTargetInfo();
            mm.filter = m;
            mm.renderer = m.GetComponent<MeshRenderer>();

            var mainTexture = mm.renderer.sharedMaterial.mainTexture as Texture2D;
            if (mainTexture == null)
                continue;

            //it is a giant texture mesh
            if (mainTexture.width > giantTextureSize || mainTexture.height > giantTextureSize)
            {
                int index = giantTextureList.FindIndex(a =>
                {
                    return a == mainTexture;

                });

                //has same texture before
                if (index >= 0)
                {
                    mm.textureIndex = index;
                }
                else
                {
                    giantTextureList.Add(mainTexture);
                    mm.textureIndex = giantTextureList.Count - 1;
                }
                mm.wrapMode = mainTexture.wrapMode;
                giantTexTarget.Add(mm);
            }
            else//normal size texture
            {
                if (mainTexture.wrapMode == TextureWrapMode.Clamp)
                {
                    int index = clampTextureList.FindIndex(a =>
                    {
                        return a == mainTexture;

                    });

                    //has same texture before
                    if (index >= 0)
                    {
                        mm.textureIndex = index;

                    }
                    else
                    {
                        clampTextureList.Add(mainTexture);
                        mm.textureIndex = clampTextureList.Count - 1;
                    }
                    mm.wrapMode = TextureWrapMode.Clamp;
                    clampTexTarget.Add(mm);
                }
                else
                {
                    int index = repeatTextureList.FindIndex(a =>
                    {
                        return a == mainTexture;

                    });

                    //has same texture before
                    if (index >= 0)
                    {
                        mm.textureIndex = index;

                    }
                    else
                    {
                        repeatTextureList.Add(mainTexture);
                        mm.textureIndex = repeatTextureList.Count - 1;
                    }
                    mm.wrapMode = TextureWrapMode.Repeat;
                    repeatTexTarget.Add(mm);
                }
            }
            allCombiningTarget.Add(mm);
        }
    }

    void CookMaterialAndClampCombineTexture()
    {
        if (localCombineClampTexture != null)
            return;

        //localCombineClampTexture = new Texture2D(combineTextureSize, combineTextureSize);

        localCombineClampTexture = new Texture2D(combineTextureSize, combineTextureSize, TextureFormat.DXT1, true);
        localCombineTexRects = localCombineClampTexture.PackTextures(clampTextureList.ToArray(), 0, combineTextureSize, false);

#if UNITY_EDITOR&&STATIC_OPTIMIZE
        StoreLocalCombineTexture();
#endif
        if(clampTexTarget.Count!=0)
        {
            var standardMat = clampTexTarget[0].renderer.sharedMaterial;

            generatedMaterial = new Material(standardMat.shader);
            generatedMaterial.CopyPropertiesFromMaterial(standardMat);
            generatedMaterial.SetTexture("_MainTex", localCombineClampTexture);
        }
        
    }

    void CombineRepeatTexMesh()
    {
        Matrix4x4 goMatrix = transform.localToWorldMatrix;
        //iterating based on repeat texture
        for (int i = 0; i < repeatTextureList.Count; i++)
        {
            Texture2D tex = repeatTextureList[i];
            List<CombiningTargetInfo> mm = repeatTexTarget.FindAll(a => { return a.textureIndex == i; });
            CombineInstance[] repeatCombine = new CombineInstance[mm.Count];

            GameObject go = new GameObject();
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.name = "RepeatMesh-" + mm[0].renderer.sharedMaterial.name;
            repeatObjects.Add(go);

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            MeshFilter mf = go.AddComponent<MeshFilter>();

            var sMat = mm[0].renderer.sharedMaterial;
            var mn = new Material(sMat.shader);
            mn.CopyPropertiesFromMaterial(sMat);
            mr.sharedMaterial = mn;



            mn.SetTexture("_MainTex", tex);


            for (int j = 0; j < mm.Count; j++)
            {
#if !STATIC_OPTIMIZE
                Mesh meshCombine = mm[j].filter.mesh;
#else
                Mesh meshCombine = mm[j].filter.sharedMesh;
#endif
                repeatCombine[j].mesh = meshCombine;


                Matrix4x4 meMatrix = mm[j].renderer.transform.localToWorldMatrix;

                repeatCombine[j].transform = Matrix4x4.Inverse(goMatrix) * meMatrix;
            }

            Mesh nmesh = new Mesh();
            nmesh.CombineMeshes(repeatCombine, true, true);//合并网格  
            mf.mesh = nmesh;
#if STATIC_OPTIMIZE&&UNITY_EDITOR
            CreateAsset<Mesh>(nmesh, go.name);
            CreateAsset<Material>(mn, go.name);
#endif
        }
    }

    void CombineGiantTexMesh()
    {
        Matrix4x4 goMatrix = transform.localToWorldMatrix;
        //iterating based on repeat texture
        for (int i = 0; i < giantTextureList.Count; i++)
        {
            Texture2D tex = giantTextureList[i];
            List<CombiningTargetInfo> mm = giantTexTarget.FindAll(a => { return a.textureIndex == i; });
            CombineInstance[] giantCombine = new CombineInstance[mm.Count];

            GameObject go = new GameObject();
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.name = "GiantMesh-" + mm[0].renderer.sharedMaterial.name;
            repeatObjects.Add(go);

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            MeshFilter mf = go.AddComponent<MeshFilter>();

            var sMat = mm[0].renderer.sharedMaterial;
            var mn = new Material(sMat.shader);
            mn.CopyPropertiesFromMaterial(sMat);
            mr.sharedMaterial = mn;

            mn.SetTexture("_MainTex", tex);

            mr.sharedMaterial = mn;

            for (int j = 0; j < mm.Count; j++)
            {
#if !STATIC_OPTIMIZE
                Mesh meshCombine = mm[j].filter.mesh;
#else
                Mesh meshCombine = mm[j].filter.sharedMesh;
#endif
                giantCombine[j].mesh = meshCombine;


                Matrix4x4 meMatrix = mm[j].renderer.transform.localToWorldMatrix;

                giantCombine[j].transform = Matrix4x4.Inverse(goMatrix) * meMatrix;
            }

            Mesh nmesh = new Mesh();
            nmesh.CombineMeshes(giantCombine, true, true);//合并网格  

            mf.mesh = nmesh;
#if STATIC_OPTIMIZE&&UNITY_EDITOR
            CreateAsset<Mesh>(nmesh, go.name);
            CreateAsset<Material>(mn, go.name);
#endif
        }
    }
    #endregion

    #region Post pipe operation
#if !STATIC_OPTIMIZE
    void UnloadUnusedResources()
    {
        foreach (var m in allCombiningTarget)
        {
            Destroy(m.renderer.gameObject);
        }
#if !UNITY_EDITOR
        //unload all small clamp texture
        foreach(var m in clampTextureList)
        {
            Resources.UnloadAsset(m);
        }
#endif
    }
#endif

#if STATIC_OPTIMIZE
    void HideChildrenMeshRenderers()
    {
        foreach (var p in allCombiningTarget)
        {
            if (p.renderer != null)
                p.renderer.enabled = false;
        }
    }

    void KillChildRenderers()
    {
        foreach (var p in allCombiningTarget)
        {
            Destroy(p.renderer.gameObject);
        }
    }
#endif
    #endregion
}

