using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    [DisallowMultipleComponent]
    public sealed class PerspectiveTraversalBridge : MonoBehaviour,
        IPalimpsestResettable
    {
        [SerializeField] private Transform connectionRoot;
        [SerializeField] private Renderer[] connectionRenderers;
        [SerializeField] private Collider alignedCollider;
        [SerializeField] private Collider misalignedCollider;
        [SerializeField] private Transform framePivot;
        [SerializeField] private float authoredMisalignedOffset = 5f;
        [SerializeField] private Vector3 alignedWorldPosition;
        [SerializeField] private Quaternion alignedWorldRotation = Quaternion.identity;
        [SerializeField, Range(0f, 1f)] private float alignmentProgress;
        [SerializeField] private bool traversable;

        public bool Traversable => traversable;
        public float AlignmentProgress => alignmentProgress;

        public void Configure(
            Transform root,
            Collider aligned,
            Collider misaligned,
            Transform pivot,
            float offsetMetres)
        {
            connectionRoot = root;
            alignedCollider = aligned;
            misalignedCollider = misaligned;
            framePivot = pivot;
            authoredMisalignedOffset = Mathf.Max(0f, offsetMetres);

            if (connectionRoot != null)
            {
                alignedWorldPosition = connectionRoot.position;
                alignedWorldRotation = connectionRoot.rotation;
                connectionRenderers = connectionRoot.GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                connectionRenderers = System.Array.Empty<Renderer>();
            }

            SetAlignmentProgress(0f);
            SetTraversable(false);
        }

        public void SetAlignmentProgress(float progress)
        {
            alignmentProgress = Mathf.Clamp01(progress);
            if (connectionRoot == null)
                return;

            Vector3 offsetDirection = framePivot != null
                ? framePivot.right
                : Vector3.right;
            Vector3 offset = offsetDirection * authoredMisalignedOffset * (1f - alignmentProgress);
            connectionRoot.SetPositionAndRotation(
                alignedWorldPosition + offset,
                alignedWorldRotation);
        }

        public void SetTraversable(bool value)
        {
            traversable = value;
            if (connectionRenderers != null)
            {
                foreach (Renderer renderer in connectionRenderers)
                {
                    if (renderer != null)
                        renderer.enabled = value;
                }
            }

            if (alignedCollider != null)
                alignedCollider.enabled = value;
            if (misalignedCollider != null)
                misalignedCollider.enabled = !value;
        }

        public void ResetPalimpsestState()
        {
            SetAlignmentProgress(0f);
            SetTraversable(false);
        }
    }
}
