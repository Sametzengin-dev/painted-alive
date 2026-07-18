using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaintedAlive.EditorTools
{
    public static class CreateStainAbilityHud
    {
        private const string CanvasName = "Canvas";
        private const string FigureHudName = "FigureHUD";
        private const string PanelName = "StainAbilityPanel";

        private const string StainAbilityHudTypeName =
            "StainAbilityHUD";

        private const string FigureClarityStateTypeName =
            "FigureClarityState";

        private const string StainMarkControllerTypeName =
            "StainMarkController";

        private const string RoleSwitcherTypeName =
            "PrototypeRoleSwitcher";

        [MenuItem(
            "Tools/Painted Alive/UI/Create Stain Ability HUD")]
        public static void Create()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning(
                    "Stain Ability HUD kurulumu Play Mode dışında yapılmalıdır.");

                return;
            }

            Canvas canvas = FindCanvas();

            if (canvas == null)
            {
                Debug.LogError(
                    $"Aktif sahnede '{CanvasName}' isimli Canvas bulunamadı.");

                return;
            }

            Transform figureHud =
                canvas.transform.Find(FigureHudName);

            if (figureHud == null)
            {
                Debug.LogError(
                    $"Canvas altında '{FigureHudName}' bulunamadı.",
                    canvas);

                return;
            }

            Transform existingPanel =
                figureHud.Find(PanelName);

            if (existingPanel != null)
            {
                bool recreate =
                    EditorUtility.DisplayDialog(
                        "StainAbilityPanel mevcut",
                        "Mevcut StainAbilityPanel silinip yeniden oluşturulsun mu?",
                        "Yeniden Oluştur",
                        "İptal");

                if (!recreate)
                {
                    Selection.activeGameObject =
                        existingPanel.gameObject;

                    return;
                }

                RemoveFromRoleSwitcher(existingPanel.gameObject);

                Undo.DestroyObjectImmediate(
                    existingPanel.gameObject);
            }

            GameObject panel =
                CreatePanel(figureHud);

            Text abilityText =
                CreateAbilityText(panel.transform);

            Slider cooldownSlider =
                CreateCooldownSlider(panel.transform);

            CanvasGroup canvasGroup =
                panel.GetComponent<CanvasGroup>();

            Component stainAbilityHud =
                AddStainAbilityHud(panel);

            if (stainAbilityHud != null)
            {
                ConnectHudReferences(
                    stainAbilityHud,
                    canvasGroup,
                    abilityText,
                    cooldownSlider);

                AddToFigureBehaviours(
                    stainAbilityHud);
            }

            EditorUtility.SetDirty(panel);

            EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene());

            Selection.activeGameObject = panel;
            EditorGUIUtility.PingObject(panel);

            Debug.Log(
                "StainAbilityPanel oluşturuldu. " +
                "Bulunabilen component referansları otomatik bağlandı.",
                panel);
        }

        private static GameObject CreatePanel(
            Transform figureHud)
        {
            GameObject panel = new GameObject(
                PanelName,
                typeof(RectTransform),
                typeof(CanvasGroup));

            Undo.RegisterCreatedObjectUndo(
                panel,
                "Create Stain Ability Panel");

            panel.transform.SetParent(
                figureHud,
                false);

            RectTransform rectTransform =
                panel.GetComponent<RectTransform>();

            rectTransform.anchorMin =
                new Vector2(0f, 1f);

            rectTransform.anchorMax =
                new Vector2(0f, 1f);

            rectTransform.pivot =
                new Vector2(0f, 1f);

            rectTransform.anchoredPosition =
                new Vector2(24f, -96f);

            rectTransform.sizeDelta =
                new Vector2(280f, 55f);

            rectTransform.localRotation =
                Quaternion.identity;

            rectTransform.localScale =
                Vector3.one;

            CanvasGroup canvasGroup =
                panel.GetComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.ignoreParentGroups = false;

            return panel;
        }

        private static Text CreateAbilityText(
            Transform parent)
        {
            GameObject textObject = new GameObject(
                "AbilityText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));

            Undo.RegisterCreatedObjectUndo(
                textObject,
                "Create Stain Ability Text");

            textObject.transform.SetParent(
                parent,
                false);

            RectTransform rectTransform =
                textObject.GetComponent<RectTransform>();

            rectTransform.anchorMin =
                new Vector2(0f, 1f);

            rectTransform.anchorMax =
                new Vector2(1f, 1f);

            rectTransform.pivot =
                new Vector2(0f, 1f);

            rectTransform.anchoredPosition =
                new Vector2(10f, -4f);

            rectTransform.sizeDelta =
                new Vector2(-20f, 28f);

            rectTransform.localRotation =
                Quaternion.identity;

            rectTransform.localScale =
                Vector3.one;

            Text text =
                textObject.GetComponent<Text>();

            text.text = "G • YÖN İZİ";
            text.font = LoadLegacyFont();
            text.fontSize = 13;
            text.fontStyle = FontStyle.Bold;
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

        private static Slider CreateCooldownSlider(
            Transform parent)
        {
            GameObject sliderObject = new GameObject(
                "CooldownSlider",
                typeof(RectTransform),
                typeof(Slider));

            Undo.RegisterCreatedObjectUndo(
                sliderObject,
                "Create Stain Cooldown Slider");

            sliderObject.transform.SetParent(
                parent,
                false);

            RectTransform sliderRect =
                sliderObject.GetComponent<RectTransform>();

            sliderRect.anchorMin =
                new Vector2(0f, 0f);

            sliderRect.anchorMax =
                new Vector2(0f, 0f);

            sliderRect.pivot =
                new Vector2(0f, 0f);

            sliderRect.anchoredPosition =
                new Vector2(10f, 9f);

            sliderRect.sizeDelta =
                new Vector2(190f, 7f);

            sliderRect.localRotation =
                Quaternion.identity;

            sliderRect.localScale =
                Vector3.one;

            Image background =
                CreateSliderBackground(sliderRect);

            RectTransform fillArea =
                CreateFillArea(sliderRect);

            Image fill =
                CreateFill(fillArea);

            Slider slider =
                sliderObject.GetComponent<Slider>();

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction =
                Slider.Direction.LeftToRight;

            slider.SetValueWithoutNotify(1f);

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
            RectTransform sliderRect)
        {
            GameObject backgroundObject =
                new GameObject(
                    "Background",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            Undo.RegisterCreatedObjectUndo(
                backgroundObject,
                "Create Cooldown Background");

            backgroundObject.transform.SetParent(
                sliderRect,
                false);

            RectTransform rectTransform =
                backgroundObject.GetComponent<RectTransform>();

            StretchToParent(rectTransform);

            Image image =
                backgroundObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.08f,
                    0.025f,
                    0.045f,
                    0.85f);

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
                "Create Cooldown Fill Area");

            fillAreaObject.transform.SetParent(
                sliderRect,
                false);

            RectTransform rectTransform =
                fillAreaObject.GetComponent<RectTransform>();

            rectTransform.anchorMin =
                Vector2.zero;

            rectTransform.anchorMax =
                Vector2.one;

            rectTransform.offsetMin =
                new Vector2(1f, 1f);

            rectTransform.offsetMax =
                new Vector2(-1f, -1f);

            rectTransform.localRotation =
                Quaternion.identity;

            rectTransform.localScale =
                Vector3.one;

            return rectTransform;
        }

        private static Image CreateFill(
            RectTransform fillArea)
        {
            GameObject fillObject =
                new GameObject(
                    "Fill",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            Undo.RegisterCreatedObjectUndo(
                fillObject,
                "Create Cooldown Fill");

            fillObject.transform.SetParent(
                fillArea,
                false);

            RectTransform rectTransform =
                fillObject.GetComponent<RectTransform>();

            StretchToParent(rectTransform);

            rectTransform.pivot =
                new Vector2(0f, 0.5f);

            Image image =
                fillObject.GetComponent<Image>();

            image.color =
                ParseColor("#B99745");

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

        private static Component AddStainAbilityHud(
            GameObject panel)
        {
            Type type =
                FindMonoBehaviourType(
                    StainAbilityHudTypeName);

            if (type == null)
            {
                Debug.LogWarning(
                    $"{StainAbilityHudTypeName} sınıfı bulunamadı. " +
                    "Panel oluşturuldu ancak component eklenemedi.",
                    panel);

                return null;
            }

            Component existing =
                panel.GetComponent(type);

            if (existing != null)
            {
                return existing;
            }

            return Undo.AddComponent(
                panel,
                type);
        }

        private static void ConnectHudReferences(
            Component stainAbilityHud,
            CanvasGroup canvasGroup,
            Text abilityText,
            Slider cooldownSlider)
        {
            Component clarityState =
                FindSceneComponent(
                    FigureClarityStateTypeName,
                    "Figure_Player");

            Component markController =
                FindSceneComponent(
                    StainMarkControllerTypeName,
                    "Figure_Player");

            SerializedObject serializedObject =
                new SerializedObject(
                    stainAbilityHud);

            AssignObjectReference(
                serializedObject,
                "clarityState",
                clarityState);

            AssignObjectReference(
                serializedObject,
                "markController",
                markController);

            AssignObjectReference(
                serializedObject,
                "canvasGroup",
                canvasGroup);

            AssignObjectReference(
                serializedObject,
                "abilityText",
                abilityText);

            AssignObjectReference(
                serializedObject,
                "cooldownSlider",
                cooldownSlider);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(stainAbilityHud);

            if (clarityState == null)
            {
                Debug.LogWarning(
                    "Figure_Player üzerinde FigureClarityState bulunamadı.");
            }

            if (markController == null)
            {
                Debug.LogWarning(
                    "Figure_Player üzerinde StainMarkController bulunamadı.");
            }
        }

        private static void AddToFigureBehaviours(
            Component stainAbilityHud)
        {
            Component roleSwitcher =
                FindSceneComponent(
                    RoleSwitcherTypeName);

            if (roleSwitcher == null)
            {
                Debug.LogWarning(
                    "PrototypeRoleSwitcher bulunamadı. " +
                    "StainAbilityHUD, Figure Behaviours listesine eklenemedi.");

                return;
            }

            SerializedObject serializedObject =
                new SerializedObject(
                    roleSwitcher);

            SerializedProperty behaviours =
                serializedObject.FindProperty(
                    "figureBehaviours");

            if (behaviours == null ||
                !behaviours.isArray)
            {
                Debug.LogWarning(
                    "PrototypeRoleSwitcher içindeki " +
                    "'figureBehaviours' alanı bulunamadı.",
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
                    stainAbilityHud)
                {
                    return;
                }
            }

            int newIndex =
                behaviours.arraySize;

            behaviours.InsertArrayElementAtIndex(
                newIndex);

            behaviours
                .GetArrayElementAtIndex(newIndex)
                .objectReferenceValue =
                stainAbilityHud;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(roleSwitcher);
        }

        private static void RemoveFromRoleSwitcher(
            GameObject panel)
        {
            Component roleSwitcher =
                FindSceneComponent(
                    RoleSwitcherTypeName);

            if (roleSwitcher == null)
            {
                return;
            }

            SerializedObject serializedObject =
                new SerializedObject(
                    roleSwitcher);

            SerializedProperty behaviours =
                serializedObject.FindProperty(
                    "figureBehaviours");

            if (behaviours == null ||
                !behaviours.isArray)
            {
                return;
            }

            for (int i = behaviours.arraySize - 1;
                 i >= 0;
                 i--)
            {
                SerializedProperty element =
                    behaviours.GetArrayElementAtIndex(i);

                Component referencedComponent =
                    element.objectReferenceValue
                    as Component;

                if (referencedComponent != null &&
                    referencedComponent.gameObject ==
                    panel)
                {
                    behaviours.DeleteArrayElementAtIndex(i);

                    if (i < behaviours.arraySize &&
                        behaviours
                            .GetArrayElementAtIndex(i)
                            .objectReferenceValue != null)
                    {
                        behaviours.DeleteArrayElementAtIndex(i);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void AssignObjectReference(
            SerializedObject serializedObject,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serializedObject.FindProperty(
                    propertyName);

            if (property == null)
            {
                Debug.LogWarning(
                    $"{serializedObject.targetObject.GetType().Name} " +
                    $"içinde '{propertyName}' alanı bulunamadı.");

                return;
            }

            property.objectReferenceValue =
                value;
        }

        private static Canvas FindCanvas()
        {
            Canvas[] canvases =
                Resources.FindObjectsOfTypeAll<Canvas>();

            foreach (Canvas canvas in canvases)
            {
                if (canvas == null ||
                    !canvas.gameObject.scene.IsValid() ||
                    EditorUtility.IsPersistent(canvas))
                {
                    continue;
                }

                if (canvas.name == CanvasName)
                {
                    return canvas;
                }
            }

            return null;
        }

        private static Component FindSceneComponent(
            string typeName,
            string preferredGameObjectName = null)
        {
            Type type =
                FindMonoBehaviourType(typeName);

            if (type == null)
            {
                return null;
            }

            UnityEngine.Object[] objects =
                Resources.FindObjectsOfTypeAll(type);

            Component fallback = null;

            foreach (UnityEngine.Object item in objects)
            {
                Component component =
                    item as Component;

                if (component == null ||
                    !component.gameObject.scene.IsValid() ||
                    EditorUtility.IsPersistent(component))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(
                        preferredGameObjectName) &&
                    component.gameObject.name ==
                    preferredGameObjectName)
                {
                    return component;
                }

                fallback ??= component;
            }

            return fallback;
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

        private static Color ParseColor(
            string hex)
        {
            return ColorUtility.TryParseHtmlString(
                hex,
                out Color color)
                ? color
                : Color.white;
        }
    }
}
