using R3;
using Systems.CurrencySystem.Interfaces;
using Systems.SaveSystem.Interfaces;

namespace Systems.CurrencySystem
{
    public class CurrencySaveMonitor : ISaveMonitor
    {
        private readonly ICurrencyService _currencyService;
        private readonly ICurrencySave _currencySave;
        private float _bufferedGoldGains;

        private const float SaveThreshold = 500f;

        public CurrencySaveMonitor(ICurrencyService currencyService, ICurrencySave currencySave)
        {
            _currencyService = currencyService;
            _currencySave = currencySave;
        }
        
        public void StartMonitor()
        {
            _currencyService
                .GetCurrencyObservable(CurrencyType.Cash)
                .Subscribe(OnGoldCurrencyChanged);
        }

        private void OnGoldCurrencyChanged(ICurrency newValue)
        {
            _bufferedGoldGains += newValue.Value;

            if(_bufferedGoldGains >= SaveThreshold)
            {
                _currencySave.SaveCurrency(CurrencyType.Cash);
                _bufferedGoldGains = 0;
            }
        }
    }
}