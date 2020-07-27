using Stride.Core.Mathematics;
using Stride.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace StrideTerrain.Splines
{
    /// <summary>
    /// Custom vertex type for splines
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexPositionNormalTangentColorTexture : IEquatable<VertexPositionNormalTangentColorTexture>, IVertex
    {
        public VertexPositionNormalTangentColorTexture(Vector3 position, Vector3 normal, Vector3 tangent, Color color, Vector2 textureCoordinate) : this()
        {
            Position = position;
            Normal = normal;
            Tangent = tangent;
            Color = color;
            TextureCoordinate = textureCoordinate;
        }

        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Color Color;
        public Vector2 TextureCoordinate;

        public static readonly int Size = 48;

        public static readonly VertexDeclaration Layout = new VertexDeclaration(
           VertexElement.Position<Vector3>(),
           VertexElement.Normal<Vector3>(),
           VertexElement.Tangent<Vector3>(),
           VertexElement.Color<Color>(),
           VertexElement.TextureCoordinate<Vector2>());

        public bool Equals(VertexPositionNormalTangentColorTexture other)
            => Position.Equals(other.Position) && Normal.Equals(other.Normal) && Tangent.Equals(other.Tangent) && Color.Equals(other.Color) && TextureCoordinate.Equals(other.TextureCoordinate);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexPositionNormalTangentColorTexture && Equals((VertexPositionNormalTangentColorTexture)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Normal.GetHashCode();
                hashCode = (hashCode * 397) ^ Tangent.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureCoordinate.GetHashCode();
                return hashCode;
            }
        }

        public VertexDeclaration GetLayout()
            => Layout;

        public void FlipWinding()
            => TextureCoordinate.X = (1.0f - TextureCoordinate.X);

        public static bool operator ==(VertexPositionNormalTangentColorTexture left, VertexPositionNormalTangentColorTexture right)
            => left.Equals(right);

        public static bool operator !=(VertexPositionNormalTangentColorTexture left, VertexPositionNormalTangentColorTexture right)
            => !left.Equals(right);

        public override string ToString()
            => string.Format("Position: {0}, Normal: {1}, Tangent {2}, Color {3}, Texcoord: {4}", Position, Normal, Tangent, Color, TextureCoordinate);
    }
}
