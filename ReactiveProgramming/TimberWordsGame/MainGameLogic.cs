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
using Entropy.GamePlay.Words;


namespace Entropy.GamePlay
{
    class MainGameLogic : GameLogic
    {
        internal WordRepository wordRepository;
        internal ParticleSystem ParticleSystem;
        private Color bgColor = Color.White;
        private float lighting = 1f;
        public Vector2 TopLeft;
        public Vector2 BottomLeft;
        public Vector2 BottomRight;
        public Vector2 BoardSize;
        public Rectangle GameBounds;
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
            get { return EntropyGame.Instance.GameSettings; }
        }

        internal void TriggerTrailEffect(Vector2 pos)
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

        internal void TriggerBeamEffect(Vector2 min, Vector2 max)
        {
            (PlayScreen as GamePlayScreen).mercuryHelper.Trigger((PlayScreen as GamePlayScreen).effectBeamMeUp, RandomGenerator.Instance.NextVector2(min, max));
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

        


        internal float GetPitchFactorForGame()
        {
            //MathHelper.Clamp(GameState.Speed / 3f, 1, 2) * 
            return MathHelper.Clamp(1f - (float)GameState.CountDownTimer/GameState.PlayTime, 0.2f, 1f);
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

        

        internal void PlayLevelSucceed()
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).levelStartFx.PlayRandom(false);
        }

        internal void PlayMelody(int index)
        {
            if (GameSettings.SoundOn)
            {
                (PlayScreen as GamePlayScreen).melodyFx.Pitch = 0f;
                (PlayScreen as GamePlayScreen).melodyFx.Play(index, false);
            }
        }

        internal void PlayMelodySlow(int index)
        {
            if (GameSettings.SoundOn)
            {
                (PlayScreen as GamePlayScreen).melodyFx.Pitch = -0.5f;
                (PlayScreen as GamePlayScreen).melodyFx.Play(index, false);
            }
        }

