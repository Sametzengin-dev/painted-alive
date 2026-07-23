using PaintedAlive.Painters.Ink;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Paint.Ink.GlyphLoadouts
{
    [DisallowMultipleComponent]
    public sealed class InkGlyphLoadoutHud : MonoBehaviour
    {
        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private InkGlyphLoadoutController loadoutController;

        [SerializeField]
        private GameObject panelRoot;

        [SerializeField]
        private Text loadoutText;

        [SerializeField]
        private Image accentBar;

        private float nextRefreshTime;

        private void Update()
        {
            bool visible = roleAuthority != null &&
                roleAuthority.IsInkPainter;

            if (panelRoot != null &&
                panelRoot.activeSelf != visible)
            {
                panelRoot.SetActive(visible);
            }

            if (!visible || Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + 0.1f;
            Refresh();
        }

        public void Configure(
            InkPainterRoleAuthority authority,
            InkGlyphLoadoutController controller,
            GameObject targetPanel,
            Text targetText,
            Image targetAccentBar)
        {
            roleAuthority = authority;
            loadoutController = controller;
            panelRoot = targetPanel;
            loadoutText = targetText;
            accentBar = targetAccentBar;
            Refresh();
        }

        private void Refresh()
        {
            InkGlyphLoadoutDefinition loadout =
                loadoutController != null
                    ? loadoutController.ActiveLoadout
                    : null;

            if (loadout == null)
            {
                return;
            }

            if (loadoutText != null)
            {
                loadoutText.text =
                    $"G  {loadout.DisplayName.ToUpperInvariant()}  •  " +
                    $"{loadout.ShortDescription}\n" +
                    $"{loadout.PigmentCost:0} PİGMENT  •  " +
                    $"{loadoutController.ActiveComplexityCost} KARMAŞIKLIK";
            }

            if (accentBar != null)
            {
                accentBar.color = loadout.AccentColor;
            }
        }
    }
}
