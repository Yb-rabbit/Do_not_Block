using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBeat2 : MonoBehaviour
{
    [Header("布局")]
    public int cols = 4;
    public float blockSize = 100f; // 像素
    [Header("速度/节奏")]
    public float speed = 200f; // 像素/秒
    // 每次生成间隔（可留空让脚本根据 blockSize/speed 计算）
    public float spawnInterval = 0f;

    [Header("引用")]
    public GameObject blackBlockPrefab; // 需包含 Button + Image
    public RectTransform gameArea;      // UI Panel 的 RectTransform（白色背景）
    public Text scoreText;
    public GameObject gameOverPanel;
    public GameObject ColorControlObject;

    class PoolItem
    {
        public GameObject go;
        public RectTransform rect;
        public Button btn;
        public Image img;
        public bool active;
        public bool clicked;
    }

    private List<PoolItem> pool = new List<PoolItem>();
    private float spawnTimer = 0f;
    private int score = 0;
    private bool isGameOver = false;
    private float bottomBoundary;
    private float topY;
    private List<float> columnXs = new List<float>();

    void Start()
    {
        if (gameArea == null || blackBlockPrefab == null)
        {
            Debug.LogWarning("[NewBeat2] 请在 Inspector 关联 gameArea 与 blackBlockPrefab。");
            enabled = false;
            return;
        }

        // 计算列 X 位置（中心对齐）
        float areaWidth = gameArea.rect.width;
        float startX = -areaWidth / 2f + blockSize / 2f;
        columnXs.Clear();
        for (int c = 0; c < cols; c++) columnXs.Add(startX + c * blockSize);

        // 计算上下边界
        topY = gameArea.rect.height / 2f + blockSize * 0.5f; // 放在顶部外
        bottomBoundary = -gameArea.rect.height / 2f - blockSize * 0.5f;

        // spawnInterval 默认根据 blockSize 和 speed 推算（让行间隔 = blockSize）
        if (spawnInterval <= 0f) spawnInterval = blockSize / speed;

        // 初始化对象池（确保足够，可用 visibleRows + 2）
        int poolSize = Mathf.Max(6, Mathf.CeilToInt(gameArea.rect.height / blockSize) + 4);
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(blackBlockPrefab, gameArea);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(blockSize - 2f, blockSize - 2f);
            var btn = go.GetComponent<Button>();
            var img = go.GetComponent<Image>();
            var item = new PoolItem { go = go, rect = rect, btn = btn, img = img, active = false, clicked = false };
            go.SetActive(false);
            pool.Add(item);
        }

        ResetGame();
    }

    void Update()
    {
        if (isGameOver) return;

        // 生成节奏计时器
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer -= spawnInterval;
            SpawnBlackBlock();
        }

        float move = speed * Time.deltaTime;

        // 移动所有激活的黑块
        foreach (var item in pool)
        {
            if (!item.active) continue;
            item.rect.anchoredPosition += new Vector2(0, -move);

            // 到达底部
            if (item.rect.anchoredPosition.y < bottomBoundary)
            {
                if (!item.clicked)
                {
                    // 漏点 -> 游戏结束
                    GameOver();
                    return;
                }
                // 点击过的块到达底部后回收
                DeactivateItem(item);
            }
        }
    }

    void SpawnBlackBlock()
    {
        // 找到一个空闲的池项
        PoolItem item = pool.Find(p => !p.active);
        if (item == null) return;

        // 随机列
        int col = Random.Range(0, cols);
        float x = columnXs[col];

        item.rect.anchoredPosition = new Vector2(x, topY);
        item.img.color = Color.black;
        item.clicked = false;
        item.active = true;
        item.go.SetActive(true);

        // 设置按钮回调（移除旧监听以防重复）
        if (item.btn != null)
        {
            item.btn.onClick.RemoveAllListeners();
            item.btn.onClick.AddListener(() => OnBlackClicked(item));
            item.btn.interactable = true;
        }
    }

    void OnBlackClicked(PoolItem item)
    {
        if (isGameOver || item == null || !item.active) return;

        // 播放全局音频管理器中的击中音（如果存在）
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayHit();

        // 计分与视觉反馈
        score++;
        UpdateScoreUI();
        item.clicked = true;
        if (item.img != null) item.img.color = Color.gray;
        if (item.btn != null) item.btn.interactable = false;

        // 变色反馈
        ColorControl colorControl = gameArea.GetComponent<ColorControl>();
        if (colorControl != null)
        {
            colorControl.OnScore(); // 调用变色逻辑
        }
        else
        {
            Debug.LogWarning("[NewBeat2] 未找到 ColorControl 脚本，请检查是否挂载到 gameArea。");
        }
    }

    void DeactivateItem(PoolItem item)
    {
        if (item == null) return;
        item.active = false;
        item.clicked = false;
        item.go.SetActive(false);
    }

    void GameOver()
    {
        isGameOver = true;
        if (gameOverPanel) gameOverPanel.SetActive(true);
        Debug.Log("[NewBeat2] GameOver");
    }

    void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
    }

    // 可从 Inspector 调用以重置
    public void ResetGame()
    {
        isGameOver = false;
        score = 0;
        spawnTimer = 0f;
        foreach (var item in pool) DeactivateItem(item);
        UpdateScoreUI();
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }
}
