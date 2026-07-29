using System.Collections.Generic;
using UnityEngine;
using PaintedAlive.Figures;

namespace PaintedAlive.Figures.StainSupport.FrameExit
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PrototypeFrameExitGate : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private StainFrameExitConfig config;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer[] frameRenderers = System.Array.Empty<Renderer>();

        [Header("Runtime - Read Only")]
        [SerializeField] private FigureFrameExitOutcome lastOutcome;
        [SerializeField] private string lastFigureName = "None";
        [SerializeField] private int lastAwardedScore;
        [SerializeField] private bool lastCountsAsNormalExit;

        private readonly Dictionary<int, float> nextAllowedEvaluationByFigure = new();
        private readonly HashSet<int> figuresInside = new();
        private MaterialPropertyBlock propertyBlock;
        private float feedbackEndsAt;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public FigureFrameExitOutcome LastOutcome => lastOutcome;
        public string LastFigureName => lastFigureName;
        public int LastAwardedScore => lastAwardedScore;
        public bool LastCountsAsNormalExit => lastCountsAsNormalExit;

        public void Configure(
            StainFrameExitConfig exitConfig,
            Renderer[] renderers)
        {
            config = exitConfig;
            frameRenderers = renderers ?? System.Array.Empty<Renderer>();
            ApplyFrameColor(config != null ? config.IdleColor : Color.cyan);
        }

        /// <summary>
        /// Clears trigger/cooldown state after the authoritative prototype match
        /// teleports the Figure for a new run. Prevents a previous run's gate
        /// occupancy from blocking the next evaluation.
        /// </summary>
        public void ResetForNewMatch()
        {
            figuresInside.Clear();
            nextAllowedEvaluationByFigure.Clear();
            feedbackEndsAt = 0f;
            lastOutcome = FigureFrameExitOutcome.None;
            lastFigureName = "None";
            lastAwardedScore = 0;
            lastCountsAsNormalExit = false;
            ApplyFrameColor(config != null ? config.IdleColor : Color.cyan);
        }

        private void Awake()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            propertyBlock = new MaterialPropertyBlock();
            ApplyFrameColor(config != null ? config.IdleColor : Color.cyan);
        }

        private void Update()
        {
            if (feedbackEndsAt > 0f && Time.time >= feedbackEndsAt)
            {
                feedbackEndsAt = 0f;
                ApplyFrameColor(config != null ? config.IdleColor : Color.cyan);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryEvaluate(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryEvaluate(other);
        }

        private void OnTriggerExit(Collider other)
        {
            FigureClarityState figure = other != null
                ? other.GetComponentInParent<FigureClarityState>()
                : null;

            if (figure == null)
            {
                return;
            }

            int figureId = figure.GetInstanceID();
            figuresInside.Remove(figureId);

            float cooldown = config != null
                ? config.SameFigureCooldown
                : 1.25f;

            nextAllowedEvaluationByFigure[figureId] = Time.time + cooldown;
        }

        private void TryEvaluate(Collider other)
        {
            if (other == null)
            {
                return;
            }

            FigureClarityState figure =
                other.GetComponentInParent<FigureClarityState>();

            if (figure == null)
            {
                return;
            }

            int figureId = figure.GetInstanceID();
            if (figuresInside.Contains(figureId))
            {
                return;
            }

            if (nextAllowedEvaluationByFigure.TryGetValue(
                    figureId,
                    out float nextAllowedAt) &&
                Time.time < nextAllowedAt)
            {
                return;
            }

            figuresInside.Add(figureId);

            FigureFrameExitDecision decision =
                FigureFrameExitRuleService.Evaluate(
                    figure,
                    config,
                    transform.position);

            lastOutcome = decision.Outcome;
            lastFigureName = figure.name;
            lastAwardedScore = decision.AwardedScore;
            lastCountsAsNormalExit = decision.CountsAsNormalExit;

            Color feedbackColor = decision.CountsAsNormalExit
                ? (config != null ? config.NormalExitColor : Color.green)
                : (config != null ? config.StainArrivalColor : Color.cyan);

            ApplyFrameColor(feedbackColor);
            feedbackEndsAt = Time.time +
                (config != null ? config.FeedbackDuration : 2.4f);

            if (decision.CountsAsNormalExit)
            {
                Debug.Log(
                    $"[M36] NORMAL FIGURE EXIT | Figure={figure.name} | " +
                    $"Level={figure.CurrentLevel} | PrototypeScore=+{decision.AwardedScore}. " +
                    "Bu event ileride sunucu otoriteli skor/match flow katmanına bağlanacaktır.",
                    this);
            }
            else
            {
                Debug.Log(
                    $"[M36] STAIN SUPPORT ARRIVAL | Figure={figure.name} | " +
                    $"PrototypeScore=+{decision.AwardedScore} | NormalExit=False. " +
                    "Leke çıkışa ulaştı fakat normal Figür çıkışı tamamlanmadı; restore hâlâ gereklidir.",
                    this);
            }
        }

        private void ApplyFrameColor(Color color)
        {
            if (frameRenderers == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();

            for (int i = 0; i < frameRenderers.Length; i++)
            {
                Renderer renderer = frameRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
