﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Audio
{
    /// <summary>
    /// A collection of audio components which need central property control.
    /// </summary>
    public class AudioCollectionManager<T> : AdjustableAudioComponent, IBassAudio
        where T : AdjustableAudioComponent
    {
        protected List<T> Items = new List<T>();

        public void AddItem(T item)
        {
            RegisterItem(item);
            AddItemToList(item);
        }

        public void AddItemToList(T item)
        {
            EnqueueAction(delegate
            {
                if (Items.Contains(item)) return;

                Items.Add(item);
            });
        }

        public void RegisterItem(T item)
        {
            EnqueueAction(() => item.BindAdjustments(this));
        }

        public void UnregisterItem(T item)
        {
            EnqueueAction(() => item.UnbindAdjustments(this));
        }

        public virtual void UpdateDevice(int deviceIndex)
        {
            foreach (var item in Items.OfType<IBassAudio>())
                item.UpdateDevice(deviceIndex);
        }

        protected override void UpdateChildren()
        {
            base.UpdateChildren();

            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];

                if (!item.IsAlive)
                {
                    Items.RemoveAt(i--);
                    continue;
                }

                item.Update();
            }
        }

        public override void Dispose()
        {
            // we need to queue disposal of our Items before enqueueing the main dispose.
            foreach (var i in Items)
                i.Dispose();

            base.Dispose();
        }
    }
}
