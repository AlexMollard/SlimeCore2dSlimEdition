using System;

namespace EngineManaged.Numeric
{
    /// <summary>
    /// Small 3D vector type used across managed game code.
    /// Mutable fields are intentional — code assigns X/Y/Z directly in places.
    /// </summary>
    public struct Vec3 : IEquatable<Vec3>
    {
        public float X, Y, Z;
        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

        // -----------------------------------------------------------------
        // Operators
        // -----------------------------------------------------------------
        public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator *(float s, Vec3 a) => new(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator -(Vec3 a) => new(-a.X, -a.Y, -a.Z);
        // Component-wise multiply/divide
        public static Vec3 operator *(Vec3 a, Vec3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        public static Vec3 operator /(Vec3 a, Vec3 b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
        public static Vec3 operator /(Vec3 a, float s) => new(a.X / s, a.Y / s, a.Z / s);

        // -----------------------------------------------------------------
        // Basic properties & helpers
        // -----------------------------------------------------------------
        public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);
        public float LengthSquared() => X * X + Y * Y + Z * Z;
        public Vec3 Normalized() { var len = Length(); return len > 0.0001f ? this / len : new Vec3(0, 0, 0); }
        public static Vec3 Zero => new(0, 0, 0);

        // Linear interpolation
        public static Vec3 Lerp(in Vec3 a, in Vec3 b, float t) => a + (b - a) * t;

        public static float Dot(in Vec3 a, in Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static Vec3 Cross(in Vec3 a, in Vec3 b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public static float Distance(in Vec3 a, in Vec3 b) => (a - b).Length();

        // -----------------------------------------------------------------
        // Equality / hash
        // -----------------------------------------------------------------
        public bool Equals(Vec3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is Vec3 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public static bool operator ==(Vec3 a, Vec3 b) => a.Equals(b);
        public static bool operator !=(Vec3 a, Vec3 b) => !a.Equals(b);
        public override string ToString() => $"Vec3({X:F2}, {Y:F2}, {Z:F2})";
    }
}
