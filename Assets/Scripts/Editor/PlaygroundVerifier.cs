#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class PlaygroundVerifier
    {
        [MenuItem("Hoa va Mau/Rebuild And Verify")]
        public static void RebuildAndVerify()
        {
            CharacterPlaygroundBuilder.BuildMenuCommand();
            VerifyDiagnostics();
        }

        public static void TeleportToStation1() => TeleportPlayer(-17.5f, 0.05f);
        public static void TeleportToStation4_Crate() => TeleportPlayer(23.5f, 0.05f);
        public static void TeleportToStation5_Rope() => TeleportPlayer(37.0f, 3.65f);
        public static void TeleportToStation6_Throw() => TeleportPlayer(56.0f, 3.65f);

        private static void TeleportPlayer(float x, float y)
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.transform.position = new Vector3(x, y, 0f);
            }
            var cam = Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<Cinematic2DCamera>();
                if (camCtrl != null) camCtrl.SnapToTarget();
            }
        }

        [MenuItem("Hoa va Mau/Verify Playground Diagnostics")]
        public static void VerifyDiagnostics()
        {
            GameObject root = GameObject.Find("--- Character Playground (Planet of Lana Style) ---");
            if (root == null)
            {
                Debug.LogError("[PlaygroundVerifier] Root NOT found!");
                return;
            }

            Debug.Log($"<color=#40b8a6>[PlaygroundVerifier]</color> Root found! Total children: {root.transform.childCount}");

            // 1. Check Player
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                var ledge = player.GetComponent<LedgeGrabDetector>();
                var grassInter = player.GetComponent<GrassInteraction>();
                Debug.Log($"<color=#40b8a6>[PlaygroundVerifier]</color> Player found: {player.name} at {player.transform.position}. CC: {cc != null}, Ledge: {ledge != null}, GrassInteraction: {grassInter != null}");
            }
            else
            {
                Debug.LogError("[PlaygroundVerifier] Player NOT found!");
            }

            // 2. Check Camera
            var cam = Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<Cinematic2DCamera>();
                Debug.Log($"<color=#40b8a6>[PlaygroundVerifier]</color> Camera: {cam.name} at {cam.transform.position}, CamCtrl: {camCtrl != null}, Target: {(camCtrl != null ? camCtrl.Target?.name : "null")}");
            }

            // 3. Check Crate
            var crate = Object.FindAnyObjectByType<InteractableCrate>();
            if (crate != null)
            {
                var col = crate.GetComponent<BoxCollider>();
                var rb = crate.GetComponent<Rigidbody>();
                Debug.Log($"<color=#40b8a6>[PlaygroundVerifier]</color> Crate found at {crate.transform.position}, Collider: size={col.size}, center={col.center}, RB: mass={rb.mass}, kinematic={rb.isKinematic}, constraints={rb.constraints}");
            }

            // 4. Check Rope
            var rope = Object.FindAnyObjectByType<RopeController>();
            if (rope != null)
            {
                Debug.Log($"<color=#40b8a6>[PlaygroundVerifier]</color> Rope found at {rope.transform.position}");
            }

            // 5. Check Managers
            var cpMgr = Object.FindAnyObjectByType<CheckpointManager>();
            var windMgr = Object.FindAnyObjectByType<WindManager>();
            var grassMgr = Object.FindAnyObjectByType<GrassManager>();
            Debug.Log($"<color=#40b8a6>[PlaygroundVerifier]</color> Managers: CheckpointManager={cpMgr != null}, WindManager={windMgr != null}, GrassManager={grassMgr != null}");

            // 6. Check Grass Tufts
            var tufts = Object.FindObjectsByType<Stylized3DGrassTuft>();
            Debug.Log($"<color=#40b8a6>[PlaygroundVerifier]</color> Total Stylized 3D Grass Tufts: {tufts.Length}");

            // 7. Check Platforms thickness
            var platforms = root.GetComponentsInChildren<MeshRenderer>();
            int platformCount = 0;
            foreach (var p in platforms)
            {
                if (p.name.Contains("Runway") || p.name.Contains("Ledge") || p.name.Contains("Tunnel") || p.name.Contains("Cliff") || p.name.Contains("Floor"))
                {
                    platformCount++;
                    Debug.Log($"<color=#40b8a6>[PlaygroundVerifier]</color> Platform: {p.name} Pos={p.transform.position} Scale={p.transform.lossyScale}");
                }
            }
            Debug.Log($"<color=#40b8a6>[PlaygroundVerifier]</color> Total Verified Platforms: {platformCount}");
        }
    }
}
#endif
