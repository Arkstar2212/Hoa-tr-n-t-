using UnityEngine;
using HoaVaMau.Test3D;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// 3D Push/Pull Wooden Cargo Crate mechanic ported from Overhaul's InteractableCrate
    /// into full 3D space ("CaMau_1954_Village.unity").
    /// - Shows minimalist [E] glyph when Bé An is within grab range.
    /// - Press [E] to toggle grabbing/pushing/pulling the crate along X/Z terrain,
    ///   or walk firmly against it while holding [E] with authentic wooden scraping SFX.
    /// - Also acts as a climbable/jumpable 3D stepping block ([Space]).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class InteractableCrate3D : MonoBehaviour
    {
        [Header("3D Crate Settings")]
        [SerializeField] private string crateDisplayName = "Thùng Gỗ Bến Sông";
        [SerializeField] private float grabRadius = 1.75f;
        [SerializeField] private float pushPullSpeed = 2.4f;
        [SerializeField] private AudioClip pushLoopClip;

        private Rigidbody rb;
        private AudioSource loopAudioSource;
        private Player3DController activeGrabber;
        private Vector3 grabOffset;

        public bool IsGrabbed => activeGrabber != null;
        public string DisplayName => crateDisplayName;
        public float GrabRadius => grabRadius;

        public void ConfigureAudio(AudioClip scrapeLoop, string displayName = null)
        {
            pushLoopClip = scrapeLoop;
            if (!string.IsNullOrEmpty(displayName)) crateDisplayName = displayName;
            if (loopAudioSource != null) loopAudioSource.clip = pushLoopClip;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 24f;
            rb.linearDamping = 6f;
            rb.angularDamping = 8f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;

            loopAudioSource = GetComponent<AudioSource>();
            if (loopAudioSource == null) loopAudioSource = gameObject.AddComponent<AudioSource>();
            loopAudioSource.clip = pushLoopClip;
            loopAudioSource.loop = true;
            loopAudioSource.playOnAwake = false;
            loopAudioSource.spatialBlend = 0.8f;
            loopAudioSource.volume = 0f;
        }

        public bool CanInteract(Transform playerTr)
        {
            if (playerTr == null) return false;
            Vector2 a = new Vector2(playerTr.position.x, playerTr.position.z);
            Vector2 b = new Vector2(transform.position.x, transform.position.z);
            float dy = Mathf.Abs(playerTr.position.y - transform.position.y);
            return Vector2.Distance(a, b) <= grabRadius && dy <= 1.35f;
        }

        public void ToggleGrab(Player3DController player)
        {
            if (player == null) return;

            if (activeGrabber == player)
            {
                ReleaseGrab();
                return;
            }

            if (!CanInteract(player.transform)) return;

            activeGrabber = player;
            player.SetGrabbedCrate(this);

            Vector3 planarDelta = transform.position - player.transform.position;
            planarDelta.y = 0f;
            if (planarDelta.sqrMagnitude < 0.05f) planarDelta = player.transform.forward;
            grabOffset = planarDelta.normalized * Mathf.Max(0.82f, planarDelta.magnitude);
        }

        public void ReleaseGrab()
        {
            if (activeGrabber != null)
            {
                var prev = activeGrabber;
                activeGrabber = null;
                prev.ClearGrabbedCrate(this);
            }
            if (loopAudioSource != null && loopAudioSource.isPlaying)
            {
                loopAudioSource.Stop();
            }
        }

        private void Update()
        {
            if (activeGrabber == null)
            {
                if (loopAudioSource != null && loopAudioSource.isPlaying)
                {
                    loopAudioSource.volume = Mathf.MoveTowards(loopAudioSource.volume, 0f, Time.deltaTime * 5f);
                    if (loopAudioSource.volume <= 0.01f) loopAudioSource.Stop();
                }
                return;
            }

            // Release automatically if player jumped, crouched, disabled, or moved too far away
            if (!activeGrabber.enabled || !activeGrabber.IsGrounded || activeGrabber.IsCrouching ||
                Vector3.Distance(activeGrabber.transform.position, transform.position) > grabRadius + 1.1f)
            {
                ReleaseGrab();
                return;
            }

            Vector3 moveVel = activeGrabber.PlanarVelocity;
            float speed = moveVel.magnitude;

            if (rb != null && speed > 0.05f)
            {
                Vector3 targetStep = Vector3.ClampMagnitude(moveVel, pushPullSpeed) * Time.deltaTime;
                rb.MovePosition(rb.position + new Vector3(targetStep.x, 0f, targetStep.z));
            }

            // Face player toward crate while pushing/pulling
            Vector3 toCrate = transform.position - activeGrabber.transform.position;
            toCrate.y = 0f;
            if (toCrate.sqrMagnitude > 0.01f)
            {
                activeGrabber.transform.rotation = Quaternion.Slerp(
                    activeGrabber.transform.rotation,
                    Quaternion.LookRotation(toCrate.normalized),
                    Time.deltaTime * 10f
                );
            }

            // Scraping audio loop feedback
            if (loopAudioSource != null && pushLoopClip != null)
            {
                float sfxVol = PlayerPrefs.GetFloat("Settings_SFXVol", 0.70f);
                if (speed > 0.15f)
                {
                    if (!loopAudioSource.isPlaying) loopAudioSource.Play();
                    loopAudioSource.volume = Mathf.MoveTowards(loopAudioSource.volume, 0.55f * sfxVol, Time.deltaTime * 4f);
                }
                else
                {
                    loopAudioSource.volume = Mathf.MoveTowards(loopAudioSource.volume, 0f, Time.deltaTime * 6f);
                }
            }
        }
    }
}
