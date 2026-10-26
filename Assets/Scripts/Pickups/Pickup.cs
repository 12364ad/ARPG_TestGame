using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    
    public enum PickupType
    {
        Coin,
        HealingPotion,
        weapon,
    }
    [SerializeField] private PickupType pickupType;//设置拾取道具的类型
    [SerializeField] private int value;
    [SerializeField] private ItemData itemData;//拾取后要加入背包的脚本化对象（如sword1）

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            CollectPickup();
        }
    }

    private void CollectPickup()
    {
        switch (pickupType)
        {
            case PickupType.Coin:
                HandleCoinPickup();
                break;
            case PickupType.HealingPotion:
                HandleItemPickup();
                break;
            case PickupType.weapon:
                HandleItemPickup();
                break;

        }
        Destroy(gameObject);
    }

    private void HandleCoinPickup()
    {
        //增加玩家金币数量，数量为该掉落物的value值
        InventoryManager.Instance.AddGold(value);
    }
    //处理道具/消耗品拾取的逻辑，将对应的脚本化对象加入玩家背包
    //掉落物交给背包管理器处理：消耗品进EquipBar快捷栏，装备/材料进普通背包格子
    private void HandleItemPickup()
    {
        if (itemData == null)//没有配置脚本化对象时直接返回，避免空引用
        {
            Debug.LogWarning("掉落物没有配置ItemData：" + gameObject.name);
            return;
        }
        InventoryManager.Instance.AddItem(itemData, true);//调用背包管理器的AddItem方法，把物品加入背包
    }
}
    