using Stride.Core.Mathematics;
using Stride.Core.Collections;
using StrideTerrain.TerrainSystem;

namespace StrideTerrain.Splines
{
    public static class SplineMeshBuilder
    {
        public static void CreateSplineMesh(SplineMeshComponent component, FastList<VertexPositionNormalTangentColorTexture> vertices, FastList<int> indices)
        {
            var children = component.Entity.Transform.Children;
            if (children.Count < 2 || component.Width <= 0.0f)
            {
                return;
            }

            var points = SplineEvaluator.GetPoints(component, children);

            var indexCount = 0;
            var vertexCount = 0;

            vertices.Clear();
            indices.Clear();

            // Align points to terrain if needed
            var terrain = component.Terrain;
            if (terrain != null)
            {
                for (var i = 0; i < points.Count; i++)
                {
                    SetHeight(terrain, component.HeightOffset, ref points.Items[i]);
                }
            }

            // Generate spline mesh
            float totalDistance = 0.0f;
            var halfWidth = component.Width * 0.5f;
            for (var i = 0; i < points.Count; i++)
            {
                // Calculate forward direction
                var forward = Vector3.Zero;
                if (i < points.Count - 1)
                {
                    forward += points[i + 1] - points[i];
                }
                if (i > 0)
                {
                    forward += points[i] - points[i - 1];
                }
                forward.Normalize();

                var right = Vector3.Cross(forward, Vector3.UnitY);
                var left = -right;

                var p0 = points[i] + left * halfWidth;
                var p1 = points[i];
                var p2 = points[i] + right * halfWidth;

                // Align to terrain if needed
                if (terrain != null)
                {
                    SetHeight(terrain, component.HeightOffset, ref p0);
                    SetHeight(terrain, component.HeightOffset, ref p1);
                    SetHeight(terrain, component.HeightOffset, ref p2);
                }

                // Calculate UV coordinates
                float uvY = 0.0f;
                if (i > 0)
                {
                    var distance = (points[i] - points[i - 1]).Length();
                    totalDistance += distance;

                    uvY = totalDistance / component.SegmentUVLength;
                }

                // Calculate color
                var color = Color.White;

                // Alpha fades out at edges
                if (i == 0 || i == points.Count - 1)
                    color.A = 0;

                // Store forward direction in RGB
                var forwardBiasedAndScaled = ((forward + 1.0f) / 2.0f) * 255.0f;
                color.R = (byte)forwardBiasedAndScaled.X;
                color.G = (byte)forwardBiasedAndScaled.Y;
                color.B = (byte)forwardBiasedAndScaled.Z;

                vertices.Add(new VertexPositionNormalTangentColorTexture(p0, Vector3.UnitY, Vector3.UnitZ, color, new Vector2(0.0f, uvY)));
                vertices.Add(new VertexPositionNormalTangentColorTexture(p1, Vector3.UnitY, Vector3.UnitZ, color, new Vector2(0.5f, uvY)));
                vertices.Add(new VertexPositionNormalTangentColorTexture(p2, Vector3.UnitY, Vector3.UnitZ, color, new Vector2(1.0f, uvY)));

                if (i < points.Count - 1)
                {
                    indices.Add(vertexCount + 0);
                    indices.Add(vertexCount + 3);
                    indices.Add(vertexCount + 1);

                    indices.Add(vertexCount + 1);
                    indices.Add(vertexCount + 3);
                    indices.Add(vertexCount + 4);

                    indices.Add(vertexCount + 1);
                    indices.Add(vertexCount + 4);
                    indices.Add(vertexCount + 2);

                    indices.Add(vertexCount + 2);
                    indices.Add(vertexCount + 4);
                    indices.Add(vertexCount + 5);
                }

                vertexCount += 3;
                indexCount += 12;
            }
        }

        private static void SetHeight(TerrainComponent terrain, float heightOffset, ref Vector3 p)
        {
            p.Y = terrain.GetHeightAt(p.X, p.Z) + heightOffset;
        }
    }
}
