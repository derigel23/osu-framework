// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Clippers;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Tests.Polygons
{
    [TestFixture]
    public class ConvexPolygonClipping
    {
        private static readonly Vector2 origin = Vector2.Zero;
        private static readonly Vector2 up_1 = new Vector2(0, 1);
        private static readonly Vector2 up_2 = new Vector2(0, 2);
        private static readonly Vector2 up_3 = new Vector2(0, 3);
        private static readonly Vector2 down_1 = new Vector2(0, -1);
        private static readonly Vector2 down_2 = new Vector2(0, -2);
        private static readonly Vector2 down_3 = new Vector2(0, -3);
        private static readonly Vector2 left_1 = new Vector2(-1, 0);
        private static readonly Vector2 left_2 = new Vector2(-2, 0);
        private static readonly Vector2 left_3 = new Vector2(-3, 0);
        private static readonly Vector2 right_1 = new Vector2(1, 0);
        private static readonly Vector2 right_2 = new Vector2(2, 0);
        private static readonly Vector2 right_3 = new Vector2(3, 0);

        private static object[] externalTestCases => new object[]
        {
            // Non-rotated
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { up_2, up_3, up_3 + right_1, up_2 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { right_2, right_2 + up_1, right_3 + up_1, right_3 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { down_1, down_1 + right_1, down_2 + right_1, down_2 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { left_2, left_2 + up_1, left_1 + up_1, left_1 } },
            // Rotated
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { up_1 + right_2, up_2 + right_1, up_3 + right_2, up_2 + right_3 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { up_1 + right_2, down_1 + right_3, down_2 + right_2, down_1 + right_1 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { down_1 + right_1, down_2 + right_2, down_3 + right_1, down_2 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { left_1 + up_1, down_2, down_3 + left_2, left_2 } },
        };

        [TestCaseSource(nameof(externalTestCases))]
        public void TestExternalPolygon(Vector2[] polygonVertices1, Vector2[] polygonVertices2)
        {
            var poly1 = new SimpleConvexPolygon(polygonVertices1);
            var poly2 = new SimpleConvexPolygon(polygonVertices2);

            Assert.That(new ConvexPolygonClipper(poly1, poly2).Clip().Length, Is.Zero);
            Assert.That(new ConvexPolygonClipper(poly2, poly1).Clip().Length, Is.Zero);

            Array.Reverse(polygonVertices1);
            Array.Reverse(polygonVertices2);

            Assert.That(new ConvexPolygonClipper(poly1, poly2).Clip().Length, Is.Zero);
            Assert.That(new ConvexPolygonClipper(poly2, poly1).Clip().Length, Is.Zero);
        }

        private static object[] subjectFullyContainedTestCases => new object[]
        {
            // Same polygon
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { origin, up_2, up_2 + right_2, right_2 } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { down_2, left_2, up_2, right_2 } },
            // Corners
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { origin, up_1, up_1 + right_1, right_1 } },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_1, up_2, up_2 + right_1, up_1 + right_1 } },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_1 + right_1, up_2 + right_1, up_2 + right_2, up_1 + right_2 } },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { right_1, up_1 + right_1, up_1 + right_2, right_2 } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { down_2, down_1, right_1, right_2 } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { left_2, left_1, down_1, down_2 } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { left_2, up_2, up_1, left_1 } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { up_2, right_2, right_1, up_1 } },
            // Padded
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { right_1 * 0.5f + up_1 * 0.5f, up_2 * 0.75f, up_2 * 0.75f + right_2 * 0.75f, right_2 * 0.75f } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { down_1, left_1, up_1, right_1 } },
            // Rotated
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_1 + right_1 * 0.5f, up_2 * 0.75f + right_1, up_1 + right_2 * 0.5f, up_1 * 0.5f + right_1 } },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_1, up_2 * 0.75f, up_2 + right_1 * 0.5f, up_2 + right_1 } },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { right_1, up_1 + right_2, up_1 * 0.5f + right_2, right_2 * 0.75f } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { left_1 + up_1, up_1 + right_1, down_1 + right_1, left_1 + down_1 } },
        };

        [TestCaseSource(nameof(subjectFullyContainedTestCases))]
        public void TestSubjectFullyContained(Vector2[] clipVertices, Vector2[] subjectVertices)
        {
            var clipPolygon = new SimpleConvexPolygon(clipVertices);
            var subjectPolygon = new SimpleConvexPolygon(subjectVertices);

            assertPolygonEquals(subjectPolygon, new SimpleConvexPolygon(new ConvexPolygonClipper(clipPolygon, subjectPolygon).Clip().ToArray()), false);

            Array.Reverse(clipVertices);
            Array.Reverse(subjectVertices);

            assertPolygonEquals(subjectPolygon, new SimpleConvexPolygon(new ConvexPolygonClipper(clipPolygon, subjectPolygon).Clip().ToArray()), true);
        }

        [TestCaseSource(nameof(subjectFullyContainedTestCases))]
        public void TestClipFullyContained(Vector2[] subjectVertices, Vector2[] clipVertices)
        {
            var clipPolygon = new SimpleConvexPolygon(clipVertices);
            var subjectPolygon = new SimpleConvexPolygon(subjectVertices);

            assertPolygonEquals(clipPolygon, new SimpleConvexPolygon(new ConvexPolygonClipper(clipPolygon, subjectPolygon).Clip().ToArray()), false);

            Array.Reverse(clipVertices);
            Array.Reverse(subjectVertices);

            assertPolygonEquals(clipPolygon, new SimpleConvexPolygon(new ConvexPolygonClipper(clipPolygon, subjectPolygon).Clip().ToArray()), true);
        }

        private static object[] generalClippingTestCases => new object[]
        {
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { left_1 + up_1, up_1 + right_1, down_1 + right_1, left_1 + down_1 }, new[] { origin, up_1, up_1 + right_1, right_1 }
            },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { left_1, up_1, right_1, down_1 }, new[] { origin, up_1, right_1 } },
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_1 + right_1, up_3 + right_1, up_3 + right_3, up_1 + right_3 },
                new[] { up_1 + right_1, up_2 + right_1, up_2 + right_2, up_1 + right_2 }
            },
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_2 + right_1, up_3 + right_2, up_2 + right_3, up_1 + right_2 }, new[] { up_2 + right_1, up_2 + right_2, up_1 + right_2 }
            },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { left_1 + up_1, up_3 + right_1, up_1 + right_1, origin }, new[] { up_2, up_2 + right_1, up_1 + right_1, origin } },
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { left_1 + up_1, up_1 + right_3, down_1 + right_3, left_1 + down_1 }, new[] { up_1, up_1 + right_2, right_2, origin }
            },
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { down_1 + left_1, up_3 + left_1, up_3 + right_1, down_1 + right_1 }, new[] { origin, up_2, up_2 + right_1, right_1 }
            },
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { down_1, up_1 + right_1, down_1 + right_2, down_2 + right_1 }, new[] { right_1 * 0.5f, up_1 + right_1, right_2 * 0.75f }
            },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { origin, up_3 + right_3, right_2 }, new[] { origin, up_2 + right_2, right_2 } },
            new object[] { new[] { up_2, right_2, down_2, left_2 }, new[] { left_1 + down_2, left_2 + down_2, up_2 + left_2, up_2 + left_1 }, new[] { up_1 + left_1, down_1 + left_1, left_2 } },
            new object[] { new[] { up_2, right_2, down_2, left_2 }, new[] { origin, down_2 + left_2, up_2 + left_2 }, new[] { origin, down_1 + left_1, left_2, up_1 + left_1 } },
            new object[] { new[] { up_2, right_2, down_2, left_2 }, new[] { origin, left_3, up_3 }, new[] { origin, left_2, up_2 } },
            new object[] { new[] { up_2, right_2, down_2, left_2 }, new[] { origin, left_3, up_3, right_3 }, new[] { origin, left_2, up_2, right_2 } },
            new object[]
            {
                new[] { left_1 + up_1, right_1 + up_1, down_1 + right_1, down_1 + left_1 }, new[] { up_2 * 0.75f, right_2 * 0.75f, down_2 * 0.75f, left_2 * 0.75f },
                new[]
                {
                    down_1 + left_1 * 0.5f,
                    down_1 * 0.5f + left_1,
                    up_1 * 0.5f + left_1,
                    up_1 + left_1 * 0.5f,
                    up_1 + right_1 * 0.5f,
                    up_1 * 0.5f + right_1,
                    down_1 * 0.5f + right_1,
                    down_1 + right_1 * 0.5f
                }
            },
            new object[]
            {
                new[] { up_1, right_1, left_1 }, new[] { up_1 + right_1 * 0.5f, down_1 + right_1 * 0.5f, down_1 + left_1 * 0.5f, up_1 + left_1 * 0.5f },
                new[] { up_1 * 0.5f + left_1 * 0.5f, up_1, up_1 * 0.5f + right_1 * 0.5f, right_1 * 0.5f, left_1 * 0.5f, }
            },
            new object[]
            {
                // Inverse of the above
                new[] { up_1 + right_1 * 0.5f, down_1 + right_1 * 0.5f, down_1 + left_1 * 0.5f, up_1 + left_1 * 0.5f }, new[] { up_1, right_1, left_1 },
                new[] { up_1 * 0.5f + left_1 * 0.5f, up_1, up_1 * 0.5f + right_1 * 0.5f, right_1 * 0.5f, left_1 * 0.5f, }
            },
            new object[]
            {
                new[] { up_1, right_1, down_1 + right_1, down_1 + left_1, left_1 }, new[] { left_1, up_1, up_1 + right_1, down_1 + right_1, down_1 },
                new[] { up_1, right_1, right_1 + down_1, down_1, left_1 }
            }
        };

        [TestCaseSource(nameof(generalClippingTestCases))]
        public void TestGeneralClipping(Vector2[] clipVertices, Vector2[] subjectVertices, Vector2[] resultingVertices)
        {
            var clipPolygon = new SimpleConvexPolygon(clipVertices);
            var subjectPolygon = new SimpleConvexPolygon(subjectVertices);

            assertPolygonEquals(new SimpleConvexPolygon(resultingVertices), new SimpleConvexPolygon(new ConvexPolygonClipper(clipPolygon, subjectPolygon).Clip().ToArray()), false);

            Array.Reverse(clipVertices);
            Array.Reverse(subjectVertices);

            // The expected polygon is never reversed
            assertPolygonEquals(new SimpleConvexPolygon(resultingVertices), new SimpleConvexPolygon(new ConvexPolygonClipper(clipPolygon, subjectPolygon).Clip().ToArray()), false);
        }

        private void assertPolygonEquals(IPolygon expected, IPolygon actual, bool reverse)
            => Assert.That(Vector2Extensions.GetRotation(actual.GetVertices()),
                reverse
                    ? Is.EqualTo(-Vector2Extensions.GetRotation(expected.GetVertices()))
                    : Is.EqualTo(Vector2Extensions.GetRotation(expected.GetVertices())));
    }
}
