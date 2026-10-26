using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryManager : MonoBehaviour//库存管理器
{
    // 1. `inventoryPanel`：背包窗口本体（开关窗口）
    //2. `slots[]`：**后台物品数据仓库**
    //3. `uiSlots[]`：**界面上看得见的格子 UI**
    //4. `selectedItem + selectedItemIndex`：记录你当前点击选中哪个物品
    //5. 4 个 TMP 文本：物品详情描述面板
    //6. 3 个按钮：物品操作（使用 / 装备 / 卸下）
    //7. `goldText`：金币 UI 文字

    [Header("Item Database")]
    public List<ItemData> allItems;//脚本化对象的列表

    private Dictionary<string, ItemData> itemDictionary;//把项目保存到字典中
    public static InventoryManager Instance;

    [Header("装备属性加成")]// ===== 装备带来的总属性加成（所有已穿戴装备累加）=====
    public int equipmentHP;                      // 装备提供生命值上限
    public int equipmentHPR;                     // 装备提供生命恢复（HP Regen）
    public int equipmentMP;                      // 装备提供魔力上限
    public int equipmentMPR;                     // 装备提供魔力恢复（MP Regen）
    public int equipmentAD;                      // 装备提供攻击力（Attack Damage）
    public int equipmentDef;                     // 装备提供防御力（Defense）
    public int equipmentCR;                      // 装备提供暴击率（Crit Rate）
    public int equipmentCRD;                     // 装备提供暴击伤害（Crit Damage）
    public int equipmentPD;                      // 装备提供物理伤害（Physical Damage）
    public int equipmentXP;                      // 装备提供经验加成（Experience）

    [Header("状态UI")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI hprText;
    public TextMeshProUGUI mpText;
    public TextMeshProUGUI mprText;
    public TextMeshProUGUI adText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI crText;
    public TextMeshProUGUI crdText;
    public TextMeshProUGUI pdText;
    public TextMeshProUGUI xpText;


    [Header("背包面板")]
    public GameObject inventoryPanel;
    [Header("装备面板")]
    public GameObject equipPanel;
    [Header("测试面板")]
    public GameObject textPanel;
    [Header("物品详情面板")]
    public GameObject itemTooltipPanel;
    [Header("属性面板")]
    public GameObject statsPanel;
    [Header("前端格子显示")]
    public ItemSlotUI[] uiSlots;//前端显示
    [Header("后台数据格子物品")]
    public ItemSlot[] slots;//后台数据，记录每个格子里存什么物品、堆叠数量
    [Header("选中物体")]
    private ItemSlot selectedItem;//当前选中的格子
    private int selectedItemIndex;//当前选中的格子的序号
    public TextMeshProUGUI selectedItemName, selectedItemDescription, selectedItemStat;//选中物体的名字，描述，属性名字，属性值
    public GameObject useButton, equipButton, unequipButton;//使用按钮，装备按钮，卸下按钮

    [Header("UI显示")]

    public TextMeshProUGUI goldText;//金币文本
    public TextMeshProUGUI levelText;//等级文本
    public int gold;//当前金币数量

    private const int HelmSlotIndex = 0;//头盔槽下标
    private const int WeaponSlotIndex = 1;//武器槽下标
    private const int ArmorSlotIndex = 2;//盔甲槽下标
    private const int BootsSlotIndex = 3;//靴子槽下标
    private const int RingSlotIndex = 4;//戒指槽下标
    private const int NeglecSlotIndex = 5;//项链槽下标
    private const int ShieldSlotIndex = 6;//护盾槽下标
    private const int EquipBarSlotIndex01 = 7;
    private const int EquipBarSlotIndex02 = 8;
    private const int EquipBarSlotIndex03 = 9;
    private const int EquipBarSlotIndex04 = 10;
    private const int EquipBarSlotIndex05 = 11;
    private const int EquipBarSlotIndex06 = 12;
    private const int EquipBarSlotIndex07 = 13;
    private const int EquipBarSlotIndex08 = 14;
    private const int EquipBarSlotIndex09 = 15;
    private const int EquipBarSlotIndex10 = 16;

    //private const int BowSlotIndex = 7;//弓槽下标
    //private const int ArrowSlotIndex = 8;//箭槽下标
    //private const int ConsumableSlotIndex = 9;//药水槽下标
    //private const int ResourceSlotIndex = 10;//资源类物品槽下标
    //private const int GloveSlotIndex = 11;//法杖下标
    private const int ReservedSlotCount = 17;//预留槽
    private const int GeneralInventoryStartIndex = ReservedSlotCount;//普通背包物品从下标4之后开始存放物品



    private void Awake()//游戏一开始就会运行这个
    {
        if(Instance == null)//判断这个实例为不为空
        {
            Instance = this;//如果为空就将当前挂载这个脚本的物体，指向自己
            DontDestroyOnLoad(gameObject);

            itemDictionary = new Dictionary<string, ItemData>();
            foreach (var item in allItems)//遍历所有物品id，查找里面的每一个物品id
            {
                if (!string.IsNullOrEmpty(item.id))
                    itemDictionary[item.id] = item;//获取物品的项目id
            }

            //给详情面板加一个嵌套Canvas并提高排序层级，让它无视层级顺序永远渲染在最上层
            if (itemTooltipPanel != null)
            {
                Canvas c = itemTooltipPanel.GetComponent<Canvas>();
                if (c == null) c = itemTooltipPanel.AddComponent<Canvas>();
                c.overrideSorting = true;//脱离父Canvas的排序，使用自己的排序值
                c.sortingOrder = 100;//数值越大越靠上层
                //嵌套Canvas需要自己的射线检测组件，否则面板里的装备/卸下按钮点不到
                if (itemTooltipPanel.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                    itemTooltipPanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }


    //游戏启动（Start）时，一次性初始化背包：
    //创建和 UI 格子数量一样多的后台数据槽；给每一个 UI 格子标记它自己的序号；把所有格子清空，背包初始为空。
    void Start()//初始化，将物品格子内的东西全清空
    {
        //PlayerPrefs.DeleteKey("InventorySave");
        //PlayerPrefs.Save();
        //Debug.Log("✅背包存档已经清除！");

        slots = new ItemSlot[uiSlots.Length];//新建后台数据数组，长度和界面格子数量一模一样，一一对应
        for (int x = 0; x < slots.Length; x++)//遍历界面的所有格子
        {
            slots[x] = new ItemSlot();//后台开辟一个空位，这个格子现在是空的，还没放装备 / 药水。
            uiSlots[x].index = x;//告诉前端空位，它是背包里第 x 号位置。
            uiSlots[x].Clear();
        }
        ClearSelectedItemWindow();
        UpdateUI();
        UpdateGoldUI();
        LoadInventory();

    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory(equipPanel);

        if (Input.GetKeyDown(KeyCode.L))
        {
            PlayerPrefs.DeleteKey("InventorySave");//清空背包存档
            Debug.Log("物品栏已清空");
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("已点击Y");
            ToggleInventory(textPanel);

        }
        if (Input.GetKeyDown(KeyCode.U))
            ToggleInventory(statsPanel);
    }

    public void AddItem(ItemData item)//添加物品到物品栏的函数
    {
        AddItem(item, false);
    }

    //fromPickup：是否为地面掉落物。只有消耗品会优先填入EquipBar快捷栏，
    //武器/装备等其它物品一律进普通背包格子
    public void AddItem(ItemData item, bool fromPickup)
    {
        bool preferEquipBar = item.type == ItemType.Consumable;

        if (item.canStack)
        {
            ItemSlot stackSlot = GetItemStack(item, preferEquipBar);//新建一个堆叠格子，将要添加的物品给堆叠物品方法，
            if(stackSlot != null)//如果格子不为空
            {
                stackSlot.quantity++;//则格子物品加一
                SaveInventory();
                UpdateUI();//更新ui数据
                return;
            }
        }
        
        ItemSlot emptySlot = GetEmptySlot(preferEquipBar);//新建一个空格子，获取一个空格子回来
        if(emptySlot != null)//如果“空格子”里有格子
        {
            emptySlot.item = item;//则把需要添加的物体放入
            emptySlot.quantity = 1;//数量为1
            SaveInventory();
            UpdateUI();
            return;
        }

        Debug.Log("库存已满，掉落物品");
        ThrowItem(item);//丢弃物品
    }

    public void ThrowItem(ItemData item)//丢弃物品
    {
        SaveInventory();
        //if(playerStats !=  null)
            //Instantiate(item.propPrefab,playerStats.transform.position,playerStats.transform.rotation);
    }

    public void AddGold(int amount)//增加金币，并刷新金币显示
    {
        gold += amount;
        UpdateGoldUI();
    }

    public void UpdateGoldUI()//刷新金币文本显示
    {
        if (goldText != null)
            goldText.text = gold.ToString();
    }


   

    public void UpdateUI()//UI更新
    {
        //goldText.text = goldTextManager.instance.gold.ToString();

        for (int i = 0; i < ReservedSlotCount; i++)//遍历装备格子
        {
            if (slots[i].item != null)//如果格子里有物品
                uiSlots[i].Set(slots[i]);//传入的格子就会更新物品里的图标和数量
            else
                uiSlots[i].Clear();//格子里没有物品没有的话则直接清理
        }

        for (int i = GeneralInventoryStartIndex; i < slots.Length; i++)//遍历普通格子
        {
            if (slots[i].item != null)//格子内有物品时
                uiSlots[i].Set(slots[i]);//前端格子物品更新
            else//没有则清理
                uiSlots[i].Clear();
        }
    }

    private void SetUISlot(int index, ItemData equippedItem)//设置UI格子,根据下标传入物体数据
    {
        if (equippedItem != null)//判断装备
        {
            slots[index].item = equippedItem;//后端格子物体放入传入的装备
            slots[index].quantity = 1;//数量为1
            uiSlots[index].Set(slots[index]);//同时前端也进行改变
        }
        else
        {
            slots[index].item = null;
            slots[index].quantity = 0;
            uiSlots[index].Clear();
        }

    }
    ItemSlot GetItemStack(ItemData item, bool preferEquipBar = false)//获取可堆叠物品，主要是用来判断物体的堆叠数量到没到达它的上限
    {
        if (preferEquipBar)//优先在EquipBar快捷栏里找可堆叠的同类物品
        {
            for (int i = EquipBarSlotIndex01; i <= EquipBarSlotIndex10; i++)
            {
                if (slots[i].item == item && slots[i].quantity < item.maxStackAmount)
                    return slots[i];
            }
        }
        for (int i = GeneralInventoryStartIndex; i < slots.Length; i++)//遍历非装备格子
        {
            if (slots[i].item == item && slots[i].quantity < item.maxStackAmount)
                //如果格子里的物品是传输进来的物品同时它的数量小于它的最大堆叠数
                //则意味着它没有到达它的上限
                return slots[i];
        }
        //反之则意味着它到达了最大值，返回空就是什么也不返回
        return null;
    }
    ItemSlot GetEmptySlot(bool preferEquipBar = false)//获取空格子
    {
        if (preferEquipBar)//优先占用EquipBar快捷栏的空位
        {
            for (int i = EquipBarSlotIndex01; i <= EquipBarSlotIndex10; i++)
            {
                if (slots[i].item == null)
                    return slots[i];
            }
        }
        for (int i = GeneralInventoryStartIndex; i < slots.Length; i++)//遍历非装备格子
        {
            if (slots[i].item == null)//格子里没有物体就返回这个空格子
                return slots[i];
        }
        return null;
    }

    void ClearSelectedItemWindow()//清理选中物品的窗口
    {
        selectedItem = null;
        selectedItemName.text = string.Empty;
        selectedItemDescription.text = string.Empty;
        selectedItemStat.text = string.Empty;
        //禁用全部按钮
        useButton.SetActive(false);
        equipButton.SetActive(false);
        unequipButton.SetActive(false);
        //隐藏物品详情面板
        if (itemTooltipPanel != null)
            itemTooltipPanel.SetActive(false);

    }


    public void RemoveSelectedItem()//移除选中物品
    {
        selectedItem.quantity--;
        if (selectedItem.quantity == 0)//如果物品已经没有了
        {
            selectedItem.item = null;//将其中物品清除
            ClearSelectedItemWindow();//同时清理选中物品的窗口
        }
        UpdateUI();
    }

    private string FormatDiff(int diff)//属性差值
    {
        if (diff > 0) return $"<color=green>+{diff}</color>";//当差值大于0时，说明有了更好的属性
        if (diff < 0) return $"<color=red>{diff}</color>";//当差值小于0，属性更差
        return "";

    }

    private int GetEquipmentSlotForType(ItemType type)//得到武器格子里的武器类型,如果是以下装备类型就返回1，不是就返回-1
    {
        switch (type)
        {
            case ItemType.EquipableWeapon:return WeaponSlotIndex;
            case ItemType.EquipableHelm: return HelmSlotIndex;
            case ItemType.EquipableArmor:return ArmorSlotIndex;
            case ItemType.EquipableBoots: return BootsSlotIndex;
            case ItemType.EquipableRing:return RingSlotIndex;
            case ItemType.EquipableNeglec:return NeglecSlotIndex;
            case ItemType.EquipableShield:return ShieldSlotIndex;
            default:return -1;

        }
    }
    private string GetRarityText(ItemRarity rarity)//得到稀有度文本
    {
        switch (rarity)
        {
            case ItemRarity.Common:return "<color=grey>普通</color>";
            case ItemRarity.Rare: return "<color=blue>稀有</color>";
            case ItemRarity.Epic:return "<color=purple>稀有</color>";
            case ItemRarity.Legendary:return "<color=orange>传说</color>";
            default: return "";
        }
    }


    public void ClearStatsDiffientOfEquipment()
    {
        if (hpText) hpText.text = $"生命值:{StatsManager.instance.maxHealth}";
        if (hprText) hprText.text = $"生命恢复:{StatsManager.instance.healthRegen}";
        if (mpText) mpText.text = $"法力值:{StatsManager.instance.maxMana}";
        if (mprText) mprText.text = $"法力恢复值:{StatsManager.instance.manaRegen}";
        if (adText) adText.text = $"物理攻击:{equipmentAD}";
        if (defText) defText.text = $"防御力:{equipmentDef}";
        if (crText) crText.text = $"暴击:{equipmentCR}";
        if (crdText) crdText.text = $"暴击伤害:{equipmentCRD}";
        if (pdText) pdText.text = $"物理伤害:{equipmentPD}";
        if (xpText) xpText.text = $"经验值:{equipmentXP}";
    }
    public void SelectItem(int index)//选中物品,获取物品的下标
    {
        // 格子点击音效（空格子也有，给触觉反馈）
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxSlotClick);

        if (slots[index].item == null)//如果选中物品的下标为空就什么也不做
        {
            ClearSelectedItemWindow();
            ClearStatsDiffientOfEquipment();
            return;
        }

        //EquipBar快捷栏里的消耗品（如药水）：点击格子直接使用，不弹出详情面板
        if (IsEquipBarSlot(index) && slots[index].item.type == ItemType.Consumable)
        {
            UseEquipBarConsumable(index);
            return;
        }

        //当格子有物体时将物体放置在赋值给选中物体
        selectedItem = slots[index];
        selectedItemIndex = index;//下标也等于当前选中物体的下标

        

        selectedItemName.text = selectedItem.item.displayName + " " + GetRarityText(selectedItem.item.raity);//获取选中物体的脚本化物体的背包显示名称,并显示稀有度名称
        selectedItemDescription.text = selectedItem.item.description;//获取选中物体的描述

        selectedItemStat.text = string.Empty;

        foreach (var stat in selectedItem.item.statsOfItem)
        {
            //选中物体状态的类型和数值
            selectedItemStat.text += stat.type + "+" +stat.value + "<br>";
        }
        bool isEquipSlot = index >= 0 && index < ReservedSlotCount;//是否是装备格子的判断得是大于0的同时在预备格子之前

        ItemType type = selectedItem.item.type;//物品的类型就是选中物品的类型

        //if(type == ItemType.Consumable)//消耗品判断
        //{

        //}

        ClearStatsDiffientOfEquipment();

        useButton.SetActive(type == ItemType.Consumable);//使用按键的启用取决于物品类型是否是消耗品
        equipButton.SetActive(!isEquipSlot && IsEquipableType(type));//装备按键判断选中物体必须是非装备格同时物品类型必须是装备，然后才能装备

        unequipButton.SetActive(isEquipSlot && slots[index].item != null);//判断选中物体是装备并且选中格子不为空才可以卸下
        //unequipButton.SetActive(isEquipSlot && playerStats.GetEquipped(type) != null);//卸下按键必须得是装备格
        FollowMouse();

        if (IsEquipableType(type))
        {
            int newHP = equipmentHP, newHPR = equipmentHPR, newMP = equipmentMP, newMPR = equipmentMPR;
            int newAD = equipmentAD, newDEF = equipmentDef, newCR = equipmentCR, newCRD = equipmentCRD;
            int newPD = equipmentPD, newXP = equipmentXP;

            //检查物品是否可装备
            int equipSlot = GetEquipmentSlotForType(type);
            if (equipSlot != -1 && slots[equipSlot].item != null && index >= GeneralInventoryStartIndex)//当装备是武器类型，同时格子里有装备，而且在普通格子里
            {
                foreach (var stat in slots[equipSlot].item.statsOfItem)//遍历武器格子里的装备状态
                {
                    switch (stat.type)//逐一检查物品属性，计算差额
                    {
                        case StatsType.HP: newHP -= (int)stat.value; break;
                        case StatsType.HPR: newHPR -= (int)stat.value; break;
                        case StatsType.MP: newMP -= (int)stat.value; break;
                        case StatsType.MPR: newMPR -= (int)stat.value; break;
                        case StatsType.AD: newAD -= (int)stat.value; break;
                        case StatsType.CR: newCR -= (int)stat.value; break;
                        case StatsType.CRD: newCRD -= (int)stat.value; break;
                        case StatsType.PD: newPD -= (int)stat.value; break;
                        case StatsType.XP: newXP -= (int)stat.value; break;
                    }
                }
            }

            foreach (var stat in selectedItem.item.statsOfItem)//遍历选中物体的的装备状态
            {
                switch (stat.type)//逐一检查物品属性，计算正属性
                {
                    case StatsType.HP: newHP += (int)stat.value; break;
                    case StatsType.HPR: newHPR += (int)stat.value; break;
                    case StatsType.MP: newMP += (int)stat.value; break;
                    case StatsType.MPR: newMPR += (int)stat.value; break;
                    case StatsType.AD: newAD += (int)stat.value; break;
                    case StatsType.CR: newCR += (int)stat.value; break;
                    case StatsType.CRD: newCRD += (int)stat.value; break;
                    case StatsType.PD: newPD += (int)stat.value; break;
                    case StatsType.XP: newXP += (int)stat.value; break;
                }
            }

            //更新用户界面
            if (hpText) hpText.text = $"生命值:{StatsManager.instance.maxHealth} {FormatDiff(newHP - equipmentHP)}";
            if (hprText) hprText.text = $"生命恢复值:{StatsManager.instance.healthRegen}{FormatDiff(newHPR - equipmentHPR)}";
            if (mpText) mpText.text = $"法力值:{StatsManager.instance.maxMana} {FormatDiff(newMP - equipmentMP)}";
            if (mprText) mprText.text = $"法力恢复值:{StatsManager.instance.manaRegen} {FormatDiff(newMPR - equipmentMPR)}";
            if (adText) adText.text = $"物理攻击:{equipmentAD} {FormatDiff(newAD - equipmentAD)}";
            if (defText) defText.text = $"防御力:{equipmentDef} {FormatDiff(newDEF - equipmentDef)}";
            if (crText) crText.text = $"暴击:{equipmentCR}{FormatDiff(newCR - equipmentCR)}";
            if (crdText) crdText.text = $"暴击伤害:{equipmentCRD}{FormatDiff(newCRD - equipmentCRD)}";
            if (pdText) pdText.text = $"物理伤害{equipmentPD} {FormatDiff(newPD - equipmentPD)}";
        }
    }

    bool IsEquipableType(ItemType type)//判断是否是装备，通过类型判断，如果是以下的装备类型则返回真，如果不是则是false
    {
        return type == ItemType.EquipableWeapon ||//武器
               type == ItemType.EquipableArmor ||//护甲
               type == ItemType.EquipableBoots ||//靴子
               type == ItemType.EquipableShield ||//盾牌
               type == ItemType.EquipableRing ||//戒指
               type == ItemType.EquipableHelm ||//头盔
               type == ItemType.EquipableNeglec ||//项链
               type == ItemType.EquipableBow ||//弓
               type == ItemType.EquipableGlove ||//法杖
               type == ItemType.EquipableArrow;//箭
    }
    public void SortInventory()//库存排序
    {
        List<ItemSlot> generalInventory = new List<ItemSlot>();// 创建一个临时的List，专门用来存放【普通背包区域里有物品的格子】

        for (int i = GeneralInventoryStartIndex; i < slots.Length; i++)//遍历普通格子
        {
            if (slots[i].item != null)//如果格子里有东西
            {
                //将有东西的普通格子里的东西和数量存在临时列表里
                generalInventory.Add(new ItemSlot
                {
                    item = slots[i].item,
                    quantity = slots[i].quantity,
                });
            }
        }

        generalInventory.Sort((a,b)=>a.item.displayName.CompareTo(b.item.displayName));

        for (int i = GeneralInventoryStartIndex;i < slots.Length; i++)//然后清空背包里的所有格子
        {
            slots[i].item = null;
            slots[i].quantity = 0;
        }

        for(int i = 0;i < generalInventory.Count; i++)//把排好序的物品依次放回背包格子
        {
            int slotIndex = GeneralInventoryStartIndex + i; //从普通背包第一个格子开始放。
            slots[slotIndex] .item= generalInventory[i].item;
            slots[slotIndex].quantity = generalInventory[i].quantity;
        }
        UpdateUI();
    }

    public void ClearAllItems()//清空背包普通格子的所有物品（保留装备栏），玩家死亡重生时调用
    {
        if (slots == null) return;

        for (int i = GeneralInventoryStartIndex; i < slots.Length; i++)
        {
            slots[i].item = null;
            slots[i].quantity = 0;
        }
        UpdateUI();
    }

    public void OnUseButton()//使用按钮点击事件：消耗品使用后立刻结算其恢复效果（如药水回血/回蓝）
    {
        if (selectedItem == null || selectedItem.item == null)//如果选中物体是空的或什么都没有就什么都不做
            return;

        if (selectedItem.item.type != ItemType.Consumable)//只有消耗品才允许被“使用”，装备类走装备按钮
            return;

        ApplyConsumableEffect(selectedItem.item);//结算药水的恢复效果

        RemoveSelectedItem();//用药水消耗一个，数量归零时自动清空该格子
        SaveInventory();
        UpdateUI();

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxUseItem);
    }

    //判断下标是否属于EquipBar快捷栏（下标7~16）
    private bool IsEquipBarSlot(int index)
    {
        return index >= EquipBarSlotIndex01 && index <= EquipBarSlotIndex10;
    }

    //结算消耗品的恢复效果：遍历物品属性，HP回血、MP回蓝
    private void ApplyConsumableEffect(ItemData item)
    {
        if (item == null || item.statsOfItem == null) return;

        foreach (var stat in item.statsOfItem)
        {
            switch (stat.type)
            {
                case StatsType.HP://生命恢复：把药水的数值加到玩家当前生命值上（上限为最大生命值）
                    if (StatsManager.instance != null)
                        StatsManager.instance.Heal(stat.value);
                    break;
                case StatsType.MP://法力恢复
                    if (StatsManager.instance != null)
                        StatsManager.instance.RestoreMana(stat.value);
                    break;
            }
        }
    }

    //点击EquipBar里的消耗品格子时直接使用：结算效果并消耗一个，不弹出详情面板
    private void UseEquipBarConsumable(int index)
    {
        ApplyConsumableEffect(slots[index].item);//先结算恢复效果

        slots[index].quantity--;//消耗一个
        if (slots[index].quantity <= 0)//数量用完则清空格子
        {
            slots[index].item = null;
            slots[index].quantity = 0;
        }

        SaveInventory();
        UpdateUI();//刷新格子上药水的剩余数量

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxUseItem);
    }

    public void OnEquipButton()//装备按钮点击事件
    {
        
        if (selectedItem == null || selectedItem.item == null)//格子里啥都没有或是物品为空
            return;

        int targetSlot = -1;//用来存【要装备到哪个装备槽的下标】，初始值-1代表：还没找到对应槽位

        switch (selectedItem.item.type)
        {
            case ItemType.EquipableWeapon:
                targetSlot = WeaponSlotIndex;
                break;
            case ItemType.EquipableHelm:
                targetSlot = HelmSlotIndex;
                break;
            case ItemType.EquipableArmor:
                targetSlot=ArmorSlotIndex;
                break;
            case ItemType.EquipableBoots:
                targetSlot = BootsSlotIndex;
                break;
            case ItemType.EquipableRing:
                targetSlot=RingSlotIndex;
                break;
            case ItemType.EquipableNeglec:
                targetSlot = NeglecSlotIndex;
                break;
            case ItemType.EquipableShield: 
                targetSlot = ShieldSlotIndex;
                break;

        }

        if (targetSlot == -1)//如果定位插槽为-1代表没有固定槽位
            return;
        if (slots[targetSlot].item != null)//如果对应装备槽已经有的装备
        {
            AddItem(slots[targetSlot].item);//就将这个装备槽物品放回物品栏，并将新装备放进去换进去
        }

        slots[targetSlot].item = selectedItem.item;//装备槽的物品就变成选中的物品
        slots[targetSlot].quantity = 1;

        slots[selectedItemIndex].quantity--;//选中的物品数量减1
        if (slots[selectedItemIndex].quantity<=0)//带那个选中物品数量小于0时
            slots[selectedItemIndex].item = null;//物品为空

        RecalculateStats();
        ClearSelectedItemWindow();
        SaveInventory();
        UpdateUI();

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxEquip);
    }

    public void OnUnequipButton()//卸下装备按钮点击事件
    {
        if (selectedItem == null || selectedItem.item == null)
            return;
        int index = selectedItemIndex;


        if (index < 0 || index >= ReservedSlotCount)//如果下标小于0意味着它产生了错误，没有此物品，大于ReservedSlotCount则意味着他是普通格子
            return;//什么也不做，直接退出这个函数

        AddItem(selectedItem.item);//将物品放回物品栏

        slots[index].item = null;
        slots[index].quantity = 0;

        RecalculateStats();
        ClearSelectedItemWindow();//同时清理选中物品的窗口
        SaveInventory();
        UpdateUI();

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxUnequip);

    }
    public void ToggleInventory(GameObject panel)//切换背包栏
    {
        panel.SetActive(!panel.activeSelf);//如果背包现在关着，就打开；如果现在开着，就关掉
        if (panel.activeSelf)
        {
            ClearSelectedItemWindow();//清空清理选中物品的窗口
            SortInventory();
        }
    }

   public void RecalculateStats()// 重新计算属性
    {
        
        equipmentHP = 0;
        equipmentHPR = 0;
        equipmentMP = 0;
        equipmentMPR = 0;
        equipmentAD = 0;
        equipmentDef = 0;
        equipmentCR = 0;
        equipmentCRD = 0;
        equipmentPD = 0;
        equipmentXP = 0;

        for (int i = 0;i < ReservedSlotCount; i++)//遍历普通格子
        {
            if(slots[i].item != null)//如果格子里有东西
            {
                foreach(var stat in slots[i].item.statsOfItem)//遍历格子里物品的属性
                {
                    switch (stat.type)//查找对应属性
                    {
                        case StatsType.HP:
                            equipmentHP += Mathf.RoundToInt(stat.value); 
                            break;
                        case StatsType.HPR:
                            equipmentHPR += Mathf.RoundToInt(stat.value);
                            break;
                        case StatsType.MP:
                            equipmentMP += Mathf.RoundToInt(stat.value);
                            break;
                        case StatsType.MPR:
                            equipmentMPR += Mathf.RoundToInt(stat.value);
                            break;
                        case StatsType.AD:
                            equipmentAD += Mathf.RoundToInt(stat.value);
                            break;
                        case StatsType.DF:
                            equipmentDef += Mathf.RoundToInt(stat.value);
                            break;
                        case StatsType.CR:
                            equipmentCR += Mathf.RoundToInt(stat.value);
                            break;
                        case StatsType.CRD:
                            equipmentCRD += Mathf.RoundToInt(stat.value);
                            break;
                        case StatsType.PD:
                            equipmentPD += Mathf.RoundToInt(stat.value);
                            break;
                        case StatsType.XP:
                            equipmentXP += Mathf.RoundToInt(stat.value);
                            break;
                    }
                }
            }
        }
        StatsManager.instance.RecalculateStats();

        if (hpText) hpText.text = "生命值" + StatsManager.instance.maxHealth;
        if (hprText) hprText.text = "生命恢复" + StatsManager.instance.healthRegen;
        if(mpText)mpText.text = "法力值" + StatsManager.instance.maxMana;
        if (mprText) mprText.text = "法力恢复" + StatsManager.instance.manaRegen;
        if(adText)adText.text = "伤害"+ equipmentAD;
        if (crText) crText.text = "暴击率" + equipmentCR;
        if (crdText) crdText.text = "暴击伤害" + equipmentCRD;
        if(pdText)pdText.text = "物理伤害" + equipmentPD;
        if(xpText)xpText.text = "经验" + equipmentXP;

    }

    //游戏打开 / 读档时，从 `PlayerPrefs` 取出背包的 JSON 存档文本，把存档里的物品、数量读出来，赋值给游戏内存里的背包格子数据，最后刷新背包 UI
    
    public ItemData GetItemByID(string id)//获取物品id
    {
        if (itemDictionary.TryGetValue(id, out var item))//如果这个项目有id，将获取到的id，把找到的物品放进value里
            return item;//返回这个找到的物品
        Debug.LogWarning("没有找到物品id" + id);
        return null;
    }

    public bool HasItem(ItemData item)//判断背包里是否持有指定物品（含装备栏），用于对话的物品条件判断
    {
        if (item == null || slots == null)
            return false;

        foreach (var slot in slots)
        {
            //格子里有物品、和查询物品一致、并且数量大于0，才算持有
            if (slot != null && slot.item == item && slot.quantity > 0)
                return true;
        }
        return false;
    }

    /// <summary>查询背包里持有某物品的总数量（含装备栏）</summary>
    public int GetItemCount(ItemData item)
    {
        if (item == null || slots == null) return 0;
        int count = 0;
        foreach (var slot in slots)
        {
            if (slot != null && slot.item == item)
                count += slot.quantity;
        }
        return count;
    }

    /// <summary>从背包扣除指定物品（从普通背包扣，扣完清空格子；如果背包数量不够返回 false）</summary>
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;
        if (GetItemCount(item) < amount) return false;

        int remaining = amount;
        // 优先从普通背包扣（ReservedSlotCount 之后的格子）
        for (int i = slots.Length - 1; i >= 0 && remaining > 0; i--)
        {
            if (slots[i] == null || slots[i].item != item) continue;

            if (slots[i].quantity <= remaining)
            {
                remaining -= slots[i].quantity;
                slots[i].item = null;
                slots[i].quantity = 0;
            }
            else
            {
                slots[i].quantity -= remaining;
                remaining = 0;
            }
        }
        SaveInventory();
        UpdateUI();
        return true;
    }

    public void SaveInventory()//保存背包数据,**把当前背包里所有格子的数据，序列化成 JSON，用 PlayerPrefs 保存到本地**，做背包离线存档。//通俗讲：把背包每个格子里「是什么物品、数量多少」打包，存进电脑，下次打开游戏可以读回来。
    {
        InventorySaveData saveData = new InventorySaveData();// 1. 创建存档专用的容器类实例（只存需要保存的数据，不存Unity组件）

        foreach (var slot in slots)//遍历背包的所有格子
        {
            //保存单个格子到saveData里，键是itemID，值是quantity
            saveData.slots.Add(new SlotSaveData
            {
                //如果格子里有物品就返回这个物品的id，反之返回null
                itemID = slot.item != null ? slot.item.id : null,
                //格子有物品 → 保存数量；没有物品 → 数量存0
                quantity = slot.item != null ? slot.quantity : 0,
            });
        }
        //将saveData这个对象，转换成JSON字符串（长文本）
        string json = JsonUtility.ToJson(saveData, true);
        //使用PlayerPrefs把JSON字符串存入本地，键名叫"InventorySave"
        PlayerPrefs.SetString("InventorySave", json);
        PlayerPrefs.Save();

        Debug.Log("物品已保存");
    }
    public void LoadInventory()//加载函数
    {
        if (!PlayerPrefs.HasKey("InventorySave"))//检查 PlayerPrefs 里面有没有名叫`InventorySave`的数据
            return;
        //从 PlayerPrefs 读取保存好的字符串，拿到当初`SaveInventory`存进去的背包 JSON 文本
        string json = PlayerPrefs.GetString("InventorySave");
        //把 JSON 文本，反向变回 C# 存档类对象
        InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

        for (int i = 0; i < saveData.slots.Count && i < slots.Length; i++)
        {
            //把当前循环到的【存档格子信息】拿出来，临时变量savedSlot，里面存了 itemID 和 quantity
            var savedSlot = saveData.slots[i];
            //判断字符串是空或者 null
            if (!string.IsNullOrEmpty(savedSlot.itemID))
            {

                //用存档里存的物品 ID，去物品字典查找对应的物品数据 ItemData
                ItemData item = GetItemByID(savedSlot.itemID);
                //把查到的物品，放到内存背包第 i 格
                slots[i].item = item;
                //把存档里记录的物品数量赋值进去
                slots[i].quantity = savedSlot.quantity;
            }
            else
            {
                //如果存档格子 itemID 是空，代表格子没有物品：
                //把当前内存背包对应格子物品清空，数量置 0。
                slots[i].item = null;
                slots[i].quantity = 0;
            }
        }
        RecalculateStats();
        UpdateUI();
        Debug.Log("物品栏已加载");
    }
    public void ClosePanel(GameObject panel)//关闭面板
    {
        panel.SetActive(false);
    }

    public void FollowMouse()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 offset = new Vector3(180, -250, 0);
        itemTooltipPanel.SetActive(true);//确保面板显示（不切换，避免点击其他物品时面板被关掉）
        itemTooltipPanel.transform.SetAsLastSibling();//挪到同级末尾，保证显示在EquipBar等其他UI之上
        itemTooltipPanel.transform.position = mousePosition + offset;
    }
}
[System.Serializable]
public class InventorySaveData//保存槽位列表
{
    public List<SlotSaveData> slots = new List<SlotSaveData>();
}
[System.Serializable]//序列化
public class SlotSaveData//保存物品ID和对应的数量
{
    public string itemID;
    public int quantity;
}


[System.Serializable]
public class ItemSlot//UI中的物品格子
{
    public ItemData item;//格子里的物体
    public int quantity;//物体数量
}
