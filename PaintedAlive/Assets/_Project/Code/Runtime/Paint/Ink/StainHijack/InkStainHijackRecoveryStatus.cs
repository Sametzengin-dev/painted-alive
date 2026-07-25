using PaintedAlive.Paint.Ink.Counterplay;
using PaintedAlive.Paint.Ink.Lifecycle;
using PaintedAlive.Paint.Ink.StainSabotage;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.StainHijack
{
    [DefaultExecutionOrder(12700)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InkCreatureRuntime))]
    public sealed class InkStainHijackRecoveryStatus : MonoBehaviour
    {
        [SerializeField]
        private InkCreatureRuntime creature;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool recovering;

        [SerializeField]
        private float remainingRecoverySeconds;

        [SerializeField]
        private float remainingCooldownSeconds;

        [SerializeField]
        private string lastResult = "Ready";

        private float recoveryEndsAt;
        private float cooldownEndsAt;

        public bool IsRecovering => recovering;
        public bool IsOnCooldown =>
            Time.unscaledTime < cooldownEndsAt;
        public float RemainingRecoverySeconds =>
            remainingRecoverySeconds;
        public float RemainingCooldownSeconds =>
            remainingCooldownSeconds;
        public string LastResult => lastResult;

        private void Awake()
        {
            ResolveCreature();
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            remainingCooldownSeconds =
                Mathf.Max(0f, cooldownEndsAt - now);

            if (!recovering)
            {
                return;
            }

            remainingRecoverySeconds =
                Mathf.Max(0f, recoveryEndsAt - now);

            if (now < recoveryEndsAt)
            {
                KeepAutonomyPaused();
                lastResult = "Signal rebuilding";
                return;
            }

            if (!CanResumeAutonomy(out string blocker))
            {
                KeepAutonomyPaused();
                lastResult = blocker;
                return;
            }

            creature.enabled = true;
            recovering = false;
            remainingRecoverySeconds = 0f;
            lastResult = "Autonomous AI restored";

            Debug.Log(
                "[M26.3] Creature autonomous AI restored after " +
                "Stain hijack.",
                creature);
        }

        public void BeginRecovery(
            float recoveryDelay,
            float reentryCooldown,
            string reason)
        {
            ResolveCreature();

            if (creature == null ||
                !creature.gameObject.activeInHierarchy)
            {
                return;
            }

            float now = Time.unscaledTime;
            recovering = true;
            recoveryEndsAt = Mathf.Max(
                recoveryEndsAt,
                now + Mathf.Max(0f, recoveryDelay));
            cooldownEndsAt = Mathf.Max(
                cooldownEndsAt,
                now + Mathf.Max(
                    recoveryDelay,
                    reentryCooldown));
            remainingRecoverySeconds =
                Mathf.Max(0f, recoveryEndsAt - now);
            remainingCooldownSeconds =
                Mathf.Max(0f, cooldownEndsAt - now);
            lastResult = string.IsNullOrWhiteSpace(reason)
                ? "Hijack released"
                : reason;
            KeepAutonomyPaused();
        }

        private bool CanResumeAutonomy(out string blocker)
        {
            ResolveCreature();

            if (creature == null ||
                !creature.gameObject.activeInHierarchy ||
                !creature.IsInitialized)
            {
                blocker = "Creature unavailable";
                return false;
            }

            InkCreatureDeathSequence death =
                GetComponent<InkCreatureDeathSequence>();

            if (death != null && death.IsDying)
            {
                blocker = "Death sequence owns creature";
                return false;
            }

            if (!creature.HasGlyph(InkGlyphType.Eye) ||
                !creature.HasGlyph(InkGlyphType.Foot))
            {
                blocker = "Critical glyph missing";
                return false;
            }

            InkCommandDisruptionStatus disruption =
                GetComponent<InkCommandDisruptionStatus>();

            if (disruption != null && disruption.IsDisrupted)
            {
                blocker = "Waiting for command disruption to end";
                return false;
            }

            InkStainSabotageStatus sabotage =
                GetComponent<InkStainSabotageStatus>();

            if (sabotage != null && sabotage.IsSabotaged)
            {
                blocker = "Waiting for sabotage to end";
                return false;
            }

            blocker = null;
            return true;
        }

        private void KeepAutonomyPaused()
        {
            ResolveCreature();

            if (creature != null && creature.enabled)
            {
                creature.enabled = false;
            }
        }

        private void ResolveCreature()
        {
            if (creature == null)
            {
                creature = GetComponent<InkCreatureRuntime>();
            }
        }
    }
}
