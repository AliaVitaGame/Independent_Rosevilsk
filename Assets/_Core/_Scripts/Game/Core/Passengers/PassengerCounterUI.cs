using TMPro;
using UnityEngine;

namespace Game.Core.Passengers
{
    /// <summary>
    /// Canvas counter for boarded passengers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PassengerCounterUI : MonoBehaviour
    {
        [SerializeField] private PassengerPickupSystem _pickupSystem;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private string _format = "Passengers: {0}/{1}";

        private void Awake()
        {
            if (_pickupSystem == null)
                _pickupSystem = FindFirstObjectByType<PassengerPickupSystem>();

            if (_label == null)
                _label = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (_pickupSystem == null)
                return;

            _pickupSystem.BoardedCountChanged += OnBoardedCountChanged;
            OnBoardedCountChanged(_pickupSystem.BoardedCount, _pickupSystem.MaxCount);
        }

        private void OnDisable()
        {
            if (_pickupSystem != null)
                _pickupSystem.BoardedCountChanged -= OnBoardedCountChanged;
        }

        private void OnBoardedCountChanged(int boarded, int max)
        {
            if (_label == null)
                return;

            _label.text = string.Format(_format, boarded, max);
        }
    }
}
