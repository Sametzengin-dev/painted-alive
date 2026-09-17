#if UNITY_EDITOR
using PaintedAlive.Core.MatchFlow;
using PaintedAlive.Core.Playtests;
using PaintedAlive.Core.Prototypes;
using PaintedAlive.MatchFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupUiFocusAndMatchDuration_M55_9_5
    {
        private const float TargetMatchDurationSeconds = 900f;

        [MenuItem("Painted Alive/M55.9.5/Apply UI Focus + 15 Minute Match")]
        public static void Apply()
        {
            int updated = 0;

            foreach (PrototypeCoreMatchController controller in
                     Resources.FindObjectsOfTypeAll<PrototypeCoreMatchController>())
            {
                if (controller == null || EditorUtility.IsPersistent(controller.gameObject))
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(controller);
                SerializedProperty roundSeconds = serialized.FindProperty("roundSeconds");
                if (roundSeconds != null)
                {
                    roundSeconds.floatValue = TargetMatchDurationSeconds;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(controller);
                    updated++;
                }
            }

            foreach (PrototypeCoreMatchHud hud in
                     Resources.FindObjectsOfTypeAll<PrototypeCoreMatchHud>())
            {
                if (hud == null || EditorUtility.IsPersistent(hud.gameObject))
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(hud);
                SerializedProperty hideWhenUnified =
                    serialized.FindProperty("hideWhenUnifiedHudPresent");
                if (hideWhenUnified != null)
                {
                    hideWhenUnified.boolValue = true;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(hud);
                    updated++;
                }
            }

            updated += UpdateAssetType<PrototypeMatchConfig>("matchDuration", TargetMatchDurationSeconds);
            updated += UpdateAssetType<PrototypeExpeditionMatchConfig>("activeDuration", TargetMatchDurationSeconds);
            updated += UpdateAssetType<PrototypeOneVsOnePlaytestConfig>("expectedMatchDuration", TargetMatchDurationSeconds);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[M55.9.5] UI focus + 15 minute match applied. " +
                $"Updated records: {updated}.");
        }

        private static int UpdateAssetType<T>(
            string fieldName,
            float value)
            where T : Object
        {
            int updated = 0;
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty property = serialized.FindProperty(fieldName);
                if (property == null)
                {
                    continue;
                }

                property.floatValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                updated++;
            }

            return updated;
        }
    }
}
#endif
