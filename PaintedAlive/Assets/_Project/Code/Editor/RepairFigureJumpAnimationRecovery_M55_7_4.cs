#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// M55.7.4 — Repairs the accepted production Figure Animator without rebuilding it.
///
/// Root cause:
/// Jump_Start had only exits that require Grounded == false. On a very short hop,
/// low ceiling, step/collider edge case, or fast re-grounding, the CharacterController
/// can become grounded while the Animator is still in Jump_Start. At that point both
/// existing exits become invalid and Jump_Start can remain active indefinitely.
///
/// This repair adds:
/// Jump_Start -> Land
///     Grounded == true
///     VerticalSpeed < 0.05
///     Has Exit Time == false
///
/// FigureMotor, movement values, gravity, jump physics and clips are untouched.
/// </summary>
public static class RepairFigureJumpAnimationRecovery_M55_7_4
{
    private const string MenuRoot = "Tools/Painted Alive/Milestones/";
    private const string ControllerPath =
        "Assets/_Project/Art/Animations/Figure/Controllers/AC_Figure_Production.controller";

    private const float RecoveryVerticalThreshold = 0.05f;
    private const float RecoveryTransitionDuration = 0.035f;

    [MenuItem(MenuRoot + "55.7.4 - Repair Jump Animation Recovery")]
    public static void Repair()
    {
        try
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (controller == null)
                throw new InvalidOperationException(
                    "Production Figure Animator Controller was not found:\n" +
                    ControllerPath);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState jump = FindState(sm, "Jump_Start");
            AnimatorState land = FindState(sm, "Land");

            if (jump == null || land == null)
                throw new InvalidOperationException(
                    "Jump_Start or Land state is missing from the production Animator Controller.");

            AnimatorStateTransition existing =
                FindRecoveryTransition(jump, land);

            bool created = false;

            if (existing == null)
            {
                AnimatorStateTransition recovery =
                    jump.AddTransition(land);

                recovery.hasExitTime = false;
                recovery.hasFixedDuration = true;
                recovery.duration = RecoveryTransitionDuration;
                recovery.offset = 0f;
                recovery.canTransitionToSelf = false;

                recovery.AddCondition(
                    AnimatorConditionMode.If,
                    0f,
                    "Grounded");

                recovery.AddCondition(
                    AnimatorConditionMode.Less,
                    RecoveryVerticalThreshold,
                    "VerticalSpeed");

                created = true;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                ControllerPath,
                ImportAssetOptions.ForceUpdate);

            string message =
                "Jump_Start grounded recovery " +
                (created ? "ADDED." : "already present; no duplicate added.") +
                "\n\n" +
                "Jump_Start -> Land\n" +
                "Grounded = true\n" +
                "VerticalSpeed < 0.05\n" +
                "Has Exit Time = false\n" +
                "Transition Duration = 0.035s\n\n" +
                "FigureMotor / jump physics / gravity / movement values: UNCHANGED.";

            Debug.Log("[Painted Alive M55.7.4] " + message.Replace("\n", " | "));
            EditorUtility.DisplayDialog(
                "Painted Alive M55.7.4",
                message,
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog(
                "M55.7.4 Repair Failed",
                ex.Message,
                "OK");
        }
    }

    [MenuItem(MenuRoot + "55.7.4 - Diagnose Jump Animation Recovery")]
    public static void Diagnose()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        bool controllerOk = controller != null;
        bool jumpOk = false;
        bool landOk = false;
        bool recoveryOk = false;
        int jumpTransitionCount = 0;

        if (controller != null && controller.layers.Length > 0)
        {
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState jump = FindState(sm, "Jump_Start");
            AnimatorState land = FindState(sm, "Land");

            jumpOk = jump != null;
            landOk = land != null;

            if (jump != null)
            {
                jumpTransitionCount = jump.transitions.Length;

                if (land != null)
                    recoveryOk = FindRecoveryTransition(jump, land) != null;
            }
        }

        bool pass =
            controllerOk &&
            jumpOk &&
            landOk &&
            recoveryOk;

        string report =
            "Painted Alive M55.7.4 Diagnostics\n" +
            "Result=" + (pass ? "PASS" : "NEEDS_ATTENTION") + "\n\n" +
            "Controller=" + (controllerOk ? "OK" : "MISSING") + "\n" +
            "Jump_Start=" + (jumpOk ? "OK" : "MISSING") + "\n" +
            "Land=" + (landOk ? "OK" : "MISSING") + "\n" +
            "JumpTransitionCount=" + jumpTransitionCount + "\n" +
            "GroundedRecovery=" + (recoveryOk ? "OK" : "MISSING") + "\n\n" +
            "Required recovery:\n" +
            "Jump_Start -> Land\n" +
            "Grounded=true\n" +
            "VerticalSpeed<0.05\n" +
            "HasExitTime=false\n\n" +
            "Manual Play Mode check:\n" +
            "- normal Jump still behaves identically\n" +
            "- very short hops recover to Land/Locomotion\n" +
            "- landing after a low ceiling does not freeze Jump_Start\n" +
            "- trampoline bounce still reaches Fall_Loop/Land normally";

        Debug.Log("[Painted Alive M55.7.4]\n" + report);
        EditorUtility.DisplayDialog(
            "M55.7.4 Diagnostics",
            report,
            "OK");
    }

    private static AnimatorState FindState(
        AnimatorStateMachine sm,
        string stateName)
    {
        return sm.states
            .Select(child => child.state)
            .FirstOrDefault(
                state =>
                    state != null &&
                    string.Equals(
                        state.name,
                        stateName,
                        StringComparison.Ordinal));
    }

    private static AnimatorStateTransition FindRecoveryTransition(
        AnimatorState jump,
        AnimatorState land)
    {
        foreach (AnimatorStateTransition transition in jump.transitions)
        {
            if (transition == null || transition.destinationState != land)
                continue;

            bool grounded = false;
            bool vertical = false;

            foreach (AnimatorCondition condition in transition.conditions)
            {
                if (condition.parameter == "Grounded" &&
                    condition.mode == AnimatorConditionMode.If)
                {
                    grounded = true;
                }

                if (condition.parameter == "VerticalSpeed" &&
                    condition.mode == AnimatorConditionMode.Less &&
                    Mathf.Abs(
                        condition.threshold -
                        RecoveryVerticalThreshold) <= 0.001f)
                {
                    vertical = true;
                }
            }

            if (grounded &&
                vertical &&
                !transition.hasExitTime)
            {
                return transition;
            }
        }

        return null;
    }
}
#endif
