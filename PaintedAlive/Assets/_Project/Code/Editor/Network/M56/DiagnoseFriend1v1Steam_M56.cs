using System;
using System.IO;
using System.Linq;
using FishNet.Managing;
using FishNet.Managing.Transporting;
using FishNet.Transporting;
using FishySteamworks;
using PaintedAlive.Core.RoleAuthority;
using PaintedAlive.Environment.LivingGallery;
using PaintedAlive.Environment.Palimpsest;
using PaintedAlive.Figures;
using PaintedAlive.Networking.M56;
using PaintedAlive.Painters.Ink;
using PaintedAlive.Painters.World;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools.Networking.M56
{
    public static class DiagnoseFriend1v1Steam_M56
    {
        private const string MenuRoot = "Painted Alive/M56.0/";

        [MenuItem(MenuRoot + "2B - Diagnose Friend 1v1 Steam")]
        public static void DiagnoseMenu()
        {
            if (Application.isBatchMode)
                OpenFirstEnabledBuildScene();

            Diagnose(showDialog: !Application.isBatchMode);
        }

        private static void OpenFirstEnabledBuildScene()
        {
            EditorBuildSettingsScene buildScene =
                EditorBuildSettings.scenes.FirstOrDefault(
                    scene => scene.enabled &&
                             !string.IsNullOrWhiteSpace(scene.path));

            if (buildScene == null)
                throw new InvalidOperationException(
                    "M56 Diagnose requires an enabled build scene.");

            EditorSceneManager.OpenScene(
                buildScene.path,
                OpenSceneMode.Single);
        }

        public static bool Diagnose(bool showDialog)
        {
            int hardErrors = 0;
            int warnings = 0;

            NetworkManager[] managers =
                UnityEngine.Object.FindObjectsByType<NetworkManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(IsSceneObject)
                .ToArray();

            NetworkManager manager =
                managers.Length == 1 ? managers[0] : null;

            M56FriendSession[] sessions =
                UnityEngine.Object.FindObjectsByType<M56FriendSession>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(IsSceneObject)
                .ToArray();

            M56SteamBootstrap[] bootstraps =
                UnityEngine.Object.FindObjectsByType<M56SteamBootstrap>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(IsSceneObject)
                .ToArray();

            FishySteamworks.FishySteamworks[] transports =
                UnityEngine.Object.FindObjectsByType<
                    FishySteamworks.FishySteamworks>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                .Where(IsSceneObject)
                .ToArray();

            FishySteamworks.FishySteamworks transport =
                manager != null
                    ? manager.GetComponent<FishySteamworks.FishySteamworks>()
                    : null;

            TransportManager transportManager =
                manager != null
                    ? manager.GetComponent<TransportManager>()
                    : null;

            M56SteamBootstrap bootstrap =
                manager != null
                    ? manager.GetComponent<M56SteamBootstrap>()
                    : null;

            M56FriendSession session =
                manager != null
                    ? manager.GetComponent<M56FriendSession>()
                    : null;

            InkPainterRoleAuthority roleAuthority =
                UnityEngine.Object.FindFirstObjectByType<InkPainterRoleAuthority>(
                    FindObjectsInactive.Include);

            FigureMotor[] figureMotors =
                UnityEngine.Object.FindObjectsByType<FigureMotor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(IsSceneObject)
                .ToArray();

            bool transportBound =
                transport != null &&
                transportManager != null &&
                transportManager.Transport == transport;

            bool peerToPeer = false;
            int maximumClients = -1;

            if (transport != null)
            {
                SerializedObject serialized =
                    new SerializedObject(transport);

                SerializedProperty p2p =
                    serialized.FindProperty("_peerToPeer");
                SerializedProperty max =
                    serialized.FindProperty("_maximumClients");

                peerToPeer = p2p != null && p2p.boolValue;
                maximumClients = max != null ? max.intValue : -1;
            }

            string appIdPath =
                Path.GetFullPath(
                    Path.Combine(Application.dataPath, "..", "steam_appid.txt"));
            bool appIdOk =
                File.Exists(appIdPath) &&
                File.ReadAllText(appIdPath).Trim() == "480";

            PalimpsestPainterWorldInteraction[] palimpsest =
                UnityEngine.Object.FindObjectsByType<
                    PalimpsestPainterWorldInteraction>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(IsSceneObject)
                .ToArray();

            LivingGalleryPainterWorldInteraction[] gallery =
                UnityEngine.Object.FindObjectsByType<
                    LivingGalleryPainterWorldInteraction>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(IsSceneObject)
                .ToArray();

            int replicatedWorldActions =
                palimpsest.Count(x =>
                    x is INetworkReplicatedPainterWorldAction) +
                gallery.Count(x =>
                    x is INetworkReplicatedPainterWorldAction);

            string fishNetVersion = ResolvePackageVersion(
                "com.firstgeargames.fishnet");
            string steamworksVersion = ResolvePackageVersion(
                "com.rlabrecque.steamworks.net");
            string fishyVersion = ResolvePackageVersion(
                "com.firstgeargames.fishysteamworks");

            bool fishNetOk = !string.IsNullOrWhiteSpace(fishNetVersion);
            bool steamworksOk = !string.IsNullOrWhiteSpace(steamworksVersion);
            bool fishyOk = !string.IsNullOrWhiteSpace(fishyVersion);
            bool unityCompileRelevantState =
                !EditorApplication.isCompiling &&
                !EditorApplication.isUpdating;
            bool fishNetLoaded =
                typeof(NetworkManager).Assembly != null;
            bool steamworksNetLoaded =
                typeof(Steamworks.SteamAPI).Assembly != null;
            bool fishySteamworksLoaded =
                typeof(FishySteamworks.FishySteamworks).Assembly != null;
            bool fishyTransportResolvable =
                typeof(Transport).IsAssignableFrom(
                    typeof(FishySteamworks.FishySteamworks));

            int duplicateNetworkObjects =
                Mathf.Max(0, managers.Length - 1) +
                Mathf.Max(0, sessions.Length - 1) +
                Mathf.Max(0, bootstraps.Length - 1) +
                Mathf.Max(0, transports.Length - 1);

            bool[] checks =
            {
                managers.Length == 1,
                sessions.Length == 1,
                bootstraps.Length == 1,
                transports.Length == 1,
                manager != null,
                transportBound,
                peerToPeer,
                maximumClients == 2,
                bootstrap != null,
                session != null,
                roleAuthority != null,
                manager != null && manager.SpawnablePrefabs != null,
                appIdOk,
                fishNetOk,
                steamworksOk,
                fishyOk,
                unityCompileRelevantState,
                fishNetLoaded,
                steamworksNetLoaded,
                fishySteamworksLoaded,
                fishyTransportResolvable,
                figureMotors.Length == 1,
                duplicateNetworkObjects == 0,
                palimpsest.Length == 0 ||
                    palimpsest.All(x => x is INetworkReplicatedPainterWorldAction),
                gallery.Length == 0 ||
                    gallery.All(x => x is INetworkReplicatedPainterWorldAction)
            };

            for (int index = 0; index < checks.Length; index++)
            {
                if (!checks[index])
                    hardErrors++;
            }

            if (palimpsest.Length == 0)
                warnings++;
            if (gallery.Length == 0)
                warnings++;

            string result =
                hardErrors == 0
                    ? "CONFIG_PASS_TWO_PC_RUNTIME_REQUIRED"
                    : "NEEDS_ATTENTION";

            string report =
                "M56 Diagnose\n" +
                "Result=" + result + "\n" +
                $"UnityCompileRelevantState={unityCompileRelevantState}\n" +
                $"FishNetLoaded={fishNetLoaded} ({fishNetVersion})\n" +
                $"SteamworksNETLoaded={steamworksNetLoaded} ({steamworksVersion})\n" +
                $"FishySteamworksLoaded={fishySteamworksLoaded} ({fishyVersion})\n" +
                $"FishyTransportResolvable={fishyTransportResolvable}\n" +
                $"NetworkManagers={managers.Length}\n" +
                $"M56FriendSessions={sessions.Length}\n" +
                $"M56SteamBootstraps={bootstraps.Length}\n" +
                $"TransportConfigured={transportBound && maximumClients == 2}\n" +
                $"PeerToPeerConfigured={peerToPeer}\n" +
                $"FigureMotorFound={figureMotors.Length == 1} ({figureMotors.Length})\n" +
                $"RoleResolverFound={typeof(PrototypeRoleAuthorityResolver) != null}\n" +
                $"InkPainterAuthorityFound={roleAuthority != null}\n" +
                $"PalimpsestActions={palimpsest.Length}\n" +
                $"LivingGalleryActions={gallery.Length}\n" +
                $"DuplicateNetworkObjects={duplicateNetworkObjects}\n" +
                $"HardErrors={hardErrors}\n" +
                $"Warnings={warnings}\n\n" +
                $"MaximumClients={maximumClients} expected=2\n" +
                $"SpawnablePrefabCollection={(manager != null && manager.SpawnablePrefabs != null)}\n" +
                $"SteamAppId480={appIdOk}\n" +
                $"PalimpsestReplicatedActions={palimpsest.Count(x => x is INetworkReplicatedPainterWorldAction)}/{palimpsest.Length}\n" +
                $"LivingGalleryReplicatedActions={gallery.Count(x => x is INetworkReplicatedPainterWorldAction)}/{gallery.Length}\n" +
                $"TotalReplicatedWorldActions={replicatedWorldActions}\n\n" +
                "Role policy: host first = Figure, second client = Painter.\n" +
                "F1/F2 requests are server-owned atomic complementary swaps; " +
                "stale simultaneous requests are rejected/resynced.\n" +
                "Runtime acceptance requires two Windows PCs + two Steam accounts.";

            Debug.Log("[M56 Diagnose]\n" + report, manager);

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "M56.0 Friend 1v1 Steam",
                    report,
                    "Tamam");
            }

            return hardErrors == 0;
        }

        private static string ResolvePackageVersion(string packageName)
        {
            UnityEditor.PackageManager.PackageInfo[] packages =
                UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();

            UnityEditor.PackageManager.PackageInfo package = packages.FirstOrDefault(p =>
                p != null &&
                string.Equals(
                    p.name,
                    packageName,
                    StringComparison.Ordinal));

            return package != null
                ? package.version
                : "MISSING";
        }

        private static bool IsSceneObject(Component component)
        {
            return component != null &&
                   !EditorUtility.IsPersistent(component) &&
                   component.gameObject.scene.IsValid();
        }
    }
}
