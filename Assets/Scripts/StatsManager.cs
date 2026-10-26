using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StatsManager : MonoBehaviour//状态管理器
{
    public static StatsManager instance;

    [Header("健康")]
    public Image healthBar; //血条图片组件
    [Header("当前生命值")]
    public float currentHealth;//当前生命值\
    [Header("基础血量")]
    public float baseHealth = 100f;
    [Header("最大生命")]
    public float maxHealth = 100f;//最大生命值
    [Header("基础血量回复")]
    public float baseHealthRegen;//
    [Header("生命恢复")]
    public float healthRegen = 0f;//每秒回血数值

    [Header("法力")]
    public Image manaBar;
    [Header("当前法力")]
    public float currentMana;//当前法力
    [Header("基础法力")]
    public float baseMana = 100f;
    public float maxMana =100f;//最大法力
    [Header("基础法力回复")]
    public float baseManareRegen;//
    [Header("法力回复")]
    public float manaRegen;//每秒总回魔数值

    public float knockbackForce = 50;
    [Header("击晕时间")]
    public float stunTime = 1;

    [Header("死亡重生")]
    public float deathPauseTime = 1.5f;//死亡后原地停顿的时间（秒）
    public float respawnDelay = 1f;//玩家消失后到在世界中心复活的间隔（秒）
    public Transform respawnPoint;//重生点，不设置则回到世界中心(0,0,0)
    public string respawnSceneName = "village";//死亡后重生到哪个场景，不设置则留在当前场景
    public GameObject deathVFX;//死亡爆炸特效（可拖入DestroyVFX预制体）

    private Coroutine deathRoutine;

    private int baseAD = 5;//基础攻击力
    public int finalAD;//最终攻击力（基础+装备）



    [Header("lerp Settings")]
    public float lerpSpeed = 2f;//血/蓝条平滑插值速度。Lerp做动画，数值越大，血条变化越快；越小越丝滑缓慢

    private bool isDead;

    private Coroutine healthLerpRoutine;//相当于播放按键
    private Coroutine manaLerpRoutine;

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        finalAD = baseAD;//开局没穿装备，最终攻击力先等于基础攻击力（不能等 RecalculateStats，新档时它不会被调用）

        if (healthBar != null) healthBar.fillAmount = 1f;//如果生命值存在，就把生命值条填充到1
        if (manaBar != null) manaBar.fillAmount = 1f;
    }

    private void Update()
    {
        if (healthRegen > 0f && !isDead)
            Heal(healthRegen * Time.deltaTime);
        if(manaRegen > 0f)//如果法力恢复大于0，运行恢复法力
            RestoreMana(manaRegen * Time.deltaTime);

    }

    public void RecalculateStats()//重算属性：基础值+装备加成
    {
        maxHealth = baseHealth + InventoryManager.Instance.equipmentHP;//最大生命值等于基础生命值加武器加成生命值
        maxMana = baseMana + InventoryManager.Instance.equipmentMP;//最大法力值等于基础法力值加武器加成法力值
        healthRegen = baseHealthRegen + InventoryManager.Instance.equipmentHPR;//生命恢复数值等于基础生命恢复加武器加成
        manaRegen = baseManareRegen + InventoryManager.Instance.equipmentMPR;//法力恢复等于基础法力恢复加武器加成
        finalAD = baseAD + InventoryManager.Instance.equipmentAD;//最终攻击力等于基础攻击力加装备攻击力加成

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);//限制生命值在0~最大值之间
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);

        if (currentHealth <= 0) currentHealth = maxHealth;
        if (currentMana <= 0)currentMana = maxMana;

        UpdateHealthBar();
        UpdateManaBar();

    }

    public void Heal(float amount)//治疗
    {
        if (isDead || amount <= 0f) return;//玩家死了直接返回什么都不做

        float before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);//比如当前50生命值，治疗70生命，当前生命值就干到120，不能比最大生命值还大，所以直接变回最大生命值
        UpdateHealthBar();

        // 治疗音效：只有实际回血时才播放（满血时调用 Heal 不播放）
        if (currentHealth > before)
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxPlayerHeal);
    }

    public void TakeDamage(float amount)//受到伤害
    {
        if (isDead || amount <= 0f) return;//玩家死了直接返回什么都不做

        currentHealth = Mathf.Max(currentHealth - amount, 0f);//比如当前50生命值，受到70点伤害，当前生命值就变成了负数，不能比0还小，所以取最大值

        UpdateHealthBar();

        // 玩家受击音效
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxPlayerHurt);

        if(currentHealth <= 0f && !isDead)//如果当前生命低于0，同时没死，就代表玩家已死亡
        {
            isDead = true;
            if (deathRoutine == null)
                deathRoutine = StartCoroutine(DeathSequence());
        }


    }

    private void UpdateHealthBar()//更新血条**触发血条平滑动画**，核心是解决多次受伤导致血条抖动问题
    {
        if (healthBar == null) return;//如果没有血条就什么也不做

        //StartCoroutine(LerBar(healthBar,currentHealth/maxHralth));
        float targeFill = currentHealth / maxHealth;//目标填充

        if(healthLerpRoutine != null) //如果现在有正在运行的血条平滑协程
            StopCoroutine(healthLerpRoutine);//先把旧的动画强制关掉

        healthLerpRoutine = StartCoroutine(LerpBar(healthBar,targeFill,()=>healthLerpRoutine = null));
    }

    private IEnumerator RegenHealth()//生命再生
    {
        while (!isDead)
        {
            Heal(healthRegen * 0.1f);//治疗量取决于每秒总回血
            yield return new WaitForSeconds(0.1f); //暂停这个协程，等待 0.1 秒之后，再继续执行循环
        }
    }

    private IEnumerator LerpBar(Image bar,float targetFill,System.Action onComplete)//平滑血条协程(血量图片，目标血量)
    {
        //float startFill = bar.fillAmount;//保存动画启动瞬间的填充值。
        while (Mathf.Abs(bar.fillAmount - targetFill) > 0.001f)//当前填充值 和 目标填充值 的差距大于0.001才继续平滑
        {
            bar.fillAmount = Mathf.Lerp(bar.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
            yield return null;
        }
        bar.fillAmount = targetFill;
        onComplete?.Invoke();
    }




    public void RestoreMana(float amount)//恢复法力
    {
        if (amount <= 0f || currentMana < amount) return ;//当魔力恢复量小于0或者当前魔力量小于恢复量则拒绝恢复

        currentMana = Mathf.Min(currentMana + amount, maxMana);//如果当前魔力为50，传入的魔力为70，取最小值
        UpdateManaBar();

    }

    public bool UseMana(float amount)//使用魔力（使用魔力量）
    {
        if (amount <= 0f || currentMana < amount) return false; //如果使用魔力量小于0或者当前魔力量小于使用魔力量

        currentMana = Mathf.Max(currentMana - amount, 0f);
        UpdateManaBar();
        return true;

    }

    private void UpdateManaBar()//更新法力条
    {
        if (manaBar == null) return;//如果没设置法力条
        float targetFill = currentMana / maxMana;

        if(manaLerpRoutine != null)
            StopCoroutine(manaLerpRoutine);

        manaLerpRoutine = StartCoroutine(LerpBar(manaBar, targetFill, () => manaLerpRoutine = null));
    }

    private IEnumerator RegentMana()//魔力再生
    {
        while (true)
        {
            RestoreMana(manaRegen = 0.1f);//每秒回魔总量
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator DeathSequence()//死亡→重生完整流程（本脚本挂在独立常驻物体上，协程不受玩家隐藏影响）
    {
        //1. 冻结玩家操作，原地停住（死亡动画/定身阶段）
        if (PlayerMove.Instance != null)
            PlayerMove.Instance.isDead = true;

        //2. 暂停一两秒
        yield return new WaitForSeconds(deathPauseTime);

        //3. 爆炸特效 + 玩家"销毁"（隐藏渲染和碰撞，物体本身保留，防止单例和相机失效）
        if (PlayerMove.Instance != null)
        {
            if (deathVFX != null)
            {
                GameObject vfx = Instantiate(deathVFX, PlayerMove.Instance.transform.position, Quaternion.identity);

                //雪原等场景的地面瓦片和特效同处 Default 层、SortingOrder 都是 0，
                //同层同序时地面会把粒子盖住，导致看不到死亡特效，这里统一抬到最上层
                foreach (var particleRenderer in vfx.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    particleRenderer.sortingOrder = 200;
                foreach (var spriteRenderer in vfx.GetComponentsInChildren<SpriteRenderer>(true))
                    spriteRenderer.sortingOrder = 200;

                //确保粒子一定播放（防止预制体的Play On Awake被关掉）
                foreach (var particle in vfx.GetComponentsInChildren<ParticleSystem>(true))
                    particle.Play();
            }
            else
            {
                Debug.LogWarning("[StatsManager] 没有配置死亡特效 deathVFX，死亡时不会播放特效");
            }
            PlayerMove.Instance.HidePlayer();
        }

        yield return new WaitForSeconds(respawnDelay);

        //4. 清空背包普通格子（装备栏保留）
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ClearAllItems();

        //5. 死亡后切回村庄场景（如果设置了 respawnSceneName 且当前不在该场景）
        if (!string.IsNullOrEmpty(respawnSceneName)
            && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != respawnSceneName)
        {
            // 优先用 TransitionManager 做渐变过渡，找不到就直接加载
            if (TransitionManager.Instance != null)
            {
                bool done = false;
                TransitionManager.Instance.Transition(() =>
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(respawnSceneName);
                    done = true;
                });
                // 等场景加载完再继续
                yield return new WaitUntil(() => done);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(respawnSceneName);
                // 等一帧让场景真正切换完成
                yield return null;
            }
        }

        //6. 回到重生点复活，满血满蓝
        // 如果配置了 respawnPoint 且场景里找得到它，就用它的位置；否则用 Vector3.zero（村庄原点）
        Vector3 respawnPos = Vector3.zero;
        if (respawnPoint != null)
            respawnPos = respawnPoint.position;
        else
        {
            // 没配置 respawnPoint 时，尝试在新场景里找同名空物体作为 Spawn 点
            GameObject spawn = GameObject.Find("SpawnPoint");
            if (spawn != null) respawnPos = spawn.transform.position;
        }

        if (PlayerMove.Instance != null)
            PlayerMove.Instance.RespawnAt(respawnPos);

        currentHealth = maxHealth;
        currentMana = maxMana;
        UpdateHealthBar();
        UpdateManaBar();

        isDead = false;
        deathRoutine = null;
    }
}
