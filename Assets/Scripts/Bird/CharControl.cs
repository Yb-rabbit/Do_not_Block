using System.Collections.Generic;
using UnityEngine;

public class CharControl : MonoBehaviour
{
    // 可选的目标点列表
    public List<Transform> selectablePoints;

    // 每个目标点对应的键位
    public List<KeyCode> pointKeys;

    // 移动速度
    public float moveSpeed = 5f;

    // 当前目标点
    private Transform targetPoint;

    // 是否由键盘控制
    private bool isKeyboardControl = false;

    void Update()
    {
        // 检测键盘输入
        for (int i = 0; i < pointKeys.Count && i < selectablePoints.Count; i++)
        {
            if (Input.GetKeyDown(pointKeys[i]))
            {
                targetPoint = selectablePoints[i];
                isKeyboardControl = true; // 激活键盘控制
                break;
            }
        }

        // 如果不是键盘控制，检测鼠标点击
        if (!isKeyboardControl && Input.GetMouseButtonDown(0))
        {
            // 获取鼠标点击的世界坐标
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // 找到最近的目标点
                targetPoint = FindNearestPoint(hit.point);
            }
        }

        // 如果有目标点，移动到目标点
        if (targetPoint != null)
        {
            MoveToTarget();
        }
    }

    // 找到最近的目标点
    private Transform FindNearestPoint(Vector3 clickPosition)
    {
        Transform nearestPoint = null;
        float shortestDistance = Mathf.Infinity;

        foreach (Transform point in selectablePoints)
        {
            float distance = Vector3.Distance(clickPosition, point.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestPoint = point;
            }
        }

        return nearestPoint;
    }

    // 移动到目标点
    private void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);

        // 如果到达目标点，停止移动
        if (Vector3.Distance(transform.position, targetPoint.position) < 0.1f)
        {
            targetPoint = null;
            isKeyboardControl = false; // 停止移动后，解除键盘控制
        }
    }
}
