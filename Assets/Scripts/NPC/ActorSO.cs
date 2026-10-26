using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="ActorSO",menuName ="Dialogue/NPC")]
public class ActorSO : ScriptableObject
{
    [Header("角色名字（显示在对话框上的名字）")]
    public string actorName;
    [Header("角色头像（显示在对话框上的立绘）")]
    public Sprite portrait;
    
}
