using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Combat
{
    [DisallowMultipleComponent]
    public sealed class FigureInkFootStainStatus : MonoBehaviour
    {
        private const int MaximumGroundHits = 8;

        private readonly RaycastHit[] groundHits =
            new RaycastHit[MaximumGroundHits];

        [SerializeField]
        private InkPounceAttackConfig config;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private int stackCount;

        [SerializeField]
        private float remainingDuration;

        [SerializeField]
        private int footprintsPlaced;

        private FigureClarityState clarityState;
        private GameObject footprintRoot;
        private GameObject[] footprintPool;
        private float[] footprintExpiry;
        private int nextFootprintIndex;
        private bool placeLeftFoot;
        private Vector3 lastFootprintPosition;
        private float expiresAt;
        private float restoredClarityProgress;

        public int StackCount => stackCount;
        public float RemainingDuration => remainingDuration;
        public int FootprintsPlaced => footprintsPlaced;
        public bool IsStained => stackCount > 0;

        private void Awake()
        {
            clarityState = GetComponent<FigureClarityState>();
            lastFootprintPosition = transform.position;
        }

        private void OnEnable()
        {
            clarityState ??= GetComponent<FigureClarityState>();

            if (clarityState != null)
            {
                clarityState.ClarityChanged += HandleClarityChanged;
            }
        }

        private void OnDisable()
        {
            if (clarityState != null)
            {
                clarityState.ClarityChanged -= HandleClarityChanged;
            }
        }

        private void OnDestroy()
        {
            if (footprintRoot != null)
            {
                Destroy(footprintRoot);
            }
        }

        private void Update()
        {
            float now = Time.time;
            UpdateFootprintExpiry(now);

            if (stackCount <= 0 || config == null)
            {
                remainingDuration = 0f;
                return;
            }

            remainingDuration = Mathf.Max(0f, expiresAt - now);

            if (remainingDuration <= 0f)
            {
                ClearStain();
                return;
            }

            Vector3 current = transform.position;
            Vector3 planarDelta = current - lastFootprintPosition;
            planarDelta.y = 0f;

            if (planarDelta.sqrMagnitude >=
                config.FootprintStepDistance *
                config.FootprintStepDistance)
            {
                PlaceFootprint(current, now);
                lastFootprintPosition = current;
            }
        }

        public void Configure(InkPounceAttackConfig attackConfig)
        {
            config = attackConfig;
            clarityState ??= GetComponent<FigureClarityState>();
        }

        public void ApplyStain(InkPounceAttackConfig attackConfig)
        {
            Configure(attackConfig);

            if (config == null)
            {
                return;
            }

            bool wasClean = stackCount <= 0;
            stackCount = Mathf.Min(
                config.MaximumStainStacks,
                stackCount + 1);

            if (wasClean)
            {
                restoredClarityProgress = 0f;
            }
            expiresAt = Time.time + config.StainDuration;
            remainingDuration = config.StainDuration;
            lastFootprintPosition = transform.position;

            clarityState?.ApplyPaintExposure(
                config.ClarityDamage,
                FigurePaintRegion.Legs);
            EnsureFootprintPool();
            PlaceFootprint(transform.position, Time.time);
        }

        [ContextMenu("Debug/Clear Ink Foot Stain")]
        public void ClearStain()
        {
            stackCount = 0;
            remainingDuration = 0f;
            expiresAt = 0f;
            restoredClarityProgress = 0f;
        }

        private void HandleClarityChanged(
            float previousClarity,
            float currentClarity)
        {
            if (stackCount <= 0 || config == null ||
                currentClarity <= previousClarity)
            {
                return;
            }

            restoredClarityProgress +=
                currentClarity - previousClarity;
            float clarityPerStack = Mathf.Max(0.1f, config.ClarityDamage);

            while (stackCount > 0 &&
                   restoredClarityProgress >= clarityPerStack)
            {
                restoredClarityProgress -= clarityPerStack;
                stackCount--;
            }

            if (stackCount <= 0)
            {
                ClearStain();
            }
        }

        private void EnsureFootprintPool()
        {
            if (footprintPool != null || config == null)
            {
                return;
            }

            int count = config.FootprintPoolSize;
            footprintPool = new GameObject[count];
            footprintExpiry = new float[count];
            footprintRoot = new GameObject(
                $"InkFootprints_{GetInstanceID()}_Runtime");
            Mesh footprintMesh = BuildFootprintMesh();

            for (int i = 0; i < count; i++)
            {
                GameObject footprint = new GameObject(
                    $"InkFootprint_{i:00}");
                footprint.transform.SetParent(
                    footprintRoot.transform,
                    false);
                MeshFilter filter = footprint.AddComponent<MeshFilter>();
                MeshRenderer renderer =
                    footprint.AddComponent<MeshRenderer>();
                filter.sharedMesh = footprintMesh;
                renderer.sharedMaterial = config.FootprintMaterial;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                footprint.SetActive(false);
                footprintPool[i] = footprint;
            }
        }

        private void PlaceFootprint(Vector3 figurePosition, float now)
        {
            EnsureFootprintPool();

            if (footprintPool == null || footprintPool.Length == 0)
            {
                return;
            }

            Vector3 lateral = transform.right *
                (placeLeftFoot ? -0.12f : 0.12f);
            Vector3 origin = figurePosition + lateral + Vector3.up * 0.65f;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                config.GroundProbeHeight + config.GroundProbeDistance,
                config.GroundMask,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            RaycastHit bestHit = default;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null ||
                    hit.collider.transform == transform ||
                    hit.collider.transform.IsChildOf(transform) ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                bestHit = hit;
            }

            if (bestHit.collider == null)
            {
                return;
            }

            GameObject footprint = footprintPool[nextFootprintIndex];
            footprint.transform.position = bestHit.point +
                bestHit.normal * 0.012f;
            Vector3 forward = Vector3.ProjectOnPlane(
                transform.forward,
                bestHit.normal).normalized;

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.ProjectOnPlane(
                    Vector3.forward,
                    bestHit.normal).normalized;
            }

            footprint.transform.rotation = Quaternion.LookRotation(
                forward,
                bestHit.normal);
            footprint.SetActive(true);
            footprintExpiry[nextFootprintIndex] =
                now + config.FootprintLifetime;
            nextFootprintIndex =
                (nextFootprintIndex + 1) % footprintPool.Length;
            placeLeftFoot = !placeLeftFoot;
            footprintsPlaced++;
        }

        private void UpdateFootprintExpiry(float now)
        {
            if (footprintPool == null)
            {
                return;
            }

            for (int i = 0; i < footprintPool.Length; i++)
            {
                GameObject footprint = footprintPool[i];

                if (footprint != null &&
                    footprint.activeSelf &&
                    now >= footprintExpiry[i])
                {
                    footprint.SetActive(false);
                }
            }
        }

        private static Mesh BuildFootprintMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "M18_InkFootprint_Runtime"
            };
            mesh.vertices = new[]
            {
                Vector3.zero,
                new Vector3(0f, 0f, -0.13f),
                new Vector3(-0.065f, 0f, -0.055f),
                new Vector3(-0.08f, 0f, 0.055f),
                new Vector3(-0.042f, 0f, 0.14f),
                new Vector3(0.042f, 0f, 0.14f),
                new Vector3(0.08f, 0f, 0.055f),
                new Vector3(0.065f, 0f, -0.055f)
            };
            mesh.triangles = new[]
            {
                0, 1, 2,
                0, 2, 3,
                0, 3, 4,
                0, 4, 5,
                0, 5, 6,
                0, 6, 7,
                0, 7, 1
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
