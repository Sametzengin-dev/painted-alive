#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaintedAlive.EditorTools
{
    public static class SetupPaintMoundSystem
    {
        private const string InputActionsPath =
            "Assets/InputSystem_Actions.inputactions";

        private const string DataFolder =
            "Assets/_Project/Data/Painters";

        private const string MaterialsFolder =
            "Assets/_Project/Materials";

        private const string ConfigAssetPath =
            DataFolder +
            "/DA_OilPainterPaintMound_Default.asset";

        private const string ActionReferencePath =
            DataFolder +
            "/IA_Player_PaintMound.asset";

        private const string PreviewMaterialPath =
            MaterialsFolder +
            "/MAT_PaintMoundPreview.mat";

        private const string PaintRuntimeName =
            "PaintRuntime";

        private const string MoundsRootName =
            "PaintMounds";

        private const string PainterHudName =
            "PainterHUD";

        private const string PanelName =
            "PaintMoundPanel";

        private const bool AddGamepadBinding = true;

        [MenuItem(
            "Tools/Painted Alive/Painters/Setup Paint Mound System")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning(
                    "Paint Mound kurulumu Play Mode dışında çalıştırılmalıdır.");

                return;
            }

            if (EditorApplication.isCompiling)
            {
                Debug.LogWarning(
                    "Unity şu anda derleme yapıyor. Derleme bittikten sonra tekrar çalıştır.");

                return;
            }

            int undoGroup =
                Undo.GetCurrentGroup();

            Undo.SetCurrentGroupName(
                "Setup Paint Mound System");

            try
            {
                SetupContext context =
                    ValidateAndCreateContext();

                EnsureFolder(DataFolder);
                EnsureFolder(MaterialsFolder);

                InputActionReference moundActionReference =
                    CreateOrUpdatePaintMoundAction(
                        context.InputActions);

                ScriptableObject config =
                    CreateOrUpdateConfig(
                        context.ConfigType);

                Material previewMaterial =
                    CreateOrUpdatePreviewMaterial();

                RuntimeSetup runtime =
                    SetupRuntime(
                        context,
                        config,
                        previewMaterial,
                        moundActionReference);

                Component hud =
                    SetupHud(
                        context,
                        runtime);

                AddPainterBehaviour(
                    context.RoleSwitcher,
                    runtime.Controller);

                AddPainterBehaviour(
                    context.RoleSwitcher,
                    hud);

                EditorUtility.SetDirty(
                    context.RoleSwitcher);

                EditorSceneManager.MarkSceneDirty(
                    SceneManager.GetActiveScene());

                AssetDatabase.SaveAssets();

                Debug.Log(
                    "Paint Mound sistemi başarıyla kuruldu.\n\n" +
                    "Oluşturulanlar:\n" +
                    "• Player/PaintMound action\n" +
                    "• Paint Mound config\n" +
                    "• Preview materyali\n" +
                    "• PaintMounds root\n" +
                    "• Runtime componentleri\n" +
                    "• PaintMoundPanel HUD\n" +
                    "• Painter Behaviours bağlantıları\n\n" +
                    "Sahneyi Ctrl+S ile kaydet.",
                    context.PaintRuntime.gameObject);

                Selection.activeGameObject =
                    context.PaintRuntime.gameObject;

                EditorGUIUtility.PingObject(
                    context.PaintRuntime.gameObject);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "Paint Mound kurulamadı",
                    exception.Message,
                    "Tamam");
            }
            finally
            {
                Undo.CollapseUndoOperations(
                    undoGroup);
            }
        }

        // =========================================================
        // PREFLIGHT
        // =========================================================

        private static SetupContext ValidateAndCreateContext()
        {
            InputActionAsset inputActions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    InputActionsPath);

            if (inputActions == null)
            {
                throw new InvalidOperationException(
                    $"Input Actions asseti bulunamadı:\n{InputActionsPath}");
            }

            InputActionMap playerMap =
                inputActions.FindActionMap(
                    "Player",
                    false);

            if (playerMap == null)
            {
                throw new InvalidOperationException(
                    "InputSystem_Actions.inputactions içinde " +
                    "'Player' Action Map bulunamadı.");
            }

            Transform paintRuntime =
                FindSceneTransform(
                    PaintRuntimeName);

            if (paintRuntime == null)
            {
                throw new InvalidOperationException(
                    "Hierarchy içinde PaintRuntime bulunamadı.");
            }

            Component oilStrokeSystem =
                FindComponentOnObject(
                    paintRuntime.gameObject,
                    "OilStrokeSystem");

            Component brushController =
                FindComponentOnObject(
                    paintRuntime.gameObject,
                    "PainterBrushController");

            Component pigmentReservoir =
                FindComponentOnObject(
                    paintRuntime.gameObject,
                    "PainterPigmentReservoir");

            if (oilStrokeSystem == null)
            {
                throw new InvalidOperationException(
                    "PaintRuntime üzerinde OilStrokeSystem bulunamadı.");
            }

            if (brushController == null)
            {
                throw new InvalidOperationException(
                    "PaintRuntime üzerinde PainterBrushController bulunamadı.");
            }

            if (pigmentReservoir == null)
            {
                throw new InvalidOperationException(
                    "PaintRuntime üzerinde PainterPigmentReservoir bulunamadı.");
            }

            Component roleSwitcher =
                FindSceneComponent(
                    "PrototypeRoleSwitcher");

            if (roleSwitcher == null)
            {
                throw new InvalidOperationException(
                    "Sahnede PrototypeRoleSwitcher bulunamadı.");
            }

            Transform painterHud =
                FindPainterHud();

            if (painterHud == null)
            {
                throw new InvalidOperationException(
                    "Canvas altında PainterHUD bulunamadı.");
            }

            Type configType =
                FindScriptableObjectType(
                    "PaintMoundConfig",
                    "PainterPaintMoundConfig",
                    "OilPaintMoundConfig");

            if (configType == null)
            {
                throw new InvalidOperationException(
                    "Paint Mound Config sınıfı bulunamadı.\n" +
                    "Beklenen isimlerden biri:\n" +
                    "• PaintMoundConfig\n" +
                    "• PainterPaintMoundConfig");
            }

            Type systemType =
                FindMonoBehaviourType(
                    "PaintMoundSystem");

            Type controllerType =
                FindMonoBehaviourType(
                    "PainterPaintMoundController");

            Type hudType =
                FindMonoBehaviourType(
                    "PainterPaintMoundHud");

            if (systemType == null)
            {
                throw new InvalidOperationException(
                    "PaintMoundSystem sınıfı bulunamadı.");
            }

            if (controllerType == null)
            {
                throw new InvalidOperationException(
                    "PainterPaintMoundController sınıfı bulunamadı.");
            }

            if (hudType == null)
            {
                throw new InvalidOperationException(
                    "PainterPaintMoundHud sınıfı bulunamadı.");
            }

            return new SetupContext
            {
                InputActions = inputActions,
                PlayerMap = playerMap,
                PaintRuntime = paintRuntime,
                PainterHud = painterHud,
                OilStrokeSystem = oilStrokeSystem,
                BrushController = brushController,
                PigmentReservoir = pigmentReservoir,
                RoleSwitcher = roleSwitcher,
                ConfigType = configType,
                SystemType = systemType,
                ControllerType = controllerType,
                HudType = hudType
            };
        }

        // =========================================================
        // INPUT ACTION
        // =========================================================

        private static InputActionReference
    CreateOrUpdatePaintMoundAction(
        InputActionAsset inputActions)
{
    InputActionMap playerMap =
        inputActions.FindActionMap(
            "Player",
            true);

    InputAction action =
        playerMap.FindAction(
            "PaintMound",
            false);

    if (action == null)
    {
        action =
            playerMap.AddAction(
                "PaintMound",
                InputActionType.Button,
                expectedControlLayout: "Button");
    }
    else if (action.type !=
             InputActionType.Button)
    {
        Debug.LogWarning(
            "Player/PaintMound action mevcut ancak " +
            "Action Type değeri Button değil.");
    }

    EnsureBinding(
        action,
        "<Keyboard>/3");

    if (AddGamepadBinding)
    {
        EnsureBinding(
            action,
            "<Gamepad>/dpad/up");
    }

    string json =
        inputActions.ToJson();

    File.WriteAllText(
        InputActionsPath,
        json,
        new UTF8Encoding(false));

    AssetDatabase.ImportAsset(
        InputActionsPath,
        ImportAssetOptions.ForceUpdate);

    InputActionAsset reloadedAsset =
        AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            InputActionsPath);

    if (reloadedAsset == null)
    {
        throw new InvalidOperationException(
            "Input Action asseti kaydedildikten sonra " +
            "yeniden yüklenemedi.");
    }

    InputAction reloadedAction =
        reloadedAsset.FindAction(
            "Player/PaintMound",
            true);

    if (reloadedAction == null)
    {
        throw new InvalidOperationException(
            "Player/PaintMound action kaydedildikten sonra bulunamadı.");
    }

    string expectedAssetName =
        Path.GetFileNameWithoutExtension(
            ActionReferencePath);

    UnityEngine.Object existingMainAsset =
        AssetDatabase.LoadMainAssetAtPath(
            ActionReferencePath);

    InputActionReference reference =
        existingMainAsset as InputActionReference;

    if (existingMainAsset != null &&
        reference == null)
    {
        throw new InvalidOperationException(
            $"Şu yolda farklı tipte bir asset mevcut:\n" +
            $"{ActionReferencePath}\n\n" +
            "Bu asseti silip kurulumu tekrar çalıştır.");
    }

    if (reference == null)
    {
        reference =
            ScriptableObject.CreateInstance<
                InputActionReference>();

        reference.Set(
            reloadedAsset,
            "Player",
            "PaintMound");

        // Ana nesne adı dosya adıyla aynı olmalı.
        reference.name =
            expectedAssetName;

        AssetDatabase.CreateAsset(
            reference,
            ActionReferencePath);
    }
    else
    {
        Undo.RecordObject(
            reference,
            "Update Paint Mound Action Reference");

        reference.Set(
            reloadedAsset,
            "Player",
            "PaintMound");

        reference.name =
            expectedAssetName;

        EditorUtility.SetDirty(
            reference);
    }

    AssetDatabase.SaveAssets();

    AssetDatabase.ImportAsset(
        ActionReferencePath,
        ImportAssetOptions.ForceUpdate);

    InputActionReference savedReference =
        AssetDatabase.LoadAssetAtPath<
            InputActionReference>(
            ActionReferencePath);

    if (savedReference == null)
    {
        throw new InvalidOperationException(
            "PaintMound InputActionReference asseti " +
            "oluşturulamadı.");
    }

    return savedReference;
}

        private static void EnsureBinding(
            InputAction action,
            string bindingPath)
        {
            bool exists =
                action.bindings.Any(
                    binding =>
                        string.Equals(
                            binding.path,
                            bindingPath,
                            StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                action.AddBinding(
                    bindingPath);
            }
        }

        // =========================================================
        // CONFIG
        // =========================================================

        private static ScriptableObject CreateOrUpdateConfig(
            Type configType)
        {
            ScriptableObject config =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    ConfigAssetPath);

            if (config != null &&
                !configType.IsInstanceOfType(config))
            {
                throw new InvalidOperationException(
                    "DA_OilPainterPaintMound_Default mevcut ancak " +
                    "beklenen Paint Mound Config tipinde değil.");
            }

            if (config == null)
            {
                config =
                    ScriptableObject.CreateInstance(
                        configType);

                config.name =
                    "DA_OilPainterPaintMound_Default";

                AssetDatabase.CreateAsset(
                    config,
                    ConfigAssetPath);
            }

            SerializedObject serialized =
                new SerializedObject(config);

            serialized.Update();

            SetRequiredFloat(
                serialized,
                0.55f,
                "minimumHoldDuration");

            SetRequiredFloat(
                serialized,
                2f,
                "maximumChargeDuration");

            SetRequiredFloat(
                serialized,
                0.75f,
                "minimumRadius");

            SetRequiredFloat(
                serialized,
                1.80f,
                "maximumRadius");

            SetRequiredFloat(
                serialized,
                0.45f,
                "minimumHeight");

            SetRequiredFloat(
                serialized,
                1.35f,
                "maximumHeight");

            SetRequiredFloat(
                serialized,
                12f,
                "minimumPigmentCost");

            SetRequiredFloat(
                serialized,
                30f,
                "maximumPigmentCost");

            SetRequiredFloat(
                serialized,
                0.75f,
                "growthDuration");

            SetRequiredFloat(
                serialized,
                5f,
                "wetDuration");

            SetRequiredFloat(
                serialized,
                4f,
                "dryingDuration");

            SetRequiredFloat(
                serialized,
                2.25f,
                "placementCooldown");

            SetRequiredInt(
                serialized,
                3,
                "maximumActiveMounds");

            SetRequiredInt(
                serialized,
                8,
                "maximumTotalMounds");

            SetRequiredInt(
                serialized,
                16,
                "radialSegments");

            SetRequiredInt(
                serialized,
                6,
                "verticalRings");

            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(
                config);

            return config;
        }

        // =========================================================
        // MATERIAL
        // =========================================================

        private static Material CreateOrUpdatePreviewMaterial()
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Universal Render Pipeline/Lit shader bulunamadı.");
            }

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    PreviewMaterialPath);

            if (material == null)
            {
                material =
                    new Material(shader);

                material.name =
                    "MAT_PaintMoundPreview";

                AssetDatabase.CreateAsset(
                    material,
                    PreviewMaterialPath);
            }

            Undo.RecordObject(
                material,
                "Configure Paint Mound Preview Material");

            material.shader = shader;

            Color baseColor =
                new Color(
                    0.24f,
                    0.025f,
                    0.055f,
                    0.45f);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    baseColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor(
                    "_Color",
                    baseColor);
            }

            SetMaterialFloat(
                material,
                "_Surface",
                1f);

            SetMaterialFloat(
                material,
                "_Blend",
                0f);

            SetMaterialFloat(
                material,
                "_AlphaClip",
                0f);

            SetMaterialFloat(
                material,
                "_Smoothness",
                0.65f);

            SetMaterialFloat(
                material,
                "_ZWrite",
                0f);

            SetMaterialFloat(
                material,
                "_SrcBlend",
                (float)BlendMode.SrcAlpha);

            SetMaterialFloat(
                material,
                "_DstBlend",
                (float)BlendMode.OneMinusSrcAlpha);

            material.SetOverrideTag(
                "RenderType",
                "Transparent");

            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");

            material.DisableKeyword(
                "_ALPHATEST_ON");

            material.DisableKeyword(
                "_ALPHAPREMULTIPLY_ON");

            material.renderQueue =
                (int)RenderQueue.Transparent;

            material.SetShaderPassEnabled(
                "ShadowCaster",
                false);

            EditorUtility.SetDirty(
                material);

            return material;
        }

        private static void SetMaterialFloat(
            Material material,
            string propertyName,
            float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(
                    propertyName,
                    value);
            }
        }

        // =========================================================
        // RUNTIME
        // =========================================================

        private static RuntimeSetup SetupRuntime(
            SetupContext context,
            ScriptableObject config,
            Material previewMaterial,
            InputActionReference moundActionReference)
        {
            Transform moundsRoot =
                EnsureChild(
                    context.PaintRuntime,
                    MoundsRootName);

            moundsRoot.localPosition =
                Vector3.zero;

            moundsRoot.localRotation =
                Quaternion.identity;

            moundsRoot.localScale =
                Vector3.one;

            Component system =
                GetOrAddComponent(
                    context.PaintRuntime.gameObject,
                    context.SystemType);

            Component controller =
                GetOrAddComponent(
                    context.PaintRuntime.gameObject,
                    context.ControllerType);

            UnityEngine.Object wetMaterial =
                GetRequiredObjectReference(
                    context.OilStrokeSystem,
                    "wetMaterial",
                    "initialWetMaterial");

            UnityEngine.Object dryMaterial =
                GetRequiredObjectReference(
                    context.OilStrokeSystem,
                    "dryMaterial",
                    "finalDryMaterial");

            SetRequiredObjectReference(
                system,
                config,
                "config");

            SetRequiredObjectReference(
                system,
                wetMaterial,
                "wetMaterial",
                "initialWetMaterial");

            SetRequiredObjectReference(
                system,
                dryMaterial,
                "dryMaterial",
                "finalDryMaterial");

            SetRequiredObjectReference(
                system,
                moundsRoot,
                "moundsRoot",
                "moundRoot");

            UnityEngine.Object outputCamera =
                GetRequiredObjectReference(
                    context.BrushController,
                    "outputCamera");

            InputActionReference pointerAction =
                GetInputActionReference(
                    context.BrushController,
                    "pointerPositionAction");

            InputActionReference clearAction =
                GetInputActionReference(
                    context.BrushController,
                    "clearAction");

            if (pointerAction == null)
            {
                throw new InvalidOperationException(
                    "PainterBrushController üzerindeki Pointer Position Action boş.");
            }

            if (clearAction == null)
            {
                throw new InvalidOperationException(
                    "PainterBrushController üzerindeki Clear Action boş.");
            }

            int paintSurfaceMask =
                GetRequiredInteger(
                    context.BrushController,
                    "paintSurfaceMask");

            int oilPaintLayer =
                LayerMask.NameToLayer(
                    "OilPaint");

            if (oilPaintLayer >= 0)
            {
                paintSurfaceMask &=
                    ~(1 << oilPaintLayer);
            }

            int forbiddenZoneMask =
                GetRequiredInteger(
                    context.BrushController,
                    "forbiddenZoneMask");

            SetRequiredObjectReference(
                controller,
                outputCamera,
                "outputCamera");

            SetRequiredObjectReference(
                controller,
                system,
                "moundSystem",
                "paintMoundSystem");

            SetRequiredObjectReference(
                controller,
                context.PigmentReservoir,
                "pigmentReservoir");

            SetRequiredObjectReference(
                controller,
                context.BrushController,
                "brushController");

            SetRequiredInputActionReference(
                controller,
                pointerAction,
                "pointerPositionAction");

            SetRequiredInputActionReference(
                controller,
                moundActionReference,
                "moundAction",
                "paintMoundAction");

            SetRequiredInputActionReference(
                controller,
                clearAction,
                "clearAction");

            SetRequiredInteger(
                controller,
                paintSurfaceMask,
                "paintSurfaceMask");

            SetRequiredInteger(
                controller,
                forbiddenZoneMask,
                "forbiddenZoneMask");

            SetRequiredFloat(
                controller,
                100f,
                "maximumRayDistance");

            SetRequiredFloat(
                controller,
                1.1f,
                "forbiddenRadius");

            SetRequiredObjectReference(
                controller,
                previewMaterial,
                "previewMaterial");

            EditorUtility.SetDirty(system);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(
                context.PaintRuntime.gameObject);

            return new RuntimeSetup
            {
                System = system,
                Controller = controller,
                MoundsRoot = moundsRoot
            };
        }

        // =========================================================
        // HUD
        // =========================================================

        private static Component SetupHud(
            SetupContext context,
            RuntimeSetup runtime)
        {
            GameObject panel =
                EnsureUiPanel(
                    context.PainterHud,
                    PanelName);

            RectTransform panelRect =
                panel.GetComponent<RectTransform>();

            SetTopLeftRect(
                panelRect,
                new Vector2(24f, -260f),
                new Vector2(360f, 68f));

            CanvasGroup canvasGroup =
                panel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup =
                    Undo.AddComponent<CanvasGroup>(
                        panel);
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.ignoreParentGroups = false;

            RemoveChild(
                panel.transform,
                "StatusText");

            RemoveChild(
                panel.transform,
                "DetailText");

            RemoveChild(
                panel.transform,
                "ChargeSlider");

            Text statusText =
                CreateLegacyText(
                    panel.transform,
                    "StatusText",
                    "3 BASILI TUT — BOYA TEPESİ",
                    13,
                    FontStyle.Bold,
                    new Vector2(0f, -2f),
                    new Vector2(350f, 20f));

            Text detailText =
                CreateLegacyText(
                    panel.transform,
                    "DetailText",
                    string.Empty,
                    11,
                    FontStyle.Normal,
                    new Vector2(0f, -24f),
                    new Vector2(350f, 18f));

            Slider chargeSlider =
                CreateChargeSlider(
                    panel.transform);

            Component hud =
                GetOrAddComponent(
                    panel,
                    context.HudType);

            SetRequiredObjectReference(
                hud,
                runtime.Controller,
                "controller",
                "paintMoundController");

            SetRequiredObjectReference(
                hud,
                runtime.System,
                "moundSystem",
                "paintMoundSystem");

            SetRequiredObjectReference(
                hud,
                canvasGroup,
                "canvasGroup");

            SetRequiredObjectReference(
                hud,
                statusText,
                "statusText");

            SetRequiredObjectReference(
                hud,
                detailText,
                "detailText");

            SetRequiredObjectReference(
                hud,
                chargeSlider,
                "chargeSlider");

            panel.SetActive(true);

            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(hud);

            return hud;
        }

        private static GameObject EnsureUiPanel(
            Transform parent,
            string objectName)
        {
            Transform existing =
                parent.Find(objectName);

            if (existing != null)
            {
                RectTransform existingRect =
                    existing.GetComponent<RectTransform>();

                if (existingRect == null)
                {
                    throw new InvalidOperationException(
                        $"{objectName} mevcut ancak RectTransform içermiyor.");
                }

                return existing.gameObject;
            }

            GameObject panel =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasGroup));

            Undo.RegisterCreatedObjectUndo(
                panel,
                "Create Paint Mound Panel");

            panel.transform.SetParent(
                parent,
                false);

            return panel;
        }

        private static Text CreateLegacyText(
            Transform parent,
            string objectName,
            string content,
            int fontSize,
            FontStyle fontStyle,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject textObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            Undo.RegisterCreatedObjectUndo(
                textObject,
                $"Create {objectName}");

            textObject.transform.SetParent(
                parent,
                false);

            RectTransform rectTransform =
                textObject.GetComponent<RectTransform>();

            SetTopLeftRect(
                rectTransform,
                anchoredPosition,
                size);

            Text text =
                textObject.GetComponent<Text>();

            text.text = content;
            text.font = LoadLegacyFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment =
                TextAnchor.MiddleLeft;

            text.color =
                Color.white;

            text.raycastTarget = false;
            text.supportRichText = true;
            text.resizeTextForBestFit = false;

            text.horizontalOverflow =
                HorizontalWrapMode.Overflow;

            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            return text;
        }

        private static Slider CreateChargeSlider(
            Transform parent)
        {
            GameObject sliderObject =
                new GameObject(
                    "ChargeSlider",
                    typeof(RectTransform),
                    typeof(Slider));

            Undo.RegisterCreatedObjectUndo(
                sliderObject,
                "Create Paint Mound Charge Slider");

            sliderObject.transform.SetParent(
                parent,
                false);

            RectTransform sliderRect =
                sliderObject.GetComponent<RectTransform>();

            SetTopLeftRect(
                sliderRect,
                new Vector2(0f, -50f),
                new Vector2(240f, 7f));

            Image background =
                CreateSliderImage(
                    sliderRect,
                    "Background",
                    new Color32(
                        33,
                        28,
                        34,
                        255));

            RectTransform fillArea =
                CreateFillArea(
                    sliderRect);

            Image fill =
                CreateSliderImage(
                    fillArea,
                    "Fill",
                    new Color32(
                        163,
                        53,
                        61,
                        255));

            Slider slider =
                sliderObject.GetComponent<Slider>();

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            slider.SetValueWithoutNotify(
                0f);

            slider.direction =
                Slider.Direction.LeftToRight;

            slider.fillRect =
                fill.rectTransform;

            slider.handleRect = null;
            slider.targetGraphic =
                background;

            slider.interactable = false;

            slider.transition =
                Selectable.Transition.None;

            Navigation navigation =
                slider.navigation;

            navigation.mode =
                Navigation.Mode.None;

            slider.navigation =
                navigation;

            background.raycastTarget = false;
            fill.raycastTarget = false;

            return slider;
        }

        private static Image CreateSliderImage(
            RectTransform parent,
            string objectName,
            Color color)
        {
            GameObject imageObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            Undo.RegisterCreatedObjectUndo(
                imageObject,
                $"Create {objectName}");

            imageObject.transform.SetParent(
                parent,
                false);

            RectTransform rectTransform =
                imageObject.GetComponent<RectTransform>();

            StretchToParent(
                rectTransform);

            Image image =
                imageObject.GetComponent<Image>();

            image.color = color;

            Sprite sprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/UISprite.psd");

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type =
                    Image.Type.Sliced;
            }

            return image;
        }

        private static RectTransform CreateFillArea(
            RectTransform parent)
        {
            GameObject fillAreaObject =
                new GameObject(
                    "Fill Area",
                    typeof(RectTransform));

            Undo.RegisterCreatedObjectUndo(
                fillAreaObject,
                "Create Charge Slider Fill Area");

            fillAreaObject.transform.SetParent(
                parent,
                false);

            RectTransform fillArea =
                fillAreaObject.GetComponent<RectTransform>();

            fillArea.anchorMin =
                Vector2.zero;

            fillArea.anchorMax =
                Vector2.one;

            fillArea.offsetMin =
                new Vector2(1f, 1f);

            fillArea.offsetMax =
                new Vector2(-1f, -1f);

            fillArea.localRotation =
                Quaternion.identity;

            fillArea.localScale =
                Vector3.one;

            return fillArea;
        }

        // =========================================================
        // ROLE SWITCHER
        // =========================================================

        private static void AddPainterBehaviour(
            Component roleSwitcher,
            Component behaviour)
        {
            if (roleSwitcher == null ||
                behaviour == null)
            {
                return;
            }

            SerializedObject serialized =
                new SerializedObject(
                    roleSwitcher);

            serialized.Update();

            SerializedProperty behaviours =
                FindProperty(
                    serialized,
                    "painterBehaviours");

            if (behaviours == null ||
                !behaviours.isArray)
            {
                throw new InvalidOperationException(
                    "PrototypeRoleSwitcher içinde " +
                    "'painterBehaviours' listesi bulunamadı.");
            }

            for (int i = 0;
                 i < behaviours.arraySize;
                 i++)
            {
                SerializedProperty element =
                    behaviours.GetArrayElementAtIndex(i);

                if (element.objectReferenceValue ==
                    behaviour)
                {
                    return;
                }
            }

            Undo.RecordObject(
                roleSwitcher,
                "Add Painter Behaviour");

            int newIndex =
                behaviours.arraySize;

            behaviours.InsertArrayElementAtIndex(
                newIndex);

            behaviours
                .GetArrayElementAtIndex(newIndex)
                .objectReferenceValue =
                behaviour;

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(
                roleSwitcher);
        }

        // =========================================================
        // SERIALIZED PROPERTY HELPERS
        // =========================================================

        private static void SetRequiredObjectReference(
            Component target,
            UnityEngine.Object value,
            params string[] propertyNames)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target));
            }

            SerializedObject serialized =
                new SerializedObject(target);

            serialized.Update();

            SerializedProperty property =
                FindProperty(
                    serialized,
                    propertyNames);

            if (property == null)
            {
                throw MissingPropertyException(
                    target,
                    propertyNames);
            }

            if (property.propertyType !=
                SerializedPropertyType.ObjectReference)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} içindeki " +
                    $"'{property.propertyPath}' alanı Object Reference değil.");
            }

            Undo.RecordObject(
                target,
                $"Set {property.displayName}");

            property.objectReferenceValue =
                value;

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(
                target);
        }

        private static UnityEngine.Object
            GetRequiredObjectReference(
                Component source,
                params string[] propertyNames)
        {
            SerializedObject serialized =
                new SerializedObject(source);

            serialized.Update();

            SerializedProperty property =
                FindProperty(
                    serialized,
                    propertyNames);

            if (property == null)
            {
                throw MissingPropertyException(
                    source,
                    propertyNames);
            }

            if (property.propertyType !=
                SerializedPropertyType.ObjectReference)
            {
                throw new InvalidOperationException(
                    $"{source.GetType().Name} içindeki " +
                    $"'{property.propertyPath}' alanı Object Reference değil.");
            }

            if (property.objectReferenceValue == null)
            {
                throw new InvalidOperationException(
                    $"{source.GetType().Name} üzerindeki " +
                    $"'{property.displayName}' alanı boş.");
            }

            return property.objectReferenceValue;
        }

        private static InputActionReference
            GetInputActionReference(
                Component source,
                params string[] propertyNames)
        {
            SerializedObject serialized =
                new SerializedObject(source);

            serialized.Update();

            SerializedProperty property =
                FindProperty(
                    serialized,
                    propertyNames);

            if (property == null)
            {
                throw MissingPropertyException(
                    source,
                    propertyNames);
            }

            if (property.propertyType ==
                SerializedPropertyType.ObjectReference)
            {
                return property.objectReferenceValue
                    as InputActionReference;
            }

            SerializedProperty reference =
                property.FindPropertyRelative(
                    "m_Reference");

            if (reference != null)
            {
                return reference.objectReferenceValue
                    as InputActionReference;
            }

            return null;
        }

        private static void SetRequiredInputActionReference(
            Component target,
            InputActionReference actionReference,
            params string[] propertyNames)
        {
            SerializedObject serialized =
                new SerializedObject(target);

            serialized.Update();

            SerializedProperty property =
                FindProperty(
                    serialized,
                    propertyNames);

            if (property == null)
            {
                throw MissingPropertyException(
                    target,
                    propertyNames);
            }

            Undo.RecordObject(
                target,
                $"Set {property.displayName}");

            if (property.propertyType ==
                SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue =
                    actionReference;

                serialized.ApplyModifiedProperties();

                EditorUtility.SetDirty(target);
                return;
            }

            SerializedProperty useReference =
                property.FindPropertyRelative(
                    "m_UseReference");

            SerializedProperty reference =
                property.FindPropertyRelative(
                    "m_Reference");

            if (reference == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} içindeki " +
                    $"'{property.propertyPath}' InputActionReference olarak ayarlanamadı.");
            }

            if (useReference != null)
            {
                useReference.boolValue =
                    true;
            }

            reference.objectReferenceValue =
                actionReference;

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(target);
        }

        private static int GetRequiredInteger(
    Component source,
    params string[] propertyNames)
{
    SerializedObject serialized =
        new SerializedObject(source);

    serialized.Update();

    SerializedProperty property =
        FindProperty(
            serialized,
            propertyNames);

    if (property == null)
    {
        throw MissingPropertyException(
            source,
            propertyNames);
    }

    bool isSupportedType =
        property.propertyType ==
            SerializedPropertyType.Integer ||
        property.propertyType ==
            SerializedPropertyType.LayerMask;

    if (!isSupportedType)
    {
        throw new InvalidOperationException(
            $"{source.GetType().Name} içindeki " +
            $"'{property.propertyPath}' alanı Integer veya LayerMask değil. " +
            $"Bulunan tip: {property.propertyType}");
    }

    return property.intValue;
}

        private static void SetRequiredInteger(
    Component target,
    int value,
    params string[] propertyNames)
{
    SerializedObject serialized =
        new SerializedObject(target);

    serialized.Update();

    SerializedProperty property =
        FindProperty(
            serialized,
            propertyNames);

    if (property == null)
    {
        throw MissingPropertyException(
            target,
            propertyNames);
    }

    bool isSupportedType =
        property.propertyType ==
            SerializedPropertyType.Integer ||
        property.propertyType ==
            SerializedPropertyType.LayerMask;

    if (!isSupportedType)
    {
        throw new InvalidOperationException(
            $"{target.GetType().Name} içindeki " +
            $"'{property.propertyPath}' alanı Integer veya LayerMask değil. " +
            $"Bulunan tip: {property.propertyType}");
    }

    Undo.RecordObject(
        target,
        $"Set {property.displayName}");

    property.intValue = value;

    serialized.ApplyModifiedProperties();

    EditorUtility.SetDirty(target);
}

        private static void SetRequiredFloat(
            Component target,
            float value,
            params string[] propertyNames)
        {
            SerializedObject serialized =
                new SerializedObject(target);

            serialized.Update();

            SerializedProperty property =
                FindProperty(
                    serialized,
                    propertyNames);

            if (property == null)
            {
                throw MissingPropertyException(
                    target,
                    propertyNames);
            }

            if (property.propertyType !=
                SerializedPropertyType.Float)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} içindeki " +
                    $"'{property.propertyPath}' Float değil.");
            }

            Undo.RecordObject(
                target,
                $"Set {property.displayName}");

            property.floatValue = value;

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(target);
        }

        private static void SetRequiredFloat(
            SerializedObject serialized,
            float value,
            params string[] propertyNames)
        {
            SerializedProperty property =
                FindProperty(
                    serialized,
                    propertyNames);

            if (property == null)
            {
                throw MissingPropertyException(
                    serialized.targetObject,
                    propertyNames);
            }

            property.floatValue = value;
        }

        private static void SetRequiredInt(
            SerializedObject serialized,
            int value,
            params string[] propertyNames)
        {
            SerializedProperty property =
                FindProperty(
                    serialized,
                    propertyNames);

            if (property == null)
            {
                throw MissingPropertyException(
                    serialized.targetObject,
                    propertyNames);
            }

            property.intValue = value;
        }

        private static SerializedProperty FindProperty(
            SerializedObject serialized,
            params string[] propertyNames)
        {
            foreach (string propertyName
                     in propertyNames)
            {
                SerializedProperty property =
                    serialized.FindProperty(
                        propertyName);

                if (property != null)
                {
                    return property;
                }
            }

            return null;
        }

        private static Exception MissingPropertyException(
            UnityEngine.Object target,
            string[] propertyNames)
        {
            return new InvalidOperationException(
                $"{target.GetType().Name} içinde şu serialized alan " +
                $"bulunamadı:\n{string.Join(" / ", propertyNames)}");
        }

        // =========================================================
        // TYPE AND SCENE HELPERS
        // =========================================================

        private static Component GetOrAddComponent(
            GameObject gameObject,
            Type componentType)
        {
            Component existing =
                gameObject.GetComponent(
                    componentType);

            if (existing != null)
            {
                return existing;
            }

            return Undo.AddComponent(
                gameObject,
                componentType);
        }

        private static Component FindComponentOnObject(
            GameObject gameObject,
            string typeName)
        {
            Type type =
                FindMonoBehaviourType(
                    typeName);

            return type != null
                ? gameObject.GetComponent(type)
                : null;
        }

        private static Component FindSceneComponent(
            string typeName)
        {
            Type type =
                FindMonoBehaviourType(
                    typeName);

            if (type == null)
            {
                return null;
            }

            UnityEngine.Object[] objects =
                Resources.FindObjectsOfTypeAll(type);

            foreach (UnityEngine.Object item
                     in objects)
            {
                Component component =
                    item as Component;

                if (component == null ||
                    !component.gameObject.scene.IsValid() ||
                    EditorUtility.IsPersistent(component))
                {
                    continue;
                }

                return component;
            }

            return null;
        }

        private static Type FindMonoBehaviourType(
            string typeName)
        {
            Type fallback = null;

            foreach (Type type in
                     TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (type.IsAbstract ||
                    !string.Equals(
                        type.Name,
                        typeName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (type.Namespace != null &&
                    type.Namespace.StartsWith(
                        "PaintedAlive",
                        StringComparison.Ordinal))
                {
                    return type;
                }

                fallback = type;
            }

            return fallback;
        }

        private static Type FindScriptableObjectType(
            params string[] typeNames)
        {
            Type fallback = null;

            foreach (Type type in
                     TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                bool nameMatches =
                    typeNames.Any(
                        typeName =>
                            string.Equals(
                                type.Name,
                                typeName,
                                StringComparison.Ordinal));

                if (!nameMatches)
                {
                    continue;
                }

                if (type.Namespace != null &&
                    type.Namespace.StartsWith(
                        "PaintedAlive",
                        StringComparison.Ordinal))
                {
                    return type;
                }

                fallback = type;
            }

            return fallback;
        }

        private static Transform FindSceneTransform(
            string objectName)
        {
            Transform[] transforms =
                Resources.FindObjectsOfTypeAll<Transform>();

            foreach (Transform transform
                     in transforms)
            {
                if (transform == null ||
                    !transform.gameObject.scene.IsValid() ||
                    EditorUtility.IsPersistent(transform))
                {
                    continue;
                }

                if (transform.name ==
                    objectName)
                {
                    return transform;
                }
            }

            return null;
        }

        private static Transform FindPainterHud()
        {
            Canvas[] canvases =
                Resources.FindObjectsOfTypeAll<Canvas>();

            foreach (Canvas canvas
                     in canvases)
            {
                if (canvas == null ||
                    !canvas.gameObject.scene.IsValid() ||
                    EditorUtility.IsPersistent(canvas))
                {
                    continue;
                }

                Transform painterHud =
                    FindChildRecursive(
                        canvas.transform,
                        PainterHudName);

                if (painterHud != null)
                {
                    return painterHud;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(
            Transform parent,
            string childName)
        {
            foreach (Transform child
                     in parent)
            {
                if (child.name ==
                    childName)
                {
                    return child;
                }

                Transform nested =
                    FindChildRecursive(
                        child,
                        childName);

                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static Transform EnsureChild(
            Transform parent,
            string childName)
        {
            Transform existing =
                parent.Find(childName);

            if (existing != null)
            {
                return existing;
            }

            GameObject child =
                new GameObject(
                    childName);

            Undo.RegisterCreatedObjectUndo(
                child,
                $"Create {childName}");

            child.transform.SetParent(
                parent,
                false);

            return child.transform;
        }

        private static void RemoveChild(
            Transform parent,
            string childName)
        {
            Transform child =
                parent.Find(childName);

            if (child != null)
            {
                Undo.DestroyObjectImmediate(
                    child.gameObject);
            }
        }

        private static void EnsureFolder(
            string folderPath)
        {
            string[] parts =
                folderPath.Split('/');

            if (parts.Length == 0 ||
                parts[0] != "Assets")
            {
                throw new InvalidOperationException(
                    "Klasör yolu Assets ile başlamalıdır.");
            }

            string current =
                "Assets";

            for (int i = 1;
                 i < parts.Length;
                 i++)
            {
                string next =
                    current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        parts[i]);
                }

                current = next;
            }
        }

        // =========================================================
        // UI HELPERS
        // =========================================================

        private static void SetTopLeftRect(
            RectTransform rectTransform,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            rectTransform.anchorMin =
                new Vector2(0f, 1f);

            rectTransform.anchorMax =
                new Vector2(0f, 1f);

            rectTransform.pivot =
                new Vector2(0f, 1f);

            rectTransform.anchoredPosition =
                anchoredPosition;

            rectTransform.sizeDelta =
                size;

            rectTransform.localRotation =
                Quaternion.identity;

            rectTransform.localScale =
                Vector3.one;
        }

        private static void StretchToParent(
            RectTransform rectTransform)
        {
            rectTransform.anchorMin =
                Vector2.zero;

            rectTransform.anchorMax =
                Vector2.one;

            rectTransform.offsetMin =
                Vector2.zero;

            rectTransform.offsetMax =
                Vector2.zero;

            rectTransform.localRotation =
                Quaternion.identity;

            rectTransform.localScale =
                Vector3.one;
        }

        private static Font LoadLegacyFont()
        {
            Font font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>(
                "Arial.ttf");
        }

        // =========================================================
        // DATA
        // =========================================================

        private sealed class SetupContext
        {
            public InputActionAsset InputActions;
            public InputActionMap PlayerMap;
            public Transform PaintRuntime;
            public Transform PainterHud;
            public Component OilStrokeSystem;
            public Component BrushController;
            public Component PigmentReservoir;
            public Component RoleSwitcher;
            public Type ConfigType;
            public Type SystemType;
            public Type ControllerType;
            public Type HudType;
        }

        private sealed class RuntimeSetup
        {
            public Component System;
            public Component Controller;
            public Transform MoundsRoot;
        }
    }
}

#endif