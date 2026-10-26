using UnityEngine;
using UnityEngine.SceneManagement;

// 主菜单控制器：负责 Menu 场景"开始"按钮的进游戏逻辑
// 挂载在 Menu 场景的 MenuController 物体上，StartButton 的 OnClick 绑定 StartGame 方法
public class MainMenuController : MonoBehaviour
{
    [Header("点击开始后进入的场景名")]
    [Tooltip("需与 Build Settings 中村庄场景的名字一致")]
    public string targetSceneName = "village";

    // 防止连点导致重复触发加载
    private bool isStarting;

    // 绑定在 StartButton 的 OnClick 上（场景中已配置）
    public void StartGame()
    {
        if (isStarting)
        {
            Debug.LogWarning("[MainMenuController] 正在进入游戏，忽略重复点击");
            return;
        }
        isStarting = true;

        // 开始游戏专属音效（Fantasy_UI 23）
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxStartGame);

        if (TransitionManager.Instance != null)
        {
            // 复用全局渐变管理器：黑屏渐出 → 加载村庄 → 渐入，与场景间传送效果一致
            TransitionManager.Instance.Transition(() => SceneManager.LoadScene(targetSceneName));
        }
        else
        {
            // 降级兼容：找不到 TransitionManager 时直接加载，仅输出警告不崩溃
            Debug.LogWarning("[MainMenuController] 场景中未找到 TransitionManager，直接加载目标场景（无渐变过渡）");
            SceneManager.LoadScene(targetSceneName);
        }
    }
}