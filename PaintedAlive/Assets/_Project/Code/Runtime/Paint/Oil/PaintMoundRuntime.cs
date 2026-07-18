using PaintedAlive.Painters;
using UnityEngine;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PaintMoundRuntime : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int SmoothnessId =
            Shader.PropertyToID("_Smoothness");

        private PainterPaintMoundConfig config;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private Rigidbody body;
        private Mesh generatedMesh;
        private Material wetMaterial;
        private Material dryMaterial;
        private MaterialPropertyBlock propertyBlock;

        private float growthElapsed;
        private float lifecycleElapsed;
        private bool initialized;

        public OilStrokeState State { get; private set; }
        public float Radius { get; private set; }
        public float Height { get; private set; }
        public float ChargeNormalized { get; private set; }

        public bool IsFullyGrown =>
            initialized &&
            growthElapsed >= config.GrowthDuration;

        public bool IsActiveForBudget =>
            initialized && State != OilStrokeState.Dry;

        public void Initialize(
            PainterPaintMoundConfig moundConfig,
            Material initialWetMaterial,
            Material finalDryMaterial,
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            float chargeNormalized)
        {
            config = moundConfig;

            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(PaintMoundRuntime)} requires a config.",
                    this);

                enabled = false;
                return;
            }

            wetMaterial = initialWetMaterial;
            dryMaterial = finalDryMaterial;
            ChargeNormalized = Mathf.Clamp01(chargeNormalized);
            Radius = config.GetRadius(ChargeNormalized);
            Height = config.GetHeight(ChargeNormalized);

            surfaceNormal =
                surfaceNormal.sqrMagnitude > 0.001f
                    ? surfaceNormal.normalized
                    : Vector3.up;

            transform.SetPositionAndRotation(
                surfacePoint +
                surfaceNormal * config.SurfaceOffset,
                Quaternion.FromToRotation(
                    Vector3.up,
                    surfaceNormal));

            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
            body = GetComponent<Rigidbody>();

            generatedMesh =
                PaintMoundMeshUtility.CreateDomeMesh(
                    Radius,
                    Height,
                    config.RadialSegments,
                    config.VerticalRings,
                    $"{name}_RuntimeMesh");

            meshFilter.sharedMesh = generatedMesh;
            meshCollider.sharedMesh = generatedMesh;
            meshCollider.convex = true;

            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousSpeculative;

            meshRenderer.sharedMaterial =
                wetMaterial != null
                    ? wetMaterial
                    : dryMaterial;

            propertyBlock = new MaterialPropertyBlock();

            growthElapsed = 0f;
            lifecycleElapsed = 0f;
            State = OilStrokeState.Wet;
            initialized = true;

            transform.localScale = Vector3.one * 0.03f;
            ApplyLifecycleVisual(0f);
        }

        private void Update()
        {
            if (!initialized || config == null)
                return;

            if (growthElapsed < config.GrowthDuration)
            {
                growthElapsed += Time.deltaTime;

                float growth = Mathf.SmoothStep(
                    0.03f,
                    1f,
                    Mathf.Clamp01(
                        growthElapsed /
                        config.GrowthDuration));

                transform.localScale = Vector3.one * growth;
                return;
            }

            transform.localScale = Vector3.one;
            lifecycleElapsed += Time.deltaTime;

            float wetEnd = config.WetDuration;
            float dryEnd = wetEnd + config.DryingDuration;

            if (lifecycleElapsed < wetEnd)
            {
                State = OilStrokeState.Wet;
                ApplyLifecycleVisual(0f);
                return;
            }

            if (config.DryingDuration > 0f &&
                lifecycleElapsed < dryEnd)
            {
                State = OilStrokeState.Drying;

                ApplyLifecycleVisual(
                    Mathf.InverseLerp(
                        wetEnd,
                        dryEnd,
                        lifecycleElapsed));

                return;
            }

            State = OilStrokeState.Dry;
            ApplyLifecycleVisual(1f);
        }

        public bool TryReleaseForRolling(Vector3 impulse)
        {
            if (!initialized || body == null || !body.isKinematic)
                return false;

            transform.localScale = Vector3.one;
            body.isKinematic = false;
            body.useGravity = true;
            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;
            body.AddForce(impulse, ForceMode.Impulse);

            return true;
        }

        private void ApplyLifecycleVisual(float dryingProgress)
        {
            if (meshRenderer == null)
                return;

            Material fallbackWet =
                wetMaterial != null ? wetMaterial : dryMaterial;

            Material fallbackDry =
                dryMaterial != null ? dryMaterial : wetMaterial;

            if (fallbackWet == null || fallbackDry == null)
                return;

            if (State == OilStrokeState.Dry)
            {
                meshRenderer.SetPropertyBlock(null);
                meshRenderer.sharedMaterial = fallbackDry;
                return;
            }

            meshRenderer.sharedMaterial = fallbackWet;

            if (State == OilStrokeState.Wet)
            {
                meshRenderer.SetPropertyBlock(null);
                return;
            }

            propertyBlock.Clear();

            propertyBlock.SetColor(
                BaseColorId,
                Color.Lerp(
                    GetMaterialColor(fallbackWet),
                    GetMaterialColor(fallbackDry),
                    dryingProgress));

            propertyBlock.SetFloat(
                SmoothnessId,
                Mathf.Lerp(
                    GetMaterialSmoothness(fallbackWet),
                    GetMaterialSmoothness(fallbackDry),
                    dryingProgress));

            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private static Color GetMaterialColor(Material material)
        {
            return material.HasProperty(BaseColorId)
                ? material.GetColor(BaseColorId)
                : Color.white;
        }

        private static float GetMaterialSmoothness(Material material)
        {
            return material.HasProperty(SmoothnessId)
                ? material.GetFloat(SmoothnessId)
                : 0.5f;
        }

        private void OnDestroy()
        {
            if (generatedMesh == null)
                return;

            if (Application.isPlaying)
                Destroy(generatedMesh);
            else
                DestroyImmediate(generatedMesh);
        }
    }
}
