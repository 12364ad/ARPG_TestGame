using UnityEngine;

// 全局 ESC 退出：自动创建常驻对象，不需要在场景或预制体上手动挂载
// 商店这类用 ESC 关闭的界面调用 Consume() 后，本次按键不会再触发退出游戏
public class EscapeToQuit : MonoBehaviour
{
    // 记录最近一次被界面“消费”掉的 ESC 所在帧
    private static int consumedFrame = -1;

    private bool escPressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<EscapeToQuit>() != null) return;

        GameObject go = new GameObject("[EscapeToQuit]");
        go.AddComponent<EscapeToQuit>();
        DontDestroyOnLoad(go);
    }

    // 界面已经处理了这次 ESC（例如关闭商店），本次按键不再退出游戏
    public static void Consume()
    {
        consumedFrame = Time.frameCount;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            escPressed = true;
    }

    // 放到 LateUpdate 再判断：保证同一帧里所有界面的 Update 都先执行完，
    // 商店关闭时才能来得及调用 Consume()，避免“关商店”和“退游戏”同时发生
    private void LateUpdate()
    {
        if (!escPressed) return;
        escPressed = false;

        if (Time.frameCount == consumedFrame) return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
