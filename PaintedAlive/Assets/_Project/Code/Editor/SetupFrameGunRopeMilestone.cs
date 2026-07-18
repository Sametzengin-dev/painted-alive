using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace PaintedAlive.EditorTools
{
    public static class SetupFrameGunRopeMilestone
    {
        private const string RootFolder = "Assets/_Project";

        private const string ConfigPath =
            RootFolder +
            "/Data/Figures/FrameGunConfig.asset";

        private const string RopeMaterialPath =
            RootFolder +
            "/Art/Materials/Tools/M_FrameGunRope.mat";

        private const string MarkerMaterialPath =
            RootFolder +
            "/Art/Materials/Tools/M_FrameGunAnchor.mat";

        private const string ImpactMaterialPath =
            RootFolder +
            "/Art/Materials/VFX/M_FrameGunImpactParticles.mat";

        private const string AnchorPrefabPath =
            RootFolder +
            "/Prefabs/Tools/FrameGunAnchor.prefab";

        private const string ImpactParticlePath =
            RootFolder +
            "/Prefabs/VFX/Tools/VFX_FrameGunImpact.prefab";

        [MenuItem(
            "Tools/Painted Alive/Milestones/09 - Setup Frame Gun Rope")]
        public static void Run()
        {
            try
            {
                EnsureFolders();

                ValidateFigureMotorApi();

                FrameGunConfig config =
                    GetOrCreateConfig();

                Material ropeMaterial =
                    GetOrCreateUnlitMaterial(
                        RopeMaterialPath,
                        "M_FrameGunRope",
                        new Color(0.3f, 0.12f, 0.035f, 1f),
                        false);

                Material markerMaterial =
                    GetOrCreateUnlitMaterial(
                        MarkerMaterialPath,
                        "M_FrameGunAnchor",
                        new Color(1f, 0.48f, 0.06f, 1f),
                        false);

                Material impactMaterial =
                    GetOrCreateUnlitMaterial(
                        ImpactMaterialPath,
                        "M_FrameGunImpactParticles",
                        new Color(1f, 0.62f, 0.12f, 0.85f),
                        true);

                GameObject anchorPrefab =
                    GetOrCreateAnchorPrefab(markerMaterial);

                ParticleSystem impactParticle =
                    GetOrCreateImpactParticle(impactMaterial);

                PaletteKnifeController[] paletteKnives =
                    UnityEngine.Object.FindObjectsByType<
                        PaletteKnifeController>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None);

                if (paletteKnives.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Sahnede PaletteKnifeController bulunamadı. " +
                        "Figür test sahnesini aç.");
                }

                foreach (PaletteKnifeController paletteKnife
                         in paletteKnives)
                {
                    ConfigureFigure(
                        paletteKnife,
                        config,
                        ropeMaterial,
                        anchorPrefab,
                        impactParticle);

                    EditorSceneManager.MarkSceneDirty(
                        paletteKnife.gameObject.scene);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Milestone 09] Çerçeve Tabancası, ankraj ve " +
                    $"halat kurulumu tamamlandı. {paletteKnives.Length} " +
                    "Figür bağlandı. Test: 3 ile seç, E ile ankraj " +
                    "at, E ile bırak.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void ConfigureFigure(
            PaletteKnifeController paletteKnife,
            FrameGunConfig config,
            Material ropeMaterial,
            GameObject anchorPrefab,
            ParticleSystem impactParticle)
        {
            GameObject host = paletteKnife.gameObject;

            FixativeSprayController fixative =
                host.GetComponent<FixativeSprayController>();

            FigureToolLoadoutController loadout =
                host.GetComponent<
                    FigureToolLoadoutController>();

            FigureMotor figureMotor =
                host.GetComponentInParent<FigureMotor>();

            if (fixative == null || loadout == null)
            {
                throw new InvalidOperationException(
                    $"{host.name} üzerinde Milestone 08 tool " +
                    "bileşenleri eksik. Önce Milestone 08 " +
                    "kurulumunu tamamla.");
            }

            if (figureMotor == null)
            {
                throw new InvalidOperationException(
                    $"{host.name} için FigureMotor bulunamadı.");
            }

            Camera outputCamera =
                GetReference<Camera>(
                    paletteKnife,
                    "outputCamera");

            InputActionReference useToolAction =
                GetReference<InputActionReference>(
                    paletteKnife,
                    "useToolAction");

            FigureClarityState clarityState =
                GetReference<FigureClarityState>(
                    paletteKnife,
                    "clarityState");

            Transform frameGunOrigin =
                GetOrCreateFrameGunOrigin(outputCamera, host.transform);

            FrameGunRopeVisual ropeVisual =
                GetOrCreateRopeVisual(host, ropeMaterial);

            FrameGunFeedback feedback =
                GetOrAddComponent<FrameGunFeedback>(host);

            SetReference(
                feedback,
                "anchorImpactParticlePrefab",
                impactParticle);

            FrameGunController frameGun =
                GetOrAddComponent<FrameGunController>(host);

            SetReference(frameGun, "outputCamera", outputCamera);
            SetReference(frameGun, "muzzle", frameGunOrigin);
            SetReference(frameGun, "ropeSocket", frameGunOrigin);
            SetReference(frameGun, "useToolAction", useToolAction);
            SetReference(frameGun, "clarityState", clarityState);
            SetReference(frameGun, "figureMotor", figureMotor);
            SetReference(frameGun, "config", config);
            SetReference(frameGun, "ropeVisual", ropeVisual);
            SetReference(frameGun, "feedback", feedback);
            SetReference(frameGun, "anchorMarkerPrefab", anchorPrefab);

            SetInteger(
                frameGun,
                "anchorSurfaceMask",
                Physics.DefaultRaycastLayers);

            SetReference(
                loadout,
                "paletteKnifeController",
                paletteKnife);

            SetReference(
                loadout,
                "fixativeSprayController",
                fixative);

            SetReference(
                loadout,
                "frameGunController",
                frameGun);

            paletteKnife.enabled = true;
            fixative.enabled = false;
            frameGun.enabled = false;

            ropeVisual.SetVisible(false);

            EditorUtility.SetDirty(paletteKnife);
            EditorUtility.SetDirty(fixative);
            EditorUtility.SetDirty(frameGun);
            EditorUtility.SetDirty(feedback);
            EditorUtility.SetDirty(loadout);
            EditorUtility.SetDirty(ropeVisual);
        }

        private static Transform GetOrCreateFrameGunOrigin(
            Camera outputCamera,
            Transform fallbackParent)
        {
            Transform parent =
                outputCamera != null
                    ? outputCamera.transform
                    : fallbackParent;

            Transform existing =
                parent.Find("FrameGunOrigin");

            if (existing != null)
            {
                return existing;
            }

            var originObject =
                new GameObject("FrameGunOrigin");

            Undo.RegisterCreatedObjectUndo(
                originObject,
                "Create Frame Gun Origin");

            Transform origin = originObject.transform;
            origin.SetParent(parent, false);
            origin.localPosition =
                new Vector3(-0.22f, -0.18f, 0.42f);

            origin.localRotation = Quaternion.identity;
            origin.localScale = Vector3.one;
            return origin;
        }

        private static FrameGunRopeVisual GetOrCreateRopeVisual(
            GameObject host,
            Material ropeMaterial)
        {
            Transform existing =
                host.transform.Find("FrameGunRope");

            GameObject ropeObject;

            if (existing != null)
            {
                ropeObject = existing.gameObject;
            }
            else
            {
                ropeObject = new GameObject("FrameGunRope");

                Undo.RegisterCreatedObjectUndo(
                    ropeObject,
                    "Create Frame Gun Rope");

                ropeObject.transform.SetParent(
                    host.transform,
                    false);
            }

            LineRenderer lineRenderer =
                GetOrAddComponent<LineRenderer>(ropeObject);

            lineRenderer.useWorldSpace = true;
            lineRenderer.alignment =
                LineAlignment.View;

            lineRenderer.textureMode =
                LineTextureMode.Stretch;

            lineRenderer.numCornerVertices = 2;
            lineRenderer.numCapVertices = 2;
            lineRenderer.sharedMaterial = ropeMaterial;
            lineRenderer.enabled = false;

            FrameGunRopeVisual visual =
                GetOrAddComponent<
                    FrameGunRopeVisual>(ropeObject);

            SetReference(
                visual,
                "lineRenderer",
                lineRenderer);

            return visual;
        }

        private static FrameGunConfig GetOrCreateConfig()
        {
            FrameGunConfig existing =
                AssetDatabase.LoadAssetAtPath<
                    FrameGunConfig>(ConfigPath);

            if (existing != null)
            {
                return existing;
            }

            var created =
                ScriptableObject.CreateInstance<FrameGunConfig>();

            AssetDatabase.CreateAsset(created, ConfigPath);
            return created;
        }

        private static Material GetOrCreateUnlitMaterial(
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

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Particles/Unlit");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "URP Unlit shader bulunamadı.");
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

        private static GameObject GetOrCreateAnchorPrefab(
            Material markerMaterial)
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    AnchorPrefabPath);

            if (existing != null)
            {
                return existing;
            }

            GameObject anchor =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);

            anchor.name = "FrameGunAnchor";
            anchor.transform.localScale =
                Vector3.one * 0.16f;

            Collider collider = anchor.GetComponent<Collider>();

            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            Renderer renderer = anchor.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = markerMaterial;
            }

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    anchor,
                    AnchorPrefabPath);

            UnityEngine.Object.DestroyImmediate(anchor);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "FrameGunAnchor prefab oluşturulamadı.");
            }

            return prefab;
        }

        private static ParticleSystem GetOrCreateImpactParticle(
            Material impactMaterial)
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ImpactParticlePath);

            if (existing != null)
            {
                ParticleSystem existingParticle =
                    existing.GetComponent<ParticleSystem>();

                if (existingParticle == null)
                {
                    throw new InvalidOperationException(
                        "VFX_FrameGunImpact ParticleSystem içermiyor.");
                }

                return existingParticle;
            }

            var particleObject =
                new GameObject("VFX_FrameGunImpact");

            ParticleSystem particle =
                particleObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particle.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.45f;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(0.18f, 0.42f);

            main.startSpeed =
                new ParticleSystem.MinMaxCurve(0.8f, 2.4f);

            main.startSize =
                new ParticleSystem.MinMaxCurve(0.025f, 0.09f);

            main.simulationSpace =
                ParticleSystemSimulationSpace.World;

            main.maxParticles = 48;

            ParticleSystem.EmissionModule emission =
                particle.emission;

            emission.rateOverTime = 0f;
            emission.SetBursts(
                new[]
                {
                    new ParticleSystem.Burst(
                        0f,
                        (short)8,
                        (short)14)
                });

            ParticleSystem.ShapeModule shape = particle.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.09f;

            ParticleSystem.ColorOverLifetimeModule color =
                particle.colorOverLifetime;

            color.enabled = true;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(1f, 0.75f, 0.18f),
                        0f),
                    new GradientColorKey(
                        new Color(0.45f, 0.08f, 0.01f),
                        1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });

            color.color =
                new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer particleRenderer =
                particle.GetComponent<ParticleSystemRenderer>();

            particleRenderer.sharedMaterial = impactMaterial;

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    particleObject,
                    ImpactParticlePath);

            UnityEngine.Object.DestroyImmediate(particleObject);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "VFX_FrameGunImpact prefab oluşturulamadı.");
            }

            return prefab.GetComponent<ParticleSystem>();
        }

        private static void ValidateFigureMotorApi()
        {
            if (typeof(FigureMotor).GetMethod(
                    "AddExternalImpulse") == null)
            {
                throw new InvalidOperationException(
                    "FigureMotor.AddExternalImpulse bulunamadı. " +
                    "Milestone 07 kurulumu eksik.");
            }
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
                    "alanı bulunamadı. FigureToolLoadoutController " +
                    "replacement dosyasını kopyaladığını kontrol et.");
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
            EnsureFolder(RootFolder + "/Data", "Figures");
            EnsureFolder(RootFolder, "Art");
            EnsureFolder(RootFolder + "/Art", "Materials");
            EnsureFolder(RootFolder + "/Art/Materials", "Tools");
            EnsureFolder(RootFolder + "/Art/Materials", "VFX");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder + "/Prefabs", "Tools");
            EnsureFolder(RootFolder + "/Prefabs", "VFX");
            EnsureFolder(RootFolder + "/Prefabs/VFX", "Tools");
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
