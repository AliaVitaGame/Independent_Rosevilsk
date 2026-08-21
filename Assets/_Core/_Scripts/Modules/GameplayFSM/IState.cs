namespace Game.Systems.FSM
{
    public interface IState
    {
        void Enter();
        void Update();
        void Exit();
    }
}