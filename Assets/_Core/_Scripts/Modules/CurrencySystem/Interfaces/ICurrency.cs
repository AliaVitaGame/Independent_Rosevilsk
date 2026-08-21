namespace Systems.CurrencySystem
{
    public interface ICurrency
    {
        float Value { get; }
        void SetCurrent(float value);
    }
}