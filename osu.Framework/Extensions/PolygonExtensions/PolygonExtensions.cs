﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Extensions.PolygonExtensions
{
    public static class PolygonExtensions
    {
        /// <summary>
        /// Computes the axes for each edge in a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to compute the axes of.</param>
        /// <param name="normalize">Whether the normals should be normalized. Allows computation of the exact intersection point.</param>
        /// <returns>The axes of the polygon.</returns>
        public static Span<Vector2> GetAxes<TPolygon>(TPolygon polygon, bool normalize = false)
            where TPolygon : IPolygon
        {
            var axisVertices = polygon.GetAxisVertices();
            return getAxes(axisVertices, new Vector2[axisVertices.Length], normalize);
        }

        /// <summary>
        /// Computes the axes for each edge in a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to compute the axes of.</param>
        /// <param name="buffer">A buffer to be used as storage for the axes. Must have a length of at least the count of vertices in <paramref name="polygon"/>.</param>
        /// <param name="normalize">Whether the normals should be normalized. Allows computation of the exact intersection point.</param>
        /// <returns>The axes of the polygon. Returned as a slice of <paramref name="buffer"/>.</returns>
        public static Span<Vector2> GetAxes<TPolygon>(this TPolygon polygon, Span<Vector2> buffer, bool normalize = false)
            where TPolygon : IPolygon
            => getAxes(polygon.GetAxisVertices(), buffer, normalize);

        /// <summary>
        /// Computes the axes for a set of vertices.
        /// </summary>
        /// <param name="vertices">The vertices to compute the axes for.</param>
        /// <param name="buffer">A buffer to be used as storage for the axes. Must have a length of at least the count of <paramref name="vertices"/>.</param>
        /// <param name="normalize">Whether the normals should be normalized. Allows computation of the exact intersection point.</param>
        /// <returns>The axes represented by <paramref name="vertices"/>. Returned as a slice of <paramref name="buffer"/>.</returns>
        private static Span<Vector2> getAxes(ReadOnlySpan<Vector2> vertices, Span<Vector2> buffer, bool normalize = false)
        {
            if (buffer.Length < vertices.Length)
                throw new ArgumentException($"Axis buffer must have a length of {vertices.Length}, but was {buffer.Length}.", nameof(buffer));

            for (int i = 0; i < vertices.Length; i++)
            {
                // Construct an edge between two sequential points
                Vector2 v1 = vertices[i];
                Vector2 v2 = vertices[i == vertices.Length - 1 ? 0 : i + 1];
                Vector2 edge = v2 - v1;

                // Find the normal to the edge
                Vector2 normal = new Vector2(-edge.Y, edge.X);

                if (normalize)
                    normal = Vector2.Normalize(normal);

                buffer[i] = normal;
            }

            return buffer.Slice(0, vertices.Length);
        }

        /// <summary>
        /// Determines the minimum buffer size required to clip two polygons.
        /// </summary>
        /// <param name="clipPolygon">The polygon that will be used to clip.</param>
        /// <param name="subjectPolygon">The polygon that will be clipped.</param>
        /// <returns>The minimum buffer size required for <paramref name="clipPolygon"/> to clip <paramref name="subjectPolygon"/>.</returns>
        public static int GetClipBufferSize<TPolygon1, TPolygon2>(this TPolygon1 clipPolygon, TPolygon2 subjectPolygon)
            where TPolygon1 : IPolygon
            where TPolygon2 : IPolygon
        {
            if (clipPolygon is IConvexPolygon && subjectPolygon is IConvexPolygon)
            {
                // If both polygons are convex, there can only be at most 2 intersections for each edge of the subject
                return subjectPolygon.GetVertices().Length * 2;
            }

            // If both polygons are non-convex, each edge of one may intersect with each edge of the other, leading to at most n^2 vertices
            // For simplicity, the case where only one of the two is non-convex is also covered under this case
            return clipPolygon.GetVertices().Length * subjectPolygon.GetVertices().Length;
        }

        /// <summary>
        /// Clips a polygon by another.
        /// </summary>
        /// <param name="clipPolygon">The polygon that will clip the subject.</param>
        /// <param name="subjectPolygon">The polygon that will be clipped.</param>
        /// <returns>A clockwise-ordered set of vertices representing the result of clipping <paramref name="subjectPolygon"/> by <paramref name="clipPolygon"/>.</returns>
        public static Span<Vector2> Clip<TPolygon1, TPolygon2>(this TPolygon1 clipPolygon, TPolygon2 subjectPolygon)
            where TPolygon1 : IPolygon
            where TPolygon2 : IPolygon
            => clipPolygon.Clip(subjectPolygon, new Vector2[clipPolygon.GetClipBufferSize(subjectPolygon)]);

        /// <summary>
        /// Clips a polygon by another.
        /// </summary>
        /// <param name="clipPolygon">The polygon that will clip the subject.</param>
        /// <param name="subjectPolygon">The polygon that will be clipped.</param>
        /// <param name="buffer">The buffer to contain the clipped vertices. Must have a length of <see cref="GetClipBufferSize{TPolygon1,TPolygon2}"/>.</param>
        /// <returns>A clockwise-ordered set of vertices representing the result of clipping <paramref name="subjectPolygon"/> by <paramref name="clipPolygon"/>.</returns>
        public static Span<Vector2> Clip<TPolygon1, TPolygon2>(this TPolygon1 clipPolygon, TPolygon2 subjectPolygon, Span<Vector2> buffer)
            where TPolygon1 : IPolygon
            where TPolygon2 : IPolygon
        {
            if (buffer.Length < GetClipBufferSize(clipPolygon, subjectPolygon))
            {
                throw new ArgumentException($"Clip buffer must have a length of {GetClipBufferSize(clipPolygon, subjectPolygon)}, but was {buffer.Length}."
                                            + $"Use {nameof(GetClipBufferSize)} to calculate the size of the buffer.", nameof(buffer));
            }

            ReadOnlySpan<Vector2> subjectVertices = subjectPolygon.GetVertices();
            ReadOnlySpan<Vector2> clipVertices = clipPolygon.GetVertices();

            // Buffer is initially filled with the all of the subject's vertices
            subjectVertices.CopyTo(buffer);

            // Make sure that the subject vertices are clockwise-sorted
            Vector2Extensions.ClockwiseSort(buffer.Slice(0, subjectVertices.Length));

            // The edges of clip that the subject will be clipped against
            Span<Line> clipEdges = stackalloc Line[clipVertices.Length];

            // Joins consecutive vertices to form the clip edges
            // This is done via GetRotation() to avoid a secondary temporary storage
            if (Vector2Extensions.GetRotation(clipVertices) < 0)
            {
                for (int i = clipVertices.Length - 1, c = 0; i > 0; i--, c++)
                    clipEdges[c] = new Line(clipVertices[i], clipVertices[i - 1]);
                clipEdges[clipEdges.Length - 1] = new Line(clipVertices[0], clipVertices[clipVertices.Length - 1]);
            }
            else
            {
                for (int i = 0; i < clipVertices.Length - 1; i++)
                    clipEdges[i] = new Line(clipVertices[i], clipVertices[i + 1]);
                clipEdges[clipEdges.Length - 1] = new Line(clipVertices[clipVertices.Length - 1], clipVertices[0]);
            }

            // Number of vertices in the buffer that need to be tested against
            // This becomes the number of vertices in the resulting polygon after each clipping iteration
            int inputCount = subjectVertices.Length;

            // Temporary storage for the vertices from the buffer as the buffer gets altered
            Span<Vector2> inputVertices = stackalloc Vector2[buffer.Length];

            foreach (var ce in clipEdges)
            {
                if (inputCount == 0)
                    break;

                // Store the original vertices (buffer will get altered)
                buffer.CopyTo(inputVertices);

                int outputCount = 0;
                var startPoint = inputVertices[inputCount - 1];

                for (int i = 0; i < inputCount; i++)
                {
                    var endPoint = inputVertices[i];

                    if (ce.IsInRightHalfPlane(endPoint))
                    {
                        if (!ce.IsInRightHalfPlane(startPoint))
                            buffer[outputCount++] = ce.At(ce.IntersectWith(new Line(startPoint, endPoint)).distance);

                        buffer[outputCount++] = endPoint;
                    }
                    else if (ce.IsInRightHalfPlane(startPoint))
                        buffer[outputCount++] = ce.At(ce.IntersectWith(new Line(startPoint, endPoint)).distance);

                    startPoint = endPoint;
                }

                inputCount = outputCount;
            }

            return buffer.Slice(0, inputCount);
        }
    }
}
