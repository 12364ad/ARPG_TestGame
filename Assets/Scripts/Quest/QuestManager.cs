using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//挂在QuestCanvas上：管理任务列表的开关、显示、提交与放弃
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("任务面板根节点（按E键开关）")]
    public GameObject questPanel;

    [Header("左侧任务列表容器（QuestNamebk）")]
    public Transform questListContent;
    [Header("左侧任务条目预制体（QuestSlot，请拖入Project里的Prefab资产）")]
    public GameObject questSlotPrefab;

    [Header("右侧详情文本")]
    public TMP_Text questNameText;
    public TMP_Text questDescriptionText;

    [Header("右侧奖励区域容器（RewardsPanel）")]
    public Transform rewardsContent;
    [Header("奖励条目预制体（RewardSlot，请拖入Project里的Prefab资产）")]
    public GameObject rewardSlotPrefab;

    [Header("按钮")]
    public Button submitButton;
    public Button abandonButton;

    [Header("当前进行中的任务（可在Inspector预先放入测试）")]
    public List<QuestSO> activeQuests = new List<QuestSO>();

    [Header("已提交完成的任务（用于对话门禁：比如交了藤蔓之心才能进行看板娘的下一段对话）")]
    public List<QuestSO> submittedQuests = new List<QuestSO>();

    private QuestSO selectedQuest;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //任务面板跨场景常驻：玩家出图做任务再回村，已接取的任务不能丢失
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        questPanel.SetActive(false);

        submitButton.onClick.AddListener(OnSubmitQuest);
        abandonButton.onClick.AddListener(OnAbandonQuest);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleQuestPanel();
        }
    }

    public void ToggleQuestPanel()
    {
        bool show = !questPanel.activeSelf;
        questPanel.SetActive(show);

        if (show)
            RefreshQuestList();
    }

    public void AddQuest(QuestSO quest)
    {
        if (quest == null)
        {
            Debug.LogWarning("[QuestManager] AddQuest 收到 null 任务，已忽略", this);
            return;
        }
        if (activeQuests.Contains(quest))
        {
            Debug.Log($"[QuestManager] 任务「{quest.questName}」已在列表中，不重复添加", this);
            return;
        }
        //已提交过的任务不可再次接取（防止回村对话列表刷新后重复接任务）
        if (submittedQuests.Contains(quest))
        {
            Debug.Log($"[QuestManager] 任务「{quest.questName}」已提交过，不重复接取", this);
            return;
        }

        activeQuests.Add(quest);
        Debug.Log($"[QuestManager] 已添加任务「{quest.questName}」，当前进行中任务数：{activeQuests.Count}", this);

        //无论面板是否打开都刷新，确保下次打开时看到完整列表
        RefreshQuestList();
    }

    //检查玩家是否满足任务的全部提交条件（需求地点 + 需求物品，全部满足才允许提交）
    public bool CanCompleteQuest(QuestSO quest)
    {
        if (quest == null)
        {
            Debug.LogWarning("[QuestManager] CanCompleteQuest: quest 为 null", this);
            return false;
        }

        //条件一：必须已经到访过指定的地点（比如雪原任务要先去过雪原墓园）
        if (quest.requiredLocations != null && quest.requiredLocations.Length > 0)
        {
            if (LocationHistoryTracker.Instance == null)
            {
                Debug.LogWarning("[QuestManager] CanCompleteQuest: 场景中缺少 LocationHistoryTracker，无法判断地点条件", this);
                return false;
            }

            foreach (var location in quest.requiredLocations)
            {
                if (location == null) continue;
                if (!LocationHistoryTracker.Instance.HasVisited(location))
                {
                    Debug.Log($"[QuestManager] 任务「{quest.questName}」未到访「{location.displayName}」→ 不可提交", this);
                    return false;
                }
            }
        }

        //条件二：必须持有全部需求物品
        if (quest.requiredItems == null || quest.requiredItems.Length == 0)
        {
            Debug.Log($"[QuestManager] 任务「{quest.questName}」无需求物品（requiredItems={(quest.requiredItems == null ? "null" : "空数组")}）→ 可直接提交", this);
            return true;
        }

        Debug.Log($"[QuestManager] 任务「{quest.questName}」有 {quest.requiredItems.Length} 个需求物品，开始校验...", this);

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[QuestManager] CanCompleteQuest: InventoryManager 为 null", this);
            return false;
        }

        foreach (var req in quest.requiredItems)
        {
            if (req.item == null)
            {
                Debug.LogWarning($"[QuestManager] 任务「{quest.questName}」的某个 RequiredItem.item 为 null！说明 Inspector 里没拖或 YAML 没正确反序列化", this);
                continue;
            }

            int have = InventoryManager.Instance.GetItemCount(req.item);
            bool ok = have >= req.amount;
            Debug.Log($"[QuestManager] 校验：需要 {req.item.displayName} × {req.amount}，背包持有 {have} → {(ok ? "✓" : "✗")}", this);
            if (!ok) return false;
        }
        Debug.Log($"[QuestManager] 任务「{quest.questName}」需求物品全部满足 → 可提交", this);
        return true;
    }

    /// <summary>扣除任务所需物品（提交时调用，先 CanCompleteQuest 确认有足够物品再扣）</summary>
    public bool ConsumeRequiredItems(QuestSO quest)
    {
        if (quest == null || quest.requiredItems == null || quest.requiredItems.Length == 0)
            return true;

        bool allRemoved = true;
        foreach (var req in quest.requiredItems)
        {
            if (req.item == null) continue;
            if (!InventoryManager.Instance.RemoveItem(req.item, req.amount))
            {
                Debug.LogWarning($"[QuestManager] 扣除物品失败：{req.item.displayName} × {req.amount}", this);
                allRemoved = false;
            }
        }
        return allRemoved;
    }

    //完成指定任务：从进行中列表移除、加入已提交列表、刷新显示（交任务对话播完后由DialogueManager调用）
    public void CompleteQuest(QuestSO quest)
    {
        if (quest == null || !activeQuests.Contains(quest))
            return;

        // 自动扣除需求物品（如果有的话）
        ConsumeRequiredItems(quest);

        // 发放奖励：对话交任务与面板提交走同一套奖励逻辑，避免交了材料却什么都没拿到
        GrantRewards(quest);

        activeQuests.Remove(quest);

        //标记为已提交，解锁后续对话
        if (!submittedQuests.Contains(quest))
            submittedQuests.Add(quest);

        Debug.Log($"[QuestManager] 任务「{quest.questName}」已完成，加入已提交列表", this);

        if (selectedQuest == quest)
            selectedQuest = null;

        RefreshQuestList();
    }

    //查询某任务是否已提交（供 DialogueSO.IsConditionMet 做对话门禁）
    public bool IsQuestSubmitted(QuestSO quest)
    {
        return quest != null && submittedQuests.Contains(quest);
    }

    public void RefreshQuestList()
    {
        foreach (Transform child in questListContent)
            Destroy(child.gameObject);

        foreach (var quest in activeQuests)
        {
            var slotObj = Instantiate(questSlotPrefab, questListContent);
            slotObj.SetActive(true);

            var nameText = slotObj.GetComponentInChildren<TMP_Text>();
            if (nameText != null)
                nameText.text = quest.questName;

            var btn = slotObj.GetComponent<Button>();
            if (btn == null)
                btn = slotObj.AddComponent<Button>();

            var q = quest;
            btn.onClick.AddListener(() => SelectQuest(q));
        }

        if (activeQuests.Count > 0)
            SelectQuest(activeQuests[0]);
        else
            ClearDetail();
    }

    public void SelectQuest(QuestSO quest)
    {
        selectedQuest = quest;

        questNameText.text = quest.questName;
        questDescriptionText.text = quest.questDescription;

        foreach (Transform child in rewardsContent)
            Destroy(child.gameObject);

        foreach (var reward in quest.rewards)
        {
            if (reward.item == null)
                continue;

            var rewardObj = Instantiate(rewardSlotPrefab, rewardsContent);
            rewardObj.SetActive(true);

            // 优先查找名字为 RewardImage 的子物体 Image（RewardSlot 预制体结构：Root=背景框，子物体=RewardImage + RewardsQuantity）
            Transform rewardImageTransform = rewardObj.transform.Find("RewardImage");
            if (rewardImageTransform != null)
            {
                var icon = rewardImageTransform.GetComponent<Image>();
                if (icon != null)
                    icon.sprite = reward.item.icon;
            }
            else
            {
                // 兜底：如果没找到 RewardImage，就用第一个子物体的 Image（适用于直接用 ItemSlotUI 的情况）
                var slotUI = rewardObj.GetComponentInChildren<ItemSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Set(new ItemSlot { item = reward.item, quantity = reward.amount });
                }
                else
                {
                    var icon = rewardObj.GetComponentInChildren<Image>();
                    if (icon != null)
                        icon.sprite = reward.item.icon;
                }
            }

            // 设置数量文本（找名字为 RewardsQuantity 的 TMP_Text）
            Transform qtyTransform = rewardObj.transform.Find("RewardsQuantity");
            if (qtyTransform != null)
            {
                var countText = qtyTransform.GetComponent<TMP_Text>();
                if (countText != null)
                    countText.text = reward.amount > 1 ? reward.amount.ToString() : string.Empty;
            }
        }

        // 根据是否满足任务需求物品，控制提交按钮是否可点击
        submitButton.interactable = CanCompleteQuest(quest);
        abandonButton.interactable = true;
    }

    //发放任务奖励：金币类奖励直接加到金币数量上，其它物品才放进背包
    private void GrantRewards(QuestSO quest)
    {
        foreach (var reward in quest.rewards)
        {
            if (reward.item == null)
                continue;

            if (reward.item.type == ItemType.Coin)
            {
                InventoryManager.Instance.AddGold(reward.amount);
                continue;
            }

            for (int i = 0; i < reward.amount; i++)
                InventoryManager.Instance.AddItem(reward.item);
        }
    }

    private void OnSubmitQuest()
    {
        if (selectedQuest == null)
            return;

        // 校验：没有需求物品直接可提交；有需求物品则必须持有
        if (!CanCompleteQuest(selectedQuest))
        {
            Debug.LogWarning($"[QuestManager] 任务「{selectedQuest.questName}」需求物品不足，无法提交", this);
            return;
        }

        // 扣除需求物品
        ConsumeRequiredItems(selectedQuest);

        // 发放奖励
        GrantRewards(selectedQuest);

        Debug.Log($"[QuestManager] 手动提交任务「{selectedQuest.questName}」，奖励已发放", this);

        //标记为已提交，解锁后续对话
        if (!submittedQuests.Contains(selectedQuest))
            submittedQuests.Add(selectedQuest);

        activeQuests.Remove(selectedQuest);
        selectedQuest = null;
        RefreshQuestList();
    }

    private void OnAbandonQuest()
    {
        if (selectedQuest == null)
            return;

        activeQuests.Remove(selectedQuest);
        selectedQuest = null;
        RefreshQuestList();
    }

    private void ClearDetail()
    {
        selectedQuest = null;
        questNameText.text = string.Empty;
        questDescriptionText.text = string.Empty;

        foreach (Transform child in rewardsContent)
            Destroy(child.gameObject);

        submitButton.interactable = false;
        abandonButton.interactable = false;
    }
}
