﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using SharpFNT;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.IO.Stores
{
    public class GlyphStore : IResourceStore<TextureUpload>
    {
        private readonly string assetName;

        public readonly string FontName;

        private const float default_size = 96;

        private readonly ResourceStore<byte[]> store;

        protected BitmapFont Font => completionSource.Task.Result;

        private readonly TimedExpiryCache<int, TextureUpload> texturePages = new TimedExpiryCache<int, TextureUpload>();

        private readonly TaskCompletionSource<BitmapFont> completionSource = new TaskCompletionSource<BitmapFont>();

        private Task fontLoadTask;

        public GlyphStore(ResourceStore<byte[]> store, string assetName = null)
        {
            this.store = new ResourceStore<byte[]>(store);

            this.store.AddExtension("fnt");
            this.store.AddExtension("bin");

            this.assetName = assetName;

            FontName = assetName?.Split('/').Last();
        }

        public Task LoadFontAsync() => fontLoadTask ?? (fontLoadTask = Task.Factory.StartNew(() =>
        {
            try
            {
                BitmapFont font;
                using (var s = store.GetStream($@"{assetName}"))
                    font = BitmapFont.FromStream(s, FormatHint.Binary, false);

                completionSource.SetResult(font);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Couldn't load font asset from {assetName}.");
                completionSource.SetResult(null);
                throw;
            }
        }, TaskCreationOptions.PreferFairness));

        /// <summary>
        /// Gets the information for the specified character.
        /// </summary>
        /// <param name="c">The character to retrieve the information for.</param>
        /// <returns>The information for the specified character, without the texture.</returns>
        public FontStore.CharacterGlyph GetCharacterInfo(char c)
        {
            var character = Font.GetCharacter(c);
            return new FontStore.CharacterGlyph(c, xOffset: character.XOffset, yOffset: character.YOffset, xAdvance: character.XAdvance, containingStore: this);
        }

        public int GetKerning(char left, char right) => Font.GetKerningAmount(left, right);

        public int GetBaseHeight() => Font.Common.Base;

        /// <summary>
        /// Gets whether or not the specified texture is contained inside this GlyphStore.
        /// </summary>
        /// <param name="name">The name of the texture to look up.</param>
        /// <returns>Whether or not the specified texture is contained inside this GlyphStore.</returns>
        public bool ContainsTexture(string name) =>
            (name.Length == 1 || name.StartsWith($@"{FontName}/", StringComparison.Ordinal)) && Font.Characters.ContainsKey(name.Last());

        public TextureUpload Get(string name)
        {
            if (!ContainsTexture(name))
                return null;

            if (!Font.Characters.TryGetValue(name.Last(), out Character c))
                return null;

            return loadCharacter(c);
        }

        public virtual async Task<TextureUpload> GetAsync(string name)
        {
            if (!ContainsTexture(name))
                return null;

            if (!(await completionSource.Task).Characters.TryGetValue(name.Last(), out Character c))
                return null;

            return loadCharacter(c);
        }

        private TextureUpload loadCharacter(Character c)
        {
            var page = getTexturePage(c.Page);
            loadedGlyphCount++;

            int width = c.Width;
            int height = c.Height;

            var image = new Image<Rgba32>(width, height);

            var pixels = image.GetPixelSpan();
            var span = page.Data;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = span[(c.Y + y) * page.Width + c.X + x];
                }
            }

            return new TextureUpload(image);
        }

        private TextureUpload getTexturePage(int texturePage)
        {
            if (!texturePages.TryGetValue(texturePage, out TextureUpload t))
            {
                loadedPageCount++;
                using (var stream = store.GetStream($@"{assetName}_{texturePage.ToString().PadLeft((Font.Pages.Count - 1).ToString().Length, '0')}.png"))
                    texturePages.Add(texturePage, t = new TextureUpload(stream));
            }

            return t;
        }

        public Stream GetStream(string name) => throw new NotSupportedException();

        public IEnumerable<string> GetAvailableResources() => Font.Characters.Keys.Select(k => $"{FontName}/{(char)k}");

        private int loadedPageCount;
        private int loadedGlyphCount;

        public override string ToString() => $@"GlyphStore({assetName}) LoadedPages:{loadedPageCount} LoadedGlyphs:{loadedGlyphCount}";

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                texturePages.Dispose();
            }
        }

        ~GlyphStore()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
