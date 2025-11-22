using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 四列节奏/黑块 + UI 下落展示（使用场景中的 Transform 作为生成点）。
/// 新增：动态检测并对齐生成的场景黑块的 X 到指定物体(对齐目标)的 X。
/// </summary>
public class ShowNote : MonoBehaviour
{
    #region 触发 / 节奏
    [Header("触发与节奏")]
    public bool autoLoop = true;
    public float loopInterval = 0f;
    [Tooltip("loopInterval <= 0 时使用 fallbackInterval")]
    public float fallbackInterval = 1.0f;
    public KeyCode triggerKey = KeyCode.Space;
    public bool singleSpawnPerInterval = true;
    #endregion

    #region 使用场景 Transform 生成
    [Header("世界生成点")]
    [Tooltip("启用后：使用下面的 Transform 数组作为世界黑块生成位置")]
    public bool useSpawnTransforms = true;
    [Tooltip("拖入场景中的生成点（顺序即列索引）")]
    public Transform[] spawnTransforms;
    [Tooltip("列数（<=0 时自动 = spawnTransforms.Length）")]
    public int columns = 0;
    [Tooltip("UI X 是否按索引均匀排列；false 则按世界 X 映射")]
    public bool uiUseIndexSpacing = true;
    [Tooltip("UI 均匀排列间距")]
    public float uiColumnSpacing = 110f;
    [Tooltip("世界黑块基准位置（不用 Transform 时）")]
    public Vector3 worldStartBasePos = new(0, 2, 4);
    #endregion

    #region 判定 / 黑块
    [Header("判定与黑块")]
    [Tooltip("判定线（Z 必须小于生成点 Z）")]
    public Transform judgmentPoint;
    [Tooltip("黑块移动速度（<=0 自动按距离/下落时长推导）")]
    public float worldMoveSpeed = 0f;
    [Tooltip("黑块长度（沿 Z）")]
    public float worldBlockLength = 1.0f;
    public bool worldScaleFadeIn = true;
    public bool syncUIFromWorldProgress = true;
    public GameObject worldBlockPrefab;
    #endregion

    #region 动态 X 对齐
    [Header("动态 X 对齐")]
    [Tooltip("启用后：生成后的黑块会在 Update 中动态对齐 X 到指定对齐目标")]
    public bool dynamicAlignX = false;
    [Tooltip("对齐模式：若为 true 使用单一对齐目标 singleAlignTarget；否则按列使用 alignTargets 数组")]
    public bool useSingleAlignTarget = true;
    [Tooltip("单一对齐目标（全部黑块 X 都对齐该目标）")]
    public Transform singleAlignTarget;
    [Tooltip("按列对齐目标（长度需 >= columns；列索引对应目标数组下标）")]
    public Transform[] alignTargets;
    [Tooltip("X 对齐平滑（0=立即，>0 逐渐插值）")]
    public float alignXSmooth = 0.1f;
    [Tooltip("当对齐目标缺失时是否转为初始 X 锁定不变")]
    public bool keepOriginalXIfTargetMissing = true;
    [Tooltip("仅当偏移超过该阈值才更新（减少抖动）")]
    public float alignThreshold = 0.001f;
    #endregion

    #region UI 下落
    [Header("UI 下落")]
    public RectTransform uiParent;
    public GameObject uiNotePrefab;
    public float uiStartY = 300f;
    public float uiEndY = -300f;
    public float uiFallDuration = 1.2f;
    public AnimationCurve uiFallCurve;
    public bool forceCenterParent = true;
    public bool forceCenterNoteRect = true;
    #endregion

    #region 颜色 / 可视化
    [Header("颜色 / 可视化")]
    [Tooltip("按列着色，不足自动补全")]
    public Color[] columnColors;
    public bool debugGuides = true;
    public Color guideColor = new(0f, 0f, 0f, 0.2f);
    public float guideLineWidth = 4f;
    #endregion

    #region 事件
    [Header("事件")]
    public UnityEvent onNoteArrived;
    public UnityEvent onNoteHit;
    #endregion

