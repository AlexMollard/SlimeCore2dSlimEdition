using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Core.Numeric;

internal static class Floats
{
    public struct VecFloat3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public VecFloat3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public static VecFloat3 operator *(VecFloat3 a, float b) => new(a.X * b, a.Y * b, a.Z * b);
    }
}
