using UnityEngine;
using UnityEngine.Events;

namespace HoaVaMau.Test3D
{
    public enum InteractionType
    {
        Inspect,
        Push,
        Trigger
    }

    public class InteractableObject3D : MonoBehaviour
    {
        [Header("Config")]
        public InteractionType interactionType = InteractionType.Inspect;
        public string objectTitle = "Ancient Relic";
        public string promptMessage = "Press [E] to Inspect";
        public string actionMessage = "You found an object.";
        public float interactionRadius = 2.5f;
        public bool isTriggerZone = false;

        [Header("Feedback")]
        [SerializeField] private Light targetLight;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color activatedColor = Color.cyan;

        [Header("Events")]
        public UnityEvent onInteracted;

        private bool isActivated = false;

        public bool IsActivated => isActivated;

        public void Interact()
        {
            if (interactionType == InteractionType.Inspect)
            {
                Debug.Log($"<color=#38bdf8>[Interaction - Inspect]</color> {actionMessage} ({objectTitle})");
                onInteracted?.Invoke();
            }
            else if (interactionType == InteractionType.Trigger)
            {
                TriggerObject();
            }
        }

        public void TriggerObject()
        {
            isActivated = !isActivated;
            Debug.Log($"<color=#34d399>[Interaction - Trigger]</color> {actionMessage} State: {(isActivated ? "ON" : "OFF")}");

            if (targetLight != null)
            {
                targetLight.enabled = isActivated;
                targetLight.color = activatedColor;
            }

            if (targetRenderer != null)
            {
                targetRenderer.material.color = isActivated ? activatedColor : Color.white;
            }

            onInteracted?.Invoke();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggerZone && other.CompareTag("Player"))
            {
                TriggerObject();
                InteractionSystem3D.ShowHUDMessage(actionMessage, 3.0f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
