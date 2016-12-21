using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameBase.Logic;
using Entropy.Helper;
using GameBase.Helper;
using Microsoft.Xna.Framework;
using Microsoft.Phone.Reactive;
using GameBase.Rx;
using System.Xml.Serialization;
using GW = Entropy.GamePlay.PlayLogics; //.GroundWarLogic;
using System.Diagnostics;
using GameBase.State;

namespace Entropy.GamePlay.PlayLogics
{
    class GroundWarLogic : BasePlayLogic
    {
        ObjectPool<Bullet> bulletPool = new ObjectPool<Bullet>(50);
        ObjectPool<Gem> gemPool = new ObjectPool<Gem>(50);

        // Reserved lists
        List<Bubble> enemyBubbleListReserve = new List<Bubble>(100);
        List<Bubble> bubbleToFireListReserve = new List<Bubble>(10);
        List<Pair<Bullet, Bubble>> bulletEnemyCollisionsListReserve = new List<Pair<Bullet, Bubble>>(100);
        List<Pair<Missile, Bubble>> missileBubbleCollisionsListReserve = new List<Pair<Missile, Bubble>>(40);
        List<Pair<Bubble, Shield>> enemyShieldCollisionListReserve = new List<Pair<Bubble, Shield>>(50);
        List<Pair<Bubble, Bubble>> enemyBubbleCollisionsListReserve = new List<Pair<Bubble, Bubble>>(100);
        List<Pair<Gem, Bubble>> gemBubbleCollisionsListReserve = new List<Pair<Gem, Bubble>>(40);
        

        internal const int MaxMissiles = 3;
        internal const int MaxShields = 2;

        StateMachine<Bubble> stateMachine;
        StateMachine<Bubble> enemyStateMachine;
        StateMachine<Bullet> bulletStateMachine;
        StateMachine<Missile> missileStateMachine;
        StateMachine<Gem> gemStateMachine;
        StateMachine<HeliPod> heliPodStateMachine;
        StateMachine<Shield> shieldStateMachine;
        StateMachine<Stateful> gameSpeedStateMachine;
        internal Subject<Bullet> removeBullet;
        internal Subject<Missile> removeMissile;
        internal Subject<Gem> removeGem;
        internal Subject<Bubble> removeEnemy;
        internal Subject<Vector2> fireNowSubject;

        public GroundWarLogic(MainGameLogic mainGameLogic)
            : base(mainGameLogic)
        {
            removeBullet = new Subject<Bullet>();
            removeMissile = new Subject<Missile>();
            removeGem = new Subject<Gem>();
            removeEnemy = new Subject<Bubble>();
            fireNowSubject = new Subject<Vector2>();

            var bullet = new Bullet();
            bulletPool.OnClearObject =
                b =>
                {
                    b.BulletType = bullet.BulletType;
                    b.Friendly = bullet.Friendly;
                    b.Id = bullet.Id;
                    b.MachineState = bullet.MachineState;
                    b.Position = bullet.Position;
                    b.Rotation = bullet.Rotation;
                    b.Scale = bullet.Scale;
                    b.Speed = bullet.Speed;
                    b.Texture = bullet.Texture;
                    b.TextureCenter = bullet.TextureCenter;
                    b.TouchRadius = bullet.TouchRadius;
                };

            var gem = new Gem();
            gemPool.OnClearObject =
                g =>
                {
                    g.GemLife = gem.GemLife;
                    g.GemScore = gem.GemScore;
                    g.GemType = gem.GemType;
                    g.Id = gem.Id;
                    g.Inactive = gem.Inactive;
                    g.MachineState = gem.MachineState;
                    g.Position = gem.Position;
                    g.PowerUp = gem.PowerUp;
                    g.Rotation = gem.Rotation;
                    g.Scale = gem.Scale;
                    g.Speed = gem.Speed;
                    g.Texture = gem.Texture;
                    g.TextureCenter = gem.TextureCenter;
                    g.TouchRadius = gem.TouchRadius;
                    g.Twinkles = false;
                };
        }

        protected override void OnGameStateSet()
        {
            base.OnGameStateSet();

            this.stateMachine = new StateMachine<Bubble>(GameScheduler);
            this.enemyStateMachine = new StateMachine<Bubble>(GameScheduler);
            this.bulletStateMachine = new StateMachine<Bullet>(GameScheduler);
            this.missileStateMachine = new StateMachine<Missile>(GameScheduler);
            this.gemStateMachine = new StateMachine<Gem>(GameScheduler);
            this.heliPodStateMachine = new StateMachine<HeliPod>(GameScheduler);
            this.shieldStateMachine = new StateMachine<Shield>(GameScheduler);
            this.gameSpeedStateMachine = new StateMachine<Stateful>(GameScheduler);

            if (this.GameState != null)
            {
                GW.GroundWarState gwState = GameState.GamePlayState as GW.GroundWarState;

                GameState.PrepareForScreen(PlayScreen);
                //this.GameState.Bubbles.ForEach(b => b.PrepareForScreen(this.PlayScreen));
                

                InitAsyncLogic();
            }
        }

