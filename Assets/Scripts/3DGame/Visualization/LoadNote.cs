using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class LoadNote : MonoBehaviour
{
    [Header("数据来源/模式")]
    [Tooltip("若为 false 使用文本文件；true 则使用随机循环波浪生成。")]
    public bool proceduralMode = true;
    [Tooltip("放在 Resources 下的文本资源名(不含扩展名)。格式: time,pitch,duration[,channel]")]
    public string textAssetName = "notes";

    [Header("预制体与通道")]
    public GameObject[] channelPrefabs = new GameObject[4];
    public float[] channelDurationScale = new float[4] { 1f, 1f, 1f, 1f };

    [Header("基础坐标轴与偏移")]
    public Vector3 timeAxis = new(1, 0, 0);
    public Vector3 pitchAxis = new(0, 1, 0);
    public Vector3 channelAxis = new(0, 0, 1);
    public float timeOffset = 0f;
    public float pitchOffset = 0f;
    public float channelSpacing = 1.0f;
    [Tooltip("音高到高度的缩放系数：localHeight = pitch * pitchScale + pitchOffset")]
    public float pitchScale = 1f;

    [Header("尺寸与外观")]
    public Vector3 baseSize = new(0.2f, 0.2f, 0.2f);
    public bool colorByPitch = true;
    public bool tintByChannel = true;
    public Color[] channelTint = new Color[4] { Color.white, new(0.9f,0.9f,1f), new(1f,0.9f,0.9f), new(0.9f,1f,0.9f) };

    [Header("滚动(海浪推进)")]
    public bool scrollEnabled = true;
    public bool useAudioTime = false;
    public AudioSource audioSource;
    public float audioTimeScale = 1f;
    public float scrollSpeed = 1f;

    [Header("可视窗口(相对波前时间)")]
    public float visibleRangeBefore = 4f;
    public float visibleRangeAfter = 8f;
    public float fadeInRange = 0.75f;
    public float fadeOutRange = 0.75f;
    public bool destroyPassedNotes = false; // 在 procedural 模式下通常 false，以循环复用

    [Header("淡入淡出方式")]
    public bool fadeByScale = true;
    public bool fadeByAlpha = false;

    [Header("尺寸来源/轴映射")]
    public bool usePrefabCrossSection = true;
    public Axis timeScaleAxis = Axis.X;
    public float minLength = 0.01f;

    [Header("调试")]
    public bool autoFixSeparators = true;
    public bool logSummary = true;

    #region 随机循环波浪参数
    [Header("随机循环波浪 - 基础")]
    public float loopLength = 16f;
    public float notesPerSecondPerChannel = 4f;
    public float timeJitter = 0.05f;
    public Vector2 durationRange = new(0.2f, 0.6f);
    public Vector2Int pitchRange = new(48, 84);
    public bool regenerateOnRecycle = true;
    public int randomSeed = 1234;

    [Header("波形调制 (正弦叠加到音高)")]
    public bool wavePitchSine = true;
    public float sineAmplitude = 6f;
    public float sineFrequency = 0.5f;
    public float sinePhaseOffsetPerChannel = 0.7f;

    [Header("动态前方填充")]
    public float ensureAheadFill = 10f;

    [Header("运行时长度调整")]
    public bool allowRuntimeDurationAdjust = false;
    #endregion

    public enum Axis { X = 0, Y = 1, Z = 2 }

    private struct NoteData
    {
        public float time;
        public int pitch;
        public float duration;
        public int channel;
    }

    private class SpawnedEntry
    {
        public NoteData data;
        public GameObject go;
        public Vector3 baseLocalPos;
        public Renderer renderer;
        public Color baseColor;
        public bool active = true;
        public Vector3 crossSectionBaseScale;
    }

    private readonly List<SpawnedEntry> spawned = new();
    private float waveFrontTime;
    private MaterialPropertyBlock _mpb;
    private System.Random _rnd;

    void Start()
    {
        waveFrontTime = 0f;
        Reload();
    }

    [ContextMenu("Reload Notes")]
    public void Reload()
    {
        ClearVisual();
        waveFrontTime = 0f;
        if (proceduralMode)
        {
            InitRandom();
            var notes = GenerateProceduralLoop();
            if (logSummary) Debug.Log($"[LoadNote] Procedural 预生成 {notes.Count} 条 (loopLength={loopLength})");
            foreach (var n in notes) SpawnNote(n);
        }
        else
        {
            var notes = LoadNotesFromFile();
            if (logSummary) Debug.Log($"[LoadNote] 文本加载 {notes.Count} 条");
            foreach (var n in notes) SpawnNote(n);
        }
    }

    [ContextMenu("Reset Scroll")]
    public void ResetScroll()
    {
        waveFrontTime = 0f;
    }

    private void InitRandom()
    {
        _rnd = randomSeed >= 0 ? new System.Random(randomSeed) : new System.Random();
    }

    private float NextFloat(float min, float max)
    {
        double r = _rnd.NextDouble();
        return (float)(min + (max - min) * r);
    }

    private int NextInt(int minInclusive, int maxInclusive)
    {
        return _rnd.Next(minInclusive, maxInclusive + 1);
    }

    private List<NoteData> GenerateProceduralLoop()
    {
        var list = new List<NoteData>();
        float step = 1f / Mathf.Max(0.01f, notesPerSecondPerChannel); // 基础间隔

        for (int ch = 0; ch < 4; ch++)
        {
            float t = 0f;
            while (t < loopLength)
            {
                float jitter = (timeJitter > 0f) ? NextFloat(-timeJitter, timeJitter) : 0f;
                float noteTime = Mathf.Clamp(t + jitter, 0f, loopLength - 0.0001f);

                int pitch = NextInt(pitchRange.x, pitchRange.y);
                if (wavePitchSine)
                {
                    float phase = (noteTime * sineFrequency * Mathf.PI * 2f) + ch * sinePhaseOffsetPerChannel;
                    pitch += Mathf.RoundToInt(Mathf.Sin(phase) * sineAmplitude);
                }
                pitch = Mathf.Clamp(pitch, pitchRange.x, pitchRange.y);

                float dur = NextFloat(durationRange.x, durationRange.y);
                list.Add(new NoteData
                {
                    time = noteTime,
                    pitch = pitch,
                    duration = dur,
                    channel = ch
                });

                t += step;
            }
        }

        return list;
    }

    private List<NoteData> LoadNotesFromFile()
    {
        var list = new List<NoteData>();
        if (string.IsNullOrWhiteSpace(textAssetName))
        {
            Debug.LogWarning("[LoadNote] textAssetName 未设置");
            return list;
        }
        TextAsset ta = Resources.Load<TextAsset>(textAssetName);
        if (ta == null)
        {
            Debug.LogWarning($"[LoadNote] 未找到 TextAsset: {textAssetName}");
            return list;
        }
        var lines = ta.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int ln = 0;
        foreach (var rawLine in lines)
        {
            ln++;
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            if (autoFixSeparators)
            {
                line = line.Replace('，', ',').Replace('；', ',').Replace(';', ',').Replace('\t', ',');
                while (line.Contains(",,")) line = line.Replace(",,", ",");
            }

            var parts = line.Split(',');
            if (parts.Length < 3)
            {
                Debug.LogWarning($"[LoadNote] 行 {ln} 字段不足 -> {line}");
                continue;
            }
            bool okTime = float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float time);
            bool okPitch = int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pitch);
            bool okDur = float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float dur);
            int channel = 0;
            if (parts.Length >= 4)
                int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out channel);
            if (!okTime || !okPitch || !okDur)
            {
                Debug.LogWarning($"[LoadNote] 行 {ln} 解析失败: {line}");
                continue;
            }
            if (dur < 0) dur = 0;
            channel = Mathf.Clamp(channel, 0, 3);
            list.Add(new NoteData { time = time, pitch = pitch, duration = dur, channel = channel });
        }
        return list;
    }

    private void SpawnNote(NoteData data)
    {
        GameObject prefab = (data.channel >= 0 && data.channel < channelPrefabs.Length) ? channelPrefabs[data.channel] : null;
        GameObject go = prefab != null ? Instantiate(prefab, transform) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Ch{data.channel}_Pitch{data.pitch}_T{data.time:F2}";
        go.transform.SetParent(transform, false);

        Vector3 basePos =
            timeAxis.normalized * (data.time + timeOffset) +
            pitchAxis.normalized * (data.pitch * pitchScale + pitchOffset) +
            channelAxis.normalized * (data.channel * channelSpacing);
        go.transform.localPosition = basePos;

        Vector3 crossBase = (usePrefabCrossSection && prefab != null) ? go.transform.localScale : baseSize;

        Renderer r = go.GetComponent<Renderer>();
        Color finalColor = Color.white;
        if (r != null)
        {
            if (colorByPitch)
            {
                float hue = Mathf.Repeat(data.pitch / 128f, 1f);
                finalColor = Color.HSVToRGB(hue, 0.6f, 0.9f);
            }
            if (tintByChannel && channelTint != null && data.channel < channelTint.Length)
                finalColor *= channelTint[data.channel];
            finalColor.a = 1f;
            r.material.color = finalColor;
        }

        var entry = new SpawnedEntry
        {
            data = data,
            go = go,
            baseLocalPos = basePos,
            renderer = r,
            baseColor = finalColor,
            active = true,
            crossSectionBaseScale = crossBase
        };

        float lengthScale = (data.channel < channelDurationScale.Length) ? channelDurationScale[data.channel] : 1f;
        float length = data.duration * lengthScale;
        ApplyLengthAndCrossSection(entry, length, 1f);

        spawned.Add(entry);
    }

    void Update()
    {
        if (scrollEnabled)
        {
            if (useAudioTime && audioSource != null)
                waveFrontTime = audioSource.time * audioTimeScale;
            else
                waveFrontTime += scrollSpeed * Time.deltaTime;
        }

        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        Vector3 timeN = timeAxis == Vector3.zero ? Vector3.right : timeAxis.normalized;

        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            var e = spawned[i];
            if (e.go == null)
            {
                spawned.RemoveAt(i);
                continue;
            }

            // 循环复用逻辑（仅 procedural 且不销毁）
            if (proceduralMode && !destroyPassedNotes)
            {
                float relFront = (e.data.time + timeOffset) - waveFrontTime;
                if (relFront < -visibleRangeBefore - e.data.duration)
                {
                    e.data.time += loopLength;
                    if (regenerateOnRecycle)
                    {
                        int pitch = NextInt(pitchRange.x, pitchRange.y);
                        if (wavePitchSine)
                        {
                            float phase = (e.data.time * sineFrequency * Mathf.PI * 2f) + e.data.channel * sinePhaseOffsetPerChannel;
                            pitch += Mathf.RoundToInt(Mathf.Sin(phase) * sineAmplitude);
                        }
                        pitch = Mathf.Clamp(pitch, pitchRange.x, pitchRange.y);
                        float dur = NextFloat(durationRange.x, durationRange.y);
                        e.data.pitch = pitch;
                        e.data.duration = dur;

                        if (e.renderer != null)
                        {
                            Color c = colorByPitch ? Color.HSVToRGB(Mathf.Repeat(pitch / 128f, 1f), 0.6f, 0.9f) : Color.white;
                            if (tintByChannel && channelTint != null && e.data.channel < channelTint.Length)
                                c *= channelTint[e.data.channel];
                            c.a = 1f;
                            e.baseColor = c;
                            e.renderer.material.color = c;
                        }
                    }
                    e.baseLocalPos =
                        timeAxis.normalized * (e.data.time + timeOffset) +
                        pitchAxis.normalized * (e.data.pitch * pitchScale + pitchOffset) +
                        channelAxis.normalized * (e.data.channel * channelSpacing);
                }
            }

            // 滚动位置
            e.go.transform.localPosition = e.baseLocalPos - timeN * waveFrontTime;

            float rel = (e.data.time + timeOffset) - waveFrontTime;
            bool tooBehind = rel < -visibleRangeBefore - e.data.duration;
            bool tooAhead = rel > visibleRangeAfter;

            if (tooBehind || tooAhead)
            {
                if (destroyPassedNotes && tooBehind)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(e.go);
                    else
#endif
                        Destroy(e.go);
                    spawned.RemoveAt(i);
                    continue;
                }
                if (e.active)
                {
                    e.go.SetActive(false);
                    e.active = false;
                }
                continue;
            }
            else if (!e.active)
            {
                e.go.SetActive(true);
                e.active = true;
            }

            float lengthScaleNow = (e.data.channel < channelDurationScale.Length) ? channelDurationScale[e.data.channel] : 1f;
            float lengthNow = e.data.duration * lengthScaleNow;

            float fade = 1f;
            if (fadeInRange > 0f)
            {
                float fAhead = Mathf.InverseLerp(visibleRangeAfter, visibleRangeAfter - fadeInRange, rel);
                fade = Mathf.Min(fade, fAhead);
            }
            if (fadeOutRange > 0f)
            {
                float fBehind = Mathf.InverseLerp(-visibleRangeBefore, -visibleRangeBefore + fadeOutRange, rel);
                fade = Mathf.Min(fade, fBehind);
            }
            fade = Mathf.Clamp01(fade);

            ApplyLengthAndCrossSection(e, lengthNow, fade);

            if (fadeByAlpha && e.renderer != null)
            {
                _mpb.Clear();
                Color c = e.baseColor;
                c.a = fade;
                _mpb.SetColor("_Color", c);
                e.renderer.SetPropertyBlock(_mpb);
            }
        }

        // 动态保证前方填充 (仅 procedural)
        if (proceduralMode && ensureAheadFill > 0f && !destroyPassedNotes)
        {
            float maxFuture = float.MinValue;
            foreach (var e in spawned)
                maxFuture = Mathf.Max(maxFuture, e.data.time);
            if (maxFuture - waveFrontTime < ensureAheadFill)
            {
                float startT = maxFuture + 0.0001f;
                float targetT = waveFrontTime + ensureAheadFill;
                while (startT < targetT)
                {
                    for (int ch = 0; ch < 4; ch++)
                    {
                        float jitter = (timeJitter > 0f) ? NextFloat(-timeJitter, timeJitter) : 0f;
                        float noteTime = startT + jitter;
                        int pitch = NextInt(pitchRange.x, pitchRange.y);
                        if (wavePitchSine)
                        {
                            float phase = (noteTime * sineFrequency * Mathf.PI * 2f) + ch * sinePhaseOffsetPerChannel;
                            pitch += Mathf.RoundToInt(Mathf.Sin(phase) * sineAmplitude);
                        }
                        pitch = Mathf.Clamp(pitch, pitchRange.x, pitchRange.y);
                        float dur = NextFloat(durationRange.x, durationRange.y);
                        SpawnNote(new NoteData
                        {
                            time = noteTime,
                            pitch = pitch,
                            duration = dur,
                            channel = ch
                        });
                    }
                    startT += 1f / Mathf.Max(0.01f, notesPerSecondPerChannel);
                }
            }
        }
    }

    private static int AxisIndex(Axis a) => a == Axis.X ? 0 : (a == Axis.Y ? 1 : 2);

    private void ApplyLengthAndCrossSection(SpawnedEntry e, float length, float fade)
    {
        int idx = AxisIndex(timeScaleAxis);
        Vector3 s = e.crossSectionBaseScale;

        float L = Mathf.Max(length, minLength);
        if (idx == 0) s.x = L;
        else if (idx == 1) s.y = L;
        else s.z = L;

        if (fadeByScale)
        {
            float f = Mathf.Clamp01(fade);
            if (idx != 0) s.x = e.crossSectionBaseScale.x * (idx == 0 ? 1f : f);
            if (idx != 1) s.y = e.crossSectionBaseScale.y * (idx == 1 ? 1f : f);
            if (idx != 2) s.z = e.crossSectionBaseScale.z * (idx == 2 ? 1f : f);
        }
        else
        {
            if (idx != 0) s.x = e.crossSectionBaseScale.x;
            if (idx != 1) s.y = e.crossSectionBaseScale.y;
            if (idx != 2) s.z = e.crossSectionBaseScale.z;
        }

        e.go.transform.localScale = s;
    }

    public void ClearVisual()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i].go != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(spawned[i].go);
                else
#endif
                    Destroy(spawned[i].go);
            }
        }
        spawned.Clear();
    }
}
