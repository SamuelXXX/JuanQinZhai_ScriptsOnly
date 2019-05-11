using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// The state of all blocks' positioning
/// </summary>
public enum BlockCombinationStatus
{
    None = 0,
    Combine,//combined preview 
    CombineInsidePreview,
    Divide,//divide preview
    FocusOnOneBlock,//focus on one block
    Indoor,
    SpecialScene
}

public class BlockManager : MonoBehaviour
{
    #region Singleton
    protected static BlockManager singleton = null;
    public static BlockManager Singleton
    {
        get
        {
            return singleton;
        }
    }
    #endregion

    #region Settings
    /// <summary>
    /// All blocks managed by this block manager
    /// </summary>
    public List<BlockInfo> allBlocks;
    /// <summary>
    /// All special scene managed by this manager
    /// </summary>
    public List<SpecialScene> allSpecialScenes;
    public GameObject backButton;
    protected bool isCombining = false;

    public bool IsTransforming
    {
        get
        {
            foreach (var b in allBlocks)
            {
                if (b.IsMoving)
                    return true;
            }

            return false;
        }
    }
    #endregion

    #region Run time data
    [HideInInspector]
    public BlockCombinationStatus status = BlockCombinationStatus.Combine;
    protected int focusBlockIndex = 0;
    protected bool isBackButtonOn = true;
    #endregion

    #region Mono Events
    void Awake()
    {
        singleton = this;//Assign singleton
    }
    // Use this for initialization
    void Start()
    {
        ForwardTransform();
        //push current view status to view stack
        CameraCharacter.Singleton.PushView(GenerateViewParameterByStatus());
        CameraCharacter.Singleton.OnCameraDarkend += TryCombineBlocksWhenIndoor;
        CameraCharacter.Singleton.OnCameraDarkend += ProcessMesh;

        //Register global event handlers
        GlobalEventManager.RegisterHandler(GlobalEventManager.Back,
         (evt) =>
         {
             BackwardTransform();
         });

        GlobalEventManager.RegisterHandler(GlobalEventManager.Forward,
         (evt) =>
         {
             ForwardTransform();
         });

        foreach (var s in allSpecialScenes)
        {
            GlobalEventManager.RegisterHandler("EnterSpecialScene-" + s.specialSceneName,
                (evt) =>
                {
                    EnterSpecialScene(s.specialSceneName);
                });
        }

        GlobalEventManager.RegisterHandler("ExitSpecialScene",
            (evt) =>
            {
                ExitSpecialScene();
            });
    }

    private void Update()
    {
        ProcessForBackCommand();
    }

    void OnValidate()
    {
        if (allBlocks.Count == 0)
        {
            allBlocks.AddRange(GetComponentsInChildren<BlockInfo>());
        }

        if (allSpecialScenes.Count == 0)
        {
            allSpecialScenes.AddRange(GetComponentsInChildren<SpecialScene>());
        }

        //Validate all blocks
        foreach (var b in allBlocks)
        {
            b.manager = this;
            b.focusedPosition = Vector3.zero;
        }

        //Validate all special scene
        foreach (var s in allSpecialScenes)
        {
            s.manager = this;
        }
    }

    #endregion

    #region Mesh Loading
    public void LoadMesh()
    {
        foreach (var b in allBlocks)
        {
            b.LoadMesh();
        }
    }

    public void UnloadMesh()
    {
        foreach (var b in allBlocks)
        {
            b.UnloadMesh();
        }
    }
    #endregion

    #region Combination status transfer stack
    public class ForwardBackwardCommand
    {
        public Vector3? position;
        public Quaternion? rotation;
        public PanoramaBlock panoramaBlock;
        public bool isForwardCommand;
        public float commandTime;

        public ForwardBackwardCommand(Vector3? pos, Quaternion? rot, PanoramaBlock panoramaBlock, bool isForwardCommand, float time)
        {
            position = pos;
            rotation = rot;
            this.panoramaBlock = panoramaBlock;
            this.isForwardCommand = isForwardCommand;
            commandTime = time;
        }
    }

    int maxCommandBuffer = 2;
    Queue<ForwardBackwardCommand> commandQueue = new Queue<ForwardBackwardCommand>();

    public int CommandQueueLenght
    {
        get
        {
            return commandQueue.Count;
        }
    }
    /// <summary>
    /// Transform all blocks to forward form
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    public void ForwardTransform(Vector3? pos = null, Quaternion? rot = null, PanoramaBlock panoramaBlock = null)
    {
        if (commandQueue.Count < maxCommandBuffer)
            commandQueue.Enqueue(new ForwardBackwardCommand(pos, rot, panoramaBlock, true, Time.time));
    }

