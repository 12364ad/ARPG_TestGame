using TMPro;
using UnityEngine;
using UnityEngine.UI;

//商店里的一行商品（挂在 item 模板上，克隆出来的每一行都用它）
public class ShopItemUI : MonoBehaviour
{
    [Header("物品图标（item Sprite）")]
    public Image itemIcon;

    [Header("装备名称文本（equip Name）")]
    public TextMeshProUGUI nameText;

    [Header("价格文本（gold Text）")]
    public TextMeshProUGUI priceText;

    [Header("整行的按钮（item 物体上的 Button）")]
    public Button button;

    [Header("行背景（item background，用于选中高亮）")]
    public Image rowBackground;

    [Header("选中时的高亮颜色")]
    public Color selectedColor = new Color(1f, 0.92f, 0.6f, 1f);

    public ItemData Item { get; private set; }//这一行代表的物品

    private int myIndex;
    private ShopManager manager;
    private Color normalColor = Color.white;

    private void Awake()
    {
        EnsureNameText();
    }

    //确保名称文本引用有效：优先按名字找"equip Name"，找不到则退而求其次用价格文本以外的第一个TMP
    private void EnsureNameText()
    {
        if (nameText != null) return;

        TextMeshProUGUI fallback = null;
        foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.name == "equip Name")
            {
                nameText = text;
                return;
            }
            if (fallback == null && text != priceText)
                fallback = text;
        }
        nameText = fallback;
    }

    //由ShopManager调用，把这一行的图标和价格填上
    public void Setup(int index, ItemData item, ShopManager shopManager)
    {
        myIndex = index;
        manager = shopManager;
        Item = item;

        EnsureNameText();//兜底：Awake没执行到时（比如模板一直未激活）这里再补一次

        if (itemIcon != null)
            itemIcon.sprite = item.icon;

        if (nameText != null)
            nameText.text = item.displayName;//装备名称直接取ItemData的名称

        if (priceText != null)
            priceText.text = item.buyPrice.ToString();//价格直接取ItemData的购买价格

        if (rowBackground != null)
        {
            normalColor = rowBackground.color;
            rowBackground.color = normalColor;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()//点击这一行 → 选中它
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxSlotClick);
        if (manager != null)
            manager.SelectItem(myIndex);
    }

    public void SetSelected(bool selected)//选中高亮
    {
        if (rowBackground == null) return;
        rowBackground.color = selected ? selectedColor : normalColor;
    }
}
