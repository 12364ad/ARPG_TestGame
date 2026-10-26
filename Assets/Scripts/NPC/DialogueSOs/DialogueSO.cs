using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="DialogueSO",menuName ="Dialogue/DialogueNode")]
public class DialogueSO : ScriptableObject
{
    [Header("对话内容（按顺序逐句显示）")]
    public DialogueLine[] lines;
    [Header("对话选项（发言结束后出现的分支按钮，可留空）")]
    public DialogueOption[] options;

    [Header("条件要求")]
    [Tooltip("需要已经对话过的NPC（列表内全部满足）")]
    public ActorSO[] requiredNPCs;

    [Tooltip("需要背包中持有的物品（列表内全部满足）")]
    public ItemData[] requiredItems;

    [Tooltip("需要已经到访过的地点（列表内全部满足）")]
    public LocationSO[] requiredLocations;

    [Tooltip("需要已经提交完成的任务（列表内全部满足）")]
    public QuestSO[] requiredQuests;

    [Tooltip("这些任务已提交时条件不成立（用于门禁对话：任务做完后不再说阻拦台词）")]
    public QuestSO[] blockedByQuests;

    [Header("播放方式")]
    [Tooltip("勾选后该对话可重复播放（门禁/提示类台词），不勾选则播一次永久消耗")]
    public bool repeatable;

    [Header("对话结束后的任务联动")]
    [Tooltip("该对话播完后接取的任务（留空则不接取）")]
    public QuestSO questToStart;

    [Tooltip("该对话播完后完成的任务（留空则不完成）")]
    public QuestSO questToComplete;

    public bool IsConditionMet()
    {
        //条件一：是否已经和指定的NPC对话过
        if (requiredNPCs != null && requiredNPCs.Length > 0)
        {
            if (DialogueHistoryTracker.Instance == null)
            {
                Debug.LogWarning("场景中缺少 DialogueHistoryTracker，无法判断NPC对话条件。", this);
                return false;
            }
            foreach (var npc in requiredNPCs)
            {
                if (npc != null && !DialogueHistoryTracker.Instance.HasSpokenWith(npc))
                {
                    return false;
                }
            }
        }

        //条件二：背包里是否持有指定物品
        if (requiredItems != null && requiredItems.Length > 0)
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("场景中缺少 InventoryManager，无法判断物品持有条件。", this);
                return false;
            }
            foreach (var item in requiredItems)
            {
                if (item != null && !InventoryManager.Instance.HasItem(item))
                {
                    return false;
                }
            }
        }

        //条件三：是否已经到访过指定地点
        if (requiredLocations != null && requiredLocations.Length > 0)
        {
            if (LocationHistoryTracker.Instance == null)
            {
                Debug.LogWarning("场景中缺少 LocationHistoryTracker，无法判断地点到访条件。", this);
                return false;
            }
            foreach (var location in requiredLocations)
            {
                if (location != null && !LocationHistoryTracker.Instance.HasVisited(location))
                {
                    return false;
                }
            }
        }

        //条件四：是否已经提交了指定任务（对话门禁核心：比如交了藤蔓之心才能进行看板娘的下一段对话）
        if (requiredQuests != null && requiredQuests.Length > 0)
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("场景中缺少 QuestManager，无法判断任务提交条件。", this);
                return false;
            }
            foreach (var quest in requiredQuests)
            {
                if (quest != null && !QuestManager.Instance.IsQuestSubmitted(quest))
                {
                    return false;
                }
            }
        }

        //条件五：黑名单任务——这些任务已提交时条件不成立（门禁对话专用）
        if (blockedByQuests != null && blockedByQuests.Length > 0)
        {
            if (QuestManager.Instance != null)
            {
                foreach (var quest in blockedByQuests)
                {
                    if (quest != null && QuestManager.Instance.IsQuestSubmitted(quest))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

}

[System.Serializable]
public class DialogueLine
{
    [Header("说话者")]
    public ActorSO speaker;
    [Header("说话文本")]
    [TextArea(3,5)]public string text;
}

[System.Serializable]
public class DialogueOption
{
    [Header("选项文字")]
    public string optionText;
    [Header("选择该选项后跳转的下一段对话（留空则结束对话）")]
    public DialogueSO nextDialogue;
}
