using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class MosNote : MonoBehaviour
{
    public enum PitchAxis
    {
        X,
        Y
    }

    [Header("Audio")]
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0.1f, 3f)] private float minPitch = 0.8f;
    [SerializeField, Range(0.1f, 3f)] private float maxPitch = 1.2f;
    [SerializeField] private float volume = 1f;

    [Header("Mapping")]
    [SerializeField] private PitchAxis axis = PitchAxis.Y;
    [SerializeField] private bool invert = false;
    [SerializeField] private bool usePlayOneShot = false; // 允许叠加播放

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PlayAtClickPosition(Input.mousePosition);
        }
    }

    private void PlayAtClickPosition(Vector2 screenPos)
    {
        float t;
        if (axis == PitchAxis.Y)
        {
            t = Screen.height > 0 ? screenPos.y / Screen.height : 0f;
        }
        else
        {
            t = Screen.width > 0 ? screenPos.x / Screen.width : 0f;
        }

        t = Mathf.Clamp01(t);
        if (invert)
        {
            t = 1f - t;
        }

        float pitch = Mathf.Lerp(minPitch, maxPitch, t);
        audioSource.pitch = pitch;

        AudioClip toPlay = clip != null ? clip : audioSource.clip;
        if (toPlay == null)
        {
            Debug.LogWarning($"{nameof(MosNote)}: 未设置音频剪辑。请在检视器中分配 AudioClip。", this);
            return;
        }

        if (usePlayOneShot)
        {
            // pitch 同样会作用于 PlayOneShot
            audioSource.PlayOneShot(toPlay, volume);
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            audioSource.clip = toPlay;
            audioSource.volume = volume;
            audioSource.Play();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (minPitch < 0.1f) minPitch = 0.1f;
        if (maxPitch < 0.1f) maxPitch = 0.1f;
        if (minPitch > maxPitch)
        {
            float tmp = minPitch;
            minPitch = maxPitch;
            maxPitch = tmp;
        }

        volume = Mathf.Clamp01(volume);
    }
#endif
}
