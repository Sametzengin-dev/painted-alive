using System;
using PaintedAlive.Paint.Ink.StainHijack;
using UnityEditor;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupInkStainCliffFallMilestone
    {
        private const string ConfigPath =
            "Assets/_Project/Data/Ink/" +
            "M26_StainCreatureHijackConfig.asset";

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "27 - Enable Stain Cliff Fall")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M27 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                InkStainCreatureHijackController[] controllers =
                    UnityEngine.Object.FindObjectsByType<
                        InkStainCreatureHijackController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                InkStainHijackConfig config =
                    AssetDatabase.LoadAssetAtPath<
                        InkStainHijackConfig>(ConfigPath);

                if (controllers.Length != 1 || config == null)
                {
                    throw new InvalidOperationException(
                        "M27 çalışan tek M26 HijackController ve " +
                        "M26 config bekliyor. " +
                        $"Controllers={controllers.Length}, " +
                        $"Config={(config != null)}.");
                }

                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M27 Setup] Tamamlandı. Ele geçirilen yaratığı " +
                    "WASD ile zemin kenarından dışarı sür. Yaratık " +
                    "düşerken Leke son güvenli yüzeye çıkar.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/" +
            "27 - Diagnose Stain Cliff Fall")]
        public static void Diagnose()
        {
            InkStainCliffFallSequence[] falls =
                UnityEngine.Object.FindObjectsByType<
                    InkStainCliffFallSequence>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkStainCreatureHijackController[] controllers =
                UnityEngine.Object.FindObjectsByType<
                    InkStainCreatureHijackController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Debug.Log(
                "[M27 Diagnose] " +
                $"Controllers={controllers.Length}, " +
                $"FallSequences={falls.Length}, " +
                $"Playing={Application.isPlaying}");

            for (int i = 0; i < falls.Length; i++)
            {
                InkStainCliffFallSequence fall = falls[i];
                Debug.Log(
                    "[M27 Diagnose Fall] " +
                    $"Path={GetPath(fall.transform)}, " +
                    $"Falling={fall.IsFalling}, " +
                    $"Elapsed={fall.ElapsedSeconds:0.00}",
                    fall);
            }
        }

        private static string GetPath(Transform target)
        {
            if (target == null)
            {
                return "None";
            }

            string path = target.name;

            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }
    }
}
