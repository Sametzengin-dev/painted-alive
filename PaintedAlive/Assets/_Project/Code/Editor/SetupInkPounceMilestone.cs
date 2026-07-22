using System;
using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink;
using PaintedAlive.Paint.Ink.Combat;
using PaintedAlive.Paint.Ink.Possession;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupInkPounceMilestone
    {
        private const string RootFolder = "Assets/_Project";
        private const string InkDataFolder =
            RootFolder + "/Data/Paint/Ink";
        private const string ConfigPath =
            InkDataFolder + "/InkPounceAttackConfig.asset";
        private const string FootprintMaterialPath =
            RootFolder + "/Materials/Paint/Ink/M_InkFootprint.mat";

        [MenuItem(
            "Tools/Painted Alive/Milestones/18 - Setup Ink Pounce Combat")]
        public static void Setup()
        {
            try
            {
                ValidatePrerequisites();
                EnsureFolder(InkDataFolder);
                EnsureFolder(RootFolder + "/Materials/Paint/Ink");

                Material footprintMaterial =
                    GetOrCreateFootprintMaterial();
                InkPounceAttackConfig config =
                    GetOrCreateConfig(footprintMaterial);
                InkSystemManager manager =
                    ResolveSingleInkManager();
                InkPounceCombatDirector director =
                    ConfigureDirector(manager, config);
                InkPossessionAttackInput attackInput =
                    ConfigurePossessionInput();
                int configuredFigures = ConfigureFigures(config);
                DisableLegacyContactDamage(manager);

                EditorUtility.SetDirty(config);
                EditorUtility.SetDirty(director);

                if (attackInput != null)
                {
                    EditorUtility.SetDirty(attackInput);
                }

                EditorSceneManager.MarkSceneDirty(
                    manager.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M18 Setup] Lekebacak pounce combat ready. " +
                    $"Config={ConfigPath}, " +
                    $"Director={GetHierarchyPath(director.transform)}, " +
                    $"PossessionInput={(attackInput != null)}, " +
                    $"ConfiguredFigures={configuredFigures}, " +
                    "LegacyContactDamage=0. Scene was not saved " +
                    "automatically. Press Ctrl+S. AI attacks on approach; " +
                    "while possessing use Left Mouse or Space.",
                    director);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/18 - Diagnose Ink Pounce Combat")]
        public static void Diagnose()
        {
            InkPounceCombatDirector[] directors =
                UnityEngine.Object.FindObjectsByType<
                    InkPounceCombatDirector>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPossessionAttackInput[] inputs =
                UnityEngine.Object.FindObjectsByType<
                    InkPossessionAttackInput>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            FigureInkFootStainStatus[] stains =
                UnityEngine.Object.FindObjectsByType<
                    FigureInkFootStainStatus>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int configCount = AssetDatabase.FindAssets(
                "t:InkPounceAttackConfig").Length;
            int stainedFigures = 0;
            int totalStacks = 0;

            for (int i = 0; i < stains.Length; i++)
            {
                if (stains[i].IsStained)
                {
                    stainedFigures++;
                    totalStacks += stains[i].StackCount;
                }
            }

            float legacyContactDamage = ReadLegacyContactDamage();
            int duplicates = Mathf.Max(0, directors.Length - 1) +
                Mathf.Max(0, inputs.Length - 1);

            Debug.Log(
                "[M18 Diagnose] " +
                $"AttackConfigs={configCount}, " +
                $"CombatDirectors={directors.Length}, " +
                $"PossessionAttackInputs={inputs.Length}, " +
                $"FigureStainStatuses={stains.Length}, " +
                $"StainedFigures={stainedFigures}, " +
                $"TotalStainStacks={totalStacks}, " +
                $"LegacyContactDamage={legacyContactDamage:F2}, " +
                $"DuplicateComponents={duplicates}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < directors.Length; i++)
            {
                InkPounceCombatDirector director = directors[i];
                Debug.Log(
                    "[M18 Diagnose Director] " +
                    $"Path={GetHierarchyPath(director.transform)}, " +
                    $"TrackedCreatures={director.TrackedCreatures}, " +
                    $"ActiveAttacks={director.ActiveAttacks}, " +
                    $"TotalAttacks={director.TotalAttacks}, " +
                    $"Hits={director.TotalHits}, " +
                    $"Misses={director.TotalMisses}, " +
                    $"LastResult={director.LastAttackResult}, " +
                    $"States={director.BuildRuntimeSummary()}",
                    director);
            }

            for (int i = 0; i < stains.Length; i++)
            {
                FigureInkFootStainStatus stain = stains[i];
                Debug.Log(
                    "[M18 Diagnose Stain] " +
                    $"Path={GetHierarchyPath(stain.transform)}, " +
                    $"Stacks={stain.StackCount}, " +
                    $"Remaining={stain.RemainingDuration:F2}, " +
                    $"Footprints={stain.FootprintsPlaced}",
                    stain);
            }

            for (int i = 0; i < inputs.Length; i++)
            {
                InkPossessionAttackInput input = inputs[i];
                Debug.Log(
                    "[M18 Diagnose Possession Input] " +
                    $"Path={GetHierarchyPath(input.transform)}, " +
                    $"Controller={GetObjectName(input.PossessionController)}, " +
                    $"LastInput={input.LastInputResult}",
                    input);
            }
        }

        private static void ValidatePrerequisites()
        {
            if (AssetDatabase.LoadAssetAtPath<InkSystemConfig>(
                    InkDataFolder + "/InkSystemConfig.asset") == null)
            {
                throw new InvalidOperationException(
                    "M15 InkSystemConfig bulunamadı. Önce çalışan M15 " +
                    "kurulumunu tamamla.");
            }

            InkPossessionController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    InkPossessionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (controllers.Length != 1)
            {
                throw new InvalidOperationException(
                    "M18 tam olarak bir InkPossessionController bekliyor. " +
                    "Önce M17 Diagnose sonucunu düzelt.");
            }

            if (typeof(InkCreatureRuntime).GetMethod("TryDamageGlyph") ==
                null)
            {
                throw new InvalidOperationException(
                    "M16 InkCreatureRuntime yükseltmesi bulunamadı.");
            }
        }

        private static InkPounceAttackConfig GetOrCreateConfig(
            Material footprintMaterial)
        {
            InkPounceAttackConfig config =
                AssetDatabase.LoadAssetAtPath<InkPounceAttackConfig>(
                    ConfigPath);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<
                    InkPounceAttackConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty materialProperty =
                serialized.FindProperty("footprintMaterial");

            if (materialProperty != null)
            {
                materialProperty.objectReferenceValue = footprintMaterial;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static Material GetOrCreateFootprintMaterial()
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    FootprintMaterialPath);

            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            shader ??= Shader.Find("Standard");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "URP Lit veya Standard shader bulunamadı.");
            }

            material = new Material(shader)
            {
                name = "M_InkFootprint"
            };
            Color inkColor = new Color(0.012f, 0.008f, 0.018f, 1f);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", inkColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", inkColor);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.72f);
            }

            AssetDatabase.CreateAsset(material, FootprintMaterialPath);
            return material;
        }

        private static InkSystemManager ResolveSingleInkManager()
        {
            InkSystemManager[] managers =
                UnityEngine.Object.FindObjectsByType<InkSystemManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (managers.Length != 1)
            {
                throw new InvalidOperationException(
                    $"M18 tam olarak bir InkSystemManager bekliyor; " +
                    $"bulunan={managers.Length}.");
            }

            return managers[0];
        }

        private static InkPounceCombatDirector ConfigureDirector(
            InkSystemManager manager,
            InkPounceAttackConfig config)
        {
            InkPounceCombatDirector[] directors =
                UnityEngine.Object.FindObjectsByType<
                    InkPounceCombatDirector>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPounceCombatDirector director = directors.Length > 0
                ? directors[0]
                : Undo.AddComponent<InkPounceCombatDirector>(
                    manager.gameObject);

            for (int i = 1; i < directors.Length; i++)
            {
                Undo.DestroyObjectImmediate(directors[i]);
            }

            SerializedObject serialized = new SerializedObject(director);
            SetReference(serialized, "config", config);
            serialized.ApplyModifiedProperties();
            director.enabled = true;
            return director;
        }

        private static InkPossessionAttackInput ConfigurePossessionInput()
        {
            InkPossessionController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    InkPossessionController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPossessionController controller = controllers[0];
            InkPossessionAttackInput input =
                controller.GetComponent<InkPossessionAttackInput>();

            if (input == null)
            {
                input = Undo.AddComponent<InkPossessionAttackInput>(
                    controller.gameObject);
            }

            SerializedObject serialized = new SerializedObject(input);
            SetReference(
                serialized,
                "possessionController",
                controller);
            serialized.ApplyModifiedProperties();
            input.enabled = true;
            return input;
        }

        private static int ConfigureFigures(
            InkPounceAttackConfig config)
        {
            FigureMotor[] figures =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int i = 0; i < figures.Length; i++)
            {
                FigureInkFootStainStatus status =
                    figures[i].GetComponent<FigureInkFootStainStatus>();

                if (status == null)
                {
                    status = Undo.AddComponent<FigureInkFootStainStatus>(
                        figures[i].gameObject);
                }

                SerializedObject serialized = new SerializedObject(status);
                SetReference(serialized, "config", config);
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(status);
            }

            return figures.Length;
        }

        private static void DisableLegacyContactDamage(
            InkSystemManager manager)
        {
            InkSystemConfig inkConfig = manager.Config;

            if (inkConfig == null)
            {
                throw new InvalidOperationException(
                    "InkSystemManager config reference is missing.");
            }

            SerializedObject serialized = new SerializedObject(inkConfig);
            SerializedProperty property = serialized.FindProperty(
                "clarityExposurePerContact");

            if (property == null)
            {
                throw new MissingFieldException(
                    "InkSystemConfig.clarityExposurePerContact not found.");
            }

            property.floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(inkConfig);
        }

        private static float ReadLegacyContactDamage()
        {
            InkSystemConfig config =
                AssetDatabase.LoadAssetAtPath<InkSystemConfig>(
                    InkDataFolder + "/InkSystemConfig.asset");

            if (config == null)
            {
                return -1f;
            }

            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty property = serialized.FindProperty(
                "clarityExposurePerContact");
            return property != null ? property.floatValue : -1f;
        }

        private static void SetReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                throw new MissingFieldException(
                    serialized.targetObject.GetType().Name,
                    propertyName);
            }

            property.objectReferenceValue = value;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return "Yok";
            }

            string path = target.name;

            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }

        private static string GetObjectName(UnityEngine.Object target)
        {
            return target != null ? target.name : "Yok";
        }
    }
}
