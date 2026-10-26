using System.Collections;
using UnityEngine;

//挂在建筑物本体（根物体）上：当玩家走到建筑物后面（进入触发器区域）时，
//把整栋建筑所有子物体的 SpriteRenderer 平滑变透明，玩家离开后恢复
public class BuildingFade : MonoBehaviour
{
    [Header("淡出后的透明度")]
    [Range(0f, 1f)] public float fadedAlpha = 0.3f;

    [Header("渐变时长（秒）")]
    public float fadeDuration = 0.2f;

    //这栋建筑所有子物体的图片（拼接建筑时会有多个）
    private SpriteRenderer[] renderers;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        //包含未激活的子物体，保证全部找到
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        StartFade(fadedAlpha);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        StartFade(1f);
    }

    private void StartFade(float targetAlpha)
    {
        //打断上一次渐变，避免两个协程同时改透明度
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = renderers.Length > 0 ? renderers[0].color.a : 1f;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration));
            yield return null;
        }
        SetAlpha(targetAlpha);
        fadeCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        foreach (var r in renderers)
        {
            if (r == null) continue;
            Color c = r.color;
            c.a = alpha;
            r.color = c;
        }
    }
}