    /// <summary>
    /// Transform all blocks to backward form
    /// </summary>
    public void BackwardTransform()
    {
        if (commandQueue.Count < maxCommandBuffer)
            commandQueue.Enqueue(new ForwardBackwardCommand(null, null, null, false, Time.time));
    }

    PanoramaBlock currentHoldingPanorama = null;
    void ProcessForBackCommand()
    {
        if (commandQueue.Count == 0 || IsTransforming)
        {
            return;
        }
        var oldStatus = status;
        var command = commandQueue.Dequeue();
        currentHoldingPanorama = command.panoramaBlock;
        if (command.isForwardCommand)
        {
            switch (status)
            {
                case BlockCombinationStatus.None:
                    status = BlockCombinationStatus.Combine;
                    break;
                case BlockCombinationStatus.Combine:
                    if (CameraCharacter.Singleton.GameModeC == GameMode.GuideMode)
                        backButton.SetActive(true);
                    GlobalEventManager.SendEvent(GlobalEventManager.RoofRemoved);
                    status = BlockCombinationStatus.CombineInsidePreview;
                    break;
                case BlockCombinationStatus.CombineInsidePreview:
                    status = BlockCombinationStatus.Divide;
                    GlobalEventManager.SendEvent(GlobalEventManager.BlockDivide);
                    break;
                case BlockCombinationStatus.Divide:
                    status = BlockCombinationStatus.FocusOnOneBlock;
                    break;
                case BlockCombinationStatus.FocusOnOneBlock:
                    status = BlockCombinationStatus.Indoor;
                    break;
                case BlockCombinationStatus.Indoor:
                    return;
                case BlockCombinationStatus.SpecialScene:
                    return;
                default:
                    break;
            }
            MoveSubBlocksByStatus(oldStatus, status);
            CameraCharacter.Singleton.PushView(GenerateViewParameterByStatus(command.position, command.rotation, command.panoramaBlock));
            if (command.panoramaBlock)
                command.panoramaBlock.DownloadAssetBundle();
        }
        else
        {
            switch (status)
            {
                case BlockCombinationStatus.Combine:
                    if (CameraCharacter.Singleton.GameModeC == GameMode.FreeMode)
                    {
                        GlobalEventManager.SendEvent("ShowModeSelecting");
                    }
                    break;
                case BlockCombinationStatus.CombineInsidePreview:
                    GlobalEventManager.SendEvent(GlobalEventManager.BlockCombine);
                    if (CameraCharacter.Singleton.GameModeC == GameMode.GuideMode)
                        backButton.SetActive(false);
                    status = BlockCombinationStatus.Combine;
                    break;
                case BlockCombinationStatus.Divide:
                    status = BlockCombinationStatus.CombineInsidePreview;
                    break;
                case BlockCombinationStatus.FocusOnOneBlock:
                    GlobalEventManager.SendEvent(GlobalEventManager.BlockDivide);
                    status = BlockCombinationStatus.Divide;
                    break;
                case BlockCombinationStatus.Indoor:
                    status = BlockCombinationStatus.FocusOnOneBlock;
                    break;
                case BlockCombinationStatus.SpecialScene:
                    return;
                default:
                    break;
            }
            MoveSubBlocksByStatus(oldStatus, status);
            CameraCharacter.Singleton.PopView();
            if (command.panoramaBlock)
                command.panoramaBlock.DownloadAssetBundle();
        }
    }


    BlockCombinationStatus statusBeforeSpecialScene = BlockCombinationStatus.SpecialScene;
    SpecialScene currentSpecialScene = null;
    public void EnterSpecialScene(string sceneName)
    {
        if (currentSpecialScene != null)
            return;

        foreach (var s in allSpecialScenes)
        {
            if (s == null)
                continue;
            if (s.specialSceneName == sceneName)
            {
                currentSpecialScene = s;
                break;
            }
        }

        if (currentSpecialScene == null)
        {
            return;
        }
        var ret = new CameraCharacter.ViewParameters();
        ret.viewMode = currentSpecialScene.sceneViewMode;
        ret.viewContentType = currentSpecialScene.sceneViewContentType;
        ret.cameraZOffset = 0f;
        ret.initialPosition = currentSpecialScene.sceneViewCenter.position;
        ret.initialRotation = currentSpecialScene.sceneViewCenter.rotation;
        ret.forceFading = true;

        if (currentSpecialScene.sceneViewContentType == ViewContentType.PanoramaView && currentSpecialScene.viewContentDelegation != null)
        {
            currentSpecialScene.viewContentDelegation.DownloadAssetBundle();
            ret.FadeInFlag = currentSpecialScene.viewContentDelegation.PanoramaContentReady;
        }

        statusBeforeSpecialScene = status;
        status = BlockCombinationStatus.SpecialScene;
        CameraCharacter.Singleton.PushView(ret, true);
        currentSpecialScene.OnEnterSpecialScene();
    }

