using System;
using UnityEngine;

namespace HoaDo.Environment
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-50)]
    public class PlayerShaderPositionTracker : MonoBehaviour
    {
        public static PlayerShaderPositionTracker Instance { get; private set; }

        [Header("Tracking Target")]
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private float interactionRadius = 1.2f;

        [Header("Smoothing")]
        [SerializeField] private float velocitySmoothing = 12f;

        private Vector3 _lastPosition;
        private Vector2 _smoothVelocity;

        private static readonly int PropPlayerPosShort = Shader.PropertyToID("_PlayerPos");
        private static readonly int PropPlayerPosFull = Shader.PropertyToID("_PlayerPosition");
        private static readonly int PropPlayerMovement = Shader.PropertyToID("_PlayerMovement");

        private void OnEnable()
        {
            Instance = this;
            if (targetPlayer == null)
            {
                FindTargetPlayer();
            }
            if (targetPlayer != null)
            {
                _lastPosition = targetPlayer.position;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (targetPlayer == null)
            {
                FindTargetPlayer();
            }

            Vector3 currentPos = targetPlayer != null ? targetPlayer.position : transform.position;
            float dt = Mathf.Max(0.001f, Time.deltaTime);

            Vector2 rawVelocity = new Vector2(currentPos.x - _lastPosition.x, currentPos.y - _lastPosition.y) / dt;
            _smoothVelocity = Vector2.Lerp(_smoothVelocity, rawVelocity, dt * velocitySmoothing);
            _lastPosition = currentPos;

            float speed = _smoothVelocity.magnitude;
            float dirFactor = Mathf.Sign(_smoothVelocity.x);

            Vector4 posVector = new Vector4(currentPos.x, currentPos.y, currentPos.z, interactionRadius);
            Vector4 moveVector = new Vector4(_smoothVelocity.x, _smoothVelocity.y, speed, dirFactor);

            Shader.SetGlobalVector(PropPlayerPosShort, posVector);
            Shader.SetGlobalVector(PropPlayerPosFull, posVector);
            Shader.SetGlobalVector(PropPlayerMovement, moveVector);
        }

        private void FindTargetPlayer()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                targetPlayer = p.transform;
                return;
            }

            var playerGo = GameObject.Find("PF_Player_Minh") ?? GameObject.Find("Character_An") ?? GameObject.Find("Player");
            if (playerGo != null)
            {
                targetPlayer = playerGo.transform;
            }
        }
    }
}
