using UnityEngine;

namespace PaintedAlive.Figures.StainSupport
{
    [DisallowMultipleComponent]
    public sealed class StainCleanGripSurface : MonoBehaviour
    {
        [SerializeField]
        private bool acceptsGripMarks = true;

        [SerializeField]
        private string surfaceLabel = "Temiz Tuval";

        public bool AcceptsGripMarks =>
            acceptsGripMarks &&
            isActiveAndEnabled &&
            gameObject.activeInHierarchy;
        public string SurfaceLabel =>
            string.IsNullOrWhiteSpace(surfaceLabel)
                ? "Temiz Yüzey"
                : surfaceLabel;

        public void Configure(
            bool acceptsMarks,
            string label)
        {
            acceptsGripMarks = acceptsMarks;
            surfaceLabel = string.IsNullOrWhiteSpace(label)
                ? "Temiz Yüzey"
                : label;
        }
    }
}
