using UnityEngine;

namespace PaintedAlive.Environment.PrismaticReach
{
    public enum PrismaticReachAnchorKind
    {
        Unknown = 0,
        Spawn = 1,
        FallRespawn = 2,
        Checkpoint = 3,
        Entry = 4,
        Exit = 5,
        Encounter = 6,
        TrampolineCenter = 7,
        TrampolineLaunch = 8,
        TrampolineLanding = 9
    }

    [DisallowMultipleComponent]
    public sealed class PrismaticReachAnchor : MonoBehaviour
    {
        [SerializeField] private PrismaticReachAnchorKind kind;
        [SerializeField] private string sourceHelperName;
        [SerializeField, TextArea(1, 3)] private string note;

        public PrismaticReachAnchorKind Kind => kind;
        public string SourceHelperName => sourceHelperName;
        public string Note => note;

        public void Configure(
            PrismaticReachAnchorKind newKind,
            string newSourceHelperName,
            string newNote)
        {
            kind = newKind;
            sourceHelperName = newSourceHelperName ?? string.Empty;
            note = newNote ?? string.Empty;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color color = kind switch
            {
                PrismaticReachAnchorKind.Spawn => new Color(0.20f, 0.90f, 0.40f, 1f),
                PrismaticReachAnchorKind.FallRespawn => new Color(1.00f, 0.55f, 0.15f, 1f),
                PrismaticReachAnchorKind.Exit => new Color(0.95f, 0.85f, 0.20f, 1f),
                PrismaticReachAnchorKind.Encounter => new Color(0.90f, 0.20f, 0.25f, 1f),
                PrismaticReachAnchorKind.TrampolineCenter => new Color(0.55f, 0.25f, 0.95f, 1f),
                PrismaticReachAnchorKind.TrampolineLaunch => new Color(0.20f, 0.70f, 1.00f, 1f),
                PrismaticReachAnchorKind.TrampolineLanding => new Color(0.20f, 0.95f, 0.95f, 1f),
                _ => new Color(0.85f, 0.85f, 0.85f, 1f)
            };

            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, 0.22f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.8f);
        }
#endif
    }
}
