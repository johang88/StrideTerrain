using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrideTerrain.TerrainSystem
{
    public class TerrainVegetationProcessor : EntityProcessor<TerrainVegetationComponent, TerrainVegetationRenderData>, IEntityComponentRenderProcessor
    {
        public const int PageSize = 16;

        public VisibilityGroup VisibilityGroup { get; set; }

        private FastList<TerrainVegetationPage> _activesPages = new FastList<TerrainVegetationPage>();

        public TerrainVegetationProcessor()
            : base(typeof(ModelComponent), typeof(InstancingComponent))
        {
            // Run before the instancing processor
            Order = -101;
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] TerrainVegetationComponent component, [NotNull] TerrainVegetationRenderData data)
        {
            base.OnEntityComponentRemoved(entity, component, data);

            data?.Dispose();
        }

        protected override TerrainVegetationRenderData GenerateComponentData([NotNull] Entity entity, [NotNull] TerrainVegetationComponent component)
        {
            return new TerrainVegetationRenderData
            {
            };
        }

        public override void Draw(RenderContext context)
        {
            base.Draw(context);

            var camera = GetCamera();

            Dispatcher.ForEach(ComponentDatas, (pair) =>
            {
                var component = pair.Key;
                var renderData = pair.Value;

                ProcessComponent(component, renderData, camera);
            });
        }

        private void ProcessComponent(TerrainVegetationComponent component, TerrainVegetationRenderData renderData, CameraComponent camera)
        {
            if (component.Terrain == null || component.Terrain.Heightmap == null || component.Density <= 0.0f || component.Mask == null)
            {
                return;
            }

            var instancingComponent = component.Entity.Get<InstancingComponent>();
            if (!(instancingComponent.Type is InstancingUserArray instancingUserArray))
                return;

            UpdatePages(component.Terrain, component, renderData);
            CollectVisiblePages(renderData, component, camera);

            instancingUserArray.UpdateWorldMatrices(renderData.TransformData.Items, renderData.TransformData.Count);
        }

        /// <summary>
        /// Update and recreate a page if necessary
        /// </summary>
        private void UpdatePages(TerrainComponent terrain, TerrainVegetationComponent component, TerrainVegetationRenderData renderData)
        {
            if (renderData.Pages != null && renderData.MaskImage != null && !component.IsDirty)
                return;

            if (renderData.MaskImage == null || renderData.Mask != component.Mask)
            {
                renderData.Mask = component.Mask;
                renderData.MaskImage?.Dispose();

                // Get mask image data
                try
                {
                    var game = Services.GetService<IGame>();
                    var graphicsContext = game.GraphicsContext;
                    var commandList = graphicsContext.CommandList;
                    renderData.MaskImage = component.Mask.GetDataAsImage(commandList);
                }
                catch
                {
                    // Image probably not loaded yet .. try again next frame :)
                    return;
                }
            }

            // Cache render data so we won't need to recreate pages
            var mask = renderData.MaskImage.PixelBuffer[0];
            var maskChannel = (int)component.MaskChannel;

            // Calculate terrain center offset
            var terrainOffset = terrain.Size / 2.0f;

            RasterizeBlockingSplines(terrain, component, mask, maskChannel, terrainOffset);

            // Create vegetation pages
            var rng = new Random(component.Seed);
            var pagesPerRow = (int)terrain.Size / PageSize;

            var instancesPerRow = (int)(PageSize * component.Density);
            var distancePerInstance = PageSize / (float)instancesPerRow;

            renderData.Pages = new TerrainVegetationPage[pagesPerRow * pagesPerRow];
            var scaleRange = component.MaxScale - component.MinScale;

            for (var pz = 0; pz < pagesPerRow; pz++)
            {
                for (var px = 0; px < pagesPerRow; px++)
                {
                    var radius = PageSize * 0.5f;

                    var pagePosition = new Vector3(px * PageSize - terrainOffset, 0, pz * PageSize - terrainOffset);

                    var page = new TerrainVegetationPage();
                    renderData.Pages[pz * pagesPerRow + px] = page;

                    for (var iz = 0; iz < instancesPerRow; iz++)
                    {
                        for (var ix = 0; ix < instancesPerRow; ix++)
                        {
                            var position = pagePosition;

                            position.X += ix * distancePerInstance;
                            position.Z += iz * distancePerInstance;

                            position.X += (float)rng.NextDouble() * distancePerInstance * 2.0f - distancePerInstance;
                            position.Z += (float)rng.NextDouble() * distancePerInstance * 2.0f - distancePerInstance;

                            position.Y = terrain.GetHeightAt(position.X, position.Z);

                            var tx = (int)((position.X + terrainOffset) / terrain.Size * mask.Width);
                            var ty = (int)((position.Z + terrainOffset) / terrain.Size * mask.Height);

                            if (tx < 0 || tx >= mask.Width || ty < 0 || ty >= mask.Height)
                                continue;

                            var maskDensity = mask.GetPixel<Color>(tx, ty)[maskChannel] / 255.0;
                            if (rng.NextDouble() > maskDensity)
                                continue;

                            var normal = terrain.GetNormalAt(position.X, position.Z);
                            var slope = 1.0f - Math.Abs(normal.Y);
                            if (slope < component.MinSlope || slope > component.MaxSlope)
                                continue;

                            var scale = (float)rng.NextDouble() * scaleRange + component.MinScale;

                            var rotation = Quaternion.RotationAxis(Vector3.UnitY, (float)rng.NextDouble() * MathUtil.TwoPi) * Quaternion.BetweenDirections(Vector3.UnitY, normal);

                            var scaling = new Vector3(scale);

                            Matrix.Transformation(ref scaling, ref rotation, ref position, out var transformation);

                            page.Instances.Add(transformation);
                        }
                    }

                    page.WorldPosition = pagePosition + new Vector3(radius, 0, radius);
                }
            }

            component.IsDirty = false;
        }

        private static void RasterizeBlockingSplines(TerrainComponent terrain, TerrainVegetationComponent component, PixelBuffer mask, int maskChannel, float terrainOffset)
        {
            foreach (var spline in component.BlockingSplines)
            {
                var vertices = new FastList<Splines.VertexPositionNormalTangentColorTexture>();
                var indices = new FastList<int>();
                Splines.SplineMeshBuilder.CreateSplineMesh(spline, vertices, indices);

                for (var i = 0; i < indices.Count; i += 3)
                {
                    var vt1f = vertices[indices[i + 0]].Position;
                    var vt2f = vertices[indices[i + 1]].Position;
                    var vt3f = vertices[indices[i + 2]].Position;

                    vt1f = (vt1f + terrainOffset) / terrain.Size * mask.Width;
                    vt2f = (vt2f + terrainOffset) / terrain.Size * mask.Width;
                    vt3f = (vt3f + terrainOffset) / terrain.Size * mask.Width;

                    // Just ignore Y axis, works well enoguh for splines but if we 
                    // support generic volumes in the future then we might want to project it a bit more properly
                    // or maybe just do an offscreen render pass on the gpu ...
                    var vt1 = new Int2((int)vt1f.X, (int)vt1f.Z);
                    var vt2 = new Int2((int)vt2f.X, (int)vt2f.Z);
                    var vt3 = new Int2((int)vt3f.X, (int)vt3f.Z);

                    // Calculate bounding box
                    var maxX = Math.Max(vt1.X, Math.Max(vt2.X, vt3.X));
                    var minX = Math.Min(vt1.X, Math.Min(vt2.X, vt3.X));
                    var maxY = Math.Max(vt1.Y, Math.Max(vt2.Y, vt3.Y));
                    var minY = Math.Min(vt1.Y, Math.Min(vt2.Y, vt3.Y));

                    // Triangle intersection
                    var vs1 = vt2 - vt1;
                    var vs2 = vt3 - vt1;

                    for (var x = minX; x <= maxX; x++)
                    {
                        for (var y = minY; y < maxY; y++)
                        {
                            var q = new Int2(x - vt1.X, y - vt1.Y);

                            var s = (float)CrossProduct(q, vs2) / CrossProduct(vs1, vs2);
                            var t = (float)CrossProduct(vs1, q) / CrossProduct(vs1, vs2);

                            if (s >= 0 && t >= 0 && (s + t) <= 1)
                            {
                                var currentPixel = mask.GetPixel<Color>(x, y);
                                currentPixel[maskChannel] = 0;

                                mask.SetPixel<Color>(x, y, currentPixel);
                            }
                        }
                    }
                }
            }
        }

        private static int CrossProduct(Int2 v1, Int2 v2)
            => v1.X * v2.Y - v1.Y * v2.X;

        private void CollectVisiblePages(TerrainVegetationRenderData renderData, TerrainVegetationComponent component, CameraComponent camera)
        {
            renderData.TransformData.Clear();

            if (camera == null || renderData.Pages == null)
                return;

            var cameraPosition = GetCameraPosition(camera);
            cameraPosition.Y = 0.0f; // Only cull in xz plane

            var maxPageDistance = component.ViewDistance + PageSize;

            _activesPages.Clear();
            for (var i = 0; i < renderData.Pages.Length; i++)
            {
                var page = renderData.Pages[i];
                if (page.Instances == null) // Skip uninitialized pages
                    continue;

                var distance = (cameraPosition - page.WorldPosition).Length();
                if (distance < maxPageDistance)
                    _activesPages.Add(page);
            }

            // Reset camera position for individual instance culling
            cameraPosition = GetCameraPosition(camera);

            float maxDistance = component.ViewDistance;
            float minDistance = maxDistance * 0.8f;
            float distanceRange = maxDistance - minDistance;

            // TODO: concurrency??? That would probably be a good thing here
            var maxInstanceDistanceSquared = component.ViewDistance * component.ViewDistance;
            foreach (var page in _activesPages)
            {
                for (var p = 0; p < page.Instances.Count; p++)
                {
                    var distance = (cameraPosition - page.Instances[p].TranslationVector).LengthSquared();
                    //if (distance < maxInstanceDistanceSquared)
                    {
                        var worldMatrix = page.Instances[p];

                        if (component.UseDistanceScaling)
                        {
                            // Fade out the mesh by scaling it, this could be done in the shader for more speeeed
                            var distanceToCamera = Math.Max(0.0f, (cameraPosition - worldMatrix.TranslationVector).Length() - minDistance);
                            var relativeScale = Math.Min(1.0f, distanceToCamera / distanceRange);

                            var distanceScale = (float)MathUtil.Lerp(1.0f, 0.0f, Math.Pow(relativeScale, 2.0f));
                            var scale = Matrix.Scaling(distanceScale);

                            renderData.TransformData.Add(scale * worldMatrix);
                        }
                        else
                        {
                            renderData.TransformData.Add(worldMatrix);
                        }
                    }
                }
            }
        }

        private Vector3 GetCameraPosition(CameraComponent camera)
        {
            var viewMatrix = camera.ViewMatrix;
            viewMatrix.Invert();

            var cameraPosition = viewMatrix.TranslationVector;

            return cameraPosition;
        }

        /// <summary>
        /// Try to get the main camera, this can probably be done waaaaaaay better
        /// Contains a work around to get stuff working in the editor
        /// 
        /// Might not be needed if we switch to some kind of render feature instead 
        /// but will leave for now as we only really need to support the main camera 
        /// and it works ... usually
        /// </summary>
        private CameraComponent GetCamera()
        {
            var sceneSystem = Services.GetService<SceneSystem>();

            CameraComponent camera = null;
            if (sceneSystem.GraphicsCompositor.Cameras.Count == 0)
            {
                // The compositor wont have any cameras attached if the game is running in editor mode
                // Search through the scene systems until the camera entity is found
                // This is what you might call "A Hack"
                foreach (var system in sceneSystem.Game.GameSystems)
                {
                    if (system is SceneSystem editorSceneSystem)
                    {
                        foreach (var entity in editorSceneSystem.SceneInstance.RootScene.Entities)
                        {
                            if (entity.Name == "Camera Editor Entity")
                            {
                                camera = entity.Get<CameraComponent>();
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                camera = sceneSystem.GraphicsCompositor.Cameras[0].Camera;
            }

            return camera;
        }
    }

    public class TerrainVegetationRenderData : IDisposable
    {
        public FastList<Matrix> TransformData { get; set; } = new FastList<Matrix>();
        public TerrainVegetationPage[] Pages { get; set; }

        public Texture Mask { get; set; }
        public Image MaskImage { get; set; }

        public void Dispose()
        {
            MaskImage?.Dispose();
            MaskImage = null;
        }
    }

    public class TerrainVegetationPage
    {
        public Vector3 WorldPosition;
        public FastList<Matrix> Instances = new FastList<Matrix>();

        public override string ToString()
             => WorldPosition.ToString();
    }
}
