using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI引用")]
    public CanvasGroup canvasGroup;
    public Image portrait;
    public TMP_Text actorName;
    public TMP_Text dialogueText;
    public Button[] choiceButtons;

    private DialogueSO currentDialogue;
    private int dialogueIndex;

    private GameObject equipBar;//底部快捷栏：对话期间临时隐藏，避免遮挡对话文字

  

    public bool isDialogueActive;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //对话系统跨场景常驻：已到访地点、已对话NPC的记录必须在切换场景后仍然保留
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        foreach(var button in choiceButtons)
            button.gameObject.SetActive(false);
        
    }

    public void StartDialogue(DialogueSO dialogueSO)//传入一段对话
    {
        if (dialogueSO == null || dialogueSO.lines == null || dialogueSO.lines.Length == 0)
        {
            Debug.LogWarning("无法开始对话：Dialogue SO 未赋值，或该对话资产没有任何台词。", this);
            return;
        }

        currentDialogue = dialogueSO;
        dialogueIndex = 0;
        isDialogueActive = true;
        DialogueHistoryTracker.RecordPlayed(dialogueSO);//记录已播放，一次性对话跨场景不会重播
        SetEquipBarActive(false);//对话开始时隐藏底部快捷栏，避免遮挡
        ShowDialogue();
    }

    public void AdvanceDialogue()
    {
        if (currentDialogue == null)//上次异常导致对话数据丢失时自动复位，避免对话状态卡死
        {
            Debug.LogWarning("对话数据丢失，已重置对话状态。", this);
            EndDialogue();
            return;
        }

        // 对话推进音效（按空格/点击继续时播放）
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxDialogueAdvance);

        if (dialogueIndex < currentDialogue.lines.Length)
            ShowDialogue();
        else
            ShowChoices();
       
    }

    private void ShowDialogue()
    {
        DialogueLine line = currentDialogue.lines[dialogueIndex];

        DialogueHistoryTracker.Instance.RecordNPC(line.speaker);

        portrait.sprite = line.speaker.portrait;
        actorName.text = line.speaker.actorName;
        
        dialogueText.text = line.text;

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        dialogueIndex++;
    }

    private void ShowChoices()
    {
        ClearChoices();
        if (currentDialogue.options.Length > 0)
        {
            for(int i = 0; i < currentDialogue.options.Length; i++)
            {
                var option = currentDialogue.options[i];
                choiceButtons[i].GetComponentInChildren<TMP_Text>().text = option.optionText;
                choiceButtons[i].gameObject.SetActive(true);

                choiceButtons[i].onClick.AddListener(() => ChooseOption(option.nextDialogue));
            }
        }
        else
        {
            choiceButtons[0].GetComponentInChildren<TMP_Text>().text = "结束";
            choiceButtons[0].onClick.AddListener(EndDialogue);
            choiceButtons[0].gameObject.SetActive(true);
        }
    }

    private void ChooseOption(DialogueSO dialogueSO)//选择选项方法
    {
        if (dialogueSO == null)
            EndDialogue();
        else
        {
            ClearChoices();
            StartDialogue(dialogueSO);
        }
    }

    private void EndDialogue()//对话结束，关闭面板并重置状态
    {
        dialogueIndex = 0;
        isDialogueActive = false;

        SetEquipBarActive(true);//对话结束时恢复底部快捷栏显示

        //对话播完后的任务联动：接任务对话发放任务，交任务对话完成任务
        if (currentDialogue != null)
        {
            if (QuestManager.Instance == null)
                Debug.LogWarning("[任务发放失败] QuestManager.Instance 为 null——请确认场景中有激活的 QuestCanvas", this);
            else
            {
                if (currentDialogue.questToComplete != null)
                {
                    // 校验：交任务对话也必须持有需求物品
                    if (QuestManager.Instance.CanCompleteQuest(currentDialogue.questToComplete))
                    {
                        Debug.Log($"[任务完成] 对话「{currentDialogue.name}」→ 完成任务「{currentDialogue.questToComplete.questName}」", this);
                        QuestManager.Instance.CompleteQuest(currentDialogue.questToComplete);
                    }
                    else
                    {
                        Debug.LogWarning($"[任务完成被拦截] 对话「{currentDialogue.name}」→ 任务「{currentDialogue.questToComplete.questName}」需求物品不足", this);
                    }
                }
                if (currentDialogue.questToStart != null)
                {
                    Debug.Log($"[任务发放] 对话「{currentDialogue.name}」→ 接取任务「{currentDialogue.questToStart.questName}」", this);
                    QuestManager.Instance.AddQuest(currentDialogue.questToStart);
                }
            }
        }
        ClearChoices();

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
       
    }

    private void ClearChoices()
    {
        foreach (var button in choiceButtons)
        {
            button.gameObject.SetActive(false);
            button.onClick.RemoveAllListeners();
        }
    }

    //查找并切换底部快捷栏的显隐；找不到时静默跳过（跨场景或其他地图无 EquipBar 时不报错）
    private void SetEquipBarActive(bool active)
    {
        if (equipBar == null)
        {
            equipBar = GameObject.Find("EquipBar");
            if (equipBar == null)
            {
                var canvas = GameObject.Find("Equip Canvas");
                if (canvas != null)
                {
                    var t = canvas.transform.Find("EquipBar");
                    if (t != null) equipBar = t.gameObject;
                }
            }
        }
        if (equipBar != null) equipBar.SetActive(active);
    }
   
}