    #region 回收与对象池
    [Header("回收与对象池")]
    public bool destroyOnArrive = true;
    public bool useSimplePool = true;
    public int initialPoolSize = 12;
    #endregion

    // UI 列 X
    private float[] _uiXs;
    // 池
    private readonly List<GameObject> _uiPool = new();
    private readonly List<GameObject> _worldPool = new();
    // 计时
    private float _spawnTimer;
    private float _interval;

    // 缺失变换警告
    private HashSet<int> _missingTransformWarned = new();

    // 活跃世界块记录（用于动态 X 对齐）
    private class ActiveWorldBlock
    {
        public GameObject go;
        public BlackBlock bb;
        public int column;
        public float originalX;
        public Transform alignTarget;
    }
    private readonly List<ActiveWorldBlock> _activeWorldBlocks = new();

    void OnValidate()
    {
        if (useSpawnTransforms)
        {
            int len = spawnTransforms == null ? 0 : spawnTransforms.Length;
            if (columns <= 0 || columns != len) columns = len;
        }
        if (columns <= 0) columns = 1;
        if (uiFallCurve == null || uiFallCurve.length == 0)
            uiFallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        EnsureColors(columns);
    }

    void Start()
    {
        if (uiFallCurve == null || uiFallCurve.length == 0)
            uiFallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        RefreshSpawnPoints();
        if (forceCenterParent && uiParent != null)
        {
            uiParent.anchorMin = uiParent.anchorMax = new Vector2(0.5f, 0.5f);
            uiParent.pivot = new Vector2(0.5f, 0.5f);
        }
        if (debugGuides) CreateGuides();

        ComputeInterval();

        if (useSimplePool) PrewarmPool();

        if (judgmentPoint == null)
            Debug.LogWarning("[ShowNote] 未设置 judgmentPoint，Miss 判定失效。");
    }

