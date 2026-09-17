using PaintedAlive.MatchFlow;
using UnityEngine;

namespace PaintedAlive.Environment.Atelier
{
    /// <summary>
    /// Owns one environmental M13 WatercolorFlowSurface template and respawns
    /// it whenever the M54 core-match reset counter advances. This keeps the
    /// map-authored watercolor compatible with Sponge absorption without
    /// requiring changes to the existing M54.1 reset coordinator.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AtelierWatercolorFlowZone : MonoBehaviour
    {
        [Header("Authoring")]
        [SerializeField] private string zoneId;
        [SerializeField] private GameObject inactiveFlowTemplate;
        [SerializeField] private PrototypeCoreMatchController matchController;
        [SerializeField] private bool spawnOnStart = true;

        [Header("Runtime - Read Only")]
        [SerializeField] private GameObject liveFlowInstance;
        [SerializeField] private int lastObservedResetCount = -1;
        [SerializeField] private int spawnCount;

        public string ZoneId => zoneId;
        public GameObject InactiveFlowTemplate => inactiveFlowTemplate;
        public GameObject LiveFlowInstance => liveFlowInstance;
        public int SpawnCount => spawnCount;
        public bool UsesExistingM13Watercolor => true;
        public bool AddsNewFigureMovementSystem => false;

        public void Configure(
            string configuredZoneId,
            GameObject configuredTemplate,
            PrototypeCoreMatchController configuredMatchController)
        {
            zoneId = configuredZoneId;
            inactiveFlowTemplate = configuredTemplate;
            matchController = configuredMatchController;

            if (inactiveFlowTemplate != null)
            {
                inactiveFlowTemplate.SetActive(false);
            }
        }

        private void Awake()
        {
            if (inactiveFlowTemplate != null)
            {
                inactiveFlowTemplate.SetActive(false);
            }

            if (matchController == null)
            {
                matchController = FindFirstObjectByType<PrototypeCoreMatchController>();
            }
        }

        private void Start()
        {
            lastObservedResetCount = matchController != null
                ? matchController.ResetCount
                : -1;

            if (spawnOnStart)
            {
                SpawnFreshFlow();
            }
        }

        private void Update()
        {
            if (matchController == null)
            {
                matchController = FindFirstObjectByType<PrototypeCoreMatchController>();
                if (matchController == null)
                {
                    return;
                }

                lastObservedResetCount = matchController.ResetCount;
                return;
            }

            if (matchController.ResetCount == lastObservedResetCount)
            {
                return;
            }

            lastObservedResetCount = matchController.ResetCount;
            SpawnFreshFlow();
        }

        [ContextMenu("Development/Respawn Environmental Watercolor")]
        public void SpawnFreshFlow()
        {
            if (inactiveFlowTemplate == null)
            {
                return;
            }

            if (liveFlowInstance != null)
            {
                Destroy(liveFlowInstance);
            }

            liveFlowInstance = Instantiate(inactiveFlowTemplate, transform);
            liveFlowInstance.name = $"FLOW_Runtime_{zoneId}";
            liveFlowInstance.transform.localPosition = Vector3.zero;
            liveFlowInstance.transform.localRotation = Quaternion.identity;
            liveFlowInstance.transform.localScale = Vector3.one;
            liveFlowInstance.SetActive(true);
            spawnCount++;
        }
    }
}
