using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Figures
{
    /// <summary>
    /// Presentation-only clarity feedback for the production Figure mesh.
    /// It observes FigureClarityState and never changes gameplay state,
    /// colliders, movement, authority, or shared materials.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FigureClarityMaterialMotionFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        private static readonly int SmoothnessId =
            Shader.PropertyToID("_Smoothness");

        private static readonly int GlossinessId =
            Shader.PropertyToID("_Glossiness");

        [Header("Dependencies")]
        [SerializeField]
        private FigureClarityState clarityState;

        [SerializeField]
        private Renderer[] productionRenderers =
            System.Array.Empty<Renderer>();

        [SerializeField]
        private Renderer stainRenderer;

        [SerializeField]
        private Transform transitionAnchor;

        [SerializeField]
        private ParticleSystem transitionParticlePrefab;

        [Header("Pigment Motion")]
        [SerializeField]
        private Color stainedTint =
            new(0.72f, 0.30f, 0.34f, 1f);

        [SerializeField]
        private Color distortedTint =
            new(0.48f, 0.08f, 0.14f, 1f);

        [SerializeField]
        private Color dissolvingTint =
            new(0.24f, 0.025f, 0.055f, 1f);

        [SerializeField, Min(0.02f)]
        private float responseSeconds = 0.2f;

        [SerializeField, Range(0f, 0.15f)]
        private float distortedPulse = 0.035f;

        [SerializeField, Range(0f, 0.2f)]
        private float dissolvingPulse = 0.075f;

        [Header("Stain Motion")]
        [SerializeField, Range(0f, 0.2f)]
        private float stainSpreadAmplitude = 0.065f;

        [SerializeField, Min(0.1f)]
        private float stainPulseSpeed = 2.8f;

        private readonly List<MaterialSlot> materialSlots = new();
        private MaterialPropertyBlock propertyBlock;
        private ParticleSystem transitionParticle;
        private Vector3 stainBaseScale;
        private float currentPigment;
        private float pigmentVelocity;
        private float pulsePhase;
        private bool hasStainBaseScale;

        public FigureClarityState ClarityState => clarityState;
        public int ProductionRendererCount =>
            productionRenderers != null
                ? productionRenderers.Length
                : 0;
        public bool HasStainRenderer => stainRenderer != null;
        public bool HasTransitionParticle =>
            transitionParticlePrefab != null;
        public bool IsConfigured =>
            clarityState != null &&
            ProductionRendererCount > 0 &&
            stainRenderer != null &&
            transitionAnchor != null &&
            transitionParticlePrefab != null;

        public void Configure(
            FigureClarityState targetClarityState,
            Renderer[] targetProductionRenderers,
            Renderer targetStainRenderer,
            Transform targetTransitionAnchor,
            ParticleSystem targetTransitionParticle)
        {
            bool wasActive = isActiveAndEnabled;

            if (wasActive && clarityState != null)
            {
                Unsubscribe();
            }

            if (wasActive)
            {
                RestoreMaterialProperties();
                RestoreStainScale();
            }

            clarityState = targetClarityState;
            productionRenderers = targetProductionRenderers ??
                System.Array.Empty<Renderer>();
            stainRenderer = targetStainRenderer;
            transitionAnchor = targetTransitionAnchor;
            transitionParticlePrefab = targetTransitionParticle;

            if (wasActive)
            {
                RebuildBindings();
                Subscribe();
                RefreshTargets(true);
            }
        }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            RebuildBindings();
            RefreshTargets(true);
        }

        private void OnEnable()
        {
            if (clarityState == null)
            {
                clarityState = GetComponent<FigureClarityState>();
            }

            Subscribe();
            RefreshTargets(true);
        }

        private void OnDisable()
        {
            Unsubscribe();
            RestoreMaterialProperties();
            RestoreStainScale();

            if (transitionParticle != null)
            {
                transitionParticle.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnDestroy()
        {
            if (transitionParticle == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(transitionParticle.gameObject);
            }
            else
            {
                DestroyImmediate(transitionParticle.gameObject);
            }
        }

        private void Update()
        {
            if (clarityState == null || materialSlots.Count == 0)
            {
                return;
            }

            float target = GetTargetPigment();
            currentPigment = Mathf.SmoothDamp(
                currentPigment,
                target,
                ref pigmentVelocity,
                responseSeconds);

            pulsePhase += Time.deltaTime * GetPulseSpeed();

            float animatedPigment = Mathf.Clamp01(
                currentPigment +
                Mathf.Sin(pulsePhase) * GetPulseAmplitude());

            ApplyMaterialMotion(animatedPigment);
        }

        private void LateUpdate()
        {
            if (!hasStainBaseScale || stainRenderer == null)
            {
                return;
            }

            if (clarityState == null ||
                clarityState.CurrentLevel != FigureClarityLevel.Stain)
            {
                RestoreStainScale();
                return;
            }

            float wave =
                Mathf.Sin(Time.time * stainPulseSpeed) *
                stainSpreadAmplitude;

            stainRenderer.transform.localScale = new Vector3(
                stainBaseScale.x * (1f + wave),
                stainBaseScale.y * (1f - wave * 0.35f),
                stainBaseScale.z * (1f - wave * 0.55f));
        }

        private void Subscribe()
        {
            if (clarityState == null)
            {
                return;
            }

            clarityState.ClarityChanged -= HandleClarityChanged;
            clarityState.LevelChanged -= HandleLevelChanged;
            clarityState.ClarityChanged += HandleClarityChanged;
            clarityState.LevelChanged += HandleLevelChanged;
        }

        private void Unsubscribe()
        {
            if (clarityState == null)
            {
                return;
            }

            clarityState.ClarityChanged -= HandleClarityChanged;
            clarityState.LevelChanged -= HandleLevelChanged;
        }

        private void HandleClarityChanged(float previous, float current)
        {
            RefreshTargets(false);
        }

        private void HandleLevelChanged(
            FigureClarityLevel previous,
            FigureClarityLevel current)
        {
            RefreshTargets(false);
            PlayTransition(current);
        }

        private void RefreshTargets(bool snap)
        {
            if (clarityState == null)
            {
                return;
            }

            if (snap)
            {
                currentPigment = GetTargetPigment();
                pigmentVelocity = 0f;
                ApplyMaterialMotion(currentPigment);
            }
        }

        private float GetTargetPigment()
        {
            float contamination =
                1f - clarityState.NormalizedClarity;

            float levelFloor = clarityState.CurrentLevel switch
            {
                FigureClarityLevel.Stained => 0.2f,
                FigureClarityLevel.Distorted => 0.5f,
                FigureClarityLevel.Dissolving => 0.82f,
                FigureClarityLevel.Stain => 1f,
                _ => 0f
            };

            return Mathf.Clamp01(
                Mathf.Max(contamination, levelFloor));
        }

        private float GetPulseAmplitude()
        {
            return clarityState.CurrentLevel switch
            {
                FigureClarityLevel.Distorted => distortedPulse,
                FigureClarityLevel.Dissolving => dissolvingPulse,
                _ => 0f
            };
        }

        private float GetPulseSpeed()
        {
            return clarityState.CurrentLevel switch
            {
                FigureClarityLevel.Distorted => 3.4f,
                FigureClarityLevel.Dissolving => 5.2f,
                _ => 1.5f
            };
        }

        private Color GetCurrentTint()
        {
            return clarityState.CurrentLevel switch
            {
                FigureClarityLevel.Stained => stainedTint,
                FigureClarityLevel.Distorted => distortedTint,
                FigureClarityLevel.Dissolving => dissolvingTint,
                FigureClarityLevel.Stain => dissolvingTint,
                _ => Color.white
            };
        }

        private void RebuildBindings()
        {
            materialSlots.Clear();
            propertyBlock ??= new MaterialPropertyBlock();

            if (productionRenderers != null)
            {
                for (int rendererIndex = 0;
                     rendererIndex < productionRenderers.Length;
                     rendererIndex++)
                {
                    Renderer target =
                        productionRenderers[rendererIndex];

                    if (target == null)
                    {
                        continue;
                    }

                    Material[] materials = target.sharedMaterials;

                    for (int materialIndex = 0;
                         materialIndex < materials.Length;
                         materialIndex++)
                    {
                        Material material = materials[materialIndex];

                        if (material == null)
                        {
                            continue;
                        }

                        materialSlots.Add(new MaterialSlot(
                            target,
                            materialIndex,
                            material));
                    }
                }
            }

            if (stainRenderer != null)
            {
                stainBaseScale =
                    stainRenderer.transform.localScale;
                hasStainBaseScale = true;
            }
            else
            {
                hasStainBaseScale = false;
            }
        }

        private void ApplyMaterialMotion(float pigment)
        {
            if (propertyBlock == null || clarityState == null)
            {
                return;
            }

            Color tint = GetCurrentTint();

            for (int i = 0; i < materialSlots.Count; i++)
            {
                MaterialSlot slot = materialSlots[i];

                if (slot.Renderer == null)
                {
                    continue;
                }

                propertyBlock.Clear();
                slot.Renderer.GetPropertyBlock(
                    propertyBlock,
                    slot.MaterialIndex);

                if (slot.HasBaseColor)
                {
                    propertyBlock.SetColor(
                        BaseColorId,
                        Tint(slot.BaseColor, tint, pigment));
                }

                if (slot.HasColor)
                {
                    propertyBlock.SetColor(
                        ColorId,
                        Tint(slot.LegacyColor, tint, pigment));
                }

                if (slot.HasSmoothness)
                {
                    propertyBlock.SetFloat(
                        SmoothnessId,
                        Mathf.Lerp(
                            slot.Smoothness,
                            Mathf.Max(slot.Smoothness, 0.72f),
                            pigment));
                }

                if (slot.HasGlossiness)
                {
                    propertyBlock.SetFloat(
                        GlossinessId,
                        Mathf.Lerp(
                            slot.Glossiness,
                            Mathf.Max(slot.Glossiness, 0.72f),
                            pigment));
                }

                slot.Renderer.SetPropertyBlock(
                    propertyBlock,
                    slot.MaterialIndex);
            }
        }

        private void RestoreMaterialProperties()
        {
            if (propertyBlock == null)
            {
                return;
            }

            for (int i = 0; i < materialSlots.Count; i++)
            {
                MaterialSlot slot = materialSlots[i];

                if (slot.Renderer == null)
                {
                    continue;
                }

                propertyBlock.Clear();
                slot.Renderer.GetPropertyBlock(
                    propertyBlock,
                    slot.MaterialIndex);

                if (slot.HasBaseColor)
                {
                    propertyBlock.SetColor(
                        BaseColorId,
                        slot.BaseColor);
                }

                if (slot.HasColor)
                {
                    propertyBlock.SetColor(
                        ColorId,
                        slot.LegacyColor);
                }

                if (slot.HasSmoothness)
                {
                    propertyBlock.SetFloat(
                        SmoothnessId,
                        slot.Smoothness);
                }

                if (slot.HasGlossiness)
                {
                    propertyBlock.SetFloat(
                        GlossinessId,
                        slot.Glossiness);
                }

                slot.Renderer.SetPropertyBlock(
                    propertyBlock,
                    slot.MaterialIndex);
            }
        }

        private void RestoreStainScale()
        {
            if (hasStainBaseScale && stainRenderer != null)
            {
                stainRenderer.transform.localScale = stainBaseScale;
            }
        }

        private void PlayTransition(FigureClarityLevel level)
        {
            if (transitionParticlePrefab == null ||
                transitionAnchor == null)
            {
                return;
            }

            if (transitionParticle == null)
            {
                transitionParticle = Instantiate(
                    transitionParticlePrefab,
                    transitionAnchor.position,
                    transitionAnchor.rotation);
                transitionParticle.name =
                    "M58_FigureClarityTransition_Runtime";
            }

            transitionParticle.transform.SetPositionAndRotation(
                transitionAnchor.position,
                transitionAnchor.rotation);

            ParticleSystem.MainModule main =
                transitionParticle.main;
            main.startColor = new ParticleSystem.MinMaxGradient(
                GetTransitionColor(level));

            transitionParticle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            transitionParticle.Play(true);
        }

        private Color GetTransitionColor(FigureClarityLevel level)
        {
            return level switch
            {
                FigureClarityLevel.Clean =>
                    new Color(0.85f, 0.95f, 0.92f, 1f),
                FigureClarityLevel.Stained => stainedTint,
                FigureClarityLevel.Distorted => distortedTint,
                FigureClarityLevel.Dissolving => dissolvingTint,
                FigureClarityLevel.Stain =>
                    new Color(0.04f, 0.14f, 0.17f, 1f),
                _ => Color.white
            };
        }

        private static Color Tint(
            Color source,
            Color tint,
            float strength)
        {
            Color multiplied = new Color(
                source.r * tint.r,
                source.g * tint.g,
                source.b * tint.b,
                source.a);

            Color result = Color.Lerp(
                source,
                multiplied,
                Mathf.Clamp01(strength));
            result.a = source.a;
            return result;
        }

        private void OnValidate()
        {
            responseSeconds = Mathf.Max(0.02f, responseSeconds);
            stainPulseSpeed = Mathf.Max(0.1f, stainPulseSpeed);
        }

        private sealed class MaterialSlot
        {
            public MaterialSlot(
                Renderer renderer,
                int materialIndex,
                Material material)
            {
                Renderer = renderer;
                MaterialIndex = materialIndex;

                HasBaseColor = material.HasProperty(BaseColorId);
                BaseColor = HasBaseColor
                    ? material.GetColor(BaseColorId)
                    : Color.white;

                HasColor = material.HasProperty(ColorId);
                LegacyColor = HasColor
                    ? material.GetColor(ColorId)
                    : UnityEngine.Color.white;

                HasSmoothness =
                    material.HasProperty(SmoothnessId);
                Smoothness = HasSmoothness
                    ? material.GetFloat(SmoothnessId)
                    : 0f;

                HasGlossiness =
                    material.HasProperty(GlossinessId);
                Glossiness = HasGlossiness
                    ? material.GetFloat(GlossinessId)
                    : 0f;
            }

            public Renderer Renderer { get; }
            public int MaterialIndex { get; }
            public bool HasBaseColor { get; }
            public Color BaseColor { get; }
            public bool HasColor { get; }
            public Color LegacyColor { get; }
            public bool HasSmoothness { get; }
            public float Smoothness { get; }
            public bool HasGlossiness { get; }
            public float Glossiness { get; }
        }
    }
}
