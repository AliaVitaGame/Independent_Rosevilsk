using Systems.CurrencySystem;

namespace Systems.SaveSystem.Interfaces
{
    public interface ICurrencySave
    {
        public void SaveCurrency(CurrencyType currencyType);
        public void SaveCurrencies();
    }

}