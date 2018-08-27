﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Concurrent;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.IO.Stores;
using System;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.Textures
{
    public class TextureStore : ResourceStore<RawTexture>
    {
        private readonly ConcurrentDictionary<string, AsyncLazy<TextureGL>> textureCache = new ConcurrentDictionary<string, AsyncLazy<TextureGL>>();

        private readonly All filteringMode;
        private readonly TextureAtlas atlas;

        /// <summary>
        /// Decides at what resolution multiple this texturestore is providing sprites at.
        /// ie. if we are providing high resolution (at 2x the resolution of standard 1366x768) sprites this should be 2.
        /// </summary>
        public float ScaleAdjust = 2;

        public TextureStore(IResourceStore<RawTexture> store = null, bool useAtlas = true, All filteringMode = All.Linear)
            : base(store)
        {
            this.filteringMode = filteringMode;
            AddExtension(@"png");
            AddExtension(@"jpg");

            if (useAtlas)
                atlas = new TextureAtlas(GLWrapper.MaxTextureSize, GLWrapper.MaxTextureSize, filteringMode: filteringMode);
        }

        private async Task<Texture> getTextureAsync(string name)
        {
            RawTexture raw = await base.GetAsync($@"{name}");
            if (raw == null) return null;

            Texture tex = atlas != null ? atlas.Add(raw.Width, raw.Height) : new Texture(raw.Width, raw.Height, filteringMode: filteringMode);
            tex.SetData(new TextureUpload(raw));

            return tex;
        }

        public new async Task<Texture> GetAsync(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var cachedTex = await textureCache.GetOrAdd(name, n =>
                //Laziness ensure we are only ever creating the texture once (and blocking on other access until it is done).
                new AsyncLazy<TextureGL>(async () => (await getTextureAsync(name))?.TextureGL, LazyThreadSafetyMode.ExecutionAndPublication)).Value;

            if (cachedTex == null) return null;

            //use existing TextureGL (but provide a new texture instance).
            var tex = new Texture(cachedTex)
            {
                ScaleAdjust = ScaleAdjust
            };

            return tex;
        }

        /// <summary>
        /// Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        public new virtual Texture Get(string name) => GetAsync(name).Result;

        public class AsyncLazy<T> : Lazy<Task<T>>
        {
            public AsyncLazy(Func<Task<T>> taskFactory, LazyThreadSafetyMode safetyMode)
                : base(() => Task.Factory.StartNew(taskFactory).Unwrap(), safetyMode)
            {
            }
        }
    }
}
