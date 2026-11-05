using UnityEngine;
using UnityEditor;
using System.IO;

namespace HoaVaMau.EditorTools
{
    public static class OverhaulAndCutsceneEnhancer
    {
        [MenuItem("Tools/Hoa Tren Dat Do/1. Fix 3D Rice Field Height (Realistic 1.2m)")]
        public static void AdjustRicePaddyHeight()
        {
            var west = GameObject.Find("West_Paddy_Flank");
            var east = GameObject.Find("East_Paddy_Flank");

            if (west == null && east == null)
            {
                Debug.LogWarning("[RiceHeight] West/East Paddy Flank not found. Make sure CaMau_1954_Village is open!");
                return;
            }

            int count = 0;
            // Realistic height: ~1.2m (down from 3.44m), wider footprint for dense golden carpet
            Vector3 targetScale = new Vector3(2.2f, 1.35f, 2.2f);

            void RescaleFlank(GameObject flank)
            {
                if (flank == null) return;
                Undo.RecordObject(flank.transform, "Rescale Rice Flank");
                for (int i = 0; i < flank.transform.childCount; i++)
                {
                    var child = flank.transform.GetChild(i);
                    child.localScale = targetScale;
                    count++;
                }
            }

            RescaleFlank(west);
            RescaleFlank(east);

            if (west != null) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(west.scene);
            Debug.Log($"<color=green>[RiceHeight] Successfully rescaled {count} rice tufts to realistic height (~1.2m)!</color>");
        }

