using System.Collections;
using PaintedAlive.Paint;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintedAlive.Figures.Tools
{
    [DisallowMultipleComponent]
    public sealed class PaletteKnifeImpactFeedback : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private OilPaintFeedbackService feedbackService;

        [SerializeField]
        private Transform toolVisual;

        [Header("Tool Kick")]
        [SerializeField, Min(0.01f)]
        private float kickDuration = 0.14f;

        [SerializeField, Min(0f)]
        private float kickDistance = 0.08f;

        [SerializeField]
        private Vector3 kickRotation =
            new Vector3(-7f, 2f, -3f);

        [Header("Gamepad Rumble")]
        [SerializeField]
        private bool enableGamepadRumble = true;

        [SerializeField, Range(0f, 1f)]
        private float lowFrequencyRumble = 0.18f;

        [SerializeField, Range(0f, 1f)]
        private float highFrequencyRumble = 0.42f;

        private Vector3 restingLocalPosition;
        private Quaternion restingLocalRotation;
        private Coroutine impactRoutine;

        private void Awake()
        {
            if (feedbackService == null)
            {
                feedbackService =
                    FindFirstObjectByType<
                        OilPaintFeedbackService>();
            }

            CacheRestingPose();
        }

        public void PlayCutResult(
            Vector3 position,
            Vector3 surfaceNormal,
            OilStrokeRuntime stroke,
            bool succeeded,
            float effectiveGapWidth)
        {
            if (!succeeded)
            {
                feedbackService?.PlayBlocked(position);
                StartImpact(0.35f);
                return;
            }

            float stateIntensity =
                stroke != null
                    ? stroke.State switch
                    {
                        OilStrokeState.Wet => 0.8f,
                        OilStrokeState.Drying => 1f,
                        OilStrokeState.Dry => 1.2f,
                        _ => 1f
                    }
                    : 1f;

            float widthIntensity =
                Mathf.InverseLerp(
                    0.25f,
                    2f,
                    effectiveGapWidth);

            float intensity =
                Mathf.Lerp(0.75f, 1.25f, widthIntensity) *
                stateIntensity;

            feedbackService?.PlayKnifeCut(
                position,
                surfaceNormal,
                intensity);

            StartImpact(intensity);
        }

        public void PlayBlocked(
            Vector3 position,
            Vector3 surfaceNormal)
        {
            feedbackService?.PlayBlocked(position);
            StartImpact(0.3f);
        }

        public void PlayOutOfRange(
            Vector3 position,
            Vector3 surfaceNormal)
        {
            feedbackService?.PlayBlocked(
                transform.position);

            StartImpact(0.18f);
        }

        public void PlayMiss()
        {
            StartImpact(0.12f);
        }

        private void StartImpact(float intensity)
        {
            if (impactRoutine != null)
            {
                StopCoroutine(impactRoutine);
            }

            RestorePose();

            impactRoutine =
                StartCoroutine(
                    AnimateImpact(
                        Mathf.Clamp(intensity, 0.1f, 1.5f)));
        }

        private IEnumerator AnimateImpact(float intensity)
        {
            Gamepad gamepad = Gamepad.current;

            if (enableGamepadRumble && gamepad != null)
            {
                gamepad.SetMotorSpeeds(
                    Mathf.Clamp01(
                        lowFrequencyRumble * intensity),
                    Mathf.Clamp01(
                        highFrequencyRumble * intensity));
            }

            float elapsed = 0f;

            while (elapsed < kickDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float normalizedTime =
                    Mathf.Clamp01(elapsed / kickDuration);

                float pulse =
                    Mathf.Sin(normalizedTime * Mathf.PI);

                if (toolVisual != null)
                {
                    toolVisual.localPosition =
                        restingLocalPosition +
                        Vector3.back *
                        (kickDistance * intensity * pulse);

                    toolVisual.localRotation =
                        restingLocalRotation *
                        Quaternion.Euler(
                            kickRotation *
                            (intensity * pulse));
                }

                yield return null;
            }

            RestorePose();

            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(0f, 0f);
            }

            impactRoutine = null;
        }

        private void CacheRestingPose()
        {
            if (toolVisual == null)
            {
                return;
            }

            restingLocalPosition =
                toolVisual.localPosition;

            restingLocalRotation =
                toolVisual.localRotation;
        }

        private void RestorePose()
        {
            if (toolVisual == null)
            {
                return;
            }

            toolVisual.localPosition =
                restingLocalPosition;

            toolVisual.localRotation =
                restingLocalRotation;
        }

        private void OnDisable()
        {
            if (impactRoutine != null)
            {
                StopCoroutine(impactRoutine);
                impactRoutine = null;
            }

            RestorePose();

            if (Gamepad.current != null)
            {
                Gamepad.current.SetMotorSpeeds(0f, 0f);
            }
        }

        private void OnValidate()
        {
            kickDuration = Mathf.Max(0.01f, kickDuration);
            kickDistance = Mathf.Max(0f, kickDistance);
            lowFrequencyRumble = Mathf.Clamp01(lowFrequencyRumble);
            highFrequencyRumble = Mathf.Clamp01(highFrequencyRumble);
        }
    }
}
