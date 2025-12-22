using System;
using EngineManaged.Numeric;

namespace EngineManaged
{
	public static class Ease
	{
		/// <summary>
		/// Linear interpolation between a and b by t.
		/// </summary>
		public static float Lerp(float a, float b, float t) => a + (b - a) * t;

		/// <summary>
		/// "Back" easing out - overshoots slightly then settles.
		/// Perfect for UI pop-in effects.
		/// </summary>
		public static float OutBack(float x)
		{
			const float c1 = 1.70158f;
			const float c3 = c1 + 1;
			return 1 + c3 * MathF.Pow(x - 1, 3) + c1 * MathF.Pow(x - 1, 2);
		}

		public static float InQuad(float x) => x * x;
		public static float OutQuad(float x) => 1 - (1 - x) * (1 - x);

		public static float InOutQuad(float x)
			=> x < 0.5f ? 2 * x * x : 1 - MathF.Pow(-2 * x + 2, 2) / 2;
	}
}