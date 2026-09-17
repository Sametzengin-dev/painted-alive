using System;
using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;
using UnityEngine.Events;

namespace PaintedAlive.Environment.LivingGallery
{
    [Serializable]
    public struct InkDecoyEventData
    {
        public int eventId;
        public int seed;
        public int zoneIndex;
        public Vector3 projectionPosition;
        public float orientationDegrees;
        public float distortion;
        public float opacity;
        public float startTime;
        public float lifetime;
    }

    [DisallowMultipleComponent]
    public sealed class InkReflectionDecoySystem : MonoBehaviour, ILivingGalleryResettable
    {
        [SerializeField] private Renderer receivingSurface;
        [SerializeField] private Transform poolCenter;
        [SerializeField] private InkReflectionZone[] zones = Array.Empty<InkReflectionZone>();
        [SerializeField] private UnityEvent onDecoyRequested = new UnityEvent();

        [Header("Unity-authored presence policy")]
        [SerializeField] private bool automaticPresenceDetection = true;
        [SerializeField, Min(0f)] private float presenceInnerRadius = 10.5f;
        [SerializeField, Min(0.1f)] private float presenceOuterRadius = 14.5f;
        [SerializeField, Min(0.1f)] private float presenceVerticalTolerance = 2.6f;
        [SerializeField, Min(0.1f)] private float defaultDecoyLifetime = 1.6f;
        [SerializeField, Min(0.1f)] private float repeatPresenceInterval = 2.25f;

        [Header("Runtime")]
        [SerializeField] private bool hasActiveDecoy;
        [SerializeField] private InkDecoyEventData activeDecoy;
        [SerializeField] private int nextEventId = 1;

        private readonly HashSet<FigureMotor> currentPresence =
            new HashSet<FigureMotor>();
        private readonly Dictionary<FigureMotor, float> nextFigureEventTime =
            new Dictionary<FigureMotor, float>();
        private float nextPresenceScanTime;

        public Renderer ReceivingSurface => receivingSurface;
        public Transform PoolCenter => poolCenter;
        public InkReflectionZone[] Zones => zones;
        public bool HasActiveDecoy => hasActiveDecoy;
        public InkDecoyEventData ActiveDecoy => activeDecoy;
        public bool AutomaticPresenceDetection => automaticPresenceDetection;
        public float PresenceInnerRadius => presenceInnerRadius;
        public float PresenceOuterRadius => presenceOuterRadius;

        public void Configure(Renderer surface, Transform center, InkReflectionZone[] reflectionZones)
        {
            receivingSurface = surface;
            poolCenter = center;
            zones = reflectionZones ?? Array.Empty<InkReflectionZone>();
        }

        public void ConfigurePresenceDetection(
            float innerRadius,
            float outerRadius,
            float verticalTolerance,
            float lifetime,
            float repeatInterval)
        {
            presenceInnerRadius = Mathf.Max(0f, innerRadius);
            presenceOuterRadius = Mathf.Max(presenceInnerRadius + 0.1f, outerRadius);
            presenceVerticalTolerance = Mathf.Max(0.1f, verticalTolerance);
            defaultDecoyLifetime = Mathf.Max(0.1f, lifetime);
            repeatPresenceInterval = Mathf.Max(0.1f, repeatInterval);
            automaticPresenceDetection = true;
        }

        public bool TryCreateDecoy(int eventId, int seed, float lifetime, out InkDecoyEventData data)
        {
            data = default;
            if (zones == null || zones.Length == 0)
                return false;

            int safeSeed = seed & int.MaxValue;
            int zoneIndex = safeSeed % zones.Length;
            InkReflectionZone zone = zones[zoneIndex];
            if (zone == null)
                return false;

            System.Random random = new System.Random(seed ^ (eventId * 486187739));
            data = new InkDecoyEventData
            {
                eventId = eventId,
                seed = seed,
                zoneIndex = zoneIndex,
                projectionPosition = zone.GetDeterministicPoint(seed),
                orientationDegrees = (float)random.NextDouble() * 360f,
                distortion = Mathf.Lerp(0.65f, 1.35f, (float)random.NextDouble()),
                opacity = Mathf.Lerp(0.72f, 1f, (float)random.NextDouble()),
                startTime = Time.time,
                lifetime = Mathf.Max(0.1f, lifetime)
            };
            activeDecoy = data;
            hasActiveDecoy = true;
            onDecoyRequested?.Invoke();
            return true;
        }

        private void Update()
        {
            if (hasActiveDecoy &&
                Time.time >= activeDecoy.startTime + activeDecoy.lifetime)
            {
                hasActiveDecoy = false;
            }

            if (automaticPresenceDetection && Time.time >= nextPresenceScanTime)
            {
                nextPresenceScanTime = Time.time + 0.15f;
                ScanPresence();
            }
        }

        private void ScanPresence()
        {
            if (poolCenter == null)
                return;

            FigureMotor[] figures =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            HashSet<FigureMotor> observed = new HashSet<FigureMotor>();

            for (int index = 0; index < figures.Length; index++)
            {
                FigureMotor figure = figures[index];
                if (figure == null || !figure.gameObject.activeInHierarchy)
                    continue;

                Vector3 delta = figure.transform.position - poolCenter.position;
                float vertical = Mathf.Abs(Vector3.Dot(delta, poolCenter.up));
                Vector3 planar = Vector3.ProjectOnPlane(delta, poolCenter.up);
                float radius = planar.magnitude;

                bool inside =
                    vertical <= presenceVerticalTolerance &&
                    radius >= presenceInnerRadius &&
                    radius <= presenceOuterRadius;

                if (!inside)
                    continue;

                observed.Add(figure);

                bool newlyEntered = !currentPresence.Contains(figure);
                bool repeatReady =
                    nextFigureEventTime.TryGetValue(figure, out float nextTime) &&
                    Time.time >= nextTime;

                if (newlyEntered || repeatReady)
                {
                    int eventId = nextEventId++;
                    int seed = unchecked(
                        eventId * 1103515245 +
                        figure.GetInstanceID() * 97 +
                        12345);

                    TryCreateDecoy(
                        eventId,
                        seed,
                        defaultDecoyLifetime,
                        out _);

                    nextFigureEventTime[figure] =
                        Time.time + repeatPresenceInterval;
                }
            }

            currentPresence.Clear();
            foreach (FigureMotor figure in observed)
                currentPresence.Add(figure);

            List<FigureMotor> stale = null;
            foreach (KeyValuePair<FigureMotor, float> pair in nextFigureEventTime)
            {
                if (pair.Key != null)
                    continue;
                stale ??= new List<FigureMotor>();
                stale.Add(pair.Key);
            }

            if (stale != null)
            {
                for (int index = 0; index < stale.Count; index++)
                    nextFigureEventTime.Remove(stale[index]);
            }
        }

        public void ResetLivingGalleryState()
        {
            hasActiveDecoy = false;
            activeDecoy = default;
            currentPresence.Clear();
            nextFigureEventTime.Clear();
            nextPresenceScanTime = 0f;
            nextEventId = 1;
        }
    }
}
