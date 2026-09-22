#if UNITY_EDITOR
using System;
using System.Linq;
using PaintedAlive.Painters.SideCanvas;
using PaintedAlive.Painters.Masterpiece;
using PaintedAlive.UI.UnifiedHUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeLivingSideCanvasMilestone
    {
        private const string RootName =
            "M45_LivingSideCanvasRiskSpikeA";

        private const string CanvasName =
            "M45_LivingSideCanvasCanvas";

        private static readonly Color Ink =
            new Color(0.055f, 0.050f, 0.045f, 0.97f);

        private static readonly Color Paper =
            new Color(0.94f, 0.90f, 0.80f, 1f);

        private static readonly Color CanvasPaper =
            new Color(0.91f, 0.87f, 0.77f, 1f);

        private static readonly Color Cyan =
            new Color(0.08f, 0.70f, 0.75f, 1f);

        private static readonly Color Orange =
            new Color(1f, 0.54f, 0.06f, 1f);

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "45 - Apply Living Side Canvas Risk Spike A")]
        public static void Apply()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M45 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                PrototypeUnifiedHudController unifiedHud =
                    FindRequired<
                        PrototypeUnifiedHudController>();

                MonoBehaviour painterBrush =
                    FindRequiredMonoBehaviour(
                        "PainterBrushController");

                MonoBehaviour[] worldInputBlockers =
                    FindPainterWorldInputBlockers(
                        painterBrush);

                GameObject root =
                    GetOrCreateRoot(RootName);

                PrototypeLivingSideCanvasController controller =
                    GetOrAdd<
                        PrototypeLivingSideCanvasController>(
                            root);

                GameObject canvasObject =
                    GetOrCreateChild(
                        root.transform,
                        CanvasName,
                        typeof(RectTransform),
                        typeof(Canvas),
                        typeof(CanvasScaler),
                        typeof(GraphicRaycaster));

                Canvas canvas =
                    canvasObject.GetComponent<Canvas>();

                canvas.renderMode =
                    RenderMode.ScreenSpaceOverlay;

                canvas.sortingOrder = 850;
                canvas.overrideSorting = true;

                CanvasScaler scaler =
                    canvasObject.GetComponent<CanvasScaler>();

                scaler.uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;

                scaler.referenceResolution =
                    new Vector2(1920f, 1080f);

                scaler.screenMatchMode =
                    CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

                scaler.matchWidthOrHeight = 0.5f;

                RectTransform canvasRect =
                    canvasObject.GetComponent<RectTransform>();

                SetStretch(canvasRect);

                Image background =
                    GetOrAdd<Image>(canvasObject);

                background.color = Ink;

                CanvasGroup rootGroup =
                    GetOrAdd<CanvasGroup>(canvasObject);

                rootGroup.alpha = 0f;
                rootGroup.interactable = false;
                rootGroup.blocksRaycasts = false;

                RectTransform header =
                    CreatePanel(
                        canvasRect,
                        "Header",
                        new Vector2(0f, 1f),
                        new Vector2(1f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0f, -20f),
                        new Vector2(-80f, 72f),
                        new Color(
                            0.035f,
                            0.032f,
                            0.030f,
                            0.98f));

                CreateText(
                    header,
                    "Title",
                    "CANLI YAN TUVAL",
                    28,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(28f, 0f),
                    new Vector2(440f, 48f),
                    Paper);

                Text modeText =
                    CreateText(
                        header,
                        "Mode",
                        "01 • ÇİZ",
                        24,
                        FontStyle.Bold,
                        TextAnchor.MiddleRight,
                        new Vector2(1f, 0.5f),
                        new Vector2(1f, 0.5f),
                        new Vector2(1f, 0.5f),
                        new Vector2(-28f, 0f),
                        new Vector2(360f, 48f),
                        Orange);

                RectTransform drawingPanel =
                    CreatePanel(
                        canvasRect,
                        "DrawingPanel",
                        new Vector2(0f, 0f),
                        new Vector2(0.69f, 1f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(30f, -38f),
                        new Vector2(-80f, -160f),
                        CanvasPaper);

                // Unity UI Graphic components must not share the same
                // GameObject here. DrawingPanel keeps its background Image,
                // while the custom stroke renderer lives on a stretched child.
                PrototypeSideCanvasSurfaceGraphic legacySurface =
                    drawingPanel.GetComponent<
                        PrototypeSideCanvasSurfaceGraphic>();

                if (legacySurface != null)
                {
                    Undo.DestroyObjectImmediate(legacySurface);
                }

                RectTransform surfaceRect =
                    GetOrCreateRect(
                        drawingPanel,
                        "SurfaceGraphic",
                        Vector2.zero,
                        Vector2.one,
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        Vector2.zero);

                RawImage surfaceImage =
                    GetOrAdd<RawImage>(
                        surfaceRect.gameObject);

                surfaceImage.color = Color.white;
                surfaceImage.raycastTarget = false;

                PrototypeSideCanvasSurfaceGraphic surface =
                    GetOrAdd<
                        PrototypeSideCanvasSurfaceGraphic>(
                            surfaceRect.gameObject);

                PrototypeSideCanvasPointerInput pointerInput =
                    GetOrAdd<
                        PrototypeSideCanvasPointerInput>(
                            surfaceRect.gameObject);

                RectTransform previewPanel =
                    CreatePanel(
                        canvasRect,
                        "PreviewPanel",
                        new Vector2(0.71f, 0.49f),
                        new Vector2(0.98f, 0.91f),
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        Vector2.zero,
                        new Color(
                            0.90f,
                            0.86f,
                            0.76f,
                            1f));

                CreateText(
                    previewPanel,
                    "PreviewTitle",
                    "YÜRÜYEN KUKLA • ÖN İZLEME",
                    17,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -16f),
                    new Vector2(-20f, 32f),
                    Ink);

                RectTransform puppetRect =
                    GetOrCreateRect(
                        previewPanel,
                        "PuppetGraphic",
                        new Vector2(0.06f, 0.08f),
                        new Vector2(0.94f, 0.84f),
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        Vector2.zero);

                RawImage puppetImage =
                    GetOrAdd<RawImage>(
                        puppetRect.gameObject);

                puppetImage.color = Color.white;
                puppetImage.raycastTarget = false;

                PrototypeSideCanvasPuppetGraphic puppet =
                    GetOrAdd<
                        PrototypeSideCanvasPuppetGraphic>(
                            puppetRect.gameObject);

                RectTransform infoPanel =
                    CreatePanel(
                        canvasRect,
                        "InfoPanel",
                        new Vector2(0.71f, 0.11f),
                        new Vector2(0.98f, 0.46f),
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        Vector2.zero,
                        new Color(
                            0.035f,
                            0.032f,
                            0.030f,
                            0.98f));

                Text markerText =
                    CreateText(
                        infoPanel,
                        "MarkerStatus",
                        "RIG MARKER SIRASI",
                        15,
                        FontStyle.Normal,
                        TextAnchor.UpperLeft,
                        new Vector2(0f, 0f),
                        new Vector2(0.54f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(20f, -18f),
                        new Vector2(-30f, -36f),
                        Paper);

                Text draftSummary =
                    CreateText(
                        infoPanel,
                        "DraftSummary",
                        "STROKE 0/64\nMARKER 0/6\nRIG EKSİK\nAKTARIM KAPALI • AI YOK",
                        14,
                        FontStyle.Bold,
                        TextAnchor.UpperLeft,
                        new Vector2(0.55f, 0f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(6f, -18f),
                        new Vector2(-26f, -36f),
                        Cyan);

                Text statusText =
                    CreateText(
                        canvasRect,
                        "StatusText",
                        "C • AÇ/KAPAT   TAB • SONRAKİ AŞAMA   Z • GERİ AL   DELETE • TEMİZLE",
                        16,
                        FontStyle.Bold,
                        TextAnchor.MiddleCenter,
                        new Vector2(0.02f, 0f),
                        new Vector2(0.98f, 0f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0f, 58f),
                        new Vector2(0f, 36f),
                        Orange);

                Text helpText =
                    CreateText(
                        canvasRect,
                        "HelpText",
                        "SOL TIK: ÇİZ / MARKER SÜRÜKLE   •   1-6: RENK   •   OKLAR: X/Y   •   PGUP/PGDN: Z   •   HOME: SIFIRLA   •   ESC: KAPAT",
                        13,
                        FontStyle.Normal,
                        TextAnchor.MiddleCenter,
                        new Vector2(0.02f, 0f),
                        new Vector2(0.98f, 0f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0f, 24f),
                        new Vector2(0f, 28f),
                        Paper);

                helpText.raycastTarget = false;

                RectTransform toolbarRect =
                    CreatePanel(
                        canvasRect,
                        "Toolbar",
                        new Vector2(0.18f, 0f),
                        new Vector2(0.98f, 0f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0f, 78f),
                        new Vector2(0f, 52f),
                        new Color(
                            0.035f,
                            0.032f,
                            0.030f,
                            0.96f));

                RectTransform paletteRect =
                    CreatePanel(
                        canvasRect,
                        "ColorPalette",
                        new Vector2(0.02f, 0f),
                        new Vector2(0.17f, 0f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0f, 78f),
                        new Vector2(0f, 52f),
                        new Color(0.035f, 0.032f, 0.030f, 0.96f));

                Button[] colorButtons = new Button[6];
                Text[] colorLabels = new Text[6];

                for (int colorIndex = 0; colorIndex < colorButtons.Length; colorIndex++)
                {
                    float minimum = colorIndex / 6f;
                    float maximum = (colorIndex + 1) / 6f;
                    Color swatch = controller.GetDrawingPaletteColor(colorIndex);

                    colorButtons[colorIndex] =
                        CreateButton(
                            paletteRect,
                            $"ColorButton_{colorIndex + 1}",
                            (colorIndex + 1).ToString(),
                            new Vector2(minimum, 0f),
                            new Vector2(maximum, 1f),
                            swatch,
                            colorIndex == 5 ? Ink : Paper,
                            out colorLabels[colorIndex]);

                }

                Button advanceButton =
                    CreateButton(
                        toolbarRect,
                        "AdvanceButton",
                        "RİGLE",
                        new Vector2(0.00f, 0f),
                        new Vector2(0.18f, 1f),
                        Orange,
                        Paper,
                        out Text advanceLabel);

                Button undoButton =
                    CreateButton(
                        toolbarRect,
                        "UndoButton",
                        "GERİ AL",
                        new Vector2(0.19f, 0f),
                        new Vector2(0.33f, 1f),
                        new Color(0.15f, 0.14f, 0.13f, 1f),
                        Paper,
                        out _);

                Button clearButton =
                    CreateButton(
                        toolbarRect,
                        "ClearButton",
                        "TEMİZLE",
                        new Vector2(0.34f, 0f),
                        new Vector2(0.48f, 1f),
                        new Color(0.24f, 0.09f, 0.07f, 1f),
                        Paper,
                        out _);

                Button attackButton =
                    CreateButton(
                        toolbarRect,
                        "AttackButton",
                        "TEST KİLİTLİ",
                        new Vector2(0.49f, 0f),
                        new Vector2(0.66f, 1f),
                        Cyan,
                        Ink,
                        out Text attackLabel);

                Button deployButton =
                    CreateButton(
                        toolbarRect,
                        "DeployButton",
                        "AKTARIM KİLİTLİ",
                        new Vector2(0.67f, 0f),
                        new Vector2(0.84f, 1f),
                        new Color(0.08f, 0.42f, 0.31f, 1f),
                        Paper,
                        out Text deployLabel);

                Button closeButton =
                    CreateButton(
                        toolbarRect,
                        "CloseButton",
                        "KAPAT",
                        new Vector2(0.85f, 0f),
                        new Vector2(1.00f, 1f),
                        new Color(0.12f, 0.11f, 0.10f, 1f),
                        Paper,
                        out _);

                RectTransform cursorRect =
                    GetOrCreateRect(
                        canvasRect,
                        "SoftwareCursor",
                        Vector2.zero,
                        Vector2.zero,
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        new Vector2(22f, 22f));

                cursorRect.localRotation =
                    Quaternion.Euler(0f, 0f, 45f);

                Image cursorOuter =
                    GetOrAdd<Image>(
                        cursorRect.gameObject);

                cursorOuter.color = Orange;
                cursorOuter.raycastTarget = false;

                RectTransform cursorInnerRect =
                    GetOrCreateRect(
                        cursorRect,
                        "Inner",
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        Vector2.zero,
                        new Vector2(10f, 10f));

                Image cursorInner =
                    GetOrAdd<Image>(
                        cursorInnerRect.gameObject);

                cursorInner.color = Ink;
                cursorInner.raycastTarget = false;

                CanvasGroup cursorGroup =
                    GetOrAdd<CanvasGroup>(
                        cursorRect.gameObject);

                cursorGroup.alpha = 0f;
                cursorGroup.interactable = false;
                cursorGroup.blocksRaycasts = false;

                controller.Configure(
                    unifiedHud,
                    painterBrush,
                    worldInputBlockers,
                    rootGroup,
                    drawingPanel,
                    surface,
                    puppet,
                    modeText,
                    statusText,
                    markerText,
                    draftSummary);

                controller.ResetPrototypeDraftForSetup();

                pointerInput.Configure(
                    controller,
                    surfaceRect);

                PrototypeSideCanvasSoftwareCursor softwareCursor =
                    GetOrAdd<
                        PrototypeSideCanvasSoftwareCursor>(
                            cursorRect.gameObject);

                softwareCursor.Configure(
                    controller,
                    cursorRect,
                    cursorGroup,
                    pointerInput,
                    advanceButton,
                    undoButton,
                    clearButton,
                    attackButton,
                    deployButton,
                    closeButton,
                    colorButtons);

                PrototypeSideCanvasToolbar toolbar =
                    GetOrAdd<
                        PrototypeSideCanvasToolbar>(
                            toolbarRect.gameObject);

                toolbar.Configure(
                    controller,
                    advanceButton,
                    undoButton,
                    clearButton,
                    attackButton,
                    deployButton,
                    closeButton,
                    advanceLabel,
                    attackLabel,
                    deployLabel,
                    colorButtons,
                    colorLabels,
                    UnityEngine.Object.FindFirstObjectByType<
                        PrototypeMasterpieceWorldDeploymentController>(
                            FindObjectsInactive.Include));

                EditorUtility.SetDirty(root);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(surface);
                EditorUtility.SetDirty(puppet);
                EditorUtility.SetDirty(pointerInput);
                EditorUtility.SetDirty(softwareCursor);
                EditorUtility.SetDirty(toolbar);
                EditorUtility.SetDirty(canvasObject);
                EditorSceneManager.MarkSceneDirty(root.scene);
                EditorSceneManager.SaveOpenScenes();

                Debug.Log(
                    "[M45 Setup] Living Side Canvas Risk Spike A ready.\n" +
                    $"Root={GetHierarchyPath(root.transform)}\n" +
                    $"UnifiedHud={GetHierarchyPath(unifiedHud.transform)}\n" +
                    $"PainterBrush={Describe(painterBrush)}\n" +
                    $"WorldInputBlockerCount={worldInputBlockers.Length}\n" +
                    $"WorldInputBlockers={DescribeMany(worldInputBlockers)}\n" +
                    $"ControllerCount={CountSceneObjects<PrototypeLivingSideCanvasController>()}\n" +
                    $"SurfaceCount={CountSceneObjects<PrototypeSideCanvasSurfaceGraphic>()}\n" +
                    $"PuppetCount={CountSceneObjects<PrototypeSideCanvasPuppetGraphic>()}\n" +
                    $"ToolbarCount={CountSceneObjects<PrototypeSideCanvasToolbar>()}\n" +
                    $"ToolbarConfigured={toolbar.IsConfigured}\n" +
                    $"PointerInputConfigured={pointerInput.IsConfigured}\n" +
                    $"SoftwareCursorConfigured={softwareCursor.IsConfigured}\n" +
                    "VirtualCursorUsesRelativeDelta=True\n" +
                    "HardwareCursorLockedAndHidden=True\n" +
                    "ManualDrawingHitTesting=True\n" +
                    "ManualToolbarHitTesting=True\n" +
                    "EventSystemPointerPositionIgnored=True\n" +
                    $"SurfaceRawImage={surface.UsesStandardRawImage}\n" +
                    $"PuppetRawImage={puppet.UsesStandardRawImage}\n" +
                    "SurfaceRenderer=StandardRawImageTexture2D\n" +
                    "PuppetRenderer=StandardRawImageTexture2D\n" +
                    "RawMousePolling=False\n" +
                    "KeyboardMouseOnly=True\n" +
                    "ControllerSupport=False\n" +
                    "MatchTimePaused=False\n" +
                    "WorldPaintingLockedOnlyWhileCanvasOpen=True\n" +
                    "PainterCameraLookLockedWhileCanvasOpen=True\n" +
                    "CursorAuthority=SideCanvas\n" +
                    "DraftPersistsAfterClose=True\n" +
                    "SixRigMarkers=True\n" +
                    "PuppetPreview=True\n" +
                    "SurfaceGraphicSeparateChild=True\n" +
                    "SetupIdempotent=True\n" +
                    "DeployEnabled=TrueWhenPreviewValid\n" +
                    "DeploymentScale3D=True\n" +
                    "DeployToolbarButton=True\n" +
                    "BossAIEnabled=False\n" +
                    "GameplayAuthoritiesModified=False\n" +
                    "RealtimeNetworkPreview=M56",
                    root);

                EditorUtility.DisplayDialog(
                    "M45 tamamlandı",
                    "Painter için klavye/fare Canlı Yan Tuval Risk Spike A kuruldu. " +
                    "Play Mode'da F2 ile Painter'a geç, C ile tuvali aç.",
                    "Tamam");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "M45 kurulumu başarısız",
                    exception.Message,
                    "Tamam");
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "45 - Load Side Canvas Demo Draft (Play Mode)")]
        public static void LoadDemo()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "M45 Demo",
                    "Önce Play Mode'a gir ve F2 ile Painter rolüne geç.",
                    "Tamam");

                return;
            }

            PrototypeLivingSideCanvasController controller =
                FindRequired<
                    PrototypeLivingSideCanvasController>();

            controller.LoadDemoDraft();

            Debug.Log(
                "[M45 Demo] Six-marker walking puppet draft loaded.",
                controller);
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "45 - Diagnose Living Side Canvas Risk Spike A")]
        public static void Diagnose()
        {
            PrototypeLivingSideCanvasController controller =
                FindOptional<
                    PrototypeLivingSideCanvasController>();

            PrototypeSideCanvasSurfaceGraphic surface =
                FindOptional<
                    PrototypeSideCanvasSurfaceGraphic>();

            PrototypeSideCanvasPuppetGraphic puppet =
                FindOptional<
                    PrototypeSideCanvasPuppetGraphic>();

            PrototypeSideCanvasPointerInput pointerInput =
                FindOptional<
                    PrototypeSideCanvasPointerInput>();

            PrototypeSideCanvasSoftwareCursor softwareCursor =
                FindOptional<
                    PrototypeSideCanvasSoftwareCursor>();

            Debug.Log(
                "[M45 Diagnose]\n" +
                $"Controller={(controller != null ? "OK" : "MISSING")}\n" +
                $"ControllerCount={CountSceneObjects<PrototypeLivingSideCanvasController>()}\n" +
                $"IsOpen={(controller != null && controller.IsOpen)}\n" +
                $"Mode={(controller != null ? controller.CurrentMode.ToString() : "N/A")}\n" +
                $"StrokeCount={(controller != null ? controller.StrokeCount : 0)}\n" +
                $"MarkerCount={(controller != null ? controller.MarkerCount : 0)}\n" +
                $"HasCompleteRig={(controller != null && controller.HasCompleteRig)}\n" +
                $"RigValid={(controller != null && controller.RigValid)}\n" +
                $"Validation={(controller != null ? controller.ValidationMessage : "N/A")}\n" +
                $"OpenedCount={(controller != null ? controller.OpenedCount : 0)}\n" +
                $"CompletedPreviewCount={(controller != null ? controller.CompletedPreviewCount : 0)}\n" +
                $"InputConflictCount={(controller != null ? controller.InputConflictCount : 0)}\n" +
                $"ConfiguredInputBlockers={(controller != null ? controller.ConfiguredInputBlockerCount : 0)}\n" +
                $"CapturedInputBlockers={(controller != null ? controller.CapturedInputBlockerCount : 0)}\n" +
                $"CursorAuthorityActive={(controller != null && controller.CursorAuthorityActive)}\n" +
                $"KeyboardActionCount={(controller != null ? controller.KeyboardActionCount : 0)}\n" +
                $"ToolbarActionCount={(controller != null ? controller.ToolbarActionCount : 0)}\n" +
                $"PointerEventCount={(controller != null ? controller.PointerEventCount : 0)}\n" +
                $"SurfaceCount={CountSceneObjects<PrototypeSideCanvasSurfaceGraphic>()}\n" +
                $"SurfaceTextureReady={(surface != null && surface.TextureReady)}\n" +
                $"SurfaceTextureSize={(surface != null ? surface.TextureWidth + "x" + surface.TextureHeight : "N/A")}\n" +
                $"SurfaceRebuildCount={(surface != null ? surface.RebuildCount : 0)}\n" +
                $"PuppetRawImage={(puppet != null && puppet.UsesStandardRawImage)}\n" +
                $"PuppetPreviewVisible={(puppet != null && puppet.PreviewVisible)}\n" +
                $"PointerInputConfigured={(pointerInput != null && pointerInput.IsConfigured)}\n" +
                $"PointerEventsReceived={(pointerInput != null ? pointerInput.ReceivedEventCount : 0)}\n" +
                $"SoftwareCursorConfigured={(softwareCursor != null && softwareCursor.IsConfigured)}\n" +
                $"SoftwareCursorVisibleFrames={(softwareCursor != null ? softwareCursor.VisibleFrameCount : 0)}\n" +
                $"VirtualCursorPosition={(softwareCursor != null ? softwareCursor.VirtualScreenPosition.ToString() : "N/A")}\n" +
                $"VirtualCursorDeltaFrames={(softwareCursor != null ? softwareCursor.DeltaFrameCount : 0)}\n" +
                $"VirtualDrawingGestures={(softwareCursor != null ? softwareCursor.DrawingGestureCount : 0)}\n" +
                $"VirtualToolbarClicks={(softwareCursor != null ? softwareCursor.ToolbarClickCount : 0)}\n" +
                $"VirtualHoveredTarget={(softwareCursor != null ? softwareCursor.HoveredTarget : "N/A")}\n" +
                $"ToolbarCount={CountSceneObjects<PrototypeSideCanvasToolbar>()}\n" +
                "VirtualCursorUsesRelativeDelta=True\n" +
                "HardwareCursorLockedAndHidden=True\n" +
                "ManualDrawingHitTesting=True\n" +
                "ManualToolbarHitTesting=True\n" +
                "EventSystemPointerPositionIgnored=True\n" +
                "SurfaceRenderer=StandardRawImageTexture2D\n" +
                "PuppetRenderer=StandardRawImageTexture2D\n" +
                "RawMousePolling=False\n" +
                "KeyboardMouseOnly=True\n" +
                "ControllerSupport=False\n" +
                "MatchTimePaused=False\n" +
                "DeployEnabled=False\n" +
                "BossAIEnabled=False\n" +
                "GameplayAuthoritiesModified=False\n" +
                "NetworkIntegrationParked=True");
        }

        private static GameObject GetOrCreateRoot(string name)
        {
            GameObject existing =
                GameObject.Find(name);

            if (existing != null)
            {
                return existing;
            }

            GameObject created =
                new GameObject(name);

            Undo.RegisterCreatedObjectUndo(
                created,
                $"Create {name}");

            return created;
        }

        private static GameObject GetOrCreateChild(
            Transform parent,
            string name,
            params Type[] components)
        {
            Transform existing =
                parent.Find(name);

            if (existing != null)
            {
                GameObject existingObject =
                    existing.gameObject;

                for (int index = 0;
                     index < components.Length;
                     index++)
                {
                    Type componentType =
                        components[index];

                    if (componentType ==
                        typeof(RectTransform))
                    {
                        if (!(existing is RectTransform))
                        {
                            throw new InvalidOperationException(
                                $"{GetHierarchyPath(existing)} " +
                                "RectTransform kullanmıyor.");
                        }

                        continue;
                    }

                    if (existingObject.GetComponent(
                            componentType) == null)
                    {
                        Component added =
                            Undo.AddComponent(
                                existingObject,
                                componentType);

                        if (added == null)
                        {
                            throw new InvalidOperationException(
                                $"{componentType.Name} eklenemedi: " +
                                GetHierarchyPath(existing));
                        }
                    }
                }

                return existingObject;
            }

            GameObject child =
                new GameObject(name, components);

            Undo.RegisterCreatedObjectUndo(
                child,
                $"Create {name}");

            child.transform.SetParent(parent, false);
            return child;
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            RectTransform rect =
                GetOrCreateRect(
                    parent,
                    name,
                    anchorMin,
                    anchorMax,
                    pivot,
                    anchoredPosition,
                    sizeDelta);

            Image image =
                GetOrAdd<Image>(rect.gameObject);

            image.color = color;
            image.raycastTarget = false;

            return rect;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color backgroundColor,
            Color textColor,
            out Text labelText)
        {
            RectTransform rect =
                GetOrCreateRect(
                    parent,
                    name,
                    anchorMin,
                    anchorMax,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(-6f, -8f));

            Image image =
                GetOrAdd<Image>(
                    rect.gameObject);

            image.color = backgroundColor;
            image.raycastTarget = true;

            Button button =
                GetOrAdd<Button>(
                    rect.gameObject);

            button.targetGraphic = image;
            button.transition =
                Selectable.Transition.ColorTint;

            ColorBlock colors =
                button.colors;

            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color(1f, 1f, 1f, 0.86f);

            colors.pressedColor =
                new Color(0.78f, 0.78f, 0.78f, 1f);

            colors.selectedColor = Color.white;
            colors.disabledColor =
                new Color(0.35f, 0.35f, 0.35f, 0.45f);

            colors.colorMultiplier = 1f;
            button.colors = colors;

            labelText =
                CreateText(
                    rect,
                    "Label",
                    label,
                    15,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(-8f, -6f),
                    textColor);

            return button;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            RectTransform rect =
                GetOrCreateRect(
                    parent,
                    name,
                    anchorMin,
                    anchorMax,
                    pivot,
                    anchoredPosition,
                    sizeDelta);

            Text text =
                GetOrAdd<Text>(rect.gameObject);

            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;

            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            if (text.font == null)
            {
                text.font =
                    Resources.GetBuiltinResource<Font>(
                        "LegacyRuntime.ttf");
            }

            return text;
        }

        private static RectTransform GetOrCreateRect(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            Transform existing =
                parent.Find(name);

            GameObject child =
                existing != null
                    ? existing.gameObject
                    : new GameObject(
                        name,
                        typeof(RectTransform));

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(
                    child,
                    $"Create {name}");

                child.transform.SetParent(
                    parent,
                    false);
            }

            RectTransform rect =
                child.GetComponent<RectTransform>();

            if (rect == null)
            {
                throw new InvalidOperationException(
                    $"{GetHierarchyPath(child.transform)} " +
                    "RectTransform içermiyor.");
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;

            return rect;
        }

        private static void SetStretch(
            RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static T GetOrAdd<T>(
            GameObject target)
            where T : Component
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target));
            }

            T existing =
                target.GetComponent<T>();

            if (existing != null)
            {
                return existing;
            }

            T added =
                Undo.AddComponent<T>(target);

            if (added == null)
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name} eklenemedi: " +
                    GetHierarchyPath(target.transform));
            }

            return added;
        }

        private static T FindRequired<T>()
            where T : Component
        {
            T found = FindOptional<T>();

            if (found == null)
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name} bulunamadı.");
            }

            return found;
        }

        private static T FindOptional<T>()
            where T : Component
        {
            T[] objects =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int index = 0;
                 index < objects.Length;
                 index++)
            {
                T candidate = objects[index];

                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }

        private static MonoBehaviour[]
            FindPainterWorldInputBlockers(
                MonoBehaviour painterBrush)
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object
                    .FindObjectsByType<MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            return behaviours
                .Where(candidate =>
                    IsPainterWorldInputBlocker(
                        candidate,
                        painterBrush))
                .Distinct()
                .OrderBy(
                    candidate =>
                        GetHierarchyPath(
                            candidate.transform),
                    StringComparer.Ordinal)
                .ToArray();
        }

        private static bool IsPainterWorldInputBlocker(
            MonoBehaviour candidate,
            MonoBehaviour painterBrush)
        {
            if (candidate == null ||
                candidate == painterBrush ||
                EditorUtility.IsPersistent(candidate) ||
                !candidate.gameObject.scene.IsValid())
            {
                return false;
            }

            Type type = candidate.GetType();
            string typeName = type.Name;
            string objectName =
                candidate.gameObject.name;

            string typeNamespace =
                type.Namespace ??
                string.Empty;

            if (typeNamespace.StartsWith(
                    "PaintedAlive.Painters.SideCanvas",
                    StringComparison.Ordinal) ||
                typeNamespace.StartsWith(
                    "PaintedAlive.UI",
                    StringComparison.Ordinal) ||
                typeNamespace.StartsWith(
                    "Unity.Cinemachine",
                    StringComparison.Ordinal) ||
                typeNamespace.StartsWith(
                    "Cinemachine",
                    StringComparison.Ordinal))
            {
                return false;
            }

            return ContainsAny(
                    typeName,
                    "InkPainterIndependentCamera",
                    "PainterIndependentCamera",
                    "PainterCameraInput",
                    "PainterCameraLook",
                    "PainterLookController") ||
                ContainsAny(
                    objectName,
                    "M21_InkPainterIndependentCamera",
                    "InkPainterIndependentCamera",
                    "PainterIndependentCamera");
        }

        private static bool ContainsAny(
            string value,
            params string[] fragments)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (int index = 0;
                 index < fragments.Length;
                 index++)
            {
                if (value.IndexOf(
                        fragments[index],
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string DescribeMany(
            MonoBehaviour[] behaviours)
        {
            if (behaviours == null ||
                behaviours.Length == 0)
            {
                return "None";
            }

            return string.Join(
                " | ",
                behaviours.Select(Describe));
        }

        private static MonoBehaviour
            FindRequiredMonoBehaviour(
                string typeName)
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object
                    .FindObjectsByType<MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    behaviours[index];

                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid() &&
                    string.Equals(
                        candidate.GetType().Name,
                        typeName,
                        StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                $"{typeName} bulunamadı.");
        }

        private static int CountSceneObjects<T>()
            where T : Component
        {
            return UnityEngine.Object
                .FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Count(candidate =>
                    candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid());
        }

        private static string Describe(
            Component component)
        {
            return component == null
                ? "MISSING"
                : $"{component.GetType().Name} @ " +
                  GetHierarchyPath(
                      component.transform);
        }

        private static string GetHierarchyPath(
            Transform transform)
        {
            string path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path =
                    transform.name +
                    "/" +
                    path;
            }

            return path;
        }
    }
}
#endif
