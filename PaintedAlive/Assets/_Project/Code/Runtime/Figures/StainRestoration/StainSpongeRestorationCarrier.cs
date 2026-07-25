using PaintedAlive.Figures.StainSupport;
using UnityEngine;

namespace PaintedAlive.Figures.StainRestoration
{
    [DefaultExecutionOrder(14700)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StainSpongeCarrier))]
    public sealed class StainSpongeRestorationCarrier :
        MonoBehaviour
    {
        [SerializeField]
        private StainSpongeCarrier carrier;

        [SerializeField]
        private StainSpongeRestorationConfig config;

        [SerializeField]
        private Renderer pigmentIndicator;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool hasCleanPigment;

        [SerializeField]
        private float restorationProgress;

        [SerializeField]
        private string lastResult =
            "Temiz pigment bekleniyor";

        private MaterialPropertyBlock propertyBlock;
        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId =
            Shader.PropertyToID("_Color");

        public bool HasCleanPigment => hasCleanPigment;
        public float RestorationProgress =>
            restorationProgress;
        public float NormalizedProgress =>
            config != null &&
            config.RestorationDuration > 0f
                ? Mathf.Clamp01(
                    restorationProgress /
                    config.RestorationDuration)
                : 0f;
        public string LastResult => lastResult;

        private void Awake()
        {
            carrier ??= GetComponent<StainSpongeCarrier>();
            propertyBlock = new MaterialPropertyBlock();

            if (carrier == null || config == null)
            {
                Debug.LogError(
                    "M30 restoration carrier requires " +
                    "StainSpongeCarrier and Config. " +
                    "Run M30 Setup again.",
                    this);
                enabled = false;
                return;
            }

            RefreshIndicator();
        }

        private void Update()
        {
            if (carrier == null || config == null)
            {
                return;
            }

            if (!carrier.HasPassenger ||
                carrier.Passenger == null ||
                carrier.Passenger.ClarityState == null ||
                carrier.Passenger.ClarityState.CurrentLevel !=
                    FigureClarityLevel.Stain)
            {
                ResetProgress();
                lastResult = hasCleanPigment
                    ? "Temiz pigment yüklü; Leke bekleniyor"
                    : "Temiz pigment bekleniyor";
                return;
            }

            if (!hasCleanPigment)
            {
                TryLoadNearestPigment();

                if (!hasCleanPigment)
                {
                    ResetProgress();
                    lastResult =
                        "Leke taşınıyor; temiz pigmente git";
                    return;
                }
            }

            StainRestorationSurface surface =
                FindNearestRestorationSurface();

            if (surface == null)
            {
                ResetProgress();
                lastResult =
                    "Temiz pigment yüklü; restorasyon " +
                    "yüzeyine git";
                return;
            }

            restorationProgress += Time.deltaTime;
            lastResult =
                $"Restore ediliyor %{Mathf.RoundToInt(NormalizedProgress * 100f)}";

            if (restorationProgress <
                config.RestorationDuration)
            {
                return;
            }

            TryCompleteRestoration();
        }

        public void Configure(
            StainSpongeCarrier targetCarrier,
            StainSpongeRestorationConfig targetConfig,
            Renderer targetIndicator)
        {
            carrier = targetCarrier;
            config = targetConfig;
            pigmentIndicator = targetIndicator;

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            RefreshIndicator();
        }

        [ContextMenu("Debug/Load Clean Pigment")]
        public void DebugLoadCleanPigment()
        {
            hasCleanPigment = true;
            lastResult = "Debug: temiz pigment yüklendi";
            RefreshIndicator();
        }

        private void TryLoadNearestPigment()
        {
            StainCleanPigmentSource best = null;
            float bestDistanceSquared =
                config.PigmentLoadRadius *
                config.PigmentLoadRadius;
            var sources =
                StainCleanPigmentSource.ActiveSources;

            for (int i = 0; i < sources.Count; i++)
            {
                StainCleanPigmentSource candidate =
                    sources[i];

                if (candidate == null ||
                    !candidate.HasPigment)
                {
                    continue;
                }

                float distanceSquared =
                    (candidate.InteractionPosition -
                     transform.position).sqrMagnitude;

                if (distanceSquared >
                    bestDistanceSquared)
                {
                    continue;
                }

                best = candidate;
                bestDistanceSquared = distanceSquared;
            }

            if (best == null || !best.TryTakeCharge())
            {
                return;
            }

            hasCleanPigment = true;
            restorationProgress = 0f;
            lastResult = "Temiz pigment süngere yüklendi";
            RefreshIndicator();
            Debug.Log(
                "[M30] Temiz pigment süngere yüklendi.",
                this);
        }

        private StainRestorationSurface
            FindNearestRestorationSurface()
        {
            StainRestorationSurface best = null;
            float bestDistanceSquared =
                config.RestorationRadius *
                config.RestorationRadius;
            var surfaces =
                StainRestorationSurface.ActiveSurfaces;

            for (int i = 0; i < surfaces.Count; i++)
            {
                StainRestorationSurface candidate =
                    surfaces[i];

                if (candidate == null ||
                    !candidate.CanRestore)
                {
                    continue;
                }

                float distanceSquared =
                    (candidate.RestorationPosition -
                     transform.position).sqrMagnitude;

                if (distanceSquared >
                    bestDistanceSquared)
                {
                    continue;
                }

                best = candidate;
                bestDistanceSquared = distanceSquared;
            }

            return best;
        }

        private void TryCompleteRestoration()
        {
            StainSpongeCarryController passenger =
                carrier != null ? carrier.Passenger : null;

            if (passenger == null)
            {
                ResetProgress();
                return;
            }

            if (!passenger.TryCompleteRestoration(
                    config.RestoredNormalizedClarity,
                    out string result))
            {
                restorationProgress =
                    Mathf.Min(
                        restorationProgress,
                        config.RestorationDuration);
                lastResult = result;
                return;
            }

            hasCleanPigment = false;
            restorationProgress = 0f;
            lastResult =
                "Restorasyon tamamlandı; Figür kısmi " +
                "Netlikle döndü";
            RefreshIndicator();
            Debug.Log("[M30] " + lastResult, this);
        }

        private void ResetProgress()
        {
            restorationProgress =
                Mathf.Clamp(
                    restorationProgress *
                    (config != null
                        ? config.InterruptedProgressRetention
                        : 0f),
                    0f,
                    config != null
                        ? config.RestorationDuration
                        : 0f);
        }

        private void RefreshIndicator()
        {
            if (pigmentIndicator == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            pigmentIndicator.GetPropertyBlock(propertyBlock);
            Color color = hasCleanPigment
                ? new Color(0.92f, 0.98f, 1f, 1f)
                : new Color(0.13f, 0.21f, 0.23f, 1f);

            if (pigmentIndicator.sharedMaterial != null &&
                pigmentIndicator.sharedMaterial.HasProperty(
                    BaseColorId))
            {
                propertyBlock.SetColor(BaseColorId, color);
            }

            if (pigmentIndicator.sharedMaterial != null &&
                pigmentIndicator.sharedMaterial.HasProperty(
                    ColorId))
            {
                propertyBlock.SetColor(ColorId, color);
            }

            pigmentIndicator.SetPropertyBlock(propertyBlock);
        }
    }
}
