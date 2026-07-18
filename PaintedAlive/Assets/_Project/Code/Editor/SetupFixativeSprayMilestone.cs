using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace PaintedAlive.EditorTools
{
    public static class SetupFixativeSprayMilestone
    {
        private const string RootFolder = "Assets/_Project";

        private const string ConfigPath =
            RootFolder +
            "/Data/Paint/FixativeSprayConfig.asset";

        private const string MaterialPath =
            RootFolder +
            "/Art/Materials/VFX/M_FixativeParticles.mat";

        private const string SprayParticlePath =
            RootFolder +
            "/Prefabs/VFX/Paint/VFX_FixativeSpray.prefab";

        private const string ImpactParticlePath =
            RootFolder +
            "/Prefabs/VFX/Paint/VFX_FixativeImpact.prefab";

        [MenuItem(
            "Tools/Painted Alive/Milestones/08 - Setup Fixative Spray")]
        public static void Run()
        {
            try
            {
                EnsureFolders();

                FixativeSprayConfig config =
                    GetOrCreateConfig();

                Material particleMaterial =
                    GetOrCreateParticleMaterial();

                ParticleSystem sprayParticle =
                    GetOrCreateParticlePrefab(
                        SprayParticlePath,
                        "VFX_FixativeSpray",
                        particleMaterial,
                        false);

                ParticleSystem impactParticle =
                    GetOrCreateParticlePrefab(
                        ImpactParticlePath,
                        "VFX_FixativeImpact",
                        particleMaterial,
                        true);

                PaletteKnifeController[] paletteKnives =
                    UnityEngine.Object.FindObjectsByType<
                        PaletteKnifeController>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None);

                if (paletteKnives.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Sahnede PaletteKnifeController bulunamadı. " +
                        "Önce Figür test sahnesini aç.");
                }

                ValidateLifecycleApi();

                foreach (PaletteKnifeController paletteKnife
                         in paletteKnives)
                {
                    ConfigureFigureToolHost(
                        paletteKnife,
                        config,
                        sprayParticle,
                        impactParticle);

                    EditorSceneManager.MarkSceneDirty(
                        paletteKnife.gameObject.scene);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Milestone 08] Sabitleyici Sprey kurulumu " +
                    $"tamamlandı. {paletteKnives.Length} Figür " +
                    "tool host'u bağlandı. Test: 1 Palet Bıçağı, " +
                    "2 Sabitleyici Sprey, E kullan.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void ConfigureFigureToolHost(
            PaletteKnifeController paletteKnife,
            FixativeSprayConfig config,
            ParticleSystem sprayParticle,
            ParticleSystem impactParticle)
        {
            GameObject host = paletteKnife.gameObject;

            Camera outputCamera =
                GetReference<Camera>(
                    paletteKnife,
                    "outputCamera");

            Transform toolOrigin =
                GetReference<Transform>(
                    paletteKnife,
                    "toolOrigin");

            Transform fixativeOrigin =
                GetOrCreateFixativeOrigin(
                    outputCamera,
                    toolOrigin);

            InputActionReference useToolAction =
                GetReference<InputActionReference>(
                    paletteKnife,
                    "useToolAction");

            FigureClarityState clarityState =
                GetReference<FigureClarityState>(
                    paletteKnife,
                    "clarityState");

            int oilPaintMask =
                GetInteger(
                    paletteKnife,
                    "oilPaintMask");

            FixativeSprayFeedback feedback =
                GetOrAddComponent<FixativeSprayFeedback>(host);

            SetReference(
                feedback,
                "sprayParticlePrefab",
                sprayParticle);

            SetReference(
                feedback,
                "impactParticlePrefab",
                impactParticle);

            FixativeSprayController fixative =
                GetOrAddComponent<FixativeSprayController>(host);

            SetReference(fixative, "outputCamera", outputCamera);
            SetReference(fixative, "toolOrigin", fixativeOrigin);
            SetReference(fixative, "useToolAction", useToolAction);
            SetReference(fixative, "clarityState", clarityState);
            SetReference(fixative, "config", config);
            SetReference(fixative, "feedback", feedback);
            SetInteger(fixative, "oilPaintMask", oilPaintMask);

            FigureToolLoadoutController loadout =
                GetOrAddComponent<
                    FigureToolLoadoutController>(host);

            SetReference(
                loadout,
                "paletteKnifeController",
                paletteKnife);

            SetReference(
                loadout,
                "fixativeSprayController",
                fixative);

            paletteKnife.enabled = true;
            fixative.enabled = false;

            EditorUtility.SetDirty(paletteKnife);
            EditorUtility.SetDirty(fixative);
            EditorUtility.SetDirty(feedback);
            EditorUtility.SetDirty(loadout);
        }

        private static Transform GetOrCreateFixativeOrigin(
            Camera outputCamera,
            Transform fallbackOrigin)
        {
            if (outputCamera == null)
            {
                return fallbackOrigin;
            }

            Transform existing =
                outputCamera.transform.Find(
                    "FixativeSprayOrigin");

            if (existing != null)
            {
                return existing;
            }

            var originObject =
                new GameObject("FixativeSprayOrigin");

            Undo.RegisterCreatedObjectUndo(
                originObject,
                "Create Fixative Spray Origin");

            Transform origin = originObject.transform;
            origin.SetParent(outputCamera.transform, false);
            origin.localPosition =
                new Vector3(0.22f, -0.18f, 0.42f);

            origin.localRotation = Quaternion.identity;
            origin.localScale = Vector3.one;
            return origin;
        }

        private static void ValidateLifecycleApi()
        {
            if (typeof(OilStrokeRuntime).GetMethod(
                    "TryAdvanceLifecycle") == null)
            {
                throw new InvalidOperationException(
                    "OilStrokeRuntime.TryAdvanceLifecycle bulunamadı. " +
                    "ReplaceExisting klasöründeki OilStrokeRuntime.cs " +
                    "dosyasını projeye kopyala.");
            }
        }

        private static FixativeSprayConfig GetOrCreateConfig()
        {
            FixativeSprayConfig existing =
                AssetDatabase.LoadAssetAtPath<
                    FixativeSprayConfig>(ConfigPath);

            if (existing != null)
            {
                return existing;
            }

            var created =
                ScriptableObject.CreateInstance<
                    FixativeSprayConfig>();

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
                name = "M_FixativeParticles",
                renderQueue = 3000
            };

            Color aerosolColor =
                new Color(0.62f, 0.93f, 1f, 0.72f);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", aerosolColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", aerosolColor);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.EnableKeyword(
                    "_SURFACE_TYPE_TRANSPARENT");
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static ParticleSystem GetOrCreateParticlePrefab(
            string path,
            string objectName,
            Material material,
            bool impact)
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
                impact);

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
            bool impact)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = impact ? 0.45f : 0.25f;

            main.startLifetime = impact
                ? new ParticleSystem.MinMaxCurve(0.25f, 0.55f)
                : new ParticleSystem.MinMaxCurve(0.12f, 0.28f);

            main.startSpeed = impact
                ? new ParticleSystem.MinMaxCurve(0.2f, 0.9f)
                : new ParticleSystem.MinMaxCurve(2.4f, 4.5f);

            main.startSize = impact
                ? new ParticleSystem.MinMaxCurve(0.05f, 0.16f)
                : new ParticleSystem.MinMaxCurve(0.025f, 0.075f);

            main.simulationSpace =
                ParticleSystemSimulationSpace.World;

            main.maxParticles = impact ? 60 : 48;

            ParticleSystem.EmissionModule emission =
                particleSystem.emission;

            emission.rateOverTime = 0f;
            emission.SetBursts(
                new[]
                {
                    new ParticleSystem.Burst(
                        0f,
                        (short)(impact ? 8 : 5),
                        (short)(impact ? 15 : 9))
                });

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = impact
                ? ParticleSystemShapeType.Hemisphere
                : ParticleSystemShapeType.Cone;

            shape.radius = impact ? 0.14f : 0.025f;
            shape.angle = impact ? 35f : 6f;

            ParticleSystem.ColorOverLifetimeModule color =
                particleSystem.colorOverLifetime;

            color.enabled = true;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(0.85f, 0.98f, 1f),
                        0f),
                    new GradientColorKey(
                        new Color(0.28f, 0.75f, 0.9f),
                        1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.8f, 0f),
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

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();

            if (component == null)
            {
                component = Undo.AddComponent<T>(target);
            }

            return component;
        }

        private static T GetReference<T>(
            UnityEngine.Object target,
            string propertyName)
            where T : UnityEngine.Object
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} " +
                    "alanı bulunamadı.");
            }

            return property.objectReferenceValue as T;
        }

        private static int GetInteger(
            UnityEngine.Object target,
            string propertyName)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} " +
                    "alanı bulunamadı.");
            }

            return property.intValue;
        }

        private static void SetReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
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

        private static void SetInteger(
            UnityEngine.Object target,
            string propertyName,
            int value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} " +
                    "alanı bulunamadı.");
            }

            property.intValue = value;
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
