#if UNITY_EDITOR
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.Painters.Masterpiece;
using PaintedAlive.Painters.SideCanvas;
using UnityEditor;
using UnityEngine;

namespace PaintedAlive.Editor
{
    public static class SetupPrototypeMasterpieceRoleAuthorityBoundaryMilestone
    {
        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "49.1.1 - Diagnose Role Authority Boundary")]
        public static void Diagnose()
        {
            bool sourceResolved =
                PrototypeRoleAuthorityResolver.TryResolve(
                    out string role,
                    out bool isPainter);

            PrototypeLivingSideCanvasController sideCanvas =
                FindOptional<PrototypeLivingSideCanvasController>();
            PrototypeMasterpieceAssemblyController assembly =
                FindOptional<PrototypeMasterpieceAssemblyController>();
            PrototypeMasterpieceWorldDeploymentController deployment =
                FindOptional<PrototypeMasterpieceWorldDeploymentController>();
            PrototypeMasterpiecePossessionController possession =
                FindOptional<PrototypeMasterpiecePossessionController>();
            PrototypeMasterpieceAutonomousEncounterController encounter =
                FindOptional<PrototypeMasterpieceAutonomousEncounterController>();

            string report =
                "[M49.1.1 Role Authority Diagnose]\\n" +
                $"RoleSourceResolved={sourceResolved}\\n" +
                $"ResolvedRole={role}\\n" +
                $"IsPainter={isPainter}\\n" +
                $"ExpectedPainterInputs={(isPainter ? "ENABLED" : "DISABLED")}\\n" +
                $"ResolverAttempts={PrototypeRoleAuthorityResolver.ResolveAttemptCount}\\n" +
                $"ResolverSuccess={PrototypeRoleAuthorityResolver.ResolveSuccessCount}\\n" +
                $"ResolverFailure={PrototypeRoleAuthorityResolver.ResolveFailureCount}\\n" +
                $"ResolverRoleChanges={PrototypeRoleAuthorityResolver.RoleChangeCount}\\n" +
                $"SideCanvas={(sideCanvas != null ? "OK" : "MISSING")}\\n" +
                $"SideCanvasPainterRole={(sideCanvas != null && sideCanvas.PainterRoleActive)}\\n" +
                $"SideCanvasOpen={(sideCanvas != null && sideCanvas.IsOpen)}\\n" +
                $"SideCanvasAutoCloseCount={(sideCanvas != null ? sideCanvas.RoleAutoCloseCount : 0)}\\n" +
                $"SideCanvasRoleRejected={(sideCanvas != null ? sideCanvas.RoleRejectedActionCount : 0)}\\n" +
                $"Assembly={(assembly != null ? "OK" : "MISSING")}\\n" +
                $"AssemblyPainterRole={(assembly != null && assembly.PainterRoleActive)}\\n" +
                $"AssemblyRoleRejected={(assembly != null ? assembly.RoleRejectedActionCount : 0)}\\n" +
                $"Deployment={(deployment != null ? "OK" : "MISSING")}\\n" +
                $"DeploymentPainterRole={(deployment != null && deployment.PainterRoleActive)}\\n" +
                $"DeploymentInputs={(deployment != null && deployment.DeployInputActionsActive)}\\n" +
                $"DeploymentRoleRejected={(deployment != null ? deployment.RoleRejectedActionCount : 0)}\\n" +
                $"Possession={(possession != null ? "OK" : "MISSING")}\\n" +
                $"PossessionPainterRole={(possession != null && possession.PainterRoleActive)}\\n" +
                $"PossessionInputs={(possession != null && possession.InputActionsActive)}\\n" +
                $"Possessed={(possession != null && possession.Possessed)}\\n" +
                $"PossessionForcedRelease={(possession != null ? possession.RoleForcedReleaseCount : 0)}\\n" +
                $"PossessionRoleRejected={(possession != null ? possession.RoleRejectedActionCount : 0)}\\n" +
                $"Encounter={(encounter != null ? "OK" : "MISSING")}\\n" +
                $"EncounterPainterControl={(encounter != null && encounter.PainterControlRoleActive)}\\n" +
                $"EncounterToggleInput={(encounter != null && encounter.InputActionsActive)}\\n" +
                $"EncounterRoleRejected={(encounter != null ? encounter.RoleRejectedActionCount : 0)}\\n" +
                "FigureAllowed_KJ_VisualSafety=True\\n" +
                "FigureAllowed_LM_ClientSettings=True\\n" +
                "FigureAllowed_H_HitQueryDebug=True\\n" +
                "FigureAllowed_VXO_Deployment=False\\n" +
                "FigureAllowed_P_Possession=False\\n" +
                "FigureAllowed_I_AutonomyControl=False";

            Debug.Log(
                report,
                deployment != null ? deployment : encounter);

            EditorUtility.DisplayDialog(
                "M49.1.1 Role Authority",
                report,
                "Tamam");
        }

        private static T FindOptional<T>() where T : Component
        {
            T[] objects =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            for (int index = 0; index < objects.Length; index++)
            {
                T candidate = objects[index];

                if (candidate != null &&
                    !EditorUtility.IsPersistent(candidate) &&
                    candidate.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
#endif
