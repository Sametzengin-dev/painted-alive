using UnityEngine;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class OilStrokeFragmentRuntime : MonoBehaviour
    {
        private Mesh ownedMesh;
        private Rigidbody body;
        private float settleDelay;
        private float lifetime;
        private float elapsed;
        private bool settled;

        public bool IsSettled => settled;

        public void Initialize(
            Mesh fragmentMesh,
            Material[] materials,
            MaterialPropertyBlock propertyBlock,
            float mass,
            float drag,
            float angularDrag,
            Vector3 impulse,
            Vector3 angularImpulse,
            float settleToKinematicDelay,
            float fragmentLifetime)
        {
            ownedMesh = fragmentMesh;
            settleDelay = Mathf.Max(0f, settleToKinematicDelay);
            lifetime = Mathf.Max(0f, fragmentLifetime);

            MeshFilter filter = GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            body = GetComponent<Rigidbody>();

            filter.sharedMesh = ownedMesh;
            meshRenderer.sharedMaterials = materials;

            if (propertyBlock != null)
                meshRenderer.SetPropertyBlock(propertyBlock);

            meshCollider.sharedMesh = ownedMesh;
            meshCollider.convex = true;

            body.mass = Mathf.Max(0.01f, mass);
            body.linearDamping = Mathf.Max(0f, drag);
            body.angularDamping = Mathf.Max(0f, angularDrag);
            body.useGravity = true;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;

            body.AddForce(impulse, ForceMode.Impulse);
            body.AddTorque(angularImpulse, ForceMode.Impulse);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            if (!settled &&
                body != null &&
                elapsed >= settleDelay &&
                body.IsSleeping())
            {
                body.isKinematic = true;
                body.collisionDetectionMode =
                    CollisionDetectionMode.Discrete;
                settled = true;
            }

            if (lifetime > 0f && elapsed >= lifetime)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (ownedMesh == null)
                return;

            if (Application.isPlaying)
                Destroy(ownedMesh);
            else
                DestroyImmediate(ownedMesh);
        }
    }
}