    void Update()
    {
        if (autoLoop)
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _interval)
            {
                _spawnTimer -= _interval;
                if (singleSpawnPerInterval)
                    SpawnOne(SelectIndex());
                else
                {
                    for (int i = 0; i < columns; i++)
                        SpawnOne(i);
                }
            }
        }
        else if (Input.GetKeyDown(triggerKey))
        {
            SpawnOne(SelectIndex());
        }

        if (dynamicAlignX)
        {
            UpdateDynamicAlignX();
        }
    }

    /// <summary>
    /// 刷新生成列 / UI X
    /// </summary>
    public void RefreshSpawnPoints()
    {
        if (useSpawnTransforms)
        {
            int len = spawnTransforms == null ? 0 : spawnTransforms.Length;
            if (len == 0)
            {
                Debug.LogWarning("[ShowNote] useSpawnTransforms=true 但为空，回退单列。");
                useSpawnTransforms = false;
                columns = 1;
            }
            else
            {
                columns = len;
            }
        }
        if (columns <= 0) columns = 1;

        _uiXs = new float[columns];

        if (useSpawnTransforms)
        {
            if (uiUseIndexSpacing)
            {
                float mid = (columns - 1) * 0.5f;
                for (int i = 0; i < columns; i++)
                    _uiXs[i] = (i - mid) * uiColumnSpacing;
            }
            else
            {
                float minX = float.MaxValue;
                float maxX = float.MinValue;
                for (int i = 0; i < columns; i++)
                {
                    var t = spawnTransforms[i];
                    if (t == null) continue;
                    float wx = t.position.x;
                    if (wx < minX) minX = wx;
                    if (wx > maxX) maxX = wx;
                }
                if (minX == float.MaxValue || Mathf.Approximately(minX, maxX))
                {
                    for (int i = 0; i < columns; i++) _uiXs[i] = 0f;
                }
                else
                {
                    for (int i = 0; i < columns; i++)
                    {
                        var t = spawnTransforms[i];
                        float wx = t == null ? (minX + maxX) * 0.5f : t.position.x;
                        float norm = (wx - minX) / (maxX - minX) - 0.5f;
                        _uiXs[i] = norm * (columns - 1) * uiColumnSpacing;
                    }
                }
            }
        }
        else
        {
            float mid = (columns - 1) * 0.5f;
            for (int i = 0; i < columns; i++)
                _uiXs[i] = (i - mid) * uiColumnSpacing;
        }

        EnsureColors(columns);
        _missingTransformWarned.Clear();
    }

    private void EnsureColors(int count)
    {
        if (count <= 0) count = 1;
        if (columnColors == null || columnColors.Length < count)
        {
            var list = new List<Color>(count);
            for (int i = 0; i < count; i++)
            {
                float hue = i / Mathf.Max(1f, (float)count);
                list.Add(Color.HSVToRGB(hue, 0.55f, 0.92f));
            }
            columnColors = list.ToArray();
        }
    }

    private void CreateGuides()
    {
        if (uiParent == null || _uiXs == null) return;
#if UNITY_EDITOR
        for (int i = 0; i < columns; i++)
        {
            var g = new GameObject($"Guide_{i}");
            g.transform.SetParent(uiParent, false);
            var img = g.AddComponent<Image>();
            img.color = guideColor;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(guideLineWidth, Mathf.Abs(uiStartY - uiEndY) + 80f);
            rt.anchoredPosition = new Vector2(_uiXs[i], (uiStartY + uiEndY) * 0.5f);
            img.raycastTarget = false;
        }
#endif
    }

    private void ComputeInterval()
    {
        if (loopInterval > 0f) { _interval = loopInterval; return; }
        float fallDistance = Mathf.Abs(uiStartY - uiEndY);
        if (uiFallDuration > 0f)
        {
            float speed = fallDistance / uiFallDuration;
            float desiredGap = fallDistance / (columns * 2f);
            _interval = desiredGap / Mathf.Max(1f, speed);
        }
        if (_interval <= 0f) _interval = fallbackInterval;
    }

    private int SelectIndex()
    {
        return Random.Range(0, columns);
    }

    #region 对象池
    private void PrewarmPool()
    {
        int count = Mathf.Max(initialPoolSize, columns * 3);
        for (int i = 0; i < count; i++)
        {
            _uiPool.Add(CreateOneUINote(true));
            _worldPool.Add(CreateOneWorldBlock(true));
        }
    }

    private GameObject GetFromPool(List<GameObject> pool, System.Func<GameObject> factory)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].activeSelf)
            {
                pool[i].SetActive(true);
                return pool[i];
            }
        }
        var go = factory();
        pool.Add(go);
        return go;
    }
    #endregion

    #region 生成
    private void SpawnOne(int index)
    {
        if (_uiXs == null || _uiXs.Length == 0)
        {
            RefreshSpawnPoints();
        }

        index = Mathf.Clamp(index, 0, columns - 1);

        RectTransform uiRt = null;
        Image uiImg = null;
        if (uiParent != null)
        {
            GameObject uiGo = useSimplePool ? GetFromPool(_uiPool, () => CreateOneUINote(false)) : CreateOneUINote(false);
            uiRt = uiGo.GetComponent<RectTransform>();
            if (forceCenterNoteRect && uiRt != null)
            {
                uiRt.anchorMin = uiRt.anchorMax = new Vector2(0.5f, 0.5f);
                uiRt.pivot = new Vector2(0.5f, 0.5f);
            }
            float x = (_uiXs != null && index < _uiXs.Length) ? _uiXs[index] : 0f;
            uiRt.anchoredPosition = new Vector2(x, uiStartY);
            uiImg = uiGo.GetComponent<Image>();
            if (uiImg != null && columnColors != null && index < columnColors.Length)
                uiImg.color = columnColors[index];
        }

        Vector3 worldPos;
        if (useSpawnTransforms)
        {
            if (spawnTransforms == null || index >= spawnTransforms.Length || spawnTransforms[index] == null)
            {
                if (!_missingTransformWarned.Contains(index))
                {
                    Debug.LogWarning($"[ShowNote] spawnTransforms[{index}] 缺失，使用基准位置替代。");
                    _missingTransformWarned.Add(index);
                }
                worldPos = new Vector3(worldStartBasePos.x + (index * 0.5f), worldStartBasePos.y, worldStartBasePos.z);
            }
            else
            {
                worldPos = spawnTransforms[index].position;
            }
        }
        else
        {
            worldPos = new Vector3(worldStartBasePos.x + (_uiXs[index] * 0.01f), worldStartBasePos.y, worldStartBasePos.z);
        }

        GameObject worldGo = useSimplePool ? GetFromPool(_worldPool, () => CreateOneWorldBlock(false)) : CreateOneWorldBlock(false);
        worldGo.transform.position = worldPos;
        if (worldScaleFadeIn) worldGo.transform.localScale = Vector3.zero;

        var bb = worldGo.GetComponent<BlackBlock>();
        if (bb == null) bb = worldGo.AddComponent<BlackBlock>();
        bb.judgmentPoint = judgmentPoint;
        bb.collisionThreshold = Mathf.Max(0.01f, bb.collisionThreshold);
        float speed = ComputeSpeed(worldPos.z);
        bb.SetSpeedAndLength(speed, Mathf.Max(0.01f, worldBlockLength), index);
        bb.ResetBlock();

        //生成黑块统计
        ScoreShow.ReportBlackBlockSpawned();

        bool finished = false;
        bb.OnHit = () =>
        {
            if (finished) return;
            finished = true;
            onNoteHit?.Invoke();
        };
        bb.OnMissed = () =>
        {
            if (finished) return;
            finished = true;
            onNoteArrived?.Invoke();
        };

        // 注册到动态 X 对齐列表
        if (dynamicAlignX)
        {
            var entry = new ActiveWorldBlock
            {
                go = worldGo,
                bb = bb,
                column = index,
                originalX = worldPos.x,
                alignTarget = ResolveAlignTarget(index)
            };
            _activeWorldBlocks.Add(entry);
        }

        StartCoroutine(UnifiedDisplayRoutine(uiRt, worldGo, worldPos.z, () => finished, () =>
        {
            // 协程结束移除
            if (dynamicAlignX)
            {
                for (int i = _activeWorldBlocks.Count - 1; i >= 0; i--)
                {
                    if (_activeWorldBlocks[i].go == worldGo)
                        _activeWorldBlocks.RemoveAt(i);
                }
            }
        }));
    }

    private Transform ResolveAlignTarget(int column)
    {
        if (!dynamicAlignX) return null;
        if (useSingleAlignTarget) return singleAlignTarget;
        if (alignTargets == null || alignTargets.Length == 0) return null;
        if (column < 0 || column >= alignTargets.Length) return null;
        return alignTargets[column];
    }

    private float ComputeSpeed(float startZ)
    {
        if (worldMoveSpeed > 0f) return worldMoveSpeed;
        if (judgmentPoint == null || uiFallDuration <= 0f) return 1f;
        float dz = Mathf.Abs(startZ - judgmentPoint.position.z);
        return dz / uiFallDuration;
    }
    #endregion

    #region 动态 X 对齐更新
    private void UpdateDynamicAlignX()
    {
        if (_activeWorldBlocks.Count == 0) return;

        for (int i = _activeWorldBlocks.Count - 1; i >= 0; i--)
        {
            var e = _activeWorldBlocks[i];
            if (e.go == null) { _activeWorldBlocks.RemoveAt(i); continue; }

            Transform at = useSingleAlignTarget ? singleAlignTarget : e.alignTarget;

            if (at == null)
            {
                if (!keepOriginalXIfTargetMissing)
                {
                    // 不保持原位置则跳过（允许外部其他逻辑修改）
                    continue;
                }
                // 保持 originalX
                var p = e.go.transform.position;
                if (Mathf.Abs(p.x - e.originalX) > alignThreshold)
                {
                    p.x = Mathf.Lerp(p.x, e.originalX, 1f - Mathf.Exp(-alignXSmooth * 60f * Time.deltaTime));
                    e.go.transform.position = p;
                }
                continue;
            }

            float targetX = at.position.x;
            var pos = e.go.transform.position;
            float dx = targetX - pos.x;
            if (Mathf.Abs(dx) < alignThreshold) continue;

            float lerpFactor = alignXSmooth <= 0f ? 1f : (1f - Mathf.Exp(-alignXSmooth * 60f * Time.deltaTime));
            pos.x = Mathf.Lerp(pos.x, targetX, lerpFactor);
            e.go.transform.position = pos;
        }
    }
    #endregion

    #region 展示协程
    private IEnumerator UnifiedDisplayRoutine(RectTransform uiRt, GameObject worldGo, float startZ, System.Func<bool> isFinished, System.Action onFinalize)
    {
        float targetZ = judgmentPoint != null ? judgmentPoint.position.z : startZ - Mathf.Max(0.001f, ComputeSpeed(startZ)) * uiFallDuration;
        float t = 0f;

        while (!isFinished())
        {
            if (uiRt != null)
            {
                float uRaw;
                if (syncUIFromWorldProgress && judgmentPoint != null && worldGo != null)
                {
                    float currentZ = worldGo.transform.position.z;
                    uRaw = Mathf.InverseLerp(startZ, targetZ, currentZ);
                }
                else
                {
                    t += Time.deltaTime;
                    uRaw = Mathf.Clamp01(t / Mathf.Max(0.0001f, uiFallDuration));
                }
                float eased = uiFallCurve != null && uiFallCurve.length > 0 ? uiFallCurve.Evaluate(uRaw) : uRaw;
                uiRt.anchoredPosition = new Vector2(uiRt.anchoredPosition.x, Mathf.LerpUnclamped(uiStartY, uiEndY, eased));
            }

            if (worldScaleFadeIn && worldGo != null)
            {
                float scaleU;
                if (uiRt != null)
                {
                    float total = Mathf.Abs(uiStartY - uiEndY);
                    float cur = Mathf.Abs(uiRt.anchoredPosition.y - uiStartY);
                    scaleU = total > 0.001f ? Mathf.Clamp01(cur / total) : 1f;
                }
                else if (judgmentPoint != null)
                {
                    float currentZ = worldGo.transform.position.z;
                    scaleU = Mathf.InverseLerp(startZ, targetZ, currentZ);
                }
                else
                {
                    scaleU = Mathf.Clamp01(t / Mathf.Max(0.0001f, uiFallDuration));
                }
                worldGo.transform.localScale = Vector3.one * scaleU;
            }

            yield return null;
        }

        if (uiRt != null)
            uiRt.anchoredPosition = new Vector2(uiRt.anchoredPosition.x, uiEndY);

        HandleRecycle(worldGo);
        HandleRecycle(uiRt != null ? uiRt.gameObject : null);

        onFinalize?.Invoke();
    }

    private void HandleRecycle(GameObject go)
    {
        if (go == null) return;
        if (destroyOnArrive)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(go);
            else
#endif
            {
                if (useSimplePool) go.SetActive(false);
                else Destroy(go);
            }
        }
        else if (useSimplePool)
        {
            go.SetActive(false);
        }
    }
    #endregion

    #region 工厂
    private GameObject CreateOneUINote(bool prewarm)
    {
        if (uiParent == null) return null;

        GameObject go;
        if (uiNotePrefab != null)
        {
            go = Instantiate(uiNotePrefab, uiParent);
        }
        else
        {
            go = new GameObject("UINote");
            go.transform.SetParent(uiParent, false);
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(80f, 80f);
        }
        if (forceCenterNoteRect && go.TryGetComponent<RectTransform>(out var rtr))
        {
            rtr.anchorMin = rtr.anchorMax = new Vector2(0.5f, 0.5f);
            rtr.pivot = new Vector2(0.5f, 0.5f);
        }
        go.name = prewarm ? "UINote_Pool" : $"UINote_{Time.frameCount}";
        if (prewarm) go.SetActive(false);
        return go;
    }

    private GameObject CreateOneWorldBlock(bool prewarm)
    {
        GameObject go;
        if (worldBlockPrefab != null)
        {
            go = Instantiate(worldBlockPrefab);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "WorldBlackBlock";
            if (go.TryGetComponent<Renderer>(out var r)) r.material.color = Color.black;
        }
        if (go.GetComponent<BlackBlock>() == null) go.AddComponent<BlackBlock>();
        if (prewarm) go.SetActive(false);
        return go;
    }
    #endregion
}
