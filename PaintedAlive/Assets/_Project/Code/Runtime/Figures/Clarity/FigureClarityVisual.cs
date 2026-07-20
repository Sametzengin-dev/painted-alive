using UnityEngine;

namespace PaintedAlive.Figures
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FigureClarityVisual : MonoBehaviour
    {
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int SmoothnessId =
            Shader.PropertyToID("_Smoothness");

        [SerializeField] private FigureClarityState clarityState;

        [Header("Visuals")]
        [SerializeField] private GameObject normalVisual;
        [SerializeField] private GameObject stainVisual;
        [SerializeField] private Renderer normalRenderer;

        [Header("Colors")]
        [SerializeField] private Color cleanColor =
            new(0.82f, 0.79f, 0.75f, 1f);

        [SerializeField] private Color paintedColor =
            new(0.30f, 0.04f, 0.08f, 1f);

        [Header("Stain Collider")]
        [SerializeField, Min(0.1f)] private float stainHeight = 0.25f;
        [SerializeField, Min(0.1f)] private float stainRadius = 0.45f;

        private CharacterController characterController;
        private MaterialPropertyBlock propertyBlock;

        private float originalHeight;
        private float originalRadius;
        private Vector3 originalCenter;
        private Vector3 originalVisualScale;

        private bool isStain;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            propertyBlock = new MaterialPropertyBlock();

            originalHeight = characterController.height;
            originalRadius = characterController.radius;
            originalCenter = characterController.center;

            if (normalVisual != null)
                originalVisualScale = normalVisual.transform.localScale;
        }

        private void OnEnable()
        {
            if (clarityState == null)
                clarityState = GetComponent<FigureClarityState>();

            if (clarityState == null)
                return;

            clarityState.ClarityChanged += HandleClarityChanged;
            clarityState.LevelChanged += HandleLevelChanged;

            RefreshVisual();
        }

        private void OnDisable()
        {
            if (clarityState == null)
                return;

            clarityState.ClarityChanged -= HandleClarityChanged;
            clarityState.LevelChanged -= HandleLevelChanged;
        }

        private void Update()
        {
            if (clarityState == null || normalVisual == null || isStain)
                return;

            float wobble = clarityState.CurrentLevel switch
            {
                FigureClarityLevel.Distorted => 0.012f,
                FigureClarityLevel.Dissolving => 0.028f,
                _ => 0f
            };

            float x = Mathf.Sin(Time.time * 5.3f) * wobble;
            float z = Mathf.Cos(Time.time * 4.1f) * wobble;

            normalVisual.transform.localScale =
                originalVisualScale + new Vector3(x, -x * 0.5f, z);
        }

        private void HandleClarityChanged(
            float previous,
            float current)
        {
            RefreshMaterial();
        }

        private void HandleLevelChanged(
            FigureClarityLevel previous,
            FigureClarityLevel current)
        {
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (clarityState == null)
                return;

            bool shouldBeStain =
                clarityState.CurrentLevel == FigureClarityLevel.Stain;

            if (shouldBeStain != isStain)
            {
                isStain = shouldBeStain;
                ApplyForm();
            }

            RefreshMaterial();
        }

        private void ApplyForm()
        {
            if (normalVisual != null)
                normalVisual.SetActive(!isStain);

            if (stainVisual != null)
                stainVisual.SetActive(isStain);

            if (isStain)
            {
                characterController.height = stainHeight;
                characterController.radius = stainRadius;
                characterController.center =
                    new Vector3(0f, stainHeight * 0.5f, 0f);
            }
            else
            {
                characterController.height = originalHeight;
                characterController.radius = originalRadius;
                characterController.center = originalCenter;

                if (normalVisual != null)
                    normalVisual.transform.localScale = originalVisualScale;
            }
        }

        private void RefreshMaterial()
        {
            if (normalRenderer == null || clarityState == null)
                return;

            propertyBlock ??= new MaterialPropertyBlock();

            float clarity = clarityState.NormalizedClarity;
            Color color = Color.Lerp(paintedColor, cleanColor, clarity);

            normalRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetFloat(
                SmoothnessId,
                Mathf.Lerp(0.25f, 0.65f, clarity));

            normalRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
