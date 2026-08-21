using Unity.Netcode;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class ActiveModifierData
    {
        public double startTime;
        public double lastUpdateTime;
        public double endTime;

        public ulong ownerID;

        public ModifierBase modifier;

        public float GetCurrentProgress()
        {
            double duration = endTime - startTime;
            double timePassed = NetworkManager.Singleton.ServerTime.Time - startTime;

            return (float)(timePassed / duration);
        }

        public void PrepareToRemove()
        {
            endTime = 0;
        }
    }
}
