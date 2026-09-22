using System.Collections;
using UnityEngine;

public class IntroAudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("Kéo AudioSource chứa file tiếng gió vào đây")]
    public AudioSource windAudioSource;

    [Tooltip("Kéo AudioSource chứa file Piano Lofi (Haru Haru) vào đây")]
    public AudioSource lofiAudioSource;

    [Header("Âm lượng tối đa (0–1)")]
    [Range(0f, 1f)]
    [Tooltip("Kéo thanh này để chỉnh volume tiếng gió bất kỳ lúc nào")]
    public float windMaxVolume = 1.0f;

    [Range(0f, 1f)]
    [Tooltip("Kéo thanh này để chỉnh volume tối đa của nhạc Lofi (fade-in sẽ lên tới mức này)")]
    public float lofiMaxVolume = 1.0f;

    [Header("Thời gian (Giây)")]
    public float windDelayBeforeLofi = 3.0f;
    public float lofiFadeInDuration = 2.5f;

    // Tiến độ fade-in Lofi: 0 = im, 1 = đã đạt lofiMaxVolume
    float lofiFadeProgress;

    // True khi đã bắt đầu phát Lofi (kể cả đang fade)
    bool lofiStarted;

    void Start()
    {
        StartCoroutine(PlayIntroSequence());
    }

    // Áp volume Inspector lên AudioSource mỗi frame (gió luôn; Lofi sau khi đã Play)
    void Update()
    {
        ApplyVolumes();
    }

    IEnumerator PlayIntroSequence()
    {
        // Phát gió ngay; volume do Update/ApplyVolumes lấy từ windMaxVolume
        if (windAudioSource != null)
        {
            windAudioSource.Play();
        }

        yield return new WaitForSeconds(windDelayBeforeLofi);

        // Bắt đầu Lofi từ 0, fade tới lofiMaxVolume hiện tại
        if (lofiAudioSource != null)
        {
            lofiFadeProgress = 0f;
            lofiStarted = true;
            lofiAudioSource.Play();

            float timer = 0f;
            float duration = Mathf.Max(0.0001f, lofiFadeInDuration);

            while (timer < duration)
            {
                timer += Time.deltaTime;
                lofiFadeProgress = Mathf.Clamp01(timer / duration);
                yield return null;
            }

            lofiFadeProgress = 1f;
        }
    }

    // Gió = windMaxVolume. Lofi = tiến độ fade * lofiMaxVolume (đổi slider là đổi ngay)
    void ApplyVolumes()
    {
        if (windAudioSource != null)
        {
            windAudioSource.volume = windMaxVolume;
        }

        if (lofiStarted && lofiAudioSource != null)
        {
            lofiAudioSource.volume = lofiFadeProgress * lofiMaxVolume;
        }
    }
}
