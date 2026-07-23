using PaintedAlive.Paint.Ink.Commands;
using PaintedAlive.Paint.Ink.Counterplay;
using PaintedAlive.Paint.Ink.Possession;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.StainSabotage
{
    [DefaultExecutionOrder(12500)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InkCreatureRuntime))]
    public sealed class InkStainSabotageStatus : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        [SerializeField]
        private InkCreatureRuntime creature;

        [SerializeField]
        private InkStainSabotageConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool sabotaged;

        [SerializeField]
        private float remainingSeconds;

        [SerializeField]
        private string lastReason = "Ready";

        private Renderer[] renderers;
        private MaterialPropertyBlock[] originalBlocks;
        private MaterialPropertyBlock[] animatedBlocks;
        private Vector3 capturedScale;
        private bool capturedCreatureEnabled;
        private bool creatureWasEnabled;
        private float sabotageEndsAt;

        public bool IsSabotaged => sabotaged;
        public float RemainingSeconds => remainingSeconds;
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
            if (!sabotaged)
            {
                return;
            }

            remainingSeconds =
                Mathf.Max(0f, sabotageEndsAt - Time.time);

            if (Time.time >= sabotageEndsAt)
            {
                EndSabotage("Signal recovered");
                return;
            }

            UpdateVisualFeedback();
        }

        private void OnDisable()
        {
            if (sabotaged)
            {
                EndSabotage("Sabotage status disabled");
            }
        }

        private void OnDestroy()
        {
            if (sabotaged)
            {
                EndSabotage("Sabotage status destroyed");
            }
        }

        public void Configure(InkStainSabotageConfig sabotageConfig)
        {
            config = sabotageConfig;

            if (creature == null)
            {
                creature = GetComponent<InkCreatureRuntime>();
            }
        }

        public bool Apply(Transform source)
        {
            if (creature == null)
            {
                creature = GetComponent<InkCreatureRuntime>();
            }

            if (creature == null ||
                config == null ||
                !creature.IsInitialized ||
                creature.IsFixed ||
                creature.IsPinned)
            {
                return false;
            }

            InkCommandDisruptionStatus disruption =
                GetComponent<InkCommandDisruptionStatus>();

            if (disruption != null && disruption.IsDisrupted)
            {
                return false;
            }

            if (!sabotaged)
            {
                CancelCommand();
                ForcePossessionExit();
                CaptureVisualState();
                creatureWasEnabled = creature.enabled;
                capturedCreatureEnabled = true;
            }

            sabotaged = true;
            sabotageEndsAt = Mathf.Max(
                sabotageEndsAt,
                Time.time + config.SabotageDuration);
            remainingSeconds =
                Mathf.Max(0f, sabotageEndsAt - Time.time);
            lastReason = source != null
                ? $"Scrambled by {source.name}"
                : "Scrambled by Stain Figure";

            if (creature.enabled)
            {
                creature.enabled = false;
            }

            UpdateVisualFeedback();
            return true;
        }

        public void EndSabotage(string reason)
        {
            if (!sabotaged)
            {
                return;
            }

            sabotaged = false;
            remainingSeconds = 0f;
            RestoreVisualState();

            InkCommandDisruptionStatus disruption =
                GetComponent<InkCommandDisruptionStatus>();
            bool separatelyDisrupted =
                disruption != null &&
                disruption.IsDisrupted;

            if (capturedCreatureEnabled &&
                creature != null &&
                creatureWasEnabled &&
                !creature.IsFixed &&
                !creature.IsPinned &&
                !separatelyDisrupted)
            {
                creature.enabled = true;
            }

            capturedCreatureEnabled = false;
            lastReason = string.IsNullOrWhiteSpace(reason)
                ? "Signal recovered"
                : reason;
        }

        private void CancelCommand()
        {
            InkCreatureCommandAgent agent =
                GetComponent<InkCreatureCommandAgent>();

            if (agent != null && agent.IsCommanded)
            {
                agent.CancelCommand("Stain Figure scrambled creature");
            }
        }

        private void ForcePossessionExit()
        {
            InkPossessionController[] controllers =
                Object.FindObjectsByType<InkPossessionController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            for (int i = 0; i < controllers.Length; i++)
            {
                InkPossessionController controller = controllers[i];

                if (controller != null &&
                    controller.IsPossessing &&
                    controller.PossessedCreature == creature)
                {
                    controller.ExitPossession(
                        "Creature signal scrambled by Stain Figure");
                }
            }
        }

        private void CaptureVisualState()
        {
            capturedScale = transform.localScale;
            renderers = GetComponentsInChildren<Renderer>(true);
            originalBlocks =
                new MaterialPropertyBlock[renderers.Length];
            animatedBlocks =
                new MaterialPropertyBlock[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];
                originalBlocks[i] = new MaterialPropertyBlock();
                animatedBlocks[i] = new MaterialPropertyBlock();

                if (target == null)
                {
                    continue;
                }

                target.GetPropertyBlock(originalBlocks[i]);
                target.GetPropertyBlock(animatedBlocks[i]);
            }
        }

        private void UpdateVisualFeedback()
        {
            if (config == null)
            {
                return;
            }

            float wave =
                0.5f +
                0.5f * Mathf.Sin(
                    Time.unscaledTime * config.PulseSpeed);
            Color pulseColor = Color.Lerp(
                config.LowPulseColor,
                config.HighPulseColor,
                wave);
            float scale =
                1f +
                (wave * 2f - 1f) * config.ScalePulse;
            transform.localScale = capturedScale * scale;

            if (renderers == null || animatedBlocks == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];

                if (target == null ||
                    i >= animatedBlocks.Length ||
                    animatedBlocks[i] == null)
                {
                    continue;
                }

                MaterialPropertyBlock block = animatedBlocks[i];
                block.SetColor(BaseColorId, pulseColor);
                block.SetColor(ColorId, pulseColor);
                target.SetPropertyBlock(block);
            }
        }

        private void RestoreVisualState()
        {
            transform.localScale = capturedScale;

            if (renderers == null || originalBlocks == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];

                if (target != null && i < originalBlocks.Length)
                {
                    target.SetPropertyBlock(originalBlocks[i]);
                }
            }
        }
    }
}