        private void InitAsyncLogic()
        {
            GW.GroundWarState gwState = GameState.GamePlayState as GW.GroundWarState;


            //// Show helpbox
            //Observable.Interval(TimeSpan.FromMilliseconds(1000), GameScheduler).TakeWhile(ps => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
            //    .Subscribe(
            //        l =>
            //        {
            //            var b = GameState.Bubbles.First();
            //            GameState.HelpBox = new HelpBox() { Life = 3f, HelpText = "Counting help " + l.ToString(), Position = new Vector2(400f, 110f), HelpBoxSize = new Vector2(720f, 70f), GetArrowTargetPos = () => b.Position };
            //            GameState.HelpBox.PrepareForScreen(this.PlayScreen);
            //        });


            //Bubble lastControlledBubble = null;

            // Game speed state machine
            gameSpeedStateMachine.RegisterState<InitialState>(
                (sf, s) =>
                {
                    GameScheduler.TimeFactor = 1f;
                });

            gameSpeedStateMachine.RegisterState<SlowMotionState>(
                (sf, s) =>
                {
                    GameScheduler.TimeFactor = s.TimeFactor;
                    if (s.Duration > 0)
                        Observable.Interval(TimeSpan.FromMilliseconds(800 * s.TimeFactor), GameScheduler).TakeWhile(l => l < (int)s.Duration)
                            .Subscribe(
                                l => 
                                {
                                    if (s.TimeFactor < 1f)      // Slow down heartbeat
                                        if (GameState.CountDownTimer > 20)
                                            MainGameLogic.PlayHeartbeat();
                                },
                                () =>
                                {
                                    if (sf.IsInState<SlowMotionState>())
                                        gameSpeedStateMachine.Transition<InitialState>(sf);
                                }
                            );
                });

            // Bubble Initial State
            stateMachine.RegisterState<InitialState>(
                (b, state) =>
                {
                    //stateMachine.Transition<FlyState>(b, new FlyState() { TargetPosition = RandomGenerator.Instance.NextVector2(MainGameLogic.TopLeft, MainGameLogic.BottomRight), Speed = 500f });
                    stateMachine.Transition<BounceState>(b, new BounceState() { TargetPosition = RandomGenerator.Instance.NextVector2(MainGameLogic.BottomLeft, MainGameLogic.BottomRight) });

                    //var h = new HeliPod() { Position = new Vector2((b.MachineState as BounceState).TargetPosition.X, 100f), MachineState = InitialState.Instance };
                    //h.PrepareForScreen(PlayScreen);
                    //gwState.Helipods.Add(h);

                }
                );

            stateMachine.RegisterState<BounceState>(
                (b, state) =>
                {
                    var slide = MotionGenerator.EasingAttractor(new PosSpeed(b.Position, Vector2.Zero), ps => new PosSpeed(state.TargetPosition, Vector2.Zero), 4f, GameScheduler, false);
                    var bounce = BounceDamp(b, MainGameLogic.BottomRight.Y - b.TouchRadius, new Vector2(0f, - state.JumpSpeed), state.JumpSpeed, 0.3f, 200f, 1000f);
                    bounce.SetXFrom(slide)
                    .TakeWhile(ps => b.IsInState<BounceState>())//.TakeUntil(MainGameLogic.PlayComplete)
                        .Subscribe(
                            ps =>
                            {
                                b.Position = ps.Pos;
                                //b.Speed = ps.Speed;
                                if (b.Gun == null)
                                    MainGameLogic.TriggerMarkLeaderEffect(b.Position);
                            },
                            () => { }
                            );
                });

            stateMachine.RegisterState<ReBounceState>(
                (b, state) =>
                {
                    ShowHelp("GW_Rebounce", 4f, "You can move your player\r\nby swiping on it", () => MainGameLogic.TransformCam(b.Position), 0.5f, 2f);
                    stateMachine.DelayTransition<BounceState>(b, new BounceState() { JumpSpeed = state.NewJumpSpeed, TargetPosition = state.TargetPosition }, 1);
                });

            // Bubble Fly State
            //stateMachine.RegisterState<FlyState>(
            //    (b, state) =>
            //    {
            //        Fly(b, state.Speed, state.TargetPosition).TakeWhile(v => b.IsInState<FlyState>())
            //            .Subscribe(
            //                v => b.Position = v,       // flying
            //                () => 
            //                    {
            //                        if (b.IsInState<FlyState>())
            //                            stateMachine.DelayTransition<FlyState>(b, new FlyState() { TargetPosition = RandomGenerator.Instance.NextVector2(MainGameLogic.TopLeft, MainGameLogic.BottomRight), Speed = 500f }, 1); // fly again
            //                    }
            //               );
            //    }
            //    );

            // enemy state machine
            enemyStateMachine.RegisterState<FlyState>(
                (b, state) =>
                {
                    FlyTo(b, state.Speed, state.TargetPosition).TakeWhile(v => b.IsInState<FlyState>())
                    //EaseTo(b, 1f, state.TargetPosition).TakeWhile(ps => b.IsInState<FlyState>()).Select(ps => ps.Pos)
                        .Subscribe(
                            v => { b.Position = v; b.LookVector = state.TargetPosition - b.Position; FlightMotion(b); },     // flying
                            () =>
                            {
                                if (b.IsInState<FlyState>())
                                    enemyStateMachine.DelayTransition<FlyState>(b, new FlyState() { TargetPosition = RandomEnemyFlyPosition(), Speed = 100f * GameState.Speed }, 1); // fly again
                            }
                           );
                }
                );

            enemyStateMachine.RegisterState<BounceState>(
               (b, state) =>
               {
                   var slide = MotionGenerator.EasingAttractor(new PosSpeed(b.Position, Vector2.Zero), ps => new PosSpeed(state.TargetPosition, Vector2.Zero), 1f, GameScheduler, false).TakeWhile(ps => Math.Abs(ps.Pos.X - state.TargetPosition.X) > 2);
                   var bounce = BounceDamp(b, MainGameLogic.BottomRight.Y - b.TouchRadius, new Vector2(0f, -state.JumpSpeed), state.JumpSpeed, 0.8f, state.MinJumpSpeed, state.Gravity);
                   bounce.SetXFrom(slide)
                   .TakeWhile(ps => b.IsInState<BounceState>())
                       .Subscribe(
                           ps =>
                           {
                               b.Position = ps.Pos;
                               //b.Speed = ps.Speed;
                               FlightMotion(b);
                           },
                           () => 
                           {
                               if (b.IsInState<BounceState>())
                                   enemyStateMachine.DelayTransition<BounceState>(b, new BounceState() { TargetPosition = RandomGenerator.Instance.NextVector2(MainGameLogic.TopLeft, MainGameLogic.BottomRight), JumpSpeed = state.JumpSpeed, Gravity = state.Gravity, MinJumpSpeed = state.MinJumpSpeed }, 1); // fly again
                           }
                           );
               });

            enemyStateMachine.RegisterState<ShockedState>(
               (b, state) =>
               {
                   
                   var slide = MotionGenerator.EasingAttractor(new PosSpeed(b.Position, Vector2.Zero), ps => new PosSpeed(state.TargetPosition, Vector2.Zero), 4f, GameScheduler, false).TakeWhile(ps => Math.Abs(ps.Pos.X - state.TargetPosition.X) > 2);
                   var bounce = BounceDamp(b, MainGameLogic.BottomRight.Y - b.TouchRadius, new Vector2(0f, -500f), 500f, 0.2f, 0f, 1000f);
                   bounce.SetXFrom(slide)
                   .TakeWhile(ps => b.IsInState<ShockedState>())
                       .Subscribe(
                           ps =>
                           {
                               b.Position = ps.Pos;
                               //b.Speed = ps.Speed;
                           },
                           () =>
                           {
                               if (b.IsInState<ShockedState>())
                                   enemyStateMachine.DelayTransition<BounceState>(b, new BounceState() { TargetPosition = RandomGenerator.Instance.NextVector2(MainGameLogic.TopLeft, MainGameLogic.BottomRight), JumpSpeed = 400 }, 1); // fly again
                           }
                           );
               });

            // Bubble shot
            enemyStateMachine.RegisterState<ShotState>(
                (b, state) =>
                {
                    removeEnemy.OnNext(b);
                });

            // Bubble shocked state
            stateMachine.RegisterState<ShockedState>(
                (b, state) =>
                {
                    ShowHelp("GW_PlayerShocked", 4f, "Player is shocked\r\nPower goes down", () => MainGameLogic.TransformCam(b.Position), 0.3f);
                    ShowHelp("GW_PlayerShocked2", 4f, "Watch your power bar\r\nGame is over when it hits 0", () => new Vector2(30f, 240f), 0.3f, 4f);

                    if (b.Gun == null)
                        MainGameLogic.TriggerWaveBroadcastEffect(b.Position);

                    // stop if driving
                    StopDriveHelipod(b);

                    b.ShyAwayTimer = 2f;
                    var stateEnterTime = GameScheduler.TotalTime;
                    var slide = MotionGenerator.EasingAttractor(new PosSpeed(b.Position, Vector2.Zero), ps => new PosSpeed(state.TargetPosition, Vector2.Zero), 2f, GameScheduler, false);
                    var bounce = BounceDamp(b, MainGameLogic.BottomRight.Y - b.TouchRadius, new Vector2(0, -500f), 500f, 0.2f, 0f, 1000f);
                    bounce.SetXFrom(slide)
                    .TakeWhile(ps => b.IsInState<ShockedState>() && GameScheduler.ElapsedSince(stateEnterTime) < 3f)
                        .Subscribe(
                            ps =>
                            {
                                b.Position = ps.Pos;
                                //b.Speed = ps.Speed;
                                if (b.Gun == null)
                                    MainGameLogic.TriggerWaveBroadcastEffect(b.Position);
                            },
                            () => 
                            {
                                if (b.IsInState<ShockedState>())
                                    if (!MainGameLogic.GameState.GameOver)
                                        stateMachine.DelayTransition<BounceState>(b, new BounceState() { JumpSpeed = 400f, TargetPosition = state.TargetPosition }, 1);
                            }
                            );

                    //
                }
                );

            enemyStateMachine.RegisterState<FreeFallState>(
               (b, state) =>
               {
                   MainGameLogic.ParticleSystem.CreateFlash(p => b.Position, 1, 1f);
                   FreeFall(b, 600f).TakeWhile(v => b.IsInState<FreeFallState>() && v.Pos.Y < MainGameLogic.BottomRight.Y - b.TouchRadius)
                               .Subscribe(
                                   ps =>
                                   {
                                       b.Position = ps.Pos;
                                       b.Scale = Vector2.One * MathHelper.Clamp(1f - b.Position.Y / MainGameLogic.BottomRight.Y * 0.6f, 0.1f, 1f);
                                       b.TouchRadius = b.Scale.X * b.TextureCenter.X;
                                   },
                                   () =>
                                   {
                                       if (b.IsInState<FreeFallState>())
                                       {
                                           b.Position.Y = MainGameLogic.BottomRight.Y - b.TouchRadius;
                                           enemyStateMachine.Transition<IdleState>(b, new IdleState() { Duration = 4f });
                                       }
                                   }
                                  );
               });

            enemyStateMachine.RegisterState<IdleState>(
                (eb, state) =>
                {
                    ShowHelp("GW_EnemyIdle", 4f, "You can eat the fallen aliens", () => MainGameLogic.TransformCam(eb.Position), 0.3f);
                    Observable.Timer(TimeSpan.FromSeconds(state.Duration), GameScheduler)
                            .Subscribe(
                                l =>
                                {
                                },
                                () =>
                                {
                                    if (eb.IsInState<IdleState>())
                                        removeEnemy.OnNext(eb);
                                }
                            );
                });


            enemyStateMachine.RegisterState<SuckedState>(
               (b, state) =>
               {
                   ShowHelp("GW_SuckedEnemy", 4f, "Enemies are being\r\nsucked by your player", () => MainGameLogic.TransformCam(b.Position), 0.3f, 1f);

                   MainGameLogic.ParticleSystem.CreateFlash(p => b.Position, 1, 1f);
                   var playerBubble = (from pb in GameState.Bubbles where !pb.IsInAnyState<ShockedState, RemovedState>() orderby (pb.Position - b.Position).Length() ascending select pb).FirstOrDefault();
                   if (playerBubble != null)
                   {
                       MotionGenerator.EasingAttractor(new PosSpeed(b.Position, Vector2.Zero), ps => new PosSpeed(playerBubble.Position, Vector2.Zero), 1f, GameScheduler, false).TakeWhile(ps => b.IsInState<SuckedState>())
                               .Subscribe(ps =>
                               {
                                   b.Position = ps.Pos;
                                   b.Scale = Vector2.One * MathHelper.Clamp((playerBubble.Position - b.Position).Length() / 400f, 0.3f, 1f);
                                   b.TouchRadius = b.Scale.X * b.TextureCenter.X;
                                   //b.Rotation += b.Scale.X * GameScheduler.Elapsed * 20f;
                               },
                               () =>
                               {
                               });
                   }
               });

            // Bubble driving helipod
            stateMachine.RegisterState<DrivingHelipodState>(
                (b, state) =>
                {
                    ShowHelp("GW_DrivingHelipod", 4f, "Player is driving helipod\r\nSwipe on it to control", () => MainGameLogic.TransformCam(b.Position), 0.3f, 1f);
                });

            // Bubble free fall stat
            //stateMachine.RegisterState<FallState>(
            //    (b, state) =>
            //    {
            //        FreeFall(b).TakeWhile(v => b.IsInState<FallState>())
            //            .Subscribe(
            //                ps => b.Position = ps.Pos,
            //                () => stateMachine.DelayTransition<InitialState>(b, 5f)  // fly again
            //               );
            //    }
            //    );

            // collision detect
            //var collisions = from i in ObservableRx.GeneratePerFrame<int>(0, i => i++, GameScheduler)
            //                 from pair in GameState.Bubbles.Where(b => b.IsInState<FlyState>()).GenerateCombinations().ToObservable()
            //                 where Collides(pair.First, pair.Second)
            //                 select pair;


            //collisions.Subscribe(pair =>
            //    {
            //        // Collision occured.  free fall both bubbles
            //        stateMachine.Transition<FallState>(pair.First);
            //        stateMachine.Transition<FallState>(pair.Second);
            //    });

            

            // Bullet state machine
            this.bulletStateMachine.RegisterState<FlyState>(
                (b, state) =>
                {
                    FlyTo(b, state.Speed, state.TargetPosition).TakeWhile(v => b.IsInState<FlyState>())
                        .Subscribe(
                            v => 
                            {
                                b.Position = v;       // flying
                                MainGameLogic.TriggerBurningBulletEffect(b.Position);
                            },
                            () =>
                            {
                                if (b.IsInState<FlyState>())
                                    this.removeBullet.OnNext(b);
                            }
                           );
                });

            bulletStateMachine.RegisterState<ShotState>(
               (b, state) =>
               {
                   this.removeBullet.OnNext(b);
               });

            missileStateMachine.RegisterState<IdleState>(
                (m, state) =>
                {
                    ShowHelp("GW_MissileReady", 4f, "A missile is ready\r\nWalk on it to launch", () => MainGameLogic.TransformCam(m.Position), 0.3f, 1f);
                });

            missileStateMachine.RegisterState<PrepLaunch>(
                (m, state) =>
                {
                    MainGameLogic.PlayMissileServo(MainGameLogic.TransformCam(m.Position));
                    //var slide = MotionGenerator.EasingAttractor(new PosSpeed(b.Position, Vector2.Zero), ps => new PosSpeed(state.TargetPosition, Vector2.Zero), 4f, GameScheduler, false);
                    MotionGenerator.EasingAttractor(new PosSpeed(m.Position, Vector2.Zero), ps => new PosSpeed(state.TargetPosition, Vector2.Zero), 3f, GameScheduler, false).TakeWhile(ps => !Vector2Helper.EqualsWithTolerence(ps.Pos, state.TargetPosition, 1f) && m.IsInState<PrepLaunch>())
                           .Subscribe(ps =>
                           {
                               m.Position = ps.Pos;
                           },
                           () =>
                           {
                               MainGameLogic.ParticleSystem.CreateBurst(m.Position, 1, Color.Gray);
                               if (m.IsInState<PrepLaunch>())
                                   missileStateMachine.Transition<IdleState>(m);
                           });
                });

            missileStateMachine.RegisterState<FlyState>(
                (m, state) =>
                {
                    ShowHelp("GW_MissileFly", 4f, "Missile launched", () => MainGameLogic.TransformCam(m.Position), 0.3f, 1f);
                    MainGameLogic.TriggerHeavySmokePlumeEffect(m.Position);
                    MainGameLogic.ParticleSystem.CreateFlash(p => m.Position, 1, 4f);
                    Observable.Interval(TimeSpan.FromMilliseconds(200), GameScheduler).TakeWhile(l => m.IsInState<FlyState>())
                        .Subscribe
                        (l =>
                            {
                                MainGameLogic.ParticleSystem.CreateRocketThruster(p => m.Position, m.Scale.X * 2, 0.2f);
                                
                            });
                    EaseTo(m, 1f, state.TargetPosition).TakeWhile(v => m.IsInState<FlyState>()).Select(ps => ps.Pos)
                        .Subscribe(
                            v => 
                            {   
                                m.Position = v;       // flying
                                m.Scale.X -= GameScheduler.Elapsed * 0.15f;
                                m.Scale.Y -= GameScheduler.Elapsed * 0.15f;
                            },
                            () =>
                            {
                                if (m.IsInState<FlyState>())
                                {
                                    this.removeMissile.OnNext(m);
                                    //MainGameLogic.ParticleSystem.CreateFlash2(m.Position, 1, 1, Color.White);
                                    //MainGameLogic.ParticleSystem.CreateFlash2(m.Position, 1, 2, Color.Orange);
                                    //MainGameLogic.ParticleSystem.CreateExplosionScatter(m.Position, 8, 0.4f, Color.Yellow.SetAlpha(0.5f));
                                    MainGameLogic.ParticleSystem.CreateExplosion(m.Position, 1, 3f, Color.White.SetAlpha(0.5f));
                                    MainGameLogic.TriggerBasicExplosionEffect(m.Position);
                                    //MainGameLogic.ParticleSystem.CreateExplosion(m.Position, 1, 0.5f, Color.White.SetAlpha(0.6f));
                                    //MainGameLogic.ParticleSystem.CreateFlash2(m.Position, 1, 4, Color.Red);
                                    //MainGameLogic.ParticleSystem.CreateBurst(m.Position, 4);
                                    MainGameLogic.PlayExplosion(MainGameLogic.TransformCam(m.Position));

                                    ShowHelp("GW_MissileExplode", 4f, "Anti-alien missile exploded\r\nCollect gems left behind", null, 0.2f);

                                    gwState.EnemyBubbles.Where(eb => eb.IsInAnyState<BounceState, FlyState>())/*.ToList(enemyBubbleListReserve)*/.ForEach(eb =>
                                        {
                                            // burst sequence
                                            Observable.Timer(TimeSpan.FromMilliseconds(RandomGenerator.Instance.NextFloat(100f, 1000f)))
                                                .Subscribe(l =>
                                                    {
                                                        enemyStateMachine.Transition<ShotState>(eb);
                                                        MainGameLogic.PlayBubbleBurst(MainGameLogic.TransformCam(eb.Position));
                                                        MainGameLogic.ParticleSystem.CreateFlash(p => eb.Position, 1, 1f);
                                                        //MainGameLogic.ParticleSystem.CreateBurst(eb.Position, 1, Color.Brown.SetTransparency(0.25f));
                                                        
                                                        var gem = gemPool.Get(gm => { gm.Position = eb.Position; gm.GemType = GemType.Rect; gm.GemScore = 10; gm.PowerUp = 0.1f; });
                                                        gem.PrepareForScreen(this.PlayScreen);
                                                        gemStateMachine.Transition<GemBeatState>(gem);
                                                        gwState.Gems.Add(gem);
                                                    });
                                        });
                                }
                            }
                           );
                });

            gemStateMachine.RegisterState<GemBeatState>(
                (g, state) =>
                {
                    MotionGenerator.SineMotion(1f, 1f, 0f, GameScheduler).TakeWhile(av => g.IsInState<GemBeatState>() && g.GemLife > 0)
                        .Subscribe(av =>
                        {
                            g.Scale = Vector2.One * (1f + av.Value / 10f);
                            g.GemLife = MathHelper.Clamp(g.GemLife - GameScheduler.Elapsed, 0f, 60f);
                            if (g.Twinkles)
                                MainGameLogic.TriggerBeamEffect(g.Position);
                        },
                        () =>
                        {
                            if (g.IsInState<GemBeatState>())
                                gemStateMachine.Transition<GemDisappearState>(g);
                        }
                        );
                });

            gemStateMachine.RegisterState<GemRainState>(
                (g, state) =>
                {
                    var slide = MotionGenerator.EasingAttractor(new PosSpeed(g.Position, Vector2.Zero), ps => new PosSpeed(state.TargetPosition, Vector2.Zero), 2f, GameScheduler, false);
                    var bounce = BounceDamp(g, MainGameLogic.BottomRight.Y - g.TouchRadius, state.InitialSpeed, RandomGenerator.Instance.NextFloat(300f, 500f), 0.7f, 300f, 300f);
                    bounce.SetXFrom(slide)
                        .TakeWhile(ps => g.IsInState<GemRainState>() && g.GemLife > 0) // && v.Pos.Y < MainGameLogic.BottomRight.Y - g.TouchRadius)
                               .Subscribe(
                                   ps =>
                                   {
                                       g.Position = ps.Pos;
                                       g.Rotation = GameScheduler.TotalTime + g.Position.Y / 10f;
                                       g.GemLife = MathHelper.Clamp(g.GemLife - GameScheduler.Elapsed, 0f, 60f);
                                       //g.Scale = Vector2.One * (float)(Math.Sin(GameScheduler.TotalTime) + 1) * 0.5f;
                                   },
                                   () =>
                                   {
                                       if (g.IsInState<GemRainState>())
                                       {
                                           //g.Position.Y = MainGameLogic.BottomRight.Y - g.TouchRadius;
                                           gemStateMachine.Transition<GemDisappearState>(g);
                                       }
                                   }
                                  );
                });

            gemStateMachine.RegisterState<GemDisappearState>(
                (g, state) =>
                {
                    MotionGenerator.EasingAttractor(g.Scale.X * 1.2f, v => 0f, 4f, GameScheduler, false).TakeWhile(v => v > 0 && g.IsInState<GemDisappearState>())
                        .Subscribe(v =>
                        {
                            //if (g.MachineState == null || g.IsInState<RemovedState>())
                            //{
                            //    Debug.WriteLine("");
                            //}
                            g.Scale = Vector2.One * v;
                        },
                        () =>
                        {
                            if (g.IsInState<GemDisappearState>())
                                this.removeGem.OnNext(g);
                        }
                        );
                });

            gemStateMachine.RegisterState<GemPickedState>(
                (g, state) =>
                {
                    ShowHelp("GW_GemPicked", 4f, "Collect gems to earn more bonus", () => MainGameLogic.TransformCam(g.Position), 0.3f);
                    this.removeGem.OnNext(g);
                });

            // Helipod initial state
            heliPodStateMachine.RegisterState<InitialState>(
                (h, state) =>
                {
                    ShowHelp("GW_HeliInitial", 4f, "This is a helipod\r\nWalk on it to drive it", () => MainGameLogic.TransformCam(h.Position), 0.2f);
                    heliPodStateMachine.Transition<FreeFallState>(h);
                }
                );

            heliPodStateMachine.RegisterState<FreeFallState>(
                (h, state) =>
                {
                    FreeFall(h, 600f).TakeWhile(v => h.IsInState<FreeFallState>() && v.Pos.Y < MainGameLogic.BottomRight.Y - h.TouchRadius - 40)
                        .Subscribe(
                            ps =>
                            {
                                h.Position = ps.Pos;
                            },
                            () =>
                            {
                                if (h.IsInState<FreeFallState>())
                                    heliPodStateMachine.DelayTransition<IdleState>(h, 1);
                            }
                           );

                    // Slow down blade
                    MotionGenerator.EasingAttractor(h.BladeSpeed, v => 0f, 0.5f, GameScheduler, false).TakeWhile(v => v > 0 && h.IsInState<FreeFallState>())
                        .Subscribe(v =>
                        {
                            h.BladeSpeed = v;
                            h.BladeRotation += GameScheduler.Elapsed * h.BladeSpeed;
                        },
                        () => h.BladeRotation = 0f);
                });

            heliPodStateMachine.RegisterState<IdleState>(
                (h, state) =>
                {
                    // Sittin on ground
                    //if (h.DriverBubble != null)
                    //    StopDriveHelipod(h.DriverBubble);
                    h.Position.Y = MathHelper.Clamp(h.Position.Y, 0f, MainGameLogic.BottomRight.Y - h.TouchRadius - 20);
                    MotionGenerator.EasingAttractor(h.LandOnTransition, v => 1f, 10f, GameScheduler, false).TakeWhile(v => v < 1f && h.IsInState<IdleState>())
                        .Subscribe(v =>
                        {
                            h.LandOnTransition = v;
                        },
                        () => h.LandOnTransition = 1f);
                });

            heliPodStateMachine.RegisterState<UserFlyingHelipodState>(
                (h, state) =>
                {
                    if (h.DriverBubble == null)
                        h.DriverBubble = GameState.Bubbles.Where(b => b.Id == h.DriverBubbleId).FirstOrDefault();
                    // User is flying the helipod

                    var fx = MainGameLogic.PlayChopper(MainGameLogic.TransformCam(h.Position));
                    MainGameLogic.PlayServo(MainGameLogic.TransformCam(h.Position));

                    MotionGenerator.EasingAttractor(h.LandOnTransition, v => 0f, 5f, GameScheduler, false).TakeWhile(v => v > 0f && h.IsInState<UserFlyingHelipodState>())
                        .Subscribe(v =>
                        {
                            h.LandOnTransition = v;
                            
                        },
                        () => h.LandOnTransition = 0f);

                    h.BladeSpeed = 50f;
                    var slide = MotionGenerator.EasingAttractor(new PosSpeed(h.Position, Vector2.Zero), ps => new PosSpeed(state.TargetPosition, Vector2.Zero), 4f, GameScheduler, false);
                    slide.OffsetYPosBy(MotionGenerator.SineMotion(5f, 1f, 0f, GameScheduler).Select(av => new PosSpeed(new Vector2(0f, av.Value), Vector2.Zero)))
                    .TakeWhile(ps => h.IsInState<UserFlyingHelipodState>()).TakeUntil(MainGameLogic.PlayComplete)
                        .Subscribe(
                            ps =>
                            {
                                h.Position = ps.Pos;
                                //b.Speed = ps.Speed;
                                h.DriverBubble.Position = ps.Pos;
                                h.BladeRotation += GameScheduler.Elapsed * h.BladeSpeed;

                                if (fx != null)
                                    fx.Pan = MathHelper.Clamp(Game.CalcPanForPos(h.Position), -1, 1);
                            },
                            () => 
                            {
                                if (h.IsInState<UserFlyingHelipodState>())
                                    heliPodStateMachine.Transition<IdleState>(h);

                                if (fx != null)
                                    fx.Stop();
                            }
                            );

                });

            // Shield state machine
            shieldStateMachine.RegisterState<IdleState>(
               (s, state) =>
               {
                   MotionGenerator.EasingAttractor(s.Radius, v => s.TargetRadius, 10f, GameScheduler, false).TakeWhile(v => v >= 100f && s.IsInState<IdleState>())
                       .Subscribe(v =>
                       {
                           s.Radius = v;
                       },
                       () =>
                       {
                           MainGameLogic.ParticleSystem.CreateFlash(p => s.Position, 1, 4f);
                           MainGameLogic.ParticleSystem.CreateBurst(s.Position, 2, Color.Gray);
                           gwState.Shields.Remove(s);

                           // Game Over
                           //GameState.PlayState = PlayState.Over;
                           //GameState.PlaySucceded = false;
                           //GameState.GameOver = true;
                           //playComplete.OnNext(this);
                           //playComplete.OnCompleted();
                           //MainGameLogic.PlayLevelFail();
                           //MainGameLogic.SaveGame(false);
                       });
               });


            shieldStateMachine.RegisterState<FreeFallState>(
                (s, state) =>
                {
                    ShowHelp("GW_NewShield", 5f, "The shield protects\r\nplayers in it", () => MainGameLogic.TransformCam(s.Position), 0.2f, 1f);

                    FreeFall(s, 600f).TakeWhile(v => s.IsInState<FreeFallState>() && v.Pos.Y < MainGameLogic.BottomRight.Y)
                                .Subscribe(
                                    ps =>
                                    {
                                        s.Position = ps.Pos;
                                    },
                                    () =>
                                    {
                                        if (s.IsInState<FreeFallState>())
                                        {
                                            MainGameLogic.PlayLevelStart();
                                            shieldStateMachine.DelayTransition<IdleState>(s, 1);
                                        }
                                    }
                                   );
                });

            // Bullet Enemy Collision detection
            var bulletCollisions = from i in ObservableRx.GeneratePerFrame<int>(0, i => i++, GameScheduler)
                             from pair in gwState.Bullets.GenerateCombinations(gwState.EnemyBubbles).ToList(bulletEnemyCollisionsListReserve).ToObservable()
                             where Collides(pair.First, pair.Second)
                             select pair;

            bulletCollisions.TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(pair =>
                {
                    if (!pair.First.IsInState<ShotState>() && !pair.Second.IsInAnyState<ShotState, RemovedState>())
                    {
                        var bullet = pair.First;
                        var enemy = pair.Second;

                        ShowHelp("GW_EnemyShot", 5f, "Enemy is shot\r\nTap on it to pick the gem", () => MainGameLogic.TransformCam(enemy.Position), 0.1f);

                        
                        // Collision occured.  free fall both bubbles
                        //MainGameLogic.PlayLevelFail();
                        
                        //MainGameLogic.PlayBubbleBurst(enemy.Position);
                        //MainGameLogic.ParticleSystem.CreateFlash2(enemy.Position, 1, 1f, Color.White);
                        //MainGameLogic.ParticleSystem.CreateBurst(enemy.Position, 3, Color.Gray.SetTransparency(0.1f));

                        MainGameLogic.PlayFireworks(MainGameLogic.TransformCam(enemy.Position));
                        MainGameLogic.TriggerBasicExplosionEffect(enemy.Position);
                        MainGameLogic.TriggerBasicSmokePlumeEffect(enemy.Position);


                        //this.removeBullet.OnNext(pair.First);
                        //this.removeEnemy.OnNext(pair.Second);
                        //AddScore(10, 0.1f);
                        MainGameLogic.AddScore(10);

                        if (GameState.CountDownTimer <= 20)
                        {
                            // Add many gems for the shot enemy
                            for (int i = 0; i < RandomGenerator.Instance.Next(1, 4) && gwState.Gems.Count < 15; i++)
                            {
                                var gem = gemPool.Get(gm => { gm.Position = enemy.Position; gm.GemType = GemType.Rect; gm.GemScore = 100; gm.PowerUp = 0.1f; gm.Inactive = true; });
                                gem.PrepareForScreen(this.PlayScreen);
                                gemStateMachine.Transition<GemRainState>(gem, new GemRainState() { InitialSpeed = new Vector2(0f, RandomGenerator.Instance.NextFloat(-500f, 0f)), TargetPosition = RandomGenerator.Instance.NextVector2(MainGameLogic.BottomLeft, MainGameLogic.BottomRight) });
                                gwState.Gems.Add(gem);
                            }

                            Observable.Timer(TimeSpan.FromMilliseconds(200), GameScheduler)
                                .Subscribe(
                                l => gwState.Gems.ForEach(g => g.Inactive = false)
                                );
                        }
                        else
                        {
                            // Add 1 game for the shot enemy
                            var gem = RandomGem(enemy.Position);
                            gem.PrepareForScreen(this.PlayScreen);
                            gemStateMachine.Transition<GemBeatState>(gem);
                            gwState.Gems.Add(gem);

                            if (gem.GemType == GemType.Cryst || gem.GemType == GemType.Round)
                                gem.Twinkles = true;

                            switch (gem.GemType)
                            {
                                case GemType.Cryst:
                                    ShowHelp("GW_Gem_" + gem.GemType, 4f, "Bubble shield\r\nCreates a shielded area", () => MainGameLogic.TransformCam(gem.Position), 0.2f);
                                    break;
                                case GemType.Drop:
                                    ShowHelp("GW_Gem_" + gem.GemType, 4f, "Anti-alien Missile\r\nGives a missile", () => MainGameLogic.TransformCam(gem.Position), 0.2f);
                                    break;
                                case GemType.Round:
                                    ShowHelp("GW_Gem_" + gem.GemType, 4f, "Surprise powerup", () => MainGameLogic.TransformCam(gem.Position), 0.2f);
                                    break;
                            }
                        }

                        bulletStateMachine.Transition<ShotState>(bullet);
                        enemyStateMachine.Transition<ShotState>(enemy);
                    }
                });

             // Missile bubble Collision detection
            var missileCollisions = from i in ObservableRx.GeneratePerFrame<int>(0, i => i++, GameScheduler)
                             from pair in gwState.Missiles.GenerateCombinations(GameState.Bubbles).ToList(missileBubbleCollisionsListReserve).ToObservable()
                             where Collides(pair.First, pair.Second)
                             select pair;

            missileCollisions.TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(pair =>
                {
                    if (!pair.First.IsInAnyState<RemovedState, FlyState>() && !pair.Second.IsInAnyState<ShotState, DrivingHelipodState>())
                    {
                        // Launch missile
                        missileStateMachine.Transition<FlyState>(pair.First, new FlyState() { TargetPosition = new Vector2(pair.First.Position.X, 70) });
                        //MainGameLogic.ParticleSystem.CreateBurst(pair.First.Position + new Vector2(0, pair.First.TextureCenter.Y), 3, Color.Gray);
                        MainGameLogic.PlayRocketLaunch(MainGameLogic.TransformCam(pair.First.Position));
                        MainGameLogic.AddBonus(500);
                        //GameState.BonusIndex += 3;
                    }
                });

            // Enemy - bubble Collision detection
            var enemyCollisions = from i in ObservableRx.GeneratePerFrame<int>(0, i => i++, GameScheduler)
                                  from pair in gwState.EnemyBubbles.GenerateCombinations(GameState.Bubbles).ToList(enemyBubbleCollisionsListReserve).ToObservable()
                                   where Collides(pair.First, pair.Second)
                                   select pair;

            enemyCollisions.TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(pair =>
            {
                Bubble enemy = pair.First;
                Bubble bubble = pair.Second;
                if (!enemy.IsInState<ShotState>() && !bubble.IsInState<ShotState>() && !bubble.IsInState<ShockedState>())
                {
                    if (enemy.Inactive)
                    {
                        // Player eats the enemy
                        enemyStateMachine.Transition<ShotState>(enemy);
                        MainGameLogic.PlayBubbleBurst(MainGameLogic.TransformCam(enemy.Position));
                        //MainGameLogic.ParticleSystem.CreateBurst(enemy.Position, 1, Color.Brown.SetTransparency(0.25f));
                        MainGameLogic.TriggerBasicSmokePlumeEffect(enemy.Position);
                        if (bubble.Gun != null)
                            AddScore(100, 0.1f);
                        else
                            AddScore(200, 0.1f * 2);    // Bubble with no gun gets double score
                    }
                    else
                    if (ShieldCollision(bubble) == null)
                    {
                        // Enemy eats player
                        stateMachine.Transition<ShockedState>(pair.Second, new ShockedState() { TargetPosition = RandomBubbleThrowPosition(pair.Second.Position), Speed = 400f });
                        // Collision occured.  free fall both bubbles
                        MainGameLogic.PlayLevelFail();
                        MainGameLogic.PlayHitBubble(MainGameLogic.TransformCam(pair.Second.Position));
                        MainGameLogic.ParticleSystem.CreateFlash(p => pair.Second.Position, 1, 4f);
                        MainGameLogic.ParticleSystem.CreateBurst(pair.Second.Position, 1, Color.Brown.SetTransparency(0.25f));
                        //if (bubble.Gun != null)
                            MainGameLogic.AddPower(-0.2f);
                        //else
                        //    MainGameLogic.AddPower(-0.4f);      // Bubble with no gun looses double power
                    }
                }
            });

            // Enemy - shield Collision detection
            var enemyShieldCollisions = from i in ObservableRx.GeneratePerFrame<int>(0, i => i++, GameScheduler)
                                  from pair in gwState.EnemyBubbles.GenerateCombinations(gwState.Shields).ToList(enemyShieldCollisionListReserve).ToObservable()
                                  where Collides(pair.Second, pair.First)
                                  select pair;

            enemyShieldCollisions.TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
               .Subscribe(pair =>
               {
                   if (!pair.First.Inactive && !pair.First.IsInState<ShotState>() && !pair.First.IsInState<ShockedState>() && !pair.Second.IsInState<ShotState>())
                   {
                       ShowHelp("GW_EnemyShieldCol", 4f, "Aliens can't penetrate\r\nbubble shield", () => MainGameLogic.TransformCam(pair.First.Position), 0.3f);
                       MainGameLogic.PlayBoing(MainGameLogic.TransformCam(pair.First.Position));
                       enemyStateMachine.DelayTransition<ShockedState>(pair.First, new ShockedState() { TargetPosition = RandomEnemyFlyPosition(pair.First.Position), Speed = 100f * GameState.Speed }, 1);
                       MainGameLogic.ParticleSystem.CreateFlash(p => pair.First.Position, 1, 0.5f);
                       MainGameLogic.ParticleSystem.CreateBurst(pair.First.Position, 1, Color.Brown.SetTransparency(0.25f));
                       pair.Second.TargetRadius -= pair.Second.ShrinkRate;
                   }
               });

            // Gem Collision detection
            var gemCollisions = from i in ObservableRx.GeneratePerFrame<int>(0, i => i++, GameScheduler)
                                from pair in gwState.Gems.GenerateCombinations(GameState.Bubbles).ToList(gemBubbleCollisionsListReserve).ToObservable()
                                   where Collides(pair.First, pair.Second)
                                   select pair;

            gemCollisions.TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(pair =>
            {
                if (!pair.First.Inactive && !pair.First.IsInState<RemovedState>() && !pair.First.IsInState<GemPickedState>() && !pair.Second.IsInState<ShotState>() && !pair.Second.IsInState<ShockedState>())
                {
                    ShowHelp("GW_EnemyShot", 5f, "Well done, gem is collected", null, 0.2f);

                    
                    // Collision occured.  free fall both bubbles
                    MainGameLogic.ParticleSystem.CreateBurst(pair.First.Position, 1, Color.Orange.SetTransparency(0.25f));
                    MainGameLogic.ParticleSystem.CreateShine(pair.First.Position, 10, 10, 1);
                    if (pair.Second.Gun != null)
                    {
                        AddScore(pair.First.GemScore, pair.First.PowerUp);
                        GameState.BonusIndex++;
                    }
                    else
                    {
                        AddScore(pair.First.GemScore * 2, pair.First.PowerUp * 2);  // Bubble with no gun gets double score
                        GameState.BonusIndex += 2;
                    }

                    if (pair.First.GemType == GemType.Cryst && gwState.Shields.Count < MaxShields)
                    {
                        // Add shield
                        MainGameLogic.PlaySwoosh(MainGameLogic.TransformCam(pair.First.Position));
                        var shield = new Shield() { Position = pair.First.Position /*RandomMissilePosition()*/, Radius = 100f, TargetRadius = RandomGenerator.Instance.Next(200, 350), ShrinkRate = 20f };
                        shield.PrepareForScreen(this.PlayScreen);
                        gwState.Shields.Add(shield);
                        shieldStateMachine.Transition<FreeFallState>(shield);
                    }
                    else if (pair.First.GemType == GemType.Drop && gwState.Missiles.Count < MaxMissiles)
                    {
                        // Add missiles
                        var missilePos = RandomMissilePosition();
                        var missile = new Missile() { MissileType = MissileType.Missile1, Position = missilePos + new Vector2(0, 200f) };
                        missile.PrepareForScreen(this.PlayScreen);
                        missile.Position = missile.Position;
                        missileStateMachine.Transition<PrepLaunch>(missile, new PrepLaunch() { TargetPosition = missilePos });
                        gwState.Missiles.Add(missile);
                    }
                    else if (pair.First.GemType == GemType.Round)
                    {
                        // Surprise feature
                        RunSurpriseFeature(pair.First.Position);
                    }

                    if (!GameState.GameSpeedState.IsInState<SlowMotionState>())
                    if (GameState.BonusIndex % 25 == 0)
                    {
                        if (RandomGenerator.Instance.Next(2) == 0)
                        {
                            var visibleEnemyCount = (from e in gwState.EnemyBubbles where e.IsInAnyState<FlyState, BounceState>() && e.Position.X >= 0 && e.Position.X <= 800 && e.Position.Y >= 0 && e.Position.Y < 480f select e).Count();
                            if (visibleEnemyCount > 0)
                                gameSpeedStateMachine.Transition<SlowMotionState>(GameState.GameSpeedState, new SlowMotionState() { TimeFactor = 0.2f, Duration = 5f });      // slow down
                        }
                        else
                            gameSpeedStateMachine.Transition<SlowMotionState>(GameState.GameSpeedState, new SlowMotionState() { TimeFactor = 1.4f, Duration = 5f });      // speed up
                    }

                    gemStateMachine.Transition<GemPickedState>(pair.First);
                }
            });

            // Helipod bubble collisions
            var helipodBubbleCollisions = from i in ObservableRx.GeneratePerFrame<int>(0, i => i++, GameScheduler)
                             from pair in gwState.Helipods.GenerateCombinations(GameState.Bubbles).ToObservable()
                             where Collides(pair.First, pair.Second)
                             select pair;

            helipodBubbleCollisions.TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(pair =>
                {
                    if (!pair.First.IsInState<UserFlyingHelipodState>() && !pair.Second.IsInState<DrivingHelipodState>())
                    {
                        // Bubble should start driving helipod
                        StartDriveHelipod(pair.First, pair.Second);
                    }
                });

            gameSpeedStateMachine.Retransition(GameState.GameSpeedState);

            // Resume from current state
            GameState.Bubbles.ForEach(b =>
            {
                stateMachine.Retransition(b);       // resume bubbles
            });

            if (GameState.BubblesToProtect > 0)
                ShowHelp("GW_Protect", 4f, "Protect yellow unarmed bubbles", () => MainGameLogic.TransformCam(GameState.Bubbles.Where(b => b.Gun == null).First().Position), 0.5f, 1f);
            else if (GameState.Bubbles.Count == 1)
            {
                var pb = GameState.Bubbles.First();
                ShowHelp("GW_Player", 4f, "This is your player", () => MainGameLogic.TransformCam(pb.Position), 0.5f, 1f);
            }
            else
            {
                ShowHelp("GW_Players_" + GameState.Bubbles.Count.ToString(), 4f, string.Format("You now have {0} players\r\nControl all {0}", GameState.Bubbles.Count), null, 0.5f, 1f);
            }

            gwState.EnemyBubbles.ForEach(b =>
            {
                enemyStateMachine.Retransition(b);       // resume enemy bubbles
            });

            gwState.Bullets.ForEach(bl =>
            {
                bulletStateMachine.Retransition(bl);        // resume bullets
            });

            gwState.Gems.ForEach(g =>
            {
                gemStateMachine.Retransition(g);        // resume gems
            });


            gwState.Helipods.ForEach(h =>
            {
                heliPodStateMachine.Retransition(h);        // resume helipods
            });

            gwState.Missiles.ForEach(m =>
            {
                missileStateMachine.Retransition(m);       // resume missiles
            });

            gwState.Shields.ForEach(s =>
            {
                shieldStateMachine.Retransition(s);       // resume shields
            });


            // move touched bubbles around
            var tappedBubbleGroups = this.TappedSpriteGroups(GameState.Bubbles, (b, p) => Vector2.Distance(p, MainGameLogic.TransformCam(b.Position)) < b.TouchRadius * 2f);

            bool touchControlling = false;
            tappedBubbleGroups.TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(tg =>
                {
                    
                    //MainGameLogic.DisplayMessage(string.Format("Touch {0} down", tg.TouchLocation.Id), 4f);
                    var touchTime = GameScheduler.TotalTime;
                    var tb = tg.Sprites.LastOrDefault(); // .FirstOrDefault();
                    if (tb != null)
                    {
                        //lastControlledBubble = tb;
                        // Move bubbles in bounce state
                        if (tb.IsInState<BounceState>())
                        {
                            // While same touch is moved, until same touch is released]
                            var tlid = tg.TouchLocation.Id;
                            touchControlling = true;
                            this.MainGameLogic.TouchStream.TouchMove.Where(tl => tl.Id == tlid).TakeWhile(tl => tb.IsInState<BounceState>() && GameState.PlayState == PlayState.Playing).TakeUntil(this.MainGameLogic.TouchStream.TouchUp.Where(tl => tl.Id == tlid))
                                .TakeUntil(MainGameLogic.PlayComplete)
                                .Subscribe(tl =>
                                    {
                                        ((BounceState)tb.MachineState).TargetPosition = MainGameLogic.InvTransformCam(tl.Position);

                                        //var rebounceForce = -(((BounceState)tb.MachineState).TargetPosition.Y - tb.Position.Y);
                                        //if (rebounceForce > 20 && GameScheduler.ElapsedSince(touchTime) > 0.3f)
                                        //    stateMachine.DelayTransition<ReBounceState>(tb, new ReBounceState() { TargetPosition = ((BounceState)tb.MachineState).TargetPosition, NewJumpSpeed = MathHelper.Clamp(rebounceForce * 4, 200, 800), }, 1);
                                    },
                                    () =>
                                    {
                                        // touched up
                                        //MainGameLogic.DisplayMessage(string.Format("Touch {0} up", tlid), 4f);
                                        if (GameScheduler.ElapsedSince(touchTime) < 0.5f && tb.IsInState<BounceState>())
                                        {
                                            var rebounceForce = -(((BounceState)tb.MachineState).TargetPosition.Y - tb.Position.Y);
                                            if (rebounceForce > 20)
                                                stateMachine.Transition<ReBounceState>(tb, new ReBounceState() { TargetPosition = ((BounceState)tb.MachineState).TargetPosition, NewJumpSpeed = MathHelper.Clamp(rebounceForce * 4, 200, 800),  });
                                        }

                                        touchControlling = false;
                                    });
                        }
                    }
                });


            var tappedHelipodGroups = this.TappedSpriteGroups(gwState.Helipods, (h, p) => Vector2.Distance(p, MainGameLogic.TransformCam(h.Position)) < h.TouchRadius * 2f);

            tappedHelipodGroups.TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(tg =>
                {
                    var touchTime = GameScheduler.TotalTime;
                    var h = tg.Sprites.FirstOrDefault();

                    if (h != null)
                    {
                        if (!h.IsInState<UserFlyingHelipodState>() && h.DriverBubbleId != Guid.Empty)
                        {
                            heliPodStateMachine.Transition<UserFlyingHelipodState>(h, new UserFlyingHelipodState() { TargetPosition = h.Position + new Vector2(0, -40f) });
                        }

                        var tlid = tg.TouchLocation.Id;
                        touchControlling = true;
                        MainGameLogic.ParticleSystem.CreateBurst(h.Position, 1, Color.Gray);

                        // Control helipod
                        this.MainGameLogic.TouchStream.TouchMove.Where(tl => tl.Id == tlid).TakeWhile(tl => h.IsInState<UserFlyingHelipodState>()).TakeUntil(this.MainGameLogic.TouchStream.TouchUp.Where(tl => tl.Id == tlid))
                                    .TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                                    .Subscribe(tl =>
                                    {
                                        ((UserFlyingHelipodState)h.MachineState).TargetPosition = MainGameLogic.InvTransformCam(tl.Position);
                                    },
                                    () =>
                                    {
                                        touchControlling = false;
                                    });
                    }
                });

            
            this.MainGameLogic.TouchStream.TouchMove.Subscribe(
                tl =>
                {
                    // Gun Point
                    if (!touchControlling)
                    {
                        GameState.LastAttentionPosition = MainGameLogic.InvTransformCam(tl.Position);
                    }
                });


            bool firing = false;
            int fireCount = 0;
            this.MainGameLogic.TouchStream.TouchDown.Subscribe(
               tl =>
               {
                   // Laser fire if not controlling bubbles
                   if (!touchControlling)
                   {
                       fireNowSubject.OnNext(MainGameLogic.InvTransformCam(tl.Position));
                       firing = true;

                       // Touched on gem?
                       //if (lastControlledBubble != null)
                       {
                           var touchedGem = (from g in gwState.Gems where Vector2.Distance(tl.Position, MainGameLogic.TransformCam(g.Position)) < g.TouchRadius select g).FirstOrDefault();
                           if (touchedGem != null)
                           {
                               var pickBubble = (from b in GameState.Bubbles where b.IsInState<BounceState>() orderby Vector2.Distance(b.Position, touchedGem.Position) select b).FirstOrDefault();
                               if (pickBubble != null)
                                stateMachine.Transition<ReBounceState>(pickBubble, new ReBounceState() { TargetPosition = touchedGem.Position, NewJumpSpeed = MathHelper.Clamp((pickBubble.Position.Y - touchedGem.Position.Y) * 4, 200, 800), });
                           }
                       }
                   }
               });

            this.MainGameLogic.TouchStream.TouchUp.TakeWhile(ps => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(
               tl =>
               {
                   if (firing)
                   {
                       // firing stopped
                       firing = false;
                   }
               });

            // Autofire
            Observable.Interval(TimeSpan.FromMilliseconds(200), GameScheduler).TakeWhile(ps => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(
                    l =>
                    {
                        if (firing)
                            fireNowSubject.OnNext(GameState.LastAttentionPosition);
                    });

            // Fire now
            fireNowSubject.TakeWhile(ps => GameState.PlayState == PlayState.Playing)
                .Subscribe(
               fireTarget =>
               {
                   fireCount++;
                   // Laser fire
                   GameState.LastAttentionPosition = MainGameLogic.InvTransformCam(fireTarget);

                    // Find bubble to fire
                   var bubble = GameState.Bubbles.Where(b => b.Gun != null && !b.IsInState<ShockedState>() /*&& ShieldCollision(b) == null*/).ToList(bubbleToFireListReserve).GetByModulusIndex(fireCount);
                   //var bubble = (from b in GameState.Bubbles where b.Gun != null orderby Vector2.Distance(b.Position, fireTarget) select b).FirstOrDefault();
                    if (bubble != null)
                    {
                        FireBubbleGun(gwState, fireTarget, bubble);
                    }

                    var helipodsWithBubbles = gwState.Helipods.Where(h => h.IsInState<UserFlyingHelipodState>() && h.DriverBubble != null && h.DriverBubble.Gun != null && !h.DriverBubble.IsInState<ShockedState>());
                    foreach (var h in helipodsWithBubbles)
                    {
                        FireBubbleGun(gwState, fireTarget, h.DriverBubble);
                    }
               });


            // Enemies
            Observable.Interval(TimeSpan.FromMilliseconds(1000), GameScheduler).TakeWhile(l => GameState.CountDownTimer > 0 && GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(
                    l =>
                    {
                        GameState.CountDownTimer--;
                        MainGameLogic.PlayCountDownAlert(GameState.CountDownTimer);
                        MainGameLogic.PlayCountDownVoice(GameState.CountDownTimer);
                        //if (GameState.CountDownTimer <= 10)
                        //{
                        //    // Gem race
                        //    for (int i = 0; i < 10 && gwState.Gems.Count < 15; i++)
                        //    {
                        //        var gemPos = RandomGenerator.Instance.NextVector2(new Vector2(MainGameLogic.BottomLeft.X, -500f), new Vector2(MainGameLogic.BottomRight.X, -100f));
                        //        var gem = new Gem() { Position = gemPos, GemType = GemType.Rect, GemScore = 10, PowerUp = 0.1f };
                        //        gem.PrepareForScreen(this.PlayScreen);
                        //        gemStateMachine.Transition<GemRainState>(gem, new GemRainState() { TargetPosition = gemPos });
                        //        gwState.Gems.Add(gem);
                        //    }
                        //}


                        var expectedEnemyDensity = GameState.EnemyDensity; // ((l / 5) % GameState.EnemyDensity) + 1;
                        if (GameState.CountDownTimer <= 20)
                            expectedEnemyDensity *= 2;
                        if (gwState.EnemyBubbles.Count < expectedEnemyDensity)
                        {
                            //MainGameLogic.PlayLevelStart();
                            var enemy = RandomAlien();
                            if (RandomGenerator.Instance.NextFloat() < 0.1f)
                                MainGameLogic.PlayAlienVoice(MainGameLogic.TransformCam(enemy.Position));
                            //gwState.EnemyBubbles.Add(enemy);
                            enemy.PrepareForScreen(PlayScreen);
                            switch (RandomGenerator.Instance.Next(2))
                            {
                                case 0:
                                    enemyStateMachine.Transition<FlyState>(enemy, new FlyState() { Speed = RandomGenerator.Instance.NextFloat(100f, 200f) * GameState.Speed, TargetPosition = RandomGenerator.Instance.NextVector2(MainGameLogic.TopLeft, MainGameLogic.BottomRight) });
                                    break;
                                case 1:
                                    enemyStateMachine.Transition<BounceState>(enemy, new BounceState() { JumpSpeed = RandomGenerator.Instance.NextFloat(300f, 400f), MinJumpSpeed = 300f, TargetPosition = RandomGenerator.Instance.NextVector2(MainGameLogic.TopLeft, MainGameLogic.BottomRight), Gravity = RandomGenerator.Instance.NextFloat(300f, 350f * GameState.Speed) });
                                    break;
                            }
                            gwState.EnemyBubbles.Add(enemy);

                            ShowHelp("GW_Enemy", 4f, "Shoot the green aliens\r\nby tapping in its direction", () => MainGameLogic.TransformCam(enemy.Position), 0.2f, 2f);
                        }

                        this.GameState.Bubbles.Where(b => b.IsInState<BounceState>()).ForEach(b =>
                            {
                                if (b.Position.X < 0)
                                    (b.MachineState as BounceState).TargetPosition.X = 0;
                                else if (b.Position.X > ScreenWidth)
                                    (b.MachineState as BounceState).TargetPosition.X = ScreenWidth;
                            });

                        gwState.Helipods.Where(h => h.IsInState<UserFlyingHelipodState>()).ForEach(h =>
                        {
                            if (h.Position.X < 0)
                                (h.MachineState as UserFlyingHelipodState).TargetPosition.X = 0;
                            else if (h.Position.X > ScreenWidth)
                                (h.MachineState as UserFlyingHelipodState).TargetPosition.X = ScreenWidth;

                            if (h.Position.Y < MainGameLogic.TopLeft.Y - MainGameLogic.BubbleDiameter * 2)
                                (h.MachineState as UserFlyingHelipodState).TargetPosition.Y = MainGameLogic.TopLeft.Y - MainGameLogic.BubbleDiameter * 2;
                            else if (h.Position.Y > MainGameLogic.BottomRight.Y - MainGameLogic.BubbleDiameter * 2)
                                (h.MachineState as UserFlyingHelipodState).TargetPosition.Y = MainGameLogic.BottomRight.Y - MainGameLogic.BubbleDiameter * 2;
                        });


                        if (RandomGenerator.Instance.Next(2) == 0)
                        {
                            var b = GameState.Bubbles.PickRandomElement();
                            if (b != null)
                            {
                                b.ShyAwayTimer = 0.1f;
                            }
                        }

                        
                       
                    },
                    () =>
                    {
                        // When timer is complete, play is over
                        if (GameState.PlayState == PlayState.Playing)
                        {
                            GameState.PlayState = PlayState.Over;
                            GameState.PlaySucceded = true;
                            playComplete.OnNext(this);
                            playComplete.OnCompleted();
                            MainGameLogic.PlayLevelSucceed();

                        }
                    });

            //this.MainGameLogic.TouchStream.TouchDown
            //    .Subscribe(tl =>
            //        {
            //            var bubble = new Bubble() { Position = tl.Position, MachineState = InitialState.Instance };
            //            bubble.PrepareForScreen(this.PlayScreen);
            //            stateMachine.Retransition(bubble);
            //            this.GameState.Bubbles.Add(bubble);
            //        });

            //testState.Sprites.WhereState<FlyState>().ForEach(ss =>
            //    {
            //        Rectangle bounds = MainGameLogic.GameBounds;
            //        var fly = Fly(ss.Sprite, bounds, ss.State.TargetPosition);

            //        b.Position = new Vector2(-1000, -1000);  // move out of screen initially

            //        fly.OffsetPosBy(moveHoriz)
            //            .TakeWhile(ps => b.State == MotionState.FreeFly).TakeUntil(MainGameLogic.PlayComplete)
            //            .Subscribe(
            //                ps =>
            //                {
            //                    b.Position = ps.Pos;
            //                    //b.Speed = ps.Speed;
            //                },
            //                () => { }
            //                );
            //    });

            // Remove bubbles
            removeBubble.ObserveOn(GameScheduler)//.TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(b => 
                    {
                        GameState.Bubbles.Remove(b);
                        b.MachineState = RemovedState.Instance;
                        //if (GameState.Bubbles.Count == 0)
                        //{
                        //    playComplete.OnNext(this);
                        //    playComplete.OnCompleted(); 
                        //}
                    });

            // Remove enemies
            removeEnemy.ObserveOn(GameScheduler)//.TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(b =>
                {
                    gwState.EnemyBubbles.Remove(b);
                    b.MachineState = RemovedState.Instance;
                });

            removeBullet.ObserveOn(GameScheduler)//.TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(b => 
                    {
                        gwState.Bullets.Remove(b);
                        bulletPool.Recycle(b);
                        b.MachineState = RemovedState.Instance;
                    });

            removeMissile.ObserveOn(GameScheduler)//.TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(b =>
                {
                    gwState.Missiles.Remove(b);
                    b.MachineState = RemovedState.Instance;
                });

