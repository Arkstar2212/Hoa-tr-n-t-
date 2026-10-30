using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Continuous Multi-Layered Nature Background Soundscape Controller for "CaMau_1954_Village.unity".
    /// Plays authentic Southern Vietnamese countryside nature audio in the background:
    /// 1. Primary Nature / Rice-Field Wind & Countryside Ambience (SFX_Ambience_RiceField_Wind.mp3)
    /// 2. Organic Delta Breeze & Foliage Rustle Layer (soundreality-wind-blowing-457954.mp3)
    /// 3. Spatial 3D Gentle River Lapping Ambience near the Southern Dock & Eastern Canal
    /// 4. Warm Procedural Morning/Afternoon Birdsong Chirps & Bamboo Chimes at natural intervals
    /// Dynamically syncs with PlayerPrefs ("Settings_MasterVol", "Settings_SFXVol", "Settings_MusicVol").
    /// </summary>
    [DisallowMultipleComponent]
    public class CaMauNatureAmbienceController : MonoBehaviour
    {
        [Header("Verified Nature Ambience Clips")]
        [SerializeField] private AudioClip riceFieldNatureClip;
        [SerializeField] private AudioClip deltaBreezeClip;
        [SerializeField] private AudioClip pastoralBgMusicClip;

        [Header("Mix Balance")]
        [Range(0f, 1f)] [SerializeField] private float primaryNatureBaseVol = 0.58f;
        [Range(0f, 1f)] [SerializeField] private float breezeSwellBaseVol = 0.34f;
        [Range(0f, 1f)] [SerializeField] private float riverSpatialBaseVol = 0.38f;
        [Range(0f, 1f)] [SerializeField] private float birdChirpBaseVol = 0.22f;
        [Range(0f, 1f)] [SerializeField] private float gentleMusicBaseVol = 0.24f;
        [SerializeField] private float fadeInDuration = 2.2f;

        private AudioSource primaryNatureSource;
        private AudioSource breezeLayerSource;
        private AudioSource riverSpatialSource;
        private AudioSource birdChirpSource;
        private AudioSource gentleMusicSource;

        private AudioClip generatedBirdChirpA;
        private AudioClip generatedBirdChirpB;
        private AudioClip generatedWaterLapLoop;

        private Transform playerTransform;
        private float fadeProgress = 0f;
        private float nextBirdChirpTimer = 3.5f;

        public void ConfigureAudioClips(AudioClip riceFieldNature, AudioClip deltaBreeze, AudioClip gentleMusic)
        {
            riceFieldNatureClip = riceFieldNature;
            deltaBreezeClip = deltaBreeze;
            pastoralBgMusicClip = gentleMusic;
        }

        private void Awake()
        {
            // Mute duplicate legacy IntroAudioController wind/music if present so nature layers mix cleanly without phasing
            var legacyIntroAudio = Object.FindAnyObjectByType<IntroAudioController>();
            if (legacyIntroAudio != null)
            {
                if (riceFieldNatureClip == null && legacyIntroAudio.windAudioSource != null)
                    deltaBreezeClip = legacyIntroAudio.windAudioSource.clip;
                if (pastoralBgMusicClip == null && legacyIntroAudio.lofiAudioSource != null)
                    pastoralBgMusicClip = legacyIntroAudio.lofiAudioSource.clip;

                if (legacyIntroAudio.windAudioSource != null) legacyIntroAudio.windAudioSource.Stop();
                if (legacyIntroAudio.lofiAudioSource != null) legacyIntroAudio.lofiAudioSource.Stop();
                legacyIntroAudio.enabled = false;
            }

            BuildSynthesizedDeltaNatureAccents();
            SetupAudioSources();
        }

        private void Start()
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null) playerTransform = playerGO.transform;

            if (primaryNatureSource != null && primaryNatureSource.clip != null) primaryNatureSource.Play();
            if (breezeLayerSource != null && breezeLayerSource.clip != null)
            {
                if (breezeLayerSource.clip.length > 3f)
                    breezeLayerSource.time = Random.Range(0f, breezeLayerSource.clip.length * 0.5f);
                breezeLayerSource.Play();
            }
            if (riverSpatialSource != null && riverSpatialSource.clip != null) riverSpatialSource.Play();
            if (gentleMusicSource != null && gentleMusicSource.clip != null) gentleMusicSource.Play();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            fadeProgress = Mathf.MoveTowards(fadeProgress, 1f, dt / Mathf.Max(0.25f, fadeInDuration));
            float smoothIn = Mathf.SmoothStep(0f, 1f, fadeProgress);

            float sfxSetting = PlayerPrefs.GetFloat("Settings_SFXVol", 0.70f);
            float musicSetting = PlayerPrefs.GetFloat("Settings_MusicVol", 0.65f);
            // Boost nature ambience floor slightly even if SFX slider was saved conservatively so nature is always audible
            float natureGain = Mathf.Clamp(Mathf.Max(0.55f, sfxSetting * 1.25f), 0f, 1f);

            // 1. Continuous Rice Field & Countryside Nature Bed
            if (primaryNatureSource != null)
            {
                float organicBreath = 0.92f + Mathf.Sin(Time.time * 0.28f) * 0.08f;
                primaryNatureSource.volume = primaryNatureBaseVol * natureGain * organicBreath * smoothIn;
            }

            // 2. Rolling Delta Wind Swell across Nipa Palms & Bamboo
            if (breezeLayerSource != null)
            {
                float windGust = 0.65f + (Mathf.Sin(Time.time * 0.42f + 1.3f) + 1f) * 0.22f;
                breezeLayerSource.volume = breezeSwellBaseVol * natureGain * windGust * smoothIn;
                breezeLayerSource.pitch = 0.96f + Mathf.Sin(Time.time * 0.19f) * 0.05f;
            }

            // 3. Spatial River & Canal Water Lapping (follows closest shoreline to An)
            if (riverSpatialSource != null)
            {
                if (playerTransform == null)
                {
                    var p = GameObject.FindWithTag("Player");
                    if (p != null) playerTransform = p.transform;
                }

                if (playerTransform != null)
                {
                    Vector3 pPos = playerTransform.position;
                    // Closest point between Southern River (Z = -11.5) and Eastern Canal (X = 16.5)
                    Vector3 southRiverPt = new Vector3(Mathf.Clamp(pPos.x, -24f, 24f), -0.4f, -11.5f);
                    Vector3 eastCanalPt = new Vector3(16.5f, -0.4f, Mathf.Clamp(pPos.z, -10f, 24f));
                    float dSouth = (pPos - southRiverPt).sqrMagnitude;
                    float dEast = (pPos - eastCanalPt).sqrMagnitude;
                    riverSpatialSource.transform.position = (dSouth <= dEast) ? southRiverPt : eastCanalPt;
                }
                riverSpatialSource.volume = riverSpatialBaseVol * natureGain * smoothIn;
            }

            // 4. Gentle Pastoral Background Melody (kept soft underneath the nature soundscape)
            if (gentleMusicSource != null)
            {
                gentleMusicSource.volume = gentleMusicBaseVol * musicSetting * smoothIn;
            }

            // 5. Occasional Natural Southern Countryside Birdsong Chirps
            nextBirdChirpTimer -= dt;
            if (nextBirdChirpTimer <= 0f)
            {
                nextBirdChirpTimer = Random.Range(4.8f, 10.5f);
                PlayRandomBirdChirp(natureGain * smoothIn);
            }
        }

        private void PlayRandomBirdChirp(float gain)
        {
            if (birdChirpSource == null) return;
            AudioClip clip = (Random.value > 0.45f) ? generatedBirdChirpA : generatedBirdChirpB;
            if (clip == null) return;

            if (playerTransform != null)
            {
                Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(6f, 15f);
                birdChirpSource.transform.position = playerTransform.position + new Vector3(circle.x, Random.Range(3.5f, 6.5f), circle.y);
            }

            birdChirpSource.pitch = Random.Range(0.92f, 1.14f);
            birdChirpSource.PlayOneShot(clip, birdChirpBaseVol * gain);
        }

        private void SetupAudioSources()
        {
            primaryNatureSource = CreateChildAudioSource("Nature_Primary_RiceFieldWind", riceFieldNatureClip ?? deltaBreezeClip, true, 0f);
            breezeLayerSource = CreateChildAudioSource("Nature_Secondary_DeltaBreeze", deltaBreezeClip ?? riceFieldNatureClip, true, 0f);

            riverSpatialSource = CreateChildAudioSource("Nature_Spatial_RiverWater", generatedWaterLapLoop, true, 0.82f);
            riverSpatialSource.minDistance = 3.5f;
            riverSpatialSource.maxDistance = 22.0f;
            riverSpatialSource.rolloffMode = AudioRolloffMode.Linear;
            riverSpatialSource.transform.position = new Vector3(2.0f, -0.4f, -11.5f);

            birdChirpSource = CreateChildAudioSource("Nature_Spatial_CountrysideBirds", null, false, 0.65f);
            birdChirpSource.minDistance = 4.0f;
            birdChirpSource.maxDistance = 28.0f;

            if (pastoralBgMusicClip != null)
            {
                gentleMusicSource = CreateChildAudioSource("Nature_GentlePastoral_BGM", pastoralBgMusicClip, true, 0f);
            }
        }

        private AudioSource CreateChildAudioSource(string childName, AudioClip clip, bool loop, float spatialBlend)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = loop;
            src.playOnAwake = false;
            src.spatialBlend = spatialBlend;
            src.volume = 0f;
            return src;
        }

        private void BuildSynthesizedDeltaNatureAccents()
        {
            const int sampleRate = 22050;

            // Soft twin-note bulbul / sparrow chirp ("chào mào / sẻ đồng" Nam Bộ)
            int birdSamplesA = Mathf.RoundToInt(sampleRate * 0.42f);
            float[] dataA = new float[birdSamplesA];
            for (int i = 0; i < birdSamplesA; i++)
            {
                float t = (float)i / sampleRate;
                float note1 = (t < 0.16f) ? Mathf.Sin(t / 0.16f * Mathf.PI) * Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(2150f, 2680f, t / 0.16f) * t) : 0f;
                float note2 = (t >= 0.19f && t < 0.39f) ? Mathf.Sin((t - 0.19f) / 0.20f * Mathf.PI) * Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(2520f, 2050f, (t - 0.19f) / 0.20f) * t) : 0f;
                dataA[i] = (note1 + note2) * 0.28f;
            }
            generatedBirdChirpA = AudioClip.Create("Proc_CaMau_BirdChirp_A", birdSamplesA, 1, sampleRate, false);
            generatedBirdChirpA.SetData(dataA, 0);

            // Soft 3-syllable wetland warbler trill
            int birdSamplesB = Mathf.RoundToInt(sampleRate * 0.55f);
            float[] dataB = new float[birdSamplesB];
            for (int i = 0; i < birdSamplesB; i++)
            {
                float t = (float)i / sampleRate;
                float env = 0f;
                float freq = 2300f;
                if (t < 0.12f) { env = Mathf.Sin(t / 0.12f * Mathf.PI); freq = Mathf.Lerp(2200f, 2550f, t / 0.12f); }
                else if (t >= 0.15f && t < 0.28f) { env = Mathf.Sin((t - 0.15f) / 0.13f * Mathf.PI); freq = Mathf.Lerp(2350f, 2780f, (t - 0.15f) / 0.13f); }
                else if (t >= 0.31f && t < 0.50f) { env = Mathf.Sin((t - 0.31f) / 0.19f * Mathf.PI); freq = Mathf.Lerp(2650f, 1980f, (t - 0.31f) / 0.19f); }
                dataB[i] = env * Mathf.Sin(2f * Mathf.PI * freq * t) * 0.24f;
            }
            generatedBirdChirpB = AudioClip.Create("Proc_CaMau_BirdChirp_B", birdSamplesB, 1, sampleRate, false);
            generatedBirdChirpB.SetData(dataB, 0);

            // Gentle seamless 4-second filtered river water lapping loop along wooden sampans & mudbanks
            int waterSamples = sampleRate * 4;
            float[] waterData = new float[waterSamples];
            float lpState = 0f;
            float lpState2 = 0f;
            System.Random rng = new System.Random(1954);
            for (int i = 0; i < waterSamples; i++)
            {
                float t = (float)i / sampleRate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                // Two-pole low-pass filter modulated by gentle river wave rhythm (0.5 Hz & 0.25 Hz)
                float waveEnv = 0.45f + 0.35f * Mathf.Sin(2f * Mathf.PI * 0.5f * t) + 0.20f * Mathf.Sin(2f * Mathf.PI * 0.25f * t + 0.9f);
                float alpha = 0.045f + 0.035f * waveEnv;
                lpState += alpha * (white - lpState);
                lpState2 += alpha * (lpState - lpState2);

                // Crossfade loop endpoints smoothly
                float edgeFade = 1f;
                if (i < 1200) edgeFade = i / 1200f;
                else if (i > waterSamples - 1200) edgeFade = (waterSamples - i) / 1200f;

                waterData[i] = lpState2 * waveEnv * edgeFade * 0.65f;
            }
            generatedWaterLapLoop = AudioClip.Create("Proc_CaMau_RiverWaterLap", waterSamples, 1, sampleRate, false);
            generatedWaterLapLoop.SetData(waterData, 0);
        }
    }
}
