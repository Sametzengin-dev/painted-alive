using Steamworks;
using UnityEngine;

namespace PaintedAlive.Networking.M56
{
    [DefaultExecutionOrder(-32000)]
    [DisallowMultipleComponent]
    public sealed class M56SteamBootstrap : MonoBehaviour
    {
        private static M56SteamBootstrap instance;
        private static bool initialized;
        private static string lastError = "Not initialized";
        private bool transportStopped;

        public static bool Ready => initialized;
        public static string LastError => lastError;

        public static ulong LocalSteamId
        {
            get
            {
                if (!initialized)
                    return 0;

                try
                {
                    return SteamUser.GetSteamID().m_SteamID;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static string PersonaName
        {
            get
            {
                if (!initialized)
                    return "Steam unavailable";

                try
                {
                    return SteamFriends.GetPersonaName();
                }
                catch
                {
                    return "Steam user";
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            initialized = false;
            lastError = "Not initialized";
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError(
                    "[M56 Steam] Duplicate bootstrap disabled. " +
                    "Keep exactly one M56SteamBootstrap.",
                    this);
                enabled = false;
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (initialized)
                return;

            try
            {
                initialized = SteamAPI.Init();
                lastError = initialized
                    ? "OK"
                    : "SteamAPI.Init returned false. Steam açık mı ve steam_appid.txt var mı?";
            }
            catch (System.Exception exception)
            {
                initialized = false;
                lastError = exception.Message;
            }

            if (initialized)
            {
                Debug.Log(
                    "[M56 Steam] Ready | " +
                    $"SteamID64={LocalSteamId} | " +
                    $"User={PersonaName}",
                    this);
            }
            else
            {
                Debug.LogError(
                    "[M56 Steam] Initialization failed: " + lastError,
                    this);
            }
        }

        private void Update()
        {
            if (initialized)
                SteamAPI.RunCallbacks();
        }

        private void OnApplicationQuit()
        {
            ShutdownTransport();
            ShutdownSteam();
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            ShutdownTransport();
            ShutdownSteam();
            instance = null;
        }

        private void ShutdownTransport()
        {
            if (transportStopped)
                return;

            transportStopped = true;

            var transport = GetComponent<global::FishySteamworks.FishySteamworks>();
            if (transport != null)
                transport.Shutdown();
        }

        private static void ShutdownSteam()
        {
            if (initialized)
            {
                SteamAPI.Shutdown();
                initialized = false;
            }
        }
    }
}
