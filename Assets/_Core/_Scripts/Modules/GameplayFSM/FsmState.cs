namespace Game.Systems.FSM
{
    public abstract class FsmState : IState
    {
        protected Fsm Fsm { get; private set; }
        
        public FsmState(Fsm fsm)
        {
            Fsm = fsm;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void Exit() { }
    }
}