        internal void PlayRankUpMelody(int index)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).rankUpMelodyFx.Play(index, false);
        }

        internal void PlayMagicalTwinkle(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).magicalTwinkle.PlayRandom(false, pos);
        }

        internal void PlayMagicalBells(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).magicalBells.PlayRandom(false, pos);
        }

        internal void PlayGemCollect(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).gemCollect.PlayRandom(false, pos);
        }

        internal void PlayMagicSurprise(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).magicSurprise.PlayRandom(false, pos);
        }

        internal void PlayWhistleDown(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).whistleDown.PlayRandom(false, pos);
        }

        internal void PlayWhistleUp(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).whistleUp.PlayRandom(false, pos);
        }

        internal void PlayWoodHitB(Vector2 pos)
        {
            if (GameSettings.SoundOn)
            {
                (PlayScreen as GamePlayScreen).woodHitb.PitchFactor = GetPitchFactorForGame();
                (PlayScreen as GamePlayScreen).woodHitb.PlayRandom(false, pos);
            }
        }

        internal void PlayWoodHitC(Vector2 pos)
        {
            if (GameSettings.SoundOn)
            {
                (PlayScreen as GamePlayScreen).woodHitc.PitchFactor = GetPitchFactorForGame();
                (PlayScreen as GamePlayScreen).woodHitc.PlayRandom(false, pos);
            }
        }

        internal void PlayWoodOpenSqueak()
        {
            if (GameSettings.SoundOn)
            {
                (PlayScreen as GamePlayScreen).woodOpenSqueak.PitchFactor = GetPitchFactorForGame();
                (PlayScreen as GamePlayScreen).woodOpenSqueak.PlayRandom(false);
            }
        }

        internal void PlayWoodDoor()
        {
            if (GameSettings.SoundOn)
            {
                (PlayScreen as GamePlayScreen).woodDoor.PitchFactor = GetPitchFactorForGame();
                (PlayScreen as GamePlayScreen).woodDoor.PlayRandom(false);
            }
        }

        internal void PlaySwordHitWood(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).swordHitWood.PlayRandom(false, pos);
        }

        internal void PlaySwordHitWood()
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).swordHitWood.PlayRandom(false);
        }

        internal void PlayDoorAntique()
        {
            if (GameSettings.SoundOn)
            {
                (PlayScreen as GamePlayScreen).doorAntique.PitchFactor = GetPitchFactorForGame();
                (PlayScreen as GamePlayScreen).doorAntique.PlayRandom(false);
            }
        }

        internal void PlayCuckooStart()
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).cuckooStart.PlayRandom(false);
        }

        internal void PlayCuckooMidTime()
        {
            if (GameSettings.SoundOn)
            {
                (PlayScreen as GamePlayScreen).cuckoo.PitchFactor = GetPitchFactorForGame();
                (PlayScreen as GamePlayScreen).cuckoo.PlayRandom(false);
            }
        }

        internal void PlayCuckooWarn()
        {
            if (GameSettings.SoundOn)
            {
                (PlayScreen as GamePlayScreen).cuckoo.PitchFactor = 2f;
                (PlayScreen as GamePlayScreen).cuckoo.PlayRandom(false);
            }
        }

        internal void PlayCuckoo()
        {
            if (GameSettings.SoundOn)
            {
                (PlayScreen as GamePlayScreen).cuckoo.PitchFactor = 1f;
                (PlayScreen as GamePlayScreen).cuckoo.PlayRandom(false);
            }
        }

        internal void PlayZap(Vector2 pos)
        {
            if (GameSettings.SoundOn)
                (PlayScreen as GamePlayScreen).zap.PlayRandom(false, pos);
        }

        internal void DisplayMessage(string msg, float time)
        {
            (PlayScreen as GamePlayScreen).DisplayMessage(msg, time);
        }

        internal void SetDebugMsg(string msg)
        {
            (PlayScreen as GamePlayScreen).SetDebugMsg(msg);
        }

        internal void SetBgColor(Color color)
        {
            lighting = GameState.Power;
            bgColor = color;
            (PlayScreen as GamePlayScreen).bgLayer.Color = Color.Lerp(Color.Navy, color, lighting);
        }

        internal void SetNextBackground()
        {
            (PlayScreen as GamePlayScreen).SetNextBackground();
        }

        internal void SetPrevBackground()
        {
            (PlayScreen as GamePlayScreen).SetPrevBackground();
        }

        internal override void OnContentLoaded()
        {
            base.OnContentLoaded();

            

            TouchStream = new TouchEventStreamSource(this.PlayScreen.Game, false);
        }

        protected override void OnGameStateSet()
        {
            base.OnGameStateSet();

            this.TopLeft = new Vector2(0f, 70f);
            this.BottomRight = this.TopLeft + new Vector2(GameState.Cols * GameState.CellSize.X, GameState.Rows * GameState.CellSize.Y);
            this.BottomLeft = new Vector2(TopLeft.X, BottomRight.Y);
            this.GameBounds = new Rectangle((int)(TopLeft.X), (int)(TopLeft.Y), (int)(BottomRight.X - TopLeft.X), (int)(BottomRight.Y - TopLeft.Y));
            this.BoardSize = new Vector2(BottomRight.X - TopLeft.X, BottomRight.Y - TopLeft.Y);

            if (this.GameState != null)
                InitAsyncLogic();

            //this.GameState.ThrownBubbles = new List<Bubble>(this.GameState.BubblesAtRest.Where(b => b.State == MotionState.FreeFly));

            //PreparePlayLogicAndView();

            //ShowTip("MainGame_Init", string.Format("Welcome to Timber Words!\r\nTips will be shown to quickly introduce the game.", (int)PlayType.PlayTypeMax -1), 0f); // 0.25f);
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
                //GameState.Bubbles.Where(b => b.IsInAnyState<BurstState, RemovedState>())
                //.ToList().ForEach(b => GameState.Bubbles.Remove(b));

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
                        EntropyGame.LiveStats.TrackEvent("PlaySuccess", GameState.PlayType.ToString());
                    }
                    else
                    {
                        DisplayMessage("Failed", 4f);
                        EntropyGame.LiveStats.TrackEvent("PlayFailed", GameState.PlayType.ToString());
                    }
                });

            PlayComplete.Delay(TimeSpan.FromSeconds(1), GameScheduler).TakeWhile(pl => GameState.PlayCounter < 1)
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
                    if (GameState.PlaySucceded)
                        OnLevelCompleted();
                    else
                        OnGameOver();
                }
                );
        }

        private void OnLevelCompleted()
        {
            LocalyticsSession.Instance.tagEvent("LevelCompleted", new Dictionary<string, string>() { { "level", this.GameState.CurrentLevelId.ToString() } });

            //TimberWordsGame.Instance.Music.Stop();
            //PlayMelody(0);
            PlaySwordHitWood();
            PlayDoorAntique();
            GameState.LevelCompleted = true;
            //PlayGameCompleted();
            var levelDef = LevelDefinitionFactory.GetLevelDefinition(this.GameState.CurrentLevelId);
            if (!GameState.BonusCalculated)
            {
                AddBonus(levelDef.LevelBonus + GameState.BonusIndexForLetters * levelDef.PerLetterBonus + GameState.BonusIndexForWords * levelDef.PerWordBonus);
                GameState.BonusIndexForLetters = 0;
                GameState.BonusIndexForWords = 0;
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

            EntropyGame.Instance.SaveGameSettings(true);

            if (newlyUnlockedPlayType != PlayType.None)
            {
                var gameInitializer = CreatePlayInitializer(newlyUnlockedPlayType);
                EntropyGame.LiveStats.TrackEvent("PlayUnlocked", newlyUnlockedPlayType.ToString());
                MessageBox.Show(string.Format("Congratulations!\r\nYou have just unlocked {0}!", gameInitializer.GetTitle()), "New Game Type!", MessageBoxButton.OK);
            }

            return newlyUnlockedPlayType;
        }

        private void NewRandomPlay()
        {
            //if (GameState.PlayCounter < GameState.PlaysInAGame)
            var maxPlayType = Math.Min(GameSettings.UnlockedPlays + 1, (int)PlayType.PlayTypeMax);
            //var playType = (PlayType)RandomGenerator.Instance.Next(1, maxPlayType);
            var playType = PlayType.TimberWords;

            NewPlay(playType);
        }

        private void NewPlay(PlayType playType)
        {
            if (this.GameState.GameOver)
                return;

            // Smoke out the screen
            //for (int i = 0; i < 10; i ++)
            ParticleSystem.CreateBurst(new Vector2(0, 350), new Vector2(800, 480), 10);
            //this.PlayHitBubble(new Vector2(400, 240));
            PlayWoodDoor();

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

            EntropyGame.LiveStats.TrackEvent("NewPlay", playType.ToString());
        }

        private BasePlayLogic CreatePlayLogic(GameState gameState)
        {
            switch (gameState.PlayType)
            { 
                //case PlayType.Test:
                //    return new TestLogic(this);
                case PlayType.TimberWords:
                    return new TimberWordsLogic(this);
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
                case PlayType.TimberWords:
                    return new TimberWordsInitializer(this);
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
                case PlayType.TimberWords:
                    return new TimberWordsView(this, playLogic);
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
            gameState.BonusIndexForLetters = 0;
            gameState.BonusIndexForWords = 0;
            gameState.CurrentLevelId = levelDefinition.LevelId;
            gameState.PlayTime = levelDefinition.PlayTime;
            gameState.MinWords = levelDefinition.MinWords;
            gameState.MinLetters = levelDefinition.MinLetters;
            gameState.Speed = levelDefinition.Speed;
            gameState.PowerCost = levelDefinition.PowerCost;
            gameState.ScoreFactor = levelDefinition.ScoreFactor;

            //var gameInitializer = CreatePlayInitializer(gameState);
            //gameInitializer.Initialize(gameState);

            this.GameState = gameState;

            (PlayScreen as GamePlayScreen).RandomBackground();

            EntropyGame.LiveStats.TrackEvent(EventType.NewLevel, GameState.CurrentLevelId.ToString());
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
            //Debug.WriteLine(">>----------------- Update frame {0:00000} start ----------------- {1}", GameScheduler.TotalGameFrames, GameScheduler.TotalTime);

            base.UpdateGameState();

            lighting = MotionGenerator.EasingAttractor(lighting * 100, GameState.Power*100, 1f, GameScheduler.Elapsed) / 100f;
            SetBgColor(bgColor);

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

            //Debug.WriteLine("<<----------------- Update frame {0:00000} end   ----------------- {1}", GameScheduler.TotalGameFrames, GameScheduler.TotalTime);
        }


        private void CheckRank()
        {
            this.DynamicRankDisplayOld = this.DynamicRankDisplay;
            HiScoreResponse scoreJustPassed = null;
            HiScoreResponse nextScore = null;
            if (this.GameState != null)
            {
                if (GameState.Score < EntropyGame.LiveScores.LocalData.MinScoreToSubmit)
                {
                    // Below min required score
                    DynamicRankDisplay = string.Format("Not Ranked!");
                    DynamicRankNextRankDisplay = string.Format("Required {0}", EntropyGame.LiveScores.LocalData.MinScoreToSubmit);
                    return;
                }

                if (allRankings == null)
                {
                    var hiScores = EntropyGame.GetLastDownloadedHiScores();
                    if (hiScores != null)
                    {
                        allRankings = new List<HiScoreResponse>(hiScores.OrderBy(hs => hs.Score).Where(hs => hs.Score >= EntropyGame.LiveScores.LocalData.MinScoreToSubmit));  // ascending order
                        GetTopScore();

                        if (allRankings.FirstOrDefault(hs => hs.Range == ScoreTableRange.Day) == null)      // No daily ranking submitted yet
                            allRankings.Insert(0, new HiScoreResponse() { Rank = 1, Range = ScoreTableRange.Day, Score = EntropyGame.LiveScores.LocalData.MinScoreToSubmit - 5 }); // Add a dummy daily point to pass
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
                DynamicRankDisplay = string.Format("Below top {0}", EntropyGame.LiveScores.MaxHiScoresToRequest);
            }

            if (nextScore != null)
                DynamicRankNextRankDisplay = string.Format("{1} {0}", nextScore.Score + 5, "Rank up");

            if (DynamicRankDisplay != DynamicRankDisplayOld)
            {
                this.RankDisplayTransition = 0.001f;

                if (DynamicRankDisplayOld != null)
                {
                    if (scoreJustPassed != null && scoreJustPassed.Range == ScoreTableRange.AllTime)
                        PlayRankUpMelody(1);
                    else
                        PlayRankUpMelody(0);
                }
            }
        }

        

        internal void AddScore(int score)
        {
            AddScore(score, true);
        }

        internal void AddScore(int score, bool displayScore)
        {
            if (!GameState.GameOver)
            {
                TriggerStarShine(RandomGenerator.Instance.NextVector2(new Vector2(320, 660), new Vector2(460, 710)));

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
            //return;

            //if (GameState.PlayState == PlayState.Playing)
            if (GameState.Power > 0)
            {
                if (GameState.Power < 0.5f && power < 0)
                    ShowTip("MainGame_RedPower", "Power has been reduced.  Watch the power bar on the left and keep it as high as possible to get higher score.\r\nGame will be over when you lose all power.", 1f, 0.5f, 30f);

                if (GameState.Power < 1f && power > 0)
                    ShowTip("MainGame_IncPower", "Power has been increased.  Watch the power bar on the left and keep it as high as possible to get higher score.\r\nGame will be over when you lose all power.", 1f, 0.5f, 30f);

                GameState.Power = MathHelper.Clamp(GameState.Power + power, 0f, 1f);
                //SetBgColor(bgColor);

                if (!GameState.GameOver)
                    if (GameState.Power == 0)
                    {
                        ShowTip("MainGame_Pow0", "Game is over because power has reached to 0.\r\nYou must keep it high to keep in game and earn higher scores.", 1f, 0.5f);
                        OnGameOver();
                    }
            }
        }

        internal void OnGameOver()
        {
            LocalyticsSession.Instance.tagEvent("GameOver", new Dictionary<string, string>() { { "level", this.GameState.CurrentLevelId.ToString() } });

            if (!GameState.GameOver)
            {
                SetBgColor(Color.White);
                //TimberWordsGame.Instance.Music.Stop();
                if (!EntropyGame.Instance.Music.IsPlaying)
                    PlayMelodySlow(0);
                GameState.GameOver = true;
                GameState.PlayState = PlayState.Over;
                
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
                EntropyGame.LiveStats.TrackEvent(EventType.ResumeLevel, this.GameState.CurrentLevelId.ToString());
            }
        }

        internal Vector2 LocationToPosition(MatrixIndex matrixIndex)
        {
            return TopLeft + new Vector2(matrixIndex.Col * GameState.CellSize.X, matrixIndex.Row * GameState.CellSize.Y);
        }

        internal MatrixIndex PositionToLocation(Vector2 position)
        {
            var pos = position - TopLeft;
            return new MatrixIndex((int)(pos.X / GameState.CellSize.X), (int)(pos.Y / GameState.CellSize.Y));
        }

        internal MatrixIndex PositionToLocationRounded(Vector2 position)
        {
            var pos = position - TopLeft;
            return new MatrixIndex((int)Math.Round(pos.X / GameState.CellSize.X), (int)Math.Round(pos.Y / GameState.CellSize.Y));
        }

        internal IEnumerable<Letter> FillLetterMatrix()
        {
            GameState.LetterMatrixState = "";
            foreach (var lt in GameState.ActiveLetters.Where(l => !l.IsInState<FlyInState>()))
            {
                var loc = PositionToLocationRounded(lt.Position);
                GameState.SetMatrixLetter(loc, lt.Char);
                if (lt.Location != loc)
                {
                    lt.Location = loc;
                    yield return lt;
                }
            }
        }

        [Conditional("DEBUG")]
        internal void DumpLetterMatrix()
        {
            Debug.WriteLine(LetterMatrixToString());
        }
        
        internal string LetterMatrixToString()
        {
            StringBuilder sb = new StringBuilder();

            LetterMatrixToString(sb);

            return sb.ToString();
        }

        internal void LetterMatrixToString(StringBuilder sb)
        {
            sb.AppendLine("Letter Matrix:");
            sb.AppendLine("-------------------------");
            if (GameState.LetterMatrix == null)
                sb.AppendLine("NULL");
            else
            {
                {
                    string srow = "__";
                    for (int col = 0; col < GameState.Cols; col++)
                    {
                        srow += col.ToString() + "_";
                    }
                    sb.AppendLine(srow);
                }
                for (int row = 0; row < GameState.Rows; row++)
                {
                    string srow = row.ToString() + "_";
                    for (int col = 0; col < GameState.Cols; col++)
                    {
                        var ch = GameState.GetMatrixLetter(new MatrixIndex(col, row));
                        srow += (ch == '\0' || ch == ' ' ? '_' : ch) + "_";
                    }
                    sb.AppendLine(srow);
                }
            }
        }

        internal IEnumerable<MatrixIndex> FindEmtpyLocationOnLetterMatrix()
        {
            for (int row = 0; row < GameState.Rows; row++)
            {
                for (int col = 0; col < GameState.Cols; col++)
                {
                    var loc = new MatrixIndex(col, row);
                    var ch = GameState.GetMatrixLetter(loc);
                    if (ch == ' ' || ch == '\0')
                        yield return loc;
                }
            }
        }

        internal MatrixIndex FindRandomEmtpyLocationOnLetterMatrix()
        {
            var locs = new List<MatrixIndex>(FindEmtpyLocationOnLetterMatrix());
            if (locs.Count == 0)
                return MatrixIndex.None;
            else
                return locs[RandomGenerator.Instance.Next(locs.Count)];
        }

        internal void MarkLetter(Letter letter, bool mark)
        {
            if (mark)
                letter.HiLited = true;
            else
                letter.HiLited = false;
        }

        internal void MarkLetters(IEnumerable<Letter> letters, bool mark)
        {
            foreach (var l in letters)
                MarkLetter(l, mark);
        }


        internal Letter GetLetter(MatrixIndex loc)
        {
            return GameState.Letters.FirstOrDefault(l => l.Location == loc);
        }

        internal IEnumerable<Letter> GetLetters(IEnumerable<MatrixIndex> locs)
        {
            return from loc in locs
                   select GetLetter(loc);
        }

        internal IEnumerable<Letter> GetLetters(MatrixRange range)
        {
            return GetLetters(range.GetMatrixIndices());
        }

        internal string GetWordH(int row, int startCol, int endCol)
        {
            string s = null;
            var letterMatrix = GameState.LetterMatrix;
            for (int col = startCol; col <= endCol; col++)
                s += letterMatrix[row, col];
            return s;
        }

        internal string GetWordH(MatrixRange range)
        {
            return GetWordH(range.StartLoc.Row, range.StartLoc.Col, range.EndLoc.Col);
        }

        internal string GetWordV(int col, int startRow, int endRow)
        {
            string s = null;
            var letterMatrix = GameState.LetterMatrix;
            for (int row = startRow; row <= endRow; row++)
                s += letterMatrix[row, col];
            return s;
        }

        internal string GetWordV(MatrixRange range)
        {
            return GetWordV(range.StartLoc.Col, range.StartLoc.Row, range.EndLoc.Row);
        }

        internal string GetWordHV(MatrixRange range)
        {
            if (range.StartLoc.Row == range.EndLoc.Row)
            {
                // Horizontal
                return GetWordH(range.StartLoc.Row, range.StartLoc.Col, range.EndLoc.Col);
            }
            else 
            {
                // Vertical
                return GetWordV(range.StartLoc.Col, range.StartLoc.Row, range.EndLoc.Row);
            }
        }

        /// <summary>
        /// Return all possible horizontal ranges around the given center location
        /// </summary>
        internal IEnumerable<MatrixRange> EnumWordsH(MatrixIndex center)
        {
            // Largest to smallest range around center
            for (int startCol = 0; startCol <= center.Col; startCol++)
                for (int endCol = GameState.Cols - 1; endCol >= center.Col; endCol--)
                    yield return new MatrixRange(new MatrixIndex(startCol, center.Row), new MatrixIndex(endCol, center.Row));
        }

        /// <summary>
        /// Return all possible vertical ranges around the given center location
        /// </summary>
        internal IEnumerable<MatrixRange> EnumWordsV(MatrixIndex center)
        {
            // Largest to smallest range around center
            for (int startRow = 0; startRow <= center.Row; startRow++)
                for (int endRow = GameState.Rows - 1; endRow >= center.Row; endRow--)
                    yield return new MatrixRange(new MatrixIndex(center.Col, startRow), new MatrixIndex(center.Col, endRow));
        }


    }

    

}
