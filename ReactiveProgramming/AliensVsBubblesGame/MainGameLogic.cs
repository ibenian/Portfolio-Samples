using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameBase.Logic;
using GameBase.Helper;
using Microsoft.Xna.Framework;
using GameBase.Screen;
using Microsoft.Xna.Framework.Input.Touch;
using Entropy.Screens;
using System.Diagnostics;
using OmegaDot.Helper;
using AppStatsLiveAgent;
using Microsoft.Devices;
using Entropy.Screens.PlayViews;
using Entropy.GamePlay.PlayLogics;
using Microsoft.Phone.Reactive;
using GameBase.Rx;
using System.Windows;


namespace Entropy.GamePlay
{
    class MainGameLogic : GameLogic
    {
        internal ParticleSystem ParticleSystem;
        public Vector2 TopLeft;
        public Vector2 BottomLeft;
        public Vector2 BottomRight;
        public Rectangle GameBounds;
        public float BubbleRadius;
        public float BubbleDiameter;
        internal float ScoreTransition = 0f;

        private List<HiScoreResponse> allRankings;
        internal string DynamicRankDisplay;
        internal string DynamicRankDisplayOld;
        internal string DynamicRankNextRankDisplay;
        internal float RankDisplayTransition = 0f;

        internal HiScoreResponse topScore;
        internal string topPlayerDisplay;
        internal string topPlayerCountryCode;

        BasePlayView playView;
        BasePlayLogic playLogic;

        internal TouchEventStreamSource TouchStream;

        internal Subject<BasePlayLogic> PlayComplete = new Subject<BasePlayLogic>();
        internal Subject<BubbleColor> ColorPick = new Subject<BubbleColor>();

        public MainGameLogic()
        {
            
        }

        internal BasePlayView PlayView
        {
            get { return playView; }
        }

        internal BasePlayLogic PlayLogic
        {
            get { return playLogic; }
        }

        public new GameState GameState
        {
            get { return (GameState)base.GameState; }
            set 
            { 
                base.GameState = value;
                this.DynamicRankDisplay = null;
                this.DynamicRankDisplayOld = null;
                this.DynamicRankNextRankDisplay = null;
#if DEV_FEATURES
                //if (this.GameState != null)
                //    this.GameState.Score = 180000;
#endif
                CheckRank();
                GetTopScore();
            }
        }

        private void GetTopScore()
        {
            if (allRankings != null)
            {
                topScore = null;
                topPlayerDisplay = null;
                topPlayerCountryCode = null;

                topScore = allRankings.LastOrDefault();

                if (topScore != null)
                {
                    topPlayerDisplay = topScore.PlayerName + " was here :)";
                    topPlayerCountryCode = topScore.CountryCode;
                }
            }
        }

        internal GameSettings GameSettings
        {
            get { return BubbleWarsGame.Instance.GameSettings; }
        }

        float zoom = 2f;
        float targetZoom = 1f;
        float camSpeedFactor = 1f;
        Vector2 camPos = new Vector2(400, 240);
        Vector2 camTargetPos = new Vector2(400, 240);
        Matrix camTransform = Matrix.Identity;

        internal Matrix CamTransform
        {
            get { return camTransform; }
            set { camTransform = value; }
        }

        internal float Zoom
        {
            get { return zoom; }
            set { zoom = value; }
        }

        internal Vector2 CamPos
        {
            get { return camPos; }
            set { camPos = value; }
        }

        internal Vector2 TransformCam(Vector2 pos)
        {
            return Vector2.Transform(pos, this.camTransform);
        }

        internal Vector2 InvTransformCam(Vector2 pos)
        {
            return Vector2.Transform(pos, Matrix.Invert(this.camTransform));
        }

        private void TriggerTrailEffect(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectMagicTrail, pos);
        }

        private void TriggerStarShine(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelperHud.Trigger((PlayScreen as GamePlayScreen).effectStarShine, pos);
        }

