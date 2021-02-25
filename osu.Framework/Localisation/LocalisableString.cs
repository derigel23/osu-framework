﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

#nullable enable

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A descriptor representing text that can be localised and formatted.
    /// </summary>
    public readonly struct LocalisableString : IEquatable<LocalisableString>, IEquatable<string>
    {
        /// <summary>
        /// The underlying data, can be <see cref="string"/>, <see cref="TranslatableString"/>, or <see cref="RomanisableString"/>.
        /// </summary>
        internal readonly object? Data;

        private LocalisableString(object data) => Data = data;

        // it's somehow common to call default(LocalisableString), and we should return empty string then.
        public override string ToString() => Data?.ToString() ?? string.Empty;

        public bool Equals(LocalisableString other) => Data == other.Data;

        public static implicit operator LocalisableString(string text) => new LocalisableString(text);
        public static implicit operator LocalisableString(TranslatableString translatable) => new LocalisableString(translatable);
        public static implicit operator LocalisableString(RomanisableString romanisable) => new LocalisableString(romanisable);

        public static bool operator ==(LocalisableString left, LocalisableString right) => left.Equals(right);
        public static bool operator !=(LocalisableString left, LocalisableString right) => !left.Equals(right);

        public static implicit operator string(LocalisableString localisable) => localisable.ToString();
        public bool Equals(string other) => ToString().Equals(other);

        public override bool Equals(object? obj)
        {
            if (obj is LocalisableString localisable)
                return Equals(localisable);

            if (obj is string str)
                return Equals(str);

            return false;
        }

        public override int GetHashCode() => Data?.GetHashCode() ?? 0;
    }
}
