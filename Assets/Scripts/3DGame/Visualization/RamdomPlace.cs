using System.Collections;
using UnityEngine;

public class RamdomPlace : MonoBehaviour
{
    [Header("中心与空间")]
    [Tooltip("是否以启动时的位置作为中心点")]
    public bool centerFromStart = true;
    [Tooltip("自定义中心点（当 centerFromStart = false 时生效）")]
    public Vector3 center = Vector3.zero;
    [Tooltip("使用本地坐标（true）或世界坐标（false）")]
    public bool useLocalSpace = true;

    [Header("浮动范围（相对中心的偏移范围）")]
    public Vector3 range = new Vector3(1f, 1f, 1f);

    [Header("切换节奏")]
    [Tooltip("每次移动到新目标所用时长（秒）")]
    public float changeDuration = 1.0f;
    [Tooltip("到达目标后停留时间（秒）")]
    public float holdDuration = 0f;

    [Header("定期回归")]
    [Tooltip("是否定期回到中心点")]
    public bool periodicReturn = true;
    [Tooltip("两次回归中心的间隔（秒）")]
    public float returnInterval = 5f;
    [Tooltip("回归中心所用时长（秒）")]
    public float returnDuration = 1f;
    [Tooltip("回归后是否重置间隔计时（通常为 true）")]
    public bool resetIntervalAfterReturn = true;

    private Coroutine _routine;
    private Vector3 _current;
    private Vector3 _target;
    private float _lastReturnTime;

    void OnEnable()
    {
        if (centerFromStart)
            center = GetPos();

        _current = GetPos();
        _lastReturnTime = Time.time;
        _routine = StartCoroutine(FloatRoutine());
    }

    void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    private IEnumerator FloatRoutine()
    {
        while (true)
        {
            bool doReturn = periodicReturn && (Time.time - _lastReturnTime >= Mathf.Max(0.01f, returnInterval));
            if (doReturn)
            {
                // 回归中心
                yield return MoveTo(center, Mathf.Max(0.01f, returnDuration));
                _current = center;
                SetPos(_current);
                if (holdDuration > 0f)
                    yield return new WaitForSeconds(holdDuration);
                if (resetIntervalAfterReturn)
                    _lastReturnTime = Time.time;
                // 回归后继续下一轮随机偏移
                continue;
            }

            // 仅随机变化一个轴
            int axis = Random.Range(0, 3);
            _target = _current;
            float newOffset = Random.Range(-range[axis], range[axis]);
            _target[axis] = center[axis] + newOffset;

            yield return MoveTo(_target, Mathf.Max(0.01f, changeDuration));

            _current = _target;
            SetPos(_current);

            if (holdDuration > 0f)
                yield return new WaitForSeconds(holdDuration);
        }
    }

    private IEnumerator MoveTo(Vector3 target, float duration)
    {
        Vector3 start = GetPos();
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float eased = u * u * (3f - 2f * u); // SmoothStep
            Vector3 pos = Vector3.LerpUnclamped(start, target, eased);
            SetPos(pos);
            yield return null;
        }
        SetPos(target);
    }

    private Vector3 GetPos()
    {
        return useLocalSpace ? transform.localPosition : transform.position;
    }

    private void SetPos(Vector3 p)
    {
        if (useLocalSpace)
            transform.localPosition = p;
        else
            transform.position = p;
    }
}
