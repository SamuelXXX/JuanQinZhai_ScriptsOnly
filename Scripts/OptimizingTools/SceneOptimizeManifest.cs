using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if STATIC_OPTIMIZE
public enum MeshBuildingStatus
{
    NotBuilt = 0,
    AssetCreated,
    CleanBuilt
}

public class SceneOptimizeManifest : ScriptableObject
{
    public MeshBuildingStatus buildingStatus = MeshBuildingStatus.NotBuilt;
    public bool allowFinalBuilt = false;
}
#endif
