using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;
using StrideTerrain.TerrainSystem;
using System.Collections.Generic;
namespace StrideTerrain.Splines
{
    [DataContract(nameof(SplineMeshComponent))]
    [Display("Spline Mesh", Expand = ExpandRule.Once)]
    [DefaultEntityComponentRenderer(typeof(SplineProcessor))]
    public class SplineMeshComponent : StartupScript
    {
        [DataMember] public float SegmentLength { get; set; } = 1.0f;
        [DataMember] public float Width { get; set; } = 2.0f;
        /// <summary>
        /// How "long" the uv segment is, ie how often it repeats in world units
        /// </summary>
        [DataMember] public float SegmentUVLength { get; set; } = 1.0f;

        [DataMember] public Material Material { get; set; }
        [DataMember] public bool CastShadows { get; set; } = false;

        [DataMember] public TerrainComponent Terrain { get; set; }
        [DataMember] public float HeightOffset { get; set; } = 0.1f;
    }
}
