using System;

namespace Systems.CurrencySystem
{
    [Serializable]
    public class Currency : ICurrency
    {
        public float Value { get; private set; }
        public Currency(float current)
        {
            Value = current;
        }
        public void SetCurrent(float value) => Value = value;
    }
}