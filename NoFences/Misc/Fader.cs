using System;
using System.Windows.Forms;

namespace NoFences.Misc
{
    public class Fader
    {
        private readonly Timer fadeTimer;
        private Action<double> updateAction;
        private Action onFinish;
        private double startValue;
        private double targetValue;
        private double progress; // 0 to 1
        private readonly double fadeSpeed;
        private Func<double, double> easingFunction;

        public Fader(double speed = 0.05f)
        {
            fadeSpeed = speed;
            fadeTimer = new Timer { Interval = 20 };
            fadeTimer.Tick += FadeStep;

            easingFunction = Easing.EaseInOut;
        }

        public void StartFade(double from, double to, Action<double> updateCallback, Func<double, double> easing = null)
        {
            fadeTimer.Stop();

            startValue = from;
            targetValue = to;
            progress = 0;
            updateAction = updateCallback;
            easingFunction = easing ?? Easing.EaseInOut;

            fadeTimer.Start();
        }

        private void FadeStep(object sender, EventArgs e)
        {
            progress += fadeSpeed;
            if (progress >= 1f)
            {
                progress = 1f;
                fadeTimer.Stop();
                onFinish?.Invoke();
            }

            double easedProgress = easingFunction(progress);
            double currentValue = Lerp(startValue, targetValue, easedProgress);
            updateAction?.Invoke(currentValue);
        }

        public Fader OnFinish(Action onFinishCallback)
        {
            onFinish = onFinishCallback;
            return this;
        }

        private double Lerp(double a, double b, double t) => a + (b - a) * t;

        public void Stop() => fadeTimer.Stop();
    }

    public static class Easing
    {
        public static double Linear(double t) => t;

        public static double EaseIn(double t) => t * t;

        public static double EaseOut(double t) => 1 - Math.Pow(1 - t, 2);

        public static double EaseInOut(double t)
        {
            return t < 0.5f
                ? 2 * t * t
                : 1 - Math.Pow(-2 * t + 2, 2) / 2;
        }

        public static double EaseOutCubic(double t)
        {
            return 1 - Math.Pow(1 - t, 3);
        }

        public static double EaseInOutCubic(double t)
        {
            return t < 0.5f
                ? 4 * t * t * t
                : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }
    }
}
