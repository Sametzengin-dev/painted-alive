#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PaintedAlive.Figures;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.EditorTools.Presentation
{
    /// <summary>
    /// Final presentation pass for authored oil geometry and Figure tools.
    /// The operation is deliberately idempotent and presentation-only.
    /// </summary>
    public static class SetupFinalGameplayMaterials_M58E3
    {
        private const string ImpastoShaderName =
            "PaintedAlive/M58 Final/Impasto";
        private const string TelegraphShaderName =
            "PaintedAlive/M58 Final/Stroke Telegraph";
        private const string SpongeShaderName =
            "PaintedAlive/M58 Final/Tactile Sponge";
        private const string RopeShaderName =
            "PaintedAlive/M58 Final/Rope Fiber";

        private const string WetOilPath =
            "Assets/_Project/Materials/Paint/MAT_Oil_Wet.mat";
        private const string DryOilPath =
            "Assets/_Project/Materials/Paint/MAT_Oil_Dry.mat";
        private const string PreviewPath =
            "Assets/_Project/Materials/Paint/MAT_Oil_StrokePreview.mat";
        private const string SpongePath =
            "Assets/_Project/Art/Materials/Tools/M_SpongePrototype.mat";
        private const string AnchorPath =
            "Assets/_Project/Art/Materials/Tools/M_FrameGunAnchor.mat";
        private const string RopePath =
            "Assets/_Project/Art/Materials/Tools/M_FrameGunRope.mat";
        private const string KnifePath =
            "Assets/_Project/Art/Materials/Figures/Tools/PaletteKnife/" +
            "MAT_PaletteKnife_Production.mat";

        private const string OilParticlePath =
            "Assets/_Project/Art/Materials/VFX/M_OilImpactParticles.mat";
        private const string FixativeParticlePath =
            "Assets/_Project/Art/Materials/VFX/M_FixativeParticles.mat";
        private const string SpongeLiquidPath =
            "Assets/_Project/Art/Materials/VFX/M_SpongeLiquidVFX.mat";
        private const string SpongeBurstPath =
            "Assets/_Project/Art/Materials/VFX/M_SpongeBurstVFX.mat";
        private const string FrameImpactPath =
            "Assets/_Project/Art/Materials/VFX/" +
            "M_FrameGunImpactParticles.mat";

        private const string OilCutPrefabPath =
            "Assets/_Project/Prefabs/VFX/Paint/VFX_OilKnifeCut.prefab";
        private const string OilCreationPrefabPath =
            "Assets/_Project/Prefabs/VFX/Paint/" +
            "VFX_OilStrokeCreation_M58Final.prefab";
        private const string OilFracturePrefabPath =
            "Assets/_Project/Prefabs/VFX/Paint/VFX_OilFracture.prefab";
        private const string FixativeSprayPrefabPath =
            "Assets/_Project/Prefabs/VFX/Paint/VFX_FixativeSpray.prefab";
        private const string FixativeImpactPrefabPath =
            "Assets/_Project/Prefabs/VFX/Paint/VFX_FixativeImpact.prefab";
        private const string SpongeLiquidPrefabPath =
            "Assets/_Project/Prefabs/VFX/Tools/VFX_SpongeLiquid.prefab";
        private const string SpongeBurstPrefabPath =
            "Assets/_Project/Prefabs/VFX/Tools/VFX_SpongeBurst.prefab";
        private const string FrameImpactPrefabPath =
            "Assets/_Project/Prefabs/VFX/Tools/VFX_FrameGunImpact.prefab";

        private const string GrayGroundPath =
            "Assets/_Project/Materials/Environment/MAT_Graybox_Ground.mat";
        private const string GrayObstaclePath =
            "Assets/_Project/Materials/Environment/MAT_Graybox_Obstacle.mat";
        private const string FinalGroundPath =
            "Assets/_Project/Materials/Environment/AtelierTest/" +
            "MAT_Atelier_CanvasLimestone.mat";
        private const string FinalObstaclePath =
            "Assets/_Project/Materials/Environment/AtelierTest/" +
            "MAT_Atelier_HandLaidStone.mat";

        private const string SecondaryParticleName =
            "M58_FinalSecondary";

        private enum ParticleProfile
        {
            OilCreation,
            OilCut,
            OilFracture,
            FixativeSpray,
            FixativeImpact,
            SpongeLiquid,
            SpongeBurst,
            FrameImpact
        }

        private readonly struct ParticleSpec
        {
            public ParticleSpec(
                float lifetimeMin,
                float lifetimeMax,
                float speedMin,
                float speedMax,
                float sizeMin,
                float sizeMax,
                int burstMin,
                int burstMax,
                float gravity,
                float coneAngle,
                float radius,
                float noise,
                ParticleSystemRenderMode renderMode)
            {
                LifetimeMin = lifetimeMin;
                LifetimeMax = lifetimeMax;
                SpeedMin = speedMin;
                SpeedMax = speedMax;
                SizeMin = sizeMin;
                SizeMax = sizeMax;
                BurstMin = burstMin;
                BurstMax = burstMax;
                Gravity = gravity;
                ConeAngle = coneAngle;
                Radius = radius;
                Noise = noise;
                RenderMode = renderMode;
            }

            public float LifetimeMin { get; }
            public float LifetimeMax { get; }
            public float SpeedMin { get; }
            public float SpeedMax { get; }
            public float SizeMin { get; }
            public float SizeMax { get; }
            public int BurstMin { get; }
            public int BurstMax { get; }
            public float Gravity { get; }
            public float ConeAngle { get; }
            public float Radius { get; }
            public float Noise { get; }
            public ParticleSystemRenderMode RenderMode { get; }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "58.0E.3 - Apply Final Gameplay Materials + Tool FX")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[M58.0E.3] Exit Play Mode before applying setup.");
                return;
            }

            Shader impasto = Shader.Find(ImpastoShaderName);
            Shader telegraph = Shader.Find(TelegraphShaderName);
            Shader sponge = Shader.Find(SpongeShaderName);
            Shader rope = Shader.Find(RopeShaderName);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");

            if (impasto == null || telegraph == null || sponge == null ||
                rope == null || lit == null)
            {
                Debug.LogError(
                    "[M58.0E.3] Final shaders are not compiled yet. " +
                    "Wait for Unity compilation, then run Apply again.");
                return;
            }

            // E3 is cumulative: keep the preceding wall-creation and Figure
            // material-motion presenters installed before final tuning.
            SetupOilPaintWallVisualFeedback_M58E1.Apply();
            SetupFigureStainMaterialMotion_M58E2.Apply();

            Material wetOil = RequireMaterial(WetOilPath);
            Material dryOil = RequireMaterial(DryOilPath);
            Material preview = RequireMaterial(PreviewPath);
            Material spongeMaterial = RequireMaterial(SpongePath);
            Material anchor = RequireMaterial(AnchorPath);
            Material ropeMaterial = RequireMaterial(RopePath);
            Material knife = RequireMaterial(KnifePath);
            Material oilParticles = RequireMaterial(OilParticlePath);
            Material fixativeParticles = RequireMaterial(FixativeParticlePath);
            Material spongeLiquid = RequireMaterial(SpongeLiquidPath);
            Material spongeBurst = RequireMaterial(SpongeBurstPath);
            Material frameImpact = RequireMaterial(FrameImpactPath);

            if (wetOil == null || dryOil == null || preview == null ||
                spongeMaterial == null || anchor == null ||
                ropeMaterial == null || knife == null ||
                oilParticles == null || fixativeParticles == null ||
                spongeLiquid == null || spongeBurst == null ||
                frameImpact == null)
            {
                return;
            }

            ConfigureImpasto(wetOil, impasto, false);
            ConfigureImpasto(dryOil, impasto, true);
            ConfigureTelegraph(preview, telegraph);
            ConfigureSponge(spongeMaterial, sponge);
            ConfigureAnchor(anchor, lit);
            ConfigureRope(ropeMaterial, rope);
            ConfigureKnife(knife);
            ConfigureParticleMaterial(
                oilParticles,
                new Color(0.56f, 0.035f, 0.065f, 0.92f),
                new Color(0.19f, 0.006f, 0.015f, 1f));
            ConfigureParticleMaterial(
                fixativeParticles,
                new Color(0.66f, 0.88f, 0.94f, 0.44f),
                new Color(0.12f, 0.36f, 0.45f, 1f));
            ConfigureParticleMaterial(
                spongeLiquid,
                new Color(1f, 1f, 1f, 0.82f),
                new Color(0.12f, 0.025f, 0.035f, 1f));
            ConfigureParticleMaterial(
                spongeBurst,
                new Color(1f, 1f, 1f, 0.88f),
                new Color(0.18f, 0.035f, 0.05f, 1f));
            ConfigureParticleMaterial(
                frameImpact,
                new Color(0.95f, 0.54f, 0.13f, 0.86f),
                new Color(0.42f, 0.12f, 0.018f, 1f));

            if (!EnsureCreationPrefab())
            {
                return;
            }

            ConfigureParticlePrefab(
                OilCreationPrefabPath,
                ParticleProfile.OilCreation,
                oilParticles);
            ConfigureParticlePrefab(
                OilCutPrefabPath,
                ParticleProfile.OilCut,
                oilParticles);
            ConfigureParticlePrefab(
                OilFracturePrefabPath,
                ParticleProfile.OilFracture,
                oilParticles);
            ConfigureParticlePrefab(
                FixativeSprayPrefabPath,
                ParticleProfile.FixativeSpray,
                fixativeParticles);
            ConfigureParticlePrefab(
                FixativeImpactPrefabPath,
                ParticleProfile.FixativeImpact,
                fixativeParticles);
            ConfigureParticlePrefab(
                SpongeLiquidPrefabPath,
                ParticleProfile.SpongeLiquid,
                spongeLiquid);
            ConfigureParticlePrefab(
                SpongeBurstPrefabPath,
                ParticleProfile.SpongeBurst,
                spongeBurst);
            ConfigureParticlePrefab(
                FrameImpactPrefabPath,
                ParticleProfile.FrameImpact,
                frameImpact);

            int grayboxReplacements = ReplaceGrayboxMaterials();
            int figuresConfigured = ConfigureFigures();
            int oilServicesConfigured = ConfigureOilCreationFeedback();

            AssetDatabase.SaveAssets();

            Debug.Log(
                "[M58.0E.3 APPLY]\n" +
                "OilSurface=Final impasto + wet/dry lifecycle\n" +
                "Preview=Animated directional bristle telegraph\n" +
                "Tools=Palette knife + sponge + fixative + frame gun\n" +
                "ParticlePrefabs=8 (bounded, pooled runtime consumers)\n" +
                $"FigureRootsConfigured={figuresConfigured}\n" +
                $"OilServicesConfigured={oilServicesConfigured}\n" +
                $"GrayboxSlotsReplaced={grayboxReplacements}\n" +
                "GameplayOrNetworkMutation=False\n" +
                "Save the scene, then run Diagnose.");
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "58.0E.3 - Diagnose Final Gameplay Materials + Tool FX")]
        public static void Diagnose()
        {
            List<string> lines = new();
            bool pass = true;

            pass &= CheckShader(WetOilPath, ImpastoShaderName, lines);
            pass &= CheckShader(DryOilPath, ImpastoShaderName, lines);
            pass &= CheckShader(PreviewPath, TelegraphShaderName, lines);
            pass &= CheckShader(SpongePath, SpongeShaderName, lines);
            pass &= CheckShader(RopePath, RopeShaderName, lines);

            string[] particlePrefabs =
            {
                OilCreationPrefabPath,
                OilCutPrefabPath,
                OilFracturePrefabPath,
                FixativeSprayPrefabPath,
                FixativeImpactPrefabPath,
                SpongeLiquidPrefabPath,
                SpongeBurstPrefabPath,
                FrameImpactPrefabPath
            };

            foreach (string path in particlePrefabs)
            {
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(path);
                bool valid =
                    prefab != null &&
                    prefab.GetComponent<ParticleSystem>() != null &&
                    prefab.transform.Find(SecondaryParticleName) != null;
                pass &= valid;
                lines.Add(
                    $"{(valid ? "PASS" : "FAIL")} | Particle | {path}");
            }

            FigureMotor[] motors =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int configuredFigures = 0;

            foreach (FigureMotor motor in motors)
            {
                PaletteKnifeImpactFeedback knifeFeedback =
                    motor.GetComponent<PaletteKnifeImpactFeedback>();
                SpongeSurfaceFeedback spongeFeedback =
                    motor.GetComponent<SpongeSurfaceFeedback>();
                bool valid =
                    knifeFeedback != null &&
                    knifeFeedback.ToolVisual != null &&
                    knifeFeedback.ToolRendererCount > 0 &&
                    spongeFeedback != null &&
                    spongeFeedback.IsConfigured;
                configuredFigures += valid ? 1 : 0;
                pass &= valid;
                lines.Add(
                    $"{(valid ? "PASS" : "FAIL")} | Figure | " +
                    $"{GetHierarchyPath(motor.transform)}");
            }

            int legacySlots = CountGrayboxMaterialSlots();
            OilStrokeCreationFeedback[] strokePresenters =
                UnityEngine.Object.FindObjectsByType<
                    OilStrokeCreationFeedback>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            FigureClarityMaterialMotionFeedback[] figurePresenters =
                UnityEngine.Object.FindObjectsByType<
                    FigureClarityMaterialMotionFeedback>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int validStrokePresenters = 0;
            int validFigurePresenters = 0;

            foreach (OilStrokeCreationFeedback presenter in strokePresenters)
            {
                validStrokePresenters +=
                    presenter != null && presenter.IsConfigured ? 1 : 0;
            }

            foreach (
                FigureClarityMaterialMotionFeedback presenter
                in figurePresenters)
            {
                validFigurePresenters +=
                    presenter != null && presenter.IsConfigured ? 1 : 0;
            }

            pass &= motors.Length > 0 && configuredFigures == motors.Length;
            pass &= legacySlots == 0;
            pass &=
                strokePresenters.Length > 0 &&
                validStrokePresenters == strokePresenters.Length;
            pass &=
                figurePresenters.Length > 0 &&
                validFigurePresenters == figurePresenters.Length;
            lines.Add(
                $"{(legacySlots == 0 ? "PASS" : "FAIL")} | " +
                $"LegacyGrayboxSlots={legacySlots}");
            lines.Add(
                $"{(validStrokePresenters == strokePresenters.Length && strokePresenters.Length > 0 ? "PASS" : "FAIL")} | " +
                $"OilCreationPresenters={validStrokePresenters}/" +
                $"{strokePresenters.Length}");
            lines.Add(
                $"{(validFigurePresenters == figurePresenters.Length && figurePresenters.Length > 0 ? "PASS" : "FAIL")} | " +
                $"FigureMaterialPresenters={validFigurePresenters}/" +
                $"{figurePresenters.Length}");

            string report =
                "[M58.0E.3 DIAGNOSE]\n" +
                string.Join("\n", lines) + "\n" +
                $"FigureRoots={motors.Length}\n" +
                $"ConfiguredFigures={configuredFigures}\n" +
                "GameplayOrNetworkMutation=False\n" +
                $"STATUS={(pass ? "PASS" : "FAIL")}";

            if (pass)
            {
                Debug.Log(report);
            }
            else
            {
                Debug.LogError(report);
            }
        }

        private static void ConfigureImpasto(
            Material material,
            Shader shader,
            bool dry)
        {
            Undo.RecordObject(material, "Configure final oil material");
            material.shader = shader;
            material.SetColor(
                "_BaseColor",
                dry
                    ? new Color(0.34f, 0.045f, 0.06f, 1f)
                    : new Color(0.55f, 0.035f, 0.06f, 1f));
            material.SetColor(
                "_EdgeColor",
                dry
                    ? new Color(0.12f, 0.012f, 0.018f, 1f)
                    : new Color(0.16f, 0.004f, 0.012f, 1f));
            material.SetFloat("_Metallic", dry ? 0f : 0.025f);
            material.SetFloat("_Smoothness", dry ? 0.24f : 0.9f);
            material.SetFloat("_Dryness", dry ? 1f : 0f);
            material.SetFloat("_GrooveScale", dry ? 31f : 27f);
            material.SetFloat("_GrooveStrength", dry ? 0.43f : 0.58f);
            material.SetFloat("_EdgePigment", dry ? 0.58f : 0.72f);
            material.SetFloat("_CrackStrength", dry ? 0.9f : 0.72f);
            material.SetFloat("_CreationPulse", 0f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureTelegraph(
            Material material,
            Shader shader)
        {
            Undo.RecordObject(material, "Configure final stroke preview");
            material.shader = shader;
            material.SetColor(
                "_BaseColor",
                new Color(0.76f, 0.075f, 0.095f, 0.74f));
            material.SetColor(
                "_EdgeColor",
                new Color(1f, 0.52f, 0.24f, 0.9f));
            material.SetFloat("_FlowSpeed", 1.15f);
            material.SetFloat("_DashScale", 8.5f);
            material.SetFloat("_DashFill", 0.68f);
            material.SetFloat("_EdgeSoftness", 0.16f);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureSponge(
            Material material,
            Shader shader)
        {
            Undo.RecordObject(material, "Configure final sponge material");
            material.shader = shader;
            material.SetColor(
                "_BaseColor",
                new Color(0.72f, 0.45f, 0.105f, 1f));
            material.SetColor(
                "_AbsorbedColor",
                new Color(0.42f, 0.028f, 0.05f, 1f));
            material.SetFloat("_Fill", 0f);
            material.SetFloat("_Instability", 0f);
            material.SetFloat("_PoreScale", 34f);
            material.SetFloat("_PoreDepth", 0.72f);
            material.SetFloat("_DrySmoothness", 0.12f);
            material.SetFloat("_WetSmoothness", 0.78f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureAnchor(Material material, Shader shader)
        {
            Undo.RecordObject(material, "Configure final frame anchor");
            material.shader = shader;
            SetColorIfPresent(
                material,
                new Color(0.29f, 0.12f, 0.035f, 1f));
            SetFloatIfPresent(material, "_Metallic", 0.82f);
            SetFloatIfPresent(material, "_Smoothness", 0.46f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureRope(Material material, Shader shader)
        {
            Undo.RecordObject(material, "Configure final frame rope");
            material.shader = shader;
            material.SetColor(
                "_BaseColor",
                new Color(0.34f, 0.18f, 0.075f, 1f));
            material.SetColor(
                "_FiberColor",
                new Color(0.67f, 0.42f, 0.17f, 1f));
            material.SetFloat("_FiberScale", 46f);
            material.SetFloat("_FiberStrength", 0.62f);
            material.SetFloat("_Smoothness", 0.18f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureKnife(Material material)
        {
            Undo.RecordObject(material, "Tune final palette knife");
            SetFloatIfPresent(material, "_Metallic", 0.94f);
            SetFloatIfPresent(material, "_Smoothness", 0.68f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureParticleMaterial(
            Material material,
            Color baseColor,
            Color emission)
        {
            Undo.RecordObject(material, "Configure final tool particle");
            SetColorIfPresent(material, baseColor);
            SetColorPropertyIfPresent(material, "_EmissionColor", emission);
            SetColorPropertyIfPresent(material, "_Emission", emission);
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_SrcBlend", 5f);
            SetFloatIfPresent(material, "_DstBlend", 10f);
            SetFloatIfPresent(material, "_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static bool EnsureCreationPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    OilCreationPrefabPath) != null)
            {
                return true;
            }

            bool copied =
                AssetDatabase.CopyAsset(
                    OilCutPrefabPath,
                    OilCreationPrefabPath);
            AssetDatabase.ImportAsset(OilCreationPrefabPath);

            if (!copied)
            {
                Debug.LogError(
                    "[M58.0E.3] Could not create the dedicated oil " +
                    "stroke creation particle prefab.");
            }

            return copied;
        }

        private static void ConfigureParticlePrefab(
            string path,
            ParticleProfile profile,
            Material material)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            if (root == null)
            {
                Debug.LogError($"[M58.0E.3] Missing VFX prefab: {path}");
                return;
            }

            try
            {
                ParticleSystem primary =
                    root.GetComponent<ParticleSystem>();

                if (primary == null)
                {
                    Debug.LogError(
                        $"[M58.0E.3] No root ParticleSystem: {path}");
                    return;
                }

                ConfigureParticleSystem(
                    primary,
                    profile,
                    material,
                    false);

                Transform secondaryTransform =
                    root.transform.Find(SecondaryParticleName);

                if (secondaryTransform == null)
                {
                    GameObject secondaryObject =
                        new GameObject(SecondaryParticleName);
                    secondaryObject.transform.SetParent(
                        root.transform,
                        false);
                    secondaryTransform = secondaryObject.transform;
                }

                ParticleSystem secondary =
                    secondaryTransform.GetComponent<ParticleSystem>();

                if (secondary == null)
                {
                    secondary = secondaryTransform.gameObject.AddComponent<
                        ParticleSystem>();
                }

                ConfigureParticleSystem(
                    secondary,
                    profile,
                    material,
                    true);

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureParticleSystem(
            ParticleSystem particle,
            ParticleProfile profile,
            Material material,
            bool secondary)
        {
            ParticleSpec spec = GetParticleSpec(profile, secondary);
            ParticleSystem.MainModule main = particle.main;
            main.duration = Mathf.Max(0.1f, spec.LifetimeMax);
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = secondary ? 28 : 48;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                spec.LifetimeMin,
                spec.LifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                spec.SpeedMin,
                spec.SpeedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(
                spec.SizeMin,
                spec.SizeMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(
                -Mathf.PI,
                Mathf.PI);
            main.gravityModifier = spec.Gravity;
            main.startColor = Color.white;

            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(
                new[]
                {
                    new ParticleSystem.Burst(
                        0f,
                        (short)spec.BurstMin,
                        (short)spec.BurstMax)
                });

            ParticleSystem.ShapeModule shape = particle.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = spec.ConeAngle;
            shape.radius = spec.Radius;
            shape.radiusThickness = 0.35f;

            ParticleSystem.ColorOverLifetimeModule color =
                particle.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(
                BuildFadeGradient(secondary));

            ParticleSystem.SizeOverLifetimeModule size =
                particle.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(
                1f,
                BuildSizeCurve(profile, secondary));

            ParticleSystem.NoiseModule noise = particle.noise;
            noise.enabled = spec.Noise > 0f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            noise.strength = spec.Noise;
            noise.frequency = 0.72f;
            noise.scrollSpeed = 0.18f;
            noise.damping = true;

            ParticleSystemRenderer renderer =
                particle.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = spec.RenderMode;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingOrder = secondary ? 1 : 0;

            if (spec.RenderMode ==
                ParticleSystemRenderMode.Stretch)
            {
                renderer.velocityScale = 0.42f;
                renderer.lengthScale = 1.35f;
            }
        }

        private static ParticleSpec GetParticleSpec(
            ParticleProfile profile,
            bool secondary)
        {
            float scale = secondary ? 0.58f : 1f;
            int countScale = secondary ? 1 : 2;

            return profile switch
            {
                ParticleProfile.OilCreation =>
                    new ParticleSpec(
                        0.28f, 0.52f, 0.12f, 0.5f,
                        0.035f * scale, 0.11f * scale,
                        5 * countScale, 8 * countScale,
                        0.05f, 12f, 0.035f, 0.08f,
                        ParticleSystemRenderMode.Stretch),
                ParticleProfile.OilCut =>
                    new ParticleSpec(
                        0.18f, 0.44f, 1.1f, 2.8f,
                        0.025f * scale, 0.09f * scale,
                        5 * countScale, 9 * countScale,
                        0.42f, 24f, 0.045f, 0.04f,
                        ParticleSystemRenderMode.Stretch),
                ParticleProfile.OilFracture =>
                    new ParticleSpec(
                        0.34f, 0.72f, 0.65f, 1.85f,
                        0.04f * scale, 0.16f * scale,
                        7 * countScale, 12 * countScale,
                        0.7f, 42f, 0.11f, 0.06f,
                        ParticleSystemRenderMode.Billboard),
                ParticleProfile.FixativeSpray =>
                    new ParticleSpec(
                        0.22f, 0.5f, 2.8f, 5.1f,
                        0.018f * scale, 0.065f * scale,
                        8 * countScale, 14 * countScale,
                        -0.02f, 9f, 0.025f, 0.2f,
                        ParticleSystemRenderMode.Stretch),
                ParticleProfile.FixativeImpact =>
                    new ParticleSpec(
                        0.25f, 0.56f, 0.15f, 0.9f,
                        0.035f * scale, 0.13f * scale,
                        6 * countScale, 10 * countScale,
                        -0.04f, 38f, 0.085f, 0.16f,
                        ParticleSystemRenderMode.Billboard),
                ParticleProfile.SpongeLiquid =>
                    new ParticleSpec(
                        0.28f, 0.62f, 0.25f, 1.15f,
                        0.035f * scale, 0.12f * scale,
                        5 * countScale, 9 * countScale,
                        0.85f, 18f, 0.055f, 0.1f,
                        ParticleSystemRenderMode.Stretch),
                ParticleProfile.SpongeBurst =>
                    new ParticleSpec(
                        0.38f, 0.82f, 1.2f, 3.4f,
                        0.04f * scale, 0.17f * scale,
                        9 * countScale, 15 * countScale,
                        0.9f, 58f, 0.12f, 0.12f,
                        ParticleSystemRenderMode.Billboard),
                ParticleProfile.FrameImpact =>
                    new ParticleSpec(
                        0.16f, 0.46f, 1.3f, 4.1f,
                        0.018f * scale, 0.075f * scale,
                        6 * countScale, 11 * countScale,
                        0.5f, 48f, 0.05f, 0.025f,
                        ParticleSystemRenderMode.Stretch),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(profile),
                    profile,
                    null)
            };
        }

        private static Gradient BuildFadeGradient(bool secondary)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(
                        secondary
                            ? new Color(1f, 0.72f, 0.42f)
                            : Color.white,
                        0.42f),
                    new GradientColorKey(
                        new Color(0.48f, 0.12f, 0.1f),
                        1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.08f),
                    new GradientAlphaKey(0.82f, 0.62f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static AnimationCurve BuildSizeCurve(
            ParticleProfile profile,
            bool secondary)
        {
            bool expands =
                profile == ParticleProfile.FixativeImpact ||
                profile == ParticleProfile.FixativeSpray;

            return expands
                ? new AnimationCurve(
                    new Keyframe(0f, secondary ? 0.3f : 0.5f),
                    new Keyframe(0.55f, 1f),
                    new Keyframe(1f, secondary ? 1.25f : 1.55f))
                : new AnimationCurve(
                    new Keyframe(0f, 0.35f),
                    new Keyframe(0.18f, 1f),
                    new Keyframe(1f, 0.08f));
        }

        private static int ConfigureFigures()
        {
            FigureMotor[] motors =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int configured = 0;

            foreach (FigureMotor motor in motors)
            {
                FigureToolLoadoutController loadout =
                    motor.GetComponent<FigureToolLoadoutController>();
                PaletteKnifeImpactFeedback knifeFeedback =
                    motor.GetComponent<PaletteKnifeImpactFeedback>();
                SpongeReservoir reservoir =
                    motor.GetComponent<SpongeReservoir>();
                SpongeController spongeController =
                    motor.GetComponent<SpongeController>();

                Transform knifeVisual =
                    GetObjectReferenceTransform(
                        loadout,
                        "paletteKnifeVisual");
                Renderer spongeRenderer =
                    GetObjectReference<Renderer>(
                        spongeController,
                        "spongeRenderer");

                if (knifeFeedback == null || knifeVisual == null ||
                    reservoir == null || spongeRenderer == null)
                {
                    Debug.LogError(
                        "[M58.0E.3] Figure tool visuals are incomplete: " +
                        GetHierarchyPath(motor.transform),
                        motor);
                    continue;
                }

                Undo.RecordObject(
                    knifeFeedback,
                    "Configure final palette knife feedback");
                knifeFeedback.ConfigureToolVisual(knifeVisual);
                EditorUtility.SetDirty(knifeFeedback);

                SpongeSurfaceFeedback spongeFeedback =
                    motor.GetComponent<SpongeSurfaceFeedback>();
                if (spongeFeedback == null)
                {
                    spongeFeedback =
                        Undo.AddComponent<SpongeSurfaceFeedback>(
                            motor.gameObject);
                }

                Undo.RecordObject(
                    spongeFeedback,
                    "Configure final sponge feedback");
                spongeFeedback.Configure(reservoir, spongeRenderer);
                EditorUtility.SetDirty(spongeFeedback);
                EditorSceneManager.MarkSceneDirty(
                    motor.gameObject.scene);
                configured++;
            }

            return configured;
        }

        private static int ConfigureOilCreationFeedback()
        {
            GameObject creationPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    OilCreationPrefabPath);
            ParticleSystem creationParticle =
                creationPrefab != null
                    ? creationPrefab.GetComponent<ParticleSystem>()
                    : null;

            if (creationParticle == null)
            {
                return 0;
            }

            OilPaintFeedbackService[] services =
                UnityEngine.Object.FindObjectsByType<
                    OilPaintFeedbackService>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int configured = 0;

            foreach (OilPaintFeedbackService service in services)
            {
                SerializedObject serialized =
                    new SerializedObject(service);
                SerializedProperty property =
                    serialized.FindProperty(
                        "strokeCreationParticlePrefab");

                if (property == null)
                {
                    continue;
                }

                Undo.RecordObject(
                    service,
                    "Configure final oil creation VFX");
                property.objectReferenceValue = creationParticle;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(service);
                EditorSceneManager.MarkSceneDirty(
                    service.gameObject.scene);
                configured++;
            }

            return configured;
        }

        private static int ReplaceGrayboxMaterials()
        {
            Material grayGround = RequireMaterial(GrayGroundPath);
            Material grayObstacle = RequireMaterial(GrayObstaclePath);
            Material finalGround = RequireMaterial(FinalGroundPath);
            Material finalObstacle = RequireMaterial(FinalObstaclePath);

            if (grayGround == null || grayObstacle == null ||
                finalGround == null || finalObstacle == null)
            {
                return 0;
            }

            Renderer[] renderers =
                UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int replaced = 0;

            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == grayGround)
                    {
                        materials[i] = finalGround;
                        changed = true;
                        replaced++;
                    }
                    else if (materials[i] == grayObstacle)
                    {
                        materials[i] = finalObstacle;
                        changed = true;
                        replaced++;
                    }
                }

                if (!changed)
                {
                    continue;
                }

                Undo.RecordObject(
                    renderer,
                    "Replace graybox material with final material");
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
                EditorSceneManager.MarkSceneDirty(
                    renderer.gameObject.scene);
            }

            return replaced;
        }

        private static int CountGrayboxMaterialSlots()
        {
            Material grayGround =
                AssetDatabase.LoadAssetAtPath<Material>(GrayGroundPath);
            Material grayObstacle =
                AssetDatabase.LoadAssetAtPath<Material>(GrayObstaclePath);
            int count = 0;

            Renderer[] renderers =
                UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == grayGround || material == grayObstacle)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool CheckShader(
            string materialPath,
            string expectedShader,
            List<string> lines)
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            bool valid =
                material != null &&
                material.shader != null &&
                material.shader.name == expectedShader;
            lines.Add(
                $"{(valid ? "PASS" : "FAIL")} | Material | " +
                $"{materialPath} | Shader={expectedShader}");
            return valid;
        }

        private static Material RequireMaterial(string path)
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Debug.LogError($"[M58.0E.3] Missing material: {path}");
            }

            return material;
        }

        private static void SetColorIfPresent(
            Material material,
            Color color)
        {
            SetColorPropertyIfPresent(material, "_BaseColor", color);
            SetColorPropertyIfPresent(material, "_Color", color);
        }

        private static void SetColorPropertyIfPresent(
            Material material,
            string property,
            Color value)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, value);
            }
        }

        private static void SetFloatIfPresent(
            Material material,
            string property,
            float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static T GetObjectReference<T>(
            UnityEngine.Object target,
            string propertyName)
            where T : UnityEngine.Object
        {
            if (target == null)
            {
                return null;
            }

            SerializedProperty property =
                new SerializedObject(target).FindProperty(propertyName);
            return property != null
                ? property.objectReferenceValue as T
                : null;
        }

        private static Transform GetObjectReferenceTransform(
            UnityEngine.Object target,
            string propertyName)
        {
            if (target == null)
            {
                return null;
            }

            SerializedProperty property =
                new SerializedObject(target).FindProperty(propertyName);
            UnityEngine.Object value =
                property != null
                    ? property.objectReferenceValue
                    : null;
            return value switch
            {
                GameObject gameObject => gameObject.transform,
                Component component => component.transform,
                _ => null
            };
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return "<missing>";
            }

            string path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }
    }
}
#endif
