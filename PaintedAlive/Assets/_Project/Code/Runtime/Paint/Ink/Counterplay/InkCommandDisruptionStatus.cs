using PaintedAlive.Paint.Ink.Commands;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Counterplay
{
    [DefaultExecutionOrder(12000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InkCreatureRuntime))]
    public sealed class InkCommandDisruptionStatus : MonoBehaviour
    {
        [SerializeField]
        private InkCreatureRuntime creature;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool disrupted;

        [SerializeField]
        private float remainingSeconds;

        [SerializeField]
        private InkCreatureCommandRole disruptedRole;

        [SerializeField]
        private string lastReason = "Ready";

        private bool capturedCreatureEnabled;
        private bool creatureWasEnabled;
        private float disruptionEndsAt;

        public bool IsDisrupted => disrupted;
        public float RemainingSeconds => remainingSeconds;
        public InkCreatureCommandRole DisruptedRole => disruptedRole;
        public string LastReason => lastReason;

        private void Awake()
        {
            if (creature == null)
            {
                creature = GetComponent<InkCreatureRuntime>();
            }
        }

        private void Update()
        {
            if (!disrupted)
            {
                return;
            }

            remainingSeconds =
                Mathf.Max(0f, disruptionEndsAt - Time.time);

            if (Time.time >= disruptionEndsAt)
            {
                EndDisruption();
            }
        }

        private void OnDisable()
        {
            if (disrupted)
            {
                EndDisruption();
            }
        }

        private void OnDestroy()
        {
            if (disrupted)
            {
                EndDisruption();
            }
        }

        public void Apply(
            InkCreatureCommandRole role,
            float duration,
            string reason)
        {
            if (creature == null)
            {
                creature = GetComponent<InkCreatureRuntime>();
            }

            if (creature == null || !creature.IsInitialized)
            {
                return;
            }

            if (!disrupted)
            {
                creatureWasEnabled = creature.enabled;
                capturedCreatureEnabled = true;
            }

            disrupted = true;
            disruptedRole = role;
            disruptionEndsAt =
                Mathf.Max(disruptionEndsAt, Time.time + duration);
            remainingSeconds =
                Mathf.Max(0f, disruptionEndsAt - Time.time);
            lastReason = string.IsNullOrWhiteSpace(reason)
                ? "Command signal disrupted"
                : reason;

            if (creature.enabled)
            {
                creature.enabled = false;
            }
        }

        private void EndDisruption()
        {
            disrupted = false;
            remainingSeconds = 0f;

            if (capturedCreatureEnabled &&
                creature != null &&
                creatureWasEnabled &&
                !creature.IsFixed &&
                !creature.IsPinned)
            {
                creature.enabled = true;
            }

            capturedCreatureEnabled = false;
            lastReason = "Autonomous signal restored";
        }
    }
}
