using System;
using System.Collections.Generic;
using System.Reflection;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.Networking.M56;
using PaintedAlive.UI.UnifiedHUD;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PaintedAlive.Painters.SideCanvas
{
    [DefaultExecutionOrder(720)]
    [DisallowMultipleComponent]
    public sealed class PrototypeLivingSideCanvasController :
        MonoBehaviour
    {
        private static readonly PrototypeRigMarkerKind[]
            MarkerOrder =
            {
                PrototypeRigMarkerKind.Core,
                PrototypeRigMarkerKind.Head,
                PrototypeRigMarkerKind.LeftHand,
                PrototypeRigMarkerKind.RightHand,
                PrototypeRigMarkerKind.LeftFoot,
                PrototypeRigMarkerKind.RightFoot
            };

        private static readonly Color[] DrawingPalette =
        {
            new Color(0.055f, 0.050f, 0.045f, 1f),
            new Color(0.82f, 0.16f, 0.12f, 1f),
            new Color(0.10f, 0.39f, 0.84f, 1f),
            new Color(0.08f, 0.58f, 0.32f, 1f),
            new Color(0.48f, 0.22f, 0.72f, 1f),
            new Color(0.94f, 0.58f, 0.08f, 1f)
        };

        private static readonly string[] DrawingPaletteNames =
        {
            "Kömür", "Kızıl", "Safir", "Zümrüt", "Menekşe", "Altın"
        };

        [Header("Existing Sources")]
        [SerializeField]
        private PrototypeUnifiedHudController unifiedHudController;

        [SerializeField]
        private MonoBehaviour painterBrushController;

        [Header("Canvas Input Authority")]
        [SerializeField]
        private MonoBehaviour[] worldInputBlockers =
            Array.Empty<MonoBehaviour>();

        [SerializeField]
        private bool forceCursorWhileOpen = true;

        [Header("UI")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private RectTransform drawingArea;
        [SerializeField]
        private PrototypeSideCanvasSurfaceGraphic surfaceGraphic;

        [SerializeField]
        private PrototypeSideCanvasPuppetGraphic puppetGraphic;

        [SerializeField] private Text modeText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text markerText;
        [SerializeField] private Text draftSummaryText;

        [Header("Drawing")]
        [SerializeField, Range(0.004f, 0.08f)]
        private float normalizedBrushWidth = 0.022f;

        [SerializeField, Range(0.001f, 0.05f)]
        private float minimumPointDistance = 0.006f;

        [SerializeField, Range(0.02f, 0.20f)]
        private float markerSnapDistance = 0.10f;

        [SerializeField, Min(1)]
        private int maximumStrokeCount = 64;

        [SerializeField, Min(4)]
        private int maximumPointsPerStroke = 96;

        [SerializeField, Range(0, 5)]
        private int selectedDrawingColorIndex;

        [SerializeField, Range(0.02f, 0.16f)]
        private float markerEditSelectionDistance = 0.075f;

        [Header("3D Deployment Scale")]
        [SerializeField] private Vector3 deploymentScale = Vector3.one;

        [SerializeField, Range(0.35f, 1f)]
        private float minimumDeploymentScale = 0.55f;

        [SerializeField, Range(1f, 3f)]
        private float maximumDeploymentScale = 1.8f;

        [SerializeField, Range(0.01f, 0.25f)]
        private float deploymentScaleStep = 0.08f;

        [Header("Runtime Read Only")]
        [SerializeField] private bool isOpen;
        [SerializeField] private PrototypeSideCanvasMode currentMode;
        [SerializeField] private bool hasCompleteRig;
        [SerializeField] private bool rigValid;
        [SerializeField] private string validationMessage = "Taslak yok";
        [SerializeField] private int openedCount;
        [SerializeField] private int completedPreviewCount;
        [SerializeField] private int inputConflictCount;
        [SerializeField] private int configuredInputBlockerCount;
        [SerializeField] private int capturedInputBlockerCount;
        [SerializeField] private bool cursorAuthorityActive;
        [SerializeField] private int keyboardActionCount;
        [SerializeField] private int toolbarActionCount;
        [SerializeField] private int pointerEventCount;
        [SerializeField] private bool painterRoleActive;
        [SerializeField] private int roleAutoCloseCount;
        [SerializeField] private int roleRejectedActionCount;
        [SerializeField] private string resolvedRole = "Unknown";
        [SerializeField, Range(0f, 1f)] private float rigQuality;
        [SerializeField] private List<PrototypeSideCanvasStroke> strokes =
            new List<PrototypeSideCanvasStroke>();

        [SerializeField]
        private List<PrototypeRigMarkerPlacement> markers =
            new List<PrototypeRigMarkerPlacement>();

        private PrototypeSideCanvasStroke activeStroke;
        private sealed class InputBlockerState
        {
            public MonoBehaviour Behaviour;
            public bool WasEnabled;
        }

        private readonly List<InputBlockerState>
            capturedInputBlockers =
                new List<InputBlockerState>();

        private bool brushWasEnabled;
        private bool brushStateCaptured;
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;
        private float previewAttackStartedAt = -100f;
        private float nextScaleRepeatAt;
        private bool rigMarkerDragActive;
        private PrototypeRigMarkerKind activeRigMarkerKind;
        private const float PreviewAttackDuration = 0.52f;

        public bool IsOpen => isOpen;
        public PrototypeSideCanvasMode CurrentMode => currentMode;
        public IReadOnlyList<PrototypeSideCanvasStroke> Strokes => strokes;
        public IReadOnlyList<PrototypeRigMarkerPlacement> Markers => markers;
        public bool HasCompleteRig => hasCompleteRig;
        public bool RigValid => rigValid;
        public string ValidationMessage => validationMessage;
        public int OpenedCount => openedCount;
        public int CompletedPreviewCount => completedPreviewCount;
        public int InputConflictCount => inputConflictCount;
        public int ConfiguredInputBlockerCount =>
            configuredInputBlockerCount;
        public int CapturedInputBlockerCount =>
            capturedInputBlockerCount;
        public bool CursorAuthorityActive =>
            cursorAuthorityActive;
        public int KeyboardActionCount =>
            keyboardActionCount;
        public int ToolbarActionCount =>
            toolbarActionCount;
        public int PointerEventCount =>
            pointerEventCount;
        public bool PainterRoleActive => painterRoleActive;
        public int RoleAutoCloseCount => roleAutoCloseCount;
        public int RoleRejectedActionCount => roleRejectedActionCount;
        public string ResolvedRole => resolvedRole;
        public int StrokeCount => strokes.Count;
        public int MarkerCount => markers.Count;
        public bool KeyboardMouseOnly => true;
        public bool MatchTimePaused => false;
        public bool DeployEnabled =>
            isOpen &&
            currentMode == PrototypeSideCanvasMode.Preview &&
            rigValid;
        public Vector3 DeploymentScale => deploymentScale;
        public int DrawingColorCount => DrawingPalette.Length;
        public int SelectedDrawingColorIndex => selectedDrawingColorIndex;
        public Color SelectedDrawingColor =>
            DrawingPalette[Mathf.Clamp(
                selectedDrawingColorIndex,
                0,
                DrawingPalette.Length - 1)];
        public string SelectedDrawingColorName =>
            DrawingPaletteNames[Mathf.Clamp(
                selectedDrawingColorIndex,
                0,
                DrawingPaletteNames.Length - 1)];
        public float RigQuality => rigQuality;

        public float PreviewAttackNormalized
        {
            get
            {
                float elapsed =
                    Time.unscaledTime -
                    previewAttackStartedAt;

                if (elapsed < 0f ||
                    elapsed >= PreviewAttackDuration)
                {
                    return 0f;
                }

                float normalized =
                    Mathf.Clamp01(
                        elapsed /
                        PreviewAttackDuration);

                return Mathf.Sin(
                    normalized *
                    Mathf.PI);
            }
        }

        public void Configure(
            PrototypeUnifiedHudController configuredHudController,
            MonoBehaviour configuredBrushController,
            MonoBehaviour[] configuredWorldInputBlockers,
            CanvasGroup configuredRootGroup,
            RectTransform configuredDrawingArea,
            PrototypeSideCanvasSurfaceGraphic configuredSurfaceGraphic,
            PrototypeSideCanvasPuppetGraphic configuredPuppetGraphic,
            Text configuredModeText,
            Text configuredStatusText,
            Text configuredMarkerText,
            Text configuredDraftSummaryText)
        {
            unifiedHudController = configuredHudController;
            painterBrushController = configuredBrushController;
            worldInputBlockers =
                SanitizeInputBlockers(
                    configuredWorldInputBlockers);

            configuredInputBlockerCount =
                worldInputBlockers.Length;

            rootGroup = configuredRootGroup;
            drawingArea = configuredDrawingArea;
            surfaceGraphic = configuredSurfaceGraphic;
            puppetGraphic = configuredPuppetGraphic;
            modeText = configuredModeText;
            statusText = configuredStatusText;
            markerText = configuredMarkerText;
            draftSummaryText = configuredDraftSummaryText;

            if (surfaceGraphic != null)
            {
                surfaceGraphic.Configure(this);
            }

            if (puppetGraphic != null)
            {
                puppetGraphic.Configure(this);
            }

            ApplyVisibility(false);
            RefreshAll();
        }

        private void Awake()
        {
            if (surfaceGraphic != null)
            {
                surfaceGraphic.Configure(this);
            }

            if (puppetGraphic != null)
            {
                puppetGraphic.Configure(this);
            }

            ApplyVisibility(false);
            RefreshAll();
        }

        private void OnEnable()
        {
            ApplyVisibility(false);
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                CloseSideCanvas();
            }
        }

        private void OnDestroy()
        {
            if (isOpen)
            {
                RestoreWorldInput();
            }
        }

        private void Update()
        {
            if (PaintedAliveNetworkRoleBridge.GameplayInputSuppressed)
                return;

            RefreshRoleAuthority();

            if (!painterRoleActive)
            {
                if (isOpen)
                {
                    roleAutoCloseCount++;
                    validationMessage =
                        "Rol Figure oldu; Yan Tuval otomatik kapatıldı.";

                    CloseSideCanvas();
                }

                return;
            }

            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                if (isOpen)
                {
                    CloseSideCanvas();
                }
                else
                {
                    TryOpenSideCanvas();
                }

                return;
            }

            if (!isOpen)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                keyboardActionCount++;
                CloseSideCanvas();
                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame)
            {
                keyboardActionCount++;
                CycleMode();
            }

            if (keyboard.zKey.wasPressedThisFrame ||
                keyboard.backspaceKey.wasPressedThisFrame)
            {
                keyboardActionCount++;
                UndoCurrentStep();
            }

            if (keyboard.deleteKey.wasPressedThisFrame)
            {
                keyboardActionCount++;
                ClearDraft();
            }

            if (currentMode == PrototypeSideCanvasMode.Draw)
            {
                HandleDrawingColorInput(keyboard);
            }

            if (currentMode ==
                    PrototypeSideCanvasMode.Preview &&
                keyboard.spaceKey.wasPressedThisFrame)
            {
                keyboardActionCount++;
                previewAttackStartedAt =
                    Time.unscaledTime;

                validationMessage =
                    "Ağır saldırı ön izlemesi.";

                RefreshAll();
            }

            if (currentMode == PrototypeSideCanvasMode.Preview)
            {
                HandleDeploymentScaleInput(keyboard);
            }

            // Drawing and rig placement are driven only by UI pointer
            // events from PrototypeSideCanvasPointerInput. This prevents
            // toolbar clicks from becoming strokes.
            RefreshTextOnly();
        }


        private void LateUpdate()
        {
            if (!isOpen)
            {
                cursorAuthorityActive = false;
                return;
            }

            EnforceCanvasPointerAuthority(
                countConflict: true);

            EnsureInputBlockersRemainDisabled();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && isOpen)
            {
                EnforceCanvasPointerAuthority(
                    countConflict: false);
            }
        }

        public void TryOpenSideCanvas()
        {
            if (!IsPainterRole())
            {
                roleRejectedActionCount++;
                validationMessage =
                    "Yan Tuval yalnız Painter rolünde açılır.";

                RefreshAll();
                return;
            }

            previousCursorLockMode =
                Cursor.lockState;

            previousCursorVisible =
                Cursor.visible;

            // M45 uses a virtual cursor driven by relative mouse delta.
            // Keeping the hardware cursor locked prevents other camera/input
            // systems from recentering an absolute pointer position.
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            CaptureAndDisableWorldInput();

            isOpen = true;
            EnforceCanvasPointerAuthority(
                countConflict: false);
            openedCount++;

            ApplyVisibility(true);
            ValidateRig();
            RefreshAll();
        }

        public void CloseSideCanvas()
        {
            if (!isOpen)
            {
                return;
            }

            EndActiveStroke();
            isOpen = false;
            ApplyVisibility(false);
            RestoreWorldInput();

            Cursor.lockState =
                previousCursorLockMode;

            Cursor.visible =
                previousCursorVisible;
        }


        public void AdvanceMode()
        {
            if (!isOpen)
            {
                return;
            }

            toolbarActionCount++;
            CycleMode();
        }

        public void Undo()
        {
            if (!isOpen)
            {
                return;
            }

            toolbarActionCount++;
            UndoCurrentStep();
        }

        public void Clear()
        {
            if (!isOpen)
            {
                return;
            }

            toolbarActionCount++;
            ClearDraft();
        }

        public void TriggerPreviewAttack()
        {
            if (!isOpen ||
                currentMode !=
                    PrototypeSideCanvasMode.Preview)
            {
                validationMessage =
                    "Ağır saldırı testi yalnız Kukla Testi modunda çalışır.";

                RefreshAll();
                return;
            }

            previewAttackStartedAt =
                Time.unscaledTime;

            validationMessage =
                "Ağır saldırı ön izlemesi.";

            RefreshAll();
        }

        public void AdjustDeploymentScale(Vector3 axisDelta)
        {
            if (!isOpen ||
                currentMode != PrototypeSideCanvasMode.Preview)
            {
                return;
            }

            deploymentScale = ClampDeploymentScale(
                deploymentScale + axisDelta * deploymentScaleStep);

            validationMessage =
                $"3B boyut • X {deploymentScale.x:F2}  " +
                $"Y {deploymentScale.y:F2}  Z {deploymentScale.z:F2}";

            RefreshAll();
        }

        public void ResetDeploymentScale()
        {
            if (!isOpen ||
                currentMode != PrototypeSideCanvasMode.Preview)
            {
                return;
            }

            deploymentScale = Vector3.one;
            validationMessage = "3B boyut varsayılana döndü.";
            RefreshAll();
        }

        public Color GetDrawingPaletteColor(int index)
        {
            return DrawingPalette[Mathf.Clamp(
                index,
                0,
                DrawingPalette.Length - 1)];
        }

        public string GetDrawingPaletteName(int index)
        {
            return DrawingPaletteNames[Mathf.Clamp(
                index,
                0,
                DrawingPaletteNames.Length - 1)];
        }

        public void SelectDrawingColor(int index)
        {
            selectedDrawingColorIndex = Mathf.Clamp(
                index,
                0,
                DrawingPalette.Length - 1);

            validationMessage =
                $"Çizim rengi: {SelectedDrawingColorName}";

            RefreshTextOnly();
        }

        public Color ResolvePreviewInkColor()
        {
            Color weighted = Color.clear;
            float totalWeight = 0f;

            for (int index = 0; index < strokes.Count; index++)
            {
                PrototypeSideCanvasStroke stroke = strokes[index];

                if (stroke == null || stroke.Points.Count == 0)
                {
                    continue;
                }

                float weight = Mathf.Max(1f, stroke.Points.Count);
                weighted += stroke.Color * weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                return SelectedDrawingColor;
            }

            weighted /= totalWeight;
            weighted.a = 1f;
            return weighted;
        }

        public void CloseFromToolbar()
        {
            if (!isOpen)
            {
                return;
            }

            toolbarActionCount++;
            CloseSideCanvas();
        }

        public void LoadDemoDraft()
        {
            strokes.Clear();
            markers.Clear();

            AddDemoStroke(
                new[]
                {
                    new Vector2(0.50f, 0.24f),
                    new Vector2(0.47f, 0.36f),
                    new Vector2(0.48f, 0.52f),
                    new Vector2(0.50f, 0.68f),
                    new Vector2(0.50f, 0.80f)
                },
                0.055f);

            AddDemoStroke(
                new[]
                {
                    new Vector2(0.49f, 0.58f),
                    new Vector2(0.34f, 0.54f),
                    new Vector2(0.20f, 0.48f)
                },
                0.038f);

            AddDemoStroke(
                new[]
                {
                    new Vector2(0.50f, 0.58f),
                    new Vector2(0.66f, 0.54f),
                    new Vector2(0.80f, 0.48f)
                },
                0.038f);

            AddDemoStroke(
                new[]
                {
                    new Vector2(0.48f, 0.36f),
                    new Vector2(0.37f, 0.24f),
                    new Vector2(0.30f, 0.12f)
                },
                0.045f);

            AddDemoStroke(
                new[]
                {
                    new Vector2(0.48f, 0.36f),
                    new Vector2(0.61f, 0.24f),
                    new Vector2(0.70f, 0.12f)
                },
                0.045f);

            AddMarker(
                PrototypeRigMarkerKind.Core,
                new Vector2(0.49f, 0.46f));

            AddMarker(
                PrototypeRigMarkerKind.Head,
                new Vector2(0.50f, 0.79f));

            AddMarker(
                PrototypeRigMarkerKind.LeftHand,
                new Vector2(0.20f, 0.48f));

            AddMarker(
                PrototypeRigMarkerKind.RightHand,
                new Vector2(0.80f, 0.48f));

            AddMarker(
                PrototypeRigMarkerKind.LeftFoot,
                new Vector2(0.30f, 0.12f));

            AddMarker(
                PrototypeRigMarkerKind.RightFoot,
                new Vector2(0.70f, 0.12f));

            currentMode =
                PrototypeSideCanvasMode.Preview;

            ValidateRig();

            if (rigValid)
            {
                completedPreviewCount++;
            }

            if (!isOpen)
            {
                TryOpenSideCanvas();
            }

            RefreshAll();
        }

        public void HandleCanvasPointerDown(
            Vector2 normalized,
            PointerEventData.InputButton button)
        {
            if (!isOpen)
            {
                return;
            }

            pointerEventCount++;

            if (button ==
                PointerEventData.InputButton.Right)
            {
                if (currentMode ==
                    PrototypeSideCanvasMode.Rig)
                {
                    RemoveLastMarker();
                }

                return;
            }

            if (button !=
                PointerEventData.InputButton.Left)
            {
                return;
            }

            normalized = ClampNormalized(normalized);

            if (currentMode ==
                PrototypeSideCanvasMode.Draw)
            {
                BeginStroke(normalized);
                return;
            }

            if (currentMode ==
                PrototypeSideCanvasMode.Rig)
            {
                PlaceRigMarker(normalized);
            }
        }

        public void HandleCanvasPointerDrag(
            Vector2 normalized,
            PointerEventData.InputButton button)
        {
            if (!isOpen ||
                button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (currentMode == PrototypeSideCanvasMode.Rig &&
                rigMarkerDragActive)
            {
                if (TrySnapToInk(
                        ClampNormalized(normalized),
                        out Vector2 rigSnapped))
                {
                    SetMarker(activeRigMarkerKind, rigSnapped);
                    ValidateRig();
                    RefreshAll();
                }

                return;
            }

            if (currentMode != PrototypeSideCanvasMode.Draw ||
                activeStroke == null)
            {
                return;
            }

            ContinueStroke(
                ClampNormalized(normalized));
        }

        public void HandleCanvasPointerUp(
            Vector2 normalized,
            PointerEventData.InputButton button)
        {
            if (!isOpen ||
                button !=
                    PointerEventData.InputButton.Left)
            {
                return;
            }

            if (currentMode ==
                PrototypeSideCanvasMode.Draw)
            {
                if (activeStroke != null)
                {
                    ContinueStroke(
                        ClampNormalized(normalized));
                }

                EndActiveStroke();
                ValidateRig();
                RefreshAll();
            }
            else if (currentMode == PrototypeSideCanvasMode.Rig &&
                     rigMarkerDragActive)
            {
                if (TrySnapToInk(
                        ClampNormalized(normalized),
                        out Vector2 rigSnapped))
                {
                    SetMarker(activeRigMarkerKind, rigSnapped);
                }

                rigMarkerDragActive = false;
                NormalizeRigSides();
                ValidateRig();
                RefreshAll();
            }
        }

        public void ResetPrototypeDraftForSetup()
        {
            if (Application.isPlaying)
            {
                return;
            }

            activeStroke = null;
            strokes.Clear();
            markers.Clear();
            currentMode =
                PrototypeSideCanvasMode.Draw;

            deploymentScale = Vector3.one;
            selectedDrawingColorIndex = 0;

            hasCompleteRig = false;
            rigValid = false;
            validationMessage =
                "Taslak yok • Sol tık basılı tutarak çiz.";

            RefreshAll();
        }

        private void BeginStroke(Vector2 normalized)
        {
            if (strokes.Count >=
                maximumStrokeCount)
            {
                validationMessage =
                    $"Stroke sınırı: {maximumStrokeCount}";

                RefreshAll();
                return;
            }

            activeStroke =
                new PrototypeSideCanvasStroke(
                    SelectedDrawingColor,
                    normalizedBrushWidth);

            activeStroke.Points.Add(normalized);
            strokes.Add(activeStroke);

            validationMessage =
                "Çizim aktif • Fareyi sürükle.";

            RefreshGraphics();
            RefreshTextOnly();
        }

        private void ContinueStroke(Vector2 normalized)
        {
            if (activeStroke == null ||
                activeStroke.Points.Count >=
                    maximumPointsPerStroke)
            {
                return;
            }

            Vector2 previous =
                activeStroke.Points[
                    activeStroke.Points.Count - 1];

            if (Vector2.Distance(
                    previous,
                    normalized) <
                minimumPointDistance)
            {
                return;
            }

            activeStroke.Points.Add(normalized);
            RefreshGraphics();
        }

        private void PlaceRigMarker(Vector2 normalized)
        {
            PrototypeRigMarkerKind kind;

            if (hasCompleteRig)
            {
                if (!TryFindNearestMarker(
                        normalized,
                        markerEditSelectionDistance,
                        out kind))
                {
                    validationMessage =
                        "Düzenlemek için marker'a tıklayıp sürükle.";
                    RefreshAll();
                    return;
                }
            }
            else
            {
                kind = ResolveNextMarkerKind();
            }

            if (!TrySnapToInk(
                    normalized,
                    out Vector2 snapped))
            {
                validationMessage =
                    $"{TranslateMarker(kind)} geçerli Mürekkep çizgisine yakın olmalı.";

                RefreshAll();
                return;
            }

            SetMarker(kind, snapped);
            activeRigMarkerKind = kind;
            rigMarkerDragActive = true;
            ValidateRig();
            RefreshAll();
        }

        private static Vector2 ClampNormalized(
            Vector2 value)
        {
            return new Vector2(
                Mathf.Clamp01(value.x),
                Mathf.Clamp01(value.y));
        }

        private void CycleMode()
        {
            EndActiveStroke();

            if (currentMode ==
                PrototypeSideCanvasMode.Draw)
            {
                if (!HasDrawableStroke())
                {
                    validationMessage =
                        "Önce en az bir geçerli stroke çiz.";

                    RefreshAll();
                    return;
                }

                currentMode =
                    PrototypeSideCanvasMode.Rig;

                validationMessage =
                    "Altı marker'ı sırayla yerleştir.";
            }
            else if (currentMode ==
                     PrototypeSideCanvasMode.Rig)
            {
                ValidateRig();

                if (!rigValid)
                {
                    RefreshAll();
                    return;
                }

                currentMode =
                    PrototypeSideCanvasMode.Preview;

                completedPreviewCount++;
                validationMessage =
                    "Kukla testi hazır. SPACE: ağır saldırı ön izlemesi.";
            }
            else
            {
                currentMode =
                    PrototypeSideCanvasMode.Draw;

                validationMessage =
                    "Taslak korunuyor. Çizime devam edebilirsin.";
            }

            RefreshAll();
        }

        private void UndoCurrentStep()
        {
            EndActiveStroke();

            if (currentMode ==
                PrototypeSideCanvasMode.Rig)
            {
                RemoveLastMarker();
                return;
            }

            if (currentMode ==
                PrototypeSideCanvasMode.Preview)
            {
                currentMode =
                    PrototypeSideCanvasMode.Rig;

                validationMessage =
                    "Rig düzenleme moduna dönüldü.";

                RefreshAll();
                return;
            }

            if (strokes.Count == 0)
            {
                return;
            }

            strokes.RemoveAt(
                strokes.Count - 1);

            ValidateRig();
            RefreshAll();
        }

        private void ClearDraft()
        {
            EndActiveStroke();
            strokes.Clear();
            markers.Clear();
            currentMode =
                PrototypeSideCanvasMode.Draw;

            validationMessage =
                "Taslak temizlendi.";

            ValidateRig();
            RefreshAll();
        }

        private void RemoveLastMarker()
        {
            if (markers.Count == 0)
            {
                return;
            }

            markers.RemoveAt(
                markers.Count - 1);

            currentMode =
                PrototypeSideCanvasMode.Rig;

            ValidateRig();
            RefreshAll();
        }

        private void SetMarker(
            PrototypeRigMarkerKind kind,
            Vector2 position)
        {
            for (int index = 0;
                 index < markers.Count;
                 index++)
            {
                if (markers[index].Kind == kind)
                {
                    markers[index].NormalizedPosition =
                        position;

                    return;
                }
            }

            markers.Add(
                new PrototypeRigMarkerPlacement(
                    kind,
                    position));
        }

        private void AddMarker(
            PrototypeRigMarkerKind kind,
            Vector2 position)
        {
            markers.Add(
                new PrototypeRigMarkerPlacement(
                    kind,
                    position));
        }

        private PrototypeRigMarkerKind ResolveNextMarkerKind()
        {
            for (int orderIndex = 0;
                 orderIndex < MarkerOrder.Length;
                 orderIndex++)
            {
                PrototypeRigMarkerKind candidate =
                    MarkerOrder[orderIndex];

                bool found = false;

                for (int markerIndex = 0;
                     markerIndex < markers.Count;
                     markerIndex++)
                {
                    if (markers[markerIndex].Kind ==
                        candidate)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return candidate;
                }
            }

            return MarkerOrder[
                MarkerOrder.Length - 1];
        }

        private void ValidateRig()
        {
            hasCompleteRig =
                CountUniqueMarkers() >=
                MarkerOrder.Length;

            rigQuality = 0f;

            if (!HasDrawableStroke())
            {
                rigValid = false;
                validationMessage =
                    "En az bir geçerli stroke gerekli.";
                return;
            }

            if (!hasCompleteRig)
            {
                rigValid = false;

                PrototypeRigMarkerKind next =
                    ResolveNextMarkerKind();

                validationMessage =
                    $"Sıradaki marker: {TranslateMarker(next)}";
                return;
            }

            if (!TryGetMarker(
                    PrototypeRigMarkerKind.Core,
                    out Vector2 core) ||
                !TryGetMarker(
                    PrototypeRigMarkerKind.Head,
                    out Vector2 head))
            {
                rigValid = false;
                validationMessage =
                    "Çekirdek ve Baş marker'ları eksik.";
                return;
            }

            // The previous fixed 0.08 threshold rejected compact but valid
            // drawings even when both markers were visibly on separate ink.
            // Scale the safety gap from the actual brush size instead.
            float minimumHeadCoreDistance =
                Mathf.Clamp(
                    normalizedBrushWidth * 1.1f,
                    0.018f,
                    0.04f);

            if (Vector2.Distance(
                    core,
                    head) < minimumHeadCoreDistance)
            {
                rigValid = false;
                validationMessage =
                    "Baş ve Çekirdek birbirinden ayrılmalı.";
                return;
            }

            for (int index = 0;
                 index < markers.Count;
                 index++)
            {
                PrototypeRigMarkerPlacement marker =
                    markers[index];

                if (!IsNearInk(
                        marker.NormalizedPosition,
                        markerSnapDistance *
                        1.15f))
                {
                    rigValid = false;
                    validationMessage =
                        $"{TranslateMarker(marker.Kind)} Mürekkep dışında kaldı.";
                    return;
                }
            }

            rigQuality = CalculateRigQuality(core, head);

            rigValid = true;

            switch (currentMode)
            {
                case PrototypeSideCanvasMode.Preview:
                    validationMessage =
                        $"Kukla hazır • Boyut X{deploymentScale.x:F2} " +
                        $"Y{deploymentScale.y:F2} Z{deploymentScale.z:F2} • V: aktar";
                    break;

                case PrototypeSideCanvasMode.Rig:
                    validationMessage =
                        "Rig geçerli • TAB ile Kukla Testine geç.";
                    break;

                default:
                    validationMessage =
                        "Çizim ve rig korunuyor • TAB ile rig düzenle.";
                    break;
            }
        }

        private bool TryGetMarker(
            PrototypeRigMarkerKind kind,
            out Vector2 position)
        {
            for (int index = 0;
                 index < markers.Count;
                 index++)
            {
                if (markers[index].Kind ==
                    kind)
                {
                    position =
                        markers[index]
                            .NormalizedPosition;

                    return true;
                }
            }

            position = default;
            return false;
        }

        private bool TrySnapToInk(
            Vector2 requested,
            out Vector2 snapped)
        {
            float bestDistance =
                float.PositiveInfinity;

            Vector2 bestPoint =
                requested;

            for (int strokeIndex = 0;
                 strokeIndex < strokes.Count;
                 strokeIndex++)
            {
                PrototypeSideCanvasStroke stroke =
                    strokes[strokeIndex];

                if (stroke == null)
                {
                    continue;
                }

                for (int pointIndex = 0;
                     pointIndex < stroke.Points.Count;
                     pointIndex++)
                {
                    Vector2 point =
                        stroke.Points[pointIndex];

                    float distance =
                        Vector2.Distance(
                            requested,
                            point);

                    if (distance <
                        bestDistance)
                    {
                        bestDistance =
                            distance;

                        bestPoint =
                            point;
                    }

                    if (pointIndex == 0)
                    {
                        continue;
                    }

                    Vector2 segmentPoint = ClosestPointOnSegment(
                        requested,
                        stroke.Points[pointIndex - 1],
                        point);

                    float segmentDistance = Vector2.Distance(
                        requested,
                        segmentPoint);

                    if (segmentDistance < bestDistance)
                    {
                        bestDistance = segmentDistance;
                        bestPoint = segmentPoint;
                    }
                }
            }

            snapped = bestPoint;
            return bestDistance <=
                markerSnapDistance;
        }

        private bool IsNearInk(
            Vector2 position,
            float maximumDistance)
        {
            return TryGetClosestInkPoint(
                       position,
                       out _,
                       out float distance) &&
                   distance <= maximumDistance;
        }

        private bool TryGetClosestInkPoint(
            Vector2 requested,
            out Vector2 closest,
            out float distance)
        {
            closest = requested;
            distance = float.PositiveInfinity;
            bool found = false;

            for (int strokeIndex = 0; strokeIndex < strokes.Count; strokeIndex++)
            {
                PrototypeSideCanvasStroke stroke = strokes[strokeIndex];
                if (stroke == null || stroke.Points.Count == 0)
                {
                    continue;
                }

                for (int pointIndex = 0; pointIndex < stroke.Points.Count; pointIndex++)
                {
                    Vector2 candidate = stroke.Points[pointIndex];
                    float candidateDistance = Vector2.Distance(requested, candidate);

                    if (candidateDistance < distance)
                    {
                        distance = candidateDistance;
                        closest = candidate;
                        found = true;
                    }

                    if (pointIndex == 0)
                    {
                        continue;
                    }

                    candidate = ClosestPointOnSegment(
                        requested,
                        stroke.Points[pointIndex - 1],
                        stroke.Points[pointIndex]);
                    candidateDistance = Vector2.Distance(requested, candidate);

                    if (candidateDistance < distance)
                    {
                        distance = candidateDistance;
                        closest = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        private static Vector2 ClosestPointOnSegment(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared <= 0.0000001f)
            {
                return start;
            }

            float t = Mathf.Clamp01(
                Vector2.Dot(point - start, segment) / lengthSquared);

            return start + segment * t;
        }

        private int CountUniqueMarkers()
        {
            int count = 0;

            for (int orderIndex = 0; orderIndex < MarkerOrder.Length; orderIndex++)
            {
                if (TryGetMarker(MarkerOrder[orderIndex], out _))
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryFindNearestMarker(
            Vector2 normalized,
            float maximumDistance,
            out PrototypeRigMarkerKind kind)
        {
            kind = PrototypeRigMarkerKind.Core;
            float bestDistance = float.PositiveInfinity;
            bool found = false;

            for (int index = 0; index < markers.Count; index++)
            {
                PrototypeRigMarkerPlacement marker = markers[index];
                if (marker == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(
                    normalized,
                    marker.NormalizedPosition);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    kind = marker.Kind;
                    found = true;
                }
            }

            return found && bestDistance <= maximumDistance;
        }

        private void NormalizeRigSides()
        {
            if (!TryGetMarker(PrototypeRigMarkerKind.Core, out Vector2 core) ||
                !TryGetMarker(PrototypeRigMarkerKind.Head, out Vector2 head))
            {
                return;
            }

            Vector2 bodyAxis = head - core;
            if (bodyAxis.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            bodyAxis.Normalize();
            Vector2 rightAxis = new Vector2(bodyAxis.y, -bodyAxis.x);

            NormalizeMarkerPair(
                PrototypeRigMarkerKind.LeftHand,
                PrototypeRigMarkerKind.RightHand,
                core,
                rightAxis);

            NormalizeMarkerPair(
                PrototypeRigMarkerKind.LeftFoot,
                PrototypeRigMarkerKind.RightFoot,
                core,
                rightAxis);
        }

        private void NormalizeMarkerPair(
            PrototypeRigMarkerKind leftKind,
            PrototypeRigMarkerKind rightKind,
            Vector2 core,
            Vector2 rightAxis)
        {
            if (!TryGetMarker(leftKind, out Vector2 left) ||
                !TryGetMarker(rightKind, out Vector2 right))
            {
                return;
            }

            float leftSide = Vector2.Dot(left - core, rightAxis);
            float rightSide = Vector2.Dot(right - core, rightAxis);

            if (leftSide <= rightSide)
            {
                return;
            }

            SetMarker(leftKind, right);
            SetMarker(rightKind, left);
        }

        private float CalculateRigQuality(Vector2 core, Vector2 head)
        {
            float headSeparation = Mathf.InverseLerp(
                normalizedBrushWidth,
                0.24f,
                Vector2.Distance(core, head));

            float limbScore = 0f;
            int limbCount = 0;

            PrototypeRigMarkerKind[] limbs =
            {
                PrototypeRigMarkerKind.LeftHand,
                PrototypeRigMarkerKind.RightHand,
                PrototypeRigMarkerKind.LeftFoot,
                PrototypeRigMarkerKind.RightFoot
            };

            for (int index = 0; index < limbs.Length; index++)
            {
                if (!TryGetMarker(limbs[index], out Vector2 limb))
                {
                    continue;
                }

                limbScore += Mathf.InverseLerp(
                    normalizedBrushWidth * 0.5f,
                    0.22f,
                    Vector2.Distance(core, limb));
                limbCount++;
            }

            if (limbCount > 0)
            {
                limbScore /= limbCount;
            }

            return Mathf.Clamp01(headSeparation * 0.45f + limbScore * 0.55f);
        }

        private bool HasDrawableStroke()
        {
            for (int index = 0;
                 index < strokes.Count;
                 index++)
            {
                PrototypeSideCanvasStroke stroke =
                    strokes[index];

                if (stroke != null &&
                    stroke.Points.Count >= 2)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshRoleAuthority()
        {
            if (unifiedHudController != null)
            {
                try
                {
                    PrototypeUnifiedHudSnapshot snapshot =
                        unifiedHudController.BuildSnapshot();

                    resolvedRole =
                        string.IsNullOrWhiteSpace(snapshot.Role)
                            ? unifiedHudController.ResolvedRole
                            : snapshot.Role;

                    painterRoleActive = snapshot.IsPainter;
                    return;
                }
                catch (Exception)
                {
                    // Fall through to the shared resolver.
                }
            }

            painterRoleActive =
                PrototypeRoleAuthorityResolver.IsPainter(
                    out resolvedRole);
        }

        private bool IsPainterRole()
        {
            RefreshRoleAuthority();
            return painterRoleActive;
        }

        private void CaptureAndDisableWorldInput()
        {
            CaptureAndDisableWorldBrush();

            capturedInputBlockers.Clear();

            if (worldInputBlockers == null ||
                worldInputBlockers.Length == 0)
            {
                worldInputBlockers =
                    DiscoverFallbackInputBlockers();

                configuredInputBlockerCount =
                    worldInputBlockers.Length;
            }

            for (int index = 0;
                 index < worldInputBlockers.Length;
                 index++)
            {
                MonoBehaviour blocker =
                    worldInputBlockers[index];

                if (!IsValidInputBlocker(blocker))
                {
                    continue;
                }

                capturedInputBlockers.Add(
                    new InputBlockerState
                    {
                        Behaviour = blocker,
                        WasEnabled = blocker.enabled
                    });

                blocker.enabled = false;
            }

            capturedInputBlockerCount =
                capturedInputBlockers.Count;
        }

        private void CaptureAndDisableWorldBrush()
        {
            if (painterBrushController == null)
            {
                brushStateCaptured = false;
                return;
            }

            brushWasEnabled =
                painterBrushController.enabled;

            brushStateCaptured = true;
            painterBrushController.enabled = false;
        }

        private void EnsureInputBlockersRemainDisabled()
        {
            for (int index = 0;
                 index < capturedInputBlockers.Count;
                 index++)
            {
                InputBlockerState state =
                    capturedInputBlockers[index];

                if (state == null ||
                    state.Behaviour == null)
                {
                    continue;
                }

                if (state.Behaviour.enabled)
                {
                    state.Behaviour.enabled = false;
                    inputConflictCount++;
                }
            }

            if (brushStateCaptured &&
                painterBrushController != null &&
                painterBrushController.enabled)
            {
                painterBrushController.enabled = false;
                inputConflictCount++;
            }
        }

        private void EnforceCanvasPointerAuthority(
            bool countConflict)
        {
            bool conflict =
                Cursor.lockState !=
                    CursorLockMode.Locked ||
                Cursor.visible;

            if (conflict && countConflict)
            {
                inputConflictCount++;
            }

            if (forceCursorWhileOpen)
            {
                Cursor.lockState =
                    CursorLockMode.Locked;

                Cursor.visible = false;
            }

            cursorAuthorityActive =
                Cursor.lockState ==
                    CursorLockMode.Locked &&
                !Cursor.visible;
        }

        private MonoBehaviour[] DiscoverFallbackInputBlockers()
        {
            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            var result =
                new List<MonoBehaviour>();

            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    behaviours[index];

                if (!IsValidInputBlocker(candidate) ||
                    !LooksLikePainterCameraInput(candidate))
                {
                    continue;
                }

                result.Add(candidate);
            }

            return SanitizeInputBlockers(
                result.ToArray());
        }

        private bool IsValidInputBlocker(
            MonoBehaviour candidate)
        {
            if (candidate == null ||
                candidate == this ||
                candidate == painterBrushController ||
                !candidate.gameObject.scene.IsValid())
            {
                return false;
            }

            string candidateNamespace =
                candidate.GetType().Namespace ??
                string.Empty;

            if (candidateNamespace.StartsWith(
                    "PaintedAlive.Painters.SideCanvas",
                    StringComparison.Ordinal) ||
                candidateNamespace.StartsWith(
                    "PaintedAlive.UI",
                    StringComparison.Ordinal) ||
                candidateNamespace.StartsWith(
                    "Unity.Cinemachine",
                    StringComparison.Ordinal) ||
                candidateNamespace.StartsWith(
                    "Cinemachine",
                    StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        private static bool LooksLikePainterCameraInput(
            MonoBehaviour candidate)
        {
            Type type = candidate.GetType();
            string typeName = type.Name;
            string objectName =
                candidate.gameObject.name;

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

        private static MonoBehaviour[] SanitizeInputBlockers(
            MonoBehaviour[] source)
        {
            if (source == null ||
                source.Length == 0)
            {
                return Array.Empty<MonoBehaviour>();
            }

            var unique =
                new List<MonoBehaviour>();

            var ids =
                new HashSet<int>();

            for (int index = 0;
                 index < source.Length;
                 index++)
            {
                MonoBehaviour candidate =
                    source[index];

                if (candidate == null)
                {
                    continue;
                }

                int id = candidate.GetInstanceID();
                if (ids.Add(id))
                {
                    unique.Add(candidate);
                }
            }

            return unique.ToArray();
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

        private void RestoreWorldInput()
        {
            for (int index =
                     capturedInputBlockers.Count - 1;
                 index >= 0;
                 index--)
            {
                InputBlockerState state =
                    capturedInputBlockers[index];

                if (state == null ||
                    state.Behaviour == null)
                {
                    continue;
                }

                state.Behaviour.enabled =
                    state.WasEnabled;
            }

            capturedInputBlockers.Clear();
            capturedInputBlockerCount = 0;

            if (brushStateCaptured &&
                painterBrushController != null)
            {
                painterBrushController.enabled =
                    brushWasEnabled;
            }

            brushStateCaptured = false;
            cursorAuthorityActive = false;
        }

        private void EndActiveStroke()
        {
            activeStroke = null;
        }

        private void ApplyVisibility(bool visible)
        {
            if (rootGroup == null)
            {
                return;
            }

            rootGroup.alpha =
                visible ? 1f : 0f;

            // All M45 pointer interaction is resolved manually from the
            // virtual cursor. Physical/EventSystem pointer raycasts stay off
            // so a recentered hardware mouse cannot create phantom strokes.
            rootGroup.interactable =
                visible;

            rootGroup.blocksRaycasts =
                false;
        }

        private void RefreshAll()
        {
            RefreshGraphics();
            RefreshTextOnly();
        }

        private void RefreshGraphics()
        {
            if (surfaceGraphic != null)
            {
                surfaceGraphic.Refresh();
            }

            if (puppetGraphic != null)
            {
                puppetGraphic.Refresh();
            }
        }

        private void RefreshTextOnly()
        {
            ValidateRig();

            if (modeText != null)
            {
                modeText.text =
                    ResolveModeTitle();
            }

            if (statusText != null)
            {
                statusText.text =
                    validationMessage;
            }

            if (markerText != null)
            {
                markerText.text =
                    BuildMarkerStatus();
            }

            if (draftSummaryText != null)
            {
                draftSummaryText.text =
                    $"STROKE {strokes.Count}/{maximumStrokeCount}\n" +
                    $"MARKER {markers.Count}/{MarkerOrder.Length}\n" +
                    $"RİG {(rigValid ? $"GEÇERLİ %{Mathf.RoundToInt(rigQuality * 100f)}" : "EKSİK")}\n" +
                    $"RENK {SelectedDrawingColorName.ToUpperInvariant()} [{selectedDrawingColorIndex + 1}]\n" +
                    $"3B X{deploymentScale.x:F2} Y{deploymentScale.y:F2} Z{deploymentScale.z:F2}\n" +
                    $"AKTARIM {(DeployEnabled ? "HAZIR" : "KİLİTLİ")}";
            }
        }

        private void HandleDeploymentScaleInput(Keyboard keyboard)
        {
            if (keyboard.homeKey.wasPressedThisFrame)
            {
                keyboardActionCount++;
                ResetDeploymentScale();
                nextScaleRepeatAt = Time.unscaledTime + 0.18f;
                return;
            }

            bool anyScaleKey =
                keyboard.leftArrowKey.isPressed ||
                keyboard.rightArrowKey.isPressed ||
                keyboard.downArrowKey.isPressed ||
                keyboard.upArrowKey.isPressed ||
                keyboard.pageDownKey.isPressed ||
                keyboard.pageUpKey.isPressed;

            if (!anyScaleKey)
            {
                nextScaleRepeatAt = 0f;
                return;
            }

            if (Time.unscaledTime < nextScaleRepeatAt)
            {
                return;
            }

            Vector3 delta = Vector3.zero;

            if (keyboard.leftArrowKey.isPressed)
                delta.x -= 1f;
            if (keyboard.rightArrowKey.isPressed)
                delta.x += 1f;
            if (keyboard.downArrowKey.isPressed)
                delta.y -= 1f;
            if (keyboard.upArrowKey.isPressed)
                delta.y += 1f;
            if (keyboard.pageDownKey.isPressed)
                delta.z -= 1f;
            if (keyboard.pageUpKey.isPressed)
                delta.z += 1f;

            if (delta != Vector3.zero)
            {
                keyboardActionCount++;
                AdjustDeploymentScale(delta);
                nextScaleRepeatAt = Time.unscaledTime + 0.075f;
            }
        }

        private void HandleDrawingColorInput(Keyboard keyboard)
        {
            int requested = -1;

            if (keyboard.digit1Key.wasPressedThisFrame) requested = 0;
            else if (keyboard.digit2Key.wasPressedThisFrame) requested = 1;
            else if (keyboard.digit3Key.wasPressedThisFrame) requested = 2;
            else if (keyboard.digit4Key.wasPressedThisFrame) requested = 3;
            else if (keyboard.digit5Key.wasPressedThisFrame) requested = 4;
            else if (keyboard.digit6Key.wasPressedThisFrame) requested = 5;

            if (requested < 0)
            {
                return;
            }

            keyboardActionCount++;
            SelectDrawingColor(requested);
        }

        private Vector3 ClampDeploymentScale(Vector3 value)
        {
            float minimum = Mathf.Max(0.1f, minimumDeploymentScale);
            float maximum = Mathf.Max(minimum, maximumDeploymentScale);

            return new Vector3(
                Mathf.Clamp(value.x, minimum, maximum),
                Mathf.Clamp(value.y, minimum, maximum),
                Mathf.Clamp(value.z, minimum, maximum));
        }

        private string ResolveModeTitle()
        {
            switch (currentMode)
            {
                case PrototypeSideCanvasMode.Rig:
                    return "02 • RİGLE";

                case PrototypeSideCanvasMode.Preview:
                    return "03 • KUKLA TESTİ";

                default:
                    return "01 • ÇİZ";
            }
        }

        private string BuildMarkerStatus()
        {
            var lines =
                new List<string>
                {
                    "RIG MARKER SIRASI"
                };

            for (int index = 0;
                 index < MarkerOrder.Length;
                 index++)
            {
                PrototypeRigMarkerKind kind =
                    MarkerOrder[index];

                bool placed = false;

                for (int markerIndex = 0;
                     markerIndex < markers.Count;
                     markerIndex++)
                {
                    if (markers[markerIndex].Kind ==
                        kind)
                    {
                        placed = true;
                        break;
                    }
                }

                lines.Add(
                    $"{(placed ? "●" : "○")} " +
                    TranslateMarker(kind));
            }

            return string.Join(
                "\n",
                lines);
        }

        private void AddDemoStroke(
            Vector2[] points,
            float width)
        {
            var stroke =
                new PrototypeSideCanvasStroke(
                    new Color(
                        0.055f,
                        0.050f,
                        0.045f,
                        1f),
                    width);

            stroke.Points.AddRange(points);
            strokes.Add(stroke);
        }

        private static string TranslateMarker(
            PrototypeRigMarkerKind kind)
        {
            switch (kind)
            {
                case PrototypeRigMarkerKind.Core:
                    return "ÇEKİRDEK";

                case PrototypeRigMarkerKind.Head:
                    return "BAŞ / ALGI";

                case PrototypeRigMarkerKind.LeftHand:
                    return "SOL SALDIRI";

                case PrototypeRigMarkerKind.RightHand:
                    return "SAĞ SALDIRI";

                case PrototypeRigMarkerKind.LeftFoot:
                    return "SOL TEMAS";

                default:
                    return "SAĞ TEMAS";
            }
        }
    }
}
