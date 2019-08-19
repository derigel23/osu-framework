// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Caching;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;
using osuTK;

namespace osu.Framework.Text
{
    public class TruncatingTextBuilder : TextBuilder
    {
        /// <summary>
        /// The string to be displayed if the text exceeds the allowable text area.
        /// </summary>
        public string EllipsisString;

        private readonly ITexturedGlyphLookupStore store;
        private readonly FontUsage font;
        private readonly bool useFontSizeAsHeight;
        private readonly Vector2 spacing;

        public TruncatingTextBuilder(ITexturedGlyphLookupStore store, FontUsage font, float maxWidth, bool useFontSizeAsHeight = true, Vector2 startOffset = default, Vector2 spacing = default,
                                     List<TextBuilderGlyph> characterList = null)
            : base(store, font, maxWidth, useFontSizeAsHeight, startOffset, spacing, characterList)
        {
            this.store = store;
            this.font = font;
            this.useFontSizeAsHeight = useFontSizeAsHeight;
            this.spacing = spacing;
        }

        private bool widthExceededOnce;
        private bool addingEllipsis;

        protected override bool CanAddCharacters => base.CanAddCharacters && (!widthExceededOnce || addingEllipsis);

        protected override void OnWidthExceeded()
        {
            if (widthExceededOnce)
                return;

            widthExceededOnce = true;

            if (string.IsNullOrEmpty(EllipsisString))
                return;

            addingEllipsis = true;

            // Remove characters by backtracking until both of the following conditions are met:
            // 1. The ellipsis glyphs can be added without exceeding the text bounds
            // 2. The last character in the builder is not a whitespace (for visual niceness)
            // Or until there are no more non-ellipsis characters in the builder (if the ellipsis string is very long)

            do
            {
                RemoveLastCharacter();

                if (Characters.Count == 0)
                    break;
            } while (Characters[Characters.Count - 1].IsWhiteSpace() || !HasAvailableSpace(getEllipsisSize().X));

            AddText(EllipsisString);

            addingEllipsis = false;
        }

        private readonly Cached<Vector2> ellipsisSizeCache = new Cached<Vector2>();

        private Vector2 getEllipsisSize()
        {
            if (ellipsisSizeCache.IsValid)
                return ellipsisSizeCache.Value;

            var builder = new TextBuilder(store, font, float.MaxValue, useFontSizeAsHeight, Vector2.Zero, spacing)
            {
                NeverFixedWidthCharacters = NeverFixedWidthCharacters,
                FallbackCharacter = FallbackCharacter
            };

            builder.AddText(EllipsisString);

            return ellipsisSizeCache.Value = builder.TextSize;
        }
    }
}