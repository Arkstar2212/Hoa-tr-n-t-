using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HoaVaMau.Cutscene
{
    /// <summary>
    /// Trigger placed at the end of the wooden pier dock.
    /// When Bé An steps onto the dock tip:
    /// - Freezes player locomotion.
    /// - Blasts the resonant 1950s steamship horn (SFX_Vintage_ShipHorn_1950s.mp3).
    /// - Fades the screen to black and transitions to the Sunset Sea Departure Cutscene.
    /// </summary>
    public class PierDepartureTrigger : MonoBehaviour
    {
        [Header("Scene Transition")]
        [SerializeField] private string targetSceneName = "Cutscene_BoatSunset";
        [SerializeField] private float waitBeforeFadeSeconds = 1.0f;
        [SerializeField] private float fadeDurationSeconds = 2.0f;

        [Header("Audio")]
        [SerializeField] private AudioClip shipHornClip;
        [SerializeField] private float hornVolume = 1.0f;

        private AudioSource audioSource;
        private bool hasTriggered = false;
        private Canvas fadeCanvas;
        private CanvasGroup fadeCanvasGroup;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.0f; // 2D Full stereo immersion for the cinematic horn
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            // Check if player or character entered
            bool isPlayer = other.CompareTag("Player") ||
                            other.GetComponentInParent<HoaVaMau.Test3D.Player3DController>() != null ||
                            other.name.ToLower().Contains("an") ||
                            other.name.ToLower().Contains("player");

            if (isPlayer)
            {
                StartCoroutine(DepartureSequence(other.gameObject));
            }
        }

        private IEnumerator DepartureSequence(GameObject playerObj)
        {
            hasTriggered = true;
            Debug.Log("[PierDepartureTrigger] Bé An reached the dock edge! Starting Ship Departure Sequence...");

            // 1. Freeze player movement gracefully
            var p3d = playerObj.GetComponentInParent<HoaVaMau.Test3D.Player3DController>();
            if (p3d != null)
            {
                p3d.enabled = false;
            }

            var cc = playerObj.GetComponentInParent<CharacterController>();
            if (cc != null)
            {
                // Zero out velocity
            }

            var anim = playerObj.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetFloat("Speed", 0f);
                anim.SetBool("IsMoving", false);
            }

            // 2. Play the vintage ship horn sound
            if (shipHornClip != null)
            {
                audioSource.clip = shipHornClip;
                audioSource.volume = hornVolume;
                audioSource.Play();
            }

            // 3. Setup temporary fade canvas if not already in scene
            CreateFadeCanvas();

            // Wait a moment for the horn blast to echo
            if (waitBeforeFadeSeconds > 0)
            {
                yield return new WaitForSeconds(waitBeforeFadeSeconds);
            }

            // 4. Smooth Fade to Black
            float elapsed = 0f;
            while (elapsed < fadeDurationSeconds)
            {
                elapsed += Time.deltaTime;
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDurationSeconds);
                }
                yield return null;
            }

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(0.5f);

            // 5. Load Cutscene Scene
            Debug.Log($"[PierDepartureTrigger] Loading cutscene scene '{targetSceneName}'...");
            HoaVaMau.Gameplay.SceneTransitionBotWLoader.LoadSceneWithBotWScreen(
                targetSceneName,
                "ĐANG RA BẾN TẬP KẾT XUỒNG BA LÁ..."
            );
        }

        private void CreateFadeCanvas()
        {
            if (fadeCanvas != null) return;

            var canvasGo = new GameObject("Departure_Fade_Canvas");
            fadeCanvas = canvasGo.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            fadeCanvasGroup = canvasGo.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;

            var blackOverlay = new GameObject("BlackOverlay", typeof(RectTransform), typeof(Image));
            blackOverlay.transform.SetParent(canvasGo.transform, false);
            var rect = blackOverlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = blackOverlay.GetComponent<Image>();
            img.color = Color.black;

            DontDestroyOnLoad(canvasGo);
        }
    }
}
