using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Paint
{
    [DisallowMultipleComponent]
    public sealed class OilStrokeStructuralIntegritySystem : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private OilStrokeSystem strokeSystem;

        [SerializeField]
        private OilStrokeStructuralIntegrityConfig config;

        [Header("Runtime Hierarchy")]
        [SerializeField] private Transform fragmentsRoot;

        [Header("Runtime - Read Only")]
        [SerializeField] private int totalFracturedStrokes;
        [SerializeField] private int totalCreatedFragments;

        private readonly List<OilStrokeFragmentRuntime> fragments = new();

        public event Action<OilStrokeRuntime, int> StrokeFractured;

        public Transform FragmentRoot =>
            fragmentsRoot != null ? fragmentsRoot : transform;

        public int TotalFracturedStrokes => totalFracturedStrokes;
        public int TotalCreatedFragments => totalCreatedFragments;

        private void Awake()
        {
            if (strokeSystem == null || config == null)
            {
                Debug.LogError(
                    $"{nameof(OilStrokeStructuralIntegritySystem)} " +
                    "requires Stroke System and Config references.",
                    this);

                enabled = false;
            }
        }

        private void Update()
        {
            RegisterNewStrokes();
            PruneMissingFragments();

            if (strokeSystem != null &&
                strokeSystem.Strokes.Count == 0 &&
                fragments.Count > 0)
            {
                ClearFragments();
            }
        }

        private void RegisterNewStrokes()
        {
            if (strokeSystem == null || config == null)
                return;

            foreach (OilStrokeRuntime stroke in strokeSystem.Strokes)
            {
                if (stroke == null)
                    continue;

                OilStrokeStructuralIntegrity integrity =
                    stroke.GetComponent<OilStrokeStructuralIntegrity>();

                if (integrity == null)
                {
                    integrity =
                        stroke.gameObject.AddComponent<
                            OilStrokeStructuralIntegrity>();
                }

                if (!integrity.IsInitialized)
                {
                    integrity.Initialize(
                        config,
                        this,
                        stroke);
                }
            }
        }

        public void RegisterFragment(
            OilStrokeFragmentRuntime fragment)
        {
            if (fragment != null && !fragments.Contains(fragment))
                fragments.Add(fragment);
        }

        public void NotifyStrokeFractured(
            OilStrokeRuntime stroke,
            int createdFragmentCount)
        {
            totalFracturedStrokes++;
            totalCreatedFragments += Mathf.Max(0, createdFragmentCount);

            StrokeFractured?.Invoke(
                stroke,
                createdFragmentCount);
        }

        public void ClearFragments()
        {
            for (int i = fragments.Count - 1; i >= 0; i--)
            {
                if (fragments[i] != null)
                    Destroy(fragments[i].gameObject);
            }

            fragments.Clear();
        }

        private void PruneMissingFragments()
        {
            for (int i = fragments.Count - 1; i >= 0; i--)
            {
                if (fragments[i] == null)
                    fragments.RemoveAt(i);
            }
        }
    }
}
