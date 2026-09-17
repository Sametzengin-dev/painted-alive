using System.Collections.Generic;
using PaintedAlive.Figures;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    public enum PainterFigureVisibility
    {
        Full = 0,
        Partial = 1,
        Hidden = 2
    }

    public static class PainterVisibilityRegistry
    {
        private static readonly Dictionary<FigureMotor, HashSet<PainterVisibilityVolume>> Memberships =
            new Dictionary<FigureMotor, HashSet<PainterVisibilityVolume>>();

        public static PainterFigureVisibility GetVisibility(FigureMotor figure)
        {
            if (figure == null || !Memberships.TryGetValue(figure, out HashSet<PainterVisibilityVolume> set))
                return PainterFigureVisibility.Full;

            PainterFigureVisibility result = PainterFigureVisibility.Full;
            set.RemoveWhere(v => v == null || !v.isActiveAndEnabled);

            foreach (PainterVisibilityVolume volume in set)
            {
                if (volume.Visibility == PainterFigureVisibility.Hidden)
                    return PainterFigureVisibility.Hidden;
                if (volume.Visibility == PainterFigureVisibility.Partial)
                    result = PainterFigureVisibility.Partial;
            }
            return result;
        }

        internal static void Enter(FigureMotor figure, PainterVisibilityVolume volume)
        {
            if (figure == null || volume == null)
                return;
            if (!Memberships.TryGetValue(figure, out HashSet<PainterVisibilityVolume> set))
            {
                set = new HashSet<PainterVisibilityVolume>();
                Memberships.Add(figure, set);
            }
            set.Add(volume);
        }

        internal static void Exit(FigureMotor figure, PainterVisibilityVolume volume)
        {
            if (figure == null || volume == null)
                return;
            if (!Memberships.TryGetValue(figure, out HashSet<PainterVisibilityVolume> set))
                return;
            set.Remove(volume);
            if (set.Count == 0)
                Memberships.Remove(figure);
        }

        public static void ClearAll() => Memberships.Clear();
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PainterVisibilityVolume : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        [SerializeField] private PainterFigureVisibility visibility = PainterFigureVisibility.Hidden;
        [SerializeField] private BoxCollider occupancyCollider;

        private readonly HashSet<FigureMotor> occupants = new HashSet<FigureMotor>();

        public string BindingId => bindingId;
        public PainterFigureVisibility Visibility => visibility;
        public BoxCollider OccupancyCollider => occupancyCollider;

        public void Configure(string newBindingId, PainterFigureVisibility newVisibility, BoxCollider collider)
        {
            bindingId = newBindingId ?? string.Empty;
            visibility = newVisibility;
            occupancyCollider = collider;
            if (occupancyCollider != null)
                occupancyCollider.isTrigger = true;
        }

        private void Awake()
        {
            if (occupancyCollider == null)
                occupancyCollider = GetComponent<BoxCollider>();
            if (occupancyCollider != null)
                occupancyCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            FigureMotor figure = other != null ? other.GetComponentInParent<FigureMotor>() : null;
            if (figure == null)
                return;
            occupants.Add(figure);
            PainterVisibilityRegistry.Enter(figure, this);
        }

        private void OnTriggerExit(Collider other)
        {
            FigureMotor figure = other != null ? other.GetComponentInParent<FigureMotor>() : null;
            if (figure == null)
                return;
            occupants.Remove(figure);
            PainterVisibilityRegistry.Exit(figure, this);
        }

        private void OnDisable()
        {
            foreach (FigureMotor figure in occupants)
                PainterVisibilityRegistry.Exit(figure, this);
            occupants.Clear();
        }

        public void RefreshOccupancy()
        {
            if (occupancyCollider == null)
                return;

            foreach (FigureMotor figure in occupants)
                PainterVisibilityRegistry.Exit(figure, this);
            occupants.Clear();

            Vector3 center = transform.TransformPoint(occupancyCollider.center);
            Vector3 half = Vector3.Scale(occupancyCollider.size * 0.5f, Abs(transform.lossyScale));
            Collider[] hits = Physics.OverlapBox(center, half, transform.rotation, ~0, QueryTriggerInteraction.Collide);
            foreach (Collider hit in hits)
            {
                FigureMotor figure = hit != null ? hit.GetComponentInParent<FigureMotor>() : null;
                if (figure == null || !occupants.Add(figure))
                    continue;
                PainterVisibilityRegistry.Enter(figure, this);
            }
        }

        private static Vector3 Abs(Vector3 value) =>
            new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
