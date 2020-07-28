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
using Stride.Core.Threading;

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

            Dispatcher.ForEach(ComponentDatas, (pair) =>
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
            });
        }

        private void GenerateSplineMesh(SplineMeshComponent component, SplineMeshRenderData data, GraphicsContext graphicsContext, GraphicsDevice graphicsDevice)
        {
            // Sync properties
            data.Model.Materials.Clear();
            data.Model.Materials.Add(component.Material);
            data.ModelComponent.IsShadowCaster = component.CastShadows;

            SplineMeshBuilder.CreateSplineMesh(component, data.Vertices, data.Indices);
            if (data.Indices.Count == 0)
            {
                data.Dispose();
                return; // Spline was not valid
            }

            // Get positions for bounding meshes
            var vertexPositions = new FastList<Vector3>();
            foreach (var vertex in data.Vertices)
            {
                vertexPositions.Add(vertex.Position);
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
                DrawCount = data.Indices.Count,
                IndexBuffer = new IndexBufferBinding(data.IndexBuffer, true, data.Indices.Count),
                VertexBuffers = new[] { new VertexBufferBinding(data.VertexBuffer, VertexPositionNormalTangentColorTexture.Layout, data.VertexBuffer.ElementCount) },
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
