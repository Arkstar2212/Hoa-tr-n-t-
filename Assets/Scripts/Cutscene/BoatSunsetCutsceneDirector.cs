using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace HoaVaMau.Cutscene
{
    /// <summary>
    /// Director for the Sunset Sea Departure Cutscene ("Cutscene_BoatSunset.unity").
    /// Coordinates:
    /// - Cinematic camera slow drift & zoom.
    /// - Ship sailing towards the open horizon with gentle sea swell bobbing.
    /// - Water wake & shimmering ocean ripples.
    /// - Distant seagulls gliding across the golden sunset sky.
    /// - Horn reverberation & atmospheric ocean soundtrack.
    /// - Smooth black fade out & elegant "Còn tiếp..." presentation.
    /// </summary>
    public class BoatSunsetCutsceneDirector : MonoBehaviour
    {
        [Header("Cutscene Actors")]
        [SerializeField] private Transform shipTransform;
        [SerializeField] private Transform waterWakeTransform;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Renderer waterSurfaceRenderer;
        [SerializeField] private Transform[] seagulls;

        [Header("Ship Sailing Animation")]
        [SerializeField] private Vector3 shipStartPos = new Vector3(-14f, 0.35f, 6f);
        [SerializeField] private Vector3 shipEndPos = new Vector3(20f, 0.35f, 14f);
        [SerializeField] private float sailDuration = 12f;
        [SerializeField] private float bobbingAmplitude = 0.12f;
        [SerializeField] private float bobbingFrequency = 1.6f;
        [SerializeField] private float rollAngle = 2.0f;
        [SerializeField] private float pitchAngle = 1.2f;

        [Header("Camera Animation")]
        [SerializeField] private Vector3 camStartPos = new Vector3(-2f, 3.2f, -16f);
        [SerializeField] private Vector3 camEndPos = new Vector3(3f, 3.6f, -13.5f);

        [Header("Audio")]
        [SerializeField] private AudioSource ambientAudioSource;
        [SerializeField] private AudioSource hornAudioSource;
        [SerializeField] private AudioClip shipHornClip;
        [SerializeField] private float hornDelay = 1.0f;

        [Header("UI & Fade")]
        [SerializeField] private CanvasGroup blackFadeGroup;
        [SerializeField] private CanvasGroup titleTextGroup;
        [SerializeField] private TextMeshProUGUI mainTitleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI promptText;

        [Header("Cutscene Timing")]
        [SerializeField] private float fadeInDuration = 1.8f;
        [SerializeField] private float sailTimeBeforeFadeOut = 7.5f;
        [SerializeField] private float fadeOutDuration = 2.2f;
        [SerializeField] private float titleFadeInDuration = 1.5f;

        private float cutsceneTimer = 0f;
        private bool isWaitingForInput = false;

        private void Start()
        {
            // Destroy any leftover temporary fade canvas from the previous scene
            var oldCanvas = GameObject.Find("Departure_Fade_Canvas");
            if (oldCanvas != null)
            {
                Destroy(oldCanvas);
            }

            if (blackFadeGroup != null)
            {
                blackFadeGroup.alpha = 1f; // Start fully black
            }

            if (titleTextGroup != null)
            {
                titleTextGroup.alpha = 0f;
            }

            if (promptText != null)
            {
                promptText.gameObject.SetActive(false);
            }

            if (shipTransform != null)
            {
                shipTransform.position = shipStartPos;
            }

            if (cameraTransform != null)
            {
                cameraTransform.position = camStartPos;
            }

            StartCoroutine(RunCutsceneSequence());
        }

        private void Update()
        {
            cutsceneTimer += Time.deltaTime;
            float t = Mathf.Clamp01(cutsceneTimer / sailDuration);

            // 1. Move & Bob Ship
            if (shipTransform != null)
            {
                Vector3 basePos = Vector3.Lerp(shipStartPos, shipEndPos, t);
                float waveOffset = Mathf.Sin(cutsceneTimer * bobbingFrequency) * bobbingAmplitude;
                shipTransform.position = new Vector3(basePos.x, basePos.y + waveOffset, basePos.z);

                float pitch = Mathf.Sin(cutsceneTimer * (bobbingFrequency * 0.9f)) * pitchAngle;
                float roll = Mathf.Cos(cutsceneTimer * (bobbingFrequency * 1.1f)) * rollAngle;
                shipTransform.rotation = Quaternion.Euler(pitch, 0f, roll);

                // Water wake follows ship's stern
                if (waterWakeTransform != null)
                {
                    waterWakeTransform.position = new Vector3(shipTransform.position.x - 2.5f, -0.45f, shipTransform.position.z);
                    float wakePulse = 1.0f + Mathf.Sin(cutsceneTimer * 3f) * 0.08f;
                    waterWakeTransform.localScale = new Vector3(wakePulse, 1f, wakePulse);
                }
            }

            // 2. Cinematic Camera Drift
            if (cameraTransform != null)
            {
                cameraTransform.position = Vector3.Lerp(camStartPos, camEndPos, t * 0.8f);
            }

            // 3. Scroll Water Ripples
            if (waterSurfaceRenderer != null && waterSurfaceRenderer.material != null)
            {
                Vector2 currentOffset = waterSurfaceRenderer.material.mainTextureOffset;
                currentOffset.x += 0.035f * Time.deltaTime;
                currentOffset.y += 0.015f * Time.deltaTime;
                waterSurfaceRenderer.material.mainTextureOffset = currentOffset;
            }

            // 4. Animate Seagulls
            if (seagulls != null)
            {
                for (int i = 0; i < seagulls.Length; i++)
                {
                    if (seagulls[i] != null)
                    {
                        seagulls[i].Translate(Vector3.right * (1.8f + i * 0.4f) * Time.deltaTime, Space.World);
                        float wobble = Mathf.Sin((cutsceneTimer + i) * 2.5f) * 0.005f;
                        seagulls[i].Translate(Vector3.up * wobble, Space.World);
                    }
                }
            }

            // 5. Input Check once "Còn tiếp..." has faded in
            if (isWaitingForInput)
            {
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                {
                    ReturnToVillage();
                }
            }
        }

        private IEnumerator RunCutsceneSequence()
        {
            // A. Fade In from Black
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                if (blackFadeGroup != null)
                {
                    blackFadeGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeInDuration);
                }
                yield return null;
            }
            if (blackFadeGroup != null) blackFadeGroup.alpha = 0f;

            // B. Echo Ship Horn across the vast sunset sea
            yield return new WaitForSeconds(hornDelay);
            if (hornAudioSource != null && shipHornClip != null)
            {
                hornAudioSource.clip = shipHornClip;
                hornAudioSource.volume = 0.85f;
                hornAudioSource.Play();
            }

            // C. Enjoy the majestic sailing scene
            float remainingSailTime = sailTimeBeforeFadeOut - (fadeInDuration + hornDelay);
            if (remainingSailTime > 0)
            {
                yield return new WaitForSeconds(remainingSailTime);
            }

            // D. Fade Out to Black
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                if (blackFadeGroup != null)
                {
                    blackFadeGroup.alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
                }
                yield return null;
            }
            if (blackFadeGroup != null) blackFadeGroup.alpha = 1f;

            // E. Fade In "CÒN TIẾP..." Title on Black Background
            yield return new WaitForSeconds(0.4f);
            elapsed = 0f;
            while (elapsed < titleFadeInDuration)
            {
                elapsed += Time.deltaTime;
                if (titleTextGroup != null)
                {
                    titleTextGroup.alpha = Mathf.Clamp01(elapsed / titleFadeInDuration);
                }
                yield return null;
            }
            if (titleTextGroup != null) titleTextGroup.alpha = 1f;

            yield return new WaitForSeconds(1.0f);
            if (promptText != null)
            {
                promptText.gameObject.SetActive(true);
            }
            isWaitingForInput = true;
        }

        public void ReturnToVillage()
        {
            Debug.Log("[BoatSunsetCutsceneDirector] Player pressed key. Returning to CaMau_1954_Village...");
            SceneManager.LoadScene("CaMau_1954_Village");
        }
    }
}