            removeGem.ObserveOn(GameScheduler)//.TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(g =>
                {
                    gwState.Gems.Remove(g);
                    gemPool.Recycle(g);
                    g.MachineState = RemovedState.Instance;
                });

            // Complete game when done
            playComplete.ObserveOn(GameScheduler)
                .Subscribe(bp => MainGameLogic.PlayComplete.OnNext(bp)
                );
            
        }

        private void FireBubbleGun(GW.GroundWarState gwState, Vector2 fireTarget, Bubble bubble)
        {
            var bullet = bulletPool.Get(bl => { bl.BulletType = BulletType.Laser; bl.Position = bubble.Position + bubble.GunPosition; bl.Friendly = true; });
            var pointVector = fireTarget - bullet.Position;
            bullet.Rotation = (float)Math.Atan2((double)pointVector.Y, (double)pointVector.X) + (float)Math.PI / 2;
            gwState.Bullets.Add(bullet);
            bullet.PrepareForScreen(PlayScreen);
            bulletStateMachine.Transition<FlyState>(bullet, new FlyState() { Speed = 2 * 800f, TargetPosition = Vector2Helper.Extrapolate(bullet.Position, fireTarget, 200f) });
            //MainGameLogic.PlayBubbleBurst(MainGameLogic.TransformCam(bubble.Position));
            MainGameLogic.PlayLaser(MainGameLogic.TransformCam(bubble.Position));
            MainGameLogic.ParticleSystem.CreateFlash(p => bubble.Position + bubble.GunPosition, 1, 0.4f);
            MainGameLogic.ParticleSystem.CreateBurst(bubble.Position + bubble.GunPosition, 1, new Color(125, 108, 43, 5), 0.4f);
        }

        private void FlightMotion(Bubble s)
        {
            s.Scale.X = 1f + (float)Math.Sin(GameState.TotalGameTime * 6.5f) * 0.1f;
            s.Scale.Y = 1f + (float)Math.Sin(GameState.TotalGameTime * 6.7f + 0.5f) * 0.1f;
        }

        private void RunSurpriseFeature(Vector2 pos)
        {
            GW.GroundWarState gwState = GameState.GamePlayState as GW.GroundWarState;
            const int surpriseCount = 4;
            switch (RandomGenerator.Instance.Next(surpriseCount))
            { 
                case 0:
                    ShowHelp("GW_Surprise0", 4f, "Surprise powerup\r\nBlow up all enemies", null, 0.2f);
                    // Blow up all monster
                    foreach (var eb in gwState.EnemyBubbles)
                    {
                        enemyStateMachine.DelayTransition<ShotState>(eb, 1);
                        MainGameLogic.PlayBubbleBurst(MainGameLogic.TransformCam(eb.Position));
                        //MainGameLogic.ParticleSystem.CreateBurst(eb.Position, 1, Color.Brown.SetTransparency(0.25f));
                        MainGameLogic.TriggerBasicSmokePlumeEffect(eb.Position);
                        var gem = gemPool.Get(gm => { gm.Position = eb.Position; gm.GemType = GemType.Rect; gm.GemScore = 100; gm.PowerUp = 0.1f; });
                        gem.PrepareForScreen(this.PlayScreen);
                        gemStateMachine.Transition<GemBeatState>(gem);
                        gwState.Gems.Add(gem);
                    }
                    break;
                case 1:
                    ShowHelp("GW_Surprise1", 4f, "Surprise powerup\r\nEat shocked aliens", null, 0.2f);
                    // Freefall all monsters
                    MainGameLogic.TriggerStarTrailEffect(pos);
                    foreach (var eb in gwState.EnemyBubbles.Where(b => !b.IsInAnyState<ShotState, RemovedState>()))
                    {
                        eb.Inactive = true;
                        enemyStateMachine.Transition<FreeFallState>(eb);
                        MainGameLogic.PlayHitBubble(MainGameLogic.TransformCam(eb.Position));
                    }
                    break;
                case 2:
                    ShowHelp("GW_Surprise2", 4f, "Surprise powerup\r\nSuck all aliens", null, 0.2f);
                    // Suck monsters
                    MainGameLogic.TriggerStarTrailEffect(pos);
                    for (int i = 0; i < RandomGenerator.Instance.Next(20); i++)
                    {
                        var enemy = RandomAlien();
                        enemy.PrepareForScreen(PlayScreen);
                        enemyStateMachine.Transition<FlyState>(enemy, new FlyState() { Speed = 100f * GameState.Speed, TargetPosition = RandomGenerator.Instance.NextVector2(MainGameLogic.TopLeft, MainGameLogic.BottomRight) });
                        gwState.EnemyBubbles.Add(enemy);
                    }
                    foreach (var eb in gwState.EnemyBubbles.Where(b => !b.IsInAnyState<ShotState, RemovedState>()))
                    {
                        eb.Inactive = true;
                        enemyStateMachine.Transition<SuckedState>(eb);
                        MainGameLogic.PlayHitBubble(MainGameLogic.TransformCam(eb.Position));
                    }

                    break;
                case 3:
                    ShowHelp("GW_Surprise3", 4f, "Surprise powerup\r\nBonus gems", null, 0.2f);
                    // Explode, throw gems
                    MainGameLogic.PlayLevelStart();
                    MainGameLogic.PlayMagicSurprise(MainGameLogic.TransformCam(pos));
                    MainGameLogic.ParticleSystem.CreateFlash2(pos, 1, 2f, Color.Red.SetTransparency(0.25f));
                    MainGameLogic.ParticleSystem.CreateBurst(pos, 2, Color.Brown.SetTransparency(0.25f));
                    for (int i = 0; i < RandomGenerator.Instance.Next(5, 15) && gwState.Gems.Count < 15; i++)
                    {
                        var gem = gemPool.Get(gm => { gm.Position = pos; gm.GemType = GemType.Rect; gm.GemScore = 100; gm.PowerUp = 0.1f; gm.Inactive = true; });
                        //if (gem.Scale.X < 1f)
                        //    Debug.WriteLine("");
                        gem.PrepareForScreen(this.PlayScreen);
                        gemStateMachine.Transition<GemRainState>(gem, new GemRainState() { InitialSpeed = new Vector2(0f, RandomGenerator.Instance.NextFloat(-500f, 0f)), TargetPosition = RandomGenerator.Instance.NextVector2(MainGameLogic.BottomLeft, MainGameLogic.BottomRight) });
                        gwState.Gems.Add(gem);
                    }

                    Observable.Timer(TimeSpan.FromMilliseconds(1000), GameScheduler)
                        .Subscribe(
                        l => gwState.Gems.ForEach(g => g.Inactive = false)
                        );
                    break;
            }
        }

        private Gem RandomGem(Vector2 pos)
        {
            //GameState.MissileChance = 1f;
            float rnd = RandomGenerator.Instance.NextFloat();
            GW.GroundWarState gwState = GameState.GamePlayState as GW.GroundWarState;
            if (rnd < GameState.MissileChance)
            {
                if (gwState.Missiles.Count < MaxMissiles)
                    return gemPool.Get(gm => { gm.Position = pos; gm.GemType = GemType.Drop; gm.GemScore = 10; gm.PowerUp = 0.1f; });
            }
            else if (rnd < GameState.ShieldChance + GameState.MissileChance)
            {
                if (gwState.Shields.Count < MaxShields)
                    return gemPool.Get(gm => { gm.Position = pos; gm.GemType = GemType.Cryst; gm.GemScore = 10; gm.PowerUp = 0.1f; gm.GemLife = 5f; });
            }
            else if (rnd < GameState.ShieldChance + GameState.MissileChance + GameState.CrystChance)
                return gemPool.Get(gm => { gm.Position = pos; gm.GemType = GemType.Round; gm.GemScore = 10; gm.PowerUp = 0.1f; gm.GemLife = 4f; });

            return gemPool.Get(gm => { gm.Position = pos; gm.GemType = GemType.Rect; gm.GemScore = 10; gm.PowerUp = 0.1f; });

            //var gemType = (GemType)RandomGenerator.Instance.Next((int)GemType.Max);
            
        }

        private void StopDriveHelipod(Bubble b)
        {
            Debug.WriteLine("StopDriveHelipod {0}", b);
            GW.GroundWarState gwState = GameState.GamePlayState as GW.GroundWarState;
            (from h in gwState.Helipods where h.DriverBubbleId == b.Id select h)
                        .ForEach(h =>
                        {
                            Debug.WriteLine("StopDriveHelipod completing {0} {1}", b, h);
                            h.DriverBubble = null;
                            h.DriverBubbleId = Guid.Empty;
                            heliPodStateMachine.Transition<FreeFallState>(h);
                            //MainGameLogic.DisplayMessage("StopDriveHelipod", 4f);
                            Debug.WriteLine("StopDriveHelipod complete {0} {1}", b, h);
                        });
        }

        private void StartDriveHelipod(HeliPod h, Bubble b)
        {
            if (!h.IsInState<UserFlyingHelipodState>() && !b.IsInAnyState<ShockedState, RemovedState, DrivingHelipodState>())
            {
                Debug.WriteLine("StartDriveHelipod {0} {1}", b, h);
                //MainGameLogic.DisplayMessage("StartDriveHelipod", 4f);
                h.DriverBubbleId = b.Id;
                heliPodStateMachine.Transition<UserFlyingHelipodState>(h, new UserFlyingHelipodState() { TargetPosition = h.Position + new Vector2(0, -40f) });
                stateMachine.Transition<DrivingHelipodState>(b, new DrivingHelipodState() { HelipodId = h.Id });

                Debug.WriteLine("StartDriveHelipod {0} {1} complete", b, h);
            }
        }

        private Bubble RandomAlien()
        {
            BubbleType bubbleType = (BubbleType)RandomGenerator.Instance.Next(1, (int)BubbleType.MaxAliens);

            return new Bubble() { Position = RandomEnemyPosition(), BubbleType = bubbleType };
        }

        private Vector2 RandomBubbleThrowPosition(Vector2 pos)
        {
            var newPos = pos + RandomGenerator.Instance.NextVector2(200f);
            newPos.X = MathHelper.Clamp(newPos.X, MainGameLogic.TopLeft.X, MainGameLogic.BottomRight.X);
            return newPos;
        }

        private Vector2 RandomEnemyPosition()
        {
            if (RandomGenerator.Instance.NextFloat() > 0.5f)
                // right of screen
                return RandomGenerator.Instance.NextVector2(new Vector2(MainGameLogic.BottomRight.X + 500, MainGameLogic.TopLeft.Y), new Vector2(MainGameLogic.BottomRight.X + 300f + 500, MainGameLogic.BottomRight.Y));
            else
                // left of screen
                return RandomGenerator.Instance.NextVector2(new Vector2(MainGameLogic.TopLeft.X - 300f - 500, MainGameLogic.TopLeft.Y), new Vector2(MainGameLogic.TopLeft.X - 500, MainGameLogic.BottomRight.Y));
        }

        private IEnumerable<Vector2> RandomScreenPositions()
        {
            while (true)
                yield return RandomGenerator.Instance.NextVector2(MainGameLogic.TopLeft, MainGameLogic.BottomRight);
        }

        private IEnumerable<Vector2> RandomRelativePositions(Vector2 pos, float radius)
        {
            while (true)
                yield return RandomGenerator.Instance.NextVector2(radius) + pos;
        }

        private Vector2 RandomEnemyFlyPosition()
        {
            return RandomScreenPositions().Take(10).Where(v => ShieldCollision(v) == null).Take(3).FirstOrDefault();
        }

        private Vector2 RandomEnemyFlyPosition(Vector2 pos)
        {
            var enemyPos = RandomRelativePositions(pos, 200f).Take(3).Where(v => ShieldCollision(v) == null).FirstOrDefault();
            if (enemyPos == Vector2.Zero)
                enemyPos = RandomScreenPositions().First();
            return enemyPos;
        }

        private Shield ShieldCollision(Vector2 pos)
        {
            GW.GroundWarState gwState = GameState.GamePlayState as GW.GroundWarState;
            return gwState.Shields.Where(sh => Collides(sh, pos)).FirstOrDefault();
        }

        private Shield ShieldCollision(Bubble bubble)
        {
            GW.GroundWarState gwState = GameState.GamePlayState as GW.GroundWarState;
            return gwState.Shields.Where(sh => Collides(sh, bubble)).FirstOrDefault();
        }

        private Vector2 RandomMissilePosition()
        {
            return RandomGenerator.Instance.NextVector2(MainGameLogic.BottomLeft, MainGameLogic.BottomRight + new Vector2(0, -30));
        }
    }

        

    class GroundWarInitializer : BasePlayInitializer
    {
        public GroundWarInitializer(MainGameLogic mainGameLogic)
            : base(mainGameLogic)
        {
        }

        internal override string GetTitle()
        {
            return "";
        }

        internal override void Initialize(GameState gameState)
        {
            GW.GroundWarState gwState = new GW.GroundWarState();
            gameState.GamePlayState = gwState;
            gameState.CountDownTimer = gameState.PlayTime;
            gameState.Bubbles.Clear();

            Vector2 topLeft = MainGameLogic.TopLeft;
            topLeft.X = 200;
            Vector2 bottomRight = MainGameLogic.BottomRight;
            bottomRight.X = 800 - 200;

            for (int i = 0; i < gameState.BubbleColors; i++)
                gameState.Bubbles.Add(
                    new Bubble() 
                    { 
                        Id = Guid.NewGuid(), Color = (BubbleColor)i, MachineState = InitialState.Instance, Position = new Vector2(RandomGenerator.Instance.NextFloat(MainGameLogic.BottomLeft.X / 2, MainGameLogic.BottomRight.X) - 800, MainGameLogic.BottomRight.Y), 
                        Gun = new Gun() { BulletType = BulletType.Laser },
                        GunPosition = new Vector2(20, 30)
                    });

            // bubbles to protect
            for (int i = 0; i < gameState.BubblesToProtect; i++)
                gameState.Bubbles.Add(
                    new Bubble()
                    {
                        Id = Guid.NewGuid(),
                        Color = BubbleColor.Yellow,
                        MachineState = InitialState.Instance,
                        Position = new Vector2(RandomGenerator.Instance.NextFloat(MainGameLogic.BottomLeft.X / 2, MainGameLogic.BottomRight.X) - 800, MainGameLogic.BottomRight.Y),
                        //Gun = new Gun() { BulletType = BulletType.Laser },
                        //GunPosition = new Vector2(20, 30)
                    });


            for (int i = 0; i < gameState.BubbleColors / 2; i++)
                gwState.Helipods.Add(new HeliPod() { Id = Guid.NewGuid(), Position = RandomGenerator.Instance.NextVector2(MainGameLogic.TopLeft, MainGameLogic.BottomRight), MachineState = InitialState.Instance });
                

            gameState.PlayState = PlayState.Playing;
        }

    }


    [XmlRoot(ElementName = "groundWarState", Namespace = "")]
    public class GroundWarState : BasePlayState
    {
        [XmlArray(ElementName = "bullets")]
        [XmlArrayItem(ElementName = "bullet")]
        public List<Bullet> Bullets = new List<Bullet>();

        [XmlArray(ElementName = "missiles")]
        [XmlArrayItem(ElementName = "missile")]
        public List<Missile> Missiles = new List<Missile>();

        [XmlArray(ElementName = "enemies")]
        [XmlArrayItem(ElementName = "enemy")]
        public List<Bubble> EnemyBubbles = new List<Bubble>();

        [XmlArray(ElementName = "gems")]
        [XmlArrayItem(ElementName = "gem")]
        public List<Gem> Gems = new List<Gem>();

        [XmlArray(ElementName = "helipods")]
        [XmlArrayItem(ElementName = "helipod")]
        public List<HeliPod> Helipods = new List<HeliPod>();

        [XmlArray(ElementName = "shields")]
        [XmlArrayItem(ElementName = "shield")]
        public List<Shield> Shields = new List<Shield>();

        internal override void PrepareForScreen(global::GameBase.Screen.PlayScreen playScreen)
        {
            base.PrepareForScreen(playScreen);

            Bullets.ForEach(b => b.PrepareForScreen(playScreen));
            Gems.ForEach(g => g.Inactive = false);
            Gems.ForEach(g => g.PrepareForScreen(playScreen));
            Helipods.ForEach(v => v.PrepareForScreen(playScreen));
            EnemyBubbles.ForEach(b => b.PrepareForScreen(playScreen));
            Missiles.ForEach(m => m.PrepareForScreen(playScreen));
            Shields.ForEach(m => m.PrepareForScreen(playScreen));
        }
    }

    public class BounceState : FreeFallState
    {
        public Vector2 TargetPosition;
        [XmlAttribute]
        public float JumpSpeed = 200;
        [XmlAttribute]
        public float Gravity = 1000;
        //[XmlAttribute]
        //[XmlAttribute]
        public float MinJumpSpeed = 400;
    }

    public class ReBounceState : FreeFallState
    {
        public Vector2 TargetPosition;
        [XmlAttribute]
        public float NewJumpSpeed = 200;
    }

    public class FlyState : MachineState
    {
        public Vector2 TargetPosition;
        [XmlAttribute]
        public float Speed;
    }

    public class SuckedState : MachineState
    {

    }

    //public class ShieldShockedState : MachineState
    //{
    //    public Vector2 TargetPosition;
    //    [XmlAttribute]
    //    public float NewJumpSpeed = 200;

    //}

    public class ShotState : MachineState
    {
    }

    public class ShockedState : MachineState
    {
        public Vector2 TargetPosition;
        [XmlAttribute]
        public float Speed;
    }

    public class GemBeatState : MachineState
    {
    }

    public class GemRainState : MachineState
    {
        public Vector2 InitialSpeed;
        public Vector2 TargetPosition;
    }

    public class GemDisappearState : MachineState
    {
    }

    public class GemPickedState : MachineState
    {
    }

    public class UserFlyingHelipodState : MachineState
    {
        public Vector2 TargetPosition;
    }

    public class DrivingHelipodState : MachineState
    {
        [XmlAttribute]
        public Guid HelipodId;
    }

    public class PrepLaunch : MachineState
    {
        public Vector2 TargetPosition;
    }
}
