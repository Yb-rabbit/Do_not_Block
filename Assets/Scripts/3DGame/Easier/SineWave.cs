using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SineWave : MonoBehaviour
{
    public GameObject blackBlockPrefab; // 黑块预制体
    public Transform[] spawnPoints; // 四列生成点
    public Transform judgmentPoint; // 判定点对象
    public AudioSource audioSource; // 音乐播放器
    public float bpm = 120f; // 每分钟节拍数
    public float baseSpeed = 2f; // 基础下落速度
    public float maxSpeed = 6f; // 最大下落速度
    public float sensitivity = 5f; // 音频节奏密集度灵敏度
    public Text scoreText; // UI Text 用于显示得分

    private float beatInterval; // 每拍的时间间隔
    private float nextBeatTime; // 下一拍的时间点
    private float currentSpeed; // 当前下落速度
    private float[] spectrumData = new float[64]; // 存储频谱数据

    private List<GameObject> activeBlocks = new List<GameObject>(); // 活跃的黑块列表
    private int score = 0; // 当前得分

    // 正弦波参数
    private float amplitude = 1f; // 振幅
    private float frequency = 1f; // 频率
    private float[] columnOffsets; // 每列的时间偏移量

    void Start()
    {
        beatInterval = 60f / bpm; // 计算每拍的时间间隔
        nextBeatTime = Time.time; // 初始化下一拍时间
        currentSpeed = baseSpeed; // 初始化下落速度
        columnOffsets = new float[spawnPoints.Length]; // 初始化每列的时间偏移量
        for (int i = 0; i < columnOffsets.Length; i++)
        {
            columnOffsets[i] = Random.Range(0f, Mathf.PI * 2); // 为每列生成一个随机的时间偏移
        }
        UpdateScoreUI(); // 初始化 UI 显示
    }

    void Update()
    {
        // 检查是否到达下一拍时间
        if (Time.time >= nextBeatTime)
        {
            SpawnBlock(); // 生成黑块
            nextBeatTime += beatInterval; // 更新下一拍时间
        }

        // 获取频谱数据
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.Blackman);

        // 根据频谱数据调整下落速度
        AdjustSpeedBasedOnRhythm();

        // 更新黑块位置
        UpdateBlockPositions();
    }

    void SpawnBlock()
    {
        int randomColumn = Random.Range(0, spawnPoints.Length); // 随机选择一列
        Vector3 spawnPosition = spawnPoints[randomColumn].position;

        // 根据正弦波绝对值计算 Y 轴偏移
        float sineOffset = Mathf.Abs(Mathf.Sin(Time.time * frequency + columnOffsets[randomColumn])) * amplitude;
        spawnPosition.y += sineOffset;

        GameObject block = Instantiate(blackBlockPrefab, spawnPosition, Quaternion.identity);
        activeBlocks.Add(block); // 添加到活跃黑块列表

        // 添加鼠标点击检测
        BlackBlock3D blackBlock = block.AddComponent<BlackBlock3D>();
        blackBlock.Initialize(this, judgmentPoint);
    }

    void AdjustSpeedBasedOnRhythm()
    {
        float averageAmplitude = 0f;

        // 计算频谱数据的平均值
        for (int i = 0; i < spectrumData.Length; i++)
        {
            averageAmplitude += spectrumData[i];
        }
        averageAmplitude /= spectrumData.Length;

        // 根据平均值调整速度
        currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, averageAmplitude * sensitivity);
    }

    void UpdateBlockPositions()
    {
        for (int i = activeBlocks.Count - 1; i >= 0; i--)
        {
            GameObject block = activeBlocks[i];
            if (block == null) continue;

            // 更新黑块位置（沿 Z 轴移动）
            block.transform.Translate(Vector3.back * currentSpeed * Time.deltaTime);

            // 检查是否超出判定点
            if (block.transform.position.z < judgmentPoint.position.z)
            {
                BlackBlock3D blackBlock = block.GetComponent<BlackBlock3D>();
                if (blackBlock != null)
                {
                    // 用 IsClickedOrNear 方法判断是否被点击或靠近
                    if (blackBlock.IsClickedOrNear())
                    {
                        AddScore(); // 增加分数
                    }
                }

                Destroy(block); // 销毁黑块
                activeBlocks.RemoveAt(i); // 从列表中移除
            }
        }
    }

    public void AddScore()
    {
        score++;
        Debug.Log($"当前得分: {score}");
        UpdateScoreUI(); // 更新 UI 显示
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"- {score} -"; // 更新 UI 文本
        }
    }
}