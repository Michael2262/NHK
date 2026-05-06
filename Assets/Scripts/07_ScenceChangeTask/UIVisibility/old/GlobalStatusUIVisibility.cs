using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalStatusUIVisibility : MonoBehaviour
{
    [Header("在這裡填入『需要隱藏 UI』的場景名稱")]
    [Tooltip("Scene 名稱大小寫需精確一致")]
    [SerializeField] private string[] scenesToHideUI;

    [Tooltip("要隱藏的 UI 物件")]
    [SerializeField] private GameObject[] elementsToHide;

    void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        ApplyVisibility(SceneManager.GetActiveScene().name);
    }

    void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;

    void OnSceneChanged(Scene oldScene, Scene newScene) =>
        ApplyVisibility(newScene.name);

    void ApplyVisibility(string sceneName)
    {
        bool hide = scenesToHideUI != null &&
                    scenesToHideUI.Any(name => name == sceneName);
        foreach (var go in elementsToHide)
            if (go) go.SetActive(!hide);
    }
}