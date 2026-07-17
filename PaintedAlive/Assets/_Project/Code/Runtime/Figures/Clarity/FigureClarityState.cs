using System;
using PaintedAlive.Paint;
using UnityEngine;

namespace PaintedAlive.Figures
{
    public enum FigureClarityLevel
    {
        Clean,
        Stained,
        Distorted,
        Dissolving,
        Stain
    }

    public enum FigurePaintRegion
    {
        Legs,
        Torso,
        Arms,
        Head
    }

    public sealed class FigureClarityState : MonoBehaviour
    {
        [SerializeField]
        private FigureClarityConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private float currentClarity;

        [SerializeField]
        private FigureClarityLevel currentLevel;

        [Header("Regional Paint - Read Only")]
        [SerializeField]
        private float legPaint;

        [SerializeField]
        private float torsoPaint;

        [SerializeField]
        private float armPaint;

        [SerializeField]
        private float headPaint;

        public event Action<float, float> ClarityChanged;

        public event Action<
            FigureClarityLevel,
            FigureClarityLevel> LevelChanged;

        public float CurrentClarity =>
            currentClarity;

        public float MaximumClarity =>
            config != null
                ? config.MaximumClarity
                : 100f;

        public float NormalizedClarity =>
            MaximumClarity > 0f
                ? Mathf.Clamp01(
                    currentClarity /
                    MaximumClarity)
                : 0f;

        public FigureClarityLevel CurrentLevel =>
            currentLevel;

        public bool CanJump =>
            currentLevel !=
            FigureClarityLevel.Dissolving &&
            currentLevel !=
            FigureClarityLevel.Stain;

        public bool CanSprint =>
            currentLevel !=
            FigureClarityLevel.Dissolving &&
            currentLevel !=
            FigureClarityLevel.Stain;

        public bool CanUsePrimaryTool =>
            currentLevel ==
            FigureClarityLevel.Clean ||
            currentLevel ==
            FigureClarityLevel.Stained ||
            currentLevel ==
            FigureClarityLevel.Distorted;

        public float MovementMultiplier
        {
            get
            {
                if (config == null)
                {
                    return 1f;
                }

                float levelMultiplier =
                    config.GetMovementMultiplier(
                        currentLevel);

                float legContamination =
                    Mathf.Clamp01(
                        legPaint /
                        MaximumClarity);

                float regionalMultiplier =
                    Mathf.Lerp(
                        1f,
                        config.MinimumLegMovement,
                        legContamination);

                return levelMultiplier *
                       regionalMultiplier;
            }
        }

        public float ToolEfficiency
        {
            get
            {
                if (!CanUsePrimaryTool ||
                    config == null)
                {
                    return 0f;
                }

                float armContamination =
                    Mathf.Clamp01(
                        armPaint /
                        MaximumClarity);

                return Mathf.Lerp(
                    1f,
                    config.MinimumArmEfficiency,
                    armContamination);
            }
        }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(FigureClarityState)} " +
                    "requires a FigureClarityConfig.",
                    this);

                enabled = false;
                return;
            }

            ResetToFull();
        }

        public void ApplyPaintExposure(
            float amount,
            FigurePaintRegion region)
        {
            if (amount <= 0f ||
                currentLevel ==
                FigureClarityLevel.Stain)
            {
                return;
            }

            float previousClarity =
                currentClarity;

            FigureClarityLevel previousLevel =
                currentLevel;

            currentClarity = Mathf.Max(
                0f,
                currentClarity - amount);

            AddRegionalPaint(
                region,
                amount);

            currentLevel =
                CalculateLevel();

            ClarityChanged?.Invoke(
                previousClarity,
                currentClarity);

            if (previousLevel != currentLevel)
            {
                LevelChanged?.Invoke(
                    previousLevel,
                    currentLevel);
            }
        }

        public float GetExposurePerSecond(
            OilStrokeState strokeState)
        {
            if (config == null)
            {
                return 0f;
            }

            return strokeState switch
            {
                OilStrokeState.Wet =>
                    config.WetPaintExposure,

                OilStrokeState.Drying =>
                    config.DryingPaintExposure,

                _ => 0f
            };
        }

        public void RestorePartial(
            float normalizedAmount)
        {
            normalizedAmount =
                Mathf.Clamp01(
                    normalizedAmount);

            float previousClarity =
                currentClarity;

            FigureClarityLevel previousLevel =
                currentLevel;

            float targetClarity =
                MaximumClarity *
                normalizedAmount;

            currentClarity =
                Mathf.Max(
                    currentClarity,
                    targetClarity);

            float paintRetention =
                1f - normalizedAmount;

            legPaint *= paintRetention;
            torsoPaint *= paintRetention;
            armPaint *= paintRetention;
            headPaint *= paintRetention;

            currentLevel =
                CalculateLevel();

            ClarityChanged?.Invoke(
                previousClarity,
                currentClarity);

            if (previousLevel != currentLevel)
            {
                LevelChanged?.Invoke(
                    previousLevel,
                    currentLevel);
            }
        }

        public bool RestoreClarity(
            float amount)
        {
            if (amount <= 0f ||
                currentClarity >= MaximumClarity)
            {
                return false;
            }

            float previousClarity =
                currentClarity;

            FigureClarityLevel previousLevel =
                currentLevel;

            float restoredAmount =
                Mathf.Min(
                    amount,
                    MaximumClarity -
                    currentClarity);

            currentClarity +=
                restoredAmount;

            float paintRetention =
                Mathf.Clamp01(
                    1f -
                    restoredAmount /
                    MaximumClarity);

            legPaint *= paintRetention;
            torsoPaint *= paintRetention;
            armPaint *= paintRetention;
            headPaint *= paintRetention;

            currentLevel =
                CalculateLevel();

            ClarityChanged?.Invoke(
                previousClarity,
                currentClarity);

            if (previousLevel != currentLevel)
            {
                LevelChanged?.Invoke(
                    previousLevel,
                    currentLevel);
            }

            return true;
        }

        [ContextMenu("Debug/Reset Clarity")]
        public void ResetToFull()
        {
            FigureClarityLevel previousLevel =
                currentLevel;

            float previousClarity =
                currentClarity;

            currentClarity =
                MaximumClarity;

            currentLevel =
                FigureClarityLevel.Clean;

            legPaint = 0f;
            torsoPaint = 0f;
            armPaint = 0f;
            headPaint = 0f;

            ClarityChanged?.Invoke(
                previousClarity,
                currentClarity);

            if (previousLevel != currentLevel)
            {
                LevelChanged?.Invoke(
                    previousLevel,
                    currentLevel);
            }
        }

        [ContextMenu("Debug/Restore To 50%")]
        private void DebugRestoreHalf()
        {
            RestorePartial(0.5f);
        }

        [ContextMenu("Debug/Become Stain")]
        private void DebugBecomeStain()
        {
            if (currentClarity <= 0f)
            {
                return;
            }

            ApplyPaintExposure(
                currentClarity,
                FigurePaintRegion.Torso);
        }

        private FigureClarityLevel CalculateLevel()
        {
            float normalized =
                NormalizedClarity;

            if (currentClarity <= 0f)
            {
                return FigureClarityLevel.Stain;
            }

            if (normalized <
                config.DissolvingThreshold)
            {
                return FigureClarityLevel.Dissolving;
            }

            if (normalized <
                config.DistortedThreshold)
            {
                return FigureClarityLevel.Distorted;
            }

            if (normalized <
                config.StainedThreshold)
            {
                return FigureClarityLevel.Stained;
            }

            return FigureClarityLevel.Clean;
        }

        private void AddRegionalPaint(
            FigurePaintRegion region,
            float amount)
        {
            switch (region)
            {
                case FigurePaintRegion.Legs:
                    legPaint = Mathf.Min(
                        MaximumClarity,
                        legPaint + amount);
                    break;

                case FigurePaintRegion.Torso:
                    torsoPaint = Mathf.Min(
                        MaximumClarity,
                        torsoPaint + amount);
                    break;

                case FigurePaintRegion.Arms:
                    armPaint = Mathf.Min(
                        MaximumClarity,
                        armPaint + amount);
                    break;

                case FigurePaintRegion.Head:
                    headPaint = Mathf.Min(
                        MaximumClarity,
                        headPaint + amount);
                    break;
            }
        }
    }
}