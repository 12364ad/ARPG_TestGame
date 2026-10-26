using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ItemSlotUI : MonoBehaviour//物品槽UI脚本
{
    [Header("物品图标")]
    public UnityEngine.UI.Image icon;//用于格子里物品的图标
    [Header("物品按键")]
    public Button button;//用于槽位的按钮，点击时展示物品详细信息
    [Header("物品数量文本")]
    public TextMeshProUGUI quantityText;//物品数量的文本
    private ItemSlot currentSlot;//当前的格子槽位
    [Header("物品槽编号")]
    public int index;//索引，这个槽位的编号


    public void Set(ItemSlot slot)//设置槽位，传入一个物品格子
    {
        currentSlot = slot;//当前格子为传入的格子
        icon.gameObject.SetActive(true);
        icon.sprite = slot.item.icon;//物品格子里的物品的图标
        quantityText.text = slot.quantity > 1?slot.quantity.ToString() :string.Empty;//数量文本传入物品格子物品的数量，大于1展示可堆叠物品的数量，小于1则不展示

    }

    public void Clear()//清理格子
    {
        currentSlot = null;
        icon.gameObject.SetActive(false);
        quantityText.text = string.Empty;
    }
    public void OnclickButton()//点击按钮
    {
        InventoryManager.Instance.SelectItem(index);
    }
    
}
