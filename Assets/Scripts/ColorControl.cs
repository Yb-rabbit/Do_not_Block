using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ColorControl : MonoBehaviour
{
    [Header("要控制的两个 Image")]
    public Image imageA;
    public Image imageB;

    [Header("变色设置")]
    public Color targetColor = Color.green;   // 得分时的目标颜色
    public float colorDuration = 0.2f;        // 变色所用时间（秒）
    public bool revertToOriginal = true;      // 变色后是否恢复
    public float revertDelay = 0.2f;          // 保持目标色的时间（秒）

    // 保存原始颜色（自动在 Awake 记录）
    private Color origA = Color.white;
    private Color origB = Color.white;
    private Coroutine runningCoroutine;

    void Awake()
    {
        if (imageA != null) origA = imageA.color;
        if (imageB != null) origB = imageB.color;
    }

    // 可由外部在得分时直接调用（例如：ScoreManager.AddScore -> colorControl.OnScore()）
    public void OnScore()
    {
        FlashColors();
    }

    // 可传入得分值或阈值：示例保留以便你根据需要扩展
    public void OnScore(int score)
    {
        // 如果需要根据 score 决定是否触发，可以在这里增加条件 ：
        // if (score < 10) return;
        FlashColors();
    }

    // 启动变色（防止并发）
    public void FlashColors()
    {
        if (runningCoroutine != null)
            StopCoroutine(runningCoroutine);
        runningCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (imageA == null && imageB == null)
            yield break;

        float t = 0f;
        // lerp 到目标颜色
        while (t < colorDuration)
        {
            t += Time.deltaTime;
            float p = colorDuration > 0f ? Mathf.Clamp01(t / colorDuration) : 1f;
            if (imageA != null) imageA.color = Color.Lerp(origA, targetColor, p);
            if (imageB != null) imageB.color = Color.Lerp(origB, targetColor, p);
            yield return null;
        }

        if (revertToOriginal)
        {
            // 等待一段时间保持目标色
            if (revertDelay > 0f) yield return new WaitForSeconds(revertDelay);

            t = 0f;
            // lerp 回原始颜色
            while (t < colorDuration)
            {
                t += Time.deltaTime;
                float p = colorDuration > 0f ? Mathf.Clamp01(t / colorDuration) : 1f;
                if (imageA != null) imageA.color = Color.Lerp(targetColor, origA, p);
                if (imageB != null) imageB.color = Color.Lerp(targetColor, origB, p);
                yield return null;
            }
        }

        // 确保回到原色（避免浮点误差）
        if (imageA != null) imageA.color = revertToOriginal ? origA : targetColor;
        if (imageB != null) imageB.color = revertToOriginal ? origB : targetColor;

        runningCoroutine = null;
    }
}
