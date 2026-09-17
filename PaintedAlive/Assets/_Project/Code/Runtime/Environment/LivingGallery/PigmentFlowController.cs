using System;
using UnityEngine;

namespace PaintedAlive.Environment.LivingGallery
{
    [DisallowMultipleComponent]
    public sealed class PigmentFlowController : MonoBehaviour, ILivingGalleryResettable
    {
        [SerializeField] private PigmentChannel[] channels = Array.Empty<PigmentChannel>();

        public PigmentChannel[] Channels => channels;

        public void Configure(PigmentChannel[] configuredChannels)
        {
            channels = configuredChannels ?? Array.Empty<PigmentChannel>();
        }

        public void ResetLivingGalleryState()
        {
            if (channels == null)
                return;
            foreach (PigmentChannel channel in channels)
            {
                if (channel != null)
                    channel.SetFlowEnabled(true);
            }
        }
    }
}
