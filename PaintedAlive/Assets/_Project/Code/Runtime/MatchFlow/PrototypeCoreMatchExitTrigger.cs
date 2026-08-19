using UnityEngine;

namespace PaintedAlive.MatchFlow
{
    [DisallowMultipleComponent]
    public sealed class PrototypeCoreMatchExitTrigger :
        MonoBehaviour
    {
        [SerializeField]
        private PrototypeCoreMatchController controller;

        [SerializeField]
        private Transform figureRoot;

        [SerializeField]
        private int validExitTriggerCount;

        public int ValidExitTriggerCount =>
            validExitTriggerCount;

        public void Configure(
            PrototypeCoreMatchController configuredController,
            Transform configuredFigureRoot)
        {
            controller =
                configuredController;

            figureRoot =
                configuredFigureRoot;
        }

        private void OnTriggerEnter(
            Collider other)
        {
            if (
                controller == null ||
                figureRoot == null ||
                other == null
            )
            {
                return;
            }

            Transform candidate =
                other.transform;

            bool belongsToFigure =
                candidate ==
                    figureRoot ||
                candidate.IsChildOf(
                    figureRoot) ||
                figureRoot.IsChildOf(
                    candidate);

            if (!belongsToFigure)
            {
                return;
            }

            validExitTriggerCount++;

            controller.NotifyFigureReachedExit();
        }
    }
}
