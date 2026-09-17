using PaintedAlive.Paint;
using PaintedAlive.Paint.Ink.GlyphLoadouts;
using PaintedAlive.Painters.Ink;
using UnityEngine;

namespace PaintedAlive.Networking.M56
{
    /// <summary>
    /// FishNet-free seam used by the existing single-player controllers.
    /// The actual M56 router is compiled only after FishNet/Steam dependencies
    /// have been installed. Until then every controller keeps local behavior.
    /// </summary>
    public interface IPaintedAliveNetworkRoleRouter
    {
        bool NetworkSessionActive { get; }
        bool Connected { get; }
        bool GameplayInputSuppressed { get; }
        int LocalClientId { get; }
        int RoleRevision { get; }
        string LastRoleFeedback { get; }
        void RequestRole(PaintedAliveLocalRole desiredRole);
    }

    public interface IPaintedAliveNetworkGameplayRouter
    {
        bool NetworkSessionActive { get; }
        bool Connected { get; }
        string LastGameplayFeedback { get; }

        bool RequestWorldAction(string systemReference);

        void PublishLocalOilStroke(
            Vector3[] points,
            OilStrokeShape shape,
            OilStrokePressureProfile pressureProfile);

        void PublishLocalOilStrokeClear();

        void PublishLocalInkCreature(
            InkGlyphLoadoutId loadoutId,
            Vector3 point,
            Vector3 normal,
            Vector3 facing);
    }

    public static class PaintedAliveNetworkRoleBridge
    {
        private static IPaintedAliveNetworkRoleRouter router;

        public static bool NetworkSessionActive =>
            router != null && router.NetworkSessionActive;

        public static bool Connected =>
            router != null && router.Connected;

        /// <summary>
        /// True while M56 presentation owns local keyboard/mouse input.
        /// Networking continues running; gameplay controllers must stay idle.
        /// </summary>
        public static bool GameplayInputSuppressed =>
            router != null && router.GameplayInputSuppressed;

        public static int LocalClientId =>
            router != null ? router.LocalClientId : -1;

        public static int RoleRevision =>
            router != null ? router.RoleRevision : 0;

        public static string LastFeedback =>
            router != null ? router.LastRoleFeedback : "Offline";

        public static void Register(IPaintedAliveNetworkRoleRouter value)
        {
            router = value;
        }

        public static void Unregister(IPaintedAliveNetworkRoleRouter value)
        {
            if (ReferenceEquals(router, value))
                router = null;
        }

        public static bool TryRequestRole(PaintedAliveLocalRole desiredRole)
        {
            if (!NetworkSessionActive)
                return false;

            router.RequestRole(desiredRole);
            return true;
        }
    }

    public static class PaintedAliveNetworkGameplayBridge
    {
        private static IPaintedAliveNetworkGameplayRouter router;

        public static bool NetworkSessionActive =>
            router != null && router.NetworkSessionActive;

        public static bool Connected =>
            router != null && router.Connected;

        public static string LastFeedback =>
            router != null ? router.LastGameplayFeedback : "Offline";

        public static void Register(IPaintedAliveNetworkGameplayRouter value)
        {
            router = value;
        }

        public static void Unregister(IPaintedAliveNetworkGameplayRouter value)
        {
            if (ReferenceEquals(router, value))
                router = null;
        }

        public static bool TryRequestWorldAction(string systemReference)
        {
            return NetworkSessionActive &&
                   router.RequestWorldAction(systemReference);
        }

        public static void NotifyLocalOilStroke(
            Vector3[] points,
            OilStrokeShape shape,
            OilStrokePressureProfile pressureProfile)
        {
            if (!NetworkSessionActive)
                return;

            router.PublishLocalOilStroke(
                points,
                shape,
                pressureProfile);
        }

        public static void NotifyLocalOilStrokeClear()
        {
            if (!NetworkSessionActive)
                return;

            router.PublishLocalOilStrokeClear();
        }

        public static void NotifyLocalInkCreature(
            InkGlyphLoadoutId loadoutId,
            Vector3 point,
            Vector3 normal,
            Vector3 facing)
        {
            if (!NetworkSessionActive)
                return;

            router.PublishLocalInkCreature(
                loadoutId,
                point,
                normal,
                facing);
        }
    }

    /// <summary>
    /// M56's single display source for the currently implemented controls.
    /// Keep these labels aligned with PaintedAliveInputActions and the direct
    /// keyboard checks in the role controllers.
    /// </summary>
    public static class M56ControlHints
    {
        public readonly struct Hint
        {
            public Hint(string binding, string action)
            {
                Binding = binding;
                Action = action;
            }

            public string Binding { get; }
            public string Action { get; }
        }

        public static readonly Hint[] Figure =
        {
            new("WASD", "MOVE"),
            new("MOUSE", "LOOK"),
            new("SPACE", "JUMP"),
            new("LEFT SHIFT", "SPRINT"),
            new("1 / 2 / 3 / 4", "KNIFE / FIXATIVE / FRAME GUN / SPONGE"),
            new("E", "USE ACTIVE TOOL"),
            new("R", "SPONGE RELEASE / RESTORE"),
            new("F1 / F2", "REQUEST FIGURE / PAINTER"),
            new("ESC", "M56 MENU")
        };

        public static readonly Hint[] Painter =
        {
            new("WASD + Q / E", "MOVE CAMERA + DOWN / UP"),
            new("MOUSE", "LOOK / AIM"),
            new("LMB", "PAINT"),
            new("RMB", "AIMED WORLD ACTION"),
            new("1 / 2", "WALL / RAMP STROKE"),
            new("F7", "CREATE INK CREATURE"),
            new("R", "REFRAME / CLEAR PAINT"),
            new("F1 / F2", "REQUEST FIGURE / PAINTER"),
            new("ESC", "M56 MENU")
        };

        public static Hint[] ForRole(PaintedAliveLocalRole role)
        {
            return role == PaintedAliveLocalRole.InkPainter
                ? Painter
                : Figure;
        }
    }
}
