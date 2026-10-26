using UnityEngine;
using UnityEngine.SceneManagement;

//挂在带 Trigger Collider2D 的区域物体上。玩家走进后切换到目标场景，
//并在新场景里把玩家放到指定 Spawn 空物体位置
public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("目标场景（必须在 Build Settings 里勾选）")]
    public string targetSceneName;

    [Header("新场景中找哪个 Spawn 空物体（名字必须完全一致）")]
    public string spawnPointName;

    [Header("进入触发区时弹出提示（可选）")]
    public string hintText;

    private bool isTransitioning; //防重复触发

    private Collider2D triggerCol;

    private void Awake()
    {
        //安全网：自动纠正 Is Trigger，防止区域变成一堵隐形墙
        triggerCol = GetComponent<Collider2D>();
        if (triggerCol == null)
        {
            Debug.LogError("SceneTransitionTrigger[" + gameObject.name +
                           "] 缺少 Collider2D！必须给这个物体加 BoxCollider2D 或 CircleCollider2D 并勾 Is Trigger", this);
            enabled = false;
            return;
        }

        if (!triggerCol.isTrigger)
        {
            triggerCol.isTrigger = true;
            Debug.LogWarning("SceneTransitionTrigger[" + gameObject.name +
                             "] 的碰撞体未勾选 Is Trigger，已自动改为触发器", this);
        }
    }

    private void Start()
    {
        if (triggerCol == null) return;

        string sceneOK = SceneExistsInBuild(targetSceneName) ? "✅" : "❌ (场景没加到 Build Settings)";
        Debug.Log("[Trigger就绪] " + name + " → " + targetSceneName
                  + " Spawn=" + spawnPointName + " Collider=" + triggerCol.GetType().Name + sceneOK, this);
    }

    private bool SceneExistsInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTransitioning)
        {
            Debug.Log("[Trigger被撞] 但 isTransitioning=true 正在加载中，忽略第二次", this);
            return;
        }

        if (!collision.CompareTag("Player"))
        {
            Debug.Log("[Trigger被撞] 但对象 tag=" + collision.tag + "（必须是 Player），忽略。对象是：" + collision.name, this);
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[Trigger] " + name + " 未配置 Target Scene Name，Inspector 里补上！", this);
            return;
        }

        Debug.Log("[切图触发] " + name + " 收到 Player 进入 → 准备加载 " + targetSceneName);

        if (!string.IsNullOrEmpty(hintText))
            Debug.Log("[切图提示] " + hintText);

        isTransitioning = true;

        SceneTransitionBootstrap.PendingSpawnPoint = spawnPointName;

        // 如果场景里有 TransitionManager，就走黑屏渐变过渡；
        // 否则降级为直接加载（保证没配也不崩）
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.Transition(() =>
            {
                //加载后 Unity 会销毁当前场景。如果目标场景的 Spawn 空物体找不到，
                //Bootstrap 会 Log 警告并让玩家停在原地
                SceneManager.LoadScene(targetSceneName);
            });
        }
        else
        {
            Debug.LogWarning("[Trigger] 场景里找不到 TransitionManager，跳过渐变直接加载 —— 建议在村庄场景放一个空物体挂上 TransitionManager 脚本");
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
