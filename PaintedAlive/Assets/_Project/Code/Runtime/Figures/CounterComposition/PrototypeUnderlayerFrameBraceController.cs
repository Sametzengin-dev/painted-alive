using System;
using System.Reflection;
using PaintedAlive.Core.RoleAuthority;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaintedAlive.Figures.CounterComposition
{
    [DefaultExecutionOrder(275)]
    [DisallowMultipleComponent]
    public sealed class PrototypeUnderlayerFrameBraceController :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private PrototypeUnderlayerDiveController underlayerDive;

        [SerializeField]
        private Transform figureRoot;

        [SerializeField]
        private MonoBehaviour figureToolSource;

        [SerializeField]
        private MonoBehaviour sharedUseToolInputSource;

        [SerializeField]
        private MonoBehaviour clarityState;

        [Header("Brace")]
        [SerializeField, Min(0.5f)]
        private float braceUseRadius = 2.35f;

        [SerializeField, Min(0.5f)]
        private float braceSeconds = 5.25f;

        [Header("HUD")]
        [SerializeField]
        private CanvasGroup statusGroup;

        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text stateText;

        [SerializeField]
        private Text controlsText;

        [Header("Runtime Read Only")]
        [SerializeField]
        private bool figureRoleActive;

        [SerializeField]
        private bool frameGunSelected;

        [SerializeField]
        private bool useToolInputResolved;

        [SerializeField]
        private bool primaryToolAllowed = true;

        [SerializeField]
        private bool seamInRange;

        [SerializeField]
        private bool usePressed;

        [SerializeField]
        private int braceAttemptCount;

        [SerializeField]
        private int braceSuccessCount;

        [SerializeField]
        private int rejectedBraceCount;

        [SerializeField]
        private float nearestSeamDistance = -1f;

        [SerializeField]
        private string nearestSeam = "None";

        [SerializeField]
        private string currentTool = "Unknown";

        [SerializeField]
        private string resolvedRole = "Unknown";

        [SerializeField]
        private string useToolInputContract = "Unresolved";

        [SerializeField]
        private string lastAction =
            "Çerçeve Ankrajı için dikiş bekleniyor.";

        private InputActionReference useToolAction;
        private PrototypeUnderlayerSeam nearestSeamReference;

        private PropertyInfo currentToolProperty;
        private FieldInfo currentToolField;

        private PropertyInfo canUsePrimaryToolProperty;
        private FieldInfo canUsePrimaryToolField;

        public bool FigureRoleActive => figureRoleActive;
        public bool FrameGunSelected => frameGunSelected;
        public bool UseToolInputResolved => useToolInputResolved;
        public bool PrimaryToolAllowed => primaryToolAllowed;
        public bool SeamInRange => seamInRange;
        public bool UsePressed => usePressed;
        public int BraceAttemptCount => braceAttemptCount;
        public int BraceSuccessCount => braceSuccessCount;
        public int RejectedBraceCount => rejectedBraceCount;
        public float NearestSeamDistance => nearestSeamDistance;
        public string NearestSeam => nearestSeam;
        public string CurrentTool => currentTool;
        public string ResolvedRole => resolvedRole;
        public string UseToolInputContract => useToolInputContract;
        public string LastAction => lastAction;

        public bool UsesExistingFigureUseToolActionReadOnly => true;
        public bool AddsNewGameplayInputAction => false;
        public bool FrameGunSeamBraceEnabled => true;
        public bool BraceTemporarilyOverridesOilSeal => true;
        public bool BraceDeletesOilSealTimer => false;
        public bool OilPaintCannotSealActiveBrace => true;
        public bool FullStainCanUseBracedSealedSeam => true;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            PrototypeUnderlayerDiveController configuredDive,
            Transform configuredFigureRoot,
            MonoBehaviour configuredFigureToolSource,
            MonoBehaviour configuredSharedUseToolInputSource,
            MonoBehaviour configuredClarityState,
            CanvasGroup configuredStatusGroup,
            Text configuredTitleText,
            Text configuredStateText,
            Text configuredControlsText)
        {
            underlayerDive = configuredDive;
            figureRoot = configuredFigureRoot;
            figureToolSource = configuredFigureToolSource;
            sharedUseToolInputSource =
                configuredSharedUseToolInputSource;
            clarityState = configuredClarityState;

            statusGroup = configuredStatusGroup;
            titleText = configuredTitleText;
            stateText = configuredStateText;
            controlsText = configuredControlsText;

            ResolveToolContract();
            ResolveInputContract();
            ResolveClarityContract();
            RefreshHud();
        }

        private void Awake()
        {
            ResolveToolContract();
            ResolveInputContract();
            ResolveClarityContract();
        }

        private void OnEnable()
        {
            ResolveToolContract();
            ResolveInputContract();
            ResolveClarityContract();
        }

        private void Update()
        {
            figureRoleActive =
                PrototypeRoleAuthorityResolver.IsFigure(
                    out resolvedRole);

            RefreshCurrentTool();
            RefreshPrimaryToolGate();
            RefreshNearestSeam();
            RefreshUseToolAction();
            ProcessBraceInput();
            RefreshHud();
        }

        public bool ForceBraceNearestForPrototype()
        {
            RefreshNearestSeam();

            if (nearestSeamReference == null)
            {
                Reject(
                    "Force brace: kullanılabilir dikiş yok.");

                return false;
            }

            return ApplyBrace(
                nearestSeamReference,
                "Editor force");
        }

        private void ResolveToolContract()
        {
            currentToolProperty = null;
            currentToolField = null;

            if (figureToolSource == null)
            {
                return;
            }

            Type type =
                figureToolSource.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            foreach (string name in new[]
            {
                "CurrentTool",
                "ActiveTool",
                "SelectedTool",
                "currentTool",
                "activeTool",
                "selectedTool"
            })
            {
                PropertyInfo property =
                    type.GetProperty(
                        name,
                        flags);

                if (property != null)
                {
                    currentToolProperty =
                        property;

                    return;
                }

                FieldInfo field =
                    type.GetField(
                        name,
                        flags);

                if (field != null)
                {
                    currentToolField =
                        field;

                    return;
                }
            }
        }

        private void ResolveInputContract()
        {
            useToolAction = null;
            useToolInputResolved = false;

            if (sharedUseToolInputSource == null)
            {
                useToolInputContract =
                    "Shared input source missing";

                return;
            }

            Type type =
                sharedUseToolInputSource.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            foreach (string name in new[]
            {
                "useToolAction",
                "UseToolAction",
                "useAction",
                "UseAction",
                "primaryAction",
                "PrimaryAction",
                "fireAction",
                "FireAction"
            })
            {
                FieldInfo field =
                    type.GetField(
                        name,
                        flags);

                if (
                    field == null ||
                    !typeof(InputActionReference)
                        .IsAssignableFrom(
                            field.FieldType)
                )
                {
                    continue;
                }

                try
                {
                    useToolAction =
                        field.GetValue(
                            sharedUseToolInputSource)
                        as InputActionReference;
                }
                catch (Exception)
                {
                    useToolAction = null;
                }

                if (
                    useToolAction != null &&
                    useToolAction.action != null
                )
                {
                    useToolInputResolved = true;
                    useToolInputContract =
                        $"{type.Name}.{name} read-only";

                    return;
                }
            }

            useToolInputContract =
                "InputActionReference unresolved";
        }

        private void ResolveClarityContract()
        {
            canUsePrimaryToolProperty = null;
            canUsePrimaryToolField = null;

            if (clarityState == null)
            {
                return;
            }

            Type type =
                clarityState.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            canUsePrimaryToolProperty =
                type.GetProperty(
                    "CanUsePrimaryTool",
                    flags);

            canUsePrimaryToolField =
                type.GetField(
                    "canUsePrimaryTool",
                    flags);
        }

        private void RefreshCurrentTool()
        {
            currentTool = "Unknown";

            if (figureToolSource == null)
            {
                frameGunSelected = false;
                return;
            }

            object value = null;

            try
            {
                if (currentToolProperty != null)
                {
                    value =
                        currentToolProperty.GetValue(
                            figureToolSource);
                }
                else if (currentToolField != null)
                {
                    value =
                        currentToolField.GetValue(
                            figureToolSource);
                }
                else
                {
                    ResolveToolContract();
                }
            }
            catch (Exception)
            {
                value = null;
            }

            if (value != null)
            {
                currentTool =
                    value.ToString();
            }

            string normalized =
                currentTool
                    .Replace(" ", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();

            frameGunSelected =
                normalized.Contains(
                    "framegun") ||
                normalized.Contains(
                    "frameanchor") ||
                normalized.Contains(
                    "anchor") ||
                normalized.Contains(
                    "cerceve") ||
                normalized.Contains(
                    "çerçeve");
        }

        private void RefreshPrimaryToolGate()
        {
            primaryToolAllowed = true;

            if (clarityState == null)
            {
                return;
            }

            try
            {
                if (
                    canUsePrimaryToolProperty != null &&
                    canUsePrimaryToolProperty.PropertyType ==
                        typeof(bool)
                )
                {
                    primaryToolAllowed =
                        (bool)
                        canUsePrimaryToolProperty.GetValue(
                            clarityState);

                    return;
                }

                if (
                    canUsePrimaryToolField != null &&
                    canUsePrimaryToolField.FieldType ==
                        typeof(bool)
                )
                {
                    primaryToolAllowed =
                        (bool)
                        canUsePrimaryToolField.GetValue(
                            clarityState);
                }
            }
            catch (Exception)
            {
                primaryToolAllowed = true;
            }
        }

        private void RefreshNearestSeam()
        {
            nearestSeamReference = null;
            nearestSeamDistance =
                float.PositiveInfinity;

            nearestSeam = "None";
            seamInRange = false;

            if (
                underlayerDive == null ||
                figureRoot == null
            )
            {
                nearestSeamDistance = -1f;
                return;
            }

            EvaluateSeam(
                underlayerDive.SeamA);

            EvaluateSeam(
                underlayerDive.SeamB);

            if (
                float.IsPositiveInfinity(
                    nearestSeamDistance)
            )
            {
                nearestSeamDistance = -1f;
                return;
            }

            seamInRange =
                nearestSeamReference != null &&
                nearestSeamDistance <=
                    braceUseRadius;
        }

        private void EvaluateSeam(
            PrototypeUnderlayerSeam seam)
        {
            if (
                seam == null ||
                !seam.IsConfigured
            )
            {
                return;
            }

            Transform anchor =
                underlayerDive.InUnderlayer
                    ? seam.UnderlayerAnchor
                    : seam.SurfaceAnchor;

            if (anchor == null)
            {
                return;
            }

            float distance =
                Vector3.Distance(
                    figureRoot.position,
                    anchor.position);

            if (
                distance <
                nearestSeamDistance
            )
            {
                nearestSeamReference = seam;
                nearestSeamDistance = distance;
                nearestSeam =
                    seam.SeamId.ToString();
            }
        }

        private void RefreshUseToolAction()
        {
            usePressed = false;

            if (
                useToolAction == null ||
                useToolAction.action == null
            )
            {
                ResolveInputContract();
            }

            useToolInputResolved =
                useToolAction != null &&
                useToolAction.action != null;
        }

        private void ProcessBraceInput()
        {
            if (
                !figureRoleActive ||
                !frameGunSelected ||
                !primaryToolAllowed ||
                !seamInRange ||
                !useToolInputResolved ||
                useToolAction == null ||
                useToolAction.action == null ||
                !useToolAction.action.enabled
            )
            {
                return;
            }

            usePressed =
                useToolAction.action
                    .WasPressedThisFrame();

            if (!usePressed)
            {
                return;
            }

            braceAttemptCount++;

            ApplyBrace(
                nearestSeamReference,
                "Existing Figure UseTool");
        }

        private bool ApplyBrace(
            PrototypeUnderlayerSeam seam,
            string source)
        {
            if (
                seam == null ||
                !seam.IsConfigured
            )
            {
                Reject(
                    "Çerçeve Ankrajı: geçerli dikiş yok.");

                return false;
            }

            seam.BraceForSeconds(
                braceSeconds,
                source);

            braceSuccessCount++;

            lastAction =
                seam.IsSealed
                    ? $"Dikiş {seam.SeamId}: Yağlı Boya mühürü {braceSeconds:F1} sn bypass edildi."
                    : $"Dikiş {seam.SeamId}: {braceSeconds:F1} sn güvenli açık tutuluyor.";

            Debug.Log(
                "[M52.2 Frame Gun Seam Brace]\n" +
                $"Source={source}\n" +
                $"Seam={seam.SeamId}\n" +
                $"BraceSeconds={braceSeconds:F2}\n" +
                $"OilSealTimerStillActive={seam.IsSealed}\n" +
                $"BlocksTraversal={seam.BlocksTraversal}\n" +
                "UsesExistingFigureUseToolActionReadOnly=True\n" +
                "AddsNewGameplayInputAction=False\n" +
                "BraceTemporarilyOverridesOilSeal=True\n" +
                "BraceDeletesOilSealTimer=False\n" +
                "NetworkAuthorityEnabled=False",
                this);

            return true;
        }

        private void Reject(
            string reason)
        {
            rejectedBraceCount++;
            lastAction = reason;
        }

        private void RefreshHud()
        {
            bool visible =
                figureRoleActive &&
                (
                    frameGunSelected ||
                    underlayerDive != null &&
                    underlayerDive.InUnderlayer
                );

            if (statusGroup != null)
            {
                statusGroup.alpha =
                    visible ? 1f : 0f;

                statusGroup.interactable = false;
                statusGroup.blocksRaycasts = false;
            }

            if (!visible)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text =
                    "ALT KATMAN • ÇERÇEVE ANKRAJI";
            }

            if (stateText != null)
            {
                string seamState = "YOK";

                if (nearestSeamReference != null)
                {
                    if (nearestSeamReference.IsBraced)
                    {
                        seamState =
                            $"ANKRAJ {nearestSeamReference.BraceRemainingSeconds:F1}s";
                    }
                    else if (nearestSeamReference.IsSealed)
                    {
                        seamState =
                            $"MÜHÜRLÜ {nearestSeamReference.SealedRemainingSeconds:F1}s";
                    }
                    else
                    {
                        seamState = "AÇIK";
                    }
                }

                stateText.text =
                    $"ARAÇ {currentTool.ToUpperInvariant()}   " +
                    $"INPUT {(useToolInputResolved ? "OK" : "YOK")}\n" +
                    $"DİKİŞ {nearestSeam}   " +
                    $"MESAFE {(nearestSeamDistance >= 0f ? nearestSeamDistance.ToString("F2") : "-")} m   " +
                    $"DURUM {seamState}\n" +
                    lastAction;
            }

            if (controlsText != null)
            {
                controlsText.text =
                    "ÇERÇEVE TABANCASI • DİKİŞE YAKLAŞ • MEVCUT E / USETOOL\n" +
                    "ANKRAJ YAĞLI BOYA SÜRESİNİ SİLMEZ • GEÇİCİ BYPASS";
            }
        }
    }
}
