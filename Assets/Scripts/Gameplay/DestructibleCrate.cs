using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Thùng gỗ tương tác có khả năng chịu lực va đập khi bị ném bóng trúng:
    /// - Chịu 3 lần ném (hoặc 2-3 lần tùy chỉnh).
    /// - Lần 1: Rung giật vật lý, rơi dăm gỗ, xuất hiện rạn nứt cấp 1 trên bề mặt.
    /// - Lần 2: Rung lắc mạnh hơn, rạn nứt cấp 2 toác rộng, rơi nhiều mảnh vụn.
    /// - Lần 3 (Vỡ nát PhysX Style): Thùng nổ vỡ tung thành 8 mảnh ván gỗ (wood planks/shards).
    ///   Mỗi mảnh là Rigidbody độc lập, chịu lực AddExplosionForce và AddTorque
    ///   văng tung tóe tứ phía rồi nảy và dừng lại tự nhiên trên sàn theo chuẩn PhysX.
    /// </summary>
    public class DestructibleCrate : MonoBehaviour
    {
        [Header("Độ bền & Số lần ném trúng")]
        [SerializeField] private int maxHits = 3;
        [SerializeField] private float minImpactSpeed = 3.0f;

        [Header("Hiệu ứng sập vỡ vụn PhysX (Fracture & Crumble)")]
        [SerializeField] private float collapseOutwardForce = 1.2f;
        [SerializeField] private float upwardPopForce = 0.8f;
        [SerializeField] private int plankPieceCount = 14;
        [SerializeField] private int chunkPieceCount = 14;

        [Header("Vật liệu & Màu sắc rạn nứt")]
        [SerializeField] private Material woodMaterial;
        [SerializeField] private Color crackTintLevel1 = new Color(0.92f, 0.78f, 0.65f);
        [SerializeField] private Color crackTintLevel2 = new Color(0.70f, 0.50f, 0.35f);

        private int currentHits = 0;
        private InteractableCrate interactableCrate;
        private Transform visualRoot;
        private Vector3 initialVisualPos;
        private bool isDestroyed = false;
        private List<GameObject> crackDecals = new List<GameObject>();

        public int CurrentHits => currentHits;
        public int MaxHits => maxHits;
        public bool IsDestroyed => isDestroyed;

        private void Awake()
        {
            interactableCrate = GetComponent<InteractableCrate>();
            visualRoot = transform.Find("Visual_Body") ?? transform;
            initialVisualPos = visualRoot.localPosition;

            if (woodMaterial == null)
            {
                var rend = visualRoot.GetComponentInChildren<MeshRenderer>();
                if (rend != null) woodMaterial = rend.sharedMaterial;
            }

            CreateCrackOverlayVisuals();
        }

        private void CreateCrackOverlayVisuals()
        {
            // Tạo 4 đường nứt trực quan trên mặt thùng (crack lines)
            Vector3 size = (interactableCrate != null) ? interactableCrate.BoxSize : new Vector3(1.4f, 1.4f, 1.0f);
            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material crackMat = new Material(unlitShader) { color = new Color(0.12f, 0.08f, 0.04f, 0.95f) };

            // Vết nứt 1: Nứt chéo mặt trước (Level 1)
            GameObject crack1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crack1.name = "CrackOverlay_Level1";
            crack1.transform.SetParent(transform);
            crack1.transform.localPosition = new Vector3(0f, halfY, -0.52f);
            crack1.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
            crack1.transform.localScale = new Vector3(0.04f, size.y * 0.75f, 0.02f);
            crack1.GetComponent<MeshRenderer>().sharedMaterial = crackMat;
            Destroy(crack1.GetComponent<Collider>());
            crack1.SetActive(false);
            crackDecals.Add(crack1);

            // Vết nứt 2: Nứt nhánh rẽ (Level 1)
            GameObject crack2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crack2.name = "CrackOverlay_Level1_Branch";
            crack2.transform.SetParent(transform);
            crack2.transform.localPosition = new Vector3(0.18f, halfY + 0.12f, -0.52f);
            crack2.transform.localRotation = Quaternion.Euler(0f, 0f, -48f);
            crack2.transform.localScale = new Vector3(0.035f, size.y * 0.45f, 0.02f);
            crack2.GetComponent<MeshRenderer>().sharedMaterial = crackMat;
            Destroy(crack2.GetComponent<Collider>());
            crack2.SetActive(false);
            crackDecals.Add(crack2);

            // Vết nứt 3: Nứt dọc sâu toác rộng (Level 2)
            GameObject crack3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crack3.name = "CrackOverlay_Level2";
            crack3.transform.SetParent(transform);
            crack3.transform.localPosition = new Vector3(-0.25f, halfY - 0.05f, -0.52f);
            crack3.transform.localRotation = Quaternion.Euler(0f, 0f, 82f);
            crack3.transform.localScale = new Vector3(0.055f, size.y * 0.85f, 0.02f);
            crack3.GetComponent<MeshRenderer>().sharedMaterial = crackMat;
            Destroy(crack3.GetComponent<Collider>());
            crack3.SetActive(false);
            crackDecals.Add(crack3);

            // Vết nứt 4: Nứt góc trên sứt mẻ (Level 2)
            GameObject crack4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crack4.name = "CrackOverlay_Level2_Corner";
            crack4.transform.SetParent(transform);
            crack4.transform.localPosition = new Vector3(0.35f, halfY + 0.32f, -0.52f);
            crack4.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            crack4.transform.localScale = new Vector3(0.045f, size.y * 0.5f, 0.02f);
            crack4.GetComponent<MeshRenderer>().sharedMaterial = crackMat;
            Destroy(crack4.GetComponent<Collider>());
            crack4.SetActive(false);
            crackDecals.Add(crack4);
        }

        /// <summary>
        /// Nhận sát thương khi bị bóng hoặc vật thể ném trúng.
        /// </summary>
        public void TakeDamage(int damage, Vector3 impactPoint, Vector3 impactVelocity)
        {
            if (isDestroyed) return;

            currentHits += damage;

            // Rung lắc giật nhẹ thùng
            StartCoroutine(ShakeEffect(0.25f, 0.07f * currentHits));

            // Bắn ra các mảnh dăm gỗ nhỏ tại điểm va chạm
            SpawnWoodSplinters(impactPoint, impactVelocity);

            if (currentHits == 1)
            {
                // Cấp độ 1: Xuất hiện vết rạn nứt đầu tiên
                if (crackDecals.Count >= 2)
                {
                    crackDecals[0].SetActive(true);
                    crackDecals[1].SetActive(true);
                }
                PlayWoodHitSound(1.0f);
            }
            else if (currentHits == 2)
            {
                // Cấp độ 2: Rạn nứt chằng chịt, toác rộng
                foreach (var decal in crackDecals)
                {
                    if (decal != null) decal.SetActive(true);
                }
                PlayWoodHitSound(0.85f);
            }
            else if (currentHits >= maxHits)
            {
                // Cấp độ 3: VỠ VỤN THÀNH MẢNH (PhysX Fracture Pieces)
                FractureAndExplode(impactPoint, impactVelocity);
            }
        }

        private IEnumerator ShakeEffect(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (visualRoot == null) yield break;
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-0.5f, 0.5f) * magnitude;
                visualRoot.localPosition = initialVisualPos + new Vector3(x, y, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (visualRoot != null) visualRoot.localPosition = initialVisualPos;
        }

        private void SpawnWoodSplinters(Vector3 hitPoint, Vector3 velocity)
        {
            int splinterCount = Random.Range(5, 9);
            Vector3 normal = -velocity.normalized;

            for (int i = 0; i < splinterCount; i++)
            {
                GameObject splinter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                splinter.name = "WoodSplinter";
                splinter.transform.position = hitPoint + Random.insideUnitSphere * 0.12f;
                splinter.transform.localScale = new Vector3(
                    Random.Range(0.04f, 0.12f),
                    Random.Range(0.03f, 0.08f),
                    Random.Range(0.04f, 0.10f));

                if (woodMaterial != null)
                {
                    splinter.GetComponent<MeshRenderer>().sharedMaterial = woodMaterial;
                }

                var rb = splinter.AddComponent<Rigidbody>();
                rb.mass = 0.35f;
                Vector3 sprayDir = (normal + Random.insideUnitSphere * 0.75f).normalized;
                rb.linearVelocity = sprayDir * Random.Range(3.5f, 7.5f) + Vector3.up * 2.0f;
                rb.angularVelocity = Random.insideUnitSphere * 25f;

                Destroy(splinter, 3.5f);
            }
        }

        /// <summary>
        /// Phân rã thùng gỗ thành nhiều mảnh ván vụn (28+ mảnh) và sập đổ tự nhiên tại chỗ theo trọng lực PhysX,
        /// không bị lực nổ văng xa khắp màn hình mà tích tụ thành đống tàn dư gỗ tự nhiên.
        /// </summary>
        public void FractureAndExplode(Vector3 impactPoint, Vector3 impactVelocity)
        {
            if (isDestroyed) return;
            isDestroyed = true;

            // Nếu người chơi đang bám kéo thùng: Buông ra ngay
            if (interactableCrate != null && interactableCrate.IsGrabbed)
            {
                interactableCrate.ReleaseGrab();
            }

            Vector3 crateCenter = transform.position + new Vector3(0f, 0.70f, 0f);
            Vector3 size = (interactableCrate != null) ? interactableCrate.BoxSize : new Vector3(1.4f, 1.4f, 1.0f);

            GameObject debrisContainer = new GameObject($"FracturedCrate_Debris_{gameObject.name}");
            debrisContainer.transform.position = transform.position;

            // Physics Material tạo độ ma sát cao để mảnh gỗ nằm yên tại chỗ sau khi sập
            PhysicsMaterial woodPhysMat = new PhysicsMaterial("WoodDebrisPhysMat")
            {
                dynamicFriction = 0.75f,
                staticFriction = 0.85f,
                bounciness = 0.12f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            int totalPlanks = Mathf.Max(12, plankPieceCount);
            int totalChunks = Mathf.Max(12, chunkPieceCount);

            // 1. Sinh các mảnh ván gỗ dẹt (Wood Planks)
            for (int i = 0; i < totalPlanks; i++)
            {
                Vector3 localOffset = new Vector3(
                    Random.Range(-size.x * 0.42f, size.x * 0.42f),
                    Random.Range(0.05f, size.y * 0.90f) - 0.70f,
                    Random.Range(-size.z * 0.42f, size.z * 0.42f)
                );
                Vector3 piecePos = crateCenter + localOffset;

                GameObject pieceGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pieceGO.name = $"WoodPlank_{i}";
                pieceGO.transform.SetParent(debrisContainer.transform);
                pieceGO.transform.position = piecePos;

                // Kích thước dạng thanh ván gỗ vỡ
                pieceGO.transform.localScale = new Vector3(
                    Random.Range(size.x * 0.28f, size.x * 0.48f),
                    Random.Range(0.06f, 0.14f),
                    Random.Range(size.z * 0.22f, size.z * 0.40f)
                );
                pieceGO.transform.rotation = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(0f, 360f), Random.Range(-25f, 25f));

                if (woodMaterial != null) pieceGO.GetComponent<MeshRenderer>().sharedMaterial = woodMaterial;

                var col = pieceGO.GetComponent<BoxCollider>();
                if (col != null) col.sharedMaterial = woodPhysMat;

                var rb = pieceGO.AddComponent<Rigidbody>();
                rb.mass = Random.Range(1.5f, 3.2f);
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.linearDamping = 0.85f;
                rb.angularDamping = 1.5f;

                // Lực sập vụn nhẹ tại chỗ (không bắn tung tóe)
                Vector3 crumbleDir = (piecePos - crateCenter).normalized;
                crumbleDir.y = 0f;
                Vector3 crumbleVel = crumbleDir * Random.Range(0.2f, collapseOutwardForce) + Vector3.up * Random.Range(0.2f, upwardPopForce);
                rb.linearVelocity = crumbleVel;
                rb.angularVelocity = Random.insideUnitSphere * Random.Range(4f, 10f);
            }

            // 2. Sinh các khối dăm gỗ, góc nẹp vụn (Wood Chunks / Corner Posts)
            for (int j = 0; j < totalChunks; j++)
            {
                Vector3 localOffset = new Vector3(
                    Random.Range(-size.x * 0.45f, size.x * 0.45f),
                    Random.Range(0.05f, size.y * 0.90f) - 0.70f,
                    Random.Range(-size.z * 0.45f, size.z * 0.45f)
                );
                Vector3 piecePos = crateCenter + localOffset;

                GameObject chunkGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunkGO.name = $"WoodChunk_{j}";
                chunkGO.transform.SetParent(debrisContainer.transform);
                chunkGO.transform.position = piecePos;

                // Kích thước dạng mẩu gỗ vỡ nhỏ vụn
                chunkGO.transform.localScale = new Vector3(
                    Random.Range(0.10f, 0.24f),
                    Random.Range(0.12f, 0.32f),
                    Random.Range(0.10f, 0.24f)
                );
                chunkGO.transform.rotation = Random.rotation;

                if (woodMaterial != null) chunkGO.GetComponent<MeshRenderer>().sharedMaterial = woodMaterial;

                var col = chunkGO.GetComponent<BoxCollider>();
                if (col != null) col.sharedMaterial = woodPhysMat;

                var rb = chunkGO.AddComponent<Rigidbody>();
                rb.mass = Random.Range(0.8f, 2.0f);
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.linearDamping = 0.9f;
                rb.angularDamping = 1.6f;

                Vector3 crumbleDir = (piecePos - crateCenter).normalized;
                crumbleDir.y = 0f;
                Vector3 crumbleVel = crumbleDir * Random.Range(0.1f, collapseOutwardForce * 0.9f) + Vector3.up * Random.Range(0.1f, upwardPopForce * 0.8f);
                rb.linearVelocity = crumbleVel;
                rb.angularVelocity = Random.insideUnitSphere * Random.Range(5f, 12f);
            }

            // 3. Bụi mù và dăm dăm vụn li ti
            SpawnWoodSplinters(crateCenter, Vector3.up * 1.5f);
            SpawnCollapseDustPuff(crateCenter);

            PlayWoodBreakSound();

            // Ẩn thùng gốc và tắt collider
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders) col.enabled = false;

            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers) rend.enabled = false;

            // Tự động giải phóng sau 12 giây để tối ưu hiệu năng
            Destroy(debrisContainer, 12.0f);
            Destroy(gameObject, 0.1f);
        }

        private void SpawnCollapseDustPuff(Vector3 origin)
        {
            // Tạo 6 đám bụi nhỏ lan tỏa nhẹ dưới chân thùng
            Shader dustShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material dustMat = new Material(dustShader)
            {
                color = new Color(0.82f, 0.74f, 0.62f, 0.35f)
            };

            for (int d = 0; d < 6; d++)
            {
                GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dust.name = "WoodDustPuff";
                dust.transform.position = origin + new Vector3(Random.Range(-0.6f, 0.6f), -0.5f, Random.Range(-0.4f, 0.4f));
                dust.transform.localScale = Vector3.one * Random.Range(0.35f, 0.65f);
                Destroy(dust.GetComponent<Collider>());

                var rend = dust.GetComponent<MeshRenderer>();
                if (rend != null) rend.sharedMaterial = dustMat;

                StartCoroutine(FadeAndExpandDust(dust));
            }
        }

        private IEnumerator FadeAndExpandDust(GameObject dustGO)
        {
            if (dustGO == null) yield break;
            float dur = 0.85f;
            float elapsed = 0f;
            Vector3 startScale = dustGO.transform.localScale;
            Vector3 targetScale = startScale * 1.8f;
            var rend = dustGO.GetComponent<MeshRenderer>();

            while (elapsed < dur)
            {
                if (dustGO == null) yield break;
                float t = elapsed / dur;
                dustGO.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                dustGO.transform.position += Vector3.up * (Time.deltaTime * 0.4f);

                if (rend != null && rend.material != null)
                {
                    Color c = rend.material.color;
                    c.a = Mathf.Lerp(0.35f, 0f, t);
                    rend.material.color = c;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (dustGO != null) Destroy(dustGO);
        }

        private void PlayWoodHitSound(float pitch)
        {
            var audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.pitch = pitch + Random.Range(-0.08f, 0.08f);
            audioSource.volume = 0.85f;
            audioSource.PlayOneShot(GetOrCreateWoodClickClip());
        }

        private void PlayWoodBreakSound()
        {
            GameObject soundGO = new GameObject("Crate_BreakSound");
            soundGO.transform.position = transform.position;
            var audioSource = soundGO.AddComponent<AudioSource>();
            audioSource.pitch = 0.75f;
            audioSource.volume = 1.0f;
            audioSource.PlayOneShot(GetOrCreateWoodBreakClip());
            Destroy(soundGO, 2.5f);
        }

        private static AudioClip woodHitClip;
        private static AudioClip woodBreakClip;

        private static AudioClip GetOrCreateWoodClickClip()
        {
            if (woodHitClip != null) return woodHitClip;
            int samples = 44100 / 8; // 0.125s
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float decay = Mathf.Exp(-t * 22f);
                float wave = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.3f;
                data[i] = wave * decay * 0.8f;
            }
            woodHitClip = AudioClip.Create("WoodHitSynthetic", samples, 1, 44100, false);
            woodHitClip.SetData(data, 0);
            return woodHitClip;
        }

        private static AudioClip GetOrCreateWoodBreakClip()
        {
            if (woodBreakClip != null) return woodBreakClip;
            int samples = 44100 / 2; // 0.5s
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float decay = Mathf.Exp(-t * 9f);
                float noise = (Random.value * 2f - 1f) * 0.6f;
                float lowBoom = Mathf.Sin(2f * Mathf.PI * 85f * t) * 0.7f;
                data[i] = (lowBoom + noise) * decay * 0.9f;
            }
            woodBreakClip = AudioClip.Create("WoodBreakSynthetic", samples, 1, 44100, false);
            woodBreakClip.SetData(data, 0);
            return woodBreakClip;
        }
    }
}
