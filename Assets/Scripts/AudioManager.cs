using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("默认音效")]
    public AudioClip hitClip;
    [Range(0f, 1f)]
    public float defaultHitVolume = 1f;

    [Header("音源池设置")]
    public int initialSources = 3;
    public int maxSources = 12;
    public bool persistBetweenScenes = false;

    private readonly List<AudioSource> sources = new List<AudioSource>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistBetweenScenes)
            DontDestroyOnLoad(gameObject);

        // 初始化音源池
        for (int i = 0; i < Mathf.Max(1, initialSources); i++)
            sources.Add(CreateAudioSource());
    }

    AudioSource CreateAudioSource()
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        return src;
    }

    AudioSource GetAvailableSource()
    {
        // 查找未在播放的音源
        for (int i = 0; i < sources.Count; i++)
        {
            if (!sources[i].isPlaying)
                return sources[i];
        }

        // 如果池还未达到上限，创建新音源
        if (sources.Count < maxSources)
        {
            var s = CreateAudioSource();
            sources.Add(s);
            return s;
        }

        // 否则返回第一个（回收策略简单）
        return sources[0];
    }

    /// <summary>
    /// 播放任意音频（覆盖默认音量与可选音高）
    /// </summary>
    public void PlayOneShot(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        var src = GetAvailableSource();
        if (src == null) return;
        float prevPitch = src.pitch;
        src.pitch = pitch;
        src.PlayOneShot(clip, Mathf.Clamp01(volume));
        src.pitch = prevPitch;
    }

    /// <summary>
    /// 使用 Inspector 指定的 hitClip 播放“击中”音效
    /// </summary>
    public void PlayHit(float volume = -1f, float pitch = 1f)
    {
        if (hitClip == null) return;
        float v = volume < 0f ? defaultHitVolume : Mathf.Clamp01(volume);
        PlayOneShot(hitClip, v, pitch);
    }
}