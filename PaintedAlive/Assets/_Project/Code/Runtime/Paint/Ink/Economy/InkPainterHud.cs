using PaintedAlive.Paint.Ink.GlyphLoadouts;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Paint.Ink.Economy
{
    [DisallowMultipleComponent]
    public sealed class InkPainterHud : MonoBehaviour
    {
        [SerializeField]
        private InkPainterEconomy economy;

        [SerializeField]
        private InkPainterNestController nestController;

        [SerializeField]
        private Text pigmentText;

        [SerializeField]
        private Text complexityText;

        [SerializeField]
        private Text stateText;

        [SerializeField]
        private Image pigmentFill;

        [SerializeField]
        private Image complexityFill;

        private float nextRefreshTime;

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + 0.1f;
            Refresh();
        }

        public void Configure(
            InkPainterEconomy targetEconomy,
            InkPainterNestController targetController,
            Text targetPigmentText,
            Text targetComplexityText,
            Text targetStateText,
            Image targetPigmentFill,
            Image targetComplexityFill)
        {
            economy = targetEconomy;
            nestController = targetController;
            pigmentText = targetPigmentText;
            complexityText = targetComplexityText;
            stateText = targetStateText;
            pigmentFill = targetPigmentFill;
            complexityFill = targetComplexityFill;
            Refresh();
        }

        private void Refresh()
        {
            if (economy == null || economy.Config == null)
            {
                return;
            }

            InkPainterEconomyConfig config = economy.Config;

            if (pigmentText != null)
            {
                pigmentText.text =
                    $"PİGMENT  {economy.CurrentPigment:0}/{config.PigmentCapacity:0}";
            }

            if (complexityText != null)
            {
                complexityText.text =
                    $"KARMAŞIKLIK  {economy.CurrentComplexity}/{config.MaximumComplexity}";
            }

            if (pigmentFill != null)
            {
                pigmentFill.fillAmount = economy.PigmentNormalized;
            }

            if (complexityFill != null)
            {
                complexityFill.fillAmount = economy.ComplexityNormalized;
            }

            if (stateText == null)
            {
                return;
            }

            if (economy.PossessionActive)
            {
                stateText.text =
                    $"SAHİPLENME  -{config.PossessionDrainPerSecond:0.#}/sn";
            }
            else if (nestController != null && nestController.IsCasting)
            {
                stateText.text = nestController.TargetValid
                    ? $"YUVA ESKİZİ  %{nestController.CastProgress * 100f:0}"
                    : nestController.LastResult.ToUpperInvariant();
            }
            else
            {
                InkGlyphLoadoutController loadouts =
                    InkGlyphLoadoutController.ActiveInstance;
                float cost = loadouts != null
                    ? loadouts.ActivePigmentCost
                    : config.NestPlacementCost;
                string name =
                    loadouts != null &&
                    loadouts.ActiveLoadout != null
                        ? loadouts.ActiveLoadout.DisplayName.ToUpperInvariant()
                        : "YUVA";
                stateText.text =
                    $"F7 BASILI TUT  •  {name} {cost:0} PİGMENT";
            }
        }
    }
}
