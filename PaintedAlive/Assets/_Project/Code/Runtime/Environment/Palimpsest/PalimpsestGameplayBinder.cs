using UnityEngine;

namespace PaintedAlive.Environment.Palimpsest
{
    [DisallowMultipleComponent]
    public sealed class PalimpsestGameplayBinder : MonoBehaviour
    {
        [Header("Imported roots")]
        [SerializeField] private Transform mapRoot;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform collisionRoot;
        [SerializeField] private Transform gameplayRoot;

        [Header("Journey")]
        [SerializeField] private Transform entry;
        [SerializeField] private Transform exit;
        [SerializeField] private Transform[] routeReferences;

        [Header("Systems")]
        [SerializeField] private CanvasFoldSystem foldA;
        [SerializeField] private CanvasFoldSystem greatFold;
        [SerializeField] private CanvasBacksideRegion backsideRegion;
        [SerializeField] private CanvasBacksideTraceSystem backsideTrace;
        [SerializeField] private UnderpaintingRevealSystem underpaintA;
        [SerializeField] private UnderpaintingRevealSystem underpaintB;
        [SerializeField] private PaintSmearSurface smearA;
        [SerializeField] private PaintSmearSurface smearB;
        [SerializeField] private PerspectiveFrameSystem perspectiveExit;
        [SerializeField] private PerspectiveTraversalBridge perspectiveBridge;
        [SerializeField] private PalimpsestRespawnCoordinator respawnCoordinator;
        [SerializeField] private PalimpsestNetworkState networkState;

        public Transform MapRoot => mapRoot;
        public Transform VisualRoot => visualRoot;
        public Transform CollisionRoot => collisionRoot;
        public Transform GameplayRoot => gameplayRoot;
        public Transform Entry => entry;
        public Transform Exit => exit;
        public Transform[] RouteReferences => routeReferences;
        public CanvasFoldSystem FoldA => foldA;
        public CanvasFoldSystem GreatFold => greatFold;
        public CanvasBacksideRegion BacksideRegion => backsideRegion;
        public UnderpaintingRevealSystem UnderpaintA => underpaintA;
        public UnderpaintingRevealSystem UnderpaintB => underpaintB;
        public PaintSmearSurface SmearA => smearA;
        public PaintSmearSurface SmearB => smearB;
        public PerspectiveFrameSystem PerspectiveExit => perspectiveExit;
        public PalimpsestRespawnCoordinator RespawnCoordinator => respawnCoordinator;
        public PalimpsestNetworkState NetworkState => networkState;

        public bool IsConfigured =>
            mapRoot != null &&
            visualRoot != null &&
            collisionRoot != null &&
            gameplayRoot != null &&
            entry != null &&
            exit != null &&
            foldA != null &&
            greatFold != null &&
            backsideRegion != null &&
            backsideTrace != null &&
            underpaintA != null &&
            underpaintB != null &&
            smearA != null &&
            smearB != null &&
            perspectiveExit != null &&
            perspectiveBridge != null &&
            respawnCoordinator != null &&
            networkState != null;

        public void Configure(
            Transform root,
            Transform visual,
            Transform collision,
            Transform gameplay,
            Transform entryAnchor,
            Transform exitAnchor,
            Transform[] routes,
            CanvasFoldSystem foldASystem,
            CanvasFoldSystem greatFoldSystem,
            CanvasBacksideRegion backside,
            CanvasBacksideTraceSystem trace,
            UnderpaintingRevealSystem underpaintASystem,
            UnderpaintingRevealSystem underpaintBSystem,
            PaintSmearSurface smearASystem,
            PaintSmearSurface smearBSystem,
            PerspectiveFrameSystem perspective,
            PerspectiveTraversalBridge bridge,
            PalimpsestRespawnCoordinator respawn,
            PalimpsestNetworkState stateHub)
        {
            mapRoot = root;
            visualRoot = visual;
            collisionRoot = collision;
            gameplayRoot = gameplay;
            entry = entryAnchor;
            exit = exitAnchor;
            routeReferences = routes ?? System.Array.Empty<Transform>();
            foldA = foldASystem;
            greatFold = greatFoldSystem;
            backsideRegion = backside;
            backsideTrace = trace;
            underpaintA = underpaintASystem;
            underpaintB = underpaintBSystem;
            smearA = smearASystem;
            smearB = smearBSystem;
            perspectiveExit = perspective;
            perspectiveBridge = bridge;
            respawnCoordinator = respawn;
            networkState = stateHub;
        }
    }
}
