namespace EngineManaged.Numeric
{
    public struct Color
    {
        public float R, G, B, A;
        public Color(float r, float g, float b, float a = 1.0f) { R = r; G = g; B = b; A = a; }
        public static Color White => new(1, 1, 1, 1);
        public static Color Red => new(1, 0, 0, 1);
        public static Color Green => new(0, 1, 0, 1);
        public static Color Blue => new(0, 0, 1, 1);
        public static Color Black => new(0, 0, 0, 1);
        public static Color Transparent => new(0, 0, 0, 0);
    }
}
