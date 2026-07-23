using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaintedAlive.Paint.Ink.GlyphLoadouts
{
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class InkGlyphLoadoutController : MonoBehaviour
    {
        private static InkGlyphLoadoutController activeInstance;

        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private InkPainterEconomy economy;

        [SerializeField]
        private InkPainterNestController nestController;

        [SerializeField]
        private InkGlyphLoadoutDefinition[] loadouts;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private int selectedIndex;

        [SerializeField]
        private string lastSelectionReason = "Default";

        public static InkGlyphLoadoutController ActiveInstance =>
            activeInstance;
        public InkGlyphLoadoutDefinition ActiveLoadout =>
            ResolveActiveLoadout();
        public InkCreatureDefinition ActiveCreatureDefinition =>
            ActiveLoadout != null
                ? ActiveLoadout.CreatureDefinition
                : null;
        public float ActivePigmentCost =>
            ActiveLoadout != null
                ? ActiveLoadout.PigmentCost
                : economy != null && economy.Config != null
                    ? economy.Config.NestPlacementCost
                    : 35f;
        public int ActiveComplexityCost =>
            ActiveLoadout != null
                ? InkGlyphComplexityUtility.GetDefinitionCost(
                    ActiveLoadout.CreatureDefinition,
                    ActiveLoadout.ComplexityCost)
                : 2;
        public int SelectedIndex => selectedIndex;
        public string LastSelectionReason => lastSelectionReason;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeInstance = null;
        }

        private void Awake()
        {
            if (activeInstance != null && activeInstance != this)
            {
                Debug.LogError(
                    "Duplicate InkGlyphLoadoutController disabled. Run M22 " +
                    "Diagnose and keep one controller.",
                    this);
                enabled = false;
                return;
            }

            activeInstance = this;
            selectedIndex = FindFirstValidLoadoutIndex();

            if (roleAuthority == null ||
                economy == null ||
                nestController == null ||
                ResolveActiveLoadout() == null)
            {
                Debug.LogError(
                    "InkGlyphLoadoutController references are incomplete. " +
                    "Run M22 Setup again.",
                    this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (activeInstance == null || activeInstance == this)
            {
                activeInstance = this;
            }
        }

        private void OnDisable()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        private void Update()
        {
            if (roleAuthority == null ||
                !roleAuthority.IsInkPainter ||
                nestController == null ||
                nestController.IsCasting ||
                economy == null ||
                economy.PossessionActive ||
                IsEditingText())
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
            {
                SelectNext("G cycled glyph loadout");
            }
        }

        public void Configure(
            InkPainterRoleAuthority authority,
            InkPainterEconomy painterEconomy,
            InkPainterNestController targetNestController,
            InkGlyphLoadoutDefinition[] availableLoadouts)
        {
            roleAuthority = authority;
            economy = painterEconomy;
            nestController = targetNestController;
            loadouts = availableLoadouts;
            selectedIndex = FindFirstValidLoadoutIndex();
            lastSelectionReason = "Configured by M22 Setup";
        }

        public bool SelectNext(string reason)
        {
            if (loadouts == null || loadouts.Length == 0)
            {
                return false;
            }

            int start = Mathf.Clamp(selectedIndex, 0, loadouts.Length - 1);

            for (int step = 1; step <= loadouts.Length; step++)
            {
                int candidate = (start + step) % loadouts.Length;

                if (IsValid(loadouts[candidate]))
                {
                    selectedIndex = candidate;
                    lastSelectionReason = string.IsNullOrWhiteSpace(reason)
                        ? "Loadout cycled"
                        : reason;
                    return true;
                }
            }

            return false;
        }

        public bool Select(InkGlyphLoadoutId id, string reason)
        {
            if (loadouts == null)
            {
                return false;
            }

            for (int i = 0; i < loadouts.Length; i++)
            {
                InkGlyphLoadoutDefinition candidate = loadouts[i];

                if (IsValid(candidate) && candidate.LoadoutId == id)
                {
                    selectedIndex = i;
                    lastSelectionReason = string.IsNullOrWhiteSpace(reason)
                        ? id.ToString()
                        : reason;
                    return true;
                }
            }

            return false;
        }

        private InkGlyphLoadoutDefinition ResolveActiveLoadout()
        {
            if (loadouts == null || loadouts.Length == 0)
            {
                return null;
            }

            selectedIndex = Mathf.Clamp(
                selectedIndex,
                0,
                loadouts.Length - 1);

            if (IsValid(loadouts[selectedIndex]))
            {
                return loadouts[selectedIndex];
            }

            int fallback = FindFirstValidLoadoutIndex();
            selectedIndex = fallback;
            return fallback >= 0 ? loadouts[fallback] : null;
        }

        private int FindFirstValidLoadoutIndex()
        {
            if (loadouts == null)
            {
                return -1;
            }

            for (int i = 0; i < loadouts.Length; i++)
            {
                if (IsValid(loadouts[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsValid(InkGlyphLoadoutDefinition loadout)
        {
            return loadout != null &&
                loadout.CreatureDefinition != null;
        }

        private static bool IsEditingText()
        {
            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            return selected != null &&
                (selected.GetComponent("TMP_InputField") != null ||
                 selected.GetComponent("InputField") != null);
        }
    }
}
