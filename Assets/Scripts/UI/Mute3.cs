using UnityEngine;

public class Mute3 : MonoBehaviour
{
    [Header("输入（可选）")]
    public KeyCode toggleKey = KeyCode.None; // 例如设置为 KeyCode.Escape

    [Header("音频")]
    public bool pauseAudioListener = true;   // 是否同时暂停全局音频

    private bool _isPaused;
    private float _prevTimeScale = 1f;

    void Awake()
    {
        _prevTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        _isPaused = Time.timeScale <= 0f;

        if (pauseAudioListener)
        {
            AudioListener.pause = _isPaused;
        }
    }

    void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            TogglePause();
        }
    }

    // 供 UI Button 绑定：再次点击可恢复
    public void TogglePause()
    {
        SetPaused(!_isPaused);
    }

    public void Pause()
    {
        SetPaused(true);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    private void SetPaused(bool pause)
    {
        if (_isPaused == pause) return;

        _isPaused = pause;

        if (pause)
        {
            _prevTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0f; // 暂停一切受时间缩放影响的系统（物理、动画（默认）、Update中的deltaTime等）
        }
        else
        {
            Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;
        }

        if (pauseAudioListener)
        {
            AudioListener.pause = _isPaused; // 暂停/恢复音频（忽略 Listener Pause 的音源除外）
        }
    }
}
