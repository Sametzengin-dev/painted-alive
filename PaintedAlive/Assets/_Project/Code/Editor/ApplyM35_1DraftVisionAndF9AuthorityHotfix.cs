#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PaintedAlive.Figures.StainSupport.DraftVision;
using PaintedAlive.Painters.Authority;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools
{
    public static class ApplyM35_1DraftVisionAndF9AuthorityHotfix
    {
        [MenuItem("Tools/Painted Alive/Milestones/35.1 - Apply Draft Vision and F9 Authority Hotfix")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("M35.1 hotfix yalnız Play Mode kapalıyken uygulanabilir.");
                return;
            }

            StainEarlyDraftVisionController vision =
                UnityEngine.Object.FindFirstObjectByType<StainEarlyDraftVisionController>(
                    FindObjectsInactive.Include);

            StainEarlyDraftVisionDebugEmitter emitter =
                UnityEngine.Object.FindFirstObjectByType<StainEarlyDraftVisionDebugEmitter>(
                    FindObjectsInactive.Include);

            if (vision == null || emitter == null)
            {
                Debug.LogError(
                    "[M35.1] M35 controller veya F10 debug emitter bulunamadı. " +
                    "Önce M35'in daha önce kurulmuş olduğu ana sahneyi aç.");
                return;
            }

            MonoBehaviour roleAuthority = FindBestRoleAuthority();
            Behaviour[] f9Spawners = FindF9InkDebugSpawners();
            GameObject[] painterRoleObjects = FindPainterRoleObjects();

            GameObject gateHost = roleAuthority != null
                ? roleAuthority.gameObject
                : vision.gameObject;

            InkDebugSpawnerPainterOnlyGate gate =
                gateHost.GetComponent<InkDebugSpawnerPainterOnlyGate>();

            if (gate == null)
            {
                gate = Undo.AddComponent<InkDebugSpawnerPainterOnlyGate>(gateHost);
            }

            Undo.RecordObject(gate, "Configure M35.1 F9 Painter Authority");
            gate.Configure(roleAuthority, f9Spawners, painterRoleObjects);
            EditorUtility.SetDirty(gate);

            RepairRoleBehaviourLists(f9Spawners);

            MarkDirty(gateHost.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = gateHost;

            string spawnerSummary = f9Spawners.Length > 0
                ? JoinBehaviourNames(f9Spawners)
                : "NONE";

            Debug.Log(
                "[M35.1] Hotfix uygulandı. " +
                $"RoleAuthority={(roleAuthority != null ? roleAuthority.GetType().Name : "Fallback")}, " +
                $"F9Spawners={f9Spawners.Length} [{spawnerSummary}]. " +
                "F10 engel kontrolü düzeltildi; F9 yalnız Painter rolünde etkinleştirilecek. " +
                "M15-M35 ana Setup menülerini yeniden çalıştırma.",
                gate);

            if (f9Spawners.Length == 0)
            {
                Debug.LogWarning(
                    "[M35.1] F9 kullanan Mürekkep debug spawner kaynak metninden bulunamadı. " +
                    "35.1 Diagnose menüsünü çalıştır; Console'daki aday tip adlarını gönder.",
                    gate);
            }
        }

        [MenuItem("Tools/Painted Alive/Milestones/35.1 - Diagnose Draft Vision and F9 Authority")]
        public static void Diagnose()
        {
            StainEarlyDraftVisionController vision =
                UnityEngine.Object.FindFirstObjectByType<StainEarlyDraftVisionController>(
                    FindObjectsInactive.Include);

            StainEarlyDraftVisionDebugEmitter emitter =
                UnityEngine.Object.FindFirstObjectByType<StainEarlyDraftVisionDebugEmitter>(
                    FindObjectsInactive.Include);

            InkDebugSpawnerPainterOnlyGate gate =
                UnityEngine.Object.FindFirstObjectByType<InkDebugSpawnerPainterOnlyGate>(
                    FindObjectsInactive.Include);

            Behaviour[] spawners = FindF9InkDebugSpawners();
            MonoBehaviour authority = FindBestRoleAuthority();

            Debug.Log(
                "[M35.1 Diagnose] " +
                $"Vision={(vision != null ? vision.name : "Missing")}, " +
                $"VisionEnabled={(vision != null && vision.isActiveAndEnabled)}, " +
                $"Emitter={(emitter != null ? emitter.name : "Missing")}, " +
                $"EmitterEnabled={(emitter != null && emitter.isActiveAndEnabled)}, " +
                $"RoleAuthority={(authority != null ? authority.GetType().Name : "Missing")}, " +
                $"Gate={(gate != null ? gate.name : "Missing")}, " +
                $"PainterRole={(gate != null && gate.IsPainterRole)}, " +
                $"ControlledSpawners={(gate != null ? gate.ControlledSpawnerCount : 0)}, " +
                $"DetectedF9Spawners={spawners.Length} [{JoinBehaviourNames(spawners)}].");

            LogInkSpawnerCandidates();
        }

        private static MonoBehaviour FindBestRoleAuthority()
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            MonoBehaviour best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || !behaviour.gameObject.scene.IsValid())
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                int score = int.MinValue;

                if (typeName.Equals("InkPainterRoleAuthority", StringComparison.OrdinalIgnoreCase))
                {
                    score = 300;
                }
                else if (typeName.IndexOf("RoleAuthority", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score = 220;
                }
                else if (typeName.IndexOf("RoleSwitcher", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score = 180;
                }

                if (score == int.MinValue)
                {
                    continue;
                }

                score += behaviour.gameObject.activeInHierarchy ? 10 : 0;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = behaviour;
                }
            }

            return best;
        }

        private static Behaviour[] FindF9InkDebugSpawners()
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            var result = new List<Behaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null ||
                    behaviour is InkDebugSpawnerPainterOnlyGate ||
                    !behaviour.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (UsesF9ForInkDebugSpawn(behaviour))
                {
                    result.Add(behaviour);
                }
            }

            return result.ToArray();
        }

        private static bool UsesF9ForInkDebugSpawn(MonoBehaviour behaviour)
        {
            string typeName = behaviour.GetType().Name;
            string objectName = behaviour.gameObject.name;

            if (typeName.IndexOf("Watercolor", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
            string source = script != null ? script.text : string.Empty;

            bool referencesF9 =
                source.IndexOf("f9Key", StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf("Key.F9", StringComparison.OrdinalIgnoreCase) >= 0 ||
                source.IndexOf("Keyboard.current.f9", StringComparison.OrdinalIgnoreCase) >= 0;

            bool referencesInk =
                source.IndexOf("Ink", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Ink", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("Ink", StringComparison.OrdinalIgnoreCase) >= 0;

            bool referencesSpawn =
                source.IndexOf("Spawn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Spawner", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0;

            if (referencesF9 && referencesInk && referencesSpawn)
            {
                return true;
            }

            // Fallback for packages whose source text is unavailable to MonoScript.
            return typeName.IndexOf("Ink", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   (typeName.IndexOf("DebugSpawner", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("TestSpawner", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static GameObject[] FindPainterRoleObjects()
        {
            Transform[] transforms =
                UnityEngine.Object.FindObjectsByType<Transform>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            var result = new List<GameObject>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform == null || !transform.gameObject.scene.IsValid())
                {
                    continue;
                }

                string objectName = transform.name;
                bool painterNamed = objectName.IndexOf(
                    "Painter",
                    StringComparison.OrdinalIgnoreCase) >= 0;

                bool cameraLike =
                    objectName.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    objectName.StartsWith("CM_", StringComparison.OrdinalIgnoreCase);

                if (painterNamed && cameraLike)
                {
                    result.Add(transform.gameObject);
                }
            }

            return result.ToArray();
        }

        private static void RepairRoleBehaviourLists(Behaviour[] f9Spawners)
        {
            if (f9Spawners == null || f9Spawners.Length == 0)
            {
                return;
            }

            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour roleComponent = behaviours[i];
                if (roleComponent == null)
                {
                    continue;
                }

                string typeName = roleComponent.GetType().Name;
                if (typeName.IndexOf("RoleSwitcher", StringComparison.OrdinalIgnoreCase) < 0 &&
                    typeName.IndexOf("RoleAuthority", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(roleComponent);
                SerializedProperty figureArray = serialized.FindProperty("figureBehaviours");
                SerializedProperty painterArray = serialized.FindProperty("painterBehaviours");
                bool changed = false;

                if (figureArray != null && figureArray.isArray)
                {
                    changed |= RemoveReferences(figureArray, f9Spawners);
                }

                if (painterArray != null && painterArray.isArray)
                {
                    for (int j = 0; j < f9Spawners.Length; j++)
                    {
                        changed |= AppendUnique(painterArray, f9Spawners[j]);
                    }
                }

                if (changed)
                {
                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(roleComponent);
                }
            }
        }

        private static bool RemoveReferences(
            SerializedProperty array,
            Behaviour[] values)
        {
            bool changed = false;
            for (int i = array.arraySize - 1; i >= 0; i--)
            {
                UnityEngine.Object current =
                    array.GetArrayElementAtIndex(i).objectReferenceValue;

                for (int j = 0; j < values.Length; j++)
                {
                    if (current != values[j])
                    {
                        continue;
                    }

                    array.DeleteArrayElementAtIndex(i);
                    if (i < array.arraySize &&
                        array.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        array.DeleteArrayElementAtIndex(i);
                    }

                    changed = true;
                    break;
                }
            }

            return changed;
        }

        private static bool AppendUnique(
            SerializedProperty array,
            UnityEngine.Object value)
        {
            if (value == null)
            {
                return false;
            }

            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                {
                    return false;
                }
            }

            int index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            array.GetArrayElementAtIndex(index).objectReferenceValue = value;
            return true;
        }

        private static void LogInkSpawnerCandidates()
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (typeName.IndexOf("Ink", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (typeName.IndexOf("Spawn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     typeName.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Debug.Log(
                        "[M35.1 Diagnose Candidate] " +
                        $"Type={typeName}, Object={behaviour.gameObject.name}, " +
                        $"Enabled={behaviour.enabled}, Active={behaviour.gameObject.activeInHierarchy}",
                        behaviour);
                }
            }
        }

        private static string JoinBehaviourNames(Behaviour[] behaviours)
        {
            if (behaviours == null || behaviours.Length == 0)
            {
                return string.Empty;
            }

            var names = new string[behaviours.Length];
            for (int i = 0; i < behaviours.Length; i++)
            {
                names[i] = behaviours[i] != null
                    ? behaviours[i].GetType().Name
                    : "Missing";
            }

            return string.Join(", ", names);
        }

        private static void MarkDirty(Scene scene)
        {
            if (scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
#endif
