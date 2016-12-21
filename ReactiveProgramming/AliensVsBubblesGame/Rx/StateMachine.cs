using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameBase.State;
using System.Diagnostics;
using System.Xml.Serialization;

namespace GameBase.Rx
{
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.InitialState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.IdleState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.BurstState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.FreeFallState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.RemovedState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.SlowMotionState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.BounceState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.ReBounceState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.FlyState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.SuckedState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.ShotState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.ShockedState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.GemBeatState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.GemRainState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.GemDisappearState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.GemPickedState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.UserFlyingHelipodState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.DrivingHelipodState))]
    [XmlInclude(typeof(Entropy.GamePlay.PlayLogics.PrepLaunch))]
    public abstract class MachineState
    {
        internal Action<Stateful, object> EnterState;

        public override string ToString()
        {
            return string.Format("[{0}]", this.GetType().Name);
        }
    }

    //public abstract class MachineStateSequence : MachineState
    //{
    //    internal abstract IEnumerable<MachineState> EnumStates();
    //}

    internal class StateMachine<SF> where SF : Stateful
    {
        //internal readonly Sprite Sprite;
        private Dictionary<Type, MachineState> states = new Dictionary<Type, MachineState>();
        private GameLoopScheduler gameScheduler;

        internal StateMachine(GameLoopScheduler gameScheduler) //Sprite sprite)
        {
            this.gameScheduler = gameScheduler;
        }

        internal void RegisterState<S>(S state) where S : MachineState
        {
            this.states.Add(typeof(S), state);
        }

        internal void RegisterState<S>(S state, Action<SF, S> enterState) where S : MachineState
        {
            state.EnterState = (sf, o) => enterState((SF)sf, (S)o);
            this.states.Add(typeof(S), state);
        }

        internal void RegisterState<S>() where S : MachineState, new()
        {
            this.states.Add(typeof(S), new S());
        }

        internal void RegisterState<S>(Action<SF, S> enterState) where S : MachineState, new()
        {
            RegisterState<S>(new S(), enterState);
        }

        internal void Transition<S>(SF sf, S state) where S : MachineState
        {
            //this.Sprite.State = state;
            Debug.WriteLine("{0} transition from {1} to {2}", this, sf.MachineState, state);
            sf.MachineState = state;

            if (state.EnterState != null)
                state.EnterState(sf, state);
            else
                this.states[typeof(S)].EnterState(sf, state);
        }

        internal void DelayTransition<S>(SF sf, S state, float delay) where S : MachineState
        {
            gameScheduler.Schedule(() => Transition(sf, state), TimeSpan.FromSeconds(delay));
        }

        internal void DelayTransition<S>(SF sf, S state, int delayFrames) where S : MachineState
        {
            gameScheduler.Schedule(() => Transition(sf, state), -TimeSpan.FromTicks(delayFrames));
        }

        internal void Transition<S>(SF sf) where S : MachineState
        {
            var state = (S)this.states[typeof(S)];
            Debug.WriteLine("{0} transition from {1} to {2}", this, sf.MachineState, state);
            //this.Sprite.State = state;
            sf.MachineState = state;
            this.states[typeof(S)].EnterState(sf, state);
        }

        internal void DelayTransition<S>(SF sf, float delay) where S : MachineState
        {
            gameScheduler.Schedule(() => Transition<S>(sf), TimeSpan.FromSeconds(delay));
        }

        internal void DelayTransition<S>(SF sf, int delayFrames) where S : MachineState
        {
            gameScheduler.Schedule(() => Transition<S>(sf), -TimeSpan.FromTicks(delayFrames));
        }

        internal void Retransition(SF sf)
        {
            var state = sf.MachineState;
            Debug.WriteLine("{0} retransition from {1} to {2}", this, sf.MachineState, state);

            //this.Sprite.State = state;
            if (state.EnterState != null)
                state.EnterState(sf, state);
            else
                this.states[state.GetType()].EnterState(sf, state);
        }
    }
}
