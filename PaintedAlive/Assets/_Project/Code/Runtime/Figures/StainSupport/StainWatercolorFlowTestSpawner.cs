using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace PaintedAlive.Figures.StainSupport
{
    [DisallowMultipleComponent]
    public sealed class StainWatercolorFlowTestSpawner : MonoBehaviour
    {
        private const BindingFlags ReflectionFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        [SerializeField] private MonoBehaviour watercolorDebugSpawner;

        [Header("Debug Spawn Lifecycle")]
        [Tooltip(
            "Legacy M34 automation. Keep disabled for normal gameplay. " +
            "Manual F8 / context-menu spawning remains available for testing.")]
        [SerializeField] private bool allowAutomaticTestSpawn;

        [SerializeField] private bool spawnOnPlayModeStart = false;
        [SerializeField, Min(0f)] private float spawnDelay = 0.4f;

        private bool attempted;

        public void Configure(MonoBehaviour debugSpawner)
        {
            watercolorDebugSpawner = debugSpawner;
        }

        private IEnumerator Start()
        {
            // M55.9.6: Milestone 34's automatic test stream must never leak
            // into a normal match merely because an old scene serialized
            // spawnOnPlayModeStart=true. Both explicit debug opt-ins are now
            // required.
            if (!allowAutomaticTestSpawn ||
                !spawnOnPlayModeStart ||
                attempted)
            {
                yield break;
            }

            attempted = true;

            if (spawnDelay > 0f)
            {
                yield return new WaitForSeconds(spawnDelay);
            }

            TrySpawnExistingM13Flow();
        }

        [ContextMenu("Debug/Spawn Existing M13 Watercolor Flow")]
        public void TrySpawnExistingM13Flow()
        {
            if (watercolorDebugSpawner == null)
            {
                watercolorDebugSpawner = FindDebugSpawner();
            }

            if (watercolorDebugSpawner == null)
            {
                Debug.LogWarning(
                    "[Milestone 34] M13 WatercolorFlowDebugSpawner bulunamadı. " +
                    "Play Mode'da F8 ile mevcut M13 akışını oluştur.",
                    this);

                return;
            }

            MethodInfo spawnMethod = FindSpawnMethod(watercolorDebugSpawner.GetType());
            if (spawnMethod == null)
            {
                Debug.LogWarning(
                    "[Milestone 34] M13 debug spawner üzerinde güvenli, parametresiz " +
                    "akış oluşturma metodu bulunamadı. F8 ile akışı elle oluştur.",
                    watercolorDebugSpawner);

                return;
            }

            try
            {
                spawnMethod.Invoke(watercolorDebugSpawner, null);
                Debug.Log(
                    "[Milestone 34] Mevcut M13 debug spawner ile test akışı oluşturuldu.",
                    watercolorDebugSpawner);
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogWarning(
                    "[Milestone 34] Otomatik M13 test akışı oluşturulamadı. " +
                    $"F8 ile elle oluşturabilirsin. Sebep: {exception.InnerException?.Message}",
                    watercolorDebugSpawner);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[Milestone 34] Otomatik M13 test akışı oluşturulamadı. " +
                    $"F8 ile elle oluşturabilirsin. Sebep: {exception.Message}",
                    watercolorDebugSpawner);
            }
        }

        public bool AllowAutomaticTestSpawn => allowAutomaticTestSpawn;
        public bool SpawnOnPlayModeStart => spawnOnPlayModeStart;

        public void DisableAutomaticTestSpawn()
        {
            allowAutomaticTestSpawn = false;
            spawnOnPlayModeStart = false;
        }

        private MonoBehaviour FindDebugSpawner()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null &&
                    behaviour.GetType().Name.Equals(
                        "WatercolorFlowDebugSpawner",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static MethodInfo FindSpawnMethod(Type type)
        {
            MethodInfo[] methods = type.GetMethods(ReflectionFlags);
            MethodInfo best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.IsSpecialName || method.GetParameters().Length != 0)
                {
                    continue;
                }

                string name = method.Name;
                int score = 0;

                if (name.IndexOf("Spawn", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 30;
                }

                if (name.IndexOf("Flow", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 20;
                }

                if (name.IndexOf("Watercolor", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 15;
                }

                if (name.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 5;
                }

                if (name.IndexOf("Try", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 2;
                }

                if (score > bestScore)
                {
                    best = method;
                    bestScore = score;
                }
            }

            return bestScore >= 30 ? best : null;
        }
    }
}
