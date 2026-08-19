using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PaintedAlive.UI.UnifiedHUD
{
    /// <summary>
    /// Keeps known passive prototype/debug HUDs available behind F3 while
    /// preserving every gameplay- or interaction-bearing HUD during normal play.
    ///
    /// Important: this controller no longer disables every Painted Alive type
    /// whose name ends in "HUD". Only an explicit allowlist of passive debug
    /// panels is managed. Unknown HUDs remain enabled so restore, tools, prompts,
    /// pickups, Stain abilities, and other inputs cannot be coupled to F3.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrototypeDeveloperHudVisibilityController : MonoBehaviour
    {
        private static readonly HashSet<string> PassiveDebugHudTypeNames =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "PrototypeJourneyScoreHUD",
                "PrototypeMatchExpeditionResultHUD",
                "PrototypeEncounterRhythmHUD",
                "PrototypeOneVsOnePlaytestHUD",
                "PrototypePlaytestAcceptanceHUD",
                "PrototypeNetworkSpikeHUD",
                "PrototypeMatchHUD",
                "PrototypeExpeditionMatchHUD"
            };

        [Header("Toggle")]
        [SerializeField] private bool developerHudVisibleAtStart;

        [Header("Discovery")]
        [SerializeField, Min(0.1f)] private float rediscoveryInterval = 1f;
        [SerializeField] private bool includeInactiveObjects = true;

        [Header("Runtime Read Only")]
        [SerializeField] private bool developerHudVisible;
        [SerializeField] private int managedHudCount;
        [SerializeField] private int preservedGameplayHudCount;
        [SerializeField] private string[] managedHudTypes = Array.Empty<string>();
        [SerializeField] private string[] preservedGameplayHudTypes = Array.Empty<string>();
        [SerializeField] private string lastToggleReason = "Not initialized";

        private readonly List<LegacyHudTarget> targets = new();
        private float nextDiscoveryAt;
        private bool initialized;

        public bool DeveloperHudVisible => developerHudVisible;
        public int ManagedHudCount => managedHudCount;
        public int PreservedGameplayHudCount => preservedGameplayHudCount;
        public string LastToggleReason => lastToggleReason;

        public string[] GetManagedHudTypeNames()
        {
            return (string[])managedHudTypes.Clone();
        }

        public string[] GetPreservedGameplayHudTypeNames()
        {
            return (string[])preservedGameplayHudTypes.Clone();
        }

        private void Awake()
        {
            developerHudVisible = developerHudVisibleAtStart;
            DiscoverAndApply("Awake");
            initialized = true;
        }

        private void OnEnable()
        {
            if (!initialized)
            {
                return;
            }

            DiscoverAndApply("OnEnable");
        }

        private void Update()
        {
            if (WasTogglePressedThisFrame())
            {
                SetDeveloperHudVisible(
                    !developerHudVisible,
                    "Keyboard F3");
            }

            if (Time.unscaledTime >= nextDiscoveryAt)
            {
                nextDiscoveryAt = Time.unscaledTime + rediscoveryInterval;
                DiscoverAndApply("Periodic discovery");
            }
        }

        private void OnDisable()
        {
            RestoreOriginalStates();
        }

        private void OnDestroy()
        {
            RestoreOriginalStates();
        }

        public void Configure(bool configuredVisibleAtStart)
        {
            developerHudVisibleAtStart = configuredVisibleAtStart;
        }

        private static bool WasTogglePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                keyboard.f3Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F3);
#else
            return false;
