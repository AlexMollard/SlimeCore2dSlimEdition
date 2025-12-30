using System;

namespace EngineManaged.Numeric
{
    /// <summary>
    /// Integer 3D vector type for grid-like or integer-coordinate usage.
    /// Provides conversions to/from floating-point Vec3.
    /// </summary>
    public struct Vec3i : IEquatable<Vec3i>
    {
        public int X, Y, Z;
        public Vec3i(int x, int y, int z) { X = x; Y = y; Z = z; }

        // -----------------------------------------------------------------
        // Operators
        // -----------------------------------------------------------------
        public static Vec3i operator +(Vec3i a, Vec3i b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3i operator -(Vec3i a, Vec3i b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3i operator *(Vec3i a, int scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
        public static Vec3i operator *(int scalar, Vec3i a) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
        public static Vec3i operator /(Vec3i a, int scalar) => new(a.X / scalar, a.Y / scalar, a.Z / scalar);

        // -----------------------------------------------------------------
        // Basic properties & helpers
        // -----------------------------------------------------------------
        public static Vec3i Zero => new(0, 0, 0);

        public int LengthSquared() => X * X + Y * Y + Z * Z;
        public float Length() => MathF.Sqrt(LengthSquared());
        public Vec3i Normalized() { float len = Length(); return len > 0.0001f ? this / (int)len : new Vec3i(0, 0, 0); }
        public static int Dot(in Vec3i a, in Vec3i b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static float Distance(in Vec3i a, in Vec3i b) => (a - b).Length();

        // -----------------------------------------------------------------
        // Conversions to/from floating point Vec3
        // -----------------------------------------------------------------
        public static implicit operator Vec3(Vec3i v) => new(v.X, v.Y, v.Z);
        /// <summary>Explicit conversion from Vec3 to Vec3i, rounding to nearest int.</summary>
        public static explicit operator Vec3i(Vec3 v) => new((int)MathF.Round(v.X), (int)MathF.Round(v.Y), (int)MathF.Round(v.Z));
        public Vec3 ToVec3() => new(X, Y, Z);
        public Vec3 ToVec3Floor() => new((float)MathF.Floor(X), (float)MathF.Floor(Y), (float)MathF.Floor(Z));

        // -----------------------------------------------------------------
        // Equality / hash
        // -----------------------------------------------------------------
        public bool Equals(Vec3i other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is Vec3i other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public override string ToString() => $"Vec3i({X}, {Y}, {Z})";

        public static bool operator ==(Vec3i left, Vec3i right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vec3i left, Vec3i right)
        {
            return !(left == right);
        }
    }
}