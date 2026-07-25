using System.Collections.Generic;
using UnityEngine;

namespace PaintedAlive.Figures.StainTraversal
{
    [DisallowMultipleComponent]
    public sealed class StainCrackPassage : MonoBehaviour
    {
        private static readonly List<StainCrackPassage>
            ActivePassageList = new List<StainCrackPassage>();

        [SerializeField]
        private StainCrackPassage linkedPassage;

        [SerializeField]
        private Transform exitPoint;

        [SerializeField]
        private Renderer crackRenderer;

        [SerializeField]
        private bool passageEnabled = true;

        public static IReadOnlyList<StainCrackPassage>
            ActivePassages => ActivePassageList;
        public StainCrackPassage LinkedPassage => linkedPassage;
        public Vector3 EntryPosition => transform.position;
        public Vector3 ExitPosition =>
            exitPoint != null
                ? exitPoint.position
                : transform.position + transform.forward;
        public bool CanTraverse =>
            passageEnabled &&
            isActiveAndEnabled &&
            linkedPassage != null &&
            linkedPassage != this &&
            linkedPassage.passageEnabled &&
            linkedPassage.isActiveAndEnabled;

        private void OnEnable()
        {
            if (!ActivePassageList.Contains(this))
            {
                ActivePassageList.Add(this);
            }

            UpdateVisual();
        }

        private void OnDisable()
        {
            ActivePassageList.Remove(this);
        }

        public void Configure(
            StainCrackPassage targetLinkedPassage,
            Transform targetExitPoint,
            Renderer targetRenderer,
            bool isEnabled)
        {
            linkedPassage = targetLinkedPassage;
            exitPoint = targetExitPoint;
            crackRenderer = targetRenderer;
            passageEnabled = isEnabled;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (crackRenderer != null)
            {
                crackRenderer.enabled = passageEnabled;
            }
        }
    }
}