        [MenuItem("Tools/Hoa Tren Dat Do/2. Build 1954 Tap Ket Harbor Atmosphere")]
        public static void BuildTapKetHarborAtmosphere()
        {
            const string GroupName = "[Additive_1954_TapKet_Harbor]";
            GameObject existing = GameObject.Find(GroupName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            GameObject root = new GameObject(GroupName);
            Undo.RegisterCreatedObjectUndo(root, "Create Tap Ket Harbor Root");

            // 1. Distant Coastal Mangrove / Headland Silhouette on the Far Horizon (Z = -275m)
            var ridgeTex = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Environment/panoramic_blue_ridges.png");
            if (ridgeTex != null)
            {
                var coastGO = new GameObject("Far_Coastal_Mangrove_Headland");
                coastGO.transform.SetParent(root.transform, false);
                coastGO.transform.position = new Vector3(56.0f, 6.5f, -275.0f);
                coastGO.transform.rotation = Quaternion.Euler(0f, 178f, 0f);
                coastGO.transform.localScale = new Vector3(450.0f, 26.0f, 1.0f);
                var sr = coastGO.AddComponent<SpriteRenderer>();
                sr.sprite = ridgeTex;
                sr.color = new Color(0.65f, 0.78f, 0.85f, 0.60f); // Soft atmospheric haze matching sunny morning bay
            }

            // 2. Offshore Transport Ship 1954 (Kilinski) - Root anchored at Sea Level (Y = -0.58)
            var shipCardSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Canh1_Sprites/TransportShip_1954_Card.png");
            var wakeSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Environment/T_Camau_Boat_WaterWake.png");

            var shipAnchor = new GameObject("Ship_1954_Kilinski_Anchor");
            shipAnchor.transform.SetParent(root.transform, false);
            shipAnchor.transform.position = new Vector3(56.0f, -0.58f, -220.0f); // Exactly at water level Y = -0.58!
            shipAnchor.transform.rotation = Quaternion.Euler(0f, 178f, 0f); // Perpendicular to camera view axis

            if (shipCardSpr != null)
            {
                var shipVisual = new GameObject("Ship_Visual_Billboard");
                shipVisual.transform.SetParent(shipAnchor.transform, false);
                // Offset Y = 22.5m so the dark lower hull waterline sits naturally in the water
                shipVisual.transform.localPosition = new Vector3(0f, 22.5f, 0f);
                shipVisual.transform.localRotation = Quaternion.identity;
                shipVisual.transform.localScale = Vector3.one * 9.5f;

                var sr = shipVisual.AddComponent<SpriteRenderer>();
                sr.sprite = shipCardSpr;
                sr.color = new Color(0.96f, 0.98f, 1.0f, 1.0f);
            }

            // Waterline foam / gentle wave wake at the ship's base
            if (wakeSpr != null)
            {
                var shipWake = new GameObject("Waterline_WaveWake");
                shipWake.transform.SetParent(shipAnchor.transform, false);
                shipWake.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                shipWake.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Flat on water surface
                shipWake.transform.localScale = new Vector3(115.0f, 14.0f, 1.0f);

                var sr = shipWake.AddComponent<SpriteRenderer>();
                sr.sprite = wakeSpr;
                sr.color = new Color(1.0f, 1.0f, 1.0f, 0.45f);
            }

            // 3. Gliding Maritime Seagulls in the sky above the bay
            var gullSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Environment/Seagulls/Seagull_Frame_Glide.png");
            if (gullSpr != null)
            {
                var g1 = new GameObject("Maritime_Seagull_01");
                g1.transform.SetParent(root.transform, false);
                g1.transform.position = new Vector3(72.0f, 17.5f, -140.0f);
                g1.transform.rotation = Quaternion.Euler(-5f, 160f, 14f);
                g1.transform.localScale = new Vector3(2.2f, 2.2f, 1.0f);
                var sr1 = g1.AddComponent<SpriteRenderer>();
                sr1.sprite = gullSpr;
                sr1.color = new Color(0.96f, 0.96f, 0.98f, 0.90f);

                var g2 = new GameObject("Maritime_Seagull_02");
                g2.transform.SetParent(root.transform, false);
                g2.transform.position = new Vector3(36.0f, 20.5f, -170.0f);
                g2.transform.rotation = Quaternion.Euler(4f, 192f, -8f);
                g2.transform.localScale = new Vector3(1.7f, 1.7f, 1.0f);
                var sr2 = g2.AddComponent<SpriteRenderer>();
                sr2.sprite = gullSpr;
                sr2.color = new Color(0.92f, 0.94f, 0.96f, 0.85f);
            }

            // 4. Wooden Transfer Sampans (Xuồng Ba Lá Trung Chuyển)
            var xuongPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Xuong_go.prefab");
            if (xuongPrefab != null)
            {
                // Boat 1: Moored at east flank of pier
                var b1 = (GameObject)PrefabUtility.InstantiatePrefab(xuongPrefab, root.transform);
                b1.transform.position = new Vector3(60.2f, -0.32f, -32.5f);
                b1.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                b1.name = "Xuong_TrungChuyen_Moored";

                // Boat 2: Midway transit out to the big ship
                var b2 = (GameObject)PrefabUtility.InstantiatePrefab(xuongPrefab, root.transform);
                b2.transform.position = new Vector3(55.0f, -0.32f, -80.0f);
                b2.transform.rotation = Quaternion.Euler(0f, 182f, 0f);
                b2.name = "Xuong_TrungChuyen_RaTau";

                if (wakeSpr != null)
                {
                    var boatWake = new GameObject("Xuong_WaterWake");
                    boatWake.transform.SetParent(b2.transform, false);
                    boatWake.transform.localPosition = new Vector3(0f, 0.05f, 3.2f);
                    boatWake.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    boatWake.transform.localScale = new Vector3(3.5f, 7.0f, 1.0f);
                    var srWake = boatWake.AddComponent<SpriteRenderer>();
                    srWake.sprite = wakeSpr;
                    srWake.color = new Color(1.0f, 1.0f, 1.0f, 0.35f);
                }
            }

            // 5. Add Tap Ket Supplies (Crates, Rice Sacks, Barrels) onto the pier flanks
            var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_CargoCrate_3D.prefab");
            var sackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_RiceSacks.prefab");
            var barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_SaltFish_Barrel.prefab");

            // Pier Main Deck (North area)
            if (cratePrefab != null)
            {
                var c1 = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, root.transform);
                c1.transform.position = new Vector3(57.5f, 0.90f, -14.5f);
                c1.transform.rotation = Quaternion.Euler(0f, 25f, 0f);

                var c2 = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, root.transform);
                c2.transform.position = new Vector3(58.8f, 0.90f, -14.8f);
                c2.transform.rotation = Quaternion.Euler(0f, -10f, 0f);

                // Along Pier Walkway Railings (near tip Z = -35m to -42m)
                var c3 = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, root.transform);
                c3.transform.position = new Vector3(53.9f, 0.88f, -36.5f);
                c3.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                c3.transform.localScale = Vector3.one * 0.80f;

