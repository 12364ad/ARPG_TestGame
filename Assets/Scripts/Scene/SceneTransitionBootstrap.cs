using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

//挂在玩家身上（玩家会被 DontDestroyOnLoad）：监听场景加载事件，
//把刚跨场景过来的玩家放到新场景里指定的 Spawn 空物体位置，
//并自动把本场景里所有 Cinemachine Virtual Camera 的 Follow 绑定到这个玩家
public class SceneTransitionBootstrap : MonoBehaviour
{
    //SceneTransitionTrigger 在切图前写入这里，场景加载完后读取并清空
    public static string PendingSpawnPoint;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //--- 1. 放玩家到 Spawn 点 ---
        if (!string.IsNullOrEmpty(PendingSpawnPoint))
        {
            Transform spawn = GameObject.Find(PendingSpawnPoint)?.transform;
            if (spawn != null)
            {
                transform.position = spawn.position;
                transform.rotation = spawn.rotation;
            }
            else
            {
                Debug.LogWarning("切图完成，但在新场景 \"" + scene.name +
                                 "\" 里找不到名为 \"" + PendingSpawnPoint +
                                 "\" 的 Spawn 空物体 —— 玩家停留在原坐标", this);
            }
            PendingSpawnPoint = null;
        }

        //--- 2. 自动把本场景里所有 Cinemachine Virtual Camera 的 Follow 绑定到玩家 ---
        //玩家是 DDOL 跨场景保留的，新场景里的 Virtual Camera 不会认识它，
        //所以每次场景加载后都手动找一次并绑定
        var cameras = FindObjectsOfType<CinemachineVirtualCamera>(includeInactive: true);
        foreach (var vcam in cameras)
        {
            if (vcam.Follow != transform)
            {
                vcam.Follow = transform;
                Debug.Log("[摄像机绑定] " + vcam.name + ".Follow → " + name +
                          "  (场景 " + scene.name + ")", vcam);
            }
            //注意：2D 正交相机不要绑 LookAt！Aim=Composer 会尝试把相机"瞄向"目标导致旋转错位
            //所以 LookAt 永远不动，保持场景里原本的配置（通常是 None）
        }
        if (cameras.Length == 0)
        {
            Debug.Log("[摄像机绑定] 场景 " + scene.name + " 里没有 CinemachineVirtualCamera，跳过");
        }
    }
}
