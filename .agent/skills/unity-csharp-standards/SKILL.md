---
name: unity-csharp-standards
description: C# coding standards, architecture patterns, and lifecycle guidelines for Unity 6 (6000.x), URP, and the New Input System in Hoa trên đất đỏ.
---

# Unity C# Standards & Architecture Guide

This skill governs all C# script creation, refactoring, and code reviews in **Hoa trên đất đỏ** (Unity 6 / URP).

## 1. Core Architectural Principles

- **Component-First & Single Responsibility (SRP)**: Each MonoBehaviour must have one distinct responsibility (e.g., `PlayerMovement` handles physics locomotion, `PlayerAnimation` reacts to movement events, `PlayerInteraction` detects raycasts).
- **Never Duplicate Systems**: Before creating a new script, check `Assets/Scripts` and existing assemblies to see if a manager or controller already exists.
- **Do Not Break Running Systems**: Preserve public APIs and serialized field names whenever possible. If refactoring is mandatory, provide backward compatibility or update all prefabs/scenes using Unity MCP.
- **Event-Driven Decoupling**: Systems should communicate via C# `Action`, `UnityEvent`, or ScriptableObject architecture rather than tightly coupled direct calls. Always unsubscribe in `OnDisable()` or `OnDestroy()`.

---

## 2. Serialized Fields & Encapsulation

- **Never use `public` fields for Inspector exposure**. Always use `[SerializeField] private`:
  ```csharp
  // Correct
  [SerializeField] private float moveSpeed = 5.5f;
  [SerializeField] private Rigidbody2D rb;

  public float MoveSpeed => moveSpeed;
  ```
- **Grouping and Tooltips**: Use `[Header]`, `[Tooltip]`, and `[Space]` for clear Inspector readability:
  ```csharp
  [Header("Movement Settings")]
  [Tooltip("Max horizontal walk speed in units per second.")]
  [SerializeField] private float moveSpeed = 5.0f;
  ```
- **RequireComponent**: Ensure required dependencies are always present:
  ```csharp
  [RequireComponent(typeof(Rigidbody2D))]
  [RequireComponent(typeof(Collider2D))]
  public class PlayerController2D : MonoBehaviour
  ```

---

## 3. Unity Lifecycle & Performance Rules

- **Caching**: Never call `GetComponent<T>()`, `FindObjectOfType<T>()`, or `GameObject.Find()` inside `Update()`, `LateUpdate()`, or `FixedUpdate()`. Cache references in `Awake()` or `Start()`.
- **Update vs FixedUpdate**:
  - `Update()`: Process input, frame-based animations, non-physics timers.
  - `FixedUpdate()`: Apply physics forces, modify `rb.linearVelocity` (Unity 6 naming), physics raycasts.
  - `LateUpdate()`: Camera tracking, post-movement adjustments.
- **Unity Object Null Checks**:
  - Remember that Unity overrides `operator ==` for `UnityEngine.Object` to handle destroyed C++ instances.
  - Avoid `obj?.Method()` on `UnityEngine.Object` if you rely on Unity's fake-null check; use standard `if (obj != null) obj.Method();`.
- **String & Hash Optimization**:
  - Cache Animator hashes instead of strings:
    ```csharp
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    ```

---

## 4. Input System (v1.20+ New Input System)

- Always use the new `UnityEngine.InputSystem`:
  ```csharp
  [SerializeField] private PlayerInput playerInput;
  private InputAction moveAction;

  private void Awake()
  {
      moveAction = playerInput.actions["Move"];
  }

  private void OnEnable() => moveAction?.Enable();
  private void OnDisable() => moveAction?.Disable();
  ```
- Never call legacy `Input.GetKey`, `Input.GetAxis`, or `Input.GetMouseButton` in Unity 6 projects where the new Input System is configured.

---

## 5. Defensive Coding Checklist

Before finishing any script:
1. Are all event subscriptions cleaned up in `OnDisable()`?
2. Are all required Inspector fields guarded with `Debug.LogError` or `RequireComponent`?
3. Does the script compile with zero warnings? (Check with `call_mcp_tool unity console` or `recompile_status`).
4. Does it conform to the namespace structure of the project (`HoaTrenDatDo...`)?
