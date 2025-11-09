using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleWhiteBlockGame : MonoBehaviour
{
    [Header("布局")]
    public int cols = 4;
    public int visibleRows = 6;
    public float blockSize = 100f; // 像素
    [Header("速度")]
    public float startSpeed = 200f; // 像素/秒
    public float speedIncrease = 50f;
    [Header("引用")]
    public GameObject blockPrefab; // Button + Image
    public RectTransform gameArea; // UI Panel 的 RectTransform
    public Text scoreText;
    public Text speedText;
    public GameObject gameOverPanel;

    class Row
    {
        public GameObject root;
        public RectTransform rect;
        public Button[] buttons;
        public Image[] images;
        public int blackIndex;
        public bool blackClicked;
    }

    private List<Row> rows = new List<Row>();
    private int score = 0;
    private float speed;
    private bool isGameOver = false;

    void Start()
    {
        speed = startSpeed;
        InitRows();
        UpdateUI();
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (isGameOver) return;
        if (gameArea == null) return;

        float delta = speed * Time.deltaTime;
        // 移动每一行
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            r.rect.anchoredPosition += new Vector2(0, -delta);
        }

        // 底部判断与回收
        float halfHeight = gameArea.rect.height / 2f;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            // 当整行越过底部一定距离（行中心低于底边 - blockSize/2）
            if (r.rect.anchoredPosition.y < -halfHeight - blockSize * 0.5f)
            {
                // 若黑块未被点击 -> GameOver
                if (!r.blackClicked)
                {
                    GameOver();
                    return;
                }
                // 回收到顶部并重置
                RecycleRowToTop(r);
            }
        }
    }

    void InitRows()
    {
        // 清理（如果脚本重启）
        foreach (var r in rows)
            if (r != null && r.root != null) Destroy(r.root);
        rows.Clear();

        if (gameArea == null || blockPrefab == null) return;

        float areaWidth = gameArea.rect.width;
        // 计算起始 X (中心对齐)
        float startX = -areaWidth / 2f + blockSize / 2f;

        // topY：第一行的中心 y（放在可视区顶部内）
        float topY = gameArea.rect.height / 2f - blockSize / 2f;

        for (int i = 0; i < visibleRows; i++)
        {
            GameObject rowGO = new GameObject($"Row_{i}", typeof(RectTransform));
            rowGO.transform.SetParent(gameArea, false);
            RectTransform rRect = rowGO.GetComponent<RectTransform>();
            rRect.sizeDelta = new Vector2(areaWidth, blockSize);
            float y = topY - i * blockSize;
            rRect.anchoredPosition = new Vector2(0, y);

            Button[] buttons = new Button[cols];
            Image[] images = new Image[cols];

            for (int c = 0; c < cols; c++)
            {
                GameObject b = Instantiate(blockPrefab, rowGO.transform);
                RectTransform br = b.GetComponent<RectTransform>();
                br.anchorMin = new Vector2(0, 0.5f);
                br.anchorMax = new Vector2(0, 0.5f);
                br.pivot = new Vector2(0.5f, 0.5f);
                br.sizeDelta = new Vector2(blockSize - 2f, blockSize - 2f);
                br.anchoredPosition = new Vector2(startX + c * blockSize, 0);

                Button btn = b.GetComponent<Button>();
                Image img = b.GetComponent<Image>();
                buttons[c] = btn;
                images[c] = img;
            }

            Row row = new Row { root = rowGO, rect = rRect, buttons = buttons, images = images };
            RandomizeRow(row);
            // 添加点击监听（注意闭包）
            for (int c = 0; c < cols; c++)
            {
                int col = c;
                Button btn = row.buttons[col];
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnBlockPressed(row, col));
            }

            rows.Add(row);
        }
    }

    void RandomizeRow(Row r)
    {
        r.blackIndex = Random.Range(0, cols);
        r.blackClicked = false;
        for (int c = 0; c < cols; c++)
        {
            r.images[c].color = (c == r.blackIndex) ? Color.black : Color.white;
            if (r.buttons[c] != null) r.buttons[c].interactable = true;
        }
    }

    void OnBlockPressed(Row r, int col)
    {
        if (isGameOver) return;
        if (r == null) return;

        if (col == r.blackIndex)
        {
            // 正确：计分并标记
            score++;
            r.blackClicked = true;
            // 视觉反馈并禁用按钮
            r.images[col].color = Color.gray;
            if (r.buttons[col] != null) r.buttons[col].interactable = false;

            if (score % 5 == 0)
            {
                speed += speedIncrease;
            }
            UpdateUI();
        }
        else
        {
            // 错误：踩白块
            GameOver();
        }
    }

    void RecycleRowToTop(Row r)
    {
        // 找到当前最大的 y
        float maxY = float.MinValue;
        foreach (var rr in rows) if (rr.rect.anchoredPosition.y > maxY) maxY = rr.rect.anchoredPosition.y;
        float newY = maxY + blockSize;
        r.rect.anchoredPosition = new Vector2(r.rect.anchoredPosition.x, newY);
        RandomizeRow(r);
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"得分: {score}";
        if (speedText) speedText.text = $"速度: {speed:F0}";
    }

    void GameOver()
    {
        isGameOver = true;
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    // 可在 inspector 调用以重置游戏
    public void ResetGame()
    {
        isGameOver = false;
        score = 0;
        speed = startSpeed;
        // 重新随机所有行并调整位置
        float areaWidth = gameArea.rect.width;
        float topY = gameArea.rect.height / 2f - blockSize / 2f;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            r.rect.anchoredPosition = new Vector2(0, topY - i * blockSize);
            RandomizeRow(r);
        }
        UpdateUI();
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }
}