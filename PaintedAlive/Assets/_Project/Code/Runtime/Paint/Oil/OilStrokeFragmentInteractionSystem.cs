using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class OilStrokeFragmentInteractionSystem : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private OilStrokeFragmentInteractionConfig config;

        [SerializeField]
        private Transform fragmentsRoot;

        [Header("Runtime - Read Only")]
        [SerializeField]
        private int registeredFragmentCount;

        private readonly List<OilStrokeFragmentRuntime> fragmentBuffer =
            new();

        private readonly Dictionary<
            int,
            OilStrokeFragmentFigurePush> registeredFragments =
            new();

        private readonly List<int> staleIds = new();
        private int scanCount;

        public int RegisteredFragmentCount =>
            registeredFragmentCount;

        private void Awake()
        {
            if (fragmentsRoot == null)
            {
                fragmentsRoot = transform;
            }

            if (config == null)
            {
                Debug.LogError(
                    $"{nameof(OilStrokeFragmentInteractionSystem)} " +
                    "requires an interaction config.",
                    this);

                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            RegisterNewFragments();

            scanCount++;

            if (scanCount % 30 == 0)
            {
                PruneDestroyedFragments();
            }
        }

        private void RegisterNewFragments()
        {
            fragmentBuffer.Clear();

            fragmentsRoot.GetComponentsInChildren(
                false,
                fragmentBuffer);

            foreach (OilStrokeFragmentRuntime fragment
                     in fragmentBuffer)
            {
                if (fragment == null)
                {
                    continue;
                }

                int fragmentId = fragment.GetInstanceID();

                if (registeredFragments.TryGetValue(
                        fragmentId,
                        out OilStrokeFragmentFigurePush existing) &&
                    existing != null)
                {
                    continue;
                }

                OilStrokeFragmentFigurePush push =
                    fragment.GetComponent<
                        OilStrokeFragmentFigurePush>();

                if (push == null)
                {
                    push =
                        fragment.gameObject.AddComponent<
                            OilStrokeFragmentFigurePush>();
                }

                push.Initialize(
                    config,
                    fragment.GetComponent<Rigidbody>());

                registeredFragments[fragmentId] = push;
            }

            registeredFragmentCount =
                registeredFragments.Count;
        }

        private void PruneDestroyedFragments()
        {
            staleIds.Clear();

            foreach (KeyValuePair<
                         int,
                         OilStrokeFragmentFigurePush> pair
                     in registeredFragments)
            {
                if (pair.Value == null)
                {
                    staleIds.Add(pair.Key);
                }
            }

            foreach (int staleId in staleIds)
            {
                registeredFragments.Remove(staleId);
            }

            registeredFragmentCount =
                registeredFragments.Count;
        }

        private void OnValidate()
        {
            if (fragmentsRoot == null)
            {
                fragmentsRoot = transform;
            }
        }
    }
}
