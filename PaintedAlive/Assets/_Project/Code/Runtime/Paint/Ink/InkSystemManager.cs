using System.Collections.Generic;
using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Paint.Ink.GlyphLoadouts;
using PaintedAlive.Paint.Ink.Lifecycle;
using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    [DisallowMultipleComponent]
    public sealed class InkSystemManager : MonoBehaviour
    {
        private const int MaximumSpawnGroundHits = 16;

        private static InkSystemManager activeInstance;

        private readonly List<InkCreatureRuntime> activeCreatures = new();
        private readonly List<FigureMotor> cachedFigures = new();
        private readonly RaycastHit[] spawnGroundHits =
            new RaycastHit[MaximumSpawnGroundHits];

        [Header("Definitions")]
        [SerializeField]
        private InkSystemConfig config;

        [SerializeField]
        private InkCreatureDefinition lekebacakDefinition;

        [Header("Prefabs")]
        [SerializeField]
        private InkSurface inkSurfacePrefab;

        [SerializeField]
        private InkCreatureRuntime inkCreaturePrefab;

        [Header("Physics")]
        [SerializeField]
        private LayerMask navigationMask = Physics.DefaultRaycastLayers;

        [SerializeField]
        private LayerMask visibilityMask = Physics.DefaultRaycastLayers;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private int activeCreatureCount;

        [SerializeField]
        private int cachedFigureCount;

        [SerializeField]
        private string lastSpawnRejection = "None";

        private float accumulatedSimulationTime;
        private float nextFigureDiscoveryTime;

        public static InkSystemManager ActiveInstance => activeInstance;
        public InkSystemConfig Config => config;
        public InkCreatureDefinition LekebacakDefinition =>
            lekebacakDefinition;
        public IReadOnlyList<InkCreatureRuntime> ActiveCreatures =>
            activeCreatures;
        public int ActiveCreatureCount => activeCreatureCount;
        public int CachedFigureCount => cachedFigureCount;
        public int CreatureLimit => config != null
            ? config.MaximumConcurrentCreatures
            : 0;
        public string LastSpawnRejection => lastSpawnRejection;

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
                    "Duplicate InkSystemManager disabled. " +
                    "Run M15 Diagnose and keep only one manager.",
                    this);
                enabled = false;
                return;
            }

            activeInstance = this;

            if (config == null ||
                lekebacakDefinition == null ||
                inkSurfacePrefab == null ||
                inkCreaturePrefab == null)
            {
                Debug.LogError(
                    "InkSystemManager requires config, Lekebacak definition, " +
                    "surface prefab and creature prefab.",
                    this);
                enabled = false;
                return;
            }

            if (!lekebacakDefinition.ContainsGlyph(InkGlyphType.Eye) ||
                !lekebacakDefinition.ContainsGlyph(InkGlyphType.Foot))
            {
                Debug.LogError(
                    "Lekebacak definition must contain Eye and Foot glyphs.",
                    this);
                enabled = false;
            }
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

            float now = Time.time;

            if (now >= nextFigureDiscoveryTime)
            {
                RefreshFigureCache();
                nextFigureDiscoveryTime = now +
                    config.FigureDiscoveryInterval;
            }

            accumulatedSimulationTime += Time.deltaTime;

            if (accumulatedSimulationTime < config.SimulationInterval)
            {
                return;
            }

            float simulationDeltaTime = Mathf.Min(
                accumulatedSimulationTime,
                config.SimulationInterval * 2f);
            accumulatedSimulationTime = 0f;
            SimulateCreatures(simulationDeltaTime, now);
        }

        public bool TrySpawnLekebacak(
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            Vector3 facingDirection,
            out InkSurface createdSurface,
            out InkCreatureRuntime createdCreature)
        {
            return TrySpawnCreature(
                lekebacakDefinition,
                surfacePoint,
                surfaceNormal,
                facingDirection,
                out createdSurface,
                out createdCreature);
        }

        public bool TrySpawnCreature(
            InkCreatureDefinition creatureDefinition,
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            Vector3 facingDirection,
            out InkSurface createdSurface,
            out InkCreatureRuntime createdCreature)
        {
            createdSurface = null;
            createdCreature = null;
            InkCreatureDefinition definition =
                creatureDefinition != null
                    ? creatureDefinition
                    : lekebacakDefinition;
            int creatureComplexity =
                InkGlyphComplexityUtility.GetDefinitionCost(
                    definition,
                    config != null ? 2 : 0);
            InkPainterEconomy economy = InkPainterEconomy.ActiveInstance;

            if (economy != null &&
                !economy.CanCreateNest(creatureComplexity))
            {
                lastSpawnRejection = "Ink complexity budget full";
                return false;
            }

            if (!CanSpawnCreature(creatureComplexity))
            {
                return false;
            }

            Vector3 normal = NormalizeSurfaceNormal(surfaceNormal);
            Vector3 planarForward = NormalizePlanarDirection(
                facingDirection,
                normal);
            Quaternion surfaceRotation = Quaternion.FromToRotation(
                Vector3.up,
                normal);
            createdSurface = Instantiate(
                inkSurfacePrefab,
                surfacePoint + normal * 0.012f,
                surfaceRotation);
            createdSurface.name = "InkSurface_Runtime";
            createdSurface.Initialize(
                config.InitialSurfaceRadius,
                config.InitialInkAmount,
                config.InitialWetness,
                normal);

            WatercolorInkReaction surfaceReaction =
                createdSurface.GetComponent<WatercolorInkReaction>();
            surfaceReaction?.Configure(config);

            Vector3 creaturePosition =
                surfacePoint + normal * config.SurfaceOffset;

            if (!TryCreateCreature(
                    definition,
                    creaturePosition,
                    normal,
                    planarForward,
                    definition != null
                        ? definition.DisplayName + "_Runtime"
                        : "InkCreature_Runtime",
                    out createdCreature))
            {
                Destroy(createdSurface.gameObject);
                createdSurface = null;
                return false;
            }

            InkNestSpawner nest =
                createdSurface.GetComponent<InkNestSpawner>();
            nest?.Initialize(this, createdCreature);
            lastSpawnRejection = "None";
            return true;
        }

        public bool TrySpawnLekebacakFromNest(
            InkSurface nestSurface,
            Vector3 outwardDirection,
            float requestedRadius,
            out InkCreatureRuntime createdCreature)
        {
            return TrySpawnCreatureFromNest(
                nestSurface,
                lekebacakDefinition,
                outwardDirection,
                requestedRadius,
                out createdCreature);
        }

        public bool TrySpawnCreatureFromNest(
            InkSurface nestSurface,
            InkCreatureDefinition creatureDefinition,
            Vector3 outwardDirection,
            float requestedRadius,
            out InkCreatureRuntime createdCreature)
        {
            createdCreature = null;
            InkCreatureDefinition definition =
                creatureDefinition != null
                    ? creatureDefinition
                    : lekebacakDefinition;
            int creatureComplexity =
                InkGlyphComplexityUtility.GetDefinitionCost(definition);

            if (nestSurface == null || !nestSurface.IsInitialized)
            {
                lastSpawnRejection = "Nest surface invalid";
                return false;
            }

            if (!CanSpawnCreature(creatureComplexity))
            {
                return false;
            }

            Vector3 normal = NormalizeSurfaceNormal(
                nestSurface.SurfaceNormal);
            Vector3 planarForward = NormalizePlanarDirection(
                outwardDirection,
                normal);
            float radius = Mathf.Clamp(
                requestedRadius,
                0.1f,
                Mathf.Max(0.1f, nestSurface.CurrentRadius * 0.8f));
            Vector3 requestedPoint =
                nestSurface.transform.position + planarForward * radius;

            if (!TryFindSpawnGround(
                    requestedPoint,
                    out Vector3 groundedPoint,
                    out Vector3 groundedNormal))
            {
                lastSpawnRejection = "No valid nest spawn point";
                return false;
            }

            bool created = TryCreateCreature(
                definition,
                groundedPoint + groundedNormal * config.SurfaceOffset,
                groundedNormal,
                planarForward,
                definition != null
                    ? definition.DisplayName + "_Runtime_Nest"
                    : "InkCreature_Runtime_Nest",
                out createdCreature);

            if (created)
            {
                lastSpawnRejection = "None";
            }

            return created;
        }

        public void UnregisterCreature(InkCreatureRuntime creature)
        {
            if (creature == null)
            {
                return;
            }

            activeCreatures.Remove(creature);
            activeCreatureCount = activeCreatures.Count;
        }

        private bool CanSpawnCreature(int requestedComplexity = 0)
        {
            RemoveDestroyedCreatures();

            if (!isActiveAndEnabled || config == null)
            {
                lastSpawnRejection = "Ink manager unavailable";
                return false;
            }

            if (activeCreatures.Count >= config.MaximumConcurrentCreatures)
            {
                lastSpawnRejection = "Global creature limit reached";
                return false;
            }

            InkPainterEconomy economy = InkPainterEconomy.ActiveInstance;

            if (economy != null &&
                !economy.CanAddCreature(requestedComplexity))
            {
                lastSpawnRejection = "Ink complexity budget full";
                return false;
            }

            return true;
        }

        private bool TryCreateCreature(
            InkCreatureDefinition creatureDefinition,
            Vector3 position,
            Vector3 surfaceNormal,
            Vector3 facingDirection,
            string runtimeName,
            out InkCreatureRuntime createdCreature)
        {
            createdCreature = null;

            int creatureComplexity =
                InkGlyphComplexityUtility.GetDefinitionCost(
                    creatureDefinition);

            if (!CanSpawnCreature(creatureComplexity))
            {
                return false;
            }

            Vector3 normal = NormalizeSurfaceNormal(surfaceNormal);
            Vector3 planarForward = NormalizePlanarDirection(
                facingDirection,
                normal);
            Quaternion creatureRotation = Quaternion.LookRotation(
                planarForward,
                normal);
            createdCreature = Instantiate(
                inkCreaturePrefab,
                position,
                creatureRotation);
            createdCreature.name = runtimeName;

            if (!createdCreature.Initialize(
                    this,
                    config,
                    creatureDefinition != null
                        ? creatureDefinition
                        : lekebacakDefinition,
                    position))
            {
                Destroy(createdCreature.gameObject);
                createdCreature = null;
                lastSpawnRejection = "Creature initialization failed";
                return false;
            }

            WatercolorInkReaction creatureReaction =
                createdCreature.GetComponent<WatercolorInkReaction>();
            creatureReaction?.Configure(config);
            activeCreatures.Add(createdCreature);
            activeCreatureCount = activeCreatures.Count;
            lastSpawnRejection = "None";
            return true;
        }

        private bool TryFindSpawnGround(
            Vector3 requestedPoint,
            out Vector3 point,
            out Vector3 normal)
        {
            Vector3 origin = requestedPoint +
                Vector3.up * config.GroundProbeHeight;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                spawnGroundHits,
                config.GroundProbeHeight + config.GroundProbeDistance,
                navigationMask,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            RaycastHit bestHit = default;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = spawnGroundHits[i];

                if (hit.collider == null ||
                    hit.distance >= nearestDistance ||
                    Vector3.Angle(hit.normal, Vector3.up) >
                    config.MaximumWalkableSlope)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                bestHit = hit;
            }

            if (bestHit.collider == null)
            {
                point = requestedPoint;
                normal = Vector3.up;
                return false;
            }

            point = bestHit.point;
            normal = bestHit.normal.normalized;
            return true;
        }

        private void RefreshFigureCache()
        {
            FigureMotor[] figures =
                Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            cachedFigures.Clear();

            for (int i = 0; i < figures.Length; i++)
            {
                FigureMotor figure = figures[i];

                if (figure != null && figure.isActiveAndEnabled)
                {
                    cachedFigures.Add(figure);
                }
            }

            cachedFigureCount = cachedFigures.Count;
        }

        private void SimulateCreatures(float deltaTime, float now)
        {
            for (int i = activeCreatures.Count - 1; i >= 0; i--)
            {
                InkCreatureRuntime creature = activeCreatures[i];

                if (creature == null)
                {
                    activeCreatures.RemoveAt(i);
                    continue;
                }

                if (!creature.isActiveAndEnabled)
                {
                    continue;
                }

                creature.Simulate(
                    deltaTime,
                    now,
                    cachedFigures,
                    navigationMask,
                    visibilityMask);
            }

            activeCreatureCount = activeCreatures.Count;
        }

        private void RemoveDestroyedCreatures()
        {
            for (int i = activeCreatures.Count - 1; i >= 0; i--)
            {
                if (activeCreatures[i] == null)
                {
                    activeCreatures.RemoveAt(i);
                }
            }

            activeCreatureCount = activeCreatures.Count;
        }

        private static Vector3 NormalizeSurfaceNormal(Vector3 normal)
        {
            return normal.sqrMagnitude > 0.001f
                ? normal.normalized
                : Vector3.up;
        }

        private static Vector3 NormalizePlanarDirection(
            Vector3 direction,
            Vector3 normal)
        {
            Vector3 planar = Vector3.ProjectOnPlane(
                direction,
                normal).normalized;

            if (planar.sqrMagnitude < 0.001f)
            {
                planar = Vector3.ProjectOnPlane(
                    Vector3.forward,
                    normal).normalized;
            }

            if (planar.sqrMagnitude < 0.001f)
            {
                planar = Vector3.right;
            }

            return planar;
        }
    }
}
