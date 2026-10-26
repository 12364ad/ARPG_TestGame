using UnityEngine;

//挂在村口带 Trigger 碰撞体的区域物体上：玩家进入区域且对话条件全部满足时，
//自动播放指定对话（用于"村口三人聚集"这种不需要按键的剧情触发）
public class DialogueTriggerArea : MonoBehaviour
{
    [Header("进入区域且条件满足时自动播放的对话")]
    public DialogueSO dialogue;

    [Header("是否只触发一次")]
    public bool triggerOnce = true;

    private bool hasTriggered;

    private void Awake()
    {
        //安全网：自动纠正 Is Trigger，防止区域变成一堵隐形墙
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("DialogueTriggerArea[" + gameObject.name +
                           "] 缺少 Collider2D！必须加 BoxCollider2D 并勾选 Is Trigger", this);
            enabled = false;
            return;
        }

        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("DialogueTriggerArea[" + gameObject.name +
                             "] 的碰撞体未勾选 Is Trigger，已自动改为触发器", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggerOnce && hasTriggered) return;
        //跨场景防重复：对话已播过就不再触发（hasTriggered 会随场景重载被重置）
        if (triggerOnce && DialogueHistoryTracker.HasPlayed(dialogue)) return;
        if (!collision.CompareTag("Player")) return;

        if (dialogue == null)
        {
            Debug.LogWarning("DialogueTriggerArea[" + gameObject.name + "] 没有配置对话资产", this);
            return;
        }

        //条件不满足就安静跳过，等玩家达成条件后再进一次即可
        if (!dialogue.IsConditionMet())
        {
            Debug.Log("[剧情触发] " + name + " 条件未满足，暂不播放 " + dialogue.name, this);
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("无法播放剧情：场景中找不到激活的 DialogueManager" +
                             "（检查 DialogueCanvas 是否存在且处于激活状态）。", this);
            return;
        }

        if (DialogueManager.Instance.isDialogueActive) return;

        hasTriggered = true;
        DialogueManager.Instance.StartDialogue(dialogue);
    }
}
