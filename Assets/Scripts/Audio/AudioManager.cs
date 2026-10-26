using UnityEngine;
using UnityEngine.SceneManagement;

// 全局音效管理器：单例 + DontDestroyOnLoad，负责 BGM 切换和 SFX 播放
// 使用方式：AudioManager.Instance.PlayBGM(bgmClip); AudioManager.Instance.PlaySFX(sfxClip);
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    private static bool bootstrapTried;

    // 用属性而不是自动字段：万一单例被意外销毁，下次访问会自动重建，
    // 避免 Instance 指向已销毁对象导致所有音效静默失效
    public static AudioManager Instance
    {
        get
        {
            if (instance == null) EnsureExists();
            return instance;
        }
    }

    [Header("音量")]
    [Range(0f, 1f)] public float bgmVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("场景 BGM 配置")]
    [Tooltip("菜单场景名 → 对应 BGM")]
    public string menuSceneName = "Menu";
    public AudioClip menuBGM;
    [Tooltip("游戏场景（村庄/森林/雪原/地牢）→ 对应 BGM")]
    public AudioClip gameBGM;

    [Header("SFX 资源（战斗）")]
    public AudioClip sfxSlash;          // 挥砍
    public AudioClip sfxImpact;         // 击中肉体
    public AudioClip sfxEnemyDeath;     // 敌人死亡
    public AudioClip sfxPlayerHurt;     // 玩家受击
    public AudioClip sfxPlayerHeal;     // 治疗

    [Header("SFX 资源（UI）")]
    public AudioClip sfxButtonMenu;     // 菜单界面按钮
    public AudioClip sfxButtonGame;     // 游戏内按钮
    public AudioClip sfxEquip;          // 装备
    public AudioClip sfxUnequip;        // 卸下
    public AudioClip sfxUseItem;        // 使用物品
    public AudioClip sfxHover;          // 按钮悬停
    [Header("SFX 资源（场景）")]
    public AudioClip sfxStartGame;      // 开始游戏按钮（Fantasy_UI 23）
    public AudioClip sfxDialogueAdvance;// 对话推进（按空格/点击继续）
    public AudioClip sfxSlotClick;      // 点击背包/商店格子

    // 保持原有音源结构：两个音源都挂在管理器自身，配置与最初版本一致
    private AudioSource bgmSource;
    private AudioSource sfxSource;

    // 手动调用 StopBGM 后不再自动续播；切场景/播新 BGM 时复位
    private bool bgmManuallyStopped;
    // BGM 自愈重试冷却，避免每帧重复 Play 把音效顶成连续杂音
    private float bgmRetryTimer;

    //优先使用场景里已经配置好的 AudioManager（Menu 场景那个就是这个），
    //只有当前场景完全没有时才用 Resources 里的预制体兜底。
    //改到 AfterSceneLoad 执行，保证音频系统和场景对象都已初始化完成，
    //避免抢先创建的单例把场景里配好的实例顶掉
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (instance != null || bootstrapTried) return;
        bootstrapTried = true;

        GameObject prefab = Resources.Load<GameObject>("AudioManager");
        if (prefab != null)
            Instantiate(prefab);//Instantiate会立即执行Awake：设置实例 + DontDestroyOnLoad
        else
            Debug.LogWarning("[AudioManager] 找不到 Resources/AudioManager.prefab，无法自动创建");
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // 场景里已经有管理器了，这个重复物体直接销毁，避免两套音源同时工作
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // 自动创建两个 AudioSource，无需手动配置（与原实现一致）
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
    }

    private void Start()
    {
        // Unity 生命周期：sceneLoaded 事件在 Awake/OnEnable 之前触发，
        // 所以首次进入场景时 OnSceneLoaded 不会被调，这里手动补一次
        Debug.Log($"[AudioManager] Start() | 当前场景: {SceneManager.GetActiveScene().name} | 实例已就绪");
        PlayBGMForCurrentScene();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            // 允许下次访问时重新创建，否则单例销毁后 Instance 会永远为 null
            bootstrapTried = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AudioManager] OnSceneLoaded | 新场景: {scene.name}");
        bgmManuallyStopped = false;
        StartCoroutine(PlayBGMNextFrame());
    }

    //延迟一帧再切BGM：sceneLoaded回调触发时Unity还在做场景切换收尾，
    //此刻给AudioSource换clip并立即Play，大场景（如村庄）下可能静默失败，
    //表现为菜单音乐停掉后游戏音乐不响，等下一帧引擎稳定后再播即可规避
    private System.Collections.IEnumerator PlayBGMNextFrame()
    {
        yield return null;
        PlayBGMForCurrentScene();
    }

    //BGM 自愈：只要设置过 BGM 且没有手动停止，一旦发现它意外停住就重新播放。
    //这样即使播放中途被引擎/其他音频行为打断，背景音乐也能立刻恢复而不是永久静默
    private void Update()
    {
        if (bgmManuallyStopped) return;
        if (bgmSource == null || bgmSource.clip == null) return;
        if (bgmSource.isPlaying)
        {
            bgmRetryTimer = 0f;
            return;
        }

        // 冷却 1 秒再重试：避免每帧重复 Play 把音乐顶成连续杂音
        bgmRetryTimer += Time.unscaledDeltaTime;
        if (bgmRetryTimer < 1f) return;
        bgmRetryTimer = 0f;

        bgmSource.Play();
        Debug.LogWarning($"[AudioManager] BGM 意外中断，已自动续播: {bgmSource.clip.name}");
    }

    private void PlayBGMForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool isMenu = sceneName == menuSceneName;
        AudioClip clip = isMenu ? menuBGM : gameBGM;

        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] 场景 {sceneName} 的 BGM 未配置" +
                             $"（{(isMenu ? "menuBGM" : "gameBGM")} 为空），保持当前音乐");
            return;
        }

        Debug.Log($"[AudioManager] 场景 {sceneName} → 播放 BGM: {clip.name}");
        PlayBGM(clip);
    }

    /// <summary>播放 BGM（如果是同一首且正在播放则忽略，避免重启）</summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource == null) return;

        bgmManuallyStopped = false;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    /// <summary>停止 BGM</summary>
    public void StopBGM()
    {
        bgmManuallyStopped = true;
        if (bgmSource != null) bgmSource.Stop();
    }

    /// <summary>播放音效（One-shot，多个可叠加）</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (sfxSource == null) return;
        Debug.Log($"[AudioManager] PlaySFX: {clip.name}");   // 排查音效用日志
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>播放音效（指定音量）</summary>
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;
        if (sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }
}
