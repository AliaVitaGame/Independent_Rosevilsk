using System;
using System.Collections.Generic;
using R3;
using Systems.CurrencySystem.Interfaces;
using UnityEngine;

namespace Systems.CurrencySystem
{
    public class CurrencyService : ICurrencyService
    {
        private static readonly HashSet<CurrencyType> NegativeAllowedCurrencies = new()
        {
            CurrencyType.Cash
        };

        private readonly Dictionary<CurrencyType, ReactiveProperty<ICurrency>> _currencies = new();
        private readonly object _lock = new();
        private readonly IPlayerSaveService _playerSaveService;
        
        public CurrencyService(IPlayerSaveService playerSaveService)
        {
            _playerSaveService = playerSaveService;
        }

        public void SetCurrency(CurrencyType type, float amount, float max = 0)
        {
            lock (_lock)
            {
                if (!IsKnownCurrency(type))
                {
                    Debug.LogWarning($"SetCurrency ignored for unknown currency type value {(int)type}.");
                    return;
                }

                var property = EnsureCurrencyProperty(type);

                if (amount < 0 && !CanGoNegative(type))
                {
                    property.Value.SetCurrent(0);
                    _playerSaveService.SaveCurrency(type, 0f);
                    property.ForceNotify();
                    Debug.LogWarning($"Set {amount} {type} to 0");
                    return;
                }

                var currentCurrency = property.Value;
                if (currentCurrency is CurrencyWithMax cwm && max > 0)
                {
                    cwm.SetMax(max);
                }
                currentCurrency.SetCurrent(amount);

                _playerSaveService.SaveCurrency(type, amount);
                property.ForceNotify();
            }
        }

        public ReactiveProperty<ICurrency> GetCurrencyObservable(CurrencyType type)
        {
            lock (_lock)
            {
                if (!IsKnownCurrency(type))
                {
                    Debug.LogWarning($"GetCurrencyObservable requested for unknown currency type value {(int)type}.");
                    return new ReactiveProperty<ICurrency>(CreateCurrency(0f));
                }

                return EnsureCurrencyProperty(type);
            }
        }

        public ICurrency GetCurrency(CurrencyType type)
        {
            lock (_lock)
            {
                if (!IsKnownCurrency(type))
                    return null;

                return _currencies.TryGetValue(type, out var currency) ? currency.Value : null;
            }
        }

        public void AddCurrency(CurrencyType type, float amount)
        {
            lock (_lock)
            {
                if (!IsKnownCurrency(type))
                {
                    Debug.LogWarning($"AddCurrency ignored for unknown currency type value {(int)type}.");
                    return;
                }

                var property = EnsureCurrencyProperty(type);
                var currency = property.Value;
                var targetValue = ApplyMinBalanceRule(type, currency.Value + amount);

                float newValue;
                switch (currency)
                {
                    case CurrencyWithMax cwm:
                        cwm.SetCurrentWithClamp(targetValue);
                        newValue = cwm.Value;
                        break;
                    default:
                        currency.SetCurrent(targetValue);
                        newValue = currency.Value;
                        break;
                }

                _playerSaveService.SaveCurrency(type, newValue);
                property.ForceNotify();
            }
        }

        public bool SpendCurrency(CurrencyType type, float amount)
        {
            lock (_lock)
            {
                if (!IsKnownCurrency(type))
                {
                    Debug.LogWarning($"SpendCurrency ignored for unknown currency type value {(int)type}.");
                    return false;
                }

                if (amount < 0)
                {
                    Debug.LogWarning($"Spend amount must be non-negative. Requested: {amount} {type}");
                    return false;
                }

                if (!_currencies.TryGetValue(type, out var currencyProperty))
                {
                    Debug.LogWarning($"Currency {type} not found.");
                    return false;
                }

                var currency = currencyProperty.Value;

                if (CanGoNegative(type) || currency.Value >= amount)
                {
                    currency.SetCurrent(currency.Value - amount);
                    LogCurrencyUpdate(type, amount, currency);
                    _playerSaveService.SaveCurrency(type, currency.Value);
                    currencyProperty.ForceNotify();
                    return true;
                }

                Debug.LogWarning($"Not enough {type} to spend.");
                return false;
            }
        }

        public Dictionary<CurrencyType, ICurrency> GetAllCurrencies()
        {
            lock (_lock)
            {
                var snapshot = new Dictionary<CurrencyType, ICurrency>(_currencies.Count);

                foreach (var pair in _currencies)
                {
                    snapshot[pair.Key] = pair.Value.Value;
                }

                return snapshot;
            }
        }

        private ReactiveProperty<ICurrency> EnsureCurrencyProperty(CurrencyType type)
        {
            if (_currencies.TryGetValue(type, out var property))
            {
                return property;
            }

            property = new ReactiveProperty<ICurrency>(CreateCurrency(0f));
            _currencies[type] = property;
            return property;
        }

        private static ICurrency CreateCurrency(float amount) 
            => CurrencyFactory.CreateBasicCurrency(amount);

        private static ICurrency CreateCurrencyWithMax(float amount, float max) 
            => CurrencyFactory.CreateCurrencyWithMax(amount, max);

        private static bool IsKnownCurrency(CurrencyType type)
        {
            return Enum.IsDefined(typeof(CurrencyType), type);
        }

        private static bool CanGoNegative(CurrencyType type)
        {
            return NegativeAllowedCurrencies.Contains(type);
        }

        private static float ApplyMinBalanceRule(CurrencyType type, float value)
        {
            return CanGoNegative(type) ? value : Mathf.Max(0f, value);
        }

        private void LogCurrencyUpdate(CurrencyType type, float amount, ICurrency currency)
        {
            if (currency is CurrencyWithMax cwm)
            {
                Debug.Log($"Changed {amount} {type}. New balance: {cwm.Value}/{cwm.Max}");
                return;
            }

            Debug.Log($"Changed {amount} {type}. New balance: {currency.Value}");
        }
    }
}
