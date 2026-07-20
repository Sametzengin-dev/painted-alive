using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint.Sponge
{
    [DisallowMultipleComponent]
    public sealed class SpongeAbsorbablePaintSource :
        MonoBehaviour,
        ISpongeAbsorbableSource
    {
        private static readonly List<SpongeAbsorbablePaintSource>
            ActiveSourceList = new();

        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        [Header("Paint Storage")]
        [SerializeField, Min(1f)]
        private float maximumAmount = 100f;

        [SerializeField, Min(0f)]
        private float availableAmount = 65f;

        [SerializeField]
        private Color paintColor =
            new Color(0.12f, 0.48f, 0.92f, 1f);

        [SerializeField, Range(0f, 1f)]
        private float instability;

        [Header("Visual")]
        [SerializeField]
        private Renderer targetRenderer;

        [SerializeField]
        private Collider targetCollider;

        [SerializeField]
        private bool scaleWithAmount = true;

        [SerializeField]
        private bool destroyWhenEmpty = true;

        private MaterialPropertyBlock propertyBlock;
        private Vector3 fullScale;

        public bool CanAbsorb =>
            isActiveAndEnabled && availableAmount > 0.001f;

        public float AvailableAmount => availableAmount;
        public Color PaintColor => paintColor;
        public float Instability => instability;

        public static IReadOnlyList<SpongeAbsorbablePaintSource>
            ActiveSources => ActiveSourceList;

        public Vector3 InteractionPoint =>
            targetCollider != null
                ? targetCollider.bounds.center
                : transform.position;

        public float InteractionRadius
        {
            get
            {
                if (targetCollider == null)
                {
                    return 0.25f;
                }

                Vector3 extents =
                    targetCollider.bounds.extents;

                return Mathf.Max(
                    0.1f,
                    Mathf.Max(
                        extents.x,
                        Mathf.Max(extents.y, extents.z)));
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveSources()
        {
            ActiveSourceList.Clear();
        }

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetCollider == null)
            {
                targetCollider = GetComponentInChildren<Collider>();
            }

            fullScale = transform.localScale;
            propertyBlock = new MaterialPropertyBlock();
            ClampState();
            RefreshVisual();
        }

        private void OnEnable()
        {
            if (!ActiveSourceList.Contains(this))
            {
                ActiveSourceList.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveSourceList.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveSourceList.Remove(this);
        }

        public void Initialize(
            float amount,
            Color color,
            float sourceInstability)
        {
            availableAmount =
                Mathf.Clamp(amount, 0f, maximumAmount);
            paintColor = color;
            paintColor.a = 1f;
            instability = Mathf.Clamp01(sourceInstability);

            if (fullScale.sqrMagnitude <= 0.0001f)
            {
                fullScale = transform.localScale;
            }

            RefreshVisual();
        }

        public float Absorb(float requestedAmount)
        {
            float absorbed =
                Mathf.Min(
                    Mathf.Max(0f, requestedAmount),
                    availableAmount);

            if (absorbed <= 0f)
            {
                return 0f;
            }

            availableAmount -= absorbed;
            RefreshVisual();

            if (availableAmount <= 0.001f)
            {
                availableAmount = 0f;

                if (destroyWhenEmpty)
                {
                    Destroy(gameObject);
                }
                else if (targetCollider != null)
                {
                    targetCollider.enabled = false;
                }
            }

            return absorbed;
        }

        private void RefreshVisual()
        {
            float normalizedAmount =
                maximumAmount > 0f
                    ? Mathf.Clamp01(
                        availableAmount / maximumAmount)
                    : 0f;

            if (scaleWithAmount &&
                fullScale.sqrMagnitude > 0.0001f)
            {
                float planarScale =
                    Mathf.Lerp(
                        0.3f,
                        1f,
                        Mathf.Sqrt(normalizedAmount));

                transform.localScale =
                    new Vector3(
                        fullScale.x * planarScale,
                        fullScale.y,
                        fullScale.z * planarScale);
            }

            if (targetRenderer == null)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            targetRenderer.GetPropertyBlock(propertyBlock);

            Color displayColor = paintColor;
            displayColor.a =
                Mathf.Lerp(0.25f, 0.9f, normalizedAmount);

            propertyBlock.SetColor(BaseColorId, displayColor);
            propertyBlock.SetColor(ColorId, displayColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void OnValidate()
        {
            ClampState();
        }

        private void ClampState()
        {
            maximumAmount = Mathf.Max(1f, maximumAmount);
            availableAmount =
                Mathf.Clamp(
                    availableAmount,
                    0f,
                    maximumAmount);
            instability = Mathf.Clamp01(instability);
        }
    }
}
