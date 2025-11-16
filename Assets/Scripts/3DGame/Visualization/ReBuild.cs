using System.Collections.Generic;
using UnityEngine;

public class ReBuild : MonoBehaviour
{
    [Header("监测设置")]
    [SerializeField] private Transform monitorTarget;                 // 为空则使用本脚本所在物体
    [SerializeField] private float yThreshold = -1f;                  // 触发阈值（含临界）
    [SerializeField] private bool pauseOnlyOnce = true;               // 只触发一次

    [Header("位置还原（在暂停前执行）")]
    [SerializeField] private bool restorePositionBeforePause = true;  // 触发暂停时先复位
    [SerializeField] private Transform restorePoint;                  // 优先使用此点
    [SerializeField] private Vector3 manualRestorePosition;           // 次选（未提供 restorePoint）
    [SerializeField] private bool useOriginalStartIfManualIsZero = true; // manual 为 (0,0,0) 时使用初始位置
    [SerializeField] private bool restoreRotation = false;            // 同时还原旋转（无 restorePoint 则用初始旋转）

    [Header("UI 设置")]
    [SerializeField] private GameObject uiPanel;                      // 触发时显示的 UI 面板

    [Header("调试/便捷")]
    [SerializeField] private bool enableResumeHotkey = true;
    [SerializeField] private KeyCode resumeKey = KeyCode.R;

    // 状态
    private bool _isPaused;
    private float _prevTimeScale = 1f;
    private bool _prevAudioPaused;

    // 初始位置/旋转
    private Vector3 _startPosition;
    private Quaternion _startRotation;

    // 缓存（避免每次 FindObjectsOfType 的开销）
    private List<Rigidbody> _sceneRigidbodies;
    private List<ParticleSystem> _sceneParticles;
    private List<Animator> _sceneAnimatorsNonUI;

    // 动画原速度
    private readonly Dictionary<Animator, float> _animatorSpeeds = new();

    private Transform Target => monitorTarget != null ? monitorTarget : transform;

    [System.Obsolete]
    private void Awake()
    {
        _startPosition = Target.position;
        _startRotation = Target.rotation;
        CacheSceneComponents();
    }

    [System.Obsolete]
    private void CacheSceneComponents()
    {
        // 仅首次或场景重大变化时调用（此脚本简单场景下足够）
        _sceneRigidbodies = new List<Rigidbody>(FindObjectsOfType<Rigidbody>());
        _sceneParticles = new List<ParticleSystem>(FindObjectsOfType<ParticleSystem>());

        var animators = FindObjectsOfType<Animator>();
        _sceneAnimatorsNonUI = new List<Animator>(animators.Length);
        for (int i = 0; i < animators.Length; i++)
        {
            var anim = animators[i];
            if (anim == null) continue;
            if (anim.GetComponentInParent<Canvas>() != null) continue; // 排除 UI
            _sceneAnimatorsNonUI.Add(anim);
        }
    }

    private void Update()
    {
        if (!_isPaused || !pauseOnlyOnce)
        {
            if (Target.position.y <= yThreshold && !_isPaused)
            {
                // 先复位后暂停
                if (restorePositionBeforePause)
                {
                    RestoreTargetTransformImmediate();
                }
                PauseAllAndShowUI();
            }
        }

        if (enableResumeHotkey && _isPaused && Input.GetKeyDown(resumeKey))
        {
            ResumeAllAndHideUI();
        }
    }

    private void RestoreTargetTransformImmediate()
    {
        var t = Target;

        // 位置选择逻辑
        Vector3 targetPos;
        if (restorePoint != null)
        {
            targetPos = restorePoint.position;
        }
        else
        {
            bool manualIsZero = manualRestorePosition == Vector3.zero;
            if (manualIsZero && useOriginalStartIfManualIsZero)
                targetPos = _startPosition;
            else
                targetPos = manualRestorePosition;
        }
        t.position = targetPos;

        if (restoreRotation)
        {
            if (restorePoint != null)
                t.rotation = restorePoint.rotation;
            else
                t.rotation = _startRotation;
        }

        // 刚体清零速度避免继续下落
        var rb = t.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void PauseAllAndShowUI()
    {
        if (_isPaused) return;
        _isPaused = true;

        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        _prevAudioPaused = AudioListener.pause;
        AudioListener.pause = true;

        // 刚体休眠
        for (int i = 0; i < _sceneRigidbodies.Count; i++)
        {
            var rb = _sceneRigidbodies[i];
            if (rb == null) continue;
            rb.Sleep();
        }

        // 粒子暂停
        for (int i = 0; i < _sceneParticles.Count; i++)
        {
            var ps = _sceneParticles[i];
            if (ps == null) continue;
            if (ps.isPlaying) ps.Pause(true);
        }

        // Animator 暂停（记录速度）
        _animatorSpeeds.Clear();
        for (int i = 0; i < _sceneAnimatorsNonUI.Count; i++)
        {
            var anim = _sceneAnimatorsNonUI[i];
            if (anim == null) continue;
            _animatorSpeeds[anim] = anim.speed;
            anim.speed = 0f;
        }

        if (uiPanel != null) uiPanel.SetActive(true);
    }

    public void ResumeAllAndHideUI()
    {
        if (!_isPaused) return;
        _isPaused = false;

        // 恢复 Animator
        foreach (var kv in _animatorSpeeds)
        {
            if (kv.Key != null) kv.Key.speed = kv.Value;
        }
        _animatorSpeeds.Clear();

        // 恢复粒子
        for (int i = 0; i < _sceneParticles.Count; i++)
        {
            var ps = _sceneParticles[i];
            if (ps != null && ps.isPaused) ps.Play(true);
        }

        // 唤醒刚体
        for (int i = 0; i < _sceneRigidbodies.Count; i++)
        {
            var rb = _sceneRigidbodies[i];
            if (rb != null) rb.WakeUp();
        }

        Time.timeScale = _prevTimeScale;
        AudioListener.pause = _prevAudioPaused;

        if (uiPanel != null) uiPanel.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        var center = monitorTarget != null ? monitorTarget.position : transform.position;
        Gizmos.DrawLine(new Vector3(center.x - 100f, yThreshold, center.z),
                        new Vector3(center.x + 100f, yThreshold, center.z));
    }
#endif
}
