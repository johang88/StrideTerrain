using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using StrideTerrain.TerrainSystem;
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Mathematics;
using Stride.Core.Collections;

namespace StrideTerrain.Splines
{
    public class SplineProcessor : EntityProcessor<SplineMeshComponent, SplineMeshRenderData>, IEntityComponentRenderProcessor
    {
        public VisibilityGroup VisibilityGroup { get; set; }

        protected override SplineMeshRenderData GenerateComponentData([NotNull] Entity entity, [NotNull] SplineMeshComponent component)
            => new SplineMeshRenderData();

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] SplineMeshComponent component, [NotNull] SplineMeshRenderData data)
        {
            base.OnEntityComponentRemoved(entity, component, data);

            data?.Dispose();
        }

        public override void Draw(RenderContext context)
        {
            base.Draw(context);

            var game = Services.GetService<IGame>();
            var graphicsContext = game.GraphicsContext;
            var graphicsDevice = Services.GetService<IGraphicsDeviceService>().GraphicsDevice;

            foreach (var pair in ComponentDatas)
            {
                var component = pair.Key;
                var data = pair.Value;

                try
                {
                    GenerateSplineMesh(component, data, graphicsContext, graphicsDevice);
                }
                catch
                {
                    // We get random exceptions when the editor reloads the assets, just swallow it for now
                }
            }
        }

        private void GenerateSplineMesh(SplineMeshComponent component, SplineMeshRenderData data, GraphicsContext graphicsContext, GraphicsDevice graphicsDevice)
        {
            // Validate spline
            var children = component.Entity.Transform.Children;
            if (component.Material == null || children.Count < 2 || component.Width <= 0.0f)
            {
                data.Dispose();
                return;
            }

            // Sync properties
            data.Model.Materials.Clear();
            data.Model.Materials.Add(component.Material);
            data.ModelComponent.IsShadowCaster = component.CastShadows;

            var points = GetPoints(component, children);

            var indexCount = 0;
            var vertexCount = 0;
            var vertexPositions = new FastList<Vector3>();

            var pp2 = Vector3.Zero;
            var pp4 = Vector3.Zero;

            float uv = 0.0f;

            data.Vertices.Clear();
            data.Indices.Clear();

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

                data.Vertices.Add(new VertexPositionNormalTangentColorTexture(p0, Vector3.UnitY, Vector3.UnitZ, color, new Vector2(0.0f, uvY)));
                data.Vertices.Add(new VertexPositionNormalTangentColorTexture(p1, Vector3.UnitY, Vector3.UnitZ, color, new Vector2(0.5f, uvY)));
                data.Vertices.Add(new VertexPositionNormalTangentColorTexture(p2, Vector3.UnitY, Vector3.UnitZ, color, new Vector2(1.0f, uvY)));

                vertexPositions.Add(p0);
                vertexPositions.Add(p1);

                if (i < points.Count - 1)
                {
                    data.Indices.Add(vertexCount + 0);
                    data.Indices.Add(vertexCount + 3);
                    data.Indices.Add(vertexCount + 1);

                    data.Indices.Add(vertexCount + 1);
                    data.Indices.Add(vertexCount + 3);
                    data.Indices.Add(vertexCount + 4);

                    data.Indices.Add(vertexCount + 1);
                    data.Indices.Add(vertexCount + 4);
                    data.Indices.Add(vertexCount + 2);

                    data.Indices.Add(vertexCount + 2);
                    data.Indices.Add(vertexCount + 4);
                    data.Indices.Add(vertexCount + 5);
                }

                vertexCount += 3;
                indexCount += 12;
            }

            // Upload mesh data
            if (data.VertexBuffer == null || data.VertexBuffer.ElementCount < data.Vertices.Count)
            {
                data.VertexBuffer = Stride.Graphics.Buffer.Vertex.New(graphicsDevice, data.Vertices.Items, GraphicsResourceUsage.Dynamic);
            }
            else
            {
                data.VertexBuffer.SetData(graphicsContext.CommandList, data.Vertices.Items);
            }

            if (data.IndexBuffer == null || data.IndexBuffer.ElementCount < data.Indices.Count)
            {
                data.IndexBuffer?.Dispose();
                data.IndexBuffer = Stride.Graphics.Buffer.Index.New(graphicsDevice, data.Indices.Items, GraphicsResourceUsage.Dynamic);
            }
            else
            {
                data.IndexBuffer.SetData(graphicsContext.CommandList, data.Indices.Items);
            }

            // Update mesh
            var mesh = data.Model.Meshes[0];
            mesh.Draw = new MeshDraw
            {
                PrimitiveType = Stride.Graphics.PrimitiveType.TriangleList,
                DrawCount = indexCount,
                IndexBuffer = new IndexBufferBinding(data.IndexBuffer, true, indexCount),
                VertexBuffers = new[] { new VertexBufferBinding(data.VertexBuffer, VertexPositionNormalTangentColorTexture.Layout, vertexCount) },
            };
            mesh.BoundingBox = BoundingBox.FromPoints(vertexPositions.Items);
            mesh.BoundingSphere = BoundingSphere.FromPoints(vertexPositions.Items);

            // This resets the model hierarchy and prevents issues, mainly in the editor
            data.ModelComponent.Model = null;
            data.ModelComponent.Model = data.Model;

            if (data.ModelComponent.Entity == null)
            {
                component.Entity.Add(data.ModelComponent);
            }
        }

        private FastList<Vector3> GetPoints(SplineMeshComponent component, FastCollection<TransformComponent> children)
        {
            // Setup the points
            var points = new FastList<Vector3>();
            foreach (var child in children)
            {
                points.Add(child.WorldMatrix.TranslationVector);
            }

            // Create evenly spaced points along the spline
            var evenlySpacedPoints = new FastList<Vector3>() { points[0] };
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

                    while (distanceSinceLastEvenPoint >= component.SegmentLength)
                    {
                        var overshootDistance = distanceSinceLastEvenPoint - component.SegmentLength;

                        var dir = (previousPoint - p);
                        dir.Normalize();

                        var newEvenlySpacedPoint = p + dir * overshootDistance;
                        evenlySpacedPoints.Add(newEvenlySpacedPoint);

                        distanceSinceLastEvenPoint = overshootDistance;

                        previousPoint = newEvenlySpacedPoint;
                    }

                    previousPoint = p;
                }
            }
            evenlySpacedPoints.Add(points[points.Count - 1]);

            return evenlySpacedPoints;
        }

        private void SetHeight(TerrainComponent terrain, float heightOffset, ref Vector3 p)
        {
            float terrainOffset = terrain.Size / 2.0f;
            p.Y = terrain.GetHeightAt(p.X + terrainOffset, p.Z + terrainOffset) + heightOffset;
        }
    }

    public class SplineMeshRenderData : IDisposable
    {
        public ModelComponent ModelComponent { get; set; } = new ModelComponent();
        public Model Model { get; set; } = new Model
        {
            new Mesh
            {
                Draw = new MeshDraw()
            }
        };

        public FastList<VertexPositionNormalTangentColorTexture> Vertices { get; set; } = new FastList<VertexPositionNormalTangentColorTexture>();
        public FastList<int> Indices { get; set; } = new FastList<int>();

        public Buffer<VertexPositionNormalTangentColorTexture> VertexBuffer { get; set; }
        public Buffer<int> IndexBuffer { get; set; }

        public void Dispose()
        {
            ModelComponent?.Entity?.Remove(ModelComponent);

            IndexBuffer?.Dispose();
            VertexBuffer?.Dispose();

            IndexBuffer = null;
            VertexBuffer = null;
        }
    }
}
