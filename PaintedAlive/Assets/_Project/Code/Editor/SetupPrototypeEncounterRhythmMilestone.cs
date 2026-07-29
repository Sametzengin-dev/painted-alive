#if UNITY_EDITOR
using System;
using PaintedAlive.Core.Encounters;
using PaintedAlive.Core.Prototypes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools
{
    public static class SetupPrototypeEncounterRhythmMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Core/DA_PrototypeEncounterRhythm.asset";
        private const string MarkerRootName = "M39_EncounterMarkers";
        private const string MaterialPath =
            "Assets/_Project/Materials/Prototype/MAT_M39_EncounterMarker.mat";

        [MenuItem("Tools/Painted Alive/Milestones/39 - Setup Encounter Rhythm Director")]
        public static void Setup()
        {
            try
            {
                PrototypeMatchController match =
                    FindRequiredSceneObject<PrototypeMatchController>(
                        "PrototypeMatchController");
                FigureProgressTracker progress =
                    FindRequiredSceneObject<FigureProgressTracker>(
                        "FigureProgressTracker");
                RoutePath route =
                    FindRequiredSceneObject<RoutePath>(
                        "RoutePath");

                PrototypeEncounterRhythmConfig config = CreateOrLoadConfig();

                PrototypeEncounterRhythmDirector director =
                    GetOrAddComponent<PrototypeEncounterRhythmDirector>(
                        match.gameObject);
                director.Configure(match, progress, config);

                PrototypeEncounterRhythmHUD hud =
                    GetOrAddComponent<PrototypeEncounterRhythmHUD>(
                        match.gameObject);
                hud.Configure(director);

                PrototypeEncounterRhythmLedger ledger =
                    GetOrAddComponent<PrototypeEncounterRhythmLedger>(
                        match.gameObject);

                int markerCount = CreateOrRefreshMarkers(route);

                EditorUtility.SetDirty(director);
                EditorUtility.SetDirty(hud);
                EditorUtility.SetDirty(ledger);
                EditorUtility.SetDirty(match.gameObject);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();

                Debug.Log(
                    "[M39 Setup] Tamamlandı.\n" +
                    $"Match={GetHierarchyPath(match.transform)}\n" +
                    $"Progress={GetHierarchyPath(progress.transform)}\n" +
                    $"Route={GetHierarchyPath(route.transform)}\n" +
                    $"Director={GetHierarchyPath(director.transform)}\n" +
                    $"Markers={markerCount}\n" +
                    "Existing PrototypeMatchController remains the only match authority.",
                    director);

                EditorUtility.DisplayDialog(
                    "PAINTED ALIVE — M39",
                    "Encounter ritim katmanı mevcut ana maç döngüsüne eklendi.\n\n" +
                    "3 rota düğümü\n" +
                    "Okuma → hafif baskı → araç karşılığı → kombinasyon → kaçış → nefes\n" +
                    "Final çerçeve koşusu\n\n" +
                    "Bu sistem yeni timer, reset veya input otoritesi oluşturmaz.\n" +
                    "Sahneyi Ctrl+S ile kaydet.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M39 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/39 - Diagnose Encounter Rhythm Director")]
        public static void Diagnose()
        {
            PrototypeMatchController match =
                FindFirstSceneObject<PrototypeMatchController>();
            FigureProgressTracker progress =
                FindFirstSceneObject<FigureProgressTracker>();
            PrototypeEncounterRhythmDirector director =
                FindFirstSceneObject<PrototypeEncounterRhythmDirector>();
            PrototypeEncounterRhythmHUD hud =
                FindFirstSceneObject<PrototypeEncounterRhythmHUD>();
            PrototypeEncounterRhythmLedger ledger =
                FindFirstSceneObject<PrototypeEncounterRhythmLedger>();
            RoutePath route = FindFirstSceneObject<RoutePath>();

            int directorCount = CountSceneObjects<PrototypeEncounterRhythmDirector>();
            int markerCount = CountMarkers(route);
            PrototypeEncounterTransitionRecord last =
                ledger != null ? ledger.LastRecord : default;

            Debug.Log(
                "[M39 Diagnose]\n" +
                $"PrototypeMatchController={(match != null ? GetHierarchyPath(match.transform) : "MISSING")}\n" +
                $"FigureProgressTracker={(progress != null ? "OK" : "MISSING")}\n" +
                $"RoutePath={(route != null ? "OK" : "MISSING")}\n" +
                $"Director={(director != null ? "OK" : "MISSING")}\n" +
                $"DirectorCount={directorCount}\n" +
                $"HUD={(hud != null ? "OK" : "MISSING")}\n" +
                $"Ledger={(ledger != null ? "OK" : "MISSING")}\n" +
                $"Markers={markerCount}\n" +
                $"MatchState={(match != null ? match.State.ToString() : "Unknown")}\n" +
                $"RouteProgress={(progress != null ? progress.NormalizedProgress : 0f):P1}\n" +
                $"Encounter={(director != null ? director.CurrentEncounterIndex : 0)}\n" +
                $"Phase={(director != null ? director.CurrentPhase.ToString() : "Unknown")}\n" +
                $"Pressure={(director != null ? director.CurrentPressure01 : 0f):0.00}\n" +
                $"Transitions={(director != null ? director.TransitionCount : 0)}\n" +
                $"LedgerRecords={(ledger != null ? ledger.RecordCount : 0)}\n" +
                $"LastLedgerPhase={last.phase}");
        }

        private static PrototypeEncounterRhythmConfig CreateOrLoadConfig()
        {
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Core");

            PrototypeEncounterRhythmConfig config =
                AssetDatabase.LoadAssetAtPath<PrototypeEncounterRhythmConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<PrototypeEncounterRhythmConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static int CreateOrRefreshMarkers(RoutePath route)
        {
            Transform root = route.transform.Find(MarkerRootName);
            if (root == null)
            {
                GameObject rootObject = new GameObject(MarkerRootName);
                Undo.RegisterCreatedObjectUndo(rootObject, "Create M39 encounter markers");
                rootObject.transform.SetParent(route.transform, false);
                root = rootObject.transform;
            }

            while (root.childCount > 0)
            {
                Undo.DestroyObjectImmediate(root.GetChild(0).gameObject);
            }

            Transform[] waypoints = ReadWaypoints(route);
            Material material = CreateOrLoadMarkerMaterial();

            for (int i = 0; i < 3; i++)
            {
                Vector3 position = ResolveMarkerPosition(route, waypoints, i);
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(marker, "Create M39 encounter marker");
                marker.name = $"M39_EncounterNode_{i + 1:00}";
                marker.transform.SetParent(root, true);
                marker.transform.position = position + Vector3.up * 0.035f;
                marker.transform.rotation = Quaternion.identity;
                marker.transform.localScale = new Vector3(8f, 0.07f, 0.45f);

                Collider collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    Undo.DestroyObjectImmediate(collider);
                }

                Renderer renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                }
            }

            EditorUtility.SetDirty(root.gameObject);
            return root.childCount;
        }

        private static Transform[] ReadWaypoints(RoutePath route)
        {
            SerializedObject serializedRoute = new SerializedObject(route);
            SerializedProperty waypointsProperty = serializedRoute.FindProperty("waypoints");
            if (waypointsProperty == null || !waypointsProperty.isArray)
            {
                return Array.Empty<Transform>();
            }

            Transform[] waypoints = new Transform[waypointsProperty.arraySize];
            for (int i = 0; i < waypoints.Length; i++)
            {
                waypoints[i] = waypointsProperty
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue as Transform;
            }

            return waypoints;
        }

        private static Vector3 ResolveMarkerPosition(
            RoutePath route,
            Transform[] waypoints,
            int markerIndex)
        {
            int preferredIndex = markerIndex + 1;
            if (waypoints.Length > preferredIndex &&
                waypoints[preferredIndex] != null)
            {
                return waypoints[preferredIndex].position;
            }

            Transform start = waypoints.Length > 0 ? waypoints[0] : null;
            Transform finish = waypoints.Length > 1
                ? waypoints[waypoints.Length - 1]
                : null;

            if (start != null && finish != null)
            {
                float t = (markerIndex + 1f) / 4f;
                return Vector3.Lerp(start.position, finish.position, t);
            }

            return route.transform.position +
                route.transform.forward * ((markerIndex + 1) * 15f);
        }

        private static Material CreateOrLoadMarkerMaterial()
        {
            EnsureFolder("Assets/_Project/Materials");
            EnsureFolder("Assets/_Project/Materials/Prototype");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = "MAT_M39_EncounterMarker"
            };
            material.color = new Color(0.10f, 0.78f, 0.82f, 1f);
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.35f);
            }

            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static int CountMarkers(RoutePath route)
        {
            if (route == null)
            {
                return 0;
            }

            Transform root = route.transform.Find(MarkerRootName);
            return root != null ? root.childCount : 0;
        }

        private static int CountSceneObjects<T>() where T : Component
        {
            T[] objects = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null &&
                    !EditorUtility.IsPersistent(objects[i]) &&
                    objects[i].gameObject.scene.IsValid())
                {
                    count++;
                }
            }

            return count;
        }

        private static T FindRequiredSceneObject<T>(string label)
            where T : UnityEngine.Object
        {
            T found = FindFirstSceneObject<T>();
            if (found == null)
            {
                throw new InvalidOperationException(
                    $"M39 {label} bulamadı. M38.1'in çalıştığı ana sahneyi aç ve " +
                    "mevcut MatchSystems/Route yapısının sahnede olduğundan emin ol.");
            }

            return found;
        }

        private static T FindFirstSceneObject<T>() where T : UnityEngine.Object
        {
            T[] objects = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < objects.Length; i++)
            {
                T candidate = objects[i];
                if (candidate == null || EditorUtility.IsPersistent(candidate))
                {
                    continue;
                }

                if (candidate is Component component && component.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "None";
            }

            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
#endif
