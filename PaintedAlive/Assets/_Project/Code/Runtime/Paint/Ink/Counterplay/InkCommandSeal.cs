using System.Collections.Generic;
using PaintedAlive.Paint.Ink.Commands;
using PaintedAlive.Paint.Sponge;
using PaintedAlive.Painters.Ink;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Counterplay
{
    [DefaultExecutionOrder(-24000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class InkCommandSeal :
        MonoBehaviour,
        ISpongeAbsorbableSource
    {
        private const int SealPointCount = 16;

        [Header("References")]
        [SerializeField]
        private InkCreatureCommandDirector director;

        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private LineRenderer sealRenderer;

        [SerializeField]
        private SphereCollider sealCollider;

        [Header("Counterplay")]
        [SerializeField, Min(1f)]
        private float maximumInk = 18f;

        [SerializeField, Min(0.5f)]
        private float disruptionRadius = 7.5f;

        [SerializeField, Min(0.1f)]
        private float runnerDisruption = 2.2f;

        [SerializeField, Min(0.1f)]
        private float bulwarkDisruption = 1.35f;

        [SerializeField, Min(0.1f)]
        private float ambusherDisruption = 3f;

        [SerializeField, Min(0f)]
        private float surfaceOffset = 0.055f;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool sealActive;

        [SerializeField]
        private float remainingInk;

        [SerializeField]
        private int observedCommandSequence;

        [SerializeField]
        private int lastAffectedCount;

        [SerializeField]
        private string lastResult = "Waiting for F5 command";

        private readonly List<DisruptionCandidate> candidates = new();
        private Material sealMaterial;
        private Vector3 baseScale = Vector3.one;

        public bool CanAbsorb =>
            sealActive &&
            roleAuthority != null &&
            !roleAuthority.IsInkPainter &&
            remainingInk > 0.001f;

        public float AvailableAmount => remainingInk;
        public Color PaintColor =>
            new Color(0.38f, 0.02f, 0.48f, 1f);
        public float Instability => 0.72f;

        public bool IsSealActive => sealActive;
        public float RemainingInk => remainingInk;
        public float MaximumInk => maximumInk;
        public int ObservedCommandSequence =>
            observedCommandSequence;
        public int LastAffectedCount => lastAffectedCount;
        public string LastResult => lastResult;

        private void Awake()
        {
            EnsureComponents();
            baseScale = transform.localScale;
            SetSealVisible(false);
        }

        private void Update()
        {
            if (director == null)
            {
                return;
            }

            if (director.CommandSequence != observedCommandSequence)
            {
                observedCommandSequence =
                    director.CommandSequence;

                if (director.LastCommandedCount > 0)
                {
                    ActivateSeal(
                        director.LastCommandPoint,
                        Vector3.up);
                }
            }

            if (sealActive && director.ActiveCommandCount <= 0)
            {
                DeactivateSeal("No active creature order");
                return;
            }

            if (!sealActive)
            {
                return;
            }

            float pulse =
                1f + Mathf.Sin(Time.unscaledTime * 5.5f) * 0.045f;
            transform.localScale = baseScale * pulse;

            if (sealRenderer != null)
            {
                Color start =
                    new Color(0.92f, 0.22f, 1f, 0.98f);
                Color end =
                    new Color(0.25f, 0.01f, 0.36f, 0.96f);
                float normalized =
                    maximumInk > 0f
                        ? Mathf.Clamp01(
                            remainingInk / maximumInk)
                        : 0f;
                sealRenderer.startColor =
                    Color.Lerp(end, start, normalized);
                sealRenderer.endColor = end;
            }
        }

        private void OnDestroy()
        {
            if (sealMaterial != null)
            {
                Destroy(sealMaterial);
            }
        }

        public void Configure(
            InkCreatureCommandDirector commandDirector,
            InkPainterRoleAuthority authority)
        {
            director = commandDirector;
            roleAuthority = authority;
            EnsureComponents();
            SetSealVisible(false);
        }

        public float Absorb(float requestedAmount)
        {
            if (!CanAbsorb)
            {
                return 0f;
            }

            float absorbed = Mathf.Min(
                Mathf.Max(0f, requestedAmount),
                remainingInk);

            if (absorbed <= 0f)
            {
                return 0f;
            }

            remainingInk -= absorbed;
            lastResult = "Figure absorbing command seal";

            if (remainingInk <= 0.001f)
            {
                remainingInk = 0f;
                BreakSeal();
            }

            return absorbed;
        }

        private void ActivateSeal(
            Vector3 point,
            Vector3 normal)
        {
            EnsureComponents();
            transform.position =
                point + normal.normalized * surfaceOffset;
            transform.rotation = Quaternion.FromToRotation(
                Vector3.up,
                normal.sqrMagnitude > 0.001f
                    ? normal.normalized
                    : Vector3.up);
            transform.localScale = baseScale;
            remainingInk = maximumInk;
            lastAffectedCount = 0;
            lastResult = "Command seal active";
            sealActive = true;
            SetSealVisible(true);
        }

        private void BreakSeal()
        {
            candidates.Clear();
            InkCreatureCommandAgent[] agents =
                Object.FindObjectsByType<InkCreatureCommandAgent>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            float radiusSquared =
                disruptionRadius * disruptionRadius;

            for (int i = 0; i < agents.Length; i++)
            {
                InkCreatureCommandAgent agent = agents[i];

                if (agent == null ||
                    !agent.IsCommanded ||
                    (agent.transform.position -
                     transform.position).sqrMagnitude >
                    radiusSquared)
                {
                    continue;
                }

                candidates.Add(
                    new DisruptionCandidate(
                        agent,
                        agent.CommandRole));
            }

            director?.ReleaseAllCommands(
                "Figure absorbed command seal");
            lastAffectedCount = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                DisruptionCandidate candidate =
                    candidates[i];

                if (candidate.Agent == null)
                {
                    continue;
                }

                InkCommandDisruptionStatus status =
                    candidate.Agent.GetComponent<
                        InkCommandDisruptionStatus>();

                if (status == null)
                {
                    status =
                        candidate.Agent.gameObject.AddComponent<
                            InkCommandDisruptionStatus>();
                }

                status.Apply(
                    candidate.Role,
                    ResolveDisruptionDuration(candidate.Role),
                    "Command seal absorbed");
                lastAffectedCount++;
            }

            DeactivateSeal(
                lastAffectedCount > 0
                    ? $"Seal broken; {lastAffectedCount} creature(s) confused"
                    : "Seal broken; orders released");
        }

        private float ResolveDisruptionDuration(
            InkCreatureCommandRole role)
        {
            switch (role)
            {
                case InkCreatureCommandRole.Bulwark:
                    return bulwarkDisruption;

                case InkCreatureCommandRole.Ambusher:
                    return ambusherDisruption;

                default:
                    return runnerDisruption;
            }
        }

        private void DeactivateSeal(string reason)
        {
            sealActive = false;
            remainingInk = 0f;
            transform.localScale = baseScale;
            lastResult = string.IsNullOrWhiteSpace(reason)
                ? "Seal inactive"
                : reason;
            SetSealVisible(false);
        }

        private void EnsureComponents()
        {
            if (sealRenderer == null)
            {
                sealRenderer = GetComponent<LineRenderer>();
            }

            if (sealCollider == null)
            {
                sealCollider = GetComponent<SphereCollider>();
            }

            sealCollider.isTrigger = true;
            sealCollider.radius = 0.82f;
            sealCollider.center = Vector3.zero;

            Shader shader = Shader.Find("Sprites/Default");

            if (sealMaterial == null && shader != null)
            {
                sealMaterial = new Material(shader);
                sealMaterial.color =
                    new Color(0.72f, 0.08f, 0.9f, 0.95f);
            }

            if (sealMaterial != null)
            {
                sealRenderer.sharedMaterial = sealMaterial;
            }

            sealRenderer.useWorldSpace = false;
            sealRenderer.loop = true;
            sealRenderer.positionCount = SealPointCount;
            sealRenderer.startWidth = 0.065f;
            sealRenderer.endWidth = 0.065f;
            sealRenderer.numCornerVertices = 2;

            for (int i = 0; i < SealPointCount; i++)
            {
                float angle =
                    (Mathf.PI * 2f * i) / SealPointCount;
                float radius = (i & 1) == 0
                    ? 0.78f
                    : 0.42f;
                sealRenderer.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius));
            }
        }

        private void SetSealVisible(bool visible)
        {
            if (sealRenderer != null)
            {
                sealRenderer.enabled = visible;
            }

            if (sealCollider != null)
            {
                sealCollider.enabled = visible;
            }
        }

        private readonly struct DisruptionCandidate
        {
            public DisruptionCandidate(
                InkCreatureCommandAgent agent,
                InkCreatureCommandRole role)
            {
                Agent = agent;
                Role = role;
            }

            public InkCreatureCommandAgent Agent { get; }
            public InkCreatureCommandRole Role { get; }
        }
    }
}
