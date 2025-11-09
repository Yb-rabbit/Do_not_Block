using UnityEngine;

public class BlackBlock3D : MonoBehaviour
{
    private FourBeatRhythm gameController; // 游戏控制器引用
    private Transform judgmentPoint; // 判定点引用
    private bool isClickedOrNear = false; // 是否已点击或靠近判定点

    public void Initialize(FourBeatRhythm controller, Transform judgment)
    {
        gameController = controller;
        judgmentPoint = judgment;
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
        if (!isClickedOrNear)
        {
            ChangeColor(); // 变色
            isClickedOrNear = true;
        }
    }

    public bool IsClickedOrNear()
    {
        return isClickedOrNear;
    }

    private void ChangeColor()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.gray; // 将颜色变为灰色
        }
    }
}