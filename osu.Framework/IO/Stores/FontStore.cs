﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Textures;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace osu.Framework.IO.Stores
{
    public class FontStore : TextureStore
    {
        private readonly List<GlyphStore> glyphStores = new List<GlyphStore>();

        private readonly List<FontStore> nestedFontStores = new List<FontStore>();

        private readonly Func<(string, char), Texture> cachedTextureLookup;

        /// <summary>
        /// A local cache to avoid string allocation overhead. Can be changed to (string,char)=>string if this ever becomes an issue,
        /// but as long as we directly inherit <see cref="TextureStore"/> this is a slight optimisation.
        /// </summary>
        private readonly ConcurrentDictionary<(string, char), Texture> namespacedTextureCache = new ConcurrentDictionary<(string, char), Texture>();

        public FontStore(IResourceStore<TextureUpload> store = null, float scaleAdjust = 100)
            : base(store, scaleAdjust: scaleAdjust)
        {
            cachedTextureLookup = t =>
            {
                var tex = Get(getTextureName(t.Item1, t.Item2));

                if (tex == null)
                    Logger.Log($"Glyph texture lookup for {getTextureName(t.Item1, t.Item2)} was unsuccessful.");

                return tex;
            };
        }

        /// <summary>
        /// Attempts to retrieve the texture of a character from a specified font and its associated spacing information.
        /// </summary>
        /// <param name="charName">The character to look up.</param>
        /// <param name="fontName">The font look for the character in.</param>
        /// <param name="glyph">The glyph retrieved, if it exists.</param>
        /// <returns>Whether or not a <see cref="CharacterGlyph"/> was able to be retrieved.</returns>
        public bool TryGetCharacter(string fontName, char charName, out CharacterGlyph glyph)
        {
            if (!tryGetCharacterGlyph(fontName, charName, out glyph))
                return false;

            glyph.Texture = namespacedTextureCache.GetOrAdd((fontName, charName), cachedTextureLookup);

            if (glyph.IsWhiteSpace)
            {
                glyph.Width = glyph.XAdvance;
                glyph.Height = 0;
            }
            else
            {
                glyph.Width = glyph.Texture.Width;
                glyph.Height = glyph.Texture.Height;
            }

            glyph.ApplyScaleAdjust(1 / ScaleAdjust);

            return true;
        }

        /// <summary>
        /// Retrieves the base height of a font containing a particular character.
        /// </summary>
        /// <param name="c">The charcter to search for.</param>
        /// <returns>The base height of the font.</returns>
        public float? GetBaseHeight(char c)
        {
            var glyphStore = getGlyphStore(string.Empty, c);

            return glyphStore?.GetBaseHeight() / ScaleAdjust;
        }

        /// <summary>
        /// Retrieves the base height of a font containing a particular character.
        /// </summary>
        /// <param name="fontName">The font to search for.</param>
        /// <returns>The base height of the font.</returns>
        public float? GetBaseHeight(string fontName)
        {
            var glyphStore = getGlyphStore(fontName);

            return glyphStore?.GetBaseHeight() / ScaleAdjust;
        }

        /// <summary>
        /// Retrieves the character information from this <see cref="FontStore"/>.
        /// </summary>
        /// <param name="charName">The character to look up.</param>
        /// <param name="fontName">The font look in for the character.</param>
        /// <param name="glyph">The found glyph.</param>
        /// <returns>Whether a matching <see cref="CharacterGlyph"/> was found. If a font name is not provided, gets the glyph from the first font store that supports it.</returns>
        private bool tryGetCharacterGlyph(string fontName, char charName, out CharacterGlyph glyph)
        {
            var glyphStore = getGlyphStore(fontName, charName);

            if (glyphStore == null)
            {
                glyph = default;
                return false;
            }

            glyph = glyphStore.GetCharacterInfo(charName);
            return true;
        }

        private string getTextureName(string fontName, char charName) => string.IsNullOrEmpty(fontName) ? charName.ToString() : $"{fontName}/{charName}";

        /// <summary>
        /// Retrieves a <see cref="GlyphStore"/> from this <see cref="FontStore"/> that matches a font and character.
        /// </summary>
        /// <param name="fontName">The font to look up the <see cref="GlyphStore"/> for.</param>
        /// <param name="charName">A character to look up in the <see cref="GlyphStore"/>.</param>
        /// <returns>The first available <see cref="GlyphStore"/> matches the name and contains the specified character. Null if not available.</returns>
        private GlyphStore getGlyphStore(string fontName, char? charName = null)
        {
            foreach (var store in glyphStores)
            {
                if (charName == null)
                {
                    if (store.FontName == fontName)
                        return store;
                }
                else
                {
                    if (store.ContainsTexture(getTextureName(fontName, charName.Value)))
                        return store;
                }
            }

            foreach (var store in nestedFontStores)
            {
                var nestedStore = store.getGlyphStore(fontName, charName);
                if (nestedStore != null)
                    return nestedStore;
            }

            return null;
        }

        protected override IEnumerable<string> GetFilenames(string name)
        {
            // extensions should not be used as they interfere with character lookup.
            yield return name;
        }

        public override void AddStore(IResourceStore<TextureUpload> store)
        {
            switch (store)
            {
                case FontStore fs:
                    nestedFontStores.Add(fs);
                    return;

                case GlyphStore gs:
                    glyphStores.Add(gs);
                    queueLoad(gs);
                    break;
            }

            base.AddStore(store);
        }

        private Task childStoreLoadTasks;

        /// <summary>
        /// Append child stores to a single threaded load task.
        /// </summary>
        private void queueLoad(GlyphStore store)
        {
            var previousLoadStream = childStoreLoadTasks;

            childStoreLoadTasks = Task.Run(async () =>
            {
                if (previousLoadStream != null)
                    await previousLoadStream;

                try
                {
                    Logger.Log($"Loading Font {store.FontName}...", level: LogLevel.Debug);
                    await store.LoadFontAsync();
                    Logger.Log($"Loaded Font {store.FontName}!", level: LogLevel.Debug);
                }
                catch (OperationCanceledException)
                {
                }
            });
        }

        public override void RemoveStore(IResourceStore<TextureUpload> store)
        {
            switch (store)
            {
                case FontStore fs:
                    nestedFontStores.Remove(fs);
                    return;

                case GlyphStore gs:
                    glyphStores.Remove(gs);
                    break;
            }

            base.RemoveStore(store);
        }

        public override Texture Get(string name)
        {
            var found = base.Get(name);

            if (found == null)
            {
                foreach (var store in nestedFontStores)
                    if ((found = store.Get(name)) != null)
                        break;
            }

            return found;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            glyphStores.ForEach(g => g.Dispose());
        }

        /// <summary>
        /// Contains the texture and associated spacing information for a character.
        /// </summary>
        public struct CharacterGlyph
        {
            /// <summary>
            /// The texture for this character.
            /// </summary>
            public Texture Texture { get; set; }

            /// <summary>
            /// The amount of space that should be given to the left of the character texture.
            /// </summary>
            public float XOffset { get; set; }

            /// <summary>
            /// The amount of space that should be given to the top of the character texture.
            /// </summary>
            public float YOffset { get; set; }

            /// <summary>
            /// The amount of space to advance the cursor by after drawing the texture.
            /// </summary>
            public float XAdvance { get; set; }

            public float Width { get; set; }

            public float Height { get; set; }

            private readonly GlyphStore containingStore;
            private readonly char character;

            private float scaleAdjust;

            public CharacterGlyph(char character, float xOffset, float yOffset, float xAdvance, GlyphStore containingStore)
            {
                this.containingStore = containingStore;
                this.character = character;

                Texture = null;
                XOffset = xOffset;
                YOffset = yOffset;
                XAdvance = xAdvance;
                Width = 0;
                Height = 0;

                scaleAdjust = 1;
            }

            /// <summary>
            /// Apply a scale adjust to metrics of this glyph.
            /// </summary>
            /// <param name="scaleAdjust">The adjustment to multiply all metrics by.</param>
            public void ApplyScaleAdjust(float scaleAdjust)
            {
                XOffset *= scaleAdjust;
                YOffset *= scaleAdjust;
                XAdvance *= scaleAdjust;
                Width *= scaleAdjust;
                Height *= scaleAdjust;

                this.scaleAdjust *= scaleAdjust;
            }

            public float GetKerning(CharacterGlyph lastGlyph) => containingStore.GetKerning(lastGlyph.character, character) * scaleAdjust;

            public bool IsWhiteSpace => Texture == null || char.IsWhiteSpace(character);
        }
    }
}
