using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupStrokeImpactFeedbackMilestone
    {
        private const string RootFolder =
            "Assets/_Project";

        private const string ConfigPath =
            RootFolder +
            "/Data/Paint/OilStrokeFragmentInteractionConfig.asset";

        private const string MaterialPath =
            RootFolder +
            "/Art/Materials/VFX/M_OilImpactParticles.mat";

        private const string KnifeParticlePath =
            RootFolder +
            "/Prefabs/VFX/Paint/VFX_OilKnifeCut.prefab";

        private const string FractureParticlePath =
            RootFolder +
            "/Prefabs/VFX/Paint/VFX_OilFracture.prefab";

        [MenuItem(
            "Tools/Painted Alive/Milestones/07 - Setup Stroke Impact Feedback")]
        public static void Run()
        {
            try
            {
                EnsureFolders();

                OilStrokeFragmentInteractionConfig config =
                    GetOrCreateConfig();

                Material particleMaterial =
                    GetOrCreateParticleMaterial();

                ParticleSystem knifeParticle =
                    GetOrCreateParticlePrefab(
                        KnifeParticlePath,
                        "VFX_OilKnifeCut",
                        particleMaterial,
                        false);

                ParticleSystem fractureParticle =
                    GetOrCreateParticlePrefab(
                        FractureParticlePath,
                        "VFX_OilFracture",
                        particleMaterial,
                        true);

                OilStrokeStructuralIntegritySystem integritySystem =
                    UnityEngine.Object.FindFirstObjectByType<
                        OilStrokeStructuralIntegritySystem>(
                            FindObjectsInactive.Include);

                if (integritySystem == null)
                {
                    throw new InvalidOperationException(
                        "Sahnede OilStrokeStructuralIntegritySystem " +
                        "bulunamadı. Önce fracture milestone sahnesini aç.");
                }

                GameObject runtimeObject =
                    integritySystem.gameObject;

                OilPaintFeedbackService feedbackService =
                    GetOrAddComponent<OilPaintFeedbackService>(
                        runtimeObject);

                SetReference(
                    feedbackService,
                    "knifeCutParticlePrefab",
                    knifeParticle);

                SetReference(
                    feedbackService,
                    "fractureParticlePrefab",
                    fractureParticle);

                OilStrokeFractureFeedback fractureFeedback =
                    GetOrAddComponent<OilStrokeFractureFeedback>(
                        runtimeObject);

                SetReference(
                    fractureFeedback,
                    "integritySystem",
                    integritySystem);

                SetReference(
                    fractureFeedback,
                    "feedbackService",
                    feedbackService);

                OilStrokeFragmentInteractionSystem interactionSystem =
                    GetOrAddComponent<
                        OilStrokeFragmentInteractionSystem>(
                            runtimeObject);

                SetReference(
                    interactionSystem,
                    "config",
                    config);

                SetReference(
                    interactionSystem,
                    "fragmentsRoot",
                    integritySystem.FragmentRoot);

                int knifeCount =
                    ConfigurePaletteKnives(feedbackService);

                ValidateFigureMotor();

                EditorUtility.SetDirty(runtimeObject);
                EditorSceneManager.MarkSceneDirty(
                    runtimeObject.scene);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Milestone 07] Kurulum tamamlandı. " +
                    $"{knifeCount} PaletteKnifeController bağlandı. " +
                    "PaletteKnifeImpactFeedback üzerindeki Tool Visual " +
                    "alanına yalnızca bıçak modelinin pivotunu ata; " +
                    "oyuncu kökünü atama.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static int ConfigurePaletteKnives(
            OilPaintFeedbackService feedbackService)
        {
            PaletteKnifeController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    PaletteKnifeController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            foreach (PaletteKnifeController controller
                     in controllers)
            {
                PaletteKnifeImpactFeedback impactFeedback =
                    GetOrAddComponent<
                        PaletteKnifeImpactFeedback>(
                            controller.gameObject);

                SetReference(
                    impactFeedback,
                    "feedbackService",
                    feedbackService);

                SetReference(
                    controller,
                    "impactFeedback",
                    impactFeedback);

                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(impactFeedback);
            }

            return controllers.Length;
        }

        private static void ValidateFigureMotor()
        {
            if (typeof(FigureMotor).GetMethod(
                    "AddExternalImpulse") == null)
            {
                throw new InvalidOperationException(
                    "FigureMotor.AddExternalImpulse bulunamadı. " +
                    "Paketin ReplaceExisting klasöründeki " +
                    "FigureMotor.cs dosyasını projeye kopyala.");
            }
        }

        private static OilStrokeFragmentInteractionConfig
            GetOrCreateConfig()
        {
            OilStrokeFragmentInteractionConfig existing =
                AssetDatabase.LoadAssetAtPath<
                    OilStrokeFragmentInteractionConfig>(
                        ConfigPath);

            if (existing != null)
            {
                return existing;
            }

            var created =
                ScriptableObject.CreateInstance<
                    OilStrokeFragmentInteractionConfig>();

            AssetDatabase.CreateAsset(created, ConfigPath);
            return created;
        }

        private static Material GetOrCreateParticleMaterial()
        {
            Material existing =
                AssetDatabase.LoadAssetAtPath<Material>(
                    MaterialPath);

            if (existing != null)
            {
                return existing;
            }

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Particles/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "URP particle shader bulunamadı.");
            }

            var material = new Material(shader)
            {
                name = "M_OilImpactParticles"
            };

            Color paintColor =
                new Color(0.42f, 0.08f, 0.035f, 1f);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", paintColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", paintColor);
            }

            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static ParticleSystem GetOrCreateParticlePrefab(
            string path,
            string objectName,
            Material material,
            bool fracture)
        {
            GameObject existingPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (existingPrefab != null)
            {
                ParticleSystem existing =
                    existingPrefab.GetComponent<ParticleSystem>();

                if (existing == null)
                {
                    throw new InvalidOperationException(
                        $"Prefab ParticleSystem içermiyor: {path}");
                }

                return existing;
            }

            var particleObject = new GameObject(objectName);
            ParticleSystem particleSystem =
                particleObject.AddComponent<ParticleSystem>();

            ConfigureParticleSystem(
                particleSystem,
                material,
                fracture);

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    particleObject,
                    path);

            UnityEngine.Object.DestroyImmediate(particleObject);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Particle prefab oluşturulamadı: {path}");
            }

            return prefab.GetComponent<ParticleSystem>();
        }

        private static void ConfigureParticleSystem(
            ParticleSystem particleSystem,
            Material material,
            bool fracture)
        {
            ParticleSystem.MainModule main =
                particleSystem.main;

            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.7f;
            main.startLifetime = fracture
                ? new ParticleSystem.MinMaxCurve(0.45f, 0.9f)
                : new ParticleSystem.MinMaxCurve(0.18f, 0.42f);

            main.startSpeed = fracture
                ? new ParticleSystem.MinMaxCurve(0.7f, 2.2f)
                : new ParticleSystem.MinMaxCurve(1.2f, 3.4f);

            main.startSize = fracture
                ? new ParticleSystem.MinMaxCurve(0.08f, 0.24f)
                : new ParticleSystem.MinMaxCurve(0.035f, 0.12f);

            main.startRotation =
                new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            main.simulationSpace =
                ParticleSystemSimulationSpace.World;

            main.maxParticles = fracture ? 80 : 36;

            ParticleSystem.EmissionModule emission =
                particleSystem.emission;

            emission.rateOverTime = 0f;
            emission.SetBursts(
                new[]
                {
                    new ParticleSystem.Burst(
                        0f,
                        (short)(fracture ? 24 : 10),
                        (short)(fracture ? 42 : 18))
                });

            ParticleSystem.ShapeModule shape =
                particleSystem.shape;

            shape.enabled = true;
            shape.shapeType = fracture
                ? ParticleSystemShapeType.Hemisphere
                : ParticleSystemShapeType.Cone;

            shape.radius = fracture ? 0.35f : 0.08f;
            shape.angle = fracture ? 45f : 18f;

            ParticleSystem.ColorOverLifetimeModule color =
                particleSystem.colorOverLifetime;

            color.enabled = true;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(0.72f, 0.15f, 0.05f),
                        0f),
                    new GradientColorKey(
                        new Color(0.18f, 0.025f, 0.01f),
                        1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });

            color.color =
                new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystem.SizeOverLifetimeModule size =
                particleSystem.sizeOverLifetime;

            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.EaseInOut(
                    0f,
                    1f,
                    1f,
                    0f));

            ParticleSystemRenderer renderer =
                particleSystem.GetComponent<
                    ParticleSystemRenderer>();

            renderer.renderMode =
                ParticleSystemRenderMode.Billboard;

            renderer.sharedMaterial = material;
        }

        private static T GetOrAddComponent<T>(
            GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();

            if (component == null)
            {
                component = Undo.AddComponent<T>(target);
            }

            return component;
        }

        private static void SetReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serializedObject =
                new SerializedObject(target);

            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} " +
                    "alanı bulunamadı.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder(RootFolder, "Data");
            EnsureFolder(RootFolder + "/Data", "Paint");
            EnsureFolder(RootFolder, "Art");
            EnsureFolder(RootFolder + "/Art", "Materials");
            EnsureFolder(RootFolder + "/Art/Materials", "VFX");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder + "/Prefabs", "VFX");
            EnsureFolder(RootFolder + "/Prefabs/VFX", "Paint");
        }

        private static void EnsureFolder(
            string parent,
            string child)
        {
            string path = $"{parent}/{child}";

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
