using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
namespace StrideTerrain.TerrainSystem
{
    /// <summary>
    /// Manages a single layer of vegetation. 
    /// Requires a model component with the desired model as well as an instancing component
    /// 
    /// Note: Instancing component should be added after the model component or game studio might crash next time you restart it.
    /// This is due to an ordering issue on initialization.
    /// </summary>
    [DataContract(nameof(TerrainVegetationComponent))]
    [Display("Terrain Vegetation", Expand = ExpandRule.Once)]
    [DefaultEntityComponentRenderer(typeof(TerrainVegetationProcessor))]
    public class TerrainVegetationComponent : StartupScript
    {
        [DataMember(10)] public TerrainComponent Terrain { get; set; }
        /// <summary>
        /// Mask to use when randomly placing instances
        /// </summary>
        [DataMember(30)] public Texture Mask { get; set; }
        /// <summary>
        /// Channel of the mask texture to use for placement
        /// </summary>
        [DataMember(40), DefaultValue(ColorChannel.R)] public ColorChannel MaskChannel { get; set; } = ColorChannel.R;
        /// <summary>
        /// A multiplier to the density provided by the mask channel
        /// </summary>
        [DataMember(50), DefaultValue(1.0f)] public float Density { get; set; } = 1.0f;
        [DataMember(60), DefaultValue(0.5f)] public float MinScale { get; set; } = 0.5f;
        [DataMember(70), DefaultValue(1.5f)] public float MaxScale { get; set; } = 1.5f;
        [DataMember(80), DefaultValue(0.0f)] public float MinSlope { get; set; } = 0.0f;
        [DataMember(90), DefaultValue(1.0f)] public float MaxSlope { get; set; } = 1.0f;
        [DataMember(100)] public int Seed { get; set; }

        /// <summary>
        /// Maximum distance the vegetation is visible
        /// </summary>
        [DataMember(120), DefaultValue(64.0f)]
        public float ViewDistance { get; set; } = 64.0f;

        /// <summary>
        /// Should distance scaling be used or not.
        /// If true then the models will fade ut by scaling to 0 when approaching the maximum view distance
        /// </summary>
        [DataMember(130), DefaultValue(true)]
        public bool UseDistanceScaling { get; set; } = true;
    }
}
