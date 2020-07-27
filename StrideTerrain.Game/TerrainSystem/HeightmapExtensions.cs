using Stride.Core.Mathematics;
using Stride.Physics;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrideTerrain.TerrainSystem
{
    public static class HeightmapExtensions
    {
        public static Vector3 GetNormal(this Heightmap heightmap, int x, int y)
        {
            var heightL = GetTerrainHeight(heightmap, x - 1, y);
            var heightR = GetTerrainHeight(heightmap, x + 1, y);
            var heightD = GetTerrainHeight(heightmap, x, y - 1);
            var heightU = GetTerrainHeight(heightmap, x, y + 1);

            var normal = new Vector3(heightL - heightR, 2.0f, heightD - heightU);
            normal.Normalize();

            return normal;
        }

        public static Vector3 GetTangent(this Heightmap heightmap, int x, int z)
        {
            var flip = 1;
            var here = new Vector3(x, GetTerrainHeight(heightmap, x, z), z);
            var left = new Vector3(x - 1, GetTerrainHeight(heightmap, x - 1, z), z);
            if (left.X < 0.0f)
            {
                flip *= -1;
                left = new Vector3(x + 1, GetTerrainHeight(heightmap, x + 1, z), z);
            }

            left -= here;

            var tangent = left * flip;
            tangent.Normalize();

            return tangent;
        }

        public static bool IsValidCoordinate(this Heightmap heightmap, int x, int y)
            => x >= 0 && x < heightmap.Size.X && y >= 0 && y < heightmap.Size.Y;

        public static int GetHeightIndex(this Heightmap heightmap, int x, int y)
            => y * heightmap.Size.X + x;

        public static float GetTerrainHeight(this Heightmap heightmap, int x, int y)
        {
            if (!IsValidCoordinate(heightmap, x, y))
            {
                return 0.0f;
            }

            var index = GetHeightIndex(heightmap, x, y);
            var heightData = heightmap.Shorts[index];

            var height = HeightmapUtils.ConvertToFloatHeight(short.MinValue, short.MaxValue, heightData);
            height *= heightmap.HeightRange.Y;

            return height;
        }
    }
}
