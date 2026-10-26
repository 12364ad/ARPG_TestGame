using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Talk : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    public Animator interactAnim;

    public List<DialogueSO> conversations;
    public DialogueSO currentConversation;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
    }

    private void OnEnable()//启用这个组件时运行
    {
        //NPC3这类纯对话NPC没有Rigidbody2D，访问前必须判空，否则抛MissingComponentException
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        if (anim != null && anim.gameObject.activeInHierarchy)
            anim.Play("Idle");
        if (interactAnim != null && interactAnim.gameObject.activeInHierarchy)
            interactAnim.Play("Open");
    }

    private void OnDisable()
    {
        if (interactAnim != null && interactAnim.gameObject.activeInHierarchy)
            interactAnim.Play("Close");
        if (rb != null)
            rb.isKinematic = false;
    }

    void Update()
    {
        if (Input.GetButtonDown("Interact"))
        {
            //对话总控缺失时直接拦截，避免空引用刷屏，并提示场景里丢了什么
            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("无法对话：场景中找不到激活的 DialogueManager（检查 DialogueCanvas 是否存在且处于激活状态）。", this);
                return;
            }

            if (DialogueManager.Instance.isDialogueActive)
                DialogueManager.Instance.AdvanceDialogue();
            else
            {
                CheckForNewConversation();
                //没有满足条件的对话时直接跳过，避免对空对话反复弹警告
                if (currentConversation != null)
                    DialogueManager.Instance.StartDialogue(currentConversation);
            }
        }
    }

    private void CheckForNewConversation()
    {
        currentConversation = null;//先清空，避免没有任何对话满足条件时，重复播放上一次的旧对话

        for (int i = 0; i < conversations.Count; i++)
        {
            var convo = conversations[i];
            if (convo == null) continue;

            //一次性对话只要播过就永久跳过（跨场景也生效）：
            //场景列表每次回村都会重建，不跳过的话任务会被重新接取
            if (!convo.repeatable && DialogueHistoryTracker.HasPlayed(convo))
            {
                conversations.RemoveAt(i);
                i--;
                continue;
            }

            if (convo.IsConditionMet())
            {
                //取第一个满足条件的对话并立即break，保证选取结果唯一确定；
                //一次性对话播完即从列表消耗，可重复对话（门禁/提示）留在列表里
                if (!convo.repeatable)
                    conversations.RemoveAt(i);
                currentConversation = convo;
                break;
            }
        }
    }
}
