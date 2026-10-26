using System.Collections.Generic;
using UnityEngine;

//挂在NPC身上：配置这个NPC售卖的物品，并提供一个打开商店的方法
//价格直接使用每个ItemData自己的购买价格(buyPrice)
public class ShopNPC : MonoBehaviour
{
    [Header("该NPC售卖的物品")]
    public List<ItemData> shopItems = new List<ItemData>();

    private bool playerInRange;

    private void Update()
    {
        if (!playerInRange) return;
        //按Input Manager里配置的"Shop"键：商店关着就打开，开着就关闭
        if (Input.GetButtonDown("Shop"))
        {
            if (ShopManager.Instance != null && ShopManager.Instance.IsOpen)
                ShopManager.Instance.CloseShop();
            else
                OpenShop();
        }
    }

    //在交互（比如对话结束后）时调用它，打开这个NPC的商店
    public void OpenShop()
    {
        if (shopItems == null || shopItems.Count == 0)
        {
            Debug.LogWarning(gameObject.name + " 没有配置任何售卖的物品");
            return;
        }
        if (ShopManager.Instance != null)
            ShopManager.Instance.OpenShop(shopItems);
    }

    private void OnTriggerEnter2D(Collider2D collision)//NPC上有Trigger碰撞体时，检测玩家进入
    {
        if (collision.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            playerInRange = false;
    }
}
