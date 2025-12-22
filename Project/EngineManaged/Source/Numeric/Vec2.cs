using System;

namespace EngineManaged.Numeric
{
	/// <summary>
	/// Small 2D vector type used across managed game code.
	/// Mutable fields are intentional — code assigns X/Y directly in places.
	/// </summary>
	public struct Vec2 : IEquatable<Vec2>
	{
		public float X, Y;
		public Vec2(float x, float y) { X = x; Y = y; }

        // -----------------------------------------------------------------
        // Operators
        // -----------------------------------------------------------------
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator *(float s, Vec2 a) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator /(Vec2 a, float s) => new Vec2(a.X / s, a.Y / s);
        
        // -----------------------------------------------------------------
        // Basic properties & helpers
        // -----------------------------------------------------------------
        public static Vec2 Zero => new Vec2(0, 0);

        public float Length() => MathF.Sqrt(X * X + Y * Y);
        public float LengthSquared() => X * X + Y * Y;
        public Vec2 Normalized() { float len = Length(); return len > 0.0001f ? this / len : new Vec2(0, 0); }
        public static float Dot(in Vec2 a, in Vec2 b) => a.X * b.X + a.Y * b.Y;
        public static float Distance(in Vec2 a, in Vec2 b) => (a - b).Length();

        // -----------------------------------------------------------------
        // Equality / hash
        // -----------------------------------------------------------------
        public bool Equals(Vec2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"Vec2({X:F2}, {Y:F2})";
	}
}




