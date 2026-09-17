using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    public enum LivingGalleryAnchorKind
    {
        Unknown = 0,
        Entry = 1,
        Exit = 2,
        RouteGuide = 3,
        Visibility = 4,
        Interaction = 5
    }

    [DisallowMultipleComponent]
    public sealed class LivingGalleryAnchor : MonoBehaviour
    {
        [SerializeField] private LivingGalleryAnchorKind kind;
        [SerializeField] private string sourceName;
        [SerializeField, TextArea(1, 3)] private string note;

        public LivingGalleryAnchorKind Kind => kind;
        public string SourceName => sourceName;
        public string Note => note;

        public void Configure(LivingGalleryAnchorKind newKind, string newSourceName, string newNote)
        {
            kind = newKind;
            sourceName = newSourceName ?? string.Empty;
            note = newNote ?? string.Empty;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = kind == LivingGalleryAnchorKind.Entry
                ? new Color(0.2f, 0.85f, 1f, 1f)
                : kind == LivingGalleryAnchorKind.Exit
                    ? new Color(1f, 0.8f, 0.15f, 1f)
                    : new Color(0.75f, 0.75f, 0.75f, 1f);
            Gizmos.DrawWireSphere(transform.position, 0.22f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.8f);
        }
#endif
    }
}