    public void ExitSpecialScene()
    {
        if (!currentSpecialScene)
            return;

        currentSpecialScene.OnExitSpecialScene();
        status = statusBeforeSpecialScene;
        currentSpecialScene = null;
        CameraCharacter.Singleton.PopView(true);

        if (currentHoldingPanorama != null && !currentHoldingPanorama.bundleDownloaded)
        {
            currentHoldingPanorama.DownloadAssetBundle();
        }

    }

    CameraCharacter.ViewParameters GenerateViewParameterByStatus(Vector3? pos = null, Quaternion? rot = null, PanoramaBlock panoramaBlock = null)
    {
        CameraCharacter.ViewParameters ret = new CameraCharacter.ViewParameters();
        switch (status)
        {
            case BlockCombinationStatus.Combine:
                ret.viewMode = CameraViewMode.HorizontalFreeView;
                ret.initialRotation = CameraCharacter.Singleton.fixedViewRotation;
                ret.cameraZOffset = 24f;
                break;
            case BlockCombinationStatus.CombineInsidePreview:
                ret.viewMode = CameraViewMode.HorizontalHalfFreeView;
                ret.initialRotation = CameraCharacter.Singleton.lookDownViewRotation;
                ret.cameraZOffset = 25f;
                ret.cameraScaleMode = CameraScaleMode.MoveCamera;
                break;
            case BlockCombinationStatus.Divide:
                ret.viewMode = CameraViewMode.FixedView;
                ret.cameraZOffset = 22f;
                ret.initialPosition = CameraCharacter.Singleton.fixedViewPosition;
                ret.initialRotation = CameraCharacter.Singleton.fixedViewRotation;
                break;
            case BlockCombinationStatus.FocusOnOneBlock:
                ret.viewMode = CameraViewMode.HorizontalHalfFreeView;
                ret.initialPosition = CameraCharacter.Singleton.lookDownViewPosition;
                ret.initialRotation = CameraCharacter.Singleton.lookDownViewRotation;
                ret.cameraZOffset = 18f;
                ret.cameraXOffset = 5f;
                ret.cameraScaleMode = CameraScaleMode.MoveCamera;
                break;
            case BlockCombinationStatus.Indoor:
                ret.viewMode = CameraViewMode.FreeView;
                ret.viewContentType = ViewContentType.PanoramaView;
                if (panoramaBlock != null)
                    ret.FadeInFlag = panoramaBlock.PanoramaContentReady;
                ret.cameraZOffset = 0f;
                ret.initialPosition = pos;
                ret.initialRotation = rot;
                ret.cameraScaleMode = CameraScaleMode.ScaleFov;
                break;
            case BlockCombinationStatus.SpecialScene:
                break;
            default:
                break;
        }

        return ret;
    }

    public void SetFocusBlockIndex(int index)
    {
        focusBlockIndex = index;
        focusBlockIndex = Mathf.Clamp(focusBlockIndex, 0, allBlocks.Count - 1);
    }

    public void SetFocusBlockIndex(string name)
    {
        focusBlockIndex = 0;
        for (int i = 0; i < allBlocks.Count; i++)
        {
            BlockInfo b = allBlocks[i];
            if (b.blockName == name)
            {
                focusBlockIndex = i;
                break;
            }
        }
    }

    public void ProcessMesh()
    {
        if (status == BlockCombinationStatus.Indoor)
        {
            UnloadMesh();
        }
        else if (status == BlockCombinationStatus.SpecialScene)
        {
            if (currentSpecialScene != null && currentSpecialScene.isIndoor && currentSpecialScene.sceneViewContentType == ViewContentType.Model3DView)
            {
                LoadMesh();
            }
            else
            {
                UnloadMesh();
            }
        }
        else
        {
            LoadMesh();
        }
    }

    public void TryCombineBlocksWhenIndoor()
    {
        if (status != BlockCombinationStatus.Indoor)
            return;

        Vector3 offset = Vector3.zero;
        offset = -allBlocks[focusBlockIndex].combiningPosition;
        foreach (var b in allBlocks)
        {
            b.transform.localPosition = b.combiningPosition + offset;
        }
    }