#endif
        }

        [ContextMenu("Refresh Legacy HUD Targets")]
        public void RefreshTargets()
        {
            DiscoverAndApply("Inspector refresh");
        }

        [ContextMenu("Show Developer HUD")]
        public void ShowDeveloperHud()
        {
            SetDeveloperHudVisible(true, "Inspector command");
        }

        [ContextMenu("Hide Developer HUD")]
        public void HideDeveloperHud()
        {
            SetDeveloperHudVisible(false, "Inspector command");
        }

        public void SetDeveloperHudVisible(bool visible, string reason)
        {
            developerHudVisible = visible;
            lastToggleReason = string.IsNullOrWhiteSpace(reason)
                ? "Unspecified"
                : reason;

            DiscoverTargets();
            ApplyCurrentVisibility();

            Debug.Log(
                $"[M43.3.3] Passive developer HUD " +
                $"{(visible ? "VISIBLE" : "HIDDEN")} | " +
                $"Managed={managedHudCount} | " +
                $"GameplayHUDsPreserved={preservedGameplayHudCount} | " +
                $"Reason={lastToggleReason}",
                this);
        }

        private void DiscoverAndApply(string reason)
        {
            lastToggleReason = reason;
            DiscoverTargets();
            ApplyCurrentVisibility();
        }

        private void DiscoverTargets()
        {
            // Restore before rebuilding so a destroyed/replaced target is not
            // left in a forced state.
            RestoreOriginalStates();
            targets.Clear();

            var managedNames = new HashSet<string>(StringComparer.Ordinal);
            var preservedNames = new HashSet<string>(StringComparer.Ordinal);

            FindObjectsInactive inactiveMode = includeInactiveObjects
                ? FindObjectsInactive.Include
                : FindObjectsInactive.Exclude;

            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                    inactiveMode,
                    FindObjectsSortMode.None);

            for (int index = 0; index < behaviours.Length; index++)
            {
                MonoBehaviour behaviour = behaviours[index];
                if (!IsPaintedAliveLegacyHud(behaviour))
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;

                if (PassiveDebugHudTypeNames.Contains(typeName))
                {
                    targets.Add(new LegacyHudTarget(behaviour));
                    managedNames.Add(typeName);
                }
                else
                {
                    // Unknown HUDs are intentionally preserved. Some legacy
                    // components combine prompt drawing with Update/input logic.
                    preservedNames.Add(typeName);
                }
            }

            managedHudTypes = ToSortedArray(managedNames);
            preservedGameplayHudTypes = ToSortedArray(preservedNames);
            managedHudCount = targets.Count;
            preservedGameplayHudCount = preservedGameplayHudTypes.Length;
        }

        private bool IsPaintedAliveLegacyHud(MonoBehaviour behaviour)
        {
            if (behaviour == null ||
                behaviour == this ||
                !behaviour.gameObject.scene.IsValid())
            {
                return false;
            }

            Type type = behaviour.GetType();
            string typeName = type.Name;
            string typeNamespace = type.Namespace ?? string.Empty;

            if (!typeNamespace.StartsWith(
                    "PaintedAlive",
                    StringComparison.Ordinal))
            {
                return false;
            }

            // M43 unified HUD must always remain visible.
            if (typeNamespace.StartsWith(
                    "PaintedAlive.UI.UnifiedHUD",
                    StringComparison.Ordinal))
            {
                return false;
            }

            return typeName.EndsWith(
                "HUD",
                StringComparison.OrdinalIgnoreCase);
        }

        private static string[] ToSortedArray(HashSet<string> values)
        {
            string[] result = new string[values.Count];
            values.CopyTo(result);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private void ApplyCurrentVisibility()
        {
            for (int index = 0; index < targets.Count; index++)
            {
                LegacyHudTarget target = targets[index];
                target.Apply(developerHudVisible);
            }
        }

        private void RestoreOriginalStates()
        {
            for (int index = 0; index < targets.Count; index++)
            {
                targets[index].Restore();
            }
        }

        private void OnGUI()
        {
            if (!developerHudVisible)
            {
                return;
            }

            const float width = 260f;
            const float height = 32f;
            Rect rect = new Rect(
                (Screen.width - width) * 0.5f,
                8f,
                width,
                height);

            Color previousColor = GUI.color;
            GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.88f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };

            GUI.Label(
                rect,
                "DEVELOPER HUD • F3 İLE KAPAT",
                style);

            GUI.color = previousColor;
        }

        [Serializable]
        private sealed class LegacyHudTarget
        {
            private readonly MonoBehaviour behaviour;
            private readonly bool originalBehaviourEnabled;
            private readonly Canvas canvas;
            private readonly bool originalCanvasEnabled;
            private readonly CanvasGroup canvasGroup;
            private readonly float originalCanvasGroupAlpha;
            private readonly bool originalCanvasGroupInteractable;
            private readonly bool originalCanvasGroupBlocksRaycasts;

            public LegacyHudTarget(MonoBehaviour configuredBehaviour)
            {
                behaviour = configuredBehaviour;
                originalBehaviourEnabled =
                    behaviour != null && behaviour.enabled;

                canvas = behaviour != null
                    ? behaviour.GetComponent<Canvas>()
                    : null;
                originalCanvasEnabled =
                    canvas != null && canvas.enabled;

                canvasGroup = behaviour != null
                    ? behaviour.GetComponent<CanvasGroup>()
                    : null;

                if (canvasGroup != null)
                {
                    originalCanvasGroupAlpha = canvasGroup.alpha;
                    originalCanvasGroupInteractable =
                        canvasGroup.interactable;
                    originalCanvasGroupBlocksRaycasts =
                        canvasGroup.blocksRaycasts;
                }
            }

            public void Apply(bool visible)
            {
                // Only explicit passive debug HUD types ever become targets.
                // Interaction/gameplay HUDs never reach this method and remain
                // enabled regardless of the F3 developer-visibility state.
                if (behaviour != null)
                {
                    behaviour.enabled =
                        visible && originalBehaviourEnabled;
                }

                if (canvas != null)
                {
                    canvas.enabled =
                        visible && originalCanvasEnabled;
                }

                if (canvasGroup != null)
                {
                    canvasGroup.alpha =
                        visible ? originalCanvasGroupAlpha : 0f;
                    canvasGroup.interactable =
                        visible && originalCanvasGroupInteractable;
                    canvasGroup.blocksRaycasts =
                        visible && originalCanvasGroupBlocksRaycasts;
                }
            }

            public void Restore()
            {
                if (behaviour != null)
                {
                    behaviour.enabled = originalBehaviourEnabled;
                }

                if (canvas != null)
                {
                    canvas.enabled = originalCanvasEnabled;
                }

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = originalCanvasGroupAlpha;
                    canvasGroup.interactable =
                        originalCanvasGroupInteractable;
                    canvasGroup.blocksRaycasts =
                        originalCanvasGroupBlocksRaycasts;
                }
            }
        }
    }
}
