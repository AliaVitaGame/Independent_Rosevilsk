namespace Systems.CurrencySystem
{
    public interface IPlayerSaveService
    {
        void SaveCurrency(CurrencyType type, float amount);
    }
}
