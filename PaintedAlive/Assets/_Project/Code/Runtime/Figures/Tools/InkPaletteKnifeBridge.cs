using PaintedAlive.Figures;
using PaintedAlive.Paint.Ink;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PaletteKnifeController))]
    public sealed class InkPaletteKnifeBridge : MonoBehaviour
    {
        private const int MaximumHits = 32;

        private readonly RaycastHit[] hits = new RaycastHit[MaximumHits];

        [Header("Existing Palette Knife Dependencies")]
        [SerializeField]
        private PaletteKnifeController paletteKnifeController;

        [SerializeField]
        private Camera outputCamera;

        [SerializeField]
        private Transform toolOrigin;

        [SerializeField]
        private InputActionReference useToolAction;

        [SerializeField]
        private FigureClarityState clarityState;

        [Header("Ink Counterplay")]
        [SerializeField]
        private InkCounterplayConfig counterplayConfig;

        [SerializeField]
        private LayerMask inkMask = Physics.DefaultRaycastLayers;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private InkGlyphType lastCutGlyph;

        [SerializeField]
        private bool lastCutDisabledGlyph;

        [SerializeField]
        private float lastRemainingDurability;

        [SerializeField]
        private int successfulCutCount;

        public InkGlyphType LastCutGlyph => lastCutGlyph;
        public bool LastCutDisabledGlyph => lastCutDisabledGlyph;
        public float LastRemainingDurability =>
            lastRemainingDurability;
        public int SuccessfulCutCount => successfulCutCount;

        private void Awake()
        {
            paletteKnifeController ??=
                GetComponent<PaletteKnifeController>();
            toolOrigin ??= transform;
            clarityState ??=
                GetComponentInParent<FigureClarityState>();

            if (outputCamera == null)
            {
                outputCamera = Camera.main;
            }

            if (paletteKnifeController == null ||
                outputCamera == null ||
                useToolAction == null ||
                counterplayConfig == null)
            {
                Debug.LogError(
                    $"{nameof(InkPaletteKnifeBridge)} on {name} requires " +
                    "Palette Knife, Camera, Input Action and Counterplay Config.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (paletteKnifeController == null ||
                !paletteKnifeController.isActiveAndEnabled ||
                useToolAction == null ||
                useToolAction.action == null ||
                !useToolAction.action.WasPressedThisFrame())
            {
                return;
            }

            if (clarityState != null &&
                !clarityState.CanUsePrimaryTool)
            {
                return;
            }

            TryCutInkGlyph();
        }

        private void TryCutInkGlyph()
        {
            Ray aimRay = outputCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.SphereCastNonAlloc(
                aimRay,
                counterplayConfig.PaletteKnifeCastRadius,
                hits,
                counterplayConfig.PaletteKnifeMaximumAimDistance,
                inkMask,
                QueryTriggerInteraction.Collide);
            InkGlyphHitZone bestZone = null;
            RaycastHit bestHit = default;
            float bestAimDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];

                if (hit.collider == null)
                {
                    continue;
                }

                InkGlyphHitZone zone =
                    hit.collider.GetComponentInParent<InkGlyphHitZone>();

                if (zone == null ||
                    !zone.IsActive ||
                    hit.distance >= bestAimDistance)
                {
                    continue;
                }

                bestZone = zone;
                bestHit = hit;
                bestAimDistance = hit.distance;
            }

            if (bestZone == null)
            {
                return;
            }

            Vector3 rangeOrigin = toolOrigin != null
                ? toolOrigin.position
                : transform.position;

            if (Vector3.Distance(rangeOrigin, bestHit.point) >
                counterplayConfig.PaletteKnifeReach)
            {
                return;
            }

            float efficiency = clarityState != null
                ? clarityState.ToolEfficiency
                : 1f;
            float damage =
                counterplayConfig.PaletteKnifeGlyphDamage * efficiency;

            if (!bestZone.TryApplyDamage(
                    damage,
                    out bool disabled,
                    out float remaining))
            {
                return;
            }

            lastCutGlyph = bestZone.GlyphType;
            lastCutDisabledGlyph = disabled;
            lastRemainingDurability = remaining;
            successfulCutCount++;

            Debug.DrawLine(
                rangeOrigin,
                bestHit.point,
                disabled ? Color.red : Color.yellow,
                0.8f);

            Debug.Log(
                "[M16 Ink Counterplay] Palet Bıçağı " +
                $"{bestZone.GlyphType} sembolüne {damage:F2} hasar verdi. " +
                $"Kalan={remaining:F2}, DevreDışı={disabled}.",
                bestZone.Creature);
        }
    }
}
