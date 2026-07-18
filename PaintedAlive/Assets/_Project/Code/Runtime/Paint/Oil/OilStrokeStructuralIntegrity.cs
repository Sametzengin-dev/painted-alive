using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    public sealed class OilStrokeStructuralIntegrity : MonoBehaviour
    {
        [Header("Runtime - Read Only")]
        [SerializeField] private float maximumIntegrity;
        [SerializeField] private float currentIntegrity;
        [SerializeField] private int observedCutCount;
        [SerializeField] private bool fractured;

        private OilStrokeStructuralIntegrityConfig config;
        private OilStrokeStructuralIntegritySystem integritySystem;
        private OilStrokeRuntime stroke;
        private bool initialized;

        public bool IsInitialized => initialized;
        public bool IsFractured => fractured;
        public float MaximumIntegrity => maximumIntegrity;
        public float CurrentIntegrity => currentIntegrity;

        public float NormalizedIntegrity =>
            maximumIntegrity > 0f
                ? Mathf.Clamp01(currentIntegrity / maximumIntegrity)
                : 0f;

        public void Initialize(
            OilStrokeStructuralIntegrityConfig integrityConfig,
            OilStrokeStructuralIntegritySystem ownerSystem,
            OilStrokeRuntime strokeRuntime)
        {
            config = integrityConfig;
            integritySystem = ownerSystem;
            stroke = strokeRuntime;

            if (config == null || stroke == null)
            {
                Debug.LogError(
                    $"{nameof(OilStrokeStructuralIntegrity)} " +
                    "requires config and stroke references.",
                    this);

                enabled = false;
                return;
            }

            maximumIntegrity =
                config.GetInitialIntegrity(stroke.PressureProfile);

            currentIntegrity = maximumIntegrity;
            observedCutCount = stroke.CutCount;
            fractured = false;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized || fractured || stroke == null)
                return;

            int currentCutCount = stroke.CutCount;

            if (currentCutCount <= observedCutCount)
                return;

            int newCuts = currentCutCount - observedCutCount;
            observedCutCount = currentCutCount;

            for (int i = 0; i < newCuts; i++)
                ApplyCutDamage();

            if (observedCutCount >=
                    config.MinimumCutsBeforeFracture &&
                currentIntegrity <= 0f)
            {
                TryFracture();
            }
        }

        private void ApplyCutDamage()
        {
            float damage = config.GetCutDamage(stroke.State);

            OilStrokeFixativeStatus fixativeStatus =
                stroke.GetComponent<
                    OilStrokeFixativeStatus>();

            if (fixativeStatus != null)
            {
                damage *=
                    fixativeStatus.CutDamageMultiplier;
            }

            currentIntegrity = Mathf.Max(0f, currentIntegrity - damage);
        }

        private void TryFracture()
        {
            MeshFilter sourceFilter = GetComponent<MeshFilter>();
            MeshRenderer sourceRenderer = GetComponent<MeshRenderer>();
            MeshCollider sourceCollider = GetComponent<MeshCollider>();

            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                return;

            Mesh sourceMesh = sourceFilter.sharedMesh;

            List<Mesh> fragments =
                OilStrokeMeshFragmentUtility.SplitConnectedComponents(
                    sourceMesh,
                    config.MinimumTrianglesPerFragment);

            if (fragments.Count < 2)
            {
                DestroyGeneratedFragments(fragments);
                return;
            }

            fractured = true;

            if (sourceCollider != null)
                sourceCollider.enabled = false;

            if (sourceRenderer != null)
                sourceRenderer.enabled = false;

            Material[] materials =
                sourceRenderer != null
                    ? sourceRenderer.sharedMaterials
                    : System.Array.Empty<Material>();

            MaterialPropertyBlock propertyBlock =
                new MaterialPropertyBlock();

            if (sourceRenderer != null)
                sourceRenderer.GetPropertyBlock(propertyBlock);

            Transform fragmentRoot =
                integritySystem != null
                    ? integritySystem.FragmentRoot
                    : transform.parent;

            Vector3 sourceCenter =
                transform.TransformPoint(sourceMesh.bounds.center);

            int createdFragmentCount = 0;

            for (int i = 0; i < fragments.Count; i++)
            {
                Mesh fragmentMesh = fragments[i];

                OilStrokeFragmentRuntime fragment =
                    CreatePhysicalFragment(
                        fragmentMesh,
                        materials,
                        propertyBlock,
                        fragmentRoot,
                        sourceCenter,
                        i);

                if (fragment == null)
                {
                    DestroyMesh(fragmentMesh);
                    continue;
                }

                createdFragmentCount++;

                if (integritySystem != null)
                    integritySystem.RegisterFragment(fragment);
            }

            if (integritySystem != null)
            {
                integritySystem.NotifyStrokeFractured(
                    stroke,
                    createdFragmentCount);
            }

            Destroy(gameObject);
        }

        private OilStrokeFragmentRuntime CreatePhysicalFragment(
            Mesh fragmentMesh,
            Material[] materials,
            MaterialPropertyBlock propertyBlock,
            Transform fragmentRoot,
            Vector3 sourceCenter,
            int index)
        {
            if (fragmentMesh == null)
                return null;

            var fragmentObject =
                new GameObject($"{name}_Fragment_{index + 1:00}");

            if (fragmentRoot != null)
                fragmentObject.transform.SetParent(fragmentRoot, false);

            fragmentObject.transform.SetPositionAndRotation(
                transform.position,
                transform.rotation);

            fragmentObject.transform.localScale =
                GetLocalScaleForParent(
                    transform.lossyScale,
                    fragmentRoot);

            fragmentObject.layer = gameObject.layer;

            Vector3 worldFragmentCenter =
                transform.TransformPoint(fragmentMesh.bounds.center);

            Vector3 outward = worldFragmentCenter - sourceCenter;

            if (outward.sqrMagnitude < 0.001f)
            {
                float angle =
                    index * 2.39996323f;

                outward =
                    transform.TransformDirection(
                        new Vector3(
                            Mathf.Cos(angle),
                            0.2f,
                            Mathf.Sin(angle)));
            }

            outward.Normalize();

            Vector3 impulse =
                outward * config.SeparationImpulse +
                transform.up * config.UpwardImpulse;

            float torqueAngle =
                (index + 1) * 1.6180339f;

            Vector3 angularImpulse =
                new Vector3(
                    Mathf.Sin(torqueAngle),
                    Mathf.Cos(torqueAngle * 0.7f),
                    Mathf.Sin(torqueAngle * 1.3f)) *
                config.AngularImpulse;

            var fragment =
                fragmentObject.AddComponent<OilStrokeFragmentRuntime>();

            float mass = config.CalculateFragmentMass(
                fragmentMesh.bounds,
                transform.lossyScale);

            fragment.Initialize(
                fragmentMesh,
                materials,
                propertyBlock,
                mass,
                config.FragmentDrag,
                config.FragmentAngularDrag,
                impulse,
                angularImpulse,
                config.SettleToKinematicDelay,
                config.FragmentLifetime);

            return fragment;
        }

        private static Vector3 GetLocalScaleForParent(
            Vector3 worldScale,
            Transform parent)
        {
            if (parent == null)
                return worldScale;

            Vector3 parentScale = parent.lossyScale;

            return new Vector3(
                SafeDivide(worldScale.x, parentScale.x),
                SafeDivide(worldScale.y, parentScale.y),
                SafeDivide(worldScale.z, parentScale.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f
                ? value / divisor
                : value;
        }

        private static void DestroyGeneratedFragments(List<Mesh> meshes)
        {
            foreach (Mesh mesh in meshes)
                DestroyMesh(mesh);
        }

        private static void DestroyMesh(Mesh mesh)
        {
            if (mesh == null)
                return;

            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }
    }
}
