using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.Impact;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint.Sponge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PaintedAlive.EditorTools
{
    public static class SetupSpongeBurstMilestone
    {
        private const string RootFolder = "Assets/_Project";

        private const string ConfigPath =
            RootFolder + "/Data/Figures/SpongeBurstConfig.asset";

        private const string PuddlePrefabPath =
            RootFolder + "/Prefabs/Paint/SpongePaintPuddle.prefab";

        private const string BurstMaterialPath =
            RootFolder +
            "/Art/Materials/VFX/M_SpongeBurstVFX.mat";

        private const string ProjectileMaterialPath =
            RootFolder +
            "/Art/Materials/Tests/M_SpongeImpactProjectile.mat";

        private const string BurstVfxPrefabPath =
            RootFolder +
            "/Prefabs/VFX/Tools/VFX_SpongeBurst.prefab";

        private const string ProjectilePrefabPath =
            RootFolder +
            "/Prefabs/Tests/SpongeBurstImpactProjectile.prefab";

        [MenuItem(
            "Tools/Painted Alive/Milestones/12 - Setup Sponge Impact Burst")]
        public static void Run()
        {
            try
            {
                EnsureFolders();

                SpongeReservoir[] reservoirs =
                    UnityEngine.Object.FindObjectsByType<
                        SpongeReservoir>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None);

                if (reservoirs.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Sahnede SpongeReservoir bulunamadı. " +
                        "Önce çalışan M11 sahnesini aç.");
                }

                SpongeAbsorbablePaintSource puddlePrefab =
                    LoadRequiredPuddlePrefab();
                SpongeBurstConfig config =
                    GetOrCreateConfig();

                Material burstMaterial =
                    GetOrCreateMaterial(
                        BurstMaterialPath,
                        "M_SpongeBurstVFX",
                        new Color(0.95f, 0.08f, 0.62f, 0.88f),
                        true);

                Material projectileMaterial =
                    GetOrCreateMaterial(
                        ProjectileMaterialPath,
                        "M_SpongeImpactProjectile",
                        new Color(0.18f, 0.2f, 0.25f, 1f),
                        false);

                ParticleSystem burstVfxPrefab =
                    GetOrCreateBurstVfxPrefab(burstMaterial);

                SpongeBurstImpactProjectile projectilePrefab =
                    GetOrCreateProjectilePrefab(
                        projectileMaterial);

                foreach (SpongeReservoir reservoir in reservoirs)
                {
                    ConfigureFigure(
                        reservoir,
                        config,
                        puddlePrefab,
                        burstVfxPrefab,
                        projectilePrefab);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Milestone 12] Sünger darbe/patlama " +
                    $"sistemi kuruldu. {reservoirs.Length} Figür " +
                    "güncellendi. Test: Süngeri %90+ doldur, " +
                    "B ile test darbesi gönder.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/12 - Diagnose Sponge Burst")]
        public static void Diagnose()
        {
            SpongeBurstController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    SpongeBurstController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            Debug.Log(
                "[M12 Diagnose] " +
                $"Controllers={controllers.Length}, " +
                $"Playing={Application.isPlaying}");

            foreach (SpongeBurstController controller
                     in controllers)
            {
                SpongeReservoir reservoir =
                    controller.GetComponent<SpongeReservoir>();
                FigureImpactSensor sensor =
                    controller.GetComponentInParent<
                        FigureImpactSensor>();

                Debug.Log(
                    "[M12 Diagnose Figure] " +
                    $"Path={GetHierarchyPath(controller.transform)}, " +
                    $"Enabled={controller.enabled}, " +
                    $"Fill=" +
                    $"{(reservoir != null ? reservoir.NormalizedFill : 0f):F2}, " +
                    $"Instability=" +
                    $"{(reservoir != null ? reservoir.MixtureInstability : 0f):F2}, " +
                    $"Armed={controller.IsBurstArmed}, " +
                    $"LastImpact=" +
                    $"{(sensor != null ? sensor.LastImpactSpeed : 0f):F2}, " +
                    $"Bursts={controller.BurstCount}",
                    controller);
            }
        }

        private static void ConfigureFigure(
            SpongeReservoir reservoir,
            SpongeBurstConfig config,
            SpongeAbsorbablePaintSource puddlePrefab,
            ParticleSystem burstVfxPrefab,
            SpongeBurstImpactProjectile projectilePrefab)
        {
            FigureMotor motor =
                reservoir.GetComponentInParent<FigureMotor>();

            if (motor == null)
            {
                throw new InvalidOperationException(
                    $"{reservoir.name} için FigureMotor bulunamadı.");
            }

            GameObject host = reservoir.gameObject;
            FigureImpactSensor sensor =
                GetOrAddComponent<FigureImpactSensor>(
                    motor.gameObject);
            SpongeBurstFeedback feedback =
                GetOrAddComponent<SpongeBurstFeedback>(host);
            SpongeBurstController burstController =
                GetOrAddComponent<SpongeBurstController>(host);
            SpongeBurstTestLauncher testLauncher =
                GetOrAddComponent<SpongeBurstTestLauncher>(host);

            SetReference(
                feedback,
                "burstParticlePrefab",
                burstVfxPrefab);

            SetReference(
                burstController,
                "reservoir",
                reservoir);
            SetReference(
                burstController,
                "impactSensor",
                sensor);
            SetReference(
                burstController,
                "figureMotor",
                motor);
            SetReference(
                burstController,
                "config",
                config);
            SetReference(
                burstController,
                "feedback",
                feedback);
            SetReference(
                burstController,
                "puddlePrefab",
                puddlePrefab);
            SetInteger(
                burstController,
                "affectedFigureMask",
                Physics.DefaultRaycastLayers);
            SetInteger(
                burstController,
                "spillSurfaceMask",
                Physics.DefaultRaycastLayers);

            SetReference(
                testLauncher,
                "targetFigure",
                motor);
            SetReference(
                testLauncher,
                "projectilePrefab",
                projectilePrefab);

            sensor.enabled = true;
            feedback.enabled = true;
            burstController.enabled = true;
            testLauncher.enabled = true;

            EditorUtility.SetDirty(sensor);
            EditorUtility.SetDirty(feedback);
            EditorUtility.SetDirty(burstController);
            EditorUtility.SetDirty(testLauncher);
            EditorSceneManager.MarkSceneDirty(host.scene);
        }

        private static SpongeAbsorbablePaintSource
            LoadRequiredPuddlePrefab()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PuddlePrefabPath);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "SpongePaintPuddle prefab bulunamadı. " +
                    "Çalışan M11 kurulumunu doğrula.");
            }

            SpongeAbsorbablePaintSource source =
                prefab.GetComponent<SpongeAbsorbablePaintSource>();

            if (source == null)
            {
                throw new InvalidOperationException(
                    "SpongePaintPuddle prefab üzerinde " +
                    "SpongeAbsorbablePaintSource bulunamadı.");
            }

            return source;
        }

        private static SpongeBurstConfig GetOrCreateConfig()
        {
            SpongeBurstConfig existing =
                AssetDatabase.LoadAssetAtPath<SpongeBurstConfig>(
                    ConfigPath);

            if (existing != null)
            {
                return existing;
            }

            var config =
                ScriptableObject.CreateInstance<
                    SpongeBurstConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static ParticleSystem GetOrCreateBurstVfxPrefab(
            Material material)
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    BurstVfxPrefabPath);

            if (existing != null)
            {
                ParticleSystem existingParticle =
                    existing.GetComponent<ParticleSystem>();

                if (existingParticle == null)
                {
                    throw new InvalidOperationException(
                        "VFX_SpongeBurst ParticleSystem içermiyor.");
                }

                return existingParticle;
            }

            var vfxObject = new GameObject("VFX_SpongeBurst");
            ParticleSystem particle =
                vfxObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particle.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.6f;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
            main.startSpeed =
                new ParticleSystem.MinMaxCurve(2.2f, 5.8f);
            main.startSize =
                new ParticleSystem.MinMaxCurve(0.08f, 0.3f);
            main.gravityModifier =
                new ParticleSystem.MinMaxCurve(0.85f);
            main.simulationSpace =
                ParticleSystemSimulationSpace.World;
            main.maxParticles = 180;

            ParticleSystem.EmissionModule emission =
                particle.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(
                new[]
                {
                    new ParticleSystem.Burst(
                        0f,
                        (short)55,
                        (short)90)
                });

            ParticleSystem.ShapeModule shape = particle.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.28f;

            ParticleSystem.ColorOverLifetimeModule color =
                particle.colorOverLifetime;
            color.enabled = true;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(
                        new Color(0.85f, 0.05f, 0.5f),
                        1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color =
                new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer =
                particle.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    vfxObject,
                    BurstVfxPrefabPath);
            UnityEngine.Object.DestroyImmediate(vfxObject);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "VFX_SpongeBurst prefab oluşturulamadı.");
            }

            return prefab.GetComponent<ParticleSystem>();
        }

        private static SpongeBurstImpactProjectile
            GetOrCreateProjectilePrefab(Material material)
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ProjectilePrefabPath);

            if (existing != null)
            {
                SpongeBurstImpactProjectile projectile =
                    existing.GetComponent<
                        SpongeBurstImpactProjectile>();

                if (projectile == null)
                {
                    throw new InvalidOperationException(
                        "SpongeBurstImpactProjectile prefab " +
                        "gerekli test bileşenini içermiyor.");
                }

                return projectile;
            }

            GameObject projectileObject =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "SpongeBurstImpactProjectile";
            projectileObject.transform.localScale =
                Vector3.one * 0.48f;

            Renderer renderer =
                projectileObject.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            Rigidbody body =
                projectileObject.AddComponent<Rigidbody>();
            body.mass = 3.2f;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

            projectileObject.AddComponent<
                SpongeBurstImpactProjectile>();

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    projectileObject,
                    ProjectilePrefabPath);
            UnityEngine.Object.DestroyImmediate(projectileObject);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "SpongeBurstImpactProjectile prefab " +
                    "oluşturulamadı.");
            }

            return prefab.GetComponent<
                SpongeBurstImpactProjectile>();
        }

        private static Material GetOrCreateMaterial(
            string path,
            string materialName,
            Color color,
            bool transparent)
        {
            Material existing =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find(
                transparent
                    ? "Universal Render Pipeline/Particles/Unlit"
                    : "Universal Render Pipeline/Lit");

            if (shader == null)
            {
                shader = Shader.Find(
                    "Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Uygun URP shader bulunamadı.");
            }

            var material = new Material(shader)
            {
                name = materialName
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (transparent)
            {
                material.renderQueue = 3000;

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
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
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
            EnsureFolder(RootFolder, "Art");
            EnsureFolder(RootFolder + "/Art", "Materials");
            EnsureFolder(
                RootFolder + "/Art/Materials",
                "VFX");
            EnsureFolder(
                RootFolder + "/Art/Materials",
                "Tests");
            EnsureFolder(RootFolder, "Data");
            EnsureFolder(RootFolder + "/Data", "Figures");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder + "/Prefabs", "VFX");
            EnsureFolder(
                RootFolder + "/Prefabs/VFX",
                "Tools");
            EnsureFolder(
                RootFolder + "/Prefabs",
                "Tests");
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

        private static string GetHierarchyPath(Transform target)
        {
            string path = target.name;
            Transform parent = target.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
