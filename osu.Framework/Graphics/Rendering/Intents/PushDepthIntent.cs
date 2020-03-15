// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Rendering.Intents
{
    public readonly struct PushDepthIntent : IIntent
    {
        public readonly DepthInfo DepthInfo;

        public PushDepthIntent(DepthInfo depthInfo)
        {
            DepthInfo = depthInfo;
        }
    }
}