namespace Systems.CurrencySystem
{
    public static class CurrencyFactory
    {
        public static Currency CreateBasicCurrency(float current) => new(current);
        public static CurrencyWithMax CreateCurrencyWithMax(float current, float max) => new(current, max);
    }
}