using System;

namespace NoFences.Util
{
    internal class MathUtils
    {
        public static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        public static float EaseOut(float t)
        {
            return 1 - (float)Math.Pow(1 - t, 3); // Cubic Ease-Out
        }

        public static float EaseInOut(float t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - (float)Math.Pow(-2 * t + 2, 3) / 2;
        }

        public static float EaseOutQuint(float t)
        {
            return 1 - (float)Math.Pow(1 - t, 5);
        }

        public static double SmoothStep(double a, double b, double t)
        {
            t = Clamp(t, 0, 1);
            t = t * t * (3 - 2 * t);
            return Lerp(a, b, t);
        }
        public static double SmootherStep(double a, double b, double t)
        {
            t = Clamp(t, 0, 1);
            t = t * t * t * (t * (t * 6 - 15) + 10);
            return Lerp(a, b, t);
        }
        public static double InverseLerp(double a, double b, double value)
        {
            if (a != b)
            {
                return Clamp((value - a) / (b - a), 0, 1);
            }
            else
            {
                return 0;
            }
        }
        public static double Remap(double value, double from1, double to1, double from2, double to2)
        {
            return Lerp(from2, to2, InverseLerp(from1, to1, value));
        }
        public static double Map(double value, double from1, double to1, double from2, double to2)
        {
            return Lerp(from2, to2, InverseLerp(from1, to1, value));
        }

        public static double MapClamped(double value, double from1, double to1, double from2, double to2)
        {
            return Clamp(Lerp(from2, to2, InverseLerp(from1, to1, value)), from2, to2);
        }

        public static double MapSmoothStep(double value, double from1, double to1, double from2, double to2)
        {
            return SmoothStep(from2, to2, InverseLerp(from1, to1, value));
        }

        public static int FloorToInt(float value)
        {
            return (int)Math.Floor(value);
        }

        public static int CeilToInt(float value)
        {
            return (int)Math.Ceiling(value);
        }
    }
}
