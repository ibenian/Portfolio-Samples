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
        internal Subject<Bubble> removeBubble;
        //internal Subject<BubbleFlock> removeFlock;

        public BasePlayLogic(MainGameLogic mainGameLogic)
        {
            this.MainGameLogic = mainGameLogic;

            playComplete = new Subject<BasePlayLogic>();
            removeBubble = new Subject<Bubble>();
            //removeFlock = new Subject<BubbleFlock>();
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
            get { return BubbleWarsGame.Instance.GameSettings; }
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

        List<Sprite> tappedSpritesListReserve = new List<Sprite>(10);
        public IObservable<T> TappedSprites<T>(IEnumerable<T> sprites, TouchLocation td, Func<T, Vector2, bool> tapCheck) where T : Sprite
        {
            var bbs = (from b in sprites
                       where tapCheck(b, td.Position)
                       orderby Vector2.Distance(b.Position, td.Position) ascending
                       select b).Take(1);

            tappedSpritesListReserve.Clear();
            tappedSpritesListReserve.AddRange(bbs.Cast<Sprite>());
            return tappedSpritesListReserve.Cast<T>().ToObservable();
            //return bbs.ToList(tappedSpritesListReserve).ToObservable();
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

        //public IObservable<IEnumerable<Bubble>> SwipedBubbleGroups(Func<Bubble, Vector2, bool> bubbleTapCheck)
        //{
        //    var tappedBubbleGroups = from swp in this.MainGameLogic.TouchStream.Swipe
        //                             from td in swp
        //                             select TappedBubbleGroups(td.Value, bubbleTapCheck);
        //    return tappedBubbleGroups;
        //}

        //public IObservable<IEnumerable<Bubble>> SwipedBubbles(Func<Bubble, Vector2, bool> bubbleTapCheck)
        //{
        //    var tappedBubbleGroups = from swp in this.MainGameLogic.TouchStream.Swipe
        //                             from ttd in swp
        //                             from b in TappedBubbleGroups(ttd.Value, bubbleTapCheck).ToObservable()
        //                             select b;
        //    return tappedBubbleGroups;
        //}

        //public IEnumerable<Bubble> DistinctBubbles(IEnumerable<TouchLocation> td, Func<Bubble, Vector2, bool> bubbleTapCheck)
        //{ 
        //}

        

        //public IObservable<PosSpeed> FreeFall(Bubble b)
        //{
        //    b.State = MotionState.FreeFall;
        //    return MotionGenerator.AcceleratedMotion(new PosSpeed(b.Position, b.Speed), new Vector2(0f, 1000f), GameScheduler).TakeWhile(v => v.Pos.Y < MainGameLogic.BottomRight.Y + b.TouchRadius);
        //}

        //public IObservable<PosSpeed> FreeFall(BubbleFlock f)
        //{
        //    f.State = MotionState.FreeFall;
        //    return MotionGenerator.AcceleratedMotion(new PosSpeed(f.Position, f.Speed), new Vector2(0f, 1000f), GameScheduler).TakeWhile(v => v.Pos.Y < MainGameLogic.BottomRight.Y + f.TouchRadius);
        //}

        //public IObservable<PosSpeed> RandomThrowUp(Vector2 throwPosition, Bubble b)
        //{
        //    //MainGameLogic.PlayBubbleThrow(b.Position);
        //    //b.State = MotionState.FreeFall;
        //    var speedX = RandomGenerator.Instance.NextFloat(-MathHelper.Clamp(throwPosition.X, 50, 400), MathHelper.Clamp(800f - throwPosition.X, 50, 400));
        //    b.Speed = new Vector2(speedX, -RandomGenerator.Instance.NextFloat(700f, 1000f)*MathHelper.Clamp((400-Math.Abs(speedX))/500, 0.8f, 1f));
        //    //return MotionGenerator.AcceleratedMotion(new PosSpeed(b.Position, b.Speed), new Vector2(0f, 1000f), GameScheduler).TakeWhile(v => v.Pos.Y < 800f);
        //    return Throw(b);
        //}

        //public IObservable<PosSpeed> Throw(Bubble b)
        //{
        //    MainGameLogic.PlayBubbleThrow(b.Position);
        //    b.State = MotionState.FreeFall;
        //    return MotionGenerator.AcceleratedMotion(new PosSpeed(b.Position, b.Speed), new Vector2(0f, 1000f), GameScheduler).TakeWhile(v => v.Pos.Y < 800f);
        //}

        //public IObservable<PosSpeed> Bounce(Bubble b, float groundY)
        //{
        //    b.State = MotionState.FreeFall;
        //    return MotionGenerator.BounceMotion(new PosSpeed(b.Position, b.Speed), new Vector2(0f, 1000f), ps => ps.Pos.Y >= groundY, ps => new Vector2(ps.Speed.X, -b.VerticalJumpSpeedAbs), GameScheduler);
        //}

        //public IObservable<PosSpeed> BounceDamp(Bubble b, float groundY, float dampingFactor)
        //{
        //    b.State = MotionState.FreeFall;
        //    return MotionGenerator.BounceMotion(new PosSpeed(b.Position, b.Speed), new Vector2(0f, 1000f), 
        //        ps => ps.Pos.Y >= groundY,
        //        ps => { var newSpeed = new Vector2(ps.Speed.X, -Math.Abs(ps.Speed.Y) * dampingFactor); b.VerticalJumpSpeedAbs = Math.Abs(newSpeed.Y); return newSpeed; }, 
        //        GameScheduler);
        //}

        //public IObservable<PosSpeed> BatBounce(Bubble b, float groundY, float dampingFactor, Func<PosSpeed, PosSpeed> bounceCheck)
        //{
        //    b.State = MotionState.FreeFall;
        //    return MotionGenerator.BounceMotion(new PosSpeed(b.Position, b.Speed), new Vector2(0f, 1000f), bounceCheck,
        //        GameScheduler);
        //}

        //public IObservable<Vector2> Burst(Bubble b)
        //{
        //    MainGameLogic.BurstBubble(b);
        //    return MotionGenerator.LinearMotion(b.Scale, Vector2.One, GameScheduler);
        //}

        //public IObservable<Vector2> FlyUp(Bubble b)
        //{
        //    b.State = MotionState.FreeFly;
        //    return MotionGenerator.LinearMotion(b.Position, new Vector2(0f, -400f), GameScheduler);
        //}

        //public IObservable<Vector2> Fly(Bubble b)
        //{
        //    b.State = MotionState.FreeFly;
        //    return MotionGenerator.LinearMotion(b.Position, b.Speed, GameScheduler);
        //}

        //public IObservable<PosSpeed> Fly(Bubble b, Rectangle bounds)
        //{
        //    b.State = MotionState.FreeFly;
        //    return MotionGenerator.LinearMotion(new PosSpeed(b.Position, b.Speed), bounds, GameScheduler);
        //}

        //public IObservable<PosSpeed> Fly(Bubble b, Func<Rectangle> getBounds)
        //{
        //    b.State = MotionState.FreeFly;
        //    return MotionGenerator.LinearMotion(new PosSpeed(b.Position, b.Speed), getBounds, GameScheduler);
        //}

        //public IObservable<PosSpeed> Fly(Bubble b, Func<PosSpeed, BoundaryCheckResult> checkBounds/*, Func<PosSpeed, PosSpeed> iterateCallback*/)
        //{
        //    b.State = MotionState.FreeFly;
        //    return MotionGenerator.LinearMotion(new PosSpeed(b.Position, b.Speed), checkBounds, /*iterateCallback, */GameScheduler);
        //}

        //public IObservable<PosSpeed> Fly(BubbleFlock f, Rectangle bounds)
        //{
        //    f.State = MotionState.FreeFly;
        //    return MotionGenerator.LinearMotion(new PosSpeed(f.Position, f.Speed), bounds, GameScheduler);
        //}


        // Sprite animation methods
        public IObservable<Vector2> FlyTo(Sprite sp, float speed, Vector2 targetPos)
        {
            return MotionGenerator.LinearMotion(sp.Position, (targetPos - sp.Position).OfMagnitude(speed), GameScheduler).TakeWhile(v => !Vector2Helper.EqualsWithTolerence(v, targetPos, speed * GameScheduler.Elapsed * 2));
        }

        public IObservable<PosSpeed> EaseTo(Sprite sp, float speedFactor, Vector2 targetPos)
        {
            return MotionGenerator.EasingAttractor(new PosSpeed(sp.Position, sp.Speed), ps => new PosSpeed(targetPos, sp.Speed), speedFactor, GameScheduler, false).TakeWhile(ps => !Vector2Helper.EqualsWithTolerence(ps.Pos, targetPos, 1f));
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

        //public IObservable<Vector2> FlyTo(Sprite sp, float speed, Vector2 targetPos)
        //{
        //    return MotionGenerator.LinearMotion(sp.Position, (targetPos - sp.Position).OfMagnitude(speed), GameScheduler).TakeWhile(v => !Vector2Helper.EqualsWithTolerence(v, targetPos, speed * GameScheduler.Elapsed * 2));
        //}

        //public IObservable<Vector2> MoveNotThrown(Bubble b)
        //{
        //    b.State = MotionState.NotThrown;
        //    return MotionGenerator.LinearAttractor(b.Position, 300, v => b.TargetPosition, GameScheduler);
        //}

        //public IObservable<AngleAndValue> Wobble(Bubble b)
        //{
        //    return MotionGenerator.SineMotion(0.4f, 3f, 0f, GameScheduler);
        //}

        //public IObservable<PosSize> Suck(Bubble b, Vector2 suckTarget)
        //{
        //    b.Speed = suckTarget - b.Position;
        //    var totaldistToTarget = Vector2.Distance(b.Position, suckTarget);
        //    return from p in Fly(b).TakeWhile(v => Vector2.Distance(v, suckTarget) > 10f)
        //           select new PosSize(p, Vector2.Distance(p, suckTarget) / totaldistToTarget);
        //}

        public bool Collides(Bubble b1, Bubble b2)
        {
            return Vector2.Distance(b1.Position, b2.Position) <= b1.TouchRadius + b2.TouchRadius;
        }

        public bool Collides(Bullet b1, Bubble b2)
        {
            return Vector2.Distance(b1.Position, b2.Position) <= b1.TouchRadius + b2.TouchRadius;
        }

        public bool Collides(Missile m, Bubble b)
        {
            return Vector2.Distance(m.Position, b.Position) <= m.TouchRadius + b.TouchRadius;
        }

        public bool Collides(Gem g, Bubble b)
        {
            return Vector2.Distance(g.Position, b.Position) <= g.TouchRadius + b.TouchRadius;
        }

        public bool Collides(HeliPod h, Bubble b)
        {
            return Vector2.Distance(h.Position, b.Position) <= h.TouchRadius + b.TouchRadius;
        }

        public bool Collides(Shield sh, Vector2 pos)
        {
            return Vector2.Distance(sh.Position, pos) <= sh.Radius;
        }

        public bool Collides(Shield sh, Vector2 pos, float addRadius)
        {
            return Vector2.Distance(sh.Position, pos) <= sh.Radius + addRadius;
        }

        public bool Collides(Shield sh, Bubble b)
        {
            return Vector2.Distance(sh.Position, b.Position) <= sh.Radius;
        }

        //protected void BurstBubblesAndFail()
        //{
        //    if (GameState.PlayState == PlayState.Playing)
        //    {
        //        GameState.PlayState = PlayState.Over; GameState.PlaySucceded = false;
        //        var burstTimer = Observable.Interval(TimeSpan.FromMilliseconds(200f), GameScheduler);
        //        var burstSequence = burstTimer.Zip(GameState.Bubbles.ToList(), (t, b) => b);
        //        //GameState.Bubbles.Where(b => b.State == MotionState.FreeFall)
        //        //    .ForEach(b =>
        //        burstSequence.TakeUntil(MainGameLogic.PlayComplete)
        //            .Subscribe(b =>
        //            {
        //                if (b.State == MotionState.FreeFly || b.State == MotionState.FreeFall || b.State == MotionState.Rest)
        //                    MainGameLogic.AddPower(-0.1f / 2);
        //                Burst(b).TakeWhile(s => s.X < 4f)
        //                    .TakeUntil(MainGameLogic.PlayComplete)
        //                    .Subscribe(s => { b.Scale = s; },
        //                    () =>   // When bubbles burst
        //                    {
        //                        removeBubble.OnNext(b);
        //                        //playComplete.OnNext(this);
        //                        //playComplete.OnCompleted();
        //                    });
        //            });
        //        MainGameLogic.PlayLevelFail();
        //    }
        //}

        //protected void BurstBubblesAndSucceed()
        //{
        //    if (GameState.PlayState == PlayState.Playing)
        //    {
        //        GameState.PlayState = PlayState.Over; GameState.PlaySucceded = true;
        //        var burstTimer = Observable.Interval(TimeSpan.FromMilliseconds(200f), GameScheduler);
        //        var burstSequence = burstTimer.Zip(GameState.Bubbles.ToList(), (t, b) => b);
        //        //GameState.Bubbles.Where(b => b.State == MotionState.FreeFall)
        //        //    .ForEach(b =>
        //        burstSequence.TakeUntil(MainGameLogic.PlayComplete)
        //            .Subscribe(b =>
        //            {
        //                if (b.State == MotionState.Burst || b.State == MotionState.NotThrown || b.State == MotionState.Removed)
        //                    removeBubble.OnNext(b);
        //                else
        //                {
        //                    // Burst bubble first
        //                    //MainGameLogic.AddPower(-0.1f);
        //                    Burst(b).TakeWhile(s => s.X < 4f)
        //                        .TakeUntil(MainGameLogic.PlayComplete)
        //                        .Subscribe(s => { b.Scale = s; },
        //                        () =>   // When bubbles burst
        //                        {
        //                            removeBubble.OnNext(b);
        //                        });
        //                }
        //            });
        //        MainGameLogic.PlayLevelSucceed();
        //    }
        //}

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
            if (!GameSettings.IsTipSeen(tipName))
            {
                Delay(delayTime, () =>
                    {
                        this.Game.AdManagement.Visible = false;
                        GameSettings.SetTipSeen(tipName, true);
                        Entropy.BubbleWarsGame.Instance.SaveGameSettings(true);

                        GameState.HelpBox = new HelpBox() { Life = life, HelpText = text, Position = new Vector2(400f, 32f), HelpBoxSize = new Vector2(480f, 75f), GetArrowTargetPos = GetTargetPos, GameSpeed = gameSpeed };
                        GameState.HelpBox.PrepareForScreen(this.PlayScreen);
                        GameScheduler.TimeFactor = gameSpeed;
                    });
            }
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
            get { return BubbleWarsGame.Instance.GameSettings; }
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
