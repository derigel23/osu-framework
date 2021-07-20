// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Abstract class for an audio mixer.
    /// </summary>
    public abstract class AudioMixer : AdjustableAudioComponent, IAudioMixer
    {
        public void Add(IAudioChannel channel)
        {
            channel.Mixer.Remove(channel);

            AddInternal(channel);

            channel.SetMixer(this);
        }

        public void Remove(IAudioChannel channel)
        {
            if (channel.Mixer != this)
                return;

            RemoveInternal(channel);

            channel.SetMixer(null);
        }

        protected abstract void AddInternal(IAudioChannel channel);

        protected abstract void RemoveInternal(IAudioChannel channel);
    }
}