using UnityEngine;

namespace PaintedAlive.Core.Prototypes
{
    [RequireComponent(typeof(Collider))]
    public sealed class PrototypeFinishTrigger : MonoBehaviour
    {
        [SerializeField]
        private PrototypeMatchController matchController;

        private void Reset()
        {
            Collider triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            matchController?.NotifyFigureReachedExit();
        }
    }
}
