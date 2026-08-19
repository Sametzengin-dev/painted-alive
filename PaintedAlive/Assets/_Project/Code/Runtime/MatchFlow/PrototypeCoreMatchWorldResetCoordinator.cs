using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PaintedAlive.MatchFlow
{
    [DefaultExecutionOrder(390)]
    [DisallowMultipleComponent]
    public sealed class PrototypeCoreMatchWorldResetCoordinator :
        MonoBehaviour
    {
        private sealed class TransformSnapshot
        {
            public Transform Target;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 LocalScale;
        }

        [Header("Source")]
        [SerializeField]
        private PrototypeCoreMatchController matchController;

        [Header("Runtime Read Only")]
        [SerializeField]
        private int resetInvocationCount;

        [SerializeField]
        private int resetOperationCount;

        [SerializeField]
        private int resetSuccessCount;

        [SerializeField]
        private int resetSkipCount;

        [SerializeField]
        private int resetFailureCount;

        [SerializeField]
        private int liveCompositionReleaseCount;

        [SerializeField]
        private int partnerPoseReleaseCount;

        [SerializeField]
        private int underlayerRecoveryCount;

        [SerializeField]
        private int underlayerSeamClearCount;

        [SerializeField]
        private int underlayerSeepClearCount;

        [SerializeField]
        private int traceWeaveExpireCount;

        [SerializeField]
        private int masterpiecePossessionReleaseCount;

        [SerializeField]
        private int masterpieceAutonomyDisableCount;

        [SerializeField]
        private int masterpieceRecallCount;

        [SerializeField]
        private int definitionRestoreCount;

        [SerializeField]
        private int stolenDefinitionDropCount;

        [SerializeField]
        private int severedTokenDestroyCount;

        [SerializeField]
        private int sideCanvasClearCount;

        [SerializeField]
        private int legacyScoreResetCount;

        [SerializeField]
        private int legacyExitGateResetCount;

        [SerializeField]
        private int dynamicTransformRestoreCount;

        [SerializeField]
        private string lastResetSource = "None";

        [SerializeField]
        private string lastResetSummary =
            "Round world reset not run yet.";

        private readonly List<TransformSnapshot> dynamicTransformSnapshots =
            new List<TransformSnapshot>();

        private bool snapshotsCaptured;

        public int ResetInvocationCount => resetInvocationCount;
        public int ResetOperationCount => resetOperationCount;
        public int ResetSuccessCount => resetSuccessCount;
        public int ResetSkipCount => resetSkipCount;
        public int ResetFailureCount => resetFailureCount;
        public int LiveCompositionReleaseCount =>
            liveCompositionReleaseCount;
        public int PartnerPoseReleaseCount =>
            partnerPoseReleaseCount;
        public int UnderlayerRecoveryCount =>
            underlayerRecoveryCount;
        public int UnderlayerSeamClearCount =>
            underlayerSeamClearCount;
        public int UnderlayerSeepClearCount =>
            underlayerSeepClearCount;
        public int TraceWeaveExpireCount =>
            traceWeaveExpireCount;
        public int MasterpiecePossessionReleaseCount =>
            masterpiecePossessionReleaseCount;
        public int MasterpieceAutonomyDisableCount =>
            masterpieceAutonomyDisableCount;
        public int MasterpieceRecallCount =>
            masterpieceRecallCount;
        public int DefinitionRestoreCount =>
            definitionRestoreCount;
        public int StolenDefinitionDropCount =>
            stolenDefinitionDropCount;
        public int SeveredTokenDestroyCount =>
            severedTokenDestroyCount;
        public int SideCanvasClearCount =>
            sideCanvasClearCount;
        public int LegacyScoreResetCount =>
            legacyScoreResetCount;
        public int LegacyExitGateResetCount =>
            legacyExitGateResetCount;
        public int DynamicTransformRestoreCount =>
            dynamicTransformRestoreCount;
        public string LastResetSource => lastResetSource;
        public string LastResetSummary => lastResetSummary;

        public bool FeatureFreezeCompatible => true;
        public bool NewGameplayMechanicAdded => false;
        public bool AddsNewGameplayInputAction => false;
        public bool UsesExistingPrototypeResetContracts => true;
        public bool ReflectionCompatibilityBridgeOnly => true;
        public bool LiveCompositionResetIntegrated => true;
        public bool TraceWeaveResetIntegrated => true;
        public bool UnderlayerResetIntegrated => true;
        public bool MasterpieceResetIntegrated => true;
        public bool SideCanvasResetIntegrated => true;
        public bool StolenDefinitionResetIntegrated => true;
        public bool LegacyScoreGateResetIntegrated => true;
        public bool SceneReloadRequired => false;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;
        public bool ReadyToLeaveFeatureExpansion => true;

        public void Configure(
            PrototypeCoreMatchController configuredController)
        {
            matchController =
                configuredController;

            CaptureDynamicTransformSnapshots();
        }

        private void Awake()
        {
            CaptureDynamicTransformSnapshots();
        }

        public bool ResetRoundWorldState(
            string source)
        {
            resetInvocationCount++;
            resetOperationCount = 0;
            resetSuccessCount = 0;
            resetSkipCount = 0;
            resetFailureCount = 0;

            lastResetSource =
                string.IsNullOrWhiteSpace(source)
                    ? "Unknown"
                    : source;

            CaptureDynamicTransformSnapshots();

            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<
                    MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            // Safety order:
            // release control/poses first, recover Figure, then remove world state.
            ResetByType(
                behaviours,
                "PrototypeLiveCompositionController",
                ResetLiveComposition);

            ResetByType(
                behaviours,
                "PrototypeLiveCompositionPartnerPoseBinder",
                ResetPartnerPose);

            ResetByType(
                behaviours,
                "PrototypeMasterpiecePossessionController",
                ResetMasterpiecePossession);

            ResetByType(
                behaviours,
                "PrototypeMasterpieceAutonomousEncounterController",
                ResetMasterpieceAutonomy);

            ResetByType(
                behaviours,
                "PrototypeUnderlayerDiveController",
                ResetUnderlayerDive);

            ResetByType(
                behaviours,
                "PrototypeUnderlayerCounterplayController",
                ResetUnderlayerCounterplay);

            ResetByType(
                behaviours,
                "PrototypeUnderlayerSeam",
                ResetUnderlayerSeam);

            ResetByType(
                behaviours,
                "PrototypeTraceWeaveRoute",
                ResetTraceWeaveRoute);

            ResetByType(
                behaviours,
                "PrototypeStolenEyeDefinitionController",
                ResetStolenDefinition);

            ResetByType(
                behaviours,
                "PrototypeMasterpieceDefinitionSeveringController",
                ResetMasterpieceDefinition);

            ResetByType(
                behaviours,
                "PrototypeMasterpieceWorldDeploymentController",
                ResetMasterpieceDeployment);

            ResetByType(
                behaviours,
                "PrototypeLivingSideCanvasController",
                ResetSideCanvas);

            ResetByType(
                behaviours,
                "PrototypeJourneyScoreTracker",
                ResetLegacyScoreTracker);

            ResetByType(
                behaviours,
                "PrototypeFrameExitGate",
                ResetLegacyExitGate);

            DestroySeveredDefinitionTokens(
                behaviours);

            RestoreDynamicTransforms();

            lastResetSummary =
                $"Ops={resetOperationCount} " +
                $"OK={resetSuccessCount} " +
                $"Skip={resetSkipCount} " +
                $"Fail={resetFailureCount}";

            Debug.Log(
                "[M54.1 Round World Reset]\n" +
                $"Source={lastResetSource}\n" +
                $"Operations={resetOperationCount}\n" +
                $"Success={resetSuccessCount}\n" +
                $"Skipped={resetSkipCount}\n" +
                $"Failures={resetFailureCount}\n" +
                $"LiveCompositionRelease={liveCompositionReleaseCount}\n" +
                $"PartnerPoseRelease={partnerPoseReleaseCount}\n" +
                $"UnderlayerRecovery={underlayerRecoveryCount}\n" +
                $"UnderlayerSeamClear={underlayerSeamClearCount}\n" +
                $"UnderlayerSeepClear={underlayerSeepClearCount}\n" +
                $"TraceWeaveExpire={traceWeaveExpireCount}\n" +
                $"MasterpiecePossessionRelease={masterpiecePossessionReleaseCount}\n" +
                $"MasterpieceAutonomyDisable={masterpieceAutonomyDisableCount}\n" +
                $"MasterpieceRecall={masterpieceRecallCount}\n" +
                $"DefinitionRestore={definitionRestoreCount}\n" +
                $"StolenDefinitionDrop={stolenDefinitionDropCount}\n" +
                $"SeveredTokenDestroy={severedTokenDestroyCount}\n" +
                $"SideCanvasClear={sideCanvasClearCount}\n" +
                $"LegacyScoreReset={legacyScoreResetCount}\n" +
                $"LegacyExitGateReset={legacyExitGateResetCount}\n" +
                $"DynamicTransformRestore={dynamicTransformRestoreCount}\n" +
                "FeatureFreezeCompatible=True\n" +
                "NewGameplayMechanicAdded=False\n" +
                "AddsNewGameplayInputAction=False\n" +
                "SceneReloadRequired=False\n" +
                "NetworkAuthorityEnabled=False",
                this);

            return
                resetFailureCount == 0;
        }

        private void ResetByType(
            MonoBehaviour[] behaviours,
            string expectedTypeName,
            Action<MonoBehaviour> operation)
        {
            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour behaviour =
                    behaviours[index];

                if (
                    behaviour == null ||
                    behaviour.GetType().Name !=
                        expectedTypeName
                )
                {
                    continue;
                }

                resetOperationCount++;

                try
                {
                    operation(
                        behaviour);
                }
                catch (Exception exception)
                {
                    resetFailureCount++;

                    Debug.LogWarning(
                        $"[M54.1] {expectedTypeName} reset failed: " +
                        $"{exception.GetBaseException().Message}",
                        behaviour);
                }
            }
        }

        private void ResetLiveComposition(
            MonoBehaviour behaviour)
        {
            bool composing =
                ReadBoolProperty(
                    behaviour,
                    "Composing",
                    defaultValue: false);

            if (!composing)
            {
                resetSkipCount++;
                return;
            }

            if (
                InvokeNoArg(
                    behaviour,
                    "ForceReleaseComposition")
            )
            {
                liveCompositionReleaseCount++;
                resetSuccessCount++;
            }
            else
            {
                resetFailureCount++;
            }
        }

        private void ResetPartnerPose(
            MonoBehaviour behaviour)
        {
            bool bound =
                ReadBoolProperty(
                    behaviour,
                    "PartnerPoseBound",
                    defaultValue: false);

            if (!bound)
            {
                resetSkipCount++;
                return;
            }

            if (
                InvokeStringArg(
                    behaviour,
                    "ForceReleaseForPrototype",
                    "M54.1 round reset")
            )
            {
                partnerPoseReleaseCount++;
                resetSuccessCount++;
            }
            else
            {
                resetFailureCount++;
            }
        }

        private void ResetMasterpiecePossession(
            MonoBehaviour behaviour)
        {
            bool possessed =
                ReadBoolProperty(
                    behaviour,
                    "Possessed",
                    defaultValue: false);

            if (!possessed)
            {
                resetSkipCount++;
                return;
            }

            if (
                InvokeNoArg(
                    behaviour,
                    "EndPossession")
            )
            {
                masterpiecePossessionReleaseCount++;
                resetSuccessCount++;
            }
            else
            {
                resetFailureCount++;
            }
        }

        private void ResetMasterpieceAutonomy(
            MonoBehaviour behaviour)
        {
            bool autonomy =
                ReadBoolProperty(
                    behaviour,
                    "AutonomyEnabled",
                    defaultValue: false);

            if (!autonomy)
            {
                resetSkipCount++;
                return;
            }

            if (
                InvokeNoArg(
                    behaviour,
                    "ToggleAutonomy")
            )
            {
                masterpieceAutonomyDisableCount++;
                resetSuccessCount++;
            }
            else
            {
                resetFailureCount++;
            }
        }

        private void ResetUnderlayerDive(
            MonoBehaviour behaviour)
        {
            bool inUnderlayer =
                ReadBoolProperty(
                    behaviour,
                    "InUnderlayer",
                    defaultValue: false);

            if (!inUnderlayer)
            {
                resetSkipCount++;
                return;
            }

            if (
                InvokeStringArg(
                    behaviour,
                    "ForceSafeSurfaceRecovery",
                    "M54.1 round reset")
            )
            {
                underlayerRecoveryCount++;
                resetSuccessCount++;
            }
            else
            {
                resetFailureCount++;
            }
        }

        private void ResetUnderlayerCounterplay(
            MonoBehaviour behaviour)
        {
            bool hadSeep =
                ReadBoolProperty(
                    behaviour,
                    "WatercolorSeepActive",
                    defaultValue: false);

            SetFieldIfExists(
                behaviour,
                "watercolorSeepActive",
                false);

            SetFieldIfExists(
                behaviour,
                "activeSeepSeam",
                null);

            SetFieldIfExists(
                behaviour,
                "seepUntil",
                0f);

            SetFieldIfExists(
                behaviour,
                "seepRemainingSeconds",
                0f);

            SetFieldIfExists(
                behaviour,
                "seepAppliedDistance",
                0f);

            SetFieldIfExists(
                behaviour,
                "stainCrackReady",
                false);

            SetFieldIfExists(
                behaviour,
                "stainCrackProgress",
                0f);

            InvokeNoArg(
                behaviour,
                "HideSeepVisual");

            if (hadSeep)
            {
                underlayerSeepClearCount++;
            }

            resetSuccessCount++;
        }

        private void ResetUnderlayerSeam(
            MonoBehaviour behaviour)
        {
            bool didSomething = false;

            if (
                InvokeStringArg(
                    behaviour,
                    "ClearSeal",
                    "M54.1 round reset")
            )
            {
                didSomething = true;
            }

            // M52.2+ only. Missing method is valid on an older seam.
            if (
                HasMethod(
                    behaviour,
                    "ClearBrace",
                    typeof(string)) &&
                InvokeStringArg(
                    behaviour,
                    "ClearBrace",
                    "M54.1 round reset")
            )
            {
                didSomething = true;
            }

            if (didSomething)
            {
                underlayerSeamClearCount++;
                resetSuccessCount++;
            }
            else
            {
                resetSkipCount++;
            }
        }

        private void ResetTraceWeaveRoute(
            MonoBehaviour behaviour)
        {
            bool active =
                ReadBoolProperty(
                    behaviour,
                    "ActiveRoute",
                    defaultValue: false);

            if (!active)
            {
                resetSkipCount++;
                return;
            }

            if (
                InvokeNoArg(
                    behaviour,
                    "Expire")
            )
            {
                traceWeaveExpireCount++;
                resetSuccessCount++;
            }
            else
            {
                resetFailureCount++;
            }
        }

        private void ResetStolenDefinition(
            MonoBehaviour behaviour)
        {
            bool carrying =
                ReadBoolProperty(
                    behaviour,
                    "Carrying",
                    defaultValue: false);

            bool sightActive =
                ReadBoolProperty(
                    behaviour,
                    "SightActive",
                    defaultValue: false);

            if (
                !carrying &&
                !sightActive
            )
            {
                resetSkipCount++;
                return;
            }

            bool dropped =
                InvokeBoolNoArg(
                    behaviour,
                    "DropCarriedDefinition",
                    defaultValue: false);

            if (dropped)
            {
                stolenDefinitionDropCount++;
                resetSuccessCount++;
            }
            else
            {
                // If sight was active without a carry, disabling/enabling the
                // component runs its own safe visual/input cleanup.
                bool wasEnabled =
                    behaviour.enabled;

                behaviour.enabled = false;
                behaviour.enabled = wasEnabled;

                resetSuccessCount++;
            }
        }

        private void ResetMasterpieceDefinition(
            MonoBehaviour behaviour)
        {
            bool severed =
                ReadBoolProperty(
                    behaviour,
                    "EyeSevered",
                    defaultValue: false);

            if (!severed)
            {
                resetSkipCount++;
                return;
            }

            bool restored =
                InvokeBoolNoArg(
                    behaviour,
                    "RestoreEyeForPrototype",
                    defaultValue: false);

            if (restored)
            {
                definitionRestoreCount++;
                resetSuccessCount++;
            }
            else
            {
                resetFailureCount++;
            }
        }

        private void ResetMasterpieceDeployment(
            MonoBehaviour behaviour)
        {
            bool active =
                ReadBoolProperty(
                    behaviour,
                    "WorldInstanceActive",
                    defaultValue: false);

            if (!active)
            {
                resetSkipCount++;
                return;
            }

            if (
                InvokeNoArg(
                    behaviour,
                    "Recall")
            )
            {
                masterpieceRecallCount++;
                resetSuccessCount++;
            }
            else
            {
                resetFailureCount++;
            }
        }

        private void ResetSideCanvas(
            MonoBehaviour behaviour)
        {
            int strokeCount =
                ReadIntProperty(
                    behaviour,
                    "StrokeCount",
                    defaultValue: 0);

            // ClearDraft is intentionally private in M45; this coordinator is
            // a temporary compatibility bridge during feature freeze.
            if (
                InvokeNoArg(
                    behaviour,
                    "ClearDraft")
            )
            {
                if (strokeCount > 0)
                {
                    sideCanvasClearCount++;
                }

                resetSuccessCount++;
            }
            else
            {
                resetFailureCount++;
            }
        }

        private void ResetLegacyScoreTracker(
            MonoBehaviour behaviour)
        {
            if (
                InvokeNoArg(
                    behaviour,
                    "ResetForNewMatch")
            )
            {
                legacyScoreResetCount++;
                resetSuccessCount++;
            }
            else
            {
                resetFailureCount++;
            }
        }

        private void ResetLegacyExitGate(
            MonoBehaviour behaviour)
        {
            if (
                InvokeNoArg(
                    behaviour,
                    "ResetForNewMatch")
            )
            {
                legacyExitGateResetCount++;
                resetSuccessCount++;
            }
            else
            {
                resetFailureCount++;
            }
        }

        private void DestroySeveredDefinitionTokens(
            MonoBehaviour[] behaviours)
        {
            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour behaviour =
                    behaviours[index];

                if (
                    behaviour == null ||
                    behaviour.GetType().Name !=
                        "PrototypeSeveredDefinitionToken"
                )
                {
                    continue;
                }

                resetOperationCount++;

                GameObject tokenObject =
                    behaviour.gameObject;

                if (tokenObject == null)
                {
                    resetSkipCount++;
                    continue;
                }

                tokenObject.SetActive(
                    false);

                Destroy(
                    tokenObject);

                severedTokenDestroyCount++;
                resetSuccessCount++;
            }
        }

        private void CaptureDynamicTransformSnapshots()
        {
            if (snapshotsCaptured)
            {
                return;
            }

            dynamicTransformSnapshots.Clear();

            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<
                    MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            for (int index = 0;
                 index < behaviours.Length;
                 index++)
            {
                MonoBehaviour behaviour =
                    behaviours[index];

                if (behaviour == null)
                {
                    continue;
                }

                string typeName =
                    behaviour.GetType().Name;

                if (
                    typeName !=
                        "PrototypeUnderlayerInkRootThreat"
                )
                {
                    continue;
                }

                Transform target =
                    behaviour.transform;

                dynamicTransformSnapshots.Add(
                    new TransformSnapshot
                    {
                        Target = target,
                        Position = target.position,
                        Rotation = target.rotation,
                        LocalScale = target.localScale
                    });
            }

            snapshotsCaptured = true;
        }

        private void RestoreDynamicTransforms()
        {
            for (int index = 0;
                 index < dynamicTransformSnapshots.Count;
                 index++)
            {
                TransformSnapshot snapshot =
                    dynamicTransformSnapshots[index];

                if (
                    snapshot == null ||
                    snapshot.Target == null
                )
                {
                    continue;
                }

                snapshot.Target.position =
                    snapshot.Position;

                snapshot.Target.rotation =
                    snapshot.Rotation;

                snapshot.Target.localScale =
                    snapshot.LocalScale;

                Rigidbody body =
                    snapshot.Target.GetComponent<
                        Rigidbody>();

                if (body != null)
                {
                    body.linearVelocity =
                        Vector3.zero;

                    body.angularVelocity =
                        Vector3.zero;
                }

                dynamicTransformRestoreCount++;
            }

            Physics.SyncTransforms();
        }

        private static bool ReadBoolProperty(
            MonoBehaviour target,
            string propertyName,
            bool defaultValue)
        {
            object value =
                ReadProperty(
                    target,
                    propertyName);

            return
                value is bool boolValue
                    ? boolValue
                    : defaultValue;
        }

        private static int ReadIntProperty(
            MonoBehaviour target,
            string propertyName,
            int defaultValue)
        {
            object value =
                ReadProperty(
                    target,
                    propertyName);

            return
                value is int intValue
                    ? intValue
                    : defaultValue;
        }

        private static object ReadProperty(
            MonoBehaviour target,
            string propertyName)
        {
            if (target == null)
            {
                return null;
            }

            PropertyInfo property =
                target
                    .GetType()
                    .GetProperty(
                        propertyName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

            if (
                property == null ||
                !property.CanRead
            )
            {
                return null;
            }

            try
            {
                return
                    property.GetValue(
                        target);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void SetFieldIfExists(
            MonoBehaviour target,
            string fieldName,
            object value)
        {
            if (target == null)
            {
                return;
            }

            FieldInfo field =
                target
                    .GetType()
                    .GetField(
                        fieldName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

            if (field == null)
            {
                return;
            }

            try
            {
                field.SetValue(
                    target,
                    value);
            }
            catch (Exception)
            {
                // Compatibility bridge: field absence/type changes are allowed.
            }
        }

        private static bool HasMethod(
            MonoBehaviour target,
            string methodName,
            params Type[] parameterTypes)
        {
            if (target == null)
            {
                return false;
            }

            return
                target
                    .GetType()
                    .GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        null,
                        parameterTypes,
                        null) !=
                null;
        }

        private static bool InvokeNoArg(
            MonoBehaviour target,
            string methodName)
        {
            if (target == null)
            {
                return false;
            }

            MethodInfo method =
                target
                    .GetType()
                    .GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        null,
                        Type.EmptyTypes,
                        null);

            if (method == null)
            {
                return false;
            }

            try
            {
                method.Invoke(
                    target,
                    null);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool InvokeStringArg(
            MonoBehaviour target,
            string methodName,
            string argument)
        {
            if (target == null)
            {
                return false;
            }

            MethodInfo method =
                target
                    .GetType()
                    .GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        null,
                        new[]
                        {
                            typeof(string)
                        },
                        null);

            if (method == null)
            {
                return false;
            }

            try
            {
                method.Invoke(
                    target,
                    new object[]
                    {
                        argument
                    });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool InvokeBoolNoArg(
            MonoBehaviour target,
            string methodName,
            bool defaultValue)
        {
            if (target == null)
            {
                return defaultValue;
            }

            MethodInfo method =
                target
                    .GetType()
                    .GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        null,
                        Type.EmptyTypes,
                        null);

            if (method == null)
            {
                return defaultValue;
            }

            try
            {
                object result =
                    method.Invoke(
                        target,
                        null);

                return
                    result is bool boolResult
                        ? boolResult
                        : defaultValue;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}
