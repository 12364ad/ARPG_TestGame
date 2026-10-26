using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueHistoryTracker : MonoBehaviour
{
    public static DialogueHistoryTracker Instance;
    private readonly List<ActorSO> spokenNPCs = new List<ActorSO>();

    //已播放过的一次性对话（静态集合，跨场景常驻，不随场景切换清空）
    //用于修复"回村后NPC对话列表重建、任务可以重复接取"的问题
    private static readonly HashSet<string> playedDialogues = new HashSet<string>();

    public static void RecordPlayed(DialogueSO dialogue)
    {
        if (dialogue != null)
            playedDialogues.Add(dialogue.name);
    }

    public static bool HasPlayed(DialogueSO dialogue)
    {
        return dialogue != null && playedDialogues.Contains(dialogue.name);
    }

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RecordNPC(ActorSO actorSO)
    {
        spokenNPCs.Add(actorSO);

        Debug.Log("Just spoke to" +  actorSO.actorName);
    }

    public bool HasSpokenWith(ActorSO actorSO)
    {
        return spokenNPCs.Contains(actorSO);
    }
}
