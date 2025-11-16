using System.Collections.Generic;
using UnityEngine;

public class CharControl : MonoBehaviour
{
    // 可选目标点列表（在 Inspector 里填入）
    public List<Transform> selectablePoints;

    // 每个目标点对应的直接跳转键（可选，留空则忽略）
    public List<KeyCode> pointKeys;

    // 移动速度（到目标点的插值速度）
    public float moveSpeed = 5f;

    // 使用列表顺序进行左右移动；为 false 时改为按 X 坐标几何邻居
    public bool useListOrder = true;

    // 是否到达目标点后自动选最近点作为当前索引（仅在 useListOrder = false 下首次使用方向键时计算）
    public bool autoInitializeIndexFromNearest = true;

    // 当前正在前往的目标点
    private Transform targetPoint;

    // 当前索引（仅在 useListOrder == true 或用于记录几何选中的点）
    private int currentIndex = -1;

    // 是否由方向键或映射键控制（移动过程中屏蔽鼠标）
    private bool isPointKeyControl = false;

    void Update()
    {
        HandleArrowKeyNavigation();
        HandleDirectPointKeyInput();
        HandleMouseInput();
        MoveTowardsTarget();
    }

    // 方向键选择下一个/上一个目标点（不再进行自由移动）
    private void HandleArrowKeyNavigation()
    {
        bool left = Input.GetKeyDown(KeyCode.LeftArrow);
        bool right = Input.GetKeyDown(KeyCode.RightArrow);
        if (!left && !right) return;

        int dir = right ? 1 : -1;
        SelectAdjacentPoint(dir);
    }

    // 通过自定义映射键直接跳到指定点
    private void HandleDirectPointKeyInput()
    {
        if (selectablePoints == null || selectablePoints.Count == 0) return;

        for (int i = 0; i < pointKeys.Count && i < selectablePoints.Count; i++)
        {
            if (Input.GetKeyDown(pointKeys[i]))
            {
                var p = selectablePoints[i];
                if (p == null) continue;
                targetPoint = p;
                currentIndex = i;
                isPointKeyControl = true;
                return;
            }
        }
    }

    // 鼠标点击选择最近点（仅在非键盘控制时）
    private void HandleMouseInput()
    {
        if (isPointKeyControl) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            targetPoint = FindNearestPoint(hit.point);
            // 更新索引（如果找到且在列表中）
            if (targetPoint != null)
            {
                for (int i = 0; i < selectablePoints.Count; i++)
                {
                    if (selectablePoints[i] == targetPoint)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }
        }
    }

    private void MoveTowardsTarget()
    {
        if (targetPoint == null) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPoint.position,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPoint.position) < 0.01f)
        {
            // 到达
            targetPoint = null;
            isPointKeyControl = false;
        }
    }

    // 根据方向选择相邻点
    private void SelectAdjacentPoint(int direction)
    {
        if (selectablePoints == null || selectablePoints.Count == 0) return;

        // 过滤空引用
        for (int i = selectablePoints.Count - 1; i >= 0; i--)
        {
            if (selectablePoints[i] == null) selectablePoints.RemoveAt(i);
        }
        if (selectablePoints.Count == 0) return;

        if (useListOrder)
        {
            // 初始化索引
            if (currentIndex < 0 || currentIndex >= selectablePoints.Count)
            {
                currentIndex = FindNearestIndex(transform.position);
            }
            int nextIndex = currentIndex + direction;
            if (nextIndex < 0 || nextIndex >= selectablePoints.Count) return;
            currentIndex = nextIndex;
            targetPoint = selectablePoints[currentIndex];
            isPointKeyControl = true;
        }
        else
        {
            // 几何模式：找 X 轴上最近的左/右邻居
            Vector3 originPos = transform.position;
            // 若当前已有索引并且对应点存在，可用该点位置做参考
            if (currentIndex >= 0 && currentIndex < selectablePoints.Count && selectablePoints[currentIndex] != null)
            {
                originPos = selectablePoints[currentIndex].position;
            }
            else if (autoInitializeIndexFromNearest)
            {
                currentIndex = FindNearestIndex(originPos);
                if (currentIndex >= 0) originPos = selectablePoints[currentIndex].position;
            }

            Transform best = null;
            float bestDelta = direction > 0 ? float.PositiveInfinity : float.NegativeInfinity;
            int bestIndex = -1;

            for (int i = 0; i < selectablePoints.Count; i++)
            {
                var p = selectablePoints[i];
                if (p == null) continue;
                float dx = p.position.x - originPos.x;
                if (direction > 0)
                {
                    if (dx > 0f && dx < bestDelta)
                    {
                        bestDelta = dx;
                        best = p;
                        bestIndex = i;
                    }
                }
                else
                {
                    if (dx < 0f && dx > bestDelta)
                    {
                        bestDelta = dx;
                        best = p;
                        bestIndex = i;
                    }
                }
            }

            if (best != null)
            {
                targetPoint = best;
                currentIndex = bestIndex;
                isPointKeyControl = true;
            }
        }
    }

    private int FindNearestIndex(Vector3 fromPos)
    {
        float shortest = float.PositiveInfinity;
        int idx = -1;
        for (int i = 0; i < selectablePoints.Count; i++)
        {
            var p = selectablePoints[i];
            if (p == null) continue;
            float d = Vector3.SqrMagnitude(p.position - fromPos);
            if (d < shortest)
            {
                shortest = d;
                idx = i;
            }
        }
        return idx;
    }

    private Transform FindNearestPoint(Vector3 pos)
    {
        Transform nearest = null;
        float shortest = float.PositiveInfinity;
        foreach (var p in selectablePoints)
        {
            if (p == null) continue;
            float d = Vector3.SqrMagnitude(p.position - pos);
            if (d < shortest)
            {
                shortest = d;
                nearest = p;
            }
        }
        return nearest;
    }
}
