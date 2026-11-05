using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Interactive Water Wave Ripple Hover Effect for UI Menu Options.
    /// Drives the "UI/WaterWaveRipple" shader to create:
    /// 1. Expanding circular water ripples radiating from the mouse cursor on hover.
    /// 2. Continuous flowing river wave shimmer across the button frame.
    /// 3. Soft golden sun-crest highlights fitting the Nam Bộ riverine aesthetic.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class UIWaveRippleHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Target Graphic")]
        [Tooltip("The Image component that displays the selection background box.")]
        [SerializeField] private Image targetImage;

        [Header("Timing & Animation")]
        [SerializeField] private float rippleDuration = 0.65f;
        [SerializeField] private float hoverFadeSpeed = 8.0f;

        private Material instancedMaterial;
        private RectTransform rectTransform;
        private bool isHovered = false;
        private float currentHoverBlend = 0f;
        private float rippleProgress = 1f;
        private Vector2 currentCenterUV = new Vector2(0.5f, 0.5f);

        private static Shader s_WaveShader = null;
        private static Sprite s_WhiteBoxSprite = null;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (targetImage == null) targetImage = GetComponent<Image>();

            InitMaterial();
        }

        private void OnEnable()
        {
            InitMaterial();
        }

        public void InitMaterial()
        {
            if (s_WaveShader == null) s_WaveShader = Shader.Find("UI/WaterWaveRipple");
            if (s_WhiteBoxSprite == null)
            {
#if UNITY_EDITOR
                s_WhiteBoxSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/T_UI_WhiteBox.png");
#endif
            }

            if (targetImage != null)
            {
                if (targetImage.sprite == null && s_WhiteBoxSprite != null)
                {
                    targetImage.sprite = s_WhiteBoxSprite;
                }

                if (instancedMaterial == null && s_WaveShader != null)
                {
                    instancedMaterial = new Material(s_WaveShader);
                    instancedMaterial.name = $"{gameObject.name}_WaterRipple_Mat";
                    targetImage.material = instancedMaterial;
                }
            }
        }

        private void OnDestroy()
        {
            if (instancedMaterial != null)
            {
                if (Application.isPlaying) Destroy(instancedMaterial);
                else DestroyImmediate(instancedMaterial);
            }
        }

        public void TriggerRipple(Vector2 uv)
        {
            currentCenterUV = uv;
            rippleProgress = 0f;
            isHovered = true;

            if (instancedMaterial != null)
            {
                instancedMaterial.SetVector("_RippleCenter", new Vector4(currentCenterUV.x, currentCenterUV.y, 0f, 0f));
            }
        }

        public void SetSelectedState(bool selected)
        {
            if (selected)
            {
                if (!isHovered)
                {
                    TriggerRipple(new Vector2(0.5f, 0.5f));
                }
            }
            else
            {
                isHovered = false;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0.0001f) return;

            // Blend hover opacity
            float targetBlend = isHovered ? 1.0f : 0.0f;
            currentHoverBlend = Mathf.MoveTowards(currentHoverBlend, targetBlend, dt * hoverFadeSpeed);

            // Animate ripple progress
            if (rippleProgress < 1.0f)
            {
                rippleProgress += dt / rippleDuration;
                if (rippleProgress > 1.0f) rippleProgress = 1.0f;
            }

            // Update shader properties
            if (instancedMaterial != null)
            {
                instancedMaterial.SetFloat("_HoverBlend", currentHoverBlend);
                instancedMaterial.SetFloat("_RippleProgress", rippleProgress);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);

            Rect rect = rectTransform.rect;
            float uvX = Mathf.Clamp01((localPoint.x - rect.xMin) / rect.width);
            float uvY = Mathf.Clamp01((localPoint.y - rect.yMin) / rect.height);

            TriggerRipple(new Vector2(uvX, uvY));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!isHovered) return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                Rect rect = rectTransform.rect;
                float uvX = Mathf.Clamp01((localPoint.x - rect.xMin) / rect.width);
                float uvY = Mathf.Clamp01((localPoint.y - rect.yMin) / rect.height);

                currentCenterUV = Vector2.Lerp(currentCenterUV, new Vector2(uvX, uvY), Time.unscaledDeltaTime * 6.0f);
                if (instancedMaterial != null)
                {
                    instancedMaterial.SetVector("_RippleCenter", new Vector4(currentCenterUV.x, currentCenterUV.y, 0f, 0f));
                }
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            TriggerRipple(new Vector2(0.5f, 0.5f));
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isHovered = false;
        }
    }
}
