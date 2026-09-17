using PaintedAlive.Figures;
using PaintedAlive.Figures.Tools;
using UnityEngine;

namespace PaintedAlive.Paint.Ink.Thief
{
    [DisallowMultipleComponent]
    public sealed class InkStolenToolPickup : MonoBehaviour
    {
        [SerializeField]
        private FigureToolLoadoutController ownerLoadout;

        [SerializeField]
        private FigureToolId toolId;

        [SerializeField]
        private float autoReturnTime = 12f;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private bool initialized;

        [SerializeField]
        private bool collected;

        private float spawnedAt;
        private Vector3 visualBasePosition;
        private Transform visualRoot;

        public FigureToolId ToolId => toolId;
        public bool IsCollected => collected;

        public static bool TrySpawn(
            FigureToolLoadoutController loadout,
            FigureToolId tool,
            Object currentTheftOwner,
            Vector3 position,
            float autoReturnSeconds,
            out InkStolenToolPickup pickup)
        {
            pickup = null;

            if (loadout == null || currentTheftOwner == null)
            {
                return false;
            }

            var root = new GameObject(
                $"StolenToolPickup_{tool}");
            root.transform.position = position;

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.55f;

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            pickup = root.AddComponent<InkStolenToolPickup>();

            if (!loadout.TryTransferStolenTool(
                    currentTheftOwner,
                    pickup))
            {
                Object.Destroy(root);
                pickup = null;
                return false;
            }

            pickup.Configure(
                loadout,
                tool,
                autoReturnSeconds);
            return true;
        }

        private void Update()
        {
            if (!initialized || collected)
            {
                return;
            }

            if (ownerLoadout == null)
            {
                collected = true;
                Destroy(gameObject);
                return;
            }

            float age = Time.time - spawnedAt;

            if (age >= autoReturnTime)
            {
                collected = true;
                ownerLoadout.TryReturnStolenTool(
                    this,
                    "Stolen tool auto-return timeout");
                Destroy(gameObject);
                return;
            }

            if (visualRoot != null)
            {
                float bob = Mathf.Sin(Time.time * 4.5f) * 0.06f;
                visualRoot.localPosition =
                    visualBasePosition + Vector3.up * bob;
                visualRoot.Rotate(
                    Vector3.up,
                    80f * Time.deltaTime,
                    Space.Self);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized || collected || other == null)
            {
                return;
            }

            FigureMotor figure =
                other.GetComponentInParent<FigureMotor>();

            if (figure == null || !figure.isActiveAndEnabled)
            {
                return;
            }

            collected = true;
            bool returned = ownerLoadout != null &&
                ownerLoadout.TryReturnStolenTool(
                    this,
                    $"Recovered by {figure.name}");

            if (returned)
            {
                Debug.Log(
                    "[M55.3 Boya Hırsızı] Çalınan " +
                    $"{FigureToolLoadoutController.GetDisplayName(toolId)} " +
                    "bir Figür tarafından geri alındı.",
                    figure);
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (!initialized || collected || ownerLoadout == null)
            {
                return;
            }

            ownerLoadout.TryReturnStolenTool(
                this,
                "Stolen-tool pickup destroyed; safety return");
        }

        private void Configure(
            FigureToolLoadoutController loadout,
            FigureToolId tool,
            float autoReturnSeconds)
        {
            ownerLoadout = loadout;
            toolId = tool;
            autoReturnTime = Mathf.Max(2f, autoReturnSeconds);
            spawnedAt = Time.time;
            initialized = true;
            collected = false;

            GameObject visual = InkToolProxyVisualUtility.CreateProxy(
                transform,
                tool,
                "ToolVisual",
                Vector3.up * 0.35f,
                1.35f);
            visualRoot = visual.transform;
            visualBasePosition = visualRoot.localPosition;
        }
    }
}
