using System;
using UnityEngine;

public class BlackBlock3D : MonoBehaviour
{
    private SineWave sineWaveController;
    private Transform judgmentPoint;
    private bool isClickedOrNear;

    // 实现 Initialize 方法
    public void Initialize(SineWave sineWave, Transform judgment)
    {
        sineWaveController = sineWave;
        judgmentPoint = judgment;
        isClickedOrNear = false; // 初始化状态
    }

    void Update()
    {
        // 检测是否靠近判定点
        if (judgmentPoint != null && !isClickedOrNear && transform.position.z <= judgmentPoint.position.z + 1f)
        {
            ChangeColor(); // 变色
            isClickedOrNear = true;
        }
    }

    void OnMouseDown()
    {
        // 示例实现：当鼠标点击时，设置 isClickedOrNear 为 true
        isClickedOrNear = true;
        ChangeColor(); // 改变颜色以反馈点击
    }

    public bool IsClickedOrNear()
    {
        // 示例实现：返回是否被点击或靠近
        return isClickedOrNear;
    }

    private void ChangeColor()
    {
        // 示例实现：改变黑块的颜色
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.gray; // 点击后变色
        }
    }
}