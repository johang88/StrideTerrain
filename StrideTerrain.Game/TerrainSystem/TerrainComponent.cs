using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;


namespace StrideTerrain.TerrainSystem
{
    /// <summary>
    /// Terrain data used by the TerrainProcessor, just attach to an entity and you are good to go.
    /// The terrain processor uses a hidden ModelComponent in order to support picking in the editor,
    /// its important that the Entity with the TerrainComponent does not contain an existing ModelComponent
    /// as only one can exist per Entity.
    /// 
    /// Also note that the generated mesh is offset compared HeightfieldCollider so one of them has to be offset (0.5, 0, 0.5)
    /// </summary>
    [DataContract(nameof(TerrainComponent))]
    [Display("Terrain", Expand = ExpandRule.Once)]
    [DefaultEntityComponentRenderer(typeof(TerrainProcessor))]
    public class TerrainComponent : StartupScript
    {
        [DataMember(0)]
        public Material Material { get; set; }

        /// <summary>
        /// Height map asset, currently only short conversion type is supported. Make sure this is correctly set on the asset or 
        /// you will get a null exception.
        /// </summary>
        [DataMember(10)]
        public Heightmap Heightmap { get; set; }

        [DataMember(20)]
        public float Size { get; set; }

        [DataMember(30)]
        public bool CastShadows { get; set; }
    }
}
