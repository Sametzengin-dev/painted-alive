using System;
using PaintedAlive.UI.UnifiedHUD;
using UnityEngine;

namespace PaintedAlive.Core.RoleAuthority
{
    public static class PrototypeRoleAuthorityResolver
    {
        private static PrototypeUnifiedHudController cachedHudController;
        private static string lastResolvedRole = "Unknown";
        private static bool lastIsPainter;
        private static int resolveAttemptCount;
        private static int resolveSuccessCount;
        private static int resolveFailureCount;
        private static int roleChangeCount;

        public static string LastResolvedRole => lastResolvedRole;
        public static bool LastIsPainter => lastIsPainter;
        public static int ResolveAttemptCount => resolveAttemptCount;
        public static int ResolveSuccessCount => resolveSuccessCount;
        public static int ResolveFailureCount => resolveFailureCount;
        public static int RoleChangeCount => roleChangeCount;
        public static bool SourceResolved => IsUsable(cachedHudController);

        public static bool IsPainter(out string role)
        {
            bool resolved = TryResolve(out role, out bool isPainter);
            return resolved && isPainter;
        }

        public static bool IsFigure(out string role)
        {
            bool resolved = TryResolve(out role, out bool isPainter);
            return resolved && !isPainter;
        }

        public static bool TryResolve(out string role, out bool isPainter)
        {
            resolveAttemptCount++;

            if (!IsUsable(cachedHudController))
            {
                cachedHudController = FindBestHudController();
            }

            if (!IsUsable(cachedHudController))
            {
                role = "Unknown";
                isPainter = false;
                lastResolvedRole = role;
                lastIsPainter = false;
                resolveFailureCount++;
                return false;
            }

            try
            {
                PrototypeUnifiedHudSnapshot snapshot =
                    cachedHudController.BuildSnapshot();

                role = string.IsNullOrWhiteSpace(snapshot.Role)
                    ? cachedHudController.ResolvedRole
                    : snapshot.Role;

                if (string.IsNullOrWhiteSpace(role))
                {
                    role = "Unknown";
                }

                isPainter = snapshot.IsPainter;

                if (!string.Equals(
                        lastResolvedRole,
                        role,
                        StringComparison.OrdinalIgnoreCase) ||
                    lastIsPainter != isPainter)
                {
                    roleChangeCount++;
                }

                lastResolvedRole = role;
                lastIsPainter = isPainter;
                resolveSuccessCount++;
                return true;
            }
            catch (Exception)
            {
                cachedHudController = null;
                role = "Unknown";
                isPainter = false;
                lastResolvedRole = role;
                lastIsPainter = false;
                resolveFailureCount++;
                return false;
            }
        }

        public static void InvalidateCache()
        {
            cachedHudController = null;
        }

        private static PrototypeUnifiedHudController FindBestHudController()
        {
            PrototypeUnifiedHudController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    PrototypeUnifiedHudController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            PrototypeUnifiedHudController best = null;
            int bestScore = int.MinValue;

            for (int index = 0; index < controllers.Length; index++)
            {
                PrototypeUnifiedHudController candidate = controllers[index];

                if (!IsUsable(candidate))
                {
                    continue;
                }

                int score = 0;

                if (candidate.DataSourcesResolved)
                {
                    score += 1000;
                }

                if (candidate.isActiveAndEnabled)
                {
                    score += 200;
                }

                if (candidate.gameObject.activeInHierarchy)
                {
                    score += 100;
                }

                if (candidate.name.IndexOf(
                        "Unified",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 20;
                }

                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static bool IsUsable(
            PrototypeUnifiedHudController candidate)
        {
            return candidate != null &&
                candidate.gameObject != null &&
                candidate.gameObject.scene.IsValid();
        }
    }
}
