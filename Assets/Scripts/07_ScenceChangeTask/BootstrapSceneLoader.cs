#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;


#if UNITY_EDITOR  
[InitializeOnLoad] 
#endif

public static class BootstrapSceneLoader
{
    private const string BootstrapScenePath = "Assets/Scenes/BootstrapScene.unity";
    private const string PrevScenePathKey = "SceneController.PreviousScenePath";

    private static GameObject _coreManagersInstance;

#if UNITY_EDITOR
    
    static BootstrapSceneLoader()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Debug.Log("[GameInitializer] 偵測到退出播放模式，正在清理 CoreManagers...");

            // 檢查我們之前創建的實例是否存在
            if (_coreManagersInstance != null)
            {
                // 銷毀它！
                Object.Destroy(_coreManagersInstance);
            }
        }
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        var activeScene = SceneManager.GetActiveScene();

        if (activeScene.path == BootstrapScenePath)
        {
            EditorPrefs.DeleteKey(PrevScenePathKey);
        }
        else
        {
            EditorPrefs.SetString(PrevScenePathKey, activeScene.path);
        }
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RuntimeInit()
    {
        // 打包後啟動時也會執行
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != BootstrapScenePath)
        {
            GameObject coreManagersPrefab = Resources.Load<GameObject>("CoreManager");
            // 實例化 Prefab
            GameObject managersInstance = Object.Instantiate(coreManagersPrefab);
            // 重新命名以方便在 Hierarchy 中辨識
            managersInstance.name = "[CoreManagers]";
            // 確保這個物件在切換場景時不會被銷毀
            Object.DontDestroyOnLoad(managersInstance);
            _coreManagersInstance = managersInstance;
        }
    }
}
