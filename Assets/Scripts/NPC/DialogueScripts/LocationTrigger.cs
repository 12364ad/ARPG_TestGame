using UnityEngine;

//挂在带Trigger碰撞体的区域物体上：玩家进入该区域时，把对应的LocationSO记录到LocationHistoryTracker
//之后就可以在DialogueSO的requiredLocations里引用这个地点，作为对话触发条件
public class LocationTrigger : MonoBehaviour
{
    [Header("该区域对应的地点")]
    public LocationSO location;

    private void Awake()
    {
        //安全网：本组件必须配合触发器使用
        //若挂它的物体上的碰撞体忘了勾 Is Trigger，会自动纠正，避免区域变成一堵挡路的隐形墙
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("LocationTrigger[" + gameObject.name + "] 的碰撞体未勾选 Is Trigger，已自动改为触发器", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)//玩家进入区域时记录地点
    {
        if (location == null)
        {
            Debug.LogWarning(gameObject.name + " 没有配置对应的LocationSO", this);
            return;
        }
        if (!collision.CompareTag("Player"))
            return;

        if (LocationHistoryTracker.Instance == null)
        {
            Debug.LogWarning("场景中缺少 LocationHistoryTracker，无法记录地点：" + location.displayName, this);
            return;
        }

        LocationHistoryTracker.Instance.RecordLocation(location);
    }
}
