using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace PaintedAlive.Core
{
    public static class HardwareGraphicsDeviceRecovery
    {
        private const string ProbeArgument =
            "-paintedalive-gpu-probe";

        private const string DeviceIndexArgument =
            "-force-device-index";

        private const int FirstHardwareCandidateIndex = 1;
        private const int MaximumCandidateIndex = 8;

        private static bool recoveryStarted;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RecoverFromSoftwareRenderer()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (recoveryStarted || Application.isBatchMode ||
                !IsSoftwareRenderer())
            {
                return;
            }

            string[] arguments = Environment.GetCommandLineArgs();
            bool isRecoveryProbe =
                TryReadIntegerArgument(
                    arguments,
                    ProbeArgument,
                    out int currentCandidateIndex);

            if (!isRecoveryProbe &&
                HasArgument(arguments, DeviceIndexArgument))
            {
                Debug.LogError(
                    "Painted Alive is running on Microsoft Basic " +
                    "Render Driver even though a GPU index was " +
                    "explicitly requested. The requested adapter " +
                    "is unavailable; hardware recovery was not " +
                    "allowed to override the user's launch option.");
                return;
            }

            int nextCandidateIndex = isRecoveryProbe
                ? currentCandidateIndex + 1
                : FirstHardwareCandidateIndex;

            if (nextCandidateIndex > MaximumCandidateIndex)
            {
                Debug.LogError(
                    "Painted Alive could not find a hardware DirectX " +
                    "adapter. Windows exposed only Microsoft Basic " +
                    "Render Driver for adapter indices 0-" +
                    MaximumCandidateIndex + ".");
                return;
            }

            if (!TryRestartOnAdapter(
                    arguments,
                    nextCandidateIndex))
            {
                return;
            }

            recoveryStarted = true;
            Application.Quit();
#endif
        }

        public static bool IsSoftwareRenderer()
        {
            string deviceName =
                SystemInfo.graphicsDeviceName ?? string.Empty;

            return deviceName.IndexOf(
                    "Microsoft Basic Render Driver",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static bool TryRestartOnAdapter(
            IReadOnlyList<string> arguments,
            int candidateIndex)
        {
            try
            {
                using Process currentProcess =
                    Process.GetCurrentProcess();
                string executablePath =
                    currentProcess.MainModule?.FileName;

                if (string.IsNullOrWhiteSpace(executablePath) ||
                    !File.Exists(executablePath))
                {
                    Debug.LogError(
                        "Painted Alive found Microsoft Basic Render " +
                        "Driver, but could not resolve the player " +
                        "executable for hardware GPU recovery.");
                    return false;
                }

                string forwardedArguments =
                    BuildRecoveryArguments(
                        arguments,
                        candidateIndex);
                string workingDirectory =
                    Path.GetDirectoryName(executablePath);

                Debug.LogWarning(
                    "Painted Alive detected Microsoft Basic Render " +
                    "Driver. Restarting on hardware adapter index " +
                    candidateIndex + ".");

                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = forwardedArguments,
                    UseShellExecute = true,
                    WorkingDirectory = workingDirectory
                };

                using Process startedProcess =
                    Process.Start(startInfo);

                if (startedProcess == null)
                {
                    Debug.LogError(
                        "Painted Alive could not start the hardware " +
                        "GPU recovery process.");
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private static string BuildRecoveryArguments(
            IReadOnlyList<string> arguments,
            int candidateIndex)
        {
            var forwarded = new List<string>();

            for (int i = 1; i < arguments.Count; i++)
            {
                string argument = arguments[i];

                if (string.Equals(
                        argument,
                        DeviceIndexArgument,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        argument,
                        ProbeArgument,
                        StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    continue;
                }

                forwarded.Add(argument);
            }

            forwarded.Add(DeviceIndexArgument);
            forwarded.Add(candidateIndex.ToString());
            forwarded.Add(ProbeArgument);
            forwarded.Add(candidateIndex.ToString());

            var commandLine = new StringBuilder();

            for (int i = 0; i < forwarded.Count; i++)
            {
                if (i > 0)
                {
                    commandLine.Append(' ');
                }

                AppendQuotedArgument(commandLine, forwarded[i]);
            }

            return commandLine.ToString();
        }

        private static void AppendQuotedArgument(
            StringBuilder commandLine,
            string argument)
        {
            if (argument.Length > 0 &&
                argument.IndexOfAny(
                    new[] { ' ', '\t', '\n', '\v', '"' }) < 0)
            {
                commandLine.Append(argument);
                return;
            }

            commandLine.Append('"');
            int backslashCount = 0;

            foreach (char character in argument)
            {
                if (character == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (character == '"')
                {
                    commandLine.Append(
                        '\\',
                        backslashCount * 2 + 1);
                    commandLine.Append('"');
                    backslashCount = 0;
                    continue;
                }

                commandLine.Append('\\', backslashCount);
                backslashCount = 0;
                commandLine.Append(character);
            }

            commandLine.Append('\\', backslashCount * 2);
            commandLine.Append('"');
        }

        private static bool TryReadIntegerArgument(
            IReadOnlyList<string> arguments,
            string argumentName,
            out int value)
        {
            for (int i = 1; i < arguments.Count - 1; i++)
            {
                if (string.Equals(
                        arguments[i],
                        argumentName,
                        StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(arguments[i + 1], out value))
                {
                    return true;
                }
            }

            value = 0;
            return false;
        }

        private static bool HasArgument(
            IReadOnlyList<string> arguments,
            string argumentName)
        {
            for (int i = 1; i < arguments.Count; i++)
            {
                if (string.Equals(
                        arguments[i],
                        argumentName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
#endif
    }
}