                var c4 = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, root.transform);
                c4.transform.position = new Vector3(58.1f, 0.88f, -38.0f);
                c4.transform.rotation = Quaternion.Euler(0f, -8f, 0f);
                c4.transform.localScale = Vector3.one * 0.85f;
            }

            if (sackPrefab != null)
            {
                var s1 = (GameObject)PrefabUtility.InstantiatePrefab(sackPrefab, root.transform);
                s1.transform.position = new Vector3(62.0f, 0.90f, -16.0f);
                s1.transform.rotation = Quaternion.Euler(0f, 40f, 0f);

                var s2 = (GameObject)PrefabUtility.InstantiatePrefab(sackPrefab, root.transform);
                s2.transform.position = new Vector3(53.9f, 0.88f, -38.2f);
                s2.transform.rotation = Quaternion.Euler(0f, 50f, 0f);

                var s3 = (GameObject)PrefabUtility.InstantiatePrefab(sackPrefab, root.transform);
                s3.transform.position = new Vector3(58.0f, 0.88f, -40.5f);
                s3.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
            }

            if (barrelPrefab != null)
            {
                var br1 = (GameObject)PrefabUtility.InstantiatePrefab(barrelPrefab, root.transform);
                br1.transform.position = new Vector3(61.5f, 0.90f, -13.5f);

                var br2 = (GameObject)PrefabUtility.InstantiatePrefab(barrelPrefab, root.transform);
                br2.transform.position = new Vector3(58.1f, 0.88f, -36.2f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("<color=green>[TapKetHarbor] Complete 1954 Tap Ket Harbor Atmosphere generated successfully!</color>");
        }

        [MenuItem("Tools/Hoa Tren Dat Do/3. Enhance Boat Sunset Cutscene (Shadow & Coastline)")]
        public static void EnhanceBoatSunsetCutscene()
        {
            var waterObj = GameObject.Find("Ocean_Water_Surface");
            if (waterObj == null)
            {
                Debug.LogWarning("[BoatSunset] Ocean_Water_Surface not found. Open Cutscene_BoatSunset scene first!");
                return;
            }

            // 1. Fix dark blue shadow on water -> warm sunset amber-mauve
            var mr = waterObj.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                // Deep warm alluvial sunset tone (replaces harsh cold blue)
                Color warmSunsetWater = new Color(0.32f, 0.18f, 0.13f, 1.0f);
                mr.sharedMaterial.SetColor("_BaseColor", warmSunsetWater);
                mr.sharedMaterial.SetColor("_Color", warmSunsetWater);
                mr.sharedMaterial.SetFloat("_Smoothness", 0.92f);
                EditorUtility.SetDirty(mr.sharedMaterial);
            }

            // 2. Adjust ambient shadow color to warm sunset mauve
            RenderSettings.subtractiveShadowColor = new Color(0.48f, 0.32f, 0.34f, 1.0f);
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.58f, 0.38f, 1.0f);

            // 3. Add Distant Coastline / Ca Mau Mangrove Silhouette & Farewell Sampan
            const string SunsetAdditiveName = "[Additive_Sunset_Atmosphere]";
            GameObject existing = GameObject.Find(SunsetAdditiveName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(SunsetAdditiveName);
            Undo.RegisterCreatedObjectUndo(root, "Create Sunset Atmosphere");

            // Distant Coastline Quad
            GameObject coastObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            coastObj.name = "Distant_CaMau_Coastline_Silhouette";
            coastObj.transform.SetParent(root.transform);
            coastObj.transform.position = new Vector3(4.0f, 0.85f, 36.0f);
            coastObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            coastObj.transform.localScale = new Vector3(65.0f, 4.2f, 1.0f);

            // Create soft silhouette material
            var coastMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            coastMat.color = new Color(0.28f, 0.18f, 0.18f, 0.85f);
            coastObj.GetComponent<MeshRenderer>().sharedMaterial = coastMat;
            Object.DestroyImmediate(coastObj.GetComponent<Collider>());

            // Farewell Sampan in midground
            var xuongPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Xuong_go.prefab");
            if (xuongPrefab != null)
            {
                var escortBoat = (GameObject)PrefabUtility.InstantiatePrefab(xuongPrefab, root.transform);
                escortBoat.transform.position = new Vector3(8.5f, -0.38f, 14.5f);
                escortBoat.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
                escortBoat.transform.localScale = Vector3.one * 1.05f;
                escortBoat.name = "XuongBaLa_TienBiet";
            }

            // Dynamic soaring birds in sunset
            var birdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Bird_Sparrow_Entity.prefab");
            if (birdPrefab != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    var bird = (GameObject)PrefabUtility.InstantiatePrefab(birdPrefab, root.transform);
                    bird.name = $"SunsetBird_{i}";
                    bird.transform.position = new Vector3(-8.0f + i * 4.5f, 5.5f + (i % 2) * 1.5f, 22.0f + i * 2.0f);
                    bird.transform.localScale = Vector3.one * (0.6f + i * 0.08f);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("<color=green>[BoatSunset] Enhanced water shadow color, added distant coastline, farewell sampan, and soaring birds!</color>");
        }
    }
}
