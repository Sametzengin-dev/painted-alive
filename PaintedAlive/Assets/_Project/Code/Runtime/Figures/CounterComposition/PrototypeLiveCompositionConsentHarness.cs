using UnityEngine;

namespace PaintedAlive.Figures.CounterComposition
{
    [DefaultExecutionOrder(285)]
    [DisallowMultipleComponent]
    public sealed class PrototypeLiveCompositionConsentHarness :
        MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField]
        private PrototypeLiveCompositionController controller;

        [SerializeField]
        private Transform partnerProxyRoot;

        [SerializeField]
        private Renderer partnerProxyRenderer;

        [Header("Consent")]
        [SerializeField, Min(0.5f)]
        private float invitationSeconds = 6.0f;

        [SerializeField, Min(0.5f)]
        private float acceptedCommitWindowSeconds = 12.0f;

        [Header("Runtime Read Only")]
        [SerializeField]
        private PrototypeLiveCompositionConsentState state =
            PrototypeLiveCompositionConsentState.Idle;

        [SerializeField]
        private bool invitationPending;

        [SerializeField]
        private bool partnerConsentGranted;

        [SerializeField]
        private bool withdrawalRequested;

        [SerializeField]
        private int invitationCount;

        [SerializeField]
        private int partnerAcceptCount;

        [SerializeField]
        private int partnerWithdrawCount;

        [SerializeField]
        private int invitationExpireCount;

        [SerializeField]
        private int consentSessionCount;

        [SerializeField]
        private int releaseResetCount;

        [SerializeField]
        private float invitationRemainingSeconds;

        [SerializeField]
        private float acceptedCommitRemainingSeconds;

        [SerializeField]
        private int consentGeneration;

        [SerializeField]
        private int activeConsentGeneration;

        [SerializeField]
        private string lastAction =
            "Partner daveti bekleniyor.";

        private float invitationExpiresAt;
        private float acceptedCommitExpiresAt;
        private MaterialPropertyBlock propertyBlock;

        public PrototypeLiveCompositionConsentState State => state;
        public bool InvitationPending => invitationPending;
        public bool PartnerConsentGranted => partnerConsentGranted;
        public bool WithdrawalRequested => withdrawalRequested;
        public int InvitationCount => invitationCount;
        public int PartnerAcceptCount => partnerAcceptCount;
        public int PartnerWithdrawCount => partnerWithdrawCount;
        public int InvitationExpireCount => invitationExpireCount;
        public int ConsentSessionCount => consentSessionCount;
        public int ReleaseResetCount => releaseResetCount;
        public float InvitationRemainingSeconds =>
            invitationRemainingSeconds;
        public float AcceptedCommitRemainingSeconds =>
            acceptedCommitRemainingSeconds;
        public int ConsentGeneration => consentGeneration;
        public int ActiveConsentGeneration =>
            activeConsentGeneration;
        public string LastAction => lastAction;
        public string StateLabel => TranslateState(state);

        public bool RequiresPartnerConsent => true;
        public bool CanCommitComposition =>
            partnerConsentGranted &&
            !withdrawalRequested &&
            state ==
                PrototypeLiveCompositionConsentState.PartnerAccepted &&
            activeConsentGeneration ==
                consentGeneration;

        public bool LocalPartnerProxyOnly => true;
        public bool RealNetworkParticipantBound => false;
        public bool AutomaticNearbyPlayerBindingEnabled => false;
        public bool IndependentPartnerConsentRequired => true;
        public bool IndependentPartnerWithdrawalEnabled => true;
        public bool ConsentExpires => true;
        public bool ConsentConsumedPerComposition => true;
        public bool StaleConsentReusable => false;
        public bool AddsNewGameplayInputAction => false;
        public bool NetworkAuthorityEnabled => false;
        public bool LocalPrototypeAuthority => true;

        public void Configure(
            PrototypeLiveCompositionController configuredController,
            Transform configuredPartnerProxyRoot,
            Renderer configuredPartnerProxyRenderer)
        {
            controller = configuredController;
            partnerProxyRoot =
                configuredPartnerProxyRoot;
            partnerProxyRenderer =
                configuredPartnerProxyRenderer;

            BindController(
                configuredController);

            EnsurePropertyBlock();
            RefreshProxyVisual();
        }

        public void BindController(
            PrototypeLiveCompositionController configuredController)
        {
            controller =
                configuredController;
        }

        private void Awake()
        {
            EnsurePropertyBlock();
            RefreshProxyVisual();
        }

        private void Update()
        {
            RefreshTimers();
            RefreshProxyVisual();
        }

        public bool RequestInvitation(
            string source)
        {
            if (
                state ==
                    PrototypeLiveCompositionConsentState.Composing
            )
            {
                lastAction =
                    "Kompozisyon aktifken yeni davet açılamaz.";

                return false;
            }

            ResetConsentFlags(
                preserveGeneration: true);

            consentGeneration++;
            activeConsentGeneration =
                consentGeneration;

            invitationPending = true;
            partnerConsentGranted = false;
            withdrawalRequested = false;

            invitationExpiresAt =
                Time.unscaledTime +
                invitationSeconds;

            acceptedCommitExpiresAt = 0f;

            state =
                PrototypeLiveCompositionConsentState.InvitationPending;

            invitationCount++;

            lastAction =
                $"Partner daveti oluşturuldu • gen {consentGeneration}.";

            Debug.Log(
                "[M53.1 Partner Invitation]\n" +
                $"Source={source}\n" +
                $"Generation={consentGeneration}\n" +
                $"InvitationSeconds={invitationSeconds:F2}\n" +
                "IndependentPartnerConsentRequired=True\n" +
                "AutomaticNearbyPlayerBindingEnabled=False\n" +
                "AddsNewGameplayInputAction=False\n" +
                "NetworkAuthorityEnabled=False",
                this);

            return true;
        }

        public bool AcceptInvitationFromDevelopmentHarness()
        {
            RefreshTimers();

            if (
                !invitationPending ||
                state !=
                    PrototypeLiveCompositionConsentState.InvitationPending
            )
            {
                lastAction =
                    "Partner onayı reddedildi: aktif davet yok.";

                return false;
            }

            invitationPending = false;
            partnerConsentGranted = true;
            withdrawalRequested = false;

            acceptedCommitExpiresAt =
                Time.unscaledTime +
                acceptedCommitWindowSeconds;

            state =
                PrototypeLiveCompositionConsentState.PartnerAccepted;

            partnerAcceptCount++;

            lastAction =
                $"Partner daveti onayladı • gen {consentGeneration}.";

            Debug.Log(
                "[M53.1 Partner Consent Accepted]\n" +
                $"Generation={consentGeneration}\n" +
                $"CommitWindowSeconds={acceptedCommitWindowSeconds:F2}\n" +
                "FigureStillNeedsExplicitCommit=True\n" +
                "ConsentConsumedPerComposition=True\n" +
                "StaleConsentReusable=False",
                this);

            return true;
        }

        public bool WithdrawConsentFromDevelopmentHarness(
            string reason)
        {
            RefreshTimers();

            bool hasSomethingToWithdraw =
                invitationPending ||
                partnerConsentGranted ||
                state ==
                    PrototypeLiveCompositionConsentState.Composing;

            if (!hasSomethingToWithdraw)
            {
                lastAction =
                    "Partner çekilmesi: aktif davet/onay/kompozisyon yok.";

                return false;
            }

            invitationPending = false;
            partnerConsentGranted = false;
            withdrawalRequested = true;

            invitationExpiresAt = 0f;
            acceptedCommitExpiresAt = 0f;

            state =
                PrototypeLiveCompositionConsentState.Withdrawn;

            partnerWithdrawCount++;

            lastAction =
                string.IsNullOrWhiteSpace(reason)
                    ? "Partner onayını geri çekti."
                    : $"Partner onayını geri çekti: {reason}.";

            Debug.LogWarning(
                "[M53.1 Partner Consent Withdrawn]\n" +
                $"Reason={reason}\n" +
                $"Generation={consentGeneration}\n" +
                "IndependentPartnerWithdrawalEnabled=True\n" +
                "ControllerMustReleaseIfComposing=True",
                this);

            return true;
        }

        public void NotifyCompositionStarted()
        {
            if (!CanCommitComposition)
            {
                lastAction =
                    "CompositionStarted bildirimi geçerli çift onay olmadan geldi.";

                return;
            }

            invitationPending = false;
            withdrawalRequested = false;

            state =
                PrototypeLiveCompositionConsentState.Composing;

            consentSessionCount++;

            invitationExpiresAt = 0f;
            acceptedCommitExpiresAt = 0f;

            lastAction =
                $"Çift onay session {consentSessionCount} aktif.";
        }

        public void NotifyCompositionReleased(
            string reason)
        {
            releaseResetCount++;

            ResetConsentFlags(
                preserveGeneration: true);

            state =
                PrototypeLiveCompositionConsentState.Idle;

            lastAction =
                $"Session kapandı; yeni partner onayı gerekecek: {reason}.";
        }

        private void RefreshTimers()
        {
            float now =
                Time.unscaledTime;

            invitationRemainingSeconds =
                invitationPending
                    ? Mathf.Max(
                        0f,
                        invitationExpiresAt -
                        now)
                    : 0f;

            acceptedCommitRemainingSeconds =
                partnerConsentGranted &&
                state ==
                    PrototypeLiveCompositionConsentState.PartnerAccepted
                    ? Mathf.Max(
                        0f,
                        acceptedCommitExpiresAt -
                        now)
                    : 0f;

            if (
                invitationPending &&
                now >= invitationExpiresAt
            )
            {
                ExpireConsent(
                    "Invitation expired");

                return;
            }

            if (
                partnerConsentGranted &&
                state ==
                    PrototypeLiveCompositionConsentState.PartnerAccepted &&
                now >= acceptedCommitExpiresAt
            )
            {
                ExpireConsent(
                    "Accepted commit window expired");
            }
        }

        private void ExpireConsent(
            string reason)
        {
            invitationExpireCount++;

            ResetConsentFlags(
                preserveGeneration: true);

            state =
                PrototypeLiveCompositionConsentState.Expired;

            lastAction =
                $"{reason}; eski onay kullanılamaz.";

            Debug.Log(
                "[M53.1 Consent Expired]\n" +
                $"Reason={reason}\n" +
                $"Generation={consentGeneration}\n" +
                "StaleConsentReusable=False",
                this);
        }

        private void ResetConsentFlags(
            bool preserveGeneration)
        {
            invitationPending = false;
            partnerConsentGranted = false;
            withdrawalRequested = false;

            invitationExpiresAt = 0f;
            acceptedCommitExpiresAt = 0f;

            invitationRemainingSeconds = 0f;
            acceptedCommitRemainingSeconds = 0f;

            if (!preserveGeneration)
            {
                consentGeneration = 0;
                activeConsentGeneration = 0;
            }
        }

        private void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock =
                    new MaterialPropertyBlock();
            }
        }

        private void RefreshProxyVisual()
        {
            if (partnerProxyRoot != null)
            {
                float pulse =
                    1f +
                    Mathf.Sin(
                        Time.unscaledTime *
                        4f) *
                    0.035f;

                float scale =
                    state ==
                        PrototypeLiveCompositionConsentState.InvitationPending
                        ? pulse
                        : 1f;

                partnerProxyRoot.localScale =
                    new Vector3(
                        scale,
                        scale,
                        scale);
            }

            if (partnerProxyRenderer == null)
            {
                return;
            }

            EnsurePropertyBlock();

            Color color =
                GetStateColor();

            partnerProxyRenderer.GetPropertyBlock(
                propertyBlock);

            propertyBlock.SetColor(
                "_BaseColor",
                color);

            propertyBlock.SetColor(
                "_Color",
                color);

            partnerProxyRenderer.SetPropertyBlock(
                propertyBlock);
        }

        private Color GetStateColor()
        {
            switch (state)
            {
                case PrototypeLiveCompositionConsentState.InvitationPending:
                    return new Color(
                        0.95f,
                        0.70f,
                        0.22f,
                        1f);

                case PrototypeLiveCompositionConsentState.PartnerAccepted:
                    return new Color(
                        0.40f,
                        0.86f,
                        0.54f,
                        1f);

                case PrototypeLiveCompositionConsentState.Composing:
                    return new Color(
                        0.96f,
                        0.84f,
                        0.32f,
                        1f);

                case PrototypeLiveCompositionConsentState.Withdrawn:
                    return new Color(
                        0.88f,
                        0.30f,
                        0.24f,
                        1f);

                case PrototypeLiveCompositionConsentState.Expired:
                    return new Color(
                        0.38f,
                        0.32f,
                        0.30f,
                        1f);

                default:
                    return new Color(
                        0.48f,
                        0.48f,
                        0.48f,
                        1f);
            }
        }

        private static string TranslateState(
            PrototypeLiveCompositionConsentState value)
        {
            switch (value)
            {
                case PrototypeLiveCompositionConsentState.InvitationPending:
                    return "DAVET BEKLİYOR";

                case PrototypeLiveCompositionConsentState.PartnerAccepted:
                    return "PARTNER ONAYLI";

                case PrototypeLiveCompositionConsentState.Composing:
                    return "ORTAK AKTİF";

                case PrototypeLiveCompositionConsentState.Withdrawn:
                    return "GERİ ÇEKİLDİ";

                case PrototypeLiveCompositionConsentState.Expired:
                    return "SÜRESİ DOLDU";

                default:
                    return "BOŞ";
            }
        }
    }
}
