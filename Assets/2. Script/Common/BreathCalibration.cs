namespace PulmoTrainer.Calibration
{
    // Capacities retain the existing frame-accumulated sensor units, not physical volume units.
    public readonly struct BreathCalibration
    {
        public float InhaleCapacity { get; }
        public float InhalePressure { get; }
        public float ExhaleCapacity { get; }
        public float ExhalePressure { get; }
        public float Fev1 { get; }
        public BreathCalibration(float inhaleCapacity, float inhalePressure, float exhaleCapacity,
            float exhalePressure, float fev1)
        {
            InhaleCapacity = inhaleCapacity;
            InhalePressure = inhalePressure;
            ExhaleCapacity = exhaleCapacity;
            ExhalePressure = exhalePressure;
            Fev1 = fev1;
        }
        public bool IsUsable => IsFinite(InhaleCapacity) && InhaleCapacity < 0f &&
            IsFinite(InhalePressure) && InhalePressure < 0f &&
            IsFinite(ExhaleCapacity) && ExhaleCapacity > 0f &&
            IsFinite(ExhalePressure) && ExhalePressure > 0f && IsFinite(Fev1) && Fev1 > 0f;
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static BreathCalibration AverageOfThree(BreathCalibration first,
            BreathCalibration second, BreathCalibration third)
        {
            return new BreathCalibration(
                (first.InhaleCapacity + second.InhaleCapacity + third.InhaleCapacity) / 3f,
                (first.InhalePressure + second.InhalePressure + third.InhalePressure) / 3f,
                (first.ExhaleCapacity + second.ExhaleCapacity + third.ExhaleCapacity) / 3f,
                (first.ExhalePressure + second.ExhalePressure + third.ExhalePressure) / 3f,
                (first.Fev1 + second.Fev1 + third.Fev1) / 3f);
        }
    }
}
