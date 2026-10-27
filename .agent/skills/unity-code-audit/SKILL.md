---
name: unity-code-audit
description: Code auditing, compilation verification, NullReferenceException prevention, missing Inspector reference checks, and Unity Editor MCP debugging workflows.
---

# Unity Code Audit & Debugging Workflow

This skill provides an automated audit checklist and procedure for diagnosing, inspecting, and resolving errors in Unity 6, leveraging the live Unity MCP connection.

## 1. Post-Change Verification Pipeline

Whenever a C# script, asset, or scene file is modified:

```
[Write/Edit Script]
       │
       ▼
[Check Compilation (Unity MCP)] ──(Errors Found)──► Fix syntax/type errors
       │
   (Compiled OK)
       │
       ▼
[Audit Inspector & Prefab References]
       │
       ▼
[Enter Play Mode (Unity MCP)] ──► Monitor Console Logs
       │
   (Zero NREs / Clean)
       │
       ▼
[Exit Play Mode] ──► Task Complete
```

---

## 2. Using Unity MCP for Audits

The Unity MCP server is connected directly to the active Unity Editor instance. Use these tools:

1. **Verify Compilation & Status**:
   ```json
   call_mcp_tool: {
     "ServerName": "unity",
     "ToolName": "editor_status",
     "Arguments": {}
   }
   ```
   Check if `compiling` is false and no domain reload errors exist.

2. **Read Console Logs for Warnings/Errors**:
   ```json
   call_mcp_tool: {
     "ServerName": "unity",
     "ToolName": "get_console_logs",
     "Arguments": { "types": ["Error", "Exception", "Assert"] }
   }
   ```
   Inspect stack traces for any `NullReferenceException`, `MissingComponentException`, or `MissingReferenceException`.

3. **Check for Missing Scripts on Active Hierarchy**:
   Use `eval` tool to detect game objects with missing MonoBehaviours (the notorious yellow warning "The referenced script on this Behaviour is missing!"):
   ```json
   call_mcp_tool: {
     "ServerName": "unity",
     "ToolName": "eval",
     "Arguments": {
       "code": "var missingCount = 0; foreach(var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)) { var components = go.GetComponents<Component>(); for(int i=0; i<components.Length; i++) { if(components[i] == null) { Debug.LogWarning($\"Missing component on {go.name}\"); missingCount++; } } } return $\"Found {missingCount} missing components\";"
     }
   }
   ```

4. **Run Live Play Mode Smoke Test**:
   ```json
   call_mcp_tool: { "ServerName": "unity", "ToolName": "editor_play", "Arguments": {} }
   ```
   Wait 2-3 seconds, inspect console logs, then return to edit mode:
   ```json
   call_mcp_tool: { "ServerName": "unity", "ToolName": "editor_stop", "Arguments": {} }
   ```

---

## 3. Common Error Signatures & Fixes

| Symptom | Root Cause | Solution |
| :--- | :--- | :--- |
| `NullReferenceException` in `Awake`/`Start` | Unassigned Inspector reference or execution order issue | Add null guard with descriptive `Debug.LogError($"[ScriptName] '{nameof(field)}' is not assigned on {name}!");`, or use `RequireComponent`. |
| `MissingReferenceException` during scene transition | Destroyed object still referenced in singleton/event | Always unsubscribe delegates in `OnDisable()`. Use `OnDestroy()` to clear static event references. |
| `ArgumentOutOfRangeException` in UI | Text or list index out of bounds | Check collection length before indexing. |
| `linearVelocity` vs `velocity` in Unity 6 | Rigidbody2D property rename in Unity 6 | Use `rb.linearVelocity` instead of obsolete `rb.velocity`. |

---

## 4. Audit Checklist

- [ ] Script compiles with 0 compilation errors.
- [ ] No `MissingComponent` on any prefab or active GameObject.
- [ ] Serialized fields in prefabs/scenes remain assigned after field rename (use `[FormerlySerializedAs("oldName")]` if renaming).
- [ ] Play Mode starts without uncaught exceptions.
- [ ] Clean editor stop without lingering background threads or coroutines.
