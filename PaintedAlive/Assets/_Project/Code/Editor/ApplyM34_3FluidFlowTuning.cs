#if UNITY_EDITOR
using PaintedAlive.Figures.StainSupport;
using UnityEditor;
using UnityEngine;

namespace PaintedAlive.Editor.Milestones
{
    public static class ApplyM34_3FluidFlowTuning
    {
        private const string MenuPath =
            "Tools/Painted Alive/Milestones/34.3 - Apply Fluid Flow Tuning";

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:StainWatercolorFlowConfig");

            if (guids.Length == 0)
            {
                Debug.LogError(
                    "M34.3 tuning could not find a StainWatercolorFlowConfig asset.");
                return;
            }

            int updatedCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StainWatercolorFlowConfig config =
                    AssetDatabase.LoadAssetAtPath<StainWatercolorFlowConfig>(path);

                if (config == null)
                {
                    continue;
                }

                Undo.RecordObject(config, "Apply M34.3 Fluid Flow Tuning");
                SerializedObject serializedConfig = new SerializedObject(config);

                SetFloat(serializedConfig, "detectionRadius", 0.72f);
                SetFloat(serializedConfig, "entryConfirmationDuration", 0.08f);
                SetFloat(serializedConfig, "minimumRideDuration", 0.32f);
                SetFloat(serializedConfig, "exitGraceDuration", 0.26f);
                SetFloat(serializedConfig, "reentryCooldown", 0.50f);

                SetFloat(serializedConfig, "fallbackFlowSpeed", 4.4f);
                SetFloat(serializedConfig, "minimumFlowSpeed", 2.6f);
                SetFloat(serializedConfig, "maximumFlowSpeed", 7.25f);
                SetFloat(serializedConfig, "velocityAcceleration", 18f);
                SetFloat(serializedConfig, "velocitySmoothTime", 0.20f);
                SetFloat(serializedConfig, "directionResponsiveness", 6.5f);
                SetFloat(serializedConfig, "entryTargetVelocityBlend", 0.25f);
                SetFloat(serializedConfig, "missingSampleDrag", 0.85f);
                SetFloat(serializedConfig, "steeringSpeed", 1.2f);

                SetFloat(serializedConfig, "surfaceOffset", 0.055f);
                SetFloat(serializedConfig, "surfaceAdhesionSpeed", 6.5f);
                SetFloat(serializedConfig, "surfaceNormalResponsiveness", 7f);
                SetFloat(serializedConfig, "exitNudge", 0.055f);
                SetFloat(serializedConfig, "exitGlideDuration", 0.20f);
                SetFloat(serializedConfig, "exitVelocityRetention", 0.42f);
                SetFloat(serializedConfig, "adapterRefreshInterval", 0.30f);

                serializedConfig.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);
                updatedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"M34.3 fluid flow tuning applied to {updatedCount} config asset(s). " +
                "No earlier milestone setup was rerun.");
        }

        private static void SetFloat(
            SerializedObject serializedObject,
            string propertyName,
            float value)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property != null)
            {
                property.floatValue = value;
            }
        }
    }
}
#endif
