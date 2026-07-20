using System;
using PaintedAlive.Figures.Impact;
using PaintedAlive.Paint.Sponge;
using UnityEngine;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    public sealed class SpongeBurstController : MonoBehaviour
    {
        private const int MaximumAffectedFigures = 24;
        private const int MaximumSurfaceHits = 24;
        private const int MaximumPuddleSamples = 12;
        private const float GoldenAngleRadians = 2.39996323f;

        private readonly Collider[] overlapResults =
            new Collider[MaximumAffectedFigures];

        private readonly FigureClarityState[] affectedFigures =
            new FigureClarityState[MaximumAffectedFigures];

        private readonly RaycastHit[] surfaceHits =
            new RaycastHit[MaximumSurfaceHits];

        private readonly Vector3[] puddlePositions =
            new Vector3[MaximumPuddleSamples];

        private readonly Quaternion[] puddleRotations =
            new Quaternion[MaximumPuddleSamples];

        [Header("Dependencies")]
        [SerializeField]
        private SpongeReservoir reservoir;

        [SerializeField]
        private FigureImpactSensor impactSensor;

        [SerializeField]
        private FigureMotor figureMotor;

        [SerializeField]
        private SpongeBurstConfig config;

        [SerializeField]
        private SpongeBurstFeedback feedback;

        [SerializeField]
        private SpongeAbsorbablePaintSource puddlePrefab;

        [Header("Detection")]
        [SerializeField]
        private LayerMask affectedFigureMask =
            Physics.DefaultRaycastLayers;

        [SerializeField]
        private LayerMask spillSurfaceMask =
            Physics.DefaultRaycastLayers;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private float lastRequiredImpact;

        [SerializeField]
        private float lastReceivedImpact;

        [SerializeField]
        private int burstCount;

        private float nextBurstTime;

        public float LastRequiredImpact => lastRequiredImpact;
        public float LastReceivedImpact => lastReceivedImpact;
        public int BurstCount => burstCount;

        public bool IsBurstArmed =>
            reservoir != null &&
            config != null &&
            reservoir.NormalizedFill >=
            config.MinimumFillToBurst;

        public event Action BurstOccurred;

        private void Awake()
        {
            if (reservoir == null)
            {
                reservoir = GetComponent<SpongeReservoir>();
            }

            if (impactSensor == null)
            {
                impactSensor =
                    GetComponentInParent<FigureImpactSensor>();
            }

            if (figureMotor == null)
            {
                figureMotor = GetComponentInParent<FigureMotor>();
            }

            if (feedback == null)
            {
                feedback = GetComponent<SpongeBurstFeedback>();
            }

            if (reservoir == null ||
                impactSensor == null ||
                figureMotor == null ||
                config == null ||
                puddlePrefab == null)
            {
                Debug.LogError(
                    $"{nameof(SpongeBurstController)} on {name} " +
                    "requires Reservoir, Impact Sensor, Figure " +
                    "Motor, Config and Puddle Prefab references.",
                    this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (impactSensor != null)
            {
                impactSensor.ImpactDetected += HandleImpact;
            }
        }

        private void OnDisable()
        {
            if (impactSensor != null)
            {
                impactSensor.ImpactDetected -= HandleImpact;
            }
        }

        private void HandleImpact(FigureImpactData impact)
        {
            lastReceivedImpact = impact.Speed;

            if (Time.time < nextBurstTime ||
                !IsBurstArmed)
            {
                return;
            }

            lastRequiredImpact =
                config.GetRequiredImpact(
                    reservoir.MixtureInstability);

            if (impact.Speed < lastRequiredImpact)
            {
                return;
            }

            Burst(impact);
        }

        private void Burst(FigureImpactData impact)
        {
            float storedAmount = reservoir.StoredAmount;
            Color storedColor = reservoir.StoredColor;
            float instability = reservoir.MixtureInstability;
            float normalizedFill = reservoir.NormalizedFill;

            if (storedAmount <= 0f)
            {
                return;
            }

            reservoir.RemovePaint(
                storedAmount,
                out Color removedColor,
                out float removedInstability);

            Vector3 burstPosition =
                figureMotor.transform.position + Vector3.up;

            ApplyAreaPaint(
                burstPosition,
                normalizedFill,
                instability);

            SpawnRecoverablePuddles(
                figureMotor.transform.position,
                storedAmount,
                removedColor,
                removedInstability);

            feedback?.PlayBurst(
                burstPosition,
                impact.Normal,
                storedColor,
                normalizedFill);

            burstCount++;
            nextBurstTime = Time.time + config.BurstCooldown;
            BurstOccurred?.Invoke();
        }

        private void ApplyAreaPaint(
            Vector3 center,
            float normalizedFill,
            float instability)
        {
            int hitCount =
                Physics.OverlapSphereNonAlloc(
                    center,
                    config.BurstRadius,
                    overlapResults,
                    affectedFigureMask,
                    QueryTriggerInteraction.Collide);

            int affectedCount = 0;
            float exposure =
                config.BaseClarityExposure *
                Mathf.Clamp01(normalizedFill) *
                (1f +
                 Mathf.Clamp01(instability) *
                 config.InstabilityExposureScale);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = overlapResults[i];

                if (hit == null)
                {
                    continue;
                }

                FigureClarityState clarity =
                    hit.GetComponentInParent<FigureClarityState>();

                if (clarity == null ||
                    ContainsAffectedFigure(
                        clarity,
                        affectedCount))
                {
                    continue;
                }

                affectedFigures[affectedCount] = clarity;
                affectedCount++;

                clarity.ApplyPaintExposure(
                    exposure,
                    FigurePaintRegion.Torso);

                if (affectedCount >= affectedFigures.Length)
                {
                    break;
                }
            }

            for (int i = 0; i < affectedCount; i++)
            {
                affectedFigures[i] = null;
            }
        }

        private bool ContainsAffectedFigure(
            FigureClarityState target,
            int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (affectedFigures[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private void SpawnRecoverablePuddles(
            Vector3 center,
            float storedAmount,
            Color paintColor,
            float instability)
        {
            int requestedCount =
                Mathf.Min(
                    config.PuddleCount,
                    MaximumPuddleSamples);
            int validCount = 0;
            float phase = burstCount * 0.73f;

            for (int i = 0; i < requestedCount; i++)
            {
                float normalizedIndex =
                    requestedCount > 1
                        ? i / (requestedCount - 1f)
                        : 0.5f;
                float radius =
                    Mathf.Lerp(
                        config.MinimumPuddleDistance,
                        config.MaximumPuddleDistance,
                        normalizedIndex);
                float angle =
                    phase + i * GoldenAngleRadians;
                Vector3 radialOffset =
                    new Vector3(
                        Mathf.Cos(angle),
                        0f,
                        Mathf.Sin(angle)) * radius;
                Vector3 probeOrigin =
                    center +
                    radialOffset +
                    Vector3.up * config.SurfaceProbeHeight;

                if (!TryFindSpillSurface(
                        probeOrigin,
                        out RaycastHit surfaceHit))
                {
                    continue;
                }

                puddlePositions[validCount] =
                    surfaceHit.point +
                    surfaceHit.normal * config.SurfaceOffset;
                puddleRotations[validCount] =
                    Quaternion.FromToRotation(
                        Vector3.up,
                        surfaceHit.normal);
                validCount++;
            }

            if (validCount == 0)
            {
                return;
            }

            float recoverableAmount =
                storedAmount *
                config.RecoverablePaintFraction;
            float amountPerPuddle =
                recoverableAmount / validCount;

            for (int i = 0; i < validCount; i++)
            {
                SpongeAbsorbablePaintSource puddle =
                    Instantiate(
                        puddlePrefab,
                        puddlePositions[i],
                        puddleRotations[i]);

                puddle.Initialize(
                    amountPerPuddle,
                    paintColor,
                    instability);
            }
        }

        private bool TryFindSpillSurface(
            Vector3 probeOrigin,
            out RaycastHit bestHit)
        {
            int hitCount =
                Physics.RaycastNonAlloc(
                    new Ray(probeOrigin, Vector3.down),
                    surfaceHits,
                    config.SurfaceProbeDistance,
                    spillSurfaceMask,
                    QueryTriggerInteraction.Ignore);

            bestHit = default;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = surfaceHits[i];

                if (hit.collider == null ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                FigureMotor hitFigure =
                    hit.collider.GetComponentInParent<FigureMotor>();

                if (hitFigure == figureMotor)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                bestHit = hit;
            }

            return bestHit.collider != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null)
            {
                return;
            }

            Gizmos.color =
                new Color(0.95f, 0.12f, 0.58f, 0.45f);
            Gizmos.DrawWireSphere(
                transform.position + Vector3.up,
                config.BurstRadius);
        }
    }
}
