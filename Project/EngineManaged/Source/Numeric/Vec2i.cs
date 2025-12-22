using System;

namespace EngineManaged.Numeric
{
	/// <summary>
	/// Integer 2D vector type for grid-like or integer-coordinate usage.
	/// Provides conversions to/from floating-point Vec2.
	/// </summary>
	public struct Vec2i : IEquatable<Vec2i>
	{
		public int X, Y;
		public Vec2i(int x, int y) { X = x; Y = y; }

		// -----------------------------------------------------------------
		// Operators
		// -----------------------------------------------------------------
		public static Vec2i operator +(Vec2i a, Vec2i b) => new Vec2i(a.X + b.X, a.Y + b.Y);
		public static Vec2i operator -(Vec2i a, Vec2i b) => new Vec2i(a.X - b.X, a.Y - b.Y);
		public static Vec2i operator *(Vec2i a, int scalar) => new Vec2i(a.X * scalar, a.Y * scalar);
		public static Vec2i operator *(int scalar, Vec2i a) => new Vec2i(a.X * scalar, a.Y * scalar);
		public static Vec2i operator /(Vec2i a, int scalar) => new Vec2i(a.X / scalar, a.Y / scalar);

		// -----------------------------------------------------------------
		// Basic properties & helpers
		// -----------------------------------------------------------------
		public static Vec2i Zero => new Vec2i(0, 0);

		public int LengthSquared() => X * X + Y * Y;
		public float Length() => MathF.Sqrt(LengthSquared());
        public Vec2i Normalized() { float len = Length(); return len > 0.0001f ? this / (int)len : new Vec2i(0, 0); }
        public static int Dot(in Vec2i a, in Vec2i b) => a.X * b.X + a.Y * b.Y;
        public static float Distance(in Vec2i a, in Vec2i b) => (a - b).Length();

		// -----------------------------------------------------------------
		// Conversions to/from floating point Vec2
		// -----------------------------------------------------------------
		public static implicit operator Vec2(Vec2i v) => new Vec2(v.X, v.Y);
		/// <summary>Explicit conversion from Vec2 to Vec2i, rounding to nearest int.</summary>
		public static explicit operator Vec2i(Vec2 v) => new Vec2i((int)MathF.Round(v.X), (int)MathF.Round(v.Y));
		public Vec2 ToVec2() => new Vec2(X, Y);
		public Vec2 ToVec2Floor() => new Vec2((float)MathF.Floor(X), (float)MathF.Floor(Y));

		// -----------------------------------------------------------------
		// Equality / hash
		// -----------------------------------------------------------------
		public bool Equals(Vec2i other) => X == other.X && Y == other.Y;
		public override bool Equals(object obj) => obj is Vec2i other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(X, Y);

		public override string ToString() => $"Vec2i({X}, {Y})";
	}
}
