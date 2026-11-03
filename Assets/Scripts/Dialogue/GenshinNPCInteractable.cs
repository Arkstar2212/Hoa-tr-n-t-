using System;
using UnityEngine;

namespace HoaVaMau.Dialogue
{
    [Serializable]
    public class DialogueChoice
    {
        [Tooltip("Nội dung lựa chọn câu trả lời của Bé An (Genshin style)")]
        public string choiceText = "Dạ con hiểu rồi.";

        [Tooltip("Câu đối thoại phản hồi của NPC sau khi chọn")]
        [TextArea(2, 4)]
        public string responseLine = "Tốt lắm con!";

        [Tooltip("Animation state khi NPC phản hồi (tùy chọn)")]
        public string responseAnim = "";
    }

    /// <summary>
    /// Component gắn vào mỗi NPC trong làng để quản lý danh tính, hội thoại,
    /// các lựa chọn rẽ nhánh (Genshin / BOTW style), và cử chỉ animation.
    /// </summary>
    [DisallowMultipleComponent]
    public class GenshinNPCInteractable : MonoBehaviour
    {
        [Header("NPC Identity")]
        [SerializeField] private string npcName = "Người Dân Làng";
        [SerializeField] private string npcTitle = "Xóm Rạch Gốc - Cà Mau 1954";

        [Header("Conversation Lines")]
        [SerializeField]
        [TextArea(2, 4)]
        private string[] openingLines = new string[]
        {
            "Chào cháu An! Lúa năm nay trúng mùa lớn quá."
        };

        [Header("Player Branching Choices (Genshin Style)")]
        [SerializeField]
        private DialogueChoice[] choices = new DialogueChoice[0];

        [Header("Animation States")]
        [SerializeField] private string defaultAnimState = "Idle";
        [SerializeField] private string talkAnimState = "Talking";

        [Header("Interaction & Camera Tuning")]
        [SerializeField] private float interactRadius = 3.0f;
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2.05f, 0f);
        [SerializeField] private Vector3 customCamOffset = Vector3.zero;

        private Animator animator;
        private Quaternion initialRotation;

        public string NPCName => npcName;
        public string NPCTitle => npcTitle;
        public string[] OpeningLines => openingLines;
        public DialogueChoice[] Choices => choices;
        public string DefaultAnimState => defaultAnimState;
        public string TalkAnimState => talkAnimState;
        public float InteractRadius => interactRadius;
        public Vector3 PromptOffset => promptOffset;
        public Vector3 CustomCamOffset => customCamOffset;

        public void Configure(string name, string title, string[] openings, DialogueChoice[] choiceList, string defaultAnim, string talkAnim)
        {
            npcName = name;
            npcTitle = title;
            openingLines = openings;
            choices = choiceList;
            defaultAnimState = defaultAnim;
            talkAnimState = talkAnim;
        }

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            initialRotation = transform.rotation;
        }

        private void Start()
        {
            PlayAnim(defaultAnimState, UnityEngine.Random.Range(0f, 0.5f));
        }

        public void PlayAnim(string stateName, float normalizedTime = 0f)
        {
            if (animator == null || string.IsNullOrEmpty(stateName) || animator.runtimeAnimatorController == null) return;
            int hash = Animator.StringToHash(stateName);
            if (animator.HasState(0, hash))
            {
                animator.CrossFadeInFixedTime(hash, 0.25f);
            }
        }

        public void ResetToInitialRotation()
        {
            transform.rotation = initialRotation;
        }
    }
}
