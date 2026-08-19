using System;
using System.Reflection;
using UnityEngine;

namespace PaintedAlive.Painters.Masterpiece
{
    [DisallowMultipleComponent]
    public sealed class PrototypeFigureClarityImpactBridge :
        MonoBehaviour
    {
        [Header("Authoritative Source")]
        [SerializeField] private MonoBehaviour clarityState;

        [Header("Runtime Read Only")]
        [SerializeField] private bool contractResolved;
        [SerializeField] private bool sourceEnabled;
        [SerializeField] private int resolveAttemptCount;
        [SerializeField] private int resolveSuccessCount;
        [SerializeField] private int exposureWriteCount;
        [SerializeField] private int exposureWriteFailureCount;
        [SerializeField] private int resetCount;
        [SerializeField] private string sourcePath = "NULL";
        [SerializeField] private string sourceType = "NULL";
        [SerializeField] private string lastFailure = string.Empty;
        [SerializeField] private float lastClarity;
        [SerializeField] private float lastMaximumClarity;
        [SerializeField] private string lastLevel = "Unknown";

        private MethodInfo applyPaintExposureMethod;
        private MethodInfo resetToFullMethod;
        private PropertyInfo currentClarityProperty;
        private PropertyInfo maximumClarityProperty;
        private PropertyInfo currentLevelProperty;
        private Type regionEnumType;
        private object torsoRegionValue;

        public MonoBehaviour ClarityState => clarityState;
        public bool ContractResolved => contractResolved;
        public bool SourceEnabled => sourceEnabled;
        public int ResolveAttemptCount => resolveAttemptCount;
        public int ResolveSuccessCount => resolveSuccessCount;
        public int ExposureWriteCount => exposureWriteCount;
        public int ExposureWriteFailureCount =>
            exposureWriteFailureCount;
        public int ResetCount => resetCount;
        public string SourcePath => sourcePath;
        public string SourceType => sourceType;
        public string LastFailure => lastFailure;
        public float LastClarity => lastClarity;
        public float LastMaximumClarity =>
            lastMaximumClarity;
        public string LastLevel => lastLevel;

        public bool UsesReflectionCompatibilityBridge => true;
        public bool HealthSystemModified => false;
        public bool FigureMotorModified => false;
        public bool AddsCollider => false;

        public void Configure(
            MonoBehaviour configuredClarityState)
        {
            clarityState = configuredClarityState;
            ResolveContract();
        }

        private void Awake()
        {
            ResolveContract();
        }

        public bool ResolveContract()
        {
            resolveAttemptCount++;
            contractResolved = false;
            lastFailure = string.Empty;

            applyPaintExposureMethod = null;
            resetToFullMethod = null;
            currentClarityProperty = null;
            maximumClarityProperty = null;
            currentLevelProperty = null;
            regionEnumType = null;
            torsoRegionValue = null;

            if (clarityState == null)
            {
                sourcePath = "NULL";
                sourceType = "NULL";
                sourceEnabled = false;
                lastFailure =
                    "FigureClarityState source is null.";

                return false;
            }

            sourceEnabled =
                clarityState.enabled &&
                clarityState.gameObject.activeInHierarchy;

            sourcePath =
                BuildHierarchyPath(
                    clarityState.transform);

            Type sourceRuntimeType =
                clarityState.GetType();

            sourceType =
                sourceRuntimeType.FullName ??
                sourceRuntimeType.Name;

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            MethodInfo[] methods =
                sourceRuntimeType.GetMethods(
                    flags);

            for (int index = 0;
                 index < methods.Length;
                 index++)
            {
                MethodInfo candidate =
                    methods[index];

                if (
                    candidate == null ||
                    candidate.Name !=
                        "ApplyPaintExposure"
                )
                {
                    continue;
                }

                ParameterInfo[] parameters =
                    candidate.GetParameters();

                if (
                    parameters.Length != 2 ||
                    parameters[0].ParameterType !=
                        typeof(float) ||
                    !parameters[1].ParameterType.IsEnum
                )
                {
                    continue;
                }

                applyPaintExposureMethod =
                    candidate;

                regionEnumType =
                    parameters[1].ParameterType;

                break;
            }

            resetToFullMethod =
                sourceRuntimeType.GetMethod(
                    "ResetToFull",
                    flags,
                    null,
                    Type.EmptyTypes,
                    null);

            currentClarityProperty =
                sourceRuntimeType.GetProperty(
                    "CurrentClarity",
                    flags);

            maximumClarityProperty =
                sourceRuntimeType.GetProperty(
                    "MaximumClarity",
                    flags);

            currentLevelProperty =
                sourceRuntimeType.GetProperty(
                    "CurrentLevel",
                    flags);

            if (
                applyPaintExposureMethod == null ||
                regionEnumType == null
            )
            {
                lastFailure =
                    "ApplyPaintExposure(float, enum) contract not found.";

                return false;
            }

            try
            {
                torsoRegionValue =
                    Enum.Parse(
                        regionEnumType,
                        "Torso",
                        ignoreCase: true);
            }
            catch (Exception exception)
            {
                lastFailure =
                    "Torso region enum could not be resolved: " +
                    exception.Message;

                return false;
            }

            contractResolved = true;
            resolveSuccessCount++;
            RefreshSnapshot();

            return true;
        }

        public bool TryApplyTorsoExposure(
            float amount,
            out float before,
            out float after,
            out string levelBefore,
            out string levelAfter,
            out string reason)
        {
            before = 0f;
            after = 0f;
            levelBefore = "Unknown";
            levelAfter = "Unknown";
            reason = string.Empty;

            if (
                !contractResolved &&
                !ResolveContract()
            )
            {
                exposureWriteFailureCount++;
                reason = lastFailure;
                return false;
            }

            if (
                clarityState == null ||
                applyPaintExposureMethod == null ||
                torsoRegionValue == null
            )
            {
                exposureWriteFailureCount++;
                reason =
                    "Clarity write contract became unavailable.";

                return false;
            }

            if (amount <= 0f)
            {
                exposureWriteFailureCount++;
                reason =
                    "Exposure amount must be positive.";

                return false;
            }

            before =
                ReadFloat(
                    currentClarityProperty,
                    0f);

            levelBefore =
                ReadString(
                    currentLevelProperty,
                    "Unknown");

            try
            {
                applyPaintExposureMethod.Invoke(
                    clarityState,
                    new object[]
                    {
                        amount,
                        torsoRegionValue
                    });
            }
            catch (TargetInvocationException exception)
            {
                exposureWriteFailureCount++;
                reason =
                    exception.InnerException != null
                        ? exception.InnerException.Message
                        : exception.Message;

                lastFailure = reason;
                return false;
            }
            catch (Exception exception)
            {
                exposureWriteFailureCount++;
                reason = exception.Message;
                lastFailure = reason;
                return false;
            }

            after =
                ReadFloat(
                    currentClarityProperty,
                    before);

            levelAfter =
                ReadString(
                    currentLevelProperty,
                    levelBefore);

            exposureWriteCount++;
            RefreshSnapshot();

            reason =
                after < before
                    ? "Authoritative FigureClarityState accepted exposure."
                    : "Exposure call completed without clarity delta.";

            return true;
        }

        public bool TryResetToFull(
            out string reason)
        {
            reason = string.Empty;

            if (
                !contractResolved &&
                !ResolveContract()
            )
            {
                reason = lastFailure;
                return false;
            }

            if (
                clarityState == null ||
                resetToFullMethod == null
            )
            {
                reason =
                    "ResetToFull contract not found.";

                return false;
            }

            try
            {
                resetToFullMethod.Invoke(
                    clarityState,
                    null);

                resetCount++;
                RefreshSnapshot();
                reason =
                    "Authoritative clarity reset to full.";

                return true;
            }
            catch (TargetInvocationException exception)
            {
                reason =
                    exception.InnerException != null
                        ? exception.InnerException.Message
                        : exception.Message;

                lastFailure = reason;
                return false;
            }
            catch (Exception exception)
            {
                reason = exception.Message;
                lastFailure = reason;
                return false;
            }
        }

        public void RefreshSnapshot()
        {
            if (clarityState == null)
            {
                return;
            }

            sourceEnabled =
                clarityState.enabled &&
                clarityState.gameObject.activeInHierarchy;

            lastClarity =
                ReadFloat(
                    currentClarityProperty,
                    lastClarity);

            lastMaximumClarity =
                ReadFloat(
                    maximumClarityProperty,
                    lastMaximumClarity);

            lastLevel =
                ReadString(
                    currentLevelProperty,
                    lastLevel);
        }

        private float ReadFloat(
            PropertyInfo property,
            float fallback)
        {
            if (
                property == null ||
                clarityState == null
            )
            {
                return fallback;
            }

            try
            {
                object value =
                    property.GetValue(
                        clarityState);

                return value != null
                    ? Convert.ToSingle(value)
                    : fallback;
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private string ReadString(
            PropertyInfo property,
            string fallback)
        {
            if (
                property == null ||
                clarityState == null
            )
            {
                return fallback;
            }

            try
            {
                object value =
                    property.GetValue(
                        clarityState);

                return value != null
                    ? value.ToString()
                    : fallback;
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private static string BuildHierarchyPath(
            Transform target)
        {
            if (target == null)
            {
                return "NULL";
            }

            string path =
                target.name;

            while (target.parent != null)
            {
                target =
                    target.parent;

                path =
                    target.name +
                    "/" +
                    path;
            }

            return path;
        }
    }
}
