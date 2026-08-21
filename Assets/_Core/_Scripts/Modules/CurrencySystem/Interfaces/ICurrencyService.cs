using System.Collections.Generic;
using R3;

namespace Systems.CurrencySystem.Interfaces
{
    public interface ICurrencyService
    {
        ICurrency GetCurrency(CurrencyType type);
        void AddCurrency(CurrencyType type, float amount);
        void SetCurrency(CurrencyType type, float amount, float max = 0);
        ReactiveProperty<ICurrency> GetCurrencyObservable(CurrencyType type);
        Dictionary<CurrencyType, ICurrency> GetAllCurrencies();
        bool SpendCurrency(CurrencyType type, float amount);
    }
}