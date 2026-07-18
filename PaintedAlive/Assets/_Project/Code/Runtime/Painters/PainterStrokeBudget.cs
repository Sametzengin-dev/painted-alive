using PaintedAlive.Paint;
using UnityEngine;

namespace PaintedAlive.Painters
{
    public enum PainterStrokeBlockReason
    {
        None,
        Cooldown,
        ActiveStrokeLimit,
        PressureLimit,
        StrokeInProgress
    }

    [DisallowMultipleComponent]
    public sealed class PainterStrokeBudget : MonoBehaviour
    {
        [SerializeField]
        private PainterStrokeBudgetConfig config;

        [SerializeField]
        private OilStrokeSystem strokeSystem;

        private float nextAllowedStrokeTime;

        public float MaximumPressure =>
            config != null ? config.MaximumPressure : 0f;

        public int MaximumActiveStrokes =>
            config != null ? config.MaximumActiveStrokes : 0;

        public float CooldownRemaining =>
            Mathf.Max(0f, nextAllowedStrokeTime - Time.time);

        public int ActiveStrokeCount
        {
            get
            {
                if (strokeSystem == null)
                    return 0;

                int count = 0;

                foreach (OilStrokeRuntime stroke
                         in strokeSystem.Strokes)
                {
                    if (stroke == null)
                        continue;

                    if (!stroke.IsFinalized ||
                        stroke.State != OilStrokeState.Dry)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public float CurrentPressure
        {
            get
            {
                if (strokeSystem == null || config == null)
                    return 0f;

                float pressure = 0f;

                foreach (OilStrokeRuntime stroke
                         in strokeSystem.Strokes)
                {
                    if (stroke == null)
                        continue;

                    bool isDry =
                        stroke.IsFinalized &&
                        stroke.State == OilStrokeState.Dry;

                    pressure += isDry
                        ? config.DryStrokePressure
                        : config.GetActivePressure(stroke.Shape);
                }

                return pressure;
            }
        }

        public float NormalizedPressure =>
            MaximumPressure > 0f
                ? Mathf.Clamp01(
                    CurrentPressure / MaximumPressure)
                : 0f;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(PainterStrokeBudget)} requires a config.",
                    this);

                enabled = false;
                return;
            }

            if (strokeSystem == null)
            {
                Debug.LogError(
                    $"{nameof(PainterStrokeBudget)} requires " +
                    $"{nameof(OilStrokeSystem)}.",
                    this);

                enabled = false;
            }
        }

        public bool CanBeginStroke(
            OilStrokeShape shape,
            out PainterStrokeBlockReason reason)
        {
            reason = PainterStrokeBlockReason.None;

            if (config == null || strokeSystem == null)
                return false;

            if (strokeSystem.IsDrawing)
            {
                reason =
                    PainterStrokeBlockReason.StrokeInProgress;

                return false;
            }

            if (CooldownRemaining > 0f)
            {
                reason =
                    PainterStrokeBlockReason.Cooldown;

                return false;
            }

            if (ActiveStrokeCount >=
                config.MaximumActiveStrokes)
            {
                reason =
                    PainterStrokeBlockReason.ActiveStrokeLimit;

                return false;
            }

            float projectedPressure =
                CurrentPressure +
                config.GetActivePressure(shape);

            if (projectedPressure >
                config.MaximumPressure + 0.001f)
            {
                reason =
                    PainterStrokeBlockReason.PressureLimit;

                return false;
            }

            return true;
        }

        public float GetTelegraphDuration(
            OilStrokeShape shape)
        {
            return config != null
                ? config.GetTelegraphDuration(shape)
                : 0f;
        }

        public float GetPigmentSurcharge(
            OilStrokeShape shape)
        {
            return config != null
                ? config.GetPigmentSurcharge(shape)
                : 0f;
        }

        public void NotifyStrokeCommitted()
        {
            if (config == null)
                return;

            nextAllowedStrokeTime =
                Time.time + config.StrokeCooldown;
        }

        public void ResetBudget()
        {
            nextAllowedStrokeTime = 0f;
        }
    }
}