using PaintedAlive.Paint.Ink.Possession;
using PaintedAlive.Paint.Ink.GlyphLoadouts;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Economy
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class InkPainterEconomy : MonoBehaviour
    {
        private static InkPainterEconomy activeInstance;

        [SerializeField]
        private InkPainterEconomyConfig config;

        [SerializeField]
        private InkSystemManager inkManager;

        [SerializeField]
        private InkPossessionController possessionController;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private float currentPigment;

        [SerializeField]
        private int currentComplexity;

        [SerializeField]
        private int activeNestCount;

        [SerializeField]
        private int activeCreatureCount;

        [SerializeField]
        private bool castInProgress;

        [SerializeField]
        private string lastEconomyEvent = "Not started";

        private float nextComplexityRefreshTime;

        public static InkPainterEconomy ActiveInstance => activeInstance;
        public InkPainterEconomyConfig Config => config;
        public float CurrentPigment => currentPigment;
        public float PigmentNormalized => config != null
            ? Mathf.Clamp01(currentPigment / config.PigmentCapacity)
            : 0f;
        public int CurrentComplexity
        {
            get
            {
                RefreshComplexityNow();
                return currentComplexity;
            }
        }
        public float ComplexityNormalized => config != null
            ? Mathf.Clamp01((float)CurrentComplexity /
                config.MaximumComplexity)
            : 0f;
        public int ActiveNestCount => activeNestCount;
        public int ActiveCreatureCount => activeCreatureCount;
        public bool CastInProgress => castInProgress;
        public bool PossessionActive => possessionController != null &&
            possessionController.IsPossessing;
        public string LastEconomyEvent => lastEconomyEvent;

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
                    "Duplicate InkPainterEconomy disabled. Run M20 " +
                    "Diagnose and keep one local economy.",
                    this);
                enabled = false;
                return;
            }

            activeInstance = this;
            inkManager ??= InkSystemManager.ActiveInstance;

            if (config == null || inkManager == null)
            {
                Debug.LogError(
                    "InkPainterEconomy requires a config and InkSystemManager.",
                    this);
                enabled = false;
                return;
            }

            currentPigment = Mathf.Clamp(
                config.StartingPigment,
                0f,
                config.PigmentCapacity);
            RefreshComplexityNow();
            lastEconomyEvent = "Ready";
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
            castInProgress = false;

            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        private void Update()
        {
            if (config == null)
            {
                return;
            }

            if (Time.time >= nextComplexityRefreshTime)
            {
                RefreshComplexityNow();
                nextComplexityRefreshTime = Time.time +
                    config.ComplexityRefreshInterval;
            }

            if (PossessionActive)
            {
                float drain = config.PossessionDrainPerSecond *
                    Time.deltaTime;

                if (drain > 0f && currentPigment + 0.0001f < drain)
                {
                    currentPigment = 0f;
                    possessionController.ExitPossession(
                        "Ink pigment depleted");
                    lastEconomyEvent = "Possession ended: no pigment";
                    return;
                }

                currentPigment = Mathf.Max(0f, currentPigment - drain);
                return;
            }

            if (!castInProgress && currentPigment < config.PigmentCapacity)
            {
                currentPigment = Mathf.Min(
                    config.PigmentCapacity,
                    currentPigment +
                    config.RegenerationPerSecond * Time.deltaTime);
            }
        }

        public void Configure(
            InkPainterEconomyConfig economyConfig,
            InkSystemManager manager,
            InkPossessionController possession)
        {
            config = economyConfig;
            inkManager = manager;
            possessionController = possession;
        }

        public void SetCastInProgress(bool active)
        {
            castInProgress = active;
        }

        public bool CanAfford(float amount)
        {
            return amount <= 0f || currentPigment + 0.0001f >= amount;
        }

        public bool TrySpend(float amount, string reason)
        {
            amount = Mathf.Max(0f, amount);

            if (!CanAfford(amount))
            {
                lastEconomyEvent = "Rejected: insufficient pigment";
                return false;
            }

            currentPigment = Mathf.Max(0f, currentPigment - amount);
            lastEconomyEvent = string.IsNullOrWhiteSpace(reason)
                ? "Pigment spent"
                : reason;
            return true;
        }

        public void Refund(float amount, string reason)
        {
            currentPigment = Mathf.Min(
                config != null ? config.PigmentCapacity : currentPigment,
                currentPigment + Mathf.Max(0f, amount));
            lastEconomyEvent = string.IsNullOrWhiteSpace(reason)
                ? "Pigment refunded"
                : reason;
        }

        public bool CanCreateNest()
        {
            int fallback = config != null
                ? config.LekebacakComplexity
                : 2;
            return CanCreateNest(fallback);
        }

        public bool CanCreateNest(int creatureComplexity)
        {
            if (config == null)
            {
                return true;
            }

            RefreshComplexityNow();
            return currentComplexity + config.NestComplexity +
                Mathf.Max(1, creatureComplexity) <=
                config.MaximumComplexity;
        }

        public bool CanAddCreature()
        {
            int fallback = config != null
                ? config.LekebacakComplexity
                : 2;
            return CanAddCreature(fallback);
        }

        public bool CanAddCreature(int creatureComplexity)
        {
            if (config == null)
            {
                return true;
            }

            RefreshComplexityNow();
            return currentComplexity + Mathf.Max(1, creatureComplexity) <=
                config.MaximumComplexity;
        }

        [ContextMenu("Debug/Refill Ink Pigment")]
        public void Refill()
        {
            if (config == null)
            {
                return;
            }

            currentPigment = config.PigmentCapacity;
            lastEconomyEvent = "Refilled";
        }

        private void RefreshComplexityNow()
        {
            if (config == null)
            {
                currentComplexity = 0;
                return;
            }

            int nests = 0;
            var surfaces = InkSurface.ActiveSurfaces;

            for (int i = 0; i < surfaces.Count; i++)
            {
                InkSurface surface = surfaces[i];

                if (surface != null && surface.IsInitialized)
                {
                    nests++;
                }
            }

            inkManager ??= InkSystemManager.ActiveInstance;
            int creatures = 0;
            int creatureComplexity = 0;

            if (inkManager != null &&
                inkManager.ActiveCreatures != null)
            {
                for (int i = 0;
                     i < inkManager.ActiveCreatures.Count;
                     i++)
                {
                    InkCreatureRuntime creature =
                        inkManager.ActiveCreatures[i];

                    if (creature == null)
                    {
                        continue;
                    }

                    creatures++;
                    creatureComplexity +=
                        InkGlyphComplexityUtility.GetCreatureCost(
                            creature,
                            config.LekebacakComplexity);
                }
            }

            activeNestCount = nests;
            activeCreatureCount = creatures;
            currentComplexity = nests * config.NestComplexity +
                creatureComplexity;
        }
    }
}