        internal void TriggerStarTrailEffect(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectStarTrail, pos);
        }

        internal void TriggerStarShineEffect(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectStarShine, pos);
        }

        internal void TriggerMarkLeaderEffect(Vector2 pos)
        {
            //(PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectMarkLeader, pos);
        }

        internal void TriggerWaveBroadcastEffect(Vector2 pos)
        {
            //(PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectWaveBoradcast, pos);
        }

        private void TriggerFlame(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectFlame, pos);
        }

        private void TriggerFireballEffect(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectFireball, pos);
        }

        internal void TriggerBeamEffect(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectBeamMeUp, pos);
        }

        internal void TriggerBasicSmokePlumeEffect(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectBasicSmokePlume, pos);
        }

        internal void TriggerHeavySmokePlumeEffect(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectHeavySmokePlume, pos);
        }

        internal void TriggerBasicExplosionEffect(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectBasicExplosion, pos);
        }

        internal void TriggerRocketThrusterEffect(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectRocketThruster, pos);
        }


        internal void TriggerBurningBulletEffect(Vector2 pos)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectBurningBullet, pos);
        }

        internal void PlayLaser(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).laserFx.PlayRandom(false, pos);
        }

        internal void PlayRocketLaunch(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).rocketLaunchFx.PlayRandom(false, pos);
        }

        internal void PlayExplosion(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).explosionFx.PlayRandom(false, pos);
        }

        internal Microsoft.Xna.Framework.Audio.SoundEffectInstance PlayChopper(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                return (PlayScreen as GamePlayScreen).chopperFx.PlayRandom(false, pos);
            else
                return null;
        }

        internal void PlayServo(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).servoFx.PlayRandom(false, pos);
        }

        internal void PlayMissileServo(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).missileServoFx.PlayRandom(false, pos);
        }



        internal void PlayBubbleThrow(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).plopFxThrow.PlayRandom(false, pos);
        }

        internal void PlayBubbleBurst(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).plopFxBurst.PlayRandom(false, pos);

            //VibrateController.Default.Start(TimeSpan.FromMilliseconds(4));
        }

        internal void PlayBoing(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).boingFx.PlayRandom(false, pos);
        }

        internal void PlayHitGround(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).plipFx.PlayRandom(false, pos);

            //VibrateController.Default.Start(TimeSpan.FromMilliseconds(4));
        }

        internal void PlayHitSide(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).pfuffFx.PlayRandom(false, pos);
        }

        internal void PlayHitBubble(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).splashFx.PlayRandom(false, pos);
        }

        internal void PlayRisset(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).rissetDrumFx.PlayRandom(false, pos);
        }

        internal void PlayBubbgunVibrate(float volume)
        {
            if (GameSettings.SoundOn)
            {
                if (volume > 0.3f)
                {
                    var fx = (PlayScreen as GamePlayScreen).rissetDrumFx;
                    fx.RandomPitchMax = volume * 0.9f;
                    fx.RandomPitchMin = fx.RandomPitchMax * 0.8f;
                    if (volume > 0.6f)
                        fx.Play(1, false, volume * 0.7f, null);
                    else
                        fx.PlayRandom(false, volume * 0.7f);

                    VibrateController.Default.Start(TimeSpan.FromMilliseconds(1));
                }
            }
        }

        internal void PlayFireworks(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).fireworksFx.PlayRandom(false, pos);
        }

        internal void PlaySwoosh(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).swooshFx.PlayRandom(false, pos);
        }

        internal void PlayMagicSurprise(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).magicSurpriseFx.PlayRandom(false, pos);
        }

        internal void PlayAddScore()
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).scoreAddFx.PlayRandom(false);
        }

        internal void PlayGameCompleted()
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).gameCompletedFx.PlayRandom(false);
        }

        internal void PlayLevelFail()
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).levelFailFx.PlayRandom(false);
        }

        internal void PlayLevelStart()
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).levelStartFx.PlayRandom(false);
        }

        internal void PlayAlienVoice(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).alienVoiceFx.PlayRandom(false, pos);
        }

        internal void PlayCountDownAlert(int secondsLeft)
        {
            if (GameSettings.SoundOn)
            {
                if (secondsLeft <= 10)
                    (PlayScreen as GamePlayScreen).countDownFx.Play(2, false);
                else if (secondsLeft <= 20)
                    (PlayScreen as GamePlayScreen).countDownFx.Play(1, false);
                //else if (secondsLeft <= 30)
                //    (PlayScreen as GamePlayScreen).countDownFx.Play(0, false);
            }
        }

        internal void PlayCountDownVoice(int secondsLeft)
        {
            if (GameSettings.SoundOn)
            {
                if (secondsLeft <= 10 && secondsLeft >= 1)
                    (PlayScreen as GamePlayScreen).countDownVoiceFx.Play(secondsLeft- 1, false);
            }
        }

        internal void PlayHeartbeat()
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).countDownFx.Play(0, false);
        }

        internal void PlayLevelSucceed()
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).levelStartFx.PlayRandom(false);
        }

        internal void PlayMelody(int index)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).melodyFx.Play(index, false);
        }

        internal void PlayRankUpMelody(int index)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).rankUpMelodyFx.Play(index, false);
        }

        internal void DisplayMessage(string msg, float time)
        {
            (PlayScreen as GamePlayScreen).DisplayMessage(msg, time);
        }

        internal override void OnContentLoaded()
        {
            base.OnContentLoaded();

            this.TopLeft = new Vector2(0f, 70f);
            this.BottomRight = new Vector2(ScreenWidth, ScreenHeight - 45f);
            this.BottomLeft = new Vector2(TopLeft.X, BottomRight.Y);
            this.BubbleDiameter = 66;
            this.BubbleRadius = this.BubbleDiameter / 2f;
            this.GameBounds = new Rectangle((int)(TopLeft.X + BubbleRadius), (int)(TopLeft.Y + BubbleRadius), (int)(BottomRight.X - TopLeft.X - BubbleRadius * 2), (int)(BottomRight.Y - TopLeft.Y - BubbleRadius * 2));

            TouchStream = new TouchEventStreamSource(this.PlayScreen.Game, false);
        }

        protected override void OnGameStateSet()
        {
            base.OnGameStateSet();


            if (this.GameState != null)
                InitAsyncLogic();

            //this.GameState.ThrownBubbles = new List<Bubble>(this.GameState.BubblesAtRest.Where(b => b.State == MotionState.FreeFly));

            //PreparePlayLogicAndView();

            ShowTip("MainGame_Init", string.Format("Welcome to Aliens vs Bubbles!\r\nTips will be shown to quickly introduce the game.", (int)PlayType.PlayTypeMax -1), 0.25f);
        }

        private void PreparePlayLogicAndView()
        {
            if (this.GameState != null)
            {
                this.playLogic = CreatePlayLogic(this.GameState);
                this.playLogic.PlayScreen = this.PlayScreen;
                this.playLogic.ScreenWidth = this.ScreenWidth;
                this.playLogic.ScreenHeight = this.ScreenHeight;
                this.playView = CreatePlayView(this.GameState, playLogic);

                // remove deleted/bursted bubbles
                GameState.Bubbles.Where(b => b.IsInAnyState<BurstState, RemovedState>())
                .ToList().ForEach(b => GameState.Bubbles.Remove(b));

                this.playLogic.GameState = this.GameState;

                if (this.GameState.PlayState == PlayState.Over)
                    PlayComplete.OnNext(this.playLogic);
            }
        }

        private void InitAsyncLogic()
        {
            GameScheduler.Reset();

            Observable.Timer(TimeSpan.FromSeconds(1), GameScheduler)
                .Subscribe(i =>
                {
                    if (this.GameState.PlayType == PlayType.None)
                        NewRandomPlay();
                    else
                        PreparePlayLogicAndView();
                });


            PlayComplete.Subscribe(p => 
                {
                    if (GameState.PlaySucceded)
                    {
                        int bonus = (int)(GameState.Power * 500);
                        DisplayMessage("Succeeded", 4f);
                        AddBonus(bonus);
                        BubbleWarsGame.LiveStats.TrackEvent("PlaySuccess", GameState.PlayType.ToString());
                    }
                    else
                    {
                        DisplayMessage("Failed", 4f);
                        BubbleWarsGame.LiveStats.TrackEvent("PlayFailed", GameState.PlayType.ToString());
                    }
                });

            PlayComplete.Delay(TimeSpan.FromSeconds(1), GameScheduler).TakeWhile(pl => GameState.PlayCounter < GameState.PlaysInAGame)
                .Subscribe(p => // Plays
                {
                    var newlyUnlockedPlayType = UnlockPlayTypeIfReached();
                    if (newlyUnlockedPlayType != PlayType.None)
                        NewPlay(newlyUnlockedPlayType);
                    else
                        NewRandomPlay();
                },
                () =>   // Plays complete
                {
                    OnLevelCompleted();
                }
                );
        }

        private void OnLevelCompleted()
        {
            LocalyticsSession.Instance.tagEvent("LevelCompleted", new Dictionary<string, string>() { { "level", this.GameState.CurrentLevelId.ToString() } });

            PlayRocketLaunch(Vector2.Zero);
            PlayRocketLaunch(new Vector2(ScreenWidth, 0));
            GameState.LevelCompleted = true;
            PlayGameCompleted();
            PlayMelody(0);
            var levelDef = LevelDefinitionFactory.GetLevelDefinition(this.GameState.CurrentLevelId);
            if (!GameState.BonusCalculated)
            {
                AddBonus(levelDef.LevelBonus + GameState.BonusIndex * levelDef.PerBubbleBonus);
                GameState.BonusIndex = 0;
                GameState.BonusCalculated = true;
            }
        }

        private PlayType UnlockPlayTypeIfReached()
        {
            PlayType newlyUnlockedPlayType = PlayType.None;
            GameSettings.CompletedLevels++;
            
            if (GameSettings.CompletedLevels % 10 == 0)
            {
                if (GameSettings.UnlockedPlays < (int)PlayType.PlayTypeMax - 1)
                {
                    GameSettings.UnlockedPlays++;
                    newlyUnlockedPlayType = (PlayType)GameSettings.UnlockedPlays;
                }
            }

            BubbleWarsGame.Instance.SaveGameSettings(true);

            if (newlyUnlockedPlayType != PlayType.None)
            {
                var gameInitializer = CreatePlayInitializer(newlyUnlockedPlayType);
                BubbleWarsGame.LiveStats.TrackEvent("PlayUnlocked", newlyUnlockedPlayType.ToString());
                MessageBox.Show(string.Format("Congratulations!\r\nYou have just unlocked {0}!", gameInitializer.GetTitle()), "New Game Type!", MessageBoxButton.OK);
            }

            return newlyUnlockedPlayType;
        }

        private void NewRandomPlay()
        {
            //if (GameState.PlayCounter < GameState.PlaysInAGame)
            var maxPlayType = Math.Min(GameSettings.UnlockedPlays + 1, (int)PlayType.PlayTypeMax);
            //var playType = (PlayType)RandomGenerator.Instance.Next(1, maxPlayType);
            var playType = PlayType.GroundWar;

            NewPlay(playType);
        }

        private void NewPlay(PlayType playType)
        {
            if (this.GameState.GameOver)
                return;

            // Smoke out the screen
            //for (int i = 0; i < 10; i ++)
            ParticleSystem.CreateBurst(new Vector2(0, 350), new Vector2(800, 480), 10);
            this.PlayHitBubble(new Vector2(400, 240));

            //this.GameScheduler.Reset();
            this.GameState.PlayCounter ++;
            this.GameState.PlayType = playType;
            this.GameState.PlayState = PlayState.NotStarted;
            this.GameState.PlaySucceded = false;
            var gameInitializer = CreatePlayInitializer(this.GameState);
            gameInitializer.Initialize(this.GameState);
            this.GameState.Title = gameInitializer.GetTitle();

            if (this.GameState.Title != null)
                DisplayMessage(this.GameState.Title, 4f);
            PreparePlayLogicAndView();

            BubbleWarsGame.LiveStats.TrackEvent("NewPlay", playType.ToString());
        }

        private BasePlayLogic CreatePlayLogic(GameState gameState)
        {
            switch (gameState.PlayType)
            { 
                //case PlayType.Test:
                //    return new TestLogic(this);
                case PlayType.GroundWar:
                    return new GroundWarLogic(this);
                default:
                    return null;
            }
        }

        private BasePlayInitializer CreatePlayInitializer(GameState gameState)
        {
            return CreatePlayInitializer(gameState.PlayType);
        }

        private BasePlayInitializer CreatePlayInitializer(PlayType playType)
        {
            switch (playType)
            {
                //case PlayType.Test:
                //    return new TestPlayInitializer(this);
                case PlayType.GroundWar:
                    return new GroundWarInitializer(this);
                default:
                    return null;
            }
        }

        private BasePlayView CreatePlayView(GameState gameState, BasePlayLogic playLogic)
        {
            switch (gameState.PlayType)
            {
                //case PlayType.Test:
                //    return new TestView(this, playLogic);
                case PlayType.GroundWar:
                    return new GroundWarView(this, playLogic);
                default:
                    return null;
            }
        }

        internal void CreateGameState()
        {
            this.CreateGameState(1);
        }

        internal void CreateGameState(int levelId)
        {
            var levelDefinition = LevelDefinitionFactory.GetLevelDefinition(levelId);
            CreateGameStateForLevel(levelDefinition);
        }

        internal void CreateGameStateForLevel(LevelDefinition levelDefinition)//int rows, int startRow)
        {
            if (levelDefinition == null)
                return;

            GC.Collect();

            var gameState = new GameState();
            gameState.BonusIndex = 0;
            gameState.CurrentLevelId = levelDefinition.LevelId;
            gameState.BubbleColors = levelDefinition.BubbleColors;
            gameState.Speed = levelDefinition.Speed;
            gameState.PlaysInAGame = levelDefinition.PlaysInAGame;
            gameState.PlayTime = levelDefinition.PlayTime;
            gameState.EnemyDensity = levelDefinition.EnemyDensity;
            gameState.MissileChance = levelDefinition.MissileChance;
            gameState.ShieldChance = levelDefinition.ShieldChance;
            gameState.CrystChance = levelDefinition.CrystChance;
            gameState.BubblesToProtect = levelDefinition.BubblesToProtect;
            //gameState.MissileDensity = levelDefinition.MissileDensity;

            //var gameInitializer = CreatePlayInitializer(gameState);
            //gameInitializer.Initialize(gameState);

            this.GameState = gameState;

            (PlayScreen as GamePlayScreen).RandomBackground();

            BubbleWarsGame.LiveStats.TrackEvent(EventType.NewLevel, GameState.CurrentLevelId.ToString());
        }

        internal void SwitchGameState(int levelId)
        {
            var oldGameState = this.GameState;
            CreateGameState(levelId);
            this.GameState.Score = oldGameState.Score;
            this.GameState.ScoreToAdd = oldGameState.ScoreToAdd;
            this.GameState.BonusToAdd = oldGameState.BonusToAdd;
            //this.GameState.Power = oldGameState.Power;
            this.GameState.Power = 1f;
            this.GameState.PowerToAdd = oldGameState.PowerToAdd;
            CheckRank();
        }

        internal override bool HandleInput(InputState input, TouchCollection touchState)
        {

            if (!this.IsActive || this.GameState.GameOver)
                return false;
            else
            {
                if (this.playLogic != null && this.GameState.PlayState == PlayState.Playing)
                    if (this.playLogic.HandleInput(input, touchState))
                        return true;

                TouchStream.HandleInput(touchState);

                return false;
            }
        }

        internal override void Update(GameTime gameTime)
        {
            
            base.Update(gameTime);

            if (this.playLogic != null)
                this.playLogic.Update(gameTime);

            
        }

        internal override void UpdateGameState()
        {
            Debug.WriteLine(">>----------------- Update frame {0:00000} start ----------------- {1}", GameScheduler.TotalGameFrames, GameScheduler.TotalTime);

            base.UpdateGameState();

            if (this.ScoreTransition > 0f && this.ScoreTransition < 1f)
                this.ScoreTransition += GameState.ElapsedTime * 4f;

            if (this.RankDisplayTransition > 0f && this.RankDisplayTransition < 1f)
                this.RankDisplayTransition += GameState.ElapsedTime * 4f;

            if (this.GameState.BonusToAdd >= 100)
            {
                if (((int)(GameState.TotalGameTime * 10) % 2 == 0))
                {
                    var scoreAddStep = GameState.BonusToAdd / 100 > 20 ? 500 : 100;

                    this.GameState.BonusToAdd -= scoreAddStep;
                    this.AddScore(scoreAddStep, false);
                }
            }

            if (this.GameState.ScoreToAdd > 0)
            {
                this.GameState.Score += this.GameState.ScoreToAdd * 1; // temp
                this.GameState.ScoreToAdd = 0;
                this.ScoreTransition = 0.001f;
                CheckRank();
                PlayAddScore();
            }

            if (this.playLogic != null)
                this.playLogic.UpdateGameState();


            if (GameState.LevelCompleted)
                if (RandomGenerator.Instance.NextFloat() < 0.5f)
                    TriggerRocketThrusterEffect((PlayScreen as GamePlayScreen).spacePlatformLeft.Offset + new Vector2(20, 110));
                else
                    TriggerRocketThrusterEffect((PlayScreen as GamePlayScreen).spacePlatformRight.Offset + new Vector2(350, 110));


            UpdateZoom();

            Debug.WriteLine("<<----------------- Update frame {0:00000} end   ----------------- {1}", GameScheduler.TotalGameFrames, GameScheduler.TotalTime);
        }

        private void UpdateZoom()
        {
            var playerBubbles = from b in this.GameState.Bubbles where b.BubbleType == BubbleType.Bubble select b;
            if (playerBubbles.FirstOrDefault() != null)
            {
                var minX = (from b in playerBubbles select b.Position.X).Min();
                var maxX = (from b in playerBubbles select b.Position.X).Max();
                var minY = (from b in playerBubbles select b.Position.Y).Min();
                var maxY = (from b in playerBubbles select b.Position.Y).Max();
                var distX = Math.Abs(maxX - minX);
                var distY = Math.Abs(maxY - minY);

                var totalScale = this.ScreenHeight;

                if (distX > distY)
                {
                    if (distX != 0)
                        this.targetZoom = MathHelper.Clamp(totalScale / (distX + 250f), 0.7f, 1.1f);
                }
                else
                {
                    if (distY != 0)
                        this.targetZoom = MathHelper.Clamp(totalScale / (distY + 350f), 0.8f, 1.1f);
                }
                this.camTargetPos = new Vector2((maxX + minX) / 2, ((maxY + minY) / 2));
            }

            this.CamPos += (this.camTargetPos - this.camPos).OfMagnitude((this.camTargetPos - this.camPos).Length() * GameState.ElapsedTime * camSpeedFactor);
            this.Zoom += (this.targetZoom - this.zoom) * GameState.ElapsedTime * camSpeedFactor;

            this.CamTransform = Matrix.CreateTranslation(new Vector3(-camPos.X, -camPos.Y, 0)) * Matrix.CreateScale(zoom) * Matrix.CreateTranslation(new Vector3(ScreenWidth/2, ScreenHeight * 0.6f, 0));
        }

        //private void UpdateZoom()
        //{
        //    var playerBubbles = from b in this.GameState.Bubbles where b.BubbleType == BubbleType.Bubble select b;
        //    if (playerBubbles.FirstOrDefault() != null)
        //    {
        //        var minX = (from b in playerBubbles select b.Position.X).Min();
        //        var maxX = (from b in playerBubbles select b.Position.X).Max();
        //        var minY = (from b in playerBubbles select b.Position.Y).Min();
        //        var maxY = (from b in playerBubbles select b.Position.Y).Max();
        //        var distX = Math.Abs(maxX - minX) + 250;
        //        var distY = Math.Abs(maxY - minY) + 250;

        //        var totalScale = this.ScreenHeight;

        //        if (distX > distY)
        //        {
        //            if (distX != 0)
        //                this.targetZoom = MathHelper.Clamp(totalScale / distX, 0.7f, 1.3f);
        //        }
        //        else
        //        {
        //            if (distY != 0)
        //                this.targetZoom = MathHelper.Clamp(totalScale / distY, 0.7f, 1.3f);
        //        }
        //        this.camTargetPos = new Vector2((maxX + minX) / 2, ((maxY + minY) / 2));
        //    }

        //    this.CamPos += (this.camTargetPos - this.camPos).OfMagnitude((this.camTargetPos - this.camPos).Length() * GameState.ElapsedTime * camSpeedFactor);
        //    this.Zoom += (this.targetZoom - this.zoom) * GameState.ElapsedTime * camSpeedFactor;
        //}

        //private void UpdateBubblex(Bubble bubble)
        //{
        //    // Delay burst
        //    if (bubble.BurstDelay >= 0)
        //    {
        //        bubble.BurstDelay -= GameState.ElapsedTime;
        //        if (bubble.BurstDelay <= 0)
        //        {
        //            BurstBubble(bubble);
        //        }
        //    }

        //    if (bubble.State == MotionState.Burst || bubble.State == MotionState.Rest)
        //    {
        //    }

        //    //CheckSpeed(bubble);

        //    if (bubble.State == MotionState.FreeFly)
        //    {
        //        bubble.Position += bubble.Speed * GameState.ElapsedTime;
        //        if (bubble.Position.X - BubbleRadius <= TopLeft.X || bubble.Position.X + BubbleRadius >= BottomRight.X)
        //        {
        //            CheckSideCollision(bubble);
        //        }

        //        //if (bubble.Position.Y - BubbleRadius <= Math.Max(GameState.PistonY, TopLeft.Y))
        //        //{
        //        //    //bubble.Position.Y = GameState.PistonY + BubbleRadius;
        //        //    //ShakeBubble(bubble);
        //        //    // Put bubble to rest
        //        //    //FreefallBubble(bubble);
        //        //    OnCollision(bubble, null);
        //        //}

        //        if (bubble.Position.Y + BubbleRadius >= ScreenHeight && bubble.Speed.Y > 0)
        //            bubble.Speed.Y *= -1;
        //        //CheckSpeed(bubble);
        //    }
        //    else if (bubble.State == MotionState.FreeFall)
        //    {
        //        bubble.Position += bubble.Speed * GameState.ElapsedTime;

        //        bubble.Speed += new Vector2(0f, 1000f * GameState.ElapsedTime);        // acceleration

        //        if (bubble.Position.X - BubbleRadius <= TopLeft.X || bubble.Position.X + BubbleRadius >= BottomRight.X)
        //        {
        //            CheckSideCollision(bubble);
        //        }

        //        if (bubble.Position.Y + BubbleRadius * 1.0f >= ScreenHeight) //BottomRight.Y + 200f)
        //        {
        //            if (bubble.Position.Y + BubbleRadius * 0.7f >= ScreenHeight) // bounce
        //            {
        //                PlayHitGround(bubble.Position);
        //                bubble.Speed = new Vector2(bubble.Speed.X, -bubble.Speed.Y) / 1.5f;  // 1.23f;
        //                bubble.Position.Y = ScreenHeight - BubbleRadius * 0.7f;
        //            }

        //            // Squeze ball
        //            bubble.Scale.Y = (ScreenHeight - bubble.Position.Y) / BubbleRadius;
        //        }
        //        else
        //            bubble.Scale.Y = 1f;

        //        //if (GameState.TotalGameTime - bubble.StateEnterTime > 2f)
        //        //    if (GameState.TotalGameTime - bubble.StateEnterTime > 5f || RandomGenerator.Instance.Next(20) == 0)
        //        //        BurstBubble(bubble);

        //        //CheckSpeed(bubble);
        //    }
        //    else if (bubble.State == MotionState.Burst)
        //    {
        //        bubble.Scale += Vector2.One * (1f * GameState.ElapsedTime);
        //        //if (bubble.Scale.Length() > 8f)
        //        //    bubblesToRemove.Add(bubble);

        //        //CheckSpeed(bubble);
        //    }
        //    else
        //    {
        //        // Resting or not thrown motion

        //        // Approach rest location if not alrady steady
        //        if (bubble.Stability == StabilityState.Stabilizing)
        //        {
        //            // Stabilizing
        //            if (Vector2Helper.EqualsWithTolerence(bubble.Speed, Vector2.Zero, 1f) && Vector2Helper.EqualsWithTolerence(bubble.Position, bubble.TargetPosition, 0.5f))
        //            {
        //                // Stabilized
        //                bubble.Stability = StabilityState.Stable;
        //                bubble.Position = bubble.TargetPosition;
        //                bubble.Speed = Vector2.Zero;
        //                //bubble.Scale = Vector2.One;
        //                //bubbleStabilized = true;
        //                //bubblesStabilized.Add(bubble);
        //            }
        //            else
        //            {
        //                // Not stabilized yet
        //                var dir = bubble.TargetPosition - bubble.Position;
        //                var norm = dir;
        //                norm.Normalize();
        //                //bubble.Speed -= (100f / dir.Length()) * norm * GameState.ElapsedTime;
        //                //bubble.Speed = dir / 0.1f;
        //                bubble.Speed = dir * 100f * GameState.ElapsedTime;
        //                //CheckSpeed(bubble);
        //                bubble.Position += bubble.Speed * GameState.ElapsedTime;
        //                if (bubble.Speed.Length() < 10f)
        //                    bubble.Scale += (Vector2.One - bubble.Scale) * 10f * GameState.ElapsedTime;
        //                else
        //                    bubble.Scale += new Vector2((float)Math.Sin(3 * 2 * Math.PI * (double)GameState.TotalGameTime) * 0.6f, (float)Math.Cos(3 * 3.5 * Math.PI * (double)GameState.TotalGameTime) * 0.6f) * GameState.ElapsedTime;


        //            }
        //        }
        //        else
        //        {
        //            // Stabilized


        //        }

        //        if (bubble.State == MotionState.Rest)
        //        {
                    
        //        }
        //    }

        //    // Update blink
        //    if (bubble.ShineScale == 1f)
        //    {
        //        // Start a blink randomly
        //        if (RandomGenerator.Instance.Next(1000) == 0)
        //        {
        //            bubble.ShineScale = 0f;
        //            bubble.ShileScaleSpeed = 10f;
        //        }
        //    }
        //    else
        //    {
        //        // Already blinking
        //        bubble.ShineScale += bubble.ShileScaleSpeed * GameState.ElapsedTime;
        //        bubble.ShileScaleSpeed -= GameState.ElapsedTime;
        //        if (bubble.ShineScale > 0.999f)
        //        {
        //            // Blink end
        //            bubble.ShineScale = 1f;
        //            bubble.ShileScaleSpeed = 0f;
        //        }
        //    }


        //}

        //private void CheckSideCollision(Bubble bubble)
        //{
        //    if (bubble.Position.X - BubbleRadius <= TopLeft.X)
        //    {
        //        // bounc from left side
        //        PlayHitSide(bubble.Position);
        //        bubble.Speed.X = Math.Abs(bubble.Speed.X);
        //        bubble.Position.X = TopLeft.X + BubbleRadius;
        //        return;
        //    }

        //    if (bubble.Position.X + BubbleRadius >= BottomRight.X)
        //    {
        //        // bounc from right side
        //        PlayHitSide(bubble.Position);
        //        bubble.Speed.X = -Math.Abs(bubble.Speed.X);
        //        bubble.Position.X = BottomRight.X - BubbleRadius;
        //        return;
        //    }
        //}

        private void CheckRank()
        {
            this.DynamicRankDisplayOld = this.DynamicRankDisplay;
            HiScoreResponse scoreJustPassed = null;
            HiScoreResponse nextScore = null;
            if (this.GameState != null)
            {
                if (GameState.Score < BubbleWarsGame.LiveScores.LocalData.MinScoreToSubmit)
                {
                    // Below min required score
                    DynamicRankDisplay = string.Format("Not Ranked!");
                    DynamicRankNextRankDisplay = string.Format("Required {0}", BubbleWarsGame.LiveScores.LocalData.MinScoreToSubmit);
                    return;
                }

                if (allRankings == null)
                {
                    var hiScores = BubbleWarsGame.GetLastDownloadedHiScores();
                    if (hiScores != null)
                    {
                        allRankings = new List<HiScoreResponse>(hiScores.OrderBy(hs => hs.Score).Where(hs => hs.Score >= BubbleWarsGame.LiveScores.LocalData.MinScoreToSubmit));  // ascending order
                        GetTopScore();

                        if (allRankings.FirstOrDefault(hs => hs.Range == ScoreTableRange.Day) == null)      // No daily ranking submitted yet
                            allRankings.Insert(0, new HiScoreResponse() { Rank = 1, Range = ScoreTableRange.Day, Score = BubbleWarsGame.LiveScores.LocalData.MinScoreToSubmit - 5 }); // Add a dummy daily point to pass
                    }

                }

                if (allRankings != null && allRankings.Count > 0)
                {
                    // find the score that's just passed and the next score
                    var ien = allRankings.GetEnumerator();

                    while (ien.MoveNext())
                    {
                        if (ien.Current.Score >= GameState.Score)
                        {
                            nextScore = ien.Current;
                            break;
                        }
                        scoreJustPassed = ien.Current;
                    }
                }
                else
                {
                    DynamicRankDisplay = "Live Ranking"; // Ranking not available yet
                    DynamicRankNextRankDisplay = "Not Available";
                    return;
                }


            }



            if (scoreJustPassed != null)
            {
                // In one of the ranges
                if (scoreJustPassed.Range == ScoreTableRange.AllTime && scoreJustPassed.Rank <= 10)
                {
                    // All time top 10
                    DynamicRankDisplay = string.Format("    TOP {0}!", scoreJustPassed.Rank);
                    DynamicRankNextRankDisplay = null;      // No next score
                }
                else
                {
                    // Any one of top 100 ranges
                    DynamicRankDisplay = string.Format("{1} #{0}", scoreJustPassed.Rank, HiScoresScreen.GetRangeName(scoreJustPassed.Range));
                }
            }
            else
            {
                DynamicRankDisplay = string.Format("Below top {0}", BubbleWarsGame.LiveScores.MaxHiScoresToRequest);
            }

            if (nextScore != null)
                DynamicRankNextRankDisplay = string.Format("{1} {0}", nextScore.Score + 5, "Rank up");

            if (DynamicRankDisplay != DynamicRankDisplayOld)
            {
                this.RankDisplayTransition = 0.001f;

                if (DynamicRankDisplayOld != null)
                    if (scoreJustPassed != null && scoreJustPassed.Range == ScoreTableRange.AllTime)
                        PlayRankUpMelody(1);
                    else
                        PlayRankUpMelody(0);
            }
        }

        //internal void FreefallBubble(Bubble bubble)
        //{
        //    if (bubble.State != MotionState.Burst)
        //    {
        //        bubble.State = MotionState.FreeFall;
        //        bubble.StateEnterTime = GameState.TotalGameTime;
        //        bubble.Speed = new Vector2(RandomGenerator.Instance.NextFloat(-100f, 100f), RandomGenerator.Instance.NextFloat(50f));
        //        bubble.BurstDelay = RandomGenerator.Instance.NextFloat(1f, 4f);
        //    }
        //}

        //private void FreefallBubbleKeepSpeed(Bubble bubble)
        //{
        //    if (bubble.State != MotionState.Burst)
        //    {
        //        bubble.State = MotionState.FreeFall;
        //        bubble.StateEnterTime = GameState.TotalGameTime;
        //        bubble.BurstDelay = RandomGenerator.Instance.NextFloat(1f, 4f);
        //    }
        //}

        //internal void BurstBubble(Bubble bubble)
        //{
        //    PlayBubbleBurst(bubble.Position);
        //    bubble.BurstDelay = -1f;
        //    bubble.State = MotionState.Burst;
        //    bubble.StateEnterTime = GameState.TotalGameTime;
        //    bubble.Scale = Vector2.One;
        //    bubble.Rotation = RandomGenerator.Instance.NextFloat((float)Math.PI * 2f);
        //    GameState.LastAttentionPosition = bubble.Position;
        //}

        //internal void BurstBubble(Bubble bubble, float burstDelay)
        //{
        //    bubble.BurstDelay = burstDelay;
        //}


        //internal void ShakeBubble(Bubble bubble)
        //{
        //    bubble.Stability = StabilityState.Stabilizing;
        //    bubble.Speed = RandomGenerator.Instance.NextVector2(400f);
        //    bubble.Position += RandomGenerator.Instance.NextVector2(20f);
        //}

        internal void AddScore(int score)
        {
            AddScore(score, true);
        }

        internal void AddScore(int score, bool displayScore)
        {
            if (!GameState.GameOver)
            {
                TriggerStarShine(RandomGenerator.Instance.NextVector2(new Vector2(20, 20), new Vector2(180, 60)));
                //ParticleSystem.CreateShine(new Vector2(20, 20), new Vector2(180, 60), 1);
                GameState.ScoreToAdd += score;
                if (displayScore)
                    DisplayMessage("+" + score.ToString(), 1f);
            }
            else
                GameState.BonusToAdd = 0;
        }

        internal void AddBonus(int bonus)
        {
            if (!GameState.GameOver)
            {
                //if (bonus > 10000)
                //    Debug.WriteLine("large bonus");

                bonus = (int)(bonus / 5) * 5;

                if (bonus > 0)
                {

                    GameState.BonusToAdd += bonus;
                    DisplayMessage("+" + bonus.ToString(), 1f);
                }
            }
            else
                GameState.BonusToAdd = 0;
        }

        internal void AddPower(float power)
        {
            //if (GameState.PlayState == PlayState.Playing)
            if (GameState.Power > 0)
            {
                if (GameState.Power < 0.5f && power < 0)
                    ShowTip("MainGame_RedPower", "Power has been reduced.  Watch the power bar on the left and keep it as high as possible to get higher score.\r\nGame will be over when you lose all power.", 0.5f, 40f);

                if (GameState.Power < 1f && power > 0)
                    ShowTip("MainGame_IncPower", "Power has been increased.  Watch the power bar on the left and keep it as high as possible to get higher score.\r\nGame will be over when you lose all power.", 0.5f, 50f);

                GameState.Power = MathHelper.Clamp(GameState.Power + power, 0f, 1f);

                if (!GameState.GameOver)
                    if (GameState.Power == 0)
                        OnGameOver();
            }
        }

        private void OnGameOver()
        {
            LocalyticsSession.Instance.tagEvent("GameOver", new Dictionary<string, string>() { { "level", this.GameState.CurrentLevelId.ToString() } });

            //if (!GameState.GameOver)
            {
                GameState.GameOver = true;
                GameState.PlayState = PlayState.Over;
                ShowTip("MainGame_Pow0", "Game is over because power has reached to 0.\r\nYou must keep it high to keep in game and earn higher scores.", 0.5f);
                SaveGame(false);
            }
        }

        internal void SaveGame(bool async)
        {
            if (this.GameState == null || this.GameState.GameOver)
                FileHelper.Delete("GameState.txt");
            else
                if (async)
                    FileHelper.SaveObjectBase64Async<GameState>(GameState, "GameState.txt");
                else
                    FileHelper.SaveObjectBase64<GameState>(GameState, "GameState.txt");
        }

        internal void LoadGame()
        {
            this.GameState = FileHelper.LoadObjectFromBase64<GameState>("GameState.txt");
            if (this.GameState == null)
                CreateGameState();
            else
            {
                this.GameState.PrepareForScreen(this.PlayScreen);
                BubbleWarsGame.LiveStats.TrackEvent(EventType.ResumeLevel, this.GameState.CurrentLevelId.ToString());
            }
        }

    }
}
