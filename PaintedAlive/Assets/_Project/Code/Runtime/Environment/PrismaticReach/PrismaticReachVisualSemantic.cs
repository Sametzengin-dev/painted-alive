using UnityEngine;

namespace PaintedAlive.Environment.PrismaticReach
{
    public enum PrismaticReachVisualKind
    {
        Environment = 0,
        Decor = 1,
        Vegetation = 2,
        Water = 3,
        Trampoline = 4
    }

    [DisallowMultipleComponent]
    public sealed class PrismaticReachVisualSemantic : MonoBehaviour
    {
        [SerializeField] private PrismaticReachVisualKind kind;
        public PrismaticReachVisualKind Kind => kind;

        public void Configure(PrismaticReachVisualKind value)
        {
            kind = value;
        }
    }
}
