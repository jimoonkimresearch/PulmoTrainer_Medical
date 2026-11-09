namespace PulmoTrainer.Inputs
{
    /// <summary>A scene's breath input boundary, independent of the input device.</summary>
    public interface IBreathPressureReceiver
    {
        void SetPressure(float pressure);
    }
}
