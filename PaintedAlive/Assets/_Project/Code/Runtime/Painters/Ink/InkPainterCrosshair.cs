using PaintedAlive.Paint.Ink.Economy;
using PaintedAlive.Paint.Ink.Possession;
using UnityEngine;
using UnityEngine.UI;

namespace PaintedAlive.Painters.Ink
{
    [DisallowMultipleComponent]
    public sealed class InkPainterCrosshair : MonoBehaviour
    {
        [SerializeField]
        private InkPainterRoleAuthority roleAuthority;

        [SerializeField]
        private InkPainterNestController nestController;

        [SerializeField]
        private InkPossessionController possessionController;

        [SerializeField]
        private Image[] crosshairSegments =
            System.Array.Empty<Image>();

        [SerializeField]
        private Text stateText;

        private void Awake()
        {
            HideCrosshairGraphics();
        }

        private void OnEnable()
        {
            HideCrosshairGraphics();
        }

        private void LateUpdate()
        {
            HideCrosshairGraphics();
        }

        public void Configure(
            InkPainterRoleAuthority authority,
            InkPainterNestController targetNestController,
            InkPossessionController targetPossessionController,
            Image[] segments,
            Text targetStateText)
        {
            roleAuthority = authority;
            nestController = targetNestController;
            possessionController = targetPossessionController;
            crosshairSegments =
                segments ?? System.Array.Empty<Image>();
            stateText = targetStateText;
            HideCrosshairGraphics();
        }

        private void HideCrosshairGraphics()
        {
            for (int i = 0; i < crosshairSegments.Length; i++)
            {
                Image segment = crosshairSegments[i];

                if (segment != null && segment.enabled)
                {
                    segment.enabled = false;
                }
            }

            if (stateText != null && stateText.enabled)
            {
                stateText.enabled = false;
            }
        }
    }
}
