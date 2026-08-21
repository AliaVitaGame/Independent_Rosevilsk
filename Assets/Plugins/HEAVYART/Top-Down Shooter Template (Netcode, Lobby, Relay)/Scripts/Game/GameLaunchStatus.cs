namespace HEAVYART.TopDownShooter.Netcode
{
    public enum GameLaunchStatus
    {
        WaitingForPlayersToConnect,
        WaitingForPlayersToInitialize,
        WaitingForPlayersResponses,
        WaitingForJoin,
        ReadyToLaunch,
        UnableToLaunch
    }
}