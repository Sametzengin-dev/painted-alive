using UnityEngine;

namespace PaintedAlive.Figures
{
    [RequireComponent(typeof(Collider))]
    public sealed class CleanPigmentPickup : MonoBehaviour
    {
        [Header("Pickup")]
        [SerializeField, Min(1)] private int pigmentAmount = 1;

        [Header("Visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(0f)] private float rotationSpeed = 70f;
        [SerializeField, Min(0f)] private float bobHeight = 0.12f;
        [SerializeField, Min(0f)] private float bobSpeed = 2f;

        [Header("Runtime - Read Only")]
        [SerializeField] private bool isAvailable = true;

        private Collider triggerCollider;
        private Vector3 originalVisualPosition;
        private float animationOffset;

        public bool IsAvailable => isAvailable;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;

            if (visualRoot == null && transform.childCount > 0)
                visualRoot = transform.GetChild(0);

            if (visualRoot != null)
                originalVisualPosition = visualRoot.localPosition;

            animationOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (!isAvailable || visualRoot == null)
                return;

            visualRoot.Rotate(
                Vector3.up,
                rotationSpeed * Time.deltaTime,
                Space.Self);

            float bobOffset =
                Mathf.Sin(Time.time * bobSpeed + animationOffset) *
                bobHeight;

            visualRoot.localPosition =
                originalVisualPosition + Vector3.up * bobOffset;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isAvailable)
                return;

            FigureCleanPigmentInventory inventory =
                other.GetComponentInParent<FigureCleanPigmentInventory>();

            if (inventory == null)
                return;

            int addedAmount = inventory.AddPigment(pigmentAmount);

            // Envanter doluysa pickup yerde kalır.
            if (addedAmount <= 0)
                return;

            SetAvailable(false);
        }

        public void ResetPickup()
        {
            SetAvailable(true);

            if (visualRoot != null)
                visualRoot.localPosition = originalVisualPosition;
        }

        private void SetAvailable(bool available)
        {
            isAvailable = available;

            if (triggerCollider != null)
                triggerCollider.enabled = available;

            if (visualRoot != null)
                visualRoot.gameObject.SetActive(available);
        }
    }
}