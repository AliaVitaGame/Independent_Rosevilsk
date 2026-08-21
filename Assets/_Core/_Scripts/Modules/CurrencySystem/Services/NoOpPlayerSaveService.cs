namespace Systems.CurrencySystem
{
    public sealed class NoOpPlayerSaveService : IPlayerSaveService
    {
        public void SaveCurrency(CurrencyType type, float amount)
        {
            // TODO: Replace with a durable save backend before persistence is required.
        }
    }
}
