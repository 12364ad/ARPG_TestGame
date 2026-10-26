using UnityEngine;

//挂在任何UI面板上：面板每次被启用(SetActive true)时，自动把自己挪到同级末尾，
//Unity UI按层级顺序渲染，越靠后越显示在上层，因此面板不会被其他UI遮挡
public class PanelBringToFront : MonoBehaviour
{
    private void OnEnable()
    {
        transform.SetAsLastSibling();
    }
}
