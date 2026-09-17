using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    public enum DryingPaperState
    {
        Wet = 0,
        Dry = 1,
        Transitioning = 2
    }

    [DisallowMultipleComponent]
    public sealed class DryingPaperSurface : MonoBehaviour, ILivingGalleryResettable
    {
        private static readonly Dictionary<Collider, DryingPaperSurface> ColliderRegistry =
            new Dictionary<Collider, DryingPaperSurface>();

        [SerializeField] private string bindingId;
        [SerializeField] private SkinnedMeshRenderer visualRenderer;
        [SerializeField] private MeshCollider collisionProxy;
        [SerializeField] private Material wetMaterial;
        [SerializeField] private Material dryMaterial;
        [SerializeField] private DryingPaperState initialState = DryingPaperState.Wet;
        [SerializeField] private DryingPaperState state = DryingPaperState.Wet;
        [SerializeField, Range(0.1f, 1f)] private float wetSpeedMultiplier = 0.65f;
        [SerializeField, Range(0.1f, 1f)] private float drySpeedMultiplier = 1f;
        [SerializeField, Min(0.05f)] private float transitionDuration = 1.5f;

        [Header("Unity-authored coarse collision endpoints")]
        [SerializeField, Min(0f)] private float wetSagMeters;
        [SerializeField] private bool coarseCollisionStatesConfigured;
        [SerializeField] private int coarseCollisionVertexCount;

        private int wetBlendShape = -1;
        private int dryBlendShape = -1;
        private float transitionElapsed;
        private float transitionStart;
        private float transitionTarget;

        private Mesh authoredCollisionMesh;
        private Mesh runtimeCollisionMesh;
        private Vector3[] dryCollisionVertices;
        private Vector3[] wetCollisionVertices;
        private Vector3[] blendedCollisionVertices;

        public string BindingId => bindingId;
        public DryingPaperState State => state;
        public bool CoarseCollisionStatesConfigured => coarseCollisionStatesConfigured;
        public int CoarseCollisionVertexCount => coarseCollisionVertexCount;
        public float WetSagMeters => wetSagMeters;
        public float TelegraphSeconds => transitionDuration;
        public string StateLabel => state.ToString().ToUpperInvariant();
        public bool IsConfigured =>
            visualRenderer != null &&
            collisionProxy != null &&
            wetBlendShape >= 0 &&
            dryBlendShape >= 0 &&
            coarseCollisionStatesConfigured;

        public float CurrentSpeedMultiplier =>
            state == DryingPaperState.Wet ? wetSpeedMultiplier :
            state == DryingPaperState.Dry ? drySpeedMultiplier :
            Mathf.Lerp(wetSpeedMultiplier, drySpeedMultiplier, GetDryBlend01());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ColliderRegistry.Clear();

        public void Configure(
            string id,
            SkinnedMeshRenderer renderer,
            MeshCollider collider,
            Material wet,
            Material dry,
            DryingPaperState configuredInitialState,
            float configuredWetMultiplier,
            float configuredDryMultiplier,
            float configuredTransitionDuration)
        {
            UnregisterCollider();
            bindingId = id ?? string.Empty;
            visualRenderer = renderer;
            collisionProxy = collider;
            wetMaterial = wet;
            dryMaterial = dry;
            initialState = configuredInitialState;
            wetSpeedMultiplier = Mathf.Clamp(configuredWetMultiplier, 0.1f, 1f);
            drySpeedMultiplier = Mathf.Clamp(configuredDryMultiplier, 0.1f, 1f);
            transitionDuration = Mathf.Max(0.05f, configuredTransitionDuration);
            CacheBlendShapes();
            ApplyImmediate(initialState);
            if (isActiveAndEnabled)
                RegisterCollider();
        }

        public bool ConfigureCoarseCollisionStates(float authoredWetSagMeters)
        {
            wetSagMeters = Mathf.Max(0f, authoredWetSagMeters);

            if (collisionProxy == null || collisionProxy.sharedMesh == null)
            {
                coarseCollisionStatesConfigured = false;
                coarseCollisionVertexCount = 0;
                return false;
            }

            coarseCollisionVertexCount = collisionProxy.sharedMesh.vertexCount;
            coarseCollisionStatesConfigured = coarseCollisionVertexCount > 0;

            // In edit mode store only the policy/value. Runtime mesh instances
            // are rebuilt in Awake so no DontSave mesh is serialized into scene.
            if (!Application.isPlaying)
                return coarseCollisionStatesConfigured;

            return BuildRuntimeCollisionStates();
        }

        private bool BuildRuntimeCollisionStates()
        {
            if (collisionProxy == null || collisionProxy.sharedMesh == null)
                return false;

            if (authoredCollisionMesh == null ||
                authoredCollisionMesh == runtimeCollisionMesh)
            {
                authoredCollisionMesh = collisionProxy.sharedMesh;
            }

            Mesh source = authoredCollisionMesh;
            Vector3[] sourceVertices = source.vertices;
            if (sourceVertices == null || sourceVertices.Length == 0)
                return false;

            DestroyRuntimeCollisionMesh();

            runtimeCollisionMesh = Instantiate(source);
            runtimeCollisionMesh.name =
                source.name + "_LivingGallery_RuntimePaper";
            runtimeCollisionMesh.hideFlags = HideFlags.DontSave;

            dryCollisionVertices = (Vector3[])sourceVertices.Clone();
            wetCollisionVertices = (Vector3[])sourceVertices.Clone();
            blendedCollisionVertices = new Vector3[sourceVertices.Length];

            Vector3 localUp =
                collisionProxy.transform.InverseTransformDirection(Vector3.up);
            if (localUp.sqrMagnitude < 0.0001f)
                localUp = Vector3.up;
            localUp.Normalize();

            Vector3 axisA = Vector3.Cross(localUp, Vector3.forward);
            if (axisA.sqrMagnitude < 0.0001f)
                axisA = Vector3.Cross(localUp, Vector3.right);
            axisA.Normalize();
            Vector3 axisB = Vector3.Cross(localUp, axisA).normalized;

            GetProjectionRange(sourceVertices, axisA, out float minA, out float maxA);
            GetProjectionRange(sourceVertices, axisB, out float minB, out float maxB);

            float rangeA = Mathf.Max(0.0001f, maxA - minA);
            float rangeB = Mathf.Max(0.0001f, maxB - minB);

            for (int index = 0; index < sourceVertices.Length; index++)
            {
                Vector3 vertex = sourceVertices[index];
                float u = Mathf.Clamp01(
                    (Vector3.Dot(vertex, axisA) - minA) / rangeA);
                float v = Mathf.Clamp01(
                    (Vector3.Dot(vertex, axisB) - minB) / rangeB);

                float weight =
                    Mathf.Sin(Mathf.PI * u) *
                    Mathf.Sin(Mathf.PI * v);

                wetCollisionVertices[index] =
                    vertex - localUp * wetSagMeters * Mathf.Max(0f, weight);
            }

            runtimeCollisionMesh.vertices =
                initialState == DryingPaperState.Wet
                    ? wetCollisionVertices
                    : dryCollisionVertices;
            runtimeCollisionMesh.RecalculateBounds();
            ApplyRuntimeCollisionMesh();

            coarseCollisionVertexCount = sourceVertices.Length;
            coarseCollisionStatesConfigured = true;
            ApplyCollisionBlend(GetDryBlend01());
            return true;
        }

        private static void GetProjectionRange(
            Vector3[] vertices,
            Vector3 axis,
            out float min,
            out float max)
        {
            min = float.PositiveInfinity;
            max = float.NegativeInfinity;
            for (int index = 0; index < vertices.Length; index++)
            {
                float value = Vector3.Dot(vertices[index], axis);
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }
        }

        private void Awake()
        {
            CacheBlendShapes();
            if (wetSagMeters > 0f)
                BuildRuntimeCollisionStates();
            ApplyImmediate(initialState);
        }

        private void OnEnable() => RegisterCollider();
        private void OnDisable() => UnregisterCollider();

        private void OnDestroy()
        {
            DestroyRuntimeCollisionMesh();
        }

        private void DestroyRuntimeCollisionMesh()
        {
            if (runtimeCollisionMesh == null)
                return;

            if (Application.isPlaying)
                Destroy(runtimeCollisionMesh);
            else
                DestroyImmediate(runtimeCollisionMesh);

            runtimeCollisionMesh = null;
        }

        private void CacheBlendShapes()
        {
            wetBlendShape = -1;
            dryBlendShape = -1;
            Mesh mesh = visualRenderer != null ? visualRenderer.sharedMesh : null;
            if (mesh == null)
                return;
            wetBlendShape = mesh.GetBlendShapeIndex("WET_SAG");
            dryBlendShape = mesh.GetBlendShapeIndex("DRY_TAUT");
        }

        private void RegisterCollider()
        {
            if (collisionProxy == null)
                return;
            ColliderRegistry[collisionProxy] = this;
        }

        private void UnregisterCollider()
        {
            if (collisionProxy != null &&
                ColliderRegistry.TryGetValue(collisionProxy, out DryingPaperSurface current) &&
                current == this)
            {
                ColliderRegistry.Remove(collisionProxy);
            }
        }

        public static bool TryHandleControllerHit(FigureMotor motor, ControllerColliderHit hit)
        {
            if (motor == null || hit == null || hit.collider == null)
                return false;
            if (!ColliderRegistry.TryGetValue(hit.collider, out DryingPaperSurface surface) || surface == null)
                return false;
            if (hit.normal.y < 0.30f)
                return false;
            motor.RefreshEnvironmentMovementMultiplier(surface.CurrentSpeedMultiplier, 0.16f);
            return true;
        }

        public bool CanPainterToggle(out string reason)
        {
            reason = string.Empty;

            if (state == DryingPaperState.Transitioning)
            {
                reason = "Drying Paper is already transitioning";
                return false;
            }

            if (visualRenderer == null || collisionProxy == null)
            {
                reason = "Drying Paper binding incomplete";
                return false;
            }

            if (wetBlendShape < 0 || dryBlendShape < 0)
            {
                reason = "WET_SAG / DRY_TAUT blend shapes unresolved";
                return false;
            }

            if (!coarseCollisionStatesConfigured)
            {
                reason = "Coarse wet/dry collision policy is not configured";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Shared Painter interaction has already shown the authored 1.5 s
        /// warning before this call. The actual paper interpolation starts here.
        /// </summary>
        public bool TryPainterToggleAfterExternalTelegraph()
        {
            if (!CanPainterToggle(out _))
                return false;

            bool makeWet = state == DryingPaperState.Dry;
            SetWet(makeWet, immediate: false);
            return true;
        }

        public void SetWet(bool wet, bool immediate = false)
        {
            DryingPaperState target = wet ? DryingPaperState.Wet : DryingPaperState.Dry;
            if (immediate)
            {
                ApplyImmediate(target);
                return;
            }

            float current = GetDryBlend01();
            transitionStart = current;
            transitionTarget = wet ? 0f : 1f;
            transitionElapsed = 0f;
            state = DryingPaperState.Transitioning;
        }

        private void Update()
        {
            if (state != DryingPaperState.Transitioning)
                return;

            transitionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(transitionElapsed / transitionDuration);
            float smooth = t * t * (3f - 2f * t);
            float dry01 = Mathf.Lerp(transitionStart, transitionTarget, smooth);
            ApplyBlend(dry01);

            if (t >= 1f)
                ApplyImmediate(
                    transitionTarget >= 0.5f
                        ? DryingPaperState.Dry
                        : DryingPaperState.Wet);
        }

        private float GetDryBlend01()
        {
            if (visualRenderer == null || dryBlendShape < 0)
                return state == DryingPaperState.Dry ? 1f : 0f;
            return Mathf.Clamp01(
                visualRenderer.GetBlendShapeWeight(dryBlendShape) / 100f);
        }

        private void ApplyBlend(float dry01)
        {
            ApplyCollisionBlend(dry01);

            if (visualRenderer == null)
                return;

            if (wetBlendShape >= 0)
                visualRenderer.SetBlendShapeWeight(
                    wetBlendShape,
                    (1f - dry01) * 100f);
            if (dryBlendShape >= 0)
                visualRenderer.SetBlendShapeWeight(
                    dryBlendShape,
                    dry01 * 100f);

            if (visualRenderer.sharedMaterials.Length > 0)
            {
                Material chosen = dry01 >= 0.5f ? dryMaterial : wetMaterial;
                if (chosen != null)
                {
                    Material[] mats = visualRenderer.sharedMaterials;
                    mats[0] = chosen;
                    visualRenderer.sharedMaterials = mats;
                }
            }
        }

        private void ApplyCollisionBlend(float dry01)
        {
            if (!coarseCollisionStatesConfigured ||
                runtimeCollisionMesh == null ||
                dryCollisionVertices == null ||
                wetCollisionVertices == null ||
                blendedCollisionVertices == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(dry01);
            for (int index = 0; index < blendedCollisionVertices.Length; index++)
            {
                blendedCollisionVertices[index] = Vector3.Lerp(
                    wetCollisionVertices[index],
                    dryCollisionVertices[index],
                    clamped);
            }

            runtimeCollisionMesh.vertices = blendedCollisionVertices;
            runtimeCollisionMesh.RecalculateBounds();
            ApplyRuntimeCollisionMesh();
        }

        private void ApplyRuntimeCollisionMesh()
        {
            if (collisionProxy == null || runtimeCollisionMesh == null)
                return;

            collisionProxy.sharedMesh = null;
            collisionProxy.sharedMesh = runtimeCollisionMesh;
        }

        private void ApplyImmediate(DryingPaperState target)
        {
            state = target;
            transitionElapsed = 0f;
            transitionStart = transitionTarget =
                target == DryingPaperState.Dry ? 1f : 0f;
            ApplyBlend(transitionTarget);
        }

        public void ResetLivingGalleryState() => ApplyImmediate(initialState);
    }
}
