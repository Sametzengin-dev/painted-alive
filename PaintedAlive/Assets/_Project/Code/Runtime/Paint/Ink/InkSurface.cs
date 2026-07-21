using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint.Ink
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class InkSurface : MonoBehaviour
    {
        private static readonly List<InkSurface> ActiveSurfaceList = new();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        [SerializeField]
        private Renderer surfaceRenderer;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private float inkAmount;

        [SerializeField]
        private float currentRadius;

        [SerializeField, Range(0f, 1f)]
        private float wetness;

        [SerializeField]
        private Vector3 surfaceNormal = Vector3.up;

        [SerializeField]
        private bool initialized;

        private MaterialPropertyBlock propertyBlock;
        private Vector3 baseLocalScale;

        public static IReadOnlyList<InkSurface> ActiveSurfaces => ActiveSurfaceList;
        public float InkAmount => inkAmount;
        public float CurrentRadius => currentRadius;
        public float Wetness => wetness;
        public Vector3 SurfaceNormal => surfaceNormal;
        public bool IsInitialized => initialized;
        public Bounds WorldBounds => surfaceRenderer != null
            ? surfaceRenderer.bounds
            : new Bounds(transform.position, Vector3.one * currentRadius * 2f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            ActiveSurfaceList.Clear();
        }

        private void Awake()
        {
            surfaceRenderer ??= GetComponent<Renderer>();
            baseLocalScale = transform.localScale;

            if (baseLocalScale.sqrMagnitude < 0.001f)
            {
                baseLocalScale = Vector3.one;
            }
        }

        private void OnEnable()
        {
            if (!ActiveSurfaceList.Contains(this))
            {
                ActiveSurfaceList.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveSurfaceList.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveSurfaceList.Remove(this);
        }

        public void Initialize(
            float radius,
            float amount,
            float initialWetness,
            Vector3 normal)
        {
            surfaceRenderer ??= GetComponent<Renderer>();

            if (baseLocalScale.sqrMagnitude < 0.001f)
            {
                baseLocalScale = transform.localScale;
            }

            currentRadius = Mathf.Max(0.1f, radius);
            inkAmount = Mathf.Max(0f, amount);
            wetness = Mathf.Clamp01(initialWetness);
            surfaceNormal = normal.sqrMagnitude > 0.001f
                ? normal.normalized
                : Vector3.up;
            initialized = true;
            RefreshScale();
            RefreshVisual();
        }

        public void ApplyWatercolorExpansion(
            float expansion,
            float maximumRadius,
            float addedInkAmount)
        {
            if (!initialized || expansion <= 0f)
            {
                return;
            }

            currentRadius = Mathf.Min(
                Mathf.Max(currentRadius, 0.1f) + expansion,
                Mathf.Max(currentRadius, maximumRadius));
            inkAmount += Mathf.Max(0f, addedInkAmount);
            wetness = Mathf.Clamp01(wetness + expansion * 0.12f);
            RefreshScale();
            RefreshVisual();
        }

        private void RefreshScale()
        {
            float diameter = currentRadius * 2f;
            transform.localScale = new Vector3(
                baseLocalScale.x * diameter,
                baseLocalScale.y,
                baseLocalScale.z * diameter);
        }

        private void RefreshVisual()
        {
            if (surfaceRenderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            surfaceRenderer.GetPropertyBlock(propertyBlock);
            Color inkColor = Color.Lerp(
                new Color(0.012f, 0.01f, 0.018f, 0.96f),
                new Color(0.035f, 0.025f, 0.055f, 1f),
                wetness);
            propertyBlock.SetColor(BaseColorId, inkColor);
            propertyBlock.SetColor(ColorId, inkColor);
            propertyBlock.SetFloat(SmoothnessId, Mathf.Lerp(0.45f, 0.92f, wetness));
            surfaceRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
