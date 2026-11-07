using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// URP-compatible runtime screen capture helper for Save Slot & Checkpoint thumbnails.
    /// Captures the post-processed frame at WaitForEndOfFrame and downsamples to crisp vertical portrait card ratio.
    /// </summary>
    public class ThumbnailCaptureRuntime : MonoBehaviour
    {
        private static ThumbnailCaptureRuntime instance;

        public static void CaptureSlot(int slotIndex)
        {
            if (!Application.isPlaying) return;
            EnsureInstance();
            instance.StartCoroutine(instance.CaptureRoutine(slotIndex));
        }

        private static void EnsureInstance()
        {
            if (instance == null)
            {
                var go = new GameObject("[ThumbnailCaptureRuntime]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<ThumbnailCaptureRuntime>();
            }
        }

        private IEnumerator CaptureRoutine(int slotIndex)
        {
            yield return new WaitForEndOfFrame();

            try
            {
                int screenW = Screen.width;
                int screenH = Screen.height;
                if (screenW <= 10 || screenH <= 10) yield break;

                // Capture full screen texture
                Texture2D screenTex = ScreenCapture.CaptureScreenshotAsTexture();
                if (screenTex == null)
                {
                    screenTex = new Texture2D(screenW, screenH, TextureFormat.RGB24, false);
                    screenTex.ReadPixels(new Rect(0, 0, screenW, screenH), 0, 0);
                    screenTex.Apply();
                }

                // Vertical slice matching Planet of Lana save slot card aspect ratio (280 : 480 ~ 0.583)
                float targetAspect = 280f / 480f;
                int cropH = screenTex.height;
                int cropW = Mathf.RoundToInt(cropH * targetAspect);
                if (cropW > screenTex.width)
                {
                    cropW = screenTex.width;
                    cropH = Mathf.RoundToInt(cropW / targetAspect);
                }

                // Center crop or focus on player position in screen space
                int startX = (screenTex.width - cropW) / 2;
                int startY = (screenTex.height - cropH) / 2;

                var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (player != null && Camera.main != null)
                {
                    Vector3 sp = Camera.main.WorldToScreenPoint(player.transform.position + Vector3.up * 0.8f);
                    if (sp.z > 0f)
                    {
                        startX = Mathf.Clamp(Mathf.RoundToInt(sp.x - cropW * 0.5f), 0, screenTex.width - cropW);
                    }
                }

                int outW = 280;
                int outH = 480;
                RenderTexture rt = RenderTexture.GetTemporary(outW, outH, 0, RenderTextureFormat.ARGB32);
                Vector2 scale = new Vector2((float)cropW / screenTex.width, (float)cropH / screenTex.height);
                Vector2 offset = new Vector2((float)startX / screenTex.width, (float)startY / screenTex.height);

                Graphics.Blit(screenTex, rt, scale, offset);

                RenderTexture prevActive = RenderTexture.active;
                RenderTexture.active = rt;

                Texture2D thumbTex = new Texture2D(outW, outH, TextureFormat.RGB24, false);
                thumbTex.ReadPixels(new Rect(0, 0, outW, outH), 0, 0);
                thumbTex.Apply();

                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
                Destroy(screenTex);

                byte[] pngBytes = thumbTex.EncodeToPNG();
                Destroy(thumbTex);

                string path = HoaVaMauSaveSystem.GetThumbnailFilePath(slotIndex);
                File.WriteAllBytes(path, pngBytes);
                Debug.Log($"<color=#38bdf8>[ThumbnailCaptureRuntime]</color> Saved live slot {slotIndex} checkpoint thumbnail at: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ThumbnailCaptureRuntime] Failed to capture slot {slotIndex} thumbnail: {ex.Message}");
            }
        }
    }
}
