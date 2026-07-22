using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Lifecycle
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InkSurface))]
    public sealed class InkNestSpawner : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        private readonly List<InkCreatureRuntime> activeChildren = new();

        [SerializeField]
        private InkSurface inkSurface;

        [SerializeField]
        private Renderer surfaceRenderer;

        [SerializeField]
        private InkNestLifecycleConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private InkCreatureRuntime foundingCreature;

        [SerializeField]
        private int totalChildrenSpawned;

        [SerializeField]
        private float nextSpawnTime;

        [SerializeField]
        private bool spawnTelegraphActive;

        [SerializeField]
        private string lastSpawnResult = "Not initialized";

        private InkSystemManager manager;
        private MaterialPropertyBlock propertyBlock;
        private bool initialized;

        public InkSurface Surface => inkSurface;
        public InkCreatureRuntime FoundingCreature => foundingCreature;
        public int ActiveChildCount => CountActiveChildren();
        public int TotalChildrenSpawned => totalChildrenSpawned;
        public bool SpawnTelegraphActive => spawnTelegraphActive;
        public string LastSpawnResult => lastSpawnResult;
        public float TimeUntilNextSpawn => initialized
            ? Mathf.Max(0f, nextSpawnTime - Time.time)
            : 0f;

        private void Awake()
        {
            inkSurface ??= GetComponent<InkSurface>();
            surfaceRenderer ??= GetComponent<Renderer>();
        }

        private void OnDisable()
        {
            RestoreSurfaceColor();
        }

        private void Update()
        {
            if (!initialized ||
                config == null ||
                inkSurface == null ||
                !inkSurface.IsInitialized)
            {
                return;
            }

            float now = Time.time;
            RemoveDestroyedChildren();
            manager ??= InkSystemManager.ActiveInstance;
            bool hasLocalCapacity =
                activeChildren.Count < config.MaximumActiveChildren;
            bool hasGlobalCapacity = manager != null &&
                manager.ActiveCreatureCount < manager.CreatureLimit;
            bool shouldTelegraph = hasLocalCapacity &&
                hasGlobalCapacity &&
                now >= nextSpawnTime - config.SpawnTelegraphDuration;
            SetTelegraph(shouldTelegraph, now);

            if (now < nextSpawnTime)
            {
                return;
            }

            if (activeChildren.Count >= config.MaximumActiveChildren)
            {
                DelayBlockedSpawn("Per-nest child limit reached", now);
                return;
            }

            if (manager == null ||
                manager.ActiveCreatureCount >= manager.CreatureLimit)
            {
                DelayBlockedSpawn("Global creature limit reached", now);
                return;
            }

            Vector3 direction = BuildSpawnDirection();

            if (!manager.TrySpawnLekebacakFromNest(
                    inkSurface,
                    direction,
                    config.SpawnRadius,
                    out InkCreatureRuntime child))
            {
                DelayBlockedSpawn("No valid nest spawn point", now);
                return;
            }

            activeChildren.Add(child);
            totalChildrenSpawned++;
            nextSpawnTime = now + config.SpawnInterval;
            lastSpawnResult = "Spawned";
            SetTelegraph(false, now);
        }

        public void Configure(
            InkSurface surface,
            Renderer targetRenderer,
            InkNestLifecycleConfig lifecycleConfig)
        {
            inkSurface = surface;
            surfaceRenderer = targetRenderer;
            config = lifecycleConfig;
        }

        public void Initialize(
            InkSystemManager systemManager,
            InkCreatureRuntime initialCreature)
        {
            manager = systemManager;
            foundingCreature = initialCreature;
            activeChildren.Clear();
            totalChildrenSpawned = 0;
            initialized = config != null && inkSurface != null;
            nextSpawnTime = Time.time +
                (config != null ? config.FirstSpawnDelay : 6f);
            lastSpawnResult = initialized ? "Waiting" : "Missing config";
            SetTelegraph(false, Time.time);
        }

        private int CountActiveChildren()
        {
            RemoveDestroyedChildren();
            return activeChildren.Count;
        }

        private void RemoveDestroyedChildren()
        {
            for (int i = activeChildren.Count - 1; i >= 0; i--)
            {
                InkCreatureRuntime child = activeChildren[i];

                if (child == null)
                {
                    activeChildren.RemoveAt(i);
                }
            }
        }

        private Vector3 BuildSpawnDirection()
        {
            float seed = Mathf.Abs(GetInstanceID() * 0.6180339f);
            float angle = Mathf.Repeat(
                seed + totalChildrenSpawned * 137.50776f,
                360f);
            Vector3 raw = Quaternion.AngleAxis(angle, Vector3.up) *
                Vector3.forward;
            Vector3 direction = Vector3.ProjectOnPlane(
                raw,
                inkSurface.SurfaceNormal).normalized;
            return direction.sqrMagnitude > 0.001f
                ? direction
                : transform.forward;
        }

        private void DelayBlockedSpawn(string reason, float now)
        {
            lastSpawnResult = reason;
            nextSpawnTime = now + config.BlockedRetryDelay;
            SetTelegraph(false, now);
        }

        private void SetTelegraph(bool active, float now)
        {
            if (!active)
            {
                if (spawnTelegraphActive)
                {
                    RestoreSurfaceColor();
                }

                spawnTelegraphActive = false;
                return;
            }

            spawnTelegraphActive = true;

            if (surfaceRenderer == null || inkSurface == null)
            {
                return;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(now * 11f);
            Color baseColor = GetSurfaceColor();
            Color warningColor = Color.Lerp(
                baseColor,
                new Color(0.22f, 0.035f, 0.29f, 1f),
                0.45f + pulse * 0.45f);
            propertyBlock ??= new MaterialPropertyBlock();
            surfaceRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, warningColor);
            propertyBlock.SetColor(ColorId, warningColor);
            surfaceRenderer.SetPropertyBlock(propertyBlock);
        }

        private void RestoreSurfaceColor()
        {
            if (surfaceRenderer == null || inkSurface == null)
            {
                return;
            }

            Color surfaceColor = GetSurfaceColor();
            propertyBlock ??= new MaterialPropertyBlock();
            surfaceRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, surfaceColor);
            propertyBlock.SetColor(ColorId, surfaceColor);
            surfaceRenderer.SetPropertyBlock(propertyBlock);
        }

        private Color GetSurfaceColor()
        {
            float wetness = inkSurface != null
                ? inkSurface.Wetness
                : 1f;
            return Color.Lerp(
                new Color(0.012f, 0.01f, 0.018f, 0.96f),
                new Color(0.035f, 0.025f, 0.055f, 1f),
                wetness);
        }
    }
}
