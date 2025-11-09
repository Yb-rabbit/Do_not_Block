using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FourBeat3 : MonoBehaviour
{
    public GameObject blackBlockPrefab; // 黑块预制体
    public Transform[] spawnPoints; // 四列生成点
    public Transform judgmentPoint; // 判定点对象
    public int maxActiveBlocks = 10; // 最大激活黑块数量
    public float minBlockSpeed = 0.5f; // 最慢音符速度

    private RhythmGenerator rhythmGenerator; // 引用节奏生成器
    private int activeBlockCount = 0; // 当前激活的黑块数量

    private class PoolItem
    {
        public GameObject go;
        public Transform transform;
        public BlackBlock blockComponent;
        public bool active;
    }

    private List<PoolItem> pool = new List<PoolItem>();
    private int poolSize = 20; // 对象池大小

    void Start()
    {
        if (blackBlockPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("请确保黑块预制体和生成点已正确设置！");
            enabled = false;
            return;
        }

        // 初始化对象池
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = Instantiate(blackBlockPrefab);
            go.SetActive(false);
            pool.Add(new PoolItem
            {
                go = go,
                transform = go.transform,
                blockComponent = go.GetComponent<BlackBlock>(),
                active = false
            });
        }

        StartCoroutine(GenerateBeats());
    }

    public void SetRhythmGenerator(RhythmGenerator generator)
    {
        rhythmGenerator = generator;
    }

    IEnumerator GenerateBeats()
    {
        while (true) // 无限生成节奏
        {
            // 检查是否超过最大激活数量
            if (activeBlockCount >= maxActiveBlocks)
            {
                yield return new WaitUntil(() => activeBlockCount < maxActiveBlocks);
            }

            // 动态生成节奏
            if (rhythmGenerator != null)
            {
                Beat beat = rhythmGenerator.GenerateNextBeat();
                SpawnBlackBlock(beat.column);
            }

            yield return new WaitForSeconds(60f / rhythmGenerator.bpm); // 等待下一拍
        }
    }

    void SpawnBlackBlock(int column)
    {
        // 从对象池中获取一个空闲的黑块
        PoolItem item = pool.Find(p => !p.active);
        if (item == null)
        {
            Debug.LogWarning("对象池已满，无法生成更多黑块！");
            return;
        }

        // 激活黑块并设置位置
        item.active = true;
        item.go.SetActive(true);
        item.transform.position = spawnPoints[column].position;

        // 设置黑块的速度、长度和列信息
        float speed = Mathf.Clamp(1f / (60f / rhythmGenerator.bpm), minBlockSpeed, 10f);
        item.blockComponent.SetSpeedAndLength(speed, 1f, column); // 传递列号
        item.blockComponent.judgmentPoint = judgmentPoint;

        // 设置黑块到达判定点后的回调
        item.blockComponent.OnHit = () => DeactivateItem(item);

        // 增加激活黑块计数
        activeBlockCount++;
    }

    void DeactivateItem(PoolItem item)
    {
        if (item == null) return;
        item.active = false;
        item.go.SetActive(false);
    }
}