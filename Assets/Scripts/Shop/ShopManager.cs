using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//商店管理器：根据NPC配置的ItemData列表生成商品行，处理选中与购买
//挂在 Shop Canvas 上，并在 Inspector 里连好商店UI
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("商店面板（Shop Panel）")]
    public GameObject shopPanel;

    [Header("商品行的父物体（Background）")]
    public Transform itemContainer;

    [Header("商品行模板（item，脚本会自动把它设为未激活当模板）")]
    public GameObject itemRowPrefab;

    [Header("购买按钮（Buy Button）")]
    public Button buyButton;

    private List<ItemData> currentItems;//当前商店售卖的物品
    private readonly List<ShopItemUI> rows = new List<ShopItemUI>();//界面上生成的所有商品行
    private int selectedIndex = -1;//当前选中的商品下标
    private int lastToggleFrame = -1;//最近一次开/关商店所在的帧，用于防止多个NPC同一帧抢按键

    public bool IsOpen => shopPanel != null && shopPanel.activeSelf;//商店当前是否打开

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        //把模板行藏起来，之后用它的克隆体来显示商品
        if (itemRowPrefab != null)
            itemRowPrefab.SetActive(false);

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyButton);
        }

        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    private void Update()
    {
        //打开时按 ESC 关闭商店
        if (shopPanel != null && shopPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseShop();
            //告诉全局 ESC 退出逻辑：这次按键已经被商店用掉了，不要再触发退出游戏
            EscapeToQuit.Consume();
        }
    }

    //打开某个NPC的商店：传入它售卖的物品列表
    public void OpenShop(List<ItemData> items)
    {
        //同一帧内已经有一次开/关操作，忽略后面NPC的请求，避免站两个NPC中间时关闭后又被另一个打开
        if (Time.frameCount == lastToggleFrame) return;

        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("OpenShop：这个商店没有配置任何物品");
            return;
        }

        currentItems = items;
        selectedIndex = -1;

        BuildRows();

        if (shopPanel != null)
            shopPanel.SetActive(true);

        lastToggleFrame = Time.frameCount;
    }

    public void CloseShop()
    {
        //同上：这一帧已经切换过商店状态，不再重复处理
        if (Time.frameCount == lastToggleFrame) return;

        if (shopPanel != null)
            shopPanel.SetActive(false);

        lastToggleFrame = Time.frameCount;
    }

    //按当前物品列表生成商品行（改了配置重新打开，UI就跟着变）
    private void BuildRows()
    {
        foreach (var row in rows)
            if (row != null) Destroy(row.gameObject);
        rows.Clear();

        if (itemContainer == null || itemRowPrefab == null || currentItems == null)
            return;

        for (int i = 0; i < currentItems.Count; i++)
        {
            ItemData item = currentItems[i];
            if (item == null) continue;

            GameObject rowObj = Instantiate(itemRowPrefab, itemContainer);
            rowObj.SetActive(true);

            ShopItemUI rowUI = rowObj.GetComponent<ShopItemUI>();
            if (rowUI == null) rowUI = rowObj.AddComponent<ShopItemUI>();
            rowUI.Setup(i, item, this);
            rows.Add(rowUI);
        }
    }

    public void SelectItem(int index)//选中某一行商品
    {
        if (index < 0 || index >= rows.Count) return;
        selectedIndex = index;

        for (int i = 0; i < rows.Count; i++)
            rows[i].SetSelected(i == index);
    }

    //购买按钮点击事件
    public void OnBuyButton()
    {
        if (selectedIndex < 0 || selectedIndex >= rows.Count)
        {
            Debug.Log("请先选择一个商品");
            return;
        }

        ItemData item = rows[selectedIndex].Item;//取选中行对应的物品
        if (item == null) return;
        if (InventoryManager.Instance == null) return;

        if (InventoryManager.Instance.gold < item.buyPrice)
        {
            Debug.Log("金币不足，无法购买：" + item.displayName);
            return;
        }

        InventoryManager.Instance.AddGold(-item.buyPrice);//扣钱
        InventoryManager.Instance.AddItem(item);//放进背包
        Debug.Log("已购买：" + item.displayName);
    }
}
