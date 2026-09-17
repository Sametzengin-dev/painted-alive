using System;
using System.Collections.Generic;
using PaintedAlive.Environment.LivingGallery;
using PaintedAlive.Environment.Palimpsest;
using PaintedAlive.Painters.Ink;
using PaintedAlive.Networking.M56;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Painters.World
{
    [DefaultExecutionOrder(1200)]
    [DisallowMultipleComponent]
    public sealed class PainterWorldActionController : MonoBehaviour
    {
        [SerializeField] private InkPainterRoleAuthority roleAuthority;
        [SerializeField] private Camera painterCamera;
        [SerializeField] private PainterBrushController worldBrush;

        [Header("Targeting")]
        [SerializeField, Min(5f)] private float maximumInteractionDistance = 120f;
        [SerializeField, Range(0.02f, 0.35f)] private float screenAimRadius = 0.14f;
        [SerializeField, Min(0.1f)] private float refreshInterval = 0.75f;

        [Header("HUD")]
        [SerializeField] private bool showControlLegend = true;
        [SerializeField] private bool showWorldActionMarkers = true;
        [SerializeField, Min(0f)] private float legendHoldSeconds = 5f;
        [SerializeField, Min(0f)] private float targetHoldSeconds = 1.25f;

        [Header("Runtime - Read Only")]
        [SerializeField] private MonoBehaviour currentTargetComponent;
        [SerializeField] private string lastAction = "No world action selected";
        [SerializeField] private int discoveredInteractionCount;
        [SerializeField] private int palimpsestInteractionCount;
        [SerializeField] private int livingGalleryInteractionCount;

        private readonly List<MonoBehaviour> interactionComponents =
            new List<MonoBehaviour>(24);

        private IPainterWorldAction currentTarget;
        private IPainterWorldAction lastRequestedTarget;
        private float nextRefreshAt;
        private float actionFeedbackUntilUnscaled;
        private bool lastRequestedActivationInProgress;
        private float legendVisibleUntilUnscaled;
        private float targetVisibleUntilUnscaled;
        private bool painterInputWasAvailable;

        public MonoBehaviour CurrentTargetComponent => currentTargetComponent;
        public string LastAction => lastAction;
        public int DiscoveredInteractionCount => discoveredInteractionCount;
        public int PalimpsestInteractionCount => palimpsestInteractionCount;
        public int LivingGalleryInteractionCount => livingGalleryInteractionCount;

        public void Configure(
            InkPainterRoleAuthority authority,
            Camera camera,
            PainterBrushController brush)
        {
            roleAuthority = authority;
            painterCamera = camera;
            worldBrush = brush;
            RefreshInteractions();
        }

        private void Awake()
        {
            roleAuthority ??= InkPainterRoleAuthority.ActiveInstance;

            if (painterCamera == null && roleAuthority != null)
                painterCamera = roleAuthority.PainterCamera;

            if (worldBrush == null && roleAuthority != null)
                worldBrush = roleAuthority.PainterBrushController;

            RefreshInteractions();
        }

        private void OnEnable()
        {
            legendVisibleUntilUnscaled =
                Time.unscaledTime + legendHoldSeconds;
            painterInputWasAvailable = false;
        }

        private void Update()
        {
            bool inputAvailable = IsPainterWorldInputAvailable();

            if (inputAvailable && !painterInputWasAvailable)
            {
                legendVisibleUntilUnscaled =
                    Time.unscaledTime + legendHoldSeconds;
            }

            painterInputWasAvailable = inputAvailable;

            if (!inputAvailable)
            {
                currentTarget = null;
                currentTargetComponent = null;
                return;
            }

            if (Time.unscaledTime >= nextRefreshAt)
            {
                RefreshInteractions();
                nextRefreshAt = Time.unscaledTime + refreshInterval;
            }

            currentTarget = ResolveAimedInteraction(out currentTargetComponent);

            if (currentTarget != null)
            {
                targetVisibleUntilUnscaled =
                    Time.unscaledTime + targetHoldSeconds;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                if (currentTarget == null)
                {
                    lastAction =
                        "Dünya mekaniği hedeflenmedi • markerı ekran merkezine getir";
                    actionFeedbackUntilUnscaled =
                        Time.unscaledTime + 2.5f;
                }
                else
                {
                    if (PaintedAliveNetworkGameplayBridge.NetworkSessionActive)
                    {
                        bool queued =
                            PaintedAliveNetworkGameplayBridge.TryRequestWorldAction(
                                currentTarget.SystemReference);

                        lastRequestedTarget = null;
                        lastRequestedActivationInProgress = false;
                        lastAction = queued
                            ? "Network request • " + currentTarget.DisplayName
                            : PaintedAliveNetworkGameplayBridge.LastFeedback;
                        actionFeedbackUntilUnscaled =
                            Time.unscaledTime + 3.5f;
                    }
                    else
                    {
                        lastRequestedTarget = currentTarget;
                        bool accepted = currentTarget.RequestActivate();
                        lastAction = currentTarget.LastResult;
                        lastRequestedActivationInProgress =
                            currentTarget.ActivationInProgress;
                        actionFeedbackUntilUnscaled =
                            Time.unscaledTime + 3.5f;

                        if (accepted)
                        {
                            targetVisibleUntilUnscaled =
                                Time.unscaledTime +
                                Mathf.Max(
                                    targetHoldSeconds,
                                    currentTarget.TelegraphSeconds + 0.5f);
                        }
                    }
                }

                legendVisibleUntilUnscaled =
                    Time.unscaledTime + legendHoldSeconds;
            }

            if (PaintedAliveNetworkGameplayBridge.NetworkSessionActive &&
                !string.IsNullOrWhiteSpace(
                    PaintedAliveNetworkGameplayBridge.LastFeedback))
            {
                string networkFeedback =
                    PaintedAliveNetworkGameplayBridge.LastFeedback;

                if (!string.Equals(
                        networkFeedback,
                        lastAction,
                        StringComparison.Ordinal))
                {
                    lastAction = networkFeedback;
                    actionFeedbackUntilUnscaled =
                        Time.unscaledTime + 3.5f;
                }
            }

            if (lastRequestedTarget != null)
            {
                bool inProgress =
                    lastRequestedTarget.ActivationInProgress;

                if (lastRequestedActivationInProgress && !inProgress)
                {
                    lastAction = lastRequestedTarget.LastResult;
                    actionFeedbackUntilUnscaled =
                        Time.unscaledTime + 3.5f;
                }

                lastRequestedActivationInProgress = inProgress;
            }
        }

        private bool IsPainterWorldInputAvailable()
        {
            roleAuthority ??= InkPainterRoleAuthority.ActiveInstance;

            if (roleAuthority == null ||
                !roleAuthority.IsInkPainter ||
                PaintedAliveNetworkRoleBridge.GameplayInputSuppressed ||
                painterCamera == null ||
                !painterCamera.enabled ||
                IsEditingText())
            {
                return false;
            }

            // Side Canvas owns the same pointer while open.
            if (worldBrush != null && !worldBrush.enabled)
                return false;

            return true;
        }

        private void RefreshInteractions()
        {
            interactionComponents.Clear();

            PalimpsestPainterWorldInteraction[] palimpsest =
                UnityEngine.Object.FindObjectsByType<
                    PalimpsestPainterWorldInteraction>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            LivingGalleryPainterWorldInteraction[] gallery =
                UnityEngine.Object.FindObjectsByType<
                    LivingGalleryPainterWorldInteraction>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            palimpsestInteractionCount =
                palimpsest != null ? palimpsest.Length : 0;
            livingGalleryInteractionCount =
                gallery != null ? gallery.Length : 0;

            if (palimpsest != null)
            {
                for (int index = 0; index < palimpsest.Length; index++)
                {
                    if (palimpsest[index] != null)
                        interactionComponents.Add(palimpsest[index]);
                }
            }

            if (gallery != null)
            {
                for (int index = 0; index < gallery.Length; index++)
                {
                    if (gallery[index] != null)
                        interactionComponents.Add(gallery[index]);
                }
            }

            discoveredInteractionCount = interactionComponents.Count;
        }

        private IPainterWorldAction ResolveAimedInteraction(
            out MonoBehaviour resolvedComponent)
        {
            resolvedComponent = null;

            if (painterCamera == null || interactionComponents.Count == 0)
                return null;

            Vector2 center = new Vector2(
                painterCamera.pixelWidth * 0.5f,
                painterCamera.pixelHeight * 0.5f);

            float referencePixels = Mathf.Max(
                1f,
                Mathf.Min(
                    painterCamera.pixelWidth,
                    painterCamera.pixelHeight));

            float maximumPixels = referencePixels * screenAimRadius;

            IPainterWorldAction best = null;
            float bestScore = float.PositiveInfinity;

            for (int index = 0; index < interactionComponents.Count; index++)
            {
                MonoBehaviour behaviour = interactionComponents[index];
                if (behaviour == null ||
                    !behaviour.isActiveAndEnabled ||
                    !(behaviour is IPainterWorldAction candidate) ||
                    candidate.ActionTransform == null)
                {
                    continue;
                }

                Vector3 delta =
                    candidate.ActionTransform.position -
                    painterCamera.transform.position;
                float distance = delta.magnitude;

                if (distance > maximumInteractionDistance)
                    continue;

                Vector3 screen = painterCamera.WorldToScreenPoint(
                    candidate.ActionTransform.position);

                if (screen.z <= 0f)
                    continue;

                float pixelDistance = Vector2.Distance(
                    center,
                    new Vector2(screen.x, screen.y));

                if (pixelDistance > maximumPixels)
                    continue;

                float score =
                    pixelDistance / referencePixels +
                    distance / maximumInteractionDistance * 0.08f;

                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                    resolvedComponent = behaviour;
                }
            }

            return best;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying ||
                roleAuthority == null ||
                !roleAuthority.IsInkPainter ||
                painterCamera == null ||
                !painterCamera.enabled)
            {
                return;
            }

            float scale = Mathf.Clamp(
                Screen.height / 900f,
                0.8f,
                1.35f);

            GUIStyle legendStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(13f * scale),
                normal =
                {
                    textColor = new Color(0.95f, 0.95f, 0.92f, 0.95f)
                },
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };

            GUIStyle targetStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = Mathf.RoundToInt(15f * scale),
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            bool showLegendNow =
                showControlLegend &&
                (currentTarget != null ||
                 Time.unscaledTime <= legendVisibleUntilUnscaled);

            if (showLegendNow)
            {
                string height =
                    roleAuthority.PainterCameraController != null
                        ? roleAuthority.PainterCameraController
                            .CurrentHeightFromFigure.ToString("F1")
                        : "—";

                string legend =
                    "PAINTER • LMB Boya • F7 Yaratık • RMB Dünya\n" +
                    "WASD • Q/E Yükseklik • R Kadraj • PgUp/PgDn • [ / ] FOV • Home\n" +
                    "Kamera yüksekliği: " + height + " m";

                GUI.Label(
                    new Rect(
                        18f * scale,
                        18f * scale,
                        Mathf.Min(Screen.width * 0.58f, 620f * scale),
                        62f * scale),
                    legend,
                    legendStyle);
            }

            if (showWorldActionMarkers && currentTarget != null)
            {
                GUIStyle markerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(12f * scale),
                    alignment = TextAnchor.MiddleCenter,
                    normal =
                    {
                        textColor = new Color(1f, 0.72f, 0.28f, 0.92f)
                    }
                };

                Vector3 screen = painterCamera.WorldToScreenPoint(
                    currentTarget.ActionTransform.position);

                if (screen.z > 0f)
                {
                    float y = Screen.height - screen.y;
                    GUI.Label(
                        new Rect(
                            screen.x - 125f * scale,
                            y - 14f * scale,
                            250f * scale,
                            28f * scale),
                        "◆ " + currentTarget.DisplayName,
                        markerStyle);
                }
            }

            bool showTargetPrompt =
                currentTarget != null ||
                Time.unscaledTime <= targetVisibleUntilUnscaled;

            if (showTargetPrompt && currentTarget != null)
            {
                string state = currentTarget.ActivationInProgress
                    ? "TELEGRAPH"
                    : "RMB İLE AKTİFLEŞTİR";

                GUI.Box(
                    new Rect(
                        Screen.width * 0.5f - 190f * scale,
                        Screen.height - 92f * scale,
                        380f * scale,
                        50f * scale),
                    currentTarget.DisplayName + "  •  " + state,
                    targetStyle);
            }

            if (Time.unscaledTime <= actionFeedbackUntilUnscaled &&
                !string.IsNullOrWhiteSpace(lastAction))
            {
                GUIStyle feedbackStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = Mathf.RoundToInt(14f * scale),
                    normal = { textColor = Color.white },
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };

                GUI.Box(
                    new Rect(
                        Screen.width * 0.5f - 270f * scale,
                        Screen.height - 150f * scale,
                        540f * scale,
                        46f * scale),
                    lastAction,
                    feedbackStyle);
            }
        }

        private static bool IsEditingText()
        {
            GameObject selected =
                EventSystem.current != null
                    ? EventSystem.current.currentSelectedGameObject
                    : null;

            if (selected == null)
                return false;

            return selected.GetComponent("TMP_InputField") != null ||
                   selected.GetComponent("InputField") != null;
        }
    }
}
