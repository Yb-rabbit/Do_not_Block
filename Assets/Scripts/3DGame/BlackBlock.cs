using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackBlock : MonoBehaviour
{
    private float speed; // 移动速度
    private bool isHit = false; // 是否被点击

    public Transform judgmentPoint; // 判定点对象
    public System.Action OnMissed; // 未点击回调
    public System.Action OnHit; // 点击回调

    public static List<BlackBlock> activeBlocks = new List<BlackBlock>(); // 活跃黑块列表
    public int column; // 黑块所在列
    public float collisionThreshold = 0.1f; // 碰撞检测阈值

    void OnEnable()
    {
        activeBlocks.Add(this); // 添加到活跃列表
    }

    void OnDisable()
    {
        activeBlocks.Remove(this); // 从活跃列表移除
    }

    public void SetSpeedAndLength(float speed, float length, int column)
    {
        this.speed = speed;
        this.column = column; // 设置列信息
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, length); // 调整长度
    }

    void Update()
    {
        if (speed <= 0 || judgmentPoint == null) return;

        // 向判定点移动
        transform.Translate(Vector3.back * speed * Time.deltaTime);

        // 如果超出判定点且未被点击，判定为遗漏并回收
        if (transform.position.z < judgmentPoint.position.z)
        {
            if (!isHit)
            {
                Debug.Log($"BlackBlock missed: {gameObject.name}");
                OnMissed?.Invoke(); // 调用未点击回调
            }
        }
    }

    void OnMouseDown()
    {
        // 检测鼠标点击
        if (isHit) return;
        isHit = true;

        Debug.Log($"BlackBlock clicked: {gameObject.name}");
        OnHit?.Invoke(); // 调用点击回调

        // 检测同列连续的黑块
        CheckAdjacentBlocks();
    }

    private void CheckAdjacentBlocks()
    {
        foreach (var block in activeBlocks)
        {
            if (block == this || block.isHit) continue; // 跳过自身或已点击的黑块

            // 检查是否在同一列
            if (block.column == this.column)
            {
                // 检查是否与当前黑块相邻（碰撞到一起）
                float distance = Mathf.Abs(transform.position.z - block.transform.position.z);
                if (distance <= collisionThreshold)
                {
                    Debug.Log($"BlackBlock {block.gameObject.name} also removed due to adjacency.");
                    block.isHit = true;
                    block.OnHit?.Invoke(); // 触发相邻黑块的点击回调
                }
            }
        }
    }

    public void ResetBlock()
    {
        isHit = false;
        // 重置其他状态（如颜色、位置等）
    }
}