    protected void MoveSubBlocksByStatus(BlockCombinationStatus oldStatus, BlockCombinationStatus newStatus)
    {
        //Vector3 indoorOffset = Vector3.zero;
        //if (newStatus == BlockCombinationStatus.Indoor)
        //{
        //    foreach (var b in allBlocks)
        //    {
        //        if (allBlocks.IndexOf(b) == focusBlockIndex)
        //        {
        //            indoorOffset = -b.combiningPosition;
        //            break;
        //        }

        //    }
        //}

        float lerpSpeed = 8f;
        if (oldStatus == BlockCombinationStatus.Combine && newStatus == BlockCombinationStatus.CombineInsidePreview)
        {
            lerpSpeed = 3f;
        }
        else if (oldStatus == BlockCombinationStatus.CombineInsidePreview && newStatus == BlockCombinationStatus.Combine)
        {
            lerpSpeed = 6f;
        }
        else if (oldStatus == BlockCombinationStatus.CombineInsidePreview && newStatus == BlockCombinationStatus.Divide)
        {
            lerpSpeed = 5f;
        }

        foreach (var b in allBlocks)
        {
            if (b != null)
            {

                if (newStatus != BlockCombinationStatus.FocusOnOneBlock)
                {
                    if (newStatus != BlockCombinationStatus.Indoor)
                        b.StartMoving(newStatus, lerpSpeed);
                }
                else
                {
                    b.StartMoving(newStatus, lerpSpeed, null, allBlocks.IndexOf(b) == focusBlockIndex);
                }
            }
        }
    }
    #endregion

#if UNITY_EDITOR
    #region Editing Helper
    [MenuItem("Optimize Tools/Build Scene")]
    public static void BuildScene()
    {
        ImageLoader.BuildImageMapper();
        SelectorUIItem.AssetToName();
        UIContentGetter.BuildSubImageManager();
        SpecialScene.BuildSubMaterialManager();
    }

    [MenuItem("Optimize Tools/Revert Scene")]
    public static void RevertScene()
    {
        ImageLoader.CancelMapper();
        SelectorUIItem.NameToAsset();
        UIContentGetter.KillSubImageManager();
        SpecialScene.KillSubMaterialManager();
    }
    /// <summary>
    /// Build blocks position data when combined together and save it
    /// </summary>
    public void BuildCombineData()
    {
        foreach (var b in allBlocks)
        {
            if (b.block != null)
            {
                b.combiningPosition = b.block.transform.localPosition;

            }
        }
    }

    /// <summary>
    /// Build blocks position data when combined inside view together and save it
    /// </summary>
    public void BuildCombineInsideViewData()
    {
        foreach (var b in allBlocks)
        {
            if (b.block != null)
            {
                b.combineInsideViewPosition = b.block.transform.localPosition;

            }
        }
    }

    /// <summary>
    /// Build blocks position data when divided apart and save it
    /// </summary>
    public void BuildDivideData()
    {
        foreach (var b in allBlocks)
        {
            if (b.block != null)
            {
                b.dividingPosition = b.block.transform.localPosition;

            }
        }
    }
    #endregion
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(BlockManager))]
public class BlockManagerEditor : Editor
{
    public BlockManager Target
    {
        get
        {
            return target as BlockManager;
        }
    }
    bool allowDataBuilding = false;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        if (GUILayout.Button("To Combine Status"))
        {
            foreach (var b in Target.allBlocks)
            {
                if (b.block != null)
                {
                    b.block.transform.localPosition = b.combiningPosition;
                }
            }
        }
        if (GUILayout.Button("To Divide Status"))
        {
            foreach (var b in Target.allBlocks)
            {
                if (b.block != null)
                {
                    b.block.transform.localPosition = b.dividingPosition;
                }
            }
        }
        if (GUILayout.Button("To Combine Inside Status"))
        {
            foreach (var b in Target.allBlocks)
            {
                if (b.block != null)
                {
                    b.block.transform.localPosition = b.combineInsideViewPosition;
                }
            }
        }
        EditorGUILayout.Space();
        allowDataBuilding = EditorGUILayout.Toggle("Allow Data Building", allowDataBuilding);

        if (allowDataBuilding)
        {
            if (GUILayout.Button("Build Combine Data"))
            {
                Target.BuildCombineData();
                allowDataBuilding = false;
            }
            if (GUILayout.Button("Build CombineInside Data"))
            {
                Target.BuildCombineInsideViewData();
                allowDataBuilding = false;
            }
            if (GUILayout.Button("Build Dividing Data"))
            {
                Target.BuildDivideData();
                allowDataBuilding = false;
            }
        }
    }
}
#endif


