using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadingManager : MonoBehaviour
{
    public static string sceneReentranceCommand = "";
    public static AsyncOperation loadingOperation;
    // Use this for initialization
    void Awake()
    {
        GlobalEventManager.RegisterHandler("StartSceneLoadingWaiting", OnStartSceneLoadingWaiting);
        GlobalEventManager.RegisterHandler("StopSceneLoadingWaiting", OnStopSceneLoadingWaiting);
    }

    // Update is called once per frame
    void Update()
    {

    }

    #region System Command
    public void ReloadCurrentScene()
    {
        GlobalEventManager.ResetGlobalEventManager();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SwitchToFreeMode()
    {
        UISystem.Singleton.SetContent("SW_ReenterModeSelecting", false);
        sceneReentranceCommand = "FreeMode";
        GlobalEventManager.SendEvent("NewModeSelected");
        Invoke("ReloadCurrentScene", 0.8f);
    }

    public void OnStartSceneLoadingWaiting(GlobalEvent evt)
    {
        Application.ExternalCall("StartLoadingWaiting", 0);
    }

    public void OnStopSceneLoadingWaiting(GlobalEvent evt)
    {
        Application.ExternalCall("StopLoadingWaiting", 0);
    }

    public void SwitchToGuideMode()
    {
        UISystem.Singleton.SetContent("SW_ReenterModeSelecting", false);
        sceneReentranceCommand = "GuideMode";
        GlobalEventManager.SendEvent("NewModeSelected");
        Invoke("ReloadCurrentScene", 0.8f);
    }
    #endregion
}
