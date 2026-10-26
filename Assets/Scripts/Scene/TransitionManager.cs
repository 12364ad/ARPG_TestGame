using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 场景切换渐变管理器：负责黑屏 FadeOut → 场景加载 → 黑屏 FadeIn 的平滑过渡
// 使用方式：在任意场景放置一个空物体挂载此脚本即可（推荐放在村庄场景，村庄是 DDOL 玩家所在地）
// 内部会自动创建全屏黑色遮罩 Canvas，无需手动配置 UI
public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("渐变时长（秒）")]
    [Tooltip("从完全透明渐变到完全黑色的时间，也是黑到透明的时间")]
    public float fadeDuration = 1.0f;

    [Header("过渡中黑色停留时长")]
    [Tooltip("完全变黑后停一下再加载下一个场景，给玩家反应时间")]
    public float blackHoldDuration = 0.2f;

    // 内部遮罩
    private CanvasGroup fadeCanvasGroup;
    private Image fadeImage;

    // 是否正在过渡中（防重入）
    private bool isTransitioning;

    // 待执行的场景加载委托（FadeOut 完成后调用）
    private System.Action pendingLoadAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 跨场景保留，否则场景切换时会跟着旧场景一起被销毁
        DontDestroyOnLoad(gameObject);

        // 自动构造全屏黑色遮罩 Canvas
        BuildFadeCanvas();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 外部调用入口：先黑屏淡出（FadeOut），变黑后执行 loadAction（里面调 SceneManager.LoadScene），
    /// 新场景加载完后自动再变亮（FadeIn）
    /// </summary>
    /// <param name="loadAction">场景加载委托，例如 () => SceneManager.LoadScene("Forest")</param>
    public void Transition(System.Action loadAction)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[TransitionManager] 已有过渡在进行中，忽略本次请求");
            return;
        }
        isTransitioning = true;
        pendingLoadAction = loadAction;
        StartCoroutine(TransitionCoroutine());
    }

    private IEnumerator TransitionCoroutine()
    {
        // 1. FadeOut：透明 → 全黑
        yield return FadeOutCoroutine();

        // 2. 黑屏稍停一下
        if (blackHoldDuration > 0f)
            yield return new WaitForSeconds(blackHoldDuration);

        // 3. 执行场景加载（同步 LoadScene，加载完后 Unity 会切场景并触发 sceneLoaded 回调）
        pendingLoadAction?.Invoke();
        pendingLoadAction = null;
    }

    /// <summary>
    /// 场景加载完成回调：此时场景已切换、玩家已被 Bootstrap 放到 Spawn 点，
    /// 再执行 FadeIn 让玩家看到新场景
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 只有在过渡中才执行 FadeIn，正常场景加载（比如从 Editor Play 进入）不触发
        if (!isTransitioning) return;

        // 确保遮罩存在且 alpha=1（LoadScene 会销毁旧场景，遮罩是在旧场景里创建的，
        // 但 TransitionManager 本身可能被 DDOL 保留；如果遮罩跟着旧场景被销毁了，就重建一份）
        if (fadeCanvasGroup == null)
            BuildFadeCanvas();

        StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeInCoroutine()
    {
        // 确保从全黑开始
        fadeCanvasGroup.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        isTransitioning = false;
    }

    // ---------------------------------------------------------------
    // 内部：自动构造全屏黑色遮罩 Canvas
    // ---------------------------------------------------------------
    private void BuildFadeCanvas()
    {
        GameObject canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform);

        // Canvas 组件
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // 最顶层，盖住所有 UI

        // CanvasScaler：让 Canvas 自适应屏幕
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // GraphicRaycaster：让 UI 能接收射线（过渡中阻止点击）
        canvasGO.AddComponent<GraphicRaycaster>();

        // Panel（Image）：全屏黑色
        GameObject panelGO = new GameObject("FadePanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        fadeImage = panelGO.AddComponent<Image>();
        fadeImage.color = Color.black;

        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // CanvasGroup：控制透明度 + 射线阻断
        fadeCanvasGroup = panelGO.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;
    }
}
