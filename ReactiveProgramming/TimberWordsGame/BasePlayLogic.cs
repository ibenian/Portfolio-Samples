using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameBase.Logic;
using GameBase.Helper;
using Microsoft.Phone.Reactive;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using GameBase.Rx;
using GameBase.State;
using System.Xml.Serialization;

namespace Entropy.GamePlay.PlayLogics
{
    class BasePlayLogic : GameLogic
    {
        internal float Transition = 0f;
        public MainGameLogic MainGameLogic { get; private set; }
        internal Subject<BasePlayLogic> playComplete;
        internal Subject<Letter> removeLetter;

        public BasePlayLogic(MainGameLogic mainGameLogic)
        {
            this.MainGameLogic = mainGameLogic;

            playComplete = new Subject<BasePlayLogic>();
            removeLetter = new Subject<Letter>();
        }

        public new GameState GameState
        {
            get { return (GameState)base.GameState; }
            set
            {
                base.GameState = value;
            }
        }

        internal override void UpdateGameState()
        {
            if (Transition < 1f)
                Transition = MathHelper.Clamp(Transition + this.GameScheduler.Elapsed, 0f, 1f);
            base.UpdateGameState();
        }

        internal GameSettings GameSettings
        {
            get { return EntropyGame.Instance.GameSettings; }
        }

        /// <summary>
        /// Returns an observable stream of bubbles that are tapped
        /// </summary>
        public IObservable<T> TappedSprites<T>(IEnumerable<T> sprites, Func<T, Vector2, bool> tapCheck) where T : Sprite
        {
            var tappedSprites = from td in this.MainGameLogic.TouchStream.TouchDown
                                from b in TappedSprites<T>(sprites, td, tapCheck)
                                select b;
            return tappedSprites;
        }

        public IObservable<T> TappedSprites<T>(IEnumerable<T> sprites, TouchLocation td, Func<T, Vector2, bool> tapCheck) where T : Sprite
        {
            var bbs = (from b in sprites
                       where tapCheck(b, td.Position)
                       orderby Vector2.Distance(b.Position, td.Position) ascending
                       select b).Take(1);

            return bbs.ToList().ToObservable();
        }

        /// <summary>
        /// Returns an observable stream of sprites that are tapped
        /// </summary>
        public IObservable<TappedSpriteGroup<T>> TappedSpriteGroups<T>(IEnumerable<T> sprites, Func<T, Vector2, bool> tapCheck) where T : Sprite
        {
            var tappedSpriteGroups = from td in this.MainGameLogic.TouchStream.TouchDown
                                select new TappedSpriteGroup<T>(td, TappedSpriteGroups<T>(sprites, td, tapCheck));
            return tappedSpriteGroups;
        }

        public IEnumerable<T> TappedSpriteGroups<T>(IEnumerable<T> sprites, TouchLocation td, Func<T, Vector2, bool> tapCheck) where T : Sprite
        {
            var bbs = (from b in sprites
                       where tapCheck(b, td.Position)
                       orderby Vector2.Distance(b.Position, td.Position) ascending
                       select b);
            return bbs;
        }

        


        // Sprite animation methods
        public IObservable<Vector2> FlyTo(Sprite sp, float speed, Vector2 targetPos)
        {
            return MotionGenerator.LinearMotion(sp.Position, (targetPos - sp.Position).OfMagnitude(speed), GameScheduler).TakeWhile(v => !Vector2Helper.EqualsWithTolerence(v, targetPos, speed * GameScheduler.Elapsed * 2));
        }

        public IObservable<PosSpeed> EaseTo(Sprite sp, float speedFactor, Vector2 targetPos)
        {
            return MotionGenerator.EasingAttractor(new PosSpeed(sp.Position, sp.Speed), ps => new PosSpeed(targetPos, sp.Speed), speedFactor, GameScheduler, false).TakeWhile(ps => !Vector2Helper.EqualsWithTolerence(ps.Pos, targetPos, 1f));
        }

        public IObservable<PosSpeed> BatBounce(Bubble b, float groundY, float dampingFactor, Func<PosSpeed, PosSpeed> bounceCheck)
        {
            //b.State = MotionState.FreeFall;
            return MotionGenerator.BounceMotion(new PosSpeed(b.Position, b.Speed), new Vector2(0f, 1000f), bounceCheck, GameScheduler);
        }

        public IObservable<PosSpeed> Bounce(Sprite b, float groundY, Vector2 initialSpeed, float verticalJumpSpeed, float gravity)
        {
            return MotionGenerator.BounceMotion(new PosSpeed(b.Position, initialSpeed), new Vector2(0f, gravity), ps => ps.Pos.Y >= groundY, ps => new Vector2(ps.Speed.X, -verticalJumpSpeed), GameScheduler);
        }

        public IObservable<PosSpeed> BounceDamp(Sprite b, float groundY, Vector2 initialSpeed, float verticalJumpSpeed, float dampingFactor, float minVerticalJumpSpeed, float gravity)
        {
            return MotionGenerator.BounceMotion(new PosSpeed(b.Position, initialSpeed), new Vector2(0f, gravity), 
                ps => ps.Pos.Y >= groundY,
                ps => 
                {
                    var yspeed = Math.Abs(ps.Speed.Y) * dampingFactor;
                    if (yspeed < minVerticalJumpSpeed)
                        yspeed = minVerticalJumpSpeed;
                    var newSpeed = new Vector2(ps.Speed.X, -yspeed);
                    //MainGameLogic.PlayHitGround(ps.Pos);
                    return newSpeed;
                }, 
                GameScheduler);
        }

