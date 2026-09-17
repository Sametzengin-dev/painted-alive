using System;
using System.IO;
using System.Linq;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Managing.Transporting;
using FishySteamworks;
using PaintedAlive.Core.Prototypes;
using PaintedAlive.Environment.LivingGallery;
using PaintedAlive.Environment.Palimpsest;
using PaintedAlive.Figures;
using PaintedAlive.MatchFlow;
using PaintedAlive.Networking.M56;
using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Painters;
using PaintedAlive.Painters.Ink;
using PaintedAlive.Painters.World;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PaintedAlive.EditorTools.Networking.M56
{
    public static class SetupFriend1v1Steam_M56
    {
        private const string MenuRoot = "Painted Alive/M56.0/";
        private const string NetworkRootName = "__M56_FRIEND_NETWORK__";
        private const string PrefabObjectsPath =
            "Assets/DefaultPrefabObjects.asset";
        private const string SteamAppIdFile = "steam_appid.txt";
        private const string SteamAppId = "480";

        [MenuItem(MenuRoot + "2 - Setup Friend 1v1 Steam")]
        public static void Setup()
        {
            RequireEditMode();

            Scene activeScene = ResolveSetupScene();

            InkPainterRoleAuthority roleAuthority =
                FindExactlyOne<InkPainterRoleAuthority>();
            FigureMotor figureMotor =
                FindExactlyOne<FigureMotor>();
            PainterBrushController painterBrush =
                FindExactlyOne<PainterBrushController>();
            InkPainterNestController nestController =
                FindExactlyOne<InkPainterNestController>();

            PrototypeCoreMatchController coreMatch =
                FindOptional<PrototypeCoreMatchController>();
            PrototypeMatchController prototypeMatch =
                FindOptional<PrototypeMatchController>();

            NetworkManager[] existingManagers =
                UnityEngine.Object.FindObjectsByType<NetworkManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(IsSceneObject)
                .ToArray();

            if (existingManagers.Length > 1)
            {
                throw new InvalidOperationException(
                    "Scene'de birden fazla NetworkManager var. " +
                    "M56 Setup güvenli şekilde hangisini kullanacağını seçemez.");
            }

            GameObject networkRoot =
                existingManagers.Length == 1
                    ? existingManagers[0].gameObject
                    : GameObject.Find(NetworkRootName);

            if (networkRoot == null)
            {
                networkRoot = new GameObject(NetworkRootName);
                Undo.RegisterCreatedObjectUndo(
                    networkRoot,
                    "Create M56 Friend Network Root");
            }
            else if (existingManagers.Length == 1 &&
                     networkRoot.name != NetworkRootName)
            {
                Undo.RecordObject(networkRoot, "Rename M56 Network Root");
                networkRoot.name = NetworkRootName;
            }

            // FishNet initializes TransportManager during NetworkManager.Awake.
            // We add/configure the transport and TransportManager in edit mode so
            // Awake finds the exact Steam transport on the first frame.
            FishySteamworks.FishySteamworks steamTransport =
                GetOrAdd<FishySteamworks.FishySteamworks>(networkRoot);
            TransportManager transportManager =
                GetOrAdd<TransportManager>(networkRoot);

            transportManager.Transport = steamTransport;
            EditorUtility.SetDirty(transportManager);

            ConfigureFishySteamworks(steamTransport);

            NetworkManager networkManager =
                GetOrAdd<NetworkManager>(networkRoot);

            DefaultPrefabObjects prefabObjects =
                EnsureEmptyPrefabObjectsAsset();
            networkManager.SpawnablePrefabs = prefabObjects;
            EditorUtility.SetDirty(networkManager);

            M56SteamBootstrap steamBootstrap =
                GetOrAdd<M56SteamBootstrap>(networkRoot);
            M56FriendSession session =
                GetOrAdd<M56FriendSession>(networkRoot);

            session.Configure(
                networkManager,
                roleAuthority,
                figureMotor,
                painterBrush,
                nestController,
                coreMatch,
                prototypeMatch);

            EditorUtility.SetDirty(steamBootstrap);
            EditorUtility.SetDirty(session);

            // Make the project playable in Editor with Steam AppID 480. This is
            // test-only and never replaces the production Steam AppID.
            WriteSteamAppId(
                Path.GetFullPath(
                    Path.Combine(Application.dataPath, "..", SteamAppIdFile)));

            EditorSceneManager.MarkSceneDirty(activeScene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(activeScene))
            {
                throw new InvalidOperationException(
                    "M56 Setup aktif sahneyi kaydedemedi: " +
                    activeScene.path);
            }
            AssetDatabase.Refresh();

            Debug.Log(
                "[M56 Setup] COMPLETE\n" +
                "Transport=FishySteamworks Steam P2P/Relay\n" +
                "MaximumClients=2\n" +
                "InitialRoles=Host Figure / Joiner Painter\n" +
                "RoleSwap=Server-owned atomic complementary swap\n" +
                "SteamTestAppId=480\n" +
                "Next: M56.0 > 2B - Diagnose Friend 1v1 Steam",
                session);

            DiagnoseFriend1v1Steam_M56.Diagnose(
                showDialog: !Application.isBatchMode);
        }

        private static Scene ResolveSetupScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            bool hasFigure =
                UnityEngine.Object.FindFirstObjectByType<FigureMotor>(
                    FindObjectsInactive.Include) != null;

            if (activeScene.IsValid() && activeScene.isLoaded && hasFigure)
                return activeScene;

            string scenePath = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));

            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new InvalidOperationException(
                    "M56 Setup için enabled build scene bulunamadı.");
            }

            return EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single);
        }

        private static void ConfigureFishySteamworks(
            FishySteamworks.FishySteamworks transport)
        {
            SerializedObject serialized =
                new SerializedObject(transport);

            SetBool(serialized, "_peerToPeer", true);
            SetInt(serialized, "_maximumClients", 2);
            SetInt(serialized, "_port", 7770);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(transport);
        }

        private static DefaultPrefabObjects EnsureEmptyPrefabObjectsAsset()
        {
            DefaultPrefabObjects existing =
                AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>(
                    PrefabObjectsPath);

            if (existing != null)
                return existing;

            string directory =
                Path.GetDirectoryName(PrefabObjectsPath)
                    ?.Replace('\\', '/');

            if (!string.IsNullOrWhiteSpace(directory))
                EnsureAssetFolder(directory);

            DefaultPrefabObjects created =
                ScriptableObject.CreateInstance<DefaultPrefabObjects>();

            created.name = "DefaultPrefabObjects";
            AssetDatabase.CreateAsset(created, PrefabObjectsPath);
            AssetDatabase.SaveAssets();
            return created;
        }

        private static void EnsureAssetFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static void WriteSteamAppId(string absolutePath)
        {
            File.WriteAllText(
                absolutePath,
                SteamAppId + System.Environment.NewLine);
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T existing = target.GetComponent<T>();
            if (existing != null)
                return existing;

            return Undo.AddComponent<T>(target);
        }

        private static T FindExactlyOne<T>()
            where T : Component
        {
            T[] matches =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(IsSceneObject)
                .ToArray();

            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    typeof(T).Name +
                    " expected exactly 1 in loaded scene, found " +
                    matches.Length + ".");
            }

            return matches[0];
        }

        private static T FindOptional<T>()
            where T : Component
        {
            return UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(IsSceneObject);
        }

        private static bool IsSceneObject(Component component)
        {
            return component != null &&
                   !EditorUtility.IsPersistent(component) &&
                   component.gameObject.scene.IsValid();
        }

        private static void SetBool(
            SerializedObject serialized,
            string propertyName,
            bool value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
                throw new InvalidOperationException(
                    "FishySteamworks property missing: " + propertyName);

            property.boolValue = value;
        }

        private static void SetInt(
            SerializedObject serialized,
            string propertyName,
            int value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
                throw new InvalidOperationException(
                    "FishySteamworks property missing: " + propertyName);

            property.intValue = value;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "M56 Setup Play Mode kapalıyken çalıştırılmalıdır.");
            }
        }
    }
}
