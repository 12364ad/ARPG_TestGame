using UnityEngine;

[CreateAssetMenu(fileName = "QuestSO", menuName = "Quest/QuestNode")]
public class QuestSO : ScriptableObject
{
    [Header("任务名称")]
    public string questName;
    [Header("任务描述")]
    [TextArea(3, 5)]
    public string questDescription;

    [Header("任务需求物品（提交时需持有，完成后自动扣除）")]
    public QuestRequiredItem[] requiredItems;

    [Header("任务需求地点（提交前必须已到访，留空则不限制）")]
    public LocationSO[] requiredLocations;

    [Header("任务奖励列表")]
    public QuestReward[] rewards;
}

[System.Serializable]
public class QuestReward
{
    [Header("奖励物品")]
    public ItemData item;
    [Header("数量")]
    public int amount = 1;
}

[System.Serializable]
public class QuestRequiredItem
{
    [Header("需求物品")]
    public ItemData item;
    [Header("需要数量")]
    public int amount = 1;
}
