using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaintedAlive.EditorTools
{
    public static class SetupPainterStrokeBudget
    {
        private const string DataFolder =
            "Assets/_Project/Data/Painters";

        private const string BudgetAssetPath =
            DataFolder +
            "/DA_OilPainterStrokeBudget_Default.asset";

        private const string PigmentAssetPath =
            DataFolder +
            "/DA_OilPainterPigment_Default.asset";

        private const string PaintRuntimeName =
            "PaintRuntime";

        private const string CanvasName =
            "Canvas";

        private const string PainterHudName =
            "PainterHUD";

        private const string BudgetPanelName =
            "StrokeBudgetPanel";

        [MenuItem(
            "Tools/Painted Alive/Painters/Setup Stroke Budget")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning(
                    "Kurulum Play Mode dışında çalıştırılmalıdır.");

                return;
            }

            EnsureFolderExists(DataFolder);

            ScriptableObject budgetConfig =
                CreateOrLoadBudgetConfig();

            ScriptableObject pigmentConfig =
                CreateOrLoadPigmentConfig();

            if (budgetConfig == null)
            {
                Debug.LogError(
                    "Stroke Budget Config oluşturulamadığı için " +
                    "kurulum durduruldu.");

                return;
            }

            ConfigureBudgetAsset(budgetConfig);

            if (pigmentConfig != null)
            {
                ConfigurePigmentAsset(pigmentConfig);
            }

            RuntimeReferences runtimeReferences =
                SetupPaintRuntime(
                    budgetConfig,
                    pigmentConfig);

            if (runtimeReferences == null)
            {
                return;
            }

            Component budgetHud =
                SetupBudgetHud(runtimeReferences);

            if (budgetHud != null)
            {
                AddToPainterBehaviours(budgetHud);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene());

            Debug.Log(
                "Painter Stroke Budget kurulumu tamamlandı. " +
                "Sahneyi Ctrl+S ile kaydet.");
        }

        // =========================================================
        // CONFIG ASSETS
        // =========================================================

        private static ScriptableObject CreateOrLoadBudgetConfig()
        {
            ScriptableObject existing =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    BudgetAssetPath);

            if (existing != null)
            {
                return existing;
            }

            Type configType =
                FindScriptableObjectType(
                    new[]
                    {
                        "PainterStrokeBudgetConfig",
                        "OilPainterStrokeBudgetConfig"
                    },
                    new[]
                    {
                        "StrokeBudget",
                        "Config"
                    });

            if (configType == null)
            {
                Debug.LogError(
                    "PainterStrokeBudgetConfig sınıfı bulunamadı. " +
                    "Config scriptinin derlendiğini kontrol et.");

                return null;
            }

            ScriptableObject asset =
                ScriptableObject.CreateInstance(configType);

            asset.name =
                "DA_OilPainterStrokeBudget_Default";

            AssetDatabase.CreateAsset(
                asset,
                BudgetAssetPath);

            Debug.Log(
                $"Config asset oluşturuldu: {BudgetAssetPath}",
                asset);

            return asset;
        }

        private static ScriptableObject CreateOrLoadPigmentConfig()
        {
            ScriptableObject exactAsset =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    PigmentAssetPath);

            if (exactAsset != null)
            {
                return exactAsset;
            }

            ScriptableObject foundAsset =
                FindScriptableObjectAssetByName(
                    "DA_OilPainterPigment_Default");

            if (foundAsset != null)
            {
                Debug.Log(
                    "DA_OilPainterPigment_Default farklı bir klasörde " +
                    "bulundu ve mevcut asset güncellenecek.",
                    foundAsset);

                return foundAsset;
            }

            Type configType =
                FindScriptableObjectType(
                    new[]
                    {
                        "PainterPigmentConfig"
                    },
                    new[]
                    {
                        "Painter",
                        "Pigment",
                        "Config"
                    });

            if (configType == null)
            {
                Debug.LogWarning(
                    "PainterPigmentConfig sınıfı bulunamadı. " +
                    "Pigment asset güncellemesi atlandı.");

                return null;
            }

            ScriptableObject asset =
                ScriptableObject.CreateInstance(configType);

            asset.name =
                "DA_OilPainterPigment_Default";

            AssetDatabase.CreateAsset(
                asset,
                PigmentAssetPath);

            Debug.Log(
                $"Pigment config oluşturuldu: {PigmentAssetPath}",
                asset);

            return asset;
        }

        private static void ConfigureBudgetAsset(
            ScriptableObject budgetConfig)
        {
            SerializedObject serialized =
                new SerializedObject(budgetConfig);

            SetFloat(
                serialized,
                12f,
                "maximumPressure");

            SetInt(
                serialized,
                3,
                "maximumActiveStrokes");

            SetFloat(
                serialized,
                4f,
                "wallPressure");

            SetFloat(
                serialized,
                3.5f,
                "rampPressure");

            SetFloat(
                serialized,
                0.5f,
                "dryStrokePressure");

            SetFloat(
                serialized,
                1.4f,
                "wallTelegraphDuration");

            SetFloat(
                serialized,
                0.9f,
                "rampTelegraphDuration");

            SetFloat(
                serialized,
                9f,
                "wallPigmentSurcharge");

            SetFloat(
                serialized,
                7f,
                "rampPigmentSurcharge");

            SetFloat(
                serialized,
                0.35f,
                "strokeCooldown");

            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(budgetConfig);
        }

        private static void ConfigurePigmentAsset(
            ScriptableObject pigmentConfig)
        {
            SerializedObject serialized =
                new SerializedObject(pigmentConfig);

            SetFloat(
                serialized,
                100f,
                "capacity");

            SetFloat(
                serialized,
                6f,
                "regenerationPerSecond");

            SetFloat(
                serialized,
                3f,
                "strokeBeginCost");

            SetFloat(
                serialized,
                3.5f,
                "costPerMeter");

            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(pigmentConfig);
        }

        // =========================================================
        // PAINT RUNTIME
        // =========================================================

        private static RuntimeReferences SetupPaintRuntime(
            ScriptableObject budgetConfig,
            ScriptableObject pigmentConfig)
        {
            Transform paintRuntimeTransform =
                FindSceneTransform(PaintRuntimeName);

            if (paintRuntimeTransform == null)
            {
                Debug.LogError(
                    $"Hierarchy'de '{PaintRuntimeName}' bulunamadı.");

                return null;
            }

            GameObject paintRuntime =
                paintRuntimeTransform.gameObject;

            Component strokeSystem =
                FindComponentOnGameObject(
                    paintRuntime,
                    "OilStrokeSystem");

            Component brushController =
                FindComponentOnGameObject(
                    paintRuntime,
                    "PainterBrushController");

            Component modeSelector =
                FindComponentOnGameObject(
                    paintRuntime,
                    "PainterStrokeModeSelector");

            Component pigmentReservoir =
                FindComponentOnGameObject(
                    paintRuntime,
                    "PainterPigmentReservoir");

            Component strokeBudget =
                GetOrAddComponent(
                    paintRuntime,
                    "PainterStrokeBudget");

            if (strokeBudget == null)
            {
                Debug.LogError(
                    "PainterStrokeBudget componenti eklenemedi.",
                    paintRuntime);

                return null;
            }

            SetSceneObjectReferences(
                strokeBudget,
                new ObjectAssignment(
                    "config",
                    budgetConfig),
                new ObjectAssignment(
                    "strokeSystem",
                    strokeSystem));

            if (brushController != null)
            {
                SetSceneObjectReferences(
                    brushController,
                    new ObjectAssignment(
                        "strokeBudget",
                        strokeBudget));

                SetFloatIfPresent(
                    brushController,
                    1f,
                    "telegraphFallback");
            }
            else
            {
                Debug.LogWarning(
                    "PaintRuntime üzerinde PainterBrushController " +
                    "bulunamadı.",
                    paintRuntime);
            }

            if (pigmentReservoir != null &&
                pigmentConfig != null)
            {
                AssignObjectOnlyWhenEmpty(
                    pigmentReservoir,
                    "config",
                    pigmentConfig);
            }

            EditorUtility.SetDirty(paintRuntime);

            return new RuntimeReferences
            {
                PaintRuntime = paintRuntime,
                Budget = strokeBudget,
                StrokeSystem = strokeSystem,
                BrushController = brushController,
                ModeSelector = modeSelector,
                PigmentReservoir = pigmentReservoir
            };
        }

        // =========================================================
        // HUD
        // =========================================================

        private static Component SetupBudgetHud(
            RuntimeReferences runtime)
        {
            Transform canvas =
                FindSceneTransform(CanvasName);

            if (canvas == null ||
                canvas.GetComponent<Canvas>() == null)
            {
                Debug.LogError(
                    $"Aktif sahnede '{CanvasName}' isimli Canvas bulunamadı.");

                return null;
            }

            Transform painterHud =
                FindChildRecursive(
                    canvas,
                    PainterHudName);

            if (painterHud == null)
            {
                Debug.LogError(
                    $"Canvas altında '{PainterHudName}' bulunamadı.",
                    canvas);

                return null;
            }

            GameObject panel =
                GetOrCreatePanel(painterHud);

            if (panel == null)
            {
                return null;
            }

            ConfigurePanel(panel);

            RemoveChildIfExists(
                panel.transform,
                "PressureSlider");

            RemoveChildIfExists(
                panel.transform,
                "ActiveStrokeText");

            RemoveChildIfExists(
                panel.transform,
                "BudgetStatusText");

            RemoveChildIfExists(
                panel.transform,
                "EstimatedCostText");

            // Kullanıcı yalnızca boyutları verdiği için metinlerin
            // panel içi konumları çakışmayacak biçimde dikey yerleştirildi.

            Slider pressureSlider =
                CreatePressureSlider(
                    panel.transform);

            Text activeStrokeText =
                CreateLegacyText(
                    "ActiveStrokeText",
                    panel.transform,
                    "AKTİF BOYA 0/3",
                    13,
                    FontStyle.Bold,
                    new Vector2(0f, -20f),
                    new Vector2(320f, 20f));

            Text budgetStatusText =
                CreateLegacyText(
                    "BudgetStatusText",
                    panel.transform,
                    "HAZIR",
                    12,
                    FontStyle.Normal,
                    new Vector2(0f, -43f),
                    new Vector2(320f, 20f));

            Text estimatedCostText =
                CreateLegacyText(
                    "EstimatedCostText",
                    panel.transform,
                    string.Empty,
                    12,
                    FontStyle.Normal,
                    new Vector2(0f, -66f),
                    new Vector2(320f, 20f));

            CanvasGroup canvasGroup =
                panel.GetComponent<CanvasGroup>();

            Component budgetHud =
                GetOrAddComponent(
                    panel,
                    "PainterStrokeBudgetHud");

            if (budgetHud == null)
            {
                Debug.LogError(
                    "PainterStrokeBudgetHud componenti bulunamadı.",
                    panel);

                return null;
            }

            SetSceneObjectReferences(
                budgetHud,
                new ObjectAssignment(
                    "budget",
                    runtime.Budget),
                new ObjectAssignment(
                    "brushController",
                    runtime.BrushController),
                new ObjectAssignment(
                    "modeSelector",
                    runtime.ModeSelector),
                new ObjectAssignment(
                    "canvasGroup",
                    canvasGroup),
                new ObjectAssignment(
                    "pressureSlider",
                    pressureSlider),
                new ObjectAssignment(
                    "activeStrokeText",
                    activeStrokeText),
                new ObjectAssignment(
                    "statusText",
                    budgetStatusText),
                new ObjectAssignment(
                    "estimatedCostText",
                    estimatedCostText));

            panel.SetActive(true);
            EditorUtility.SetDirty(panel);

            Selection.activeGameObject = panel;
            EditorGUIUtility.PingObject(panel);

            return budgetHud;
        }

        private static GameObject GetOrCreatePanel(
            Transform painterHud)
        {
            Transform existing =
                painterHud.Find(BudgetPanelName);

            if (existing != null)
            {
                if (existing.GetComponent<RectTransform>() != null)
                {
                    return existing.gameObject;
                }

                Undo.DestroyObjectImmediate(
                    existing.gameObject);
            }

            GameObject panel =
                new GameObject(
                    BudgetPanelName,
                    typeof(RectTransform),
                    typeof(CanvasGroup));

            Undo.RegisterCreatedObjectUndo(
                panel,
                "Create Stroke Budget Panel");

            panel.transform.SetParent(
                painterHud,
                false);

            return panel;
        }

        private static void ConfigurePanel(
            GameObject panel)
        {
            RectTransform rectTransform =
                panel.GetComponent<RectTransform>();

            rectTransform.anchorMin =
                new Vector2(0f, 1f);

            rectTransform.anchorMax =
                new Vector2(0f, 1f);

            rectTransform.pivot =
                new Vector2(0f, 1f);

            rectTransform.anchoredPosition =
                new Vector2(24f, -82f);

            rectTransform.sizeDelta =
                new Vector2(330f, 95f);

            rectTransform.localRotation =
                Quaternion.identity;

            rectTransform.localScale =
                Vector3.one;

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

            panel.SetActive(true);
        }

        private static Slider CreatePressureSlider(
            Transform parent)
        {
            GameObject sliderObject =
                new GameObject(
                    "PressureSlider",
                    typeof(RectTransform),
                    typeof(Slider));

            Undo.RegisterCreatedObjectUndo(
                sliderObject,
                "Create Pressure Slider");

            sliderObject.transform.SetParent(
                parent,
                false);

            RectTransform sliderRect =
                sliderObject.GetComponent<RectTransform>();

            SetTopLeftRect(
                sliderRect,
                new Vector2(0f, -6f),
                new Vector2(220f, 8f));

            Image background =
                CreateSliderBackground(
                    sliderRect,
                    "#211C22");

            RectTransform fillArea =
                CreateFillArea(sliderRect);

            Image fill =
                CreateSliderFill(
                    fillArea,
                    "#A3353D");

            Slider slider =
                sliderObject.GetComponent<Slider>();

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction =
                Slider.Direction.LeftToRight;

            slider.SetValueWithoutNotify(0f);

            slider.fillRect =
                fill.rectTransform;

            slider.handleRect = null;
            slider.targetGraphic = null;
            slider.interactable = false;
            slider.transition =
                Selectable.Transition.None;

            Navigation navigation =
                slider.navigation;

            navigation.mode =
                Navigation.Mode.None;

            slider.navigation = navigation;

            background.raycastTarget = false;
            fill.raycastTarget = false;

            return slider;
        }

        private static Image CreateSliderBackground(
            RectTransform sliderRect,
            string colorHex)
        {
            GameObject backgroundObject =
                new GameObject(
                    "Background",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            Undo.RegisterCreatedObjectUndo(
                backgroundObject,
                "Create Slider Background");

            backgroundObject.transform.SetParent(
                sliderRect,
                false);

            RectTransform backgroundRect =
                backgroundObject.GetComponent<RectTransform>();

            StretchToParent(backgroundRect);

            Image image =
                backgroundObject.GetComponent<Image>();

            image.color =
                ParseColor(colorHex);

            Sprite sprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/Background.psd");

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        private static RectTransform CreateFillArea(
            RectTransform sliderRect)
        {
            GameObject fillAreaObject =
                new GameObject(
                    "Fill Area",
                    typeof(RectTransform));

            Undo.RegisterCreatedObjectUndo(
                fillAreaObject,
                "Create Slider Fill Area");

            fillAreaObject.transform.SetParent(
                sliderRect,
                false);

            RectTransform fillArea =
                fillAreaObject.GetComponent<RectTransform>();

            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;

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

        private static Image CreateSliderFill(
            RectTransform fillArea,
            string colorHex)
        {
            GameObject fillObject =
                new GameObject(
                    "Fill",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            Undo.RegisterCreatedObjectUndo(
                fillObject,
                "Create Slider Fill");

            fillObject.transform.SetParent(
                fillArea,
                false);

            RectTransform fillRect =
                fillObject.GetComponent<RectTransform>();

            StretchToParent(fillRect);

            fillRect.pivot =
                new Vector2(0f, 0.5f);

            Image image =
                fillObject.GetComponent<Image>();

            image.color =
                ParseColor(colorHex);

            Sprite sprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/UISprite.psd");

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        private static Text CreateLegacyText(
            string objectName,
            Transform parent,
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
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.resizeTextForBestFit = false;

            text.horizontalOverflow =
                HorizontalWrapMode.Overflow;

            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            return text;
        }

        // =========================================================
        // ROLE SWITCHER
        // =========================================================

        private static void AddToPainterBehaviours(
            Component budgetHud)
        {
            Component roleSwitcher =
                FindSceneComponent(
                    "PrototypeRoleSwitcher");

            if (roleSwitcher == null)
            {
                Debug.LogWarning(
                    "PrototypeRoleSwitcher bulunamadı. " +
                    "PainterStrokeBudgetHud listeye eklenemedi.");

                return;
            }

            SerializedObject serialized =
                new SerializedObject(roleSwitcher);

            SerializedProperty behaviours =
                serialized.FindProperty(
                    "painterBehaviours");

            if (behaviours == null ||
                !behaviours.isArray)
            {
                Debug.LogWarning(
                    "PrototypeRoleSwitcher içinde " +
                    "'painterBehaviours' dizisi bulunamadı.",
                    roleSwitcher);

                return;
            }

            for (int i = 0;
                 i < behaviours.arraySize;
                 i++)
            {
                SerializedProperty element =
                    behaviours.GetArrayElementAtIndex(i);

                if (element.objectReferenceValue ==
                    budgetHud)
                {
                    return;
                }
            }

            Undo.RecordObject(
                roleSwitcher,
                "Add Painter Budget HUD Behaviour");

            int newIndex =
                behaviours.arraySize;

            behaviours.InsertArrayElementAtIndex(
                newIndex);

            behaviours
                .GetArrayElementAtIndex(newIndex)
                .objectReferenceValue =
                budgetHud;

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(
                roleSwitcher);
        }

        // =========================================================
        // SERIALIZATION HELPERS
        // =========================================================

        private static void SetSceneObjectReferences(
            Component component,
            params ObjectAssignment[] assignments)
        {
            if (component == null)
            {
                return;
            }

            Undo.RecordObject(
                component,
                $"Configure {component.GetType().Name}");

            SerializedObject serialized =
                new SerializedObject(component);

            foreach (ObjectAssignment assignment
                     in assignments)
            {
                SerializedProperty property =
                    serialized.FindProperty(
                        assignment.PropertyName);

                if (property == null)
                {
                    Debug.LogWarning(
                        $"{component.GetType().Name} içinde " +
                        $"'{assignment.PropertyName}' alanı bulunamadı.",
                        component);

                    continue;
                }

                property.objectReferenceValue =
                    assignment.Value;
            }

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(component);
        }

        private static void AssignObjectOnlyWhenEmpty(
            Component component,
            string propertyName,
            UnityEngine.Object value)
        {
            if (component == null ||
                value == null)
            {
                return;
            }

            SerializedObject serialized =
                new SerializedObject(component);

            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            if (property == null)
            {
                return;
            }

            if (property.objectReferenceValue != null)
            {
                return;
            }

            Undo.RecordObject(
                component,
                $"Assign {propertyName}");

            property.objectReferenceValue =
                value;

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(component);
        }

        private static void SetFloatIfPresent(
            Component component,
            float value,
            string propertyName)
        {
            if (component == null)
            {
                return;
            }

            SerializedObject serialized =
                new SerializedObject(component);

            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            if (property == null ||
                property.propertyType !=
                SerializedPropertyType.Float)
            {
                return;
            }

            Undo.RecordObject(
                component,
                $"Set {propertyName}");

            property.floatValue = value;

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(component);
        }

        private static void SetFloat(
            SerializedObject serialized,
            float value,
            params string[] possibleNames)
        {
            SerializedProperty property =
                FindFirstProperty(
                    serialized,
                    possibleNames);

            if (property == null)
            {
                LogMissingProperty(
                    serialized,
                    possibleNames);

                return;
            }

            property.floatValue = value;
        }

        private static void SetInt(
            SerializedObject serialized,
            int value,
            params string[] possibleNames)
        {
            SerializedProperty property =
                FindFirstProperty(
                    serialized,
                    possibleNames);

            if (property == null)
            {
                LogMissingProperty(
                    serialized,
                    possibleNames);

                return;
            }

            property.intValue = value;
        }

        private static SerializedProperty FindFirstProperty(
            SerializedObject serialized,
            string[] possibleNames)
        {
            foreach (string propertyName
                     in possibleNames)
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

        private static void LogMissingProperty(
            SerializedObject serialized,
            string[] possibleNames)
        {
            Debug.LogWarning(
                $"{serialized.targetObject.GetType().Name} içinde " +
                $"şu alan bulunamadı: " +
                $"{string.Join(" / ", possibleNames)}",
                serialized.targetObject);
        }

        // =========================================================
        // TYPE AND COMPONENT HELPERS
        // =========================================================

        private static Component GetOrAddComponent(
            GameObject gameObject,
            string typeName)
        {
            Type type =
                FindMonoBehaviourType(typeName);

            if (type == null)
            {
                Debug.LogError(
                    $"'{typeName}' sınıfı bulunamadı. " +
                    "Console'da derleme hatası olmadığını kontrol et.");

                return null;
            }

            Component existing =
                gameObject.GetComponent(type);

            if (existing != null)
            {
                return existing;
            }

            return Undo.AddComponent(
                gameObject,
                type);
        }

        private static Component FindComponentOnGameObject(
            GameObject gameObject,
            string typeName)
        {
            Type type =
                FindMonoBehaviourType(typeName);

            if (type == null)
            {
                return null;
            }

            return gameObject.GetComponent(type);
        }

        private static Component FindSceneComponent(
            string typeName)
        {
            Type type =
                FindMonoBehaviourType(typeName);

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
            foreach (Type type in
                     TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                if (string.Equals(
                        type.Name,
                        typeName,
                        StringComparison.Ordinal))
                {
                    return type;
                }
            }

            return null;
        }

        private static Type FindScriptableObjectType(
            string[] preferredNames,
            string[] requiredNameFragments)
        {
            foreach (Type type in
                     TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                foreach (string preferredName
                         in preferredNames)
                {
                    if (string.Equals(
                            type.Name,
                            preferredName,
                            StringComparison.Ordinal))
                    {
                        return type;
                    }
                }
            }

            foreach (Type type in
                     TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                bool containsAll = true;

                foreach (string fragment
                         in requiredNameFragments)
                {
                    if (type.Name.IndexOf(
                            fragment,
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        containsAll = false;
                        break;
                    }
                }

                if (containsAll)
                {
                    return type;
                }
            }

            return null;
        }

        // =========================================================
        // SCENE AND ASSET HELPERS
        // =========================================================

        private static Transform FindSceneTransform(
            string objectName)
        {
            Transform[] transforms =
                Resources.FindObjectsOfTypeAll<Transform>();

            foreach (Transform candidate
                     in transforms)
            {
                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    EditorUtility.IsPersistent(candidate))
                {
                    continue;
                }

                if (candidate.name == objectName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(
            Transform parent,
            string childName)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child
                     in parent)
            {
                if (child.name == childName)
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

        private static ScriptableObject
            FindScriptableObjectAssetByName(
                string assetName)
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    assetName + " t:ScriptableObject",
                    new[]
                    {
                        "Assets/_Project"
                    });

            foreach (string guid in guids)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guid);

                if (Path.GetFileNameWithoutExtension(path) !=
                    assetName)
                {
                    continue;
                }

                ScriptableObject asset =
                    AssetDatabase.LoadAssetAtPath<
                        ScriptableObject>(path);

                if (asset != null)
                {
                    return asset;
                }
            }

            return null;
        }

        private static void EnsureFolderExists(
            string folderPath)
        {
            string[] parts =
                folderPath.Split('/');

            if (parts.Length == 0 ||
                parts[0] != "Assets")
            {
                throw new ArgumentException(
                    "Klasör yolu Assets ile başlamalıdır.",
                    nameof(folderPath));
            }

            string current = "Assets";

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

        private static void RemoveChildIfExists(
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

        private static Color ParseColor(
            string hex)
        {
            if (ColorUtility.TryParseHtmlString(
                    hex,
                    out Color color))
            {
                return color;
            }

            return Color.white;
        }

        // =========================================================
        // DATA TYPES
        // =========================================================

        private sealed class RuntimeReferences
        {
            public GameObject PaintRuntime;
            public Component Budget;
            public Component StrokeSystem;
            public Component BrushController;
            public Component ModeSelector;
            public Component PigmentReservoir;
        }

        private readonly struct ObjectAssignment
        {
            public ObjectAssignment(
                string propertyName,
                UnityEngine.Object value)
            {
                PropertyName = propertyName;
                Value = value;
            }

            public string PropertyName { get; }
            public UnityEngine.Object Value { get; }
        }
    }
}
