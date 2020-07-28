using Stride.Core.Mathematics;
using Stride.Core.Collections;
using Stride.Engine;

namespace StrideTerrain.Splines
{
    public static class SplineEvaluator
    {
        public static void GetPoints(FastList<Vector3> points, float segmentLength, FastList<Vector3> output)
        {
            // Create evenly spaced points along the spline
            var previousPoint = points[0];
            var distanceSinceLastEvenPoint = 0.0f;

            for (var i = 0; i < points.Count - 1; i++)
            {
                var t = 0.0f;
                while (t < 1.0f)
                {
                    t += 0.1f;

                    var p0 = i > 0 ? points[i - 1] : points[0];
                    var p1 = points[i];
                    var p2 = points[i + 1];
                    var p3 = i < points.Count - 2 ? points[i + 2] : p2;

                    Vector3.CatmullRom(ref p0, ref p1, ref p2, ref p3, t, out var p);

                    Vector3.Distance(ref previousPoint, ref p, out var distance);
                    distanceSinceLastEvenPoint += distance;

                    while (distanceSinceLastEvenPoint >= segmentLength)
                    {
                        var overshootDistance = distanceSinceLastEvenPoint - segmentLength;

                        var dir = (previousPoint - p);
                        dir.Normalize();

                        var newEvenlySpacedPoint = p + dir * overshootDistance;
                        output.Add(newEvenlySpacedPoint);

                        distanceSinceLastEvenPoint = overshootDistance;

                        previousPoint = newEvenlySpacedPoint;
                    }

                    previousPoint = p;
                }
            }
            output.Add(points[points.Count - 1]);
        }

        public static FastList<Vector3> GetPoints(SplineMeshComponent component, FastCollection<TransformComponent> children)
        {
            // Setup the points
            var points = new FastList<Vector3>();
            foreach (var child in children)
            {
                points.Add(child.WorldMatrix.TranslationVector);
            }

            var output = new FastList<Vector3>();
            GetPoints(points, component.SegmentLength, output);

            return output;
        }
    }
}
