using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    [DisallowMultipleComponent]
    public sealed class InkSystemManager : MonoBehaviour
    {
        private static InkSystemManager activeInstance;

        private readonly List<InkCreatureRuntime> activeCreatures = new();
        private readonly List<FigureMotor> cachedFigures = new();

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

        private float accumulatedSimulationTime;
        private float nextFigureDiscoveryTime;

        public static InkSystemManager ActiveInstance => activeInstance;
        public InkSystemConfig Config => config;
        public InkCreatureDefinition LekebacakDefinition => lekebacakDefinition;
        public IReadOnlyList<InkCreatureRuntime> ActiveCreatures => activeCreatures;
        public int ActiveCreatureCount => activeCreatureCount;
        public int CachedFigureCount => cachedFigureCount;
        public int CreatureLimit => config != null
            ? config.MaximumConcurrentCreatures
            : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
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
                nextFigureDiscoveryTime = now + config.FigureDiscoveryInterval;
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
            createdSurface = null;
            createdCreature = null;

            RemoveDestroyedCreatures();

            if (!isActiveAndEnabled ||
                config == null ||
                activeCreatures.Count >= config.MaximumConcurrentCreatures)
            {
                return false;
            }

            Vector3 normal = surfaceNormal.sqrMagnitude > 0.001f
                ? surfaceNormal.normalized
                : Vector3.up;
            Vector3 planarForward = Vector3.ProjectOnPlane(
                facingDirection,
                normal).normalized;

            if (planarForward.sqrMagnitude < 0.001f)
            {
                planarForward = Vector3.ProjectOnPlane(
                    Vector3.forward,
                    normal).normalized;
            }

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
            Quaternion creatureRotation = Quaternion.LookRotation(
                planarForward,
                normal);
            createdCreature = Instantiate(
                inkCreaturePrefab,
                creaturePosition,
                creatureRotation);
            createdCreature.name = "Lekebacak_Runtime";

            if (!createdCreature.Initialize(
                    this,
                    config,
                    lekebacakDefinition,
                    creaturePosition))
            {
                Destroy(createdCreature.gameObject);
                Destroy(createdSurface.gameObject);
                createdCreature = null;
                createdSurface = null;
                return false;
            }

            WatercolorInkReaction creatureReaction =
                createdCreature.GetComponent<WatercolorInkReaction>();
            creatureReaction?.Configure(config);
            activeCreatures.Add(createdCreature);
            activeCreatureCount = activeCreatures.Count;
            return true;
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

        private void RefreshFigureCache()
        {
            FigureMotor[] figures = UnityEngine.Object.FindObjectsByType<FigureMotor>(
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
    }
}