        public IObservable<PosSpeed> FreeFall(Sprite b, float gravity)
        {
            return MotionGenerator.AcceleratedMotion(new PosSpeed(b.Position, b.Speed), new Vector2(0f, gravity), GameScheduler);
        }

        

        public bool Collides(Bubble b1, Bubble b2)
        {
            return Vector2.Distance(b1.Position, b2.Position) <= b1.TouchRadius + b2.TouchRadius;
        }

        //public bool Collides(Bullet b1, Bubble b2)
        //{
        //    return Vector2.Distance(b1.Position, b2.Position) <= b1.TouchRadius + b2.TouchRadius;
        //}

        //public bool Collides(Missile m, Bubble b)
        //{
        //    return Vector2.Distance(m.Position, b.Position) <= m.TouchRadius + b.TouchRadius;
        //}

        public bool Collides(Gem g, Bubble b)
        {
            return Vector2.Distance(g.Position, b.Position) <= g.TouchRadius + b.TouchRadius;
        }

        

        internal void AddScore(int unitScore, float powerup)
        {
            int score = unitScore + (int)(GameState.Power * unitScore) * 10;
            MainGameLogic.AddScore(score);
            if (GameState.Power == 1f)
            {
                MainGameLogic.AddScore(2*score);        // Extra points for having highest power

                ShowTip("MainGame_FullPow", "You are in full power.  So you are earning scores faster.\r\nTry to keep at full power as long as possible.", 0.1f, 30f);
            }
            MainGameLogic.AddPower(powerup);
        }

        internal void AddBonusForRemainingTime()
        {
            MainGameLogic.AddBonus(GameState.CountDownTimer * 50); // 500 bonus per second left
        }

        internal void ShowHelp(string tipName, float life, string text)
        {
            ShowHelp(tipName, life, text, null, 1f);
        }

        internal void ShowHelp(string tipName, float life, string text, Func<Vector2> GetTargetPos)
        {
            ShowHelp(tipName, life, text, GetTargetPos, 1f);
        }

        internal void ShowHelp(string tipName, float life, string text, Func<Vector2> GetTargetPos, float gameSpeed)
        {
            ShowHelp(tipName, life, text, GetTargetPos, gameSpeed, 0f);
        }

        internal void ShowHelp(string tipName, float life, string text, Func<Vector2> GetTargetPos, float gameSpeed, float delayTime)
        {
            if (!GameSettings.TipsEnabled)
                return;

            if (tipName == null || !GameSettings.IsTipSeen(tipName))
            {
                Delay(delayTime, () =>
                    {
                        this.Game.AdManagement.Visible = false;
                        if (tipName != null)
                        {
                            GameSettings.SetTipSeen(tipName, true);
                            EntropyGame.Instance.SaveGameSettings(true);
                        }

                        GameState.HelpBox = new HelpBox() { Life = life, HelpText = text, Position = new Vector2(ScreenWidth / 2, 32f), HelpBoxSize = new Vector2(ScreenWidth, 75f), GetArrowTargetPos = GetTargetPos, GameSpeed = gameSpeed, Color = Color.Orange.SetTransparency(1f), TextColor = Color.Orange};
                        GameState.HelpBox.PrepareForScreen(this.PlayScreen);
                        GameScheduler.TimeFactor = gameSpeed;
                    });
            }
        }

        internal void ShowHelp(string tipName, float life, string text, Func<Vector2> GetTargetPos, float gameSpeed, float delayTime, string[] dependentTipNames)
        {
            if (dependentTipNames.All(dtip => Entropy.EntropyGame.Instance.TipSystem.IsTipSeen(dtip)))
                ShowHelp(tipName, life, text, GetTargetPos, gameSpeed, delayTime);
        }

        internal void Delay(float seconds, Action action)
        {
            MainGameLogic.GameScheduler.Schedule(action, TimeSpan.FromSeconds(seconds));
        }

        internal void Delay(int frames, Action action)
        {
            MainGameLogic.GameScheduler.Schedule(action, -TimeSpan.FromTicks(frames));
        }
    }

    abstract class BasePlayInitializer
    {
        public MainGameLogic MainGameLogic { get; private set; }

        public BasePlayInitializer(MainGameLogic mainGameLogic)
        {
            this.MainGameLogic = mainGameLogic;
        }

        internal GameSettings GameSettings
        {
            get { return EntropyGame.Instance.GameSettings; }
        }

        internal abstract string GetTitle();
        internal abstract void Initialize(GameState gameState);
    }

    public class InitialState : MachineState
    {
        public static readonly InitialState Instance = new InitialState();
    }

    public class IdleState : MachineState
    {
        public static readonly IdleState Instance = new IdleState();
        [XmlAttribute] public float Duration = 0f;
    }

    public class BurstState : MachineState
    {
        public static readonly BurstState Instance = new BurstState();
    }

    public class FreeFallState : MachineState
    {
        public static readonly FreeFallState Instance = new FreeFallState();
    }

    public class RemovedState : MachineState
    {
        public static readonly RemovedState Instance = new RemovedState();
    }

    public class SlowMotionState : MachineState
    {
        [XmlAttribute] public float TimeFactor = 1f;
        [XmlAttribute] public float Duration = 0f;
    }

    internal struct TappedSpriteGroup<T> where T : Sprite
    {
        public readonly TouchLocation TouchLocation;
        public readonly IEnumerable<T> Sprites;

        public TappedSpriteGroup(TouchLocation tl, IEnumerable<T> sprites)
        {
            this.TouchLocation = tl;
            this.Sprites = sprites;
        }

    }
}
