using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GlobalGameSettings", menuName = "Global Game Settings")]
public class GlobalGameSettings : ScriptableObject
{
    static GlobalGameSettings instance;
    public static GlobalGameSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GlobalGameSettings>("GlobalGameSettings");
            }
            return instance;
        }
    }

    [Header("Panorama Settings")]
    public string panoramaDownloadPath;   
}


