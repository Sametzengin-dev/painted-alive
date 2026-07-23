using System;
using PaintedAlive.Figures;
using PaintedAlive.Figures.Tools;
using PaintedAlive.Paint.Ink.StainSabotage;
using PaintedAlive.Painters.Ink;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaintedAlive.EditorTools
{
    public static class SetupExclusiveFigureToolInputHotfix
    {
        [MenuItem(
            "Tools/Painted Alive/Milestones/25.1 - Fix Exclusive Figure Tool Input")]
        public static void Setup()
        {
            try
            {
                if (Application.isPlaying)
                {
                    throw new InvalidOperationException(
                        "M25.1 Setup Play Mode dışında çalıştırılmalıdır.");
                }

                FigureMotor figure =
    FindExactlyOne<FigureMotor>("FigureMotor");

FigureClarityState clarity =
    figure.GetComponent<FigureClarityState>();

if (clarity == null)
{
    throw new InvalidOperationException(
        "M25.1 FigureMotor üzerinde FigureClarityState bulamadı.");
}
                FigureToolLoadoutController loadout =
                    FindExactlyOne<FigureToolLoadoutController>(
                        "FigureToolLoadoutController");
                InkPainterRoleAuthority authority =
                    FindExactlyOne<InkPainterRoleAuthority>(
                        "InkPainterRoleAuthority");

                FindExactlyOne<InkStainSabotageController>(
                    "InkStainSabotageController");

                FigurePrimaryToolClarityGate[] gates =
                    UnityEngine.Object.FindObjectsByType<
                        FigurePrimaryToolClarityGate>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                if (gates.Length > 1)
                {
                    throw new InvalidOperationException(
                        "Sahnede birden fazla " +
                        "FigurePrimaryToolClarityGate var. " +
                        "Kopyaları temizleyip M25.1 Setup'ı yeniden çalıştır.");
                }

                FigurePrimaryToolClarityGate gate =
                    gates.Length == 1
                        ? gates[0]
                        : Undo.AddComponent<
                            FigurePrimaryToolClarityGate>(
                            clarity.gameObject);

                gate.Configure(clarity, loadout, authority);

                EditorUtility.SetDirty(gate);
                EditorSceneManager.MarkSceneDirty(
                    clarity.gameObject.scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[M25.1 Setup] Tamamlandı. Temiz/Lekeli/Bozulmuş " +
                    "Figürde E yalnız seçili ana aracı kullanır. " +
                    "Çözülüyor/Leke formunda ana araç yükü kapanır; " +
                    "tam Leke formunda E yalnız M25 sabotajına gider.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(
            "Tools/Painted Alive/Milestones/25.1 - Diagnose Exclusive Figure Tool Input")]
        public static void Diagnose()
        {
            FigurePrimaryToolClarityGate[] gates =
                UnityEngine.Object.FindObjectsByType<
                    FigurePrimaryToolClarityGate>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            FigureToolLoadoutController[] loadouts =
                UnityEngine.Object.FindObjectsByType<
                    FigureToolLoadoutController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            FigureClarityState[] clarityStates =
                UnityEngine.Object.FindObjectsByType<
                    FigureClarityState>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            InkPainterRoleAuthority[] authorities =
                UnityEngine.Object.FindObjectsByType<
                    InkPainterRoleAuthority>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            string clarity =
                clarityStates.Length == 1
                    ? clarityStates[0].CurrentLevel.ToString()
                    : "Unavailable";
            string role =
                authorities.Length == 1
                    ? authorities[0].CurrentRole.ToString()
                    : "Unavailable";
            string loadoutEnabled =
                loadouts.Length == 1
                    ? loadouts[0].enabled.ToString()
                    : "Unavailable";
            string gateState =
                gates.Length == 1
                    ? gates[0].LastState
                    : "Unavailable";

            Debug.Log(
                "[M25.1 Diagnose] " +
                $"Gates={gates.Length}, " +
                $"Loadouts={loadouts.Length}, " +
                $"ClarityStates={clarityStates.Length}, " +
                $"Authorities={authorities.Length}, " +
                $"Clarity={clarity}, " +
                $"Role={role}, " +
                $"LoadoutEnabled={loadoutEnabled}, " +
                $"GateState={gateState}, " +
                $"Playing={Application.isPlaying}");
        }

        private static T FindExactlyOne<T>(string label)
            where T : UnityEngine.Object
        {
            T[] found =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (found.Length != 1)
            {
                throw new InvalidOperationException(
                    $"M25.1 tek {label} bekliyor. " +
                    $"Bulunan={found.Length}.");
            }

            return found[0];
        }
    }
}
