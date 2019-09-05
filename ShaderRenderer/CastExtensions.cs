using ShaderUtils.Mathematics;

// ReSharper disable InconsistentNaming

namespace ShaderRenderer
{
    public static class CastExtensions
    {
        public static OpenTK.Matrix4 ToOpenTK(this Matrix4x4 mat)
        {
            return new OpenTK.Matrix4(mat.M11, mat.M12, mat.M13, mat.M14, mat.M21, mat.M22, mat.M23, mat.M24, mat.M31, mat.M32, mat.M33, mat.M34, mat.M41, mat.M42, mat.M43, mat.M44);
        }

        public static OpenTK.Vector4 ToOpenTK(this Vector4 vec)
        {
            return new OpenTK.Vector4(vec.X, vec.Y, vec.Z, vec.W);
        }

        public static OpenTK.Vector3 ToOpenTK(this Vector3 vec)
        {
            return new OpenTK.Vector3(vec.X, vec.Y, vec.Z);
        }

        public static OpenTK.Vector2 ToOpenTK(this Vector2 vec)
        {
            return new OpenTK.Vector2(vec.X, vec.Y);
        }
    }
}
