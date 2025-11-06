using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WhiteBlockGame : MonoBehaviour
{
    // 配置参数
    [Header("游戏设置")]
    public int rows = 6;       // 屏幕内显示的行数
    public int cols = 4;       // 列数
    public float blockSize = 100f;  // 方块大小
    public float startSpeed = 3f;   // 初始速度
    public float speedIncrease = 0.5f;  // 每得分加速值

    // 引用
    [Header("引用")]
    public GameObject blockPrefab;  // 方块预制体
    public Transform gameArea;      // 游戏区域父物体
    public TextMeshProUGUI scoreText;  // 分数文本
    public TextMeshProUGUI speedText;  // 速度文本
    public GameObject gameOverPanel;   // 游戏结束面板
    public GameObject pausePanel;      // 暂停面板

    // 游戏状态
    private List<List<GameObject>> blocks = new List<List<GameObject>>();
    private int score = 0;
    private float speed;
    private float scrollOffset = 0;
    private bool isGameOver = false;
    private bool isPaused = false;

    void Start()
    {
        ResetGame();  // 初始化游戏
    }

    void Update()
    {
        if (isGameOver || isPaused) return;

        // 方块滚动逻辑
        scrollOffset += speed * Time.deltaTime;

        // 当滚动距离超过方块大小时，生成新行并重置偏移
        if (scrollOffset >= blockSize)
        {
            AddNewRow();
            scrollOffset -= blockSize;
            RemoveOldRow();  // 移除超出屏幕的行
        }

        // 更新所有方块位置（滚动效果）
        UpdateBlockPositions();

        // 检查是否有黑块移出屏幕
        CheckMissedBlackBlock();
    }

    // 初始化/重置游戏
    public void ResetGame()
    {
        // 清空已有方块
        foreach (var row in blocks)
        {
            foreach (var block in row)
                Destroy(block);
        }
        blocks.Clear();

        // 重置状态
        score = 0;
        speed = startSpeed;
        scrollOffset = 0;
        isGameOver = false;
        isPaused = false;

        // 更新UI
        scoreText.text = $"得分: {score}";
        speedText.text = $"速度: {speed:F1}";
        gameOverPanel.SetActive(false);
        pausePanel.SetActive(false);

        // 生成初始行
        for (int i = 0; i < rows; i++)
        {
            AddNewRow();
        }
    }

    // 添加新行（顶部生成）
    void AddNewRow()
    {
        List<GameObject> newRow = new List<GameObject>();
        int blackCol = Random.Range(0, cols);  // 随机黑块列

        for (int col = 0; col < cols; col++)
        {
            // 实例化方块
            GameObject block = Instantiate(blockPrefab, gameArea);
            block.name = $"Block_{blocks.Count}_{col}";

            // 设置位置（初始在屏幕顶部外）
            RectTransform rect = block.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                col * blockSize + blockSize / 2,  // X坐标
                -scrollOffset - blockSize / 2     // Y坐标（初始在顶部外）
            );
            rect.sizeDelta = new Vector2(blockSize - 2, blockSize - 2);  // 留间隙

            // 设置颜色（黑块/白块）
            Image image = block.GetComponent<Image>();
            bool isBlack = (col == blackCol);
            image.color = isBlack ? Color.black : Color.white;
            block.GetComponent<Button>().onClick.AddListener(() => OnBlockClick(block, isBlack));

            newRow.Add(block);
        }

        blocks.Insert(0, newRow);  // 添加到顶部
    }

    // 移除超出屏幕的行（底部）
    void RemoveOldRow()
    {
        if (blocks.Count > rows)
        {
            var oldRow = blocks[blocks.Count - 1];
            foreach (var block in oldRow)
                Destroy(block);
            blocks.RemoveAt(blocks.Count - 1);
        }
    }

    // 更新所有方块位置（实现滚动效果）
    void UpdateBlockPositions()
    {
        for (int row = 0; row < blocks.Count; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                RectTransform rect = blocks[row][col].GetComponent<RectTransform>();
                // 计算Y坐标（随滚动偏移更新）
                float yPos = -scrollOffset + row * blockSize - blockSize / 2;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, yPos);
            }
        }
    }

    // 方块点击事件
    void OnBlockClick(GameObject block, bool isBlack)
    {
        if (isGameOver || isPaused) return;

        // 点击反馈（短暂变灰）
        Image image = block.GetComponent<Image>();
        image.color = Color.gray;
        Invoke(nameof(ResetBlockColor), 0.1f);  // 0.1秒后恢复颜色

        if (isBlack)
        {
            // 点击黑块：得分
            score++;
            scoreText.text = $"得分: {score}";

            // 每得5分加速
            if (score % 5 == 0)
            {
                speed += speedIncrease;
                speedText.text = $"速度: {speed:F1}";
            }
        }
        else
        {
            // 点击白块：游戏结束
            GameOver();
        }

        // 重置方块颜色（内部方法）
        void ResetBlockColor()
        {
            image.color = isBlack ? Color.black : Color.white;
        }
    }

    // 检查是否有黑块未点击就移出屏幕
    void CheckMissedBlackBlock()
    {
        if (blocks.Count == 0) return;

        // 检查最底部一行
        var bottomRow = blocks[blocks.Count - 1];
        foreach (var block in bottomRow)
        {
            Image image = block.GetComponent<Image>();
            if (image.color == Color.black)  // 黑块未被点击且已移出屏幕
            {
                RectTransform rect = block.GetComponent<RectTransform>();
                if (rect.anchoredPosition.y < -Screen.height / 2)
                {
                    GameOver();
                    break;
                }
            }
        }
    }

    // 游戏结束
    void GameOver()
    {
        isGameOver = true;
        gameOverPanel.SetActive(true);
    }

    // 暂停/继续游戏
    public void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
    }

    // 调整速度（暂停时调用）
    public void ChangeSpeed(float delta)
    {
        if (!isPaused) return;
        speed = Mathf.Clamp(speed + delta, 1f, 10f);  // 限制速度范围1-10
        speedText.text = $"速度: {speed:F1}";
    }
}