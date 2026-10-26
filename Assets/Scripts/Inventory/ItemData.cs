using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public enum ItemType//物品类型
{
    EquipableWeapon,//装备类型物品武器
    EquipableArmor,//装备类型物品护甲
    EquipableBoots,//装备类型物品靴子
    EquipableHelm,//装备类型物品头盔
    EquipableRing,//装备类型物品饰品戒指
    EquipableNeglec,//装备类型物品饰品项链
    EquipableShield,//装备类型物品护盾
    EquipableBow,//装备类型物品弓
    EquipableArrow,//装备类型物品箭
    EquipableGlove,//装备类型法杖
    Consumable,//消耗品，例如药水
    Coin,
    exp,
    Resource,//资源类型物品
}


public enum StatsType//状态类型
{
    HP,//生命值
    HPR,//生命恢复
    MP,//法力值
    MPR,//法力恢复
    AD,//物理攻击力
    DF,//防御力
    CR,//暴击率
    CRD,//暴击伤害
    PD,//物理伤害
    XP//经验值
}

public enum ItemRarity//稀有度
{
    Common,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(menuName ="New Item")]//创建资源菜单，菜单名 = New Item
public class ItemData : ScriptableObject
{
    [Header("物品ID（唯一标识，用于存档与查找）")]
    public string id;
    [Header("背包物品名称")]
    public string displayName;
    [Header("物品描述")]
    [TextArea]public string description;    
    [Header("物品图标")]
    public Sprite icon;
    [Header("掉落物")]
    public GameObject propPrefab;
    [Header("物品类型")]
    public ItemType type;
    [Header("购买价格")]
    public int buyPrice;
    [Header("出售价格")]
    public int sellPrice;
    [Header("拆解碎片")]
    public int dismantlePrice;
    [Header("是否能堆叠")]
    public bool canStack;
    [Header("最大堆叠数")]
    public int maxStackAmount;

    [Header("物品状态信息")]
    public ItemDataStats[] statsOfItem;

    [Header("稀有度")]
    public ItemRarity raity;

}

[System.Serializable]
public class ItemDataStats
{
    [Header("属性类型")]
    public StatsType type;//状态
    [Header("属性数值")]
    public float value;//数值
}
