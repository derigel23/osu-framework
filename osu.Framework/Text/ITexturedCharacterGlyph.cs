// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;

namespace osu.Framework.Text
{
    public interface ITexturedCharacterGlyph : ICharacterGlyph
    {
        /// <summary>
        /// The texture for this character.
        /// </summary>
        Texture Texture { get; }

        /// <summary>
        /// The width of the area that should be drawn.
        /// </summary>
        float Width { get; }

        /// <summary>
        /// The height of the area that should be drawn.
        /// </summary>
        float Height { get; }
    }
}