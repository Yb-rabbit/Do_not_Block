using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScoreShow : MonoBehaviour
{
    [Header("UI 显示")]
    [SerializeField] private Text fractionText; // 普通 Text
    [SerializeField] private string format = "{0}/{1}"; // {命中}/{生成}

    [Header("命中检测(鼠标点击命中黑块)")]
    [SerializeField] private bool detectMouseHits = true;
    [SerializeField] private bool physics2D = true;
    [SerializeField] private string blockTag = "BlackBlock";
    [SerializeField] private LayerMask blockLayerMask = ~0;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float rayMaxDistance = 200f;

    [Header("音量为 0 自动暂停")]
    [SerializeField] private bool autoPauseWhenSceneMuted = true;
    [SerializeField, Range(0f, 1f)] private float volumePauseThreshold = 0.0001f;
    [SerializeField] private bool pauseAudioListener = true;

    [Header("背景音乐结束自动暂停")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private bool pauseOnBgmEnd = true;
    [SerializeField] private bool useDspAccurateEnd = true;

    [Header("BGM 结束后激活对象")]
    [SerializeField] private GameObject activateOnBgmEnd;
    [SerializeField] private float activateDelay = 0f;

    // 计数器
    private static int s_totalBlocks;
    private static int s_mouseHits;

    // 暂停状态
    private bool _pausedByVolume;
    private bool _pausedByBgmEnd;
    private float _prevTimeScale = 1f;

    // BGM 结束检测
    private double _bgmExpectedEndDsp = -1d;

    // 激活标记
    private bool _endObjectActivated;

    private Camera Cam => targetCamera != null ? targetCamera : Camera.main;

    public static int TotalBlocks => s_totalBlocks;
    public static int MouseHits => s_mouseHits;

    public static void ReportBlackBlockSpawned(int count = 1)
    {
        if (count <= 0) return;
        s_totalBlocks += count;
    }

    public static void ReportMouseHit(int count = 1)
    {
        if (count <= 0) return;
        s_mouseHits += count;
    }

    public static void ResetCounters()
    {
        s_totalBlocks = 0;
        s_mouseHits = 0;
    }

    private void Start()
    {
        ArmBgmEndDetection();
    }

    private void Update()
    {
        HandleAutoPauseByVolume();
        HandleBgmEndDetection();
        HandleMouseHitDetection();
        RefreshUIText();
    }

    private void HandleAutoPauseByVolume()
    {
        if (!autoPauseWhenSceneMuted)
        {
            return;
        }

        float vol = AudioListener.volume;
        bool shouldPause = vol <= volumePauseThreshold;

        if (shouldPause && !_pausedByVolume)
        {
            PauseGame(ref _pausedByVolume);
        }
        else if (!shouldPause && _pausedByVolume)
        {
            ResumeGameFromVolume();
        }
    }

    private void HandleBgmEndDetection()
    {
        if (!pauseOnBgmEnd || _pausedByBgmEnd) return;
        if (bgmSource == null || bgmSource.clip == null) return;
        if (bgmSource.loop) return;

        if (useDspAccurateEnd && _bgmExpectedEndDsp > 0d)
        {
            if (AudioSettings.dspTime >= _bgmExpectedEndDsp)
            {
                PauseGame(ref _pausedByBgmEnd);
                TryActivateEndObject();
                return;
            }
        }
        else
        {
            if (!bgmSource.isPlaying && bgmSource.time > 0f)
            {
                PauseGame(ref _pausedByBgmEnd);
                TryActivateEndObject();
            }
        }
    }

    private void TryActivateEndObject()
    {
        if (_endObjectActivated) return;
        if (activateOnBgmEnd == null) return;
        _endObjectActivated = true;

        if (activateDelay <= 0f)
        {
            activateOnBgmEnd.SetActive(true);
        }
        else
        {
            StartCoroutine(ActivateLaterRoutine());
        }
    }

    private IEnumerator ActivateLaterRoutine()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, activateDelay));
        if (activateOnBgmEnd != null)
        {
            activateOnBgmEnd.SetActive(true);
        }
    }

    public void ForceActivateEndObject()
    {
        if (activateOnBgmEnd == null) return;
        if (!_endObjectActivated)
        {
            _endObjectActivated = true;
            if (activateDelay <= 0f) activateOnBgmEnd.SetActive(true);
            else StartCoroutine(ActivateLaterRoutine());
        }
    }

    private void HandleMouseHitDetection()
    {
        if (!detectMouseHits) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (IsGloballyPaused()) return;

            var cam = Cam;
            if (cam == null) return;

            if (physics2D)
            {
                Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2 p = new Vector2(wp.x, wp.y);
                var hits = Physics2D.OverlapPointAll(p);
                for (int i = 0; i < hits.Length; i++)
                {
                    var col = hits[i];
                    if (!IsOnLayer(col.gameObject.layer)) continue;
                    if (!string.IsNullOrEmpty(blockTag) && !col.CompareTag(blockTag)) continue;
                    s_mouseHits++;
                    break;
                }
            }
            else
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, blockLayerMask))
                {
                    if (hit.collider != null)
                    {
                        var go = hit.collider.gameObject;
                        if (string.IsNullOrEmpty(blockTag) || go.CompareTag(blockTag))
                        {
                            s_mouseHits++;
                        }
                    }
                }
            }
        }
    }

    private bool IsOnLayer(int layer)
    {
        return (blockLayerMask.value & (1 << layer)) != 0;
    }

    private void RefreshUIText()
    {
        if (fractionText == null) return;
        fractionText.text = string.Format(format, s_mouseHits, s_totalBlocks);
    }

    private bool IsGloballyPaused()
    {
        return _pausedByVolume || _pausedByBgmEnd;
    }

    private void PauseGame(ref bool flag)
    {
        if (IsGloballyPaused())
        {
            flag = true;
            return;
        }

        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (pauseAudioListener)
        {
            AudioListener.pause = true;
        }
        flag = true;
    }

    public void ResumeGame()
    {
        if (!IsGloballyPaused()) return;

        _pausedByVolume = false;
        _pausedByBgmEnd = false;
        Time.timeScale = _prevTimeScale;
        if (pauseAudioListener)
        {
            AudioListener.pause = false;
        }
    }

    private void ResumeGameFromVolume()
    {
        _pausedByVolume = false;
        if (_pausedByBgmEnd) return;
        Time.timeScale = _prevTimeScale;
        if (pauseAudioListener) AudioListener.pause = false;
    }

    public void ArmBgmEndDetection()
    {
        if (bgmSource != null && bgmSource.clip != null && !bgmSource.loop)
        {
            _bgmExpectedEndDsp = AudioSettings.dspTime +
                                 (bgmSource.clip.length / Mathf.Max(0.01f, bgmSource.pitch));
            _pausedByBgmEnd = false;
            _endObjectActivated = false;
        }
        else
        {
            _bgmExpectedEndDsp = -1d;
            _endObjectActivated = false;
        }
    }
}
