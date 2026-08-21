using System;

namespace Systems.CurrencySystem
{
    [Serializable]
    public class CurrencyWithMax : ICurrency
    {
        public float Value { get; private set; }
        public float Max { get; private set; }

        public CurrencyWithMax(float current, float max)
        {
            Value = current;
            Max = max;
        }

        public void SetCurrentWithClamp(float value) => Value = Math.Min(value, Max);
        public void SetCurrent(float value) => Value = value;
        public void SetMax(float max) => Max = max;
    }
}