using UnityEngine;
using UnityEngine.UI;

// UI 按钮音效自动绑定器：挂在 Canvas 上，运行时查找所有子 Button 并绑定 OnClick
// 菜单场景用 sfxButtonMenu（Retro10），其他场景用 sfxButtonGame（Modern11）
public class UIButtonSFX : MonoBehaviour
{
    private void Start()
    {
        // 等一帧让所有 Button 都就位（避免 Canvas 下的动态生成按钮还没出现）
        StartCoroutine(BindAllButtonsNextFrame());
    }

    private System.Collections.IEnumerator BindAllButtonsNextFrame()
    {
        yield return null;
        BindAllButtons();
    }

    private void BindAllButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        // 菜单场景用 Retro10，其他用 Modern11
        AudioClip clip = IsMenuScene() ? AudioManager.Instance?.sfxButtonMenu : AudioManager.Instance?.sfxButtonGame;
        if (clip == null) return;

        foreach (Button btn in buttons)
        {
            btn.onClick.AddListener(() => AudioManager.Instance?.PlaySFX(clip));
        }
    }

    private bool IsMenuScene()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Menu";
    }
}
