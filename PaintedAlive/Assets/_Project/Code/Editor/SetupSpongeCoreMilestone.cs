using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint.Sponge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PaintedAlive.EditorTools
{
    public static class SetupSpongeCoreMilestone
    {
        private const string RootFolder = "Assets/_Project";

        private const string ConfigPath =
            RootFolder + "/Data/Figures/SpongeConfig.asset";

        private const string SpongeMaterialPath =
            RootFolder + "/Art/Materials/Tools/M_SpongePrototype.mat";

        private const string PuddleMaterialPath =
            RootFolder + "/Art/Materials/Paint/M_SpongePuddle.mat";

        private const string ParticleMaterialPath =
            RootFolder + "/Art/Materials/VFX/M_SpongeLiquidVFX.mat";

        private const string PuddlePrefabPath =
            RootFolder + "/Prefabs/Paint/SpongePaintPuddle.prefab";

        private const string ParticlePrefabPath =
            RootFolder + "/Prefabs/VFX/Tools/VFX_SpongeLiquid.prefab";

        [MenuItem(
            "Tools/Painted Alive/Milestones/11 - Setup Sponge Core")]
        public static void Run()
        {
            try
            {
                EnsureFolders();
                ValidateRequiredApi();

                SpongeConfig config = GetOrCreateConfig();

                Material spongeMaterial =
                    GetOrCreateUnlitMaterial(
                        SpongeMaterialPath,
                        "M_SpongePrototype",
                        new Color(0.95f, 0.72f, 0.14f, 1f),
                        false);

                Material puddleMaterial =
                    GetOrCreateUnlitMaterial(
                        PuddleMaterialPath,
                        "M_SpongePuddle",
                        new Color(0.12f, 0.48f, 0.92f, 0.72f),
                        true);

                Material particleMaterial =
                    GetOrCreateUnlitMaterial(
                        ParticleMaterialPath,
                        "M_SpongeLiquidVFX",
                        new Color(0.3f, 0.72f, 1f, 0.82f),
                        true);

                SpongeAbsorbablePaintSource puddlePrefab =
                    GetOrCreatePuddlePrefab(puddleMaterial);

                ParticleSystem particlePrefab =
                    GetOrCreateParticlePrefab(particleMaterial);

                PaletteKnifeController[] paletteKnives =
                    UnityEngine.Object.FindObjectsByType<
                        PaletteKnifeController>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None);

                if (paletteKnives.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Sahnede PaletteKnifeController bulunamadı. " +
                        "Çalışan Figür test sahnesini aç.");
                }

                Canvas canvas = GetOrCreateCanvas();

                foreach (PaletteKnifeController paletteKnife
                         in paletteKnives)
                {
                    ConfigureFigure(
                        paletteKnife,
                        canvas,
                        config,
                        spongeMaterial,
                        puddlePrefab,
                        particlePrefab);

                    EditorSceneManager.MarkSceneDirty(
                        paletteKnife.gameObject.scene);
                }

                RepairRoleSwitcherToolOwnership();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[Milestone 11] Sünger çekirdeği kuruldu. " +
                    $"{paletteKnives.Length} Figür güncellendi. " +
                    "Test: 4 ile seç, E basılı tutarak temizle/em, " +
                    "R ile emilen boyayı yüzeye bırak.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void ConfigureFigure(
            PaletteKnifeController paletteKnife,
            Canvas canvas,
            SpongeConfig config,
            Material spongeMaterial,
            SpongeAbsorbablePaintSource puddlePrefab,
            ParticleSystem particlePrefab)
        {
            GameObject host = paletteKnife.gameObject;

            FixativeSprayController fixative =
                host.GetComponent<FixativeSprayController>();

            FrameGunController frameGun =
                host.GetComponent<FrameGunController>();

            FigureToolLoadoutController loadout =
                host.GetComponent<FigureToolLoadoutController>();

            FigureClarityState clarity =
                host.GetComponentInParent<FigureClarityState>();

            FigureMotor motor =
                host.GetComponentInParent<FigureMotor>();

            if (fixative == null ||
                frameGun == null ||
                loadout == null ||
                clarity == null ||
                motor == null)
            {
                throw new InvalidOperationException(
                    $"{host.name} üzerinde M10 Figür bileşenleri " +
                    "eksik. Önce Milestone 10'un çalıştığını doğrula.");
            }

            Camera outputCamera =
                GetReference<Camera>(paletteKnife, "outputCamera");

            InputActionReference useToolAction =
                GetReference<InputActionReference>(
                    paletteKnife,
                    "useToolAction");

            if (outputCamera == null)
            {
                throw new InvalidOperationException(
                    $"{host.name} PaletteKnife outputCamera " +
                    "referansı boş.");
            }

            Transform spongeOrigin =
                GetOrCreateSpongeVisual(
                    outputCamera.transform,
                    spongeMaterial,
                    out Renderer spongeRenderer);

            SpongeReservoir reservoir =
                GetOrAddComponent<SpongeReservoir>(host);
            reservoir.Configure(config);

            SpongeFeedback feedback =
                GetOrAddComponent<SpongeFeedback>(host);

            SetReference(
                feedback,
                "absorbParticlePrefab",
                particlePrefab);
            SetReference(
                feedback,
                "dischargeParticlePrefab",
                particlePrefab);

            SpongeController sponge =
                GetOrAddComponent<SpongeController>(host);

            SpongeHudReferences hud =
                GetOrCreateSpongeHud(canvas, host.name);

            SetReference(sponge, "outputCamera", outputCamera);
            SetReference(sponge, "useToolAction", useToolAction);
            SetReference(sponge, "clarityState", clarity);
            SetReference(sponge, "figureMotor", motor);
            SetReference(sponge, "config", config);
            SetReference(sponge, "reservoir", reservoir);
            SetReference(sponge, "feedback", feedback);
            SetReference(
                sponge,
                "dischargePuddlePrefab",
                puddlePrefab);
            SetReference(
                sponge,
                "spongeRenderer",
                spongeRenderer);
            SetReference(sponge, "hudRoot", hud.Root);
            SetReference(
                sponge,
                "capacityFillImage",
                hud.FillImage);
            SetReference(
                sponge,
                "capacityText",
                hud.CapacityText);
            SetReference(
                sponge,
                "statusText",
                hud.StatusText);

            SetInteger(
                sponge,
                "absorptionMask",
                Physics.DefaultRaycastLayers);
            SetInteger(
                sponge,
                "dischargeSurfaceMask",
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
            SetReference(
                loadout,
                "spongeController",
                sponge);
            SetReference(
                loadout,
                "spongeVisual",
                spongeOrigin.gameObject);

            paletteKnife.enabled = true;
            fixative.enabled = false;
            frameGun.enabled = false;
            sponge.enabled = false;
            spongeOrigin.gameObject.SetActive(false);
            hud.Root.SetActive(false);

            EditorUtility.SetDirty(paletteKnife);
            EditorUtility.SetDirty(fixative);
            EditorUtility.SetDirty(frameGun);
            EditorUtility.SetDirty(loadout);
            EditorUtility.SetDirty(reservoir);
            EditorUtility.SetDirty(feedback);
            EditorUtility.SetDirty(sponge);
        }

        private static Transform GetOrCreateSpongeVisual(
            Transform cameraTransform,
            Material material,
            out Renderer spongeRenderer)
        {
            Transform origin = cameraTransform.Find("SpongeOrigin");

            if (origin == null)
            {
                var originObject = new GameObject("SpongeOrigin");
                Undo.RegisterCreatedObjectUndo(
                    originObject,
                    "Create Sponge Origin");
                origin = originObject.transform;
                origin.SetParent(cameraTransform, false);
            }

            origin.localPosition =
                new Vector3(0.26f, -0.22f, 0.5f);
            origin.localRotation = Quaternion.identity;
            origin.localScale = Vector3.one;

            Transform visual = origin.Find("SpongePrototypeVisual");

            if (visual == null)
            {
                GameObject visualObject =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObject.name = "SpongePrototypeVisual";
                Undo.RegisterCreatedObjectUndo(
                    visualObject,
                    "Create Sponge Prototype Visual");
                visual = visualObject.transform;
                visual.SetParent(origin, false);

                Collider collider =
                    visualObject.GetComponent<Collider>();

                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }

            visual.localPosition = Vector3.zero;
            visual.localRotation =
                Quaternion.Euler(10f, -18f, 6f);
            visual.localScale =
                new Vector3(0.3f, 0.14f, 0.2f);

            spongeRenderer = visual.GetComponent<Renderer>();

            if (spongeRenderer == null)
            {
                throw new InvalidOperationException(
                    "SpongePrototypeVisual Renderer içermiyor.");
            }

            spongeRenderer.sharedMaterial = material;
            return origin;
        }

        private static SpongeHudReferences GetOrCreateSpongeHud(
            Canvas canvas,
            string ownerName)
        {
            string safeOwnerName = ownerName.Replace(" ", "_");
            string rootName = $"SpongeHUD_{safeOwnerName}";
            Transform existing = canvas.transform.Find(rootName);
            GameObject root;

            if (existing != null)
            {
                root = existing.gameObject;
            }
            else
            {
                root = new GameObject(
                    rootName,
                    typeof(RectTransform),
                    typeof(Image));
                Undo.RegisterCreatedObjectUndo(
                    root,
                    "Create Sponge HUD");
                root.transform.SetParent(canvas.transform, false);
            }

            RectTransform rootRect =
                root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 0f);
            rootRect.anchorMax = new Vector2(1f, 0f);
            rootRect.pivot = new Vector2(1f, 0f);
            rootRect.anchoredPosition = new Vector2(-24f, 24f);
            rootRect.sizeDelta = new Vector2(292f, 74f);

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.025f, 0.035f, 0.055f, 0.92f);

            Text title = GetOrCreateText(
                root.transform,
                "Title",
                "SÜNGER",
                13,
                TextAnchor.UpperLeft,
                new Color(1f, 0.83f, 0.32f, 1f));
            SetRect(
                title.rectTransform,
                new Vector2(10f, -6f),
                new Vector2(82f, 22f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

            Text capacityText = GetOrCreateText(
                root.transform,
                "CapacityText",
                "0 / 100",
                13,
                TextAnchor.UpperRight,
                Color.white);
            SetRect(
                capacityText.rectTransform,
                new Vector2(-10f, -6f),
                new Vector2(110f, 22f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f));

            Image track = GetOrCreateImage(
                root.transform,
                "CapacityTrack",
                new Color(0.08f, 0.1f, 0.14f, 1f));
            SetRect(
                track.rectTransform,
                new Vector2(10f, -30f),
                new Vector2(272f, 13f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

            Image fill = GetOrCreateImage(
                track.transform,
                "Fill",
                new Color(0.95f, 0.72f, 0.14f, 1f));
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;

            Text status = GetOrCreateText(
                root.transform,
                "StatusText",
                "YOK  •  KARIŞIM %0",
                11,
                TextAnchor.LowerLeft,
                new Color(0.72f, 0.78f, 0.86f, 1f));
            SetRect(
                status.rectTransform,
                new Vector2(10f, 5f),
                new Vector2(272f, 20f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f));

            return new SpongeHudReferences(
                root,
                fill,
                capacityText,
                status);
        }

        private static Canvas GetOrCreateCanvas()
        {
            Canvas[] canvases =
                UnityEngine.Object.FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (canvases.Length > 0)
            {
                return canvases[0];
            }

            var canvasObject = new GameObject(
                "FigureCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Undo.RegisterCreatedObjectUndo(
                canvasObject,
                "Create Figure Canvas");

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static SpongeConfig GetOrCreateConfig()
        {
            SpongeConfig existing =
                AssetDatabase.LoadAssetAtPath<SpongeConfig>(ConfigPath);

            if (existing != null)
            {
                return existing;
            }

            var created =
                ScriptableObject.CreateInstance<SpongeConfig>();
            AssetDatabase.CreateAsset(created, ConfigPath);
            return created;
        }

        private static SpongeAbsorbablePaintSource
            GetOrCreatePuddlePrefab(Material material)
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PuddlePrefabPath);

            if (existing != null)
            {
                SpongeAbsorbablePaintSource source =
                    existing.GetComponent<
                        SpongeAbsorbablePaintSource>();

                if (source == null)
                {
                    throw new InvalidOperationException(
                        "SpongePaintPuddle prefab emilebilir " +
                        "kaynak bileşeni içermiyor.");
                }

                BoxCollider existingCollider =
                    existing.GetComponent<BoxCollider>();
                Renderer existingRenderer =
                    existing.GetComponent<Renderer>();

                if (existingCollider == null ||
                    existingRenderer == null)
                {
                    throw new InvalidOperationException(
                        "SpongePaintPuddle prefab Renderer ve " +
                        "BoxCollider içermelidir.");
                }

                ConfigurePuddleCollider(existingCollider);
                existingRenderer.sharedMaterial = material;
                SetReference(
                    source,
                    "targetRenderer",
                    existingRenderer);
                SetReference(
                    source,
                    "targetCollider",
                    existingCollider);
                EditorUtility.SetDirty(existingCollider);
                EditorUtility.SetDirty(existingRenderer);
                EditorUtility.SetDirty(source);

                return source;
            }

            GameObject puddle =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddle.name = "SpongePaintPuddle";
            puddle.transform.localScale =
                new Vector3(1.15f, 0.035f, 1.15f);

            Collider primitiveCollider =
                puddle.GetComponent<Collider>();
            UnityEngine.Object.DestroyImmediate(primitiveCollider);

            BoxCollider boxCollider =
                puddle.AddComponent<BoxCollider>();
            ConfigurePuddleCollider(boxCollider);

            Renderer renderer = puddle.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            SpongeAbsorbablePaintSource sourceComponent =
                puddle.AddComponent<SpongeAbsorbablePaintSource>();
            SetReference(
                sourceComponent,
                "targetRenderer",
                renderer);
            SetReference(
                sourceComponent,
                "targetCollider",
                boxCollider);

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    puddle,
                    PuddlePrefabPath);
            UnityEngine.Object.DestroyImmediate(puddle);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "SpongePaintPuddle prefab oluşturulamadı.");
            }

            return prefab.GetComponent<
                SpongeAbsorbablePaintSource>();
        }

        private static void ConfigurePuddleCollider(
            BoxCollider collider)
        {
            collider.center = new Vector3(0f, 2.5f, 0f);
            collider.size = new Vector3(1f, 5f, 1f);
            collider.isTrigger = false;
        }

        private static ParticleSystem GetOrCreateParticlePrefab(
            Material material)
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ParticlePrefabPath);

            if (existing != null)
            {
                ParticleSystem existingParticle =
                    existing.GetComponent<ParticleSystem>();

                if (existingParticle == null)
                {
                    throw new InvalidOperationException(
                        "VFX_SpongeLiquid ParticleSystem içermiyor.");
                }

                return existingParticle;
            }

            var particleObject =
                new GameObject("VFX_SpongeLiquid");
            ParticleSystem particle =
                particleObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particle.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.42f;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSpeed =
                new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
            main.startSize =
                new ParticleSystem.MinMaxCurve(0.03f, 0.11f);
            main.simulationSpace =
                ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission =
                particle.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(
                new[]
                {
                    new ParticleSystem.Burst(
                        0f,
                        (short)7,
                        (short)13)
                });

            ParticleSystem.ShapeModule shape = particle.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f;

            ParticleSystem.ColorOverLifetimeModule color =
                particle.colorOverLifetime;
            color.enabled = true;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(
                        new Color(0.35f, 0.65f, 1f),
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
            particleRenderer.sharedMaterial = material;

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    particleObject,
                    ParticlePrefabPath);
            UnityEngine.Object.DestroyImmediate(particleObject);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "VFX_SpongeLiquid prefab oluşturulamadı.");
            }

            return prefab.GetComponent<ParticleSystem>();
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

            Shader shader = Shader.Find(
                "Universal Render Pipeline/Particles/Unlit");

            if (shader == null)
            {
                shader = Shader.Find(
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

        private static Text GetOrCreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            Transform existing = parent.Find(name);
            Text text;

            if (existing != null)
            {
                text = existing.GetComponent<Text>();
            }
            else
            {
                var textObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(Text));
                textObject.transform.SetParent(parent, false);
                text = textObject.GetComponent<Text>();
            }

            if (text == null)
            {
                throw new InvalidOperationException(
                    $"{name} Text bileşeni oluşturulamadı.");
            }

            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Image GetOrCreateImage(
            Transform parent,
            string name,
            Color color)
        {
            Transform existing = parent.Find(name);
            Image image;

            if (existing != null)
            {
                image = existing.GetComponent<Image>();
            }
            else
            {
                var imageObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(Image));
                imageObject.transform.SetParent(parent, false);
                image = imageObject.GetComponent<Image>();
            }

            if (image == null)
            {
                throw new InvalidOperationException(
                    $"{name} Image bileşeni oluşturulamadı.");
            }

            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void ValidateRequiredApi()
        {
            if (typeof(FigureClarityState).GetMethod(
                    "RestoreAmount") == null)
            {
                throw new InvalidOperationException(
                    "FigureClarityState.RestoreAmount bulunamadı. " +
                    "M11 replacement dosyasını kopyala.");
            }

            if (typeof(FigureClarityState).GetMethod(
                    "RestoreClarity") == null)
            {
                throw new InvalidOperationException(
                    "FigureClarityState.RestoreClarity bulunamadı. " +
                    "Temiz Pigment kurtarma sistemi için güncel M11 " +
                    "replacement dosyasını kopyala.");
            }

            if (typeof(FigureMotor).GetMethod(
                    "SetEquipmentMovementMultiplier") == null)
            {
                throw new InvalidOperationException(
                    "FigureMotor.SetEquipmentMovementMultiplier " +
                    "bulunamadı. M11 replacement dosyasını kopyala.");
            }
        }

        private static void RepairRoleSwitcherToolOwnership()
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            FigureToolLoadoutController[] loadouts =
                UnityEngine.Object.FindObjectsByType<
                    FigureToolLoadoutController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            bool foundRoleSwitcher = false;

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null ||
                    behaviour.GetType().Name !=
                    "PrototypeRoleSwitcher")
                {
                    continue;
                }

                foundRoleSwitcher = true;
                var serializedObject =
                    new SerializedObject(behaviour);
                SerializedProperty figureBehaviours =
                    serializedObject.FindProperty(
                        "figureBehaviours");

                if (figureBehaviours == null ||
                    !figureBehaviours.isArray)
                {
                    Debug.LogWarning(
                        "PrototypeRoleSwitcher.figureBehaviours " +
                        "dizisi bulunamadı; araç sahipliği otomatik " +
                        "onarılmadı.",
                        behaviour);
                    continue;
                }

                Undo.RecordObject(
                    behaviour,
                    "Repair Figure Tool Ownership");

                for (int i = figureBehaviours.arraySize - 1;
                     i >= 0;
                     i--)
                {
                    SerializedProperty element =
                        figureBehaviours.GetArrayElementAtIndex(i);
                    UnityEngine.Object reference =
                        element.objectReferenceValue;

                    if (!IsIndividuallyManagedTool(reference))
                    {
                        continue;
                    }

                    int previousSize =
                        figureBehaviours.arraySize;
                    figureBehaviours.DeleteArrayElementAtIndex(i);

                    if (figureBehaviours.arraySize == previousSize)
                    {
                        figureBehaviours.DeleteArrayElementAtIndex(i);
                    }
                }

                foreach (FigureToolLoadoutController loadout
                         in loadouts)
                {
                    if (loadout == null ||
                        loadout.gameObject.scene !=
                        behaviour.gameObject.scene ||
                        ContainsReference(
                            figureBehaviours,
                            loadout))
                    {
                        continue;
                    }

                    int index = figureBehaviours.arraySize;
                    figureBehaviours.InsertArrayElementAtIndex(index);
                    figureBehaviours.GetArrayElementAtIndex(index)
                        .objectReferenceValue = loadout;
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(behaviour);
            }

            if (!foundRoleSwitcher)
            {
                Debug.LogWarning(
                    "PrototypeRoleSwitcher bulunamadı. Loadout kendi " +
                    "durumunu yine koruyacak; rol dizisi otomatik " +
                    "temizlenmedi.");
            }
        }

        private static bool IsIndividuallyManagedTool(
            UnityEngine.Object reference)
        {
            return reference is PaletteKnifeController ||
                   reference is FixativeSprayController ||
                   reference is FrameGunController ||
                   reference is SpongeController;
        }

        private static bool ContainsReference(
            SerializedProperty array,
            UnityEngine.Object reference)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i)
                        .objectReferenceValue == reference)
                {
                    return true;
                }
            }

            return false;
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
                    "alanı bulunamadı. M11 replacement " +
                    "dosyalarının tamamını kopyala.");
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
                "Tools");
            EnsureFolder(
                RootFolder + "/Art/Materials",
                "Paint");
            EnsureFolder(
                RootFolder + "/Art/Materials",
                "VFX");
            EnsureFolder(RootFolder, "Data");
            EnsureFolder(RootFolder + "/Data", "Figures");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder + "/Prefabs", "Paint");
            EnsureFolder(RootFolder + "/Prefabs", "VFX");
            EnsureFolder(
                RootFolder + "/Prefabs/VFX",
                "Tools");
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

        private readonly struct SpongeHudReferences
        {
            public SpongeHudReferences(
                GameObject root,
                Image fillImage,
                Text capacityText,
                Text statusText)
            {
                Root = root;
                FillImage = fillImage;
                CapacityText = capacityText;
                StatusText = statusText;
            }

            public GameObject Root { get; }
            public Image FillImage { get; }
            public Text CapacityText { get; }
            public Text StatusText { get; }
        }
    }
}
