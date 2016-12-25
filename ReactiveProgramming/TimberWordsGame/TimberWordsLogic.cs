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
using System.Diagnostics;
using GameBase.Screen;
using OmegaDot.Helper;

namespace Entropy.GamePlay.PlayLogics
{
    class TimberWordsLogic : BasePlayLogic
    {
        float playStartTime;
        bool powerBarLowShown = false;
        
        
        ObjectPool<Letter> letterPool = new ObjectPool<Letter>(10);
        //List<Pair<Letter, Letter>> LetterBubbleCollisionsListReserve = new List<Pair<Letter, Letter>>(40);

        StateMachine<Letter> stateMachine;
        internal List<RangeWord> matchList = new List<RangeWord>();
        internal List<RangeWord> matchListPrev = new List<RangeWord>();

        public TimberWordsLogic(MainGameLogic mainGameLogic)
            : base(mainGameLogic)
        {
            var letter = new Letter() { Position = new Vector2(-1000, -1000) };
            letterPool.OnClearObject =
                b =>
                {
                    b.Char = letter.Char;
                    b.DrawColor = letter.DrawColor;
                    b.Id = letter.Id;
                    b.Inactive = letter.Inactive;
                    b.MachineState = letter.MachineState;
                    b.Position = letter.Position;
                    b.Rotation = letter.Rotation;
                    b.Scale = letter.Scale;
                    b.Speed = letter.Speed;
                    b.StateEnterTime = letter.StateEnterTime;
                    b.Texture = letter.Texture;
                    b.TextureHi = letter.TextureHi;
                    b.Location = letter.Location;
                };

        }

        protected override void OnGameStateSet()
        {
            base.OnGameStateSet();

            this.stateMachine = new StateMachine<Letter>(GameScheduler);

            if (this.GameState != null)
            {

                playStartTime = GameScheduler.TotalTime;

                TimberWordsState bpState = GameState.GamePlayState as TimberWordsState;

                GameState.PrepareForScreen(PlayScreen);

                //TimberWordsGame.Instance.GameSettings.TipsSeen.Clear();

                
                this.Delay(0.3f, () =>
                {
                    MainGameLogic.PlayDoorAntique();

                    ShowTip("TW_Welcome", "Welcome to Timber Words!\r\nTips will be shown to quickly introduce the game.", () =>
                        {

                            this.PlayScreen.ScreenManager.AddScreen(new DialogScreen(
                                    string.Format("Level {0}\r\n\r\nMake groups of at least {1} word(s)\r\nof {2} letters in {3}.\r\nScore Factor = x{4}", this.GameState.CurrentLevelId, this.GameState.MinWords, this.GameState.MinLetters, StringHelper.FormatSeconds(this.GameState.PlayTime), GameState.ScoreFactor),
                                () =>
                                {
                                    MainGameLogic.PlayWoodDoor();
                                    InitAsyncLogic();

                                }
                                ));
                        });
                });

                
            }
        }

        private void InitAsyncLogic()
        {
            bool inputDisabled = false;
            float playInitTime = GameScheduler.TotalTime;
            TimberWordsState bpState = GameState.GamePlayState as TimberWordsState;

            MainGameLogic.FillLetterMatrix().ToList();
            Delay(0.5f, () => AddNewLetters());

            ShowTip("TW_Init", "Swipe on the letter board to move columns and rows of letters and make words.", 0f);

            MainGameLogic.PlayLevelStart();

            // Letter Initial State
            stateMachine.RegisterState<InitialState>(
                (lt, state) =>
                {
                    lt.Position = MainGameLogic.LocationToPosition(lt.Location);
                    stateMachine.Transition<LetterIdleState>(lt);
                }
                );

            stateMachine.RegisterState<LetterIdleState>(
                (mb, state) =>
                {
                    
                }
                );

            stateMachine.RegisterState<SlideInState>(
               (lt, state) =>
               {
                   MotionGenerator.EasingAttractor(new PosSpeed(lt.Position, Vector2.Zero), ps => new PosSpeed(state.TargetPosition, Vector2.Zero), 8f, GameScheduler, false).TakeWhile(ps => lt.IsInState<SlideInState>() && !Vector2Helper.EqualsWithTolerence(ps.Pos, state.TargetPosition, 1f/2))
                           .Subscribe(ps =>
                           {
                               lt.Position = ps.Pos;
                           },
                           () =>
                           {
                               if (lt.IsInState<SlideInState>())
                               {
                                   lt.Position = MainGameLogic.LocationToPosition(lt.Location);
                                   stateMachine.Transition<LetterIdleState>(lt);
                                   //if (targetBubble.IsFriendly && !targetBubble.IsInState<RemovedState>())
                                   //{
                                   //    MainGameLogic.AddScore(10);
                                   //    GameState.BonusIndex++;
                                   //}

                                   //InflateBubble(targetBubble);
                                   //removeBubble.OnNext(b);

                                   
                               }
                           });
               });

            stateMachine.RegisterState<FlyOutState>(
               (lt, state) =>
               {
                   lt.Inactive = true;
                   //MainGameLogic.ParticleSystem.CreateFlash(p => b.Position, 1, 1f);
                   MotionGenerator.EasingAttractor(new PosSpeed(lt.Position, Vector2.Zero), ps => new PosSpeed(state.TargetPosition, Vector2.Zero), 8f, GameScheduler, false).TakeWhile(ps => lt.IsInState<FlyOutState>() && !Vector2Helper.EqualsWithTolerence(ps.Pos, state.TargetPosition, 1f/2))
                           .Subscribe(ps =>
                           {
                               lt.Position = ps.Pos;
                           },
                           () =>
                           {
                               if (lt.IsInState<FlyOutState>())
                               {
                                   lt.Position = MainGameLogic.LocationToPosition(lt.Location);
                                   //stateMachine.Transition<LetterIdleState>(lt);
                                   removeLetter.OnNext(lt);
                                   //if (targetBubble.IsFriendly && !targetBubble.IsInState<RemovedState>())
                                   //{
                                   //    MainGameLogic.AddScore(10);
                                   //    GameState.BonusIndex++;
                                   //}

                                   //InflateBubble(targetBubble);
                                   //removeBubble.OnNext(b);
                               }
                           });
               });

            stateMachine.RegisterState<FlyInState>(
               (lt, state) =>
               {
                   lt.Position = state.InitialPosition;
                   inputDisabled = true;
                   Delay(state.Delay, () =>
                       {
                           //lt.Inactive = true;
                           
                           
                           //MainGameLogic.ParticleSystem.CreateFlash(p => b.Position, 1, 1f);
                           MotionGenerator.EasingAttractor(new PosSpeed(lt.Position, Vector2.Zero), ps => new PosSpeed(state.TargetPosition, Vector2.Zero), 8f, GameScheduler, false).TakeWhile(ps => lt.IsInState<FlyInState>() && !Vector2Helper.EqualsWithTolerence(ps.Pos, state.TargetPosition, 1f / 2))
                                   .Subscribe(ps =>
                                   {
                                       lt.Position = ps.Pos;
                                   },
                                   () =>
                                   {
                                       if (lt.IsInState<FlyInState>())
                                       {
                                           //lt.Inactive = false;
                                           //lt.Location = MainGameLogic.PositionToLocation(lt.Position);
                                           //lt.Position = MainGameLogic.LocationToPosition(lt.Location);
                                           stateMachine.Transition<LetterIdleState>(lt);
                                           MainGameLogic.FillLetterMatrix().ToList();
                                           //stateMachine.Transition<LetterIdleState>(lt);
                                           //removeLetter.OnNext(lt);
                                           //if (targetBubble.IsFriendly && !targetBubble.IsInState<RemovedState>())
                                           //{
                                           //    MainGameLogic.AddScore(10);
                                           //    GameState.BonusIndex++;
                                           //}

                                           //InflateBubble(targetBubble);
                                           //removeBubble.OnNext(b);

                                           MainGameLogic.PlayWoodHitC(lt.Position);
                                       }

                                       Delay(0.1f, () => inputDisabled = false);
                                   });

                       });
               });

            // controls
            Vector2 touchPos = Vector2.Zero;
            Vector2 touchDownPos = Vector2.Zero;
            Vector2 touchDownBatPos = Vector2.Zero;
            MoveDirection moveDirection = MoveDirection.None; 

            var touchDown = from t in this.MainGameLogic.TouchStream.TouchDown select t;
            var touchUp = from t in this.MainGameLogic.TouchStream.TouchUp select t;
            var touchMove = from t in this.MainGameLogic.TouchStream.TouchMove select t;

            touchDown.InRect(new Rectangle(0, 660, 170, 800)).TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(t =>
                    {
                        
                        TakeMatches();
                    });

            touchDown.TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(t =>
                {
                    if (!inputDisabled)
                    {
                        //if (t.Position.Y > MainGameLogic.BottomRight.Y)
                        //    RemoveMatches();
                        //else
                        {
                            touchDownPos = touchPos = t.Position;
                            //touchDownBatPos = bpState.BatPosition;

                            //if (t.Position.Y < 70)
                            //{
                            //    if (t.Position.X < 200)
                            //        MainGameLogic.SetPrevBackground();
                            //    else if (t.Position.X > 800 - 200)
                            //        MainGameLogic.SetNextBackground();
                            //}
                        }
                    }
                });

            touchUp.TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(t =>
                {
                    if (!inputDisabled)
                    {
                        ShowTip("TW_FirstMove2", "Longer words give more points and more bonus.", 1f, 0f, new string[] { "TW_FirstMove" });
                        ShowTip("TW_FirstMove", "If your move matches any word of minimun required length, it will be marked.");

                        GameState.Letters.ForEach(lt => lt.DrawColor = Color.White);
                        //if (t.Position.Y > MainGameLogic.BottomRight.Y)
                        //    ; //RemoveMatches();
                        //else
                        {
                            MainGameLogic.PlayWoodHitB(t.Position);
                            MainGameLogic.MarkLetters(GameState.Letters, false);
                            touchPos = Vector2.Zero;

                            //var lmat0 = MainGameLogic.LetterMatrixToString();
                            var movedLetters = MainGameLogic.FillLetterMatrix().ToList();
                            //var lmat1 = MainGameLogic.LetterMatrixToString();
                            //if (lmat0 != lmat1)
                            {
                                GameState.ActiveLetters.ForEach(lt =>
                                    {
                                        //if (lt.IsInState<SlideInState>())
                                        //    (lt.MachineState as SlideInState).TargetPosition = MainGameLogic.LocationToPosition(lt.Location);
                                        //else
                                        stateMachine.Transition<SlideInState>(lt, new SlideInState() { TargetPosition = MainGameLogic.LocationToPosition(lt.Location) });

                                    });      // Slide in all the letters
                                //MainGameLogic.DumpLetterMatrix();
                                //MainGameLogic.SetDebugMsg(MainGameLogic.LetterMatrixToString());

                                CheckLetterMatrix(movedLetters);
                            }

                            moveDirection = MoveDirection.None;

                            touchDownPos = Vector2.Zero;
                        }
                    }
                });

            touchMove.TakeWhile(b => GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(t =>
                {
                    if (inputDisabled)
                    {
                        touchPos = Vector2.Zero;
                        moveDirection = MoveDirection.None;
                        return;
                    }

                    if (touchDownPos == Vector2.Zero)
                        return;
                    //if (t.Position.Y > MainGameLogic.BottomRight.Y)
                    //    return;
                    
                    touchPos = t.Position;

                    MatrixIndex touchLocation = MainGameLogic.PositionToLocation(touchPos);

                    if (moveDirection == MoveDirection.None)
                    {
                        // determine direction
                        
                        var deltaPos = touchPos - touchDownPos;

                        if (deltaPos.X != 0 || deltaPos.Y != 0)
                        {
                            if (Math.Abs(deltaPos.Y) > Math.Abs(deltaPos.X))
                                moveDirection = MoveDirection.Vertical;
                            else
                                moveDirection = MoveDirection.Horizontal;
                        }
                    }

                    if (moveDirection != MoveDirection.None)
                    {
                        if (GameState.ActiveLetters.Where(l => l.IsInState<SlideInState>()).Count() == 0)
                        {

                            if (moveDirection == MoveDirection.Vertical)
                            {
                                // Vertical slide
                                GameState.ActiveLetters.Where(l => l.Location.Col == touchLocation.Col).ForEach(l =>
                                {
                                    if (touchPos != touchDownPos)
                                    {
                                        l.Position.Y = (MainGameLogic.LocationToPosition(l.Location) + touchPos - touchDownPos).Y;
                                        LimitPositionOnBoard(l);
                                    }
                                }
                                );
                            }
                            else
                            {
                                // Horizontal slide
                                GameState.ActiveLetters.Where(l => l.Location.Row == touchLocation.Row).ForEach(l =>
                                {
                                    if (touchPos != touchDownPos)
                                    {
                                        l.Position.X = (MainGameLogic.LocationToPosition(l.Location) + touchPos - touchDownPos).X;
                                        LimitPositionOnBoard(l);
                                    }
                                }
                                );
                            }
                        }
                    }
                });

            // Game end timer
            Observable.Interval(TimeSpan.FromSeconds(1f), GameScheduler).TakeWhile(l => GameState.CountDownTimer > 0 && GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(l =>
                {
                    GameState.CountDownTimer--;
                    GameState.CountDownDisplay = StringHelper.FormatSeconds(GameState.CountDownTimer);

                    if (matchList != null && matchList.Count > 0)
                    {
                        GameState.Letters.ForEach(lt => lt.DrawColor = Color.White);
                        int windex = GameState.CountDownTimer % matchList.Count;
                        (from ml in matchList
                            from lt in MainGameLogic.GetLetters(ml.Range)
                            select lt
                            ).ForEach(lt => lt.DrawColor = Color.White.SetAlpha(0.5f));
                        MainGameLogic.GetLetters(matchList[windex].Range).ForEach(lt => lt.DrawColor = Color.Orange);

                        //Observable.Interval(TimeSpan.FromSeconds(1f/10f), GameScheduler).TakeWhile(l2 => l2 < 10 && GameState.CountDownTimer > 0 && GameState.PlayState == PlayState.Playing).TakeUntil(MainGameLogic.PlayComplete)
                        //    .Subscribe(l2 =>
                        //        {
                        //            if (matchList.Count > 0)
                        //            {
                        //                int wi = GameState.CountDownTimer % matchList.Count;
                        //                MainGameLogic.GetLetters(matchList[wi].Range).ForEach(lt => MainGameLogic.TriggerBeamEffect(lt.Position + GameState.CellSize / 2));
                        //            }
                        //        }
                        //        );
                    }
                    else
                        GameState.Letters.ForEach(lt => lt.DrawColor = Color.White);


                    if (GameState.CountDownTimer % 10 == 0 && GameState.CountDownTimer != 0)
                    {
                        //MainGameLogic.AddPower(-0.1f / 2 * GameState.Speed);
                        if ((GameState.CountDownTimer % 60) == 0 && GameState.CountDownTimer != 0)
                        {
                            MainGameLogic.PlayCuckooMidTime();
                        }
                    }
                    if (GameState.CountDownTimer >= 60 && (GameState.CountDownTimer % 60) == 1)
                    {
                        ShowHelp(null, 2f, string.Format("{0} left", StringHelper.FormatSeconds(GameState.CountDownTimer)));
                    }
                    //if (GameState.CountDownTimer % 20 == 0)
                    //    (Game as BubblePongGame).Music.PlayNext();

                    if (GameState.CountDownTimer == 11)
                    {
                        ShowTip("TW_CountDownStart", "Last 10 seconds to level completion.", 1f);
                        MainGameLogic.PlayCuckooStart();
                    }
                    else if (GameState.CountDownTimer < 11 && GameState.CountDownTimer > 0)
                        MainGameLogic.PlayCuckoo();



                },
                () =>
                {
                    // When timer is complete, play is over
                    if (GameState.PlayState == PlayState.Playing && GameState.CountDownTimer == 0)
                    {
                        if (GameState.AtLeastOneTakePerformed)
                        {
                            // User kept playing until countdown is complete.
                            // Success
                            GameState.PlayState = PlayState.Over;
                            GameState.PlaySucceded = true;
                            playComplete.OnNext(this);
                            playComplete.OnCompleted();
                            MainGameLogic.PlayLevelSucceed();

                            if (GameState.Score > 5000000)
                                ShowTip("TW_Success6", ":O Whhhhhhhooooowwww!", 1f, 0.1f, new string[] { "TW_Success5" });
                            if (GameState.Score > 1000000)
                                ShowTip("TW_Success5", "OMG!\r\nYou're the best!\r\nGo for it :)", 1f, 0.1f, new string[] { "TW_Success4" });
                            if (GameState.Score > 500000)
                                ShowTip("TW_Success4", "You reached your first 500000 score!\r\nYou rock!", 1f, 0.1f, new string[] { "TW_Success3" });
                            if (GameState.Score > 50000)
                                ShowTip("TW_Success3", "Awesome!\r\nKeep trying different strategies.", 1f, 0.1f, new string[] { "TW_Success2" });
                            ShowTip("TW_Success2", "You're learning quickly!\r\nKeep up the good work.", 1f, 0.1f, new string[] { "TW_Success" });
                            ShowTip("TW_Success", "Welldone!  You have completed this level.\r\nAll words and letters you have taken will pay extra bonus when you complete a level.", 1f, 0.1f);
                            //MainGameLogic.AddBonus(GameState.Bubbles.Count(b => b.IsInState<BounceState>()) * 500);
                        }
                        else
                        {
                            // No takes performed.  Game over
                            ShowHelp(null, 10f, "Puzzle not solved!\r\nYou must collect at least one group.");
                            Delay(1f, () =>
                                {
                                    GameState.PlayState = PlayState.Over;
                                    GameState.PlaySucceded = false;
                                    MainGameLogic.OnGameOver();
                                });
                        }
                    }
                });

            // Resume from current state
            GameState.ActiveLetters.ForEach(lt =>
            {
                stateMachine.Retransition(lt);       // resume bubbles
            });


            // Remove bubbles
            removeLetter.ObserveOn(GameScheduler) //.TakeUntil(MainGameLogic.PlayComplete)
                .Subscribe(b => 
                    {
                        GameState.Letters.Remove(b);
                        letterPool.Recycle(b);
                        b.MachineState = RemovedState.Instance;
                        //if (bpState.Letters.Count(bb => bb.IsFriendly) == 0)
                        //{
                        //    GameState.PlayState = PlayState.Over;
                        //    GameState.PlaySucceded = false;
                        //    //GameState.GameOver = true;


                        //    playComplete.OnNext(this);
                        //    playComplete.OnCompleted();
                        //}
                    });

            // Complete game when done
            playComplete.ObserveOn(GameScheduler)
                .Subscribe(bp => MainGameLogic.PlayComplete.OnNext(bp),
                () => 
                {
                    Debug.WriteLine("Play complete");
                }
                );

            
        }

        private void AddNewLetters()
        {
            
            if (GameState.ActiveLetters.Count() < GameState.MinLettersOnBoard)
            {
                MainGameLogic.FillLetterMatrix().ToList();
                List<Letter> newLetters = new List<Letter>();

                StringBuilder sb = new StringBuilder();
                foreach (var newWord in MainGameLogic.wordRepository.GenerateRandomWords(GameState.MinLetters, GameState.MaxLetters))
                {
                    if (GameState.ActiveLetters.Count() >= GameState.MinLettersOnBoard)
                        break;

                    // Add letters on the board
                    sb.AppendLine(newWord);
                    AddNewLetters(newLetters, ScrambleWord(newWord));
                }
                
                //MainGameLogic.SetDebugMsg(sb.ToString());

                int i = 0;
                newLetters.ForEach(lt => stateMachine.Transition<FlyInState>(lt, new FlyInState() { Delay = (i++) * 0f, InitialPosition = new Vector2(480, 0), TargetPosition = MainGameLogic.LocationToPosition(lt.Location) }));
                MainGameLogic.FillLetterMatrix().ToList();
            }
        }

        private void AddNewLetters(List<Letter> newLetters, string chars)
        {
            
            Debug.WriteLine("Adding letters for " + chars);
            var charList = (from lt in GameState.ActiveLetters select lt.Char).ToList();
            foreach (var ch in chars)
            {
                var foundIndex = charList.IndexOf(ch);
                if (foundIndex >= 0)
                {
                    // Already in the board
                    Debug.WriteLine(string.Format("Letter '{0}' already on board", ch));
                    charList.RemoveAt(foundIndex);
                }
                else
                {
                    // Not on board, add new
                    Debug.WriteLine(string.Format("Adding new letter '{0}' on board", ch));
                    var letter = AddNewLetter(ch);
                    if (letter != null)
                        newLetters.Add(letter);
                }
                
            }

            //MainGameLogic.FillLetterMatrix().ToList();
            
        }

        private Letter AddNewLetter(char ch)
        {
            var loc = MainGameLogic.FindRandomEmtpyLocationOnLetterMatrix();
            if (!loc.IsNone)
            {
                Letter lt = new Letter() { Location = loc, Char = ch };
                stateMachine.Transition<InitialState>(lt);
                lt.PrepareForScreen(PlayScreen);
                GameState.Letters.Add(lt);
                GameState.SetMatrixLetter(loc, ch);

                return lt;
                //stateMachine.Transition<FlyInState>(lt, new FlyInState() { TargetPosition = MainGameLogic.LocationToPosition(lt.Location) });
            }
            else
                return null;
        }

        private string ScrambleWord(string word)
        {
            var charList = word.ToCharArray().ToList();
            var randomOrdered = EnumerableExtensions.RandomizeOrder<char>(charList).ToArray();
            return new string(randomOrdered);
        }

        //private void EnsureSufficientLetters(int minValidWords)
        //{
        //    var allPossibleMatches = FindPossibleMatches();
        //    string msg = string.Join("\r\n", allPossibleMatches.ToArray());
        //    //MainGameLogic.SetDebugMsg(msg);

        //    //if (allPossibleMatches.Count < minValidWords)
        //    //{
        //    //    //allPossibleMatches.ForEach(rw => Debug.WriteLine(rw));
        //    //}
        //}

        //private IEnumerable<string> FindPossibleMatches()
        //{
        //    var charList = (from lt in GameState.ActiveLetters select lt.Char).ToArray();
        //    Debug.WriteLine("Combinations:");
        //    var combinations = EnumerableExtensions.GenerateCombinations(charList, MinLetters, MaxLetters).ToList();
        //    Debug.WriteLine("Combinations found:" + combinations.Count);
        //    MainGameLogic.SetDebugMsg("Combinations found: " + combinations.Count);

        //    var allLetters = GameState.ActiveLetters.ToList();
        //    var letterCombinations = FindWordPermutations(allLetters, 3, 5);
        //    return letterCombinations.ToList();
        //}

        //private IEnumerable<string> FindWordPermutations(List<Letter> allLetters, int minChars, int maxChars)
        //{
        //    yield break;
        //}

        private void CheckLetterMatrix(List<Letter> movedLetters)
        {
            GameState.MatchFound = false;
            var matchedWords = FindMatchedWords(GetMatchedLetters().Union(movedLetters).ToList());
            matchListPrev = matchList;
            
            matchList = matchedWords.ToList();
            if (matchList.Count >= GameState.MinWords)
            {
                Debug.WriteLine("Words found:");
                matchList.ForEach(rw => Debug.WriteLine(rw));

                // Mark letters that matched
                int i = 0;
                foreach (var rw in matchList)
                {
                    var letters = MainGameLogic.GetLetters(rw.Range).ToList();
                    MainGameLogic.MarkLetters(letters, true);
                    if (!matchListPrev.Contains<RangeWord>(rw))
                    {
                        MainGameLogic.PlayMagicalTwinkle(letters.FirstOrDefault().Position);

                        letters.ForEach(lt =>
                                MainGameLogic.TriggerStarShineEffect(RandomGenerator.Instance.NextVector2(MainGameLogic.LocationToPosition(lt.Location), MainGameLogic.LocationToPosition(lt.Location.SouthEast(1))))
                            );
                    }
                    
                    i++;
                }


                if (matchListPrev.Count < GameState.MinWords)
                    ShowHelp(null, 6f, "Matched word(s) found!\r\nTake now or make more words...", null, 1f, 0f, new string[] { "TW_MatchFound2" });
                GameState.MatchFound = true;
                ShowTip("TW_MatchFound2", "In order to get more score, try to make as many words as possible before taking them.", 1f, 0.5f, new string[] { "TW_MatchFound" });
                ShowTip("TW_MatchFound", "Matched word(s) found! You can make more words, or tap 'Take' button to take the matched words.", 1f, 0.5f, new string[] { "TW_FirstMove2" });
            }
            else
            {
                if (matchList.Count > 0)
                    ShowTip("TW_MatchFoundMore_" + matchList.Count.ToString(), string.Format("You found {0} matches.  {1} more required to take words.", matchList.Count, GameState.MinWords - matchList.Count), 1f, 0.5f, new string[] { "TW_MatchFound2" });
                // No matches
                MainGameLogic.AddPower(GameState.PowerCost);

                int lastMoves = (int)(GameState.Power/-GameState.PowerCost) + 1;
                if (GameState.Power <= 0)
                {
                    ShowHelp(null, 10f, "Puzzle not solved!\r\nYou must collect at least one group.");
                }
                else if (lastMoves <= 10)
                {
                    ShowHelp(null, 10f, string.Format("Power bar low!\r\nLast {0} moves!", lastMoves));
                    MainGameLogic.PlayCuckooWarn();
                }
                else if (!powerBarLowShown && GameState.Power < 0.25f)
                {
                    MainGameLogic.PlayCuckooWarn();
                    ShowTip(null, string.Format("Power bar low!\r\nYou are playing the last 25% of your allowed moves."));
                    powerBarLowShown = true;
                }
            }
        }

        private IEnumerable<Letter> GetMatchedLetters()
        {
            if (matchList != null)
            {
                foreach (var rw in matchList)
                {
                    foreach (var letter in MainGameLogic.GetLetters(rw.Range).Where(l => l != null))
                        yield return letter;
                }
            }
        }

        private void TakeMatches()
        {
            if (matchList != null && matchList.Count >= GameState.MinWords)
            {
                GameState.AtLeastOneTakePerformed = true;

                ShowTip("TW_TakeMatches", "Taking the matched words will convert the words into score and bonus to be applied at the level completion.", 1f, 0.5f);
                ShowTip("TW_TakeMatches2", "The more words are taken in one shot the more points.", 1f, 0.5f, 30f);

                Debug.WriteLine("Words found:");
                matchList.ForEach(rw => Debug.WriteLine(rw));

                // Mark letters that matched
                int i = 0;
                foreach (var rw in matchList)
                {
                    var letters = MainGameLogic.GetLetters(rw.Range).Where(l => l != null).ToList();
                    if (letters.Count >= GameState.MinLetters)
                    {
                        MainGameLogic.MarkLetters(letters, true);
                        MainGameLogic.PlayMagicalTwinkle(letters.FirstOrDefault().Position);
                    }

                    

                    this.Delay((float)(i) * 0.5f,
                        () =>
                        {
                            bool isFullRowOrCol = IsFullRow(rw) || IsFullCol(rw);
                            if (isFullRowOrCol)
                            {
                                ShowTip("TW_FullRowOrCol", "Weldone!  A full row or full column doubles the score and bonus you get.", 1f, 0.5f);

                                MainGameLogic.PlayZap(MainGameLogic.LocationToPosition(rw.Range.StartLoc));
                            }
                            else
                                MainGameLogic.PlayWoodOpenSqueak(); //MainGameLogic.LocationToPosition(rw.Range.StartLoc));
                            letters.ForEach(lt =>
                                    stateMachine.Transition<FlyOutState>(lt, new FlyOutState() { TargetPosition = new Vector2(50, 730) }));

                            letters.ForEach(lt =>
                            {
                                for (int j = 0; j < RandomGenerator.Instance.Next(5); j++)
                                    if (isFullRowOrCol)
                                        MainGameLogic.TriggerBasicExplosionEffect(RandomGenerator.Instance.NextContourVector(lt.Position, lt.Position + GameState.CellSize));
                                    else
                                        MainGameLogic.TriggerBasicSmokePlumeEffect(RandomGenerator.Instance.NextContourVector(lt.Position, lt.Position + GameState.CellSize));
                                    
                                //MainGameLogic.TriggerBasicSmokePlumeEffect(lt.Position + new Vector2(0, GameState.CellSize.Y));
                            });

                            AddWordFound(rw.Word);

                            // Add score for the word
                            int scoreFactor = (isFullRowOrCol ? 2 : 1) * (int)GameState.ScoreFactor;
                            int baseScore = 10 * rw.Word.Length;
                            int score = baseScore * scoreFactor;
                            MainGameLogic.AddScore(score);
                            GameState.BonusIndexForLetters += rw.Word.Length;
                            GameState.BonusIndexForWords += 1;
                            MainGameLogic.AddPower(0.1f * rw.Word.Length);

                            if (scoreFactor == 1)
                                ShowHelp(null, 2f, string.Format("+{0} for {1}", baseScore, rw.Word));
                            else
                                ShowHelp(null, 4f, string.Format("+{3} for {2}\r\n+{0} x {1}", baseScore, scoreFactor, rw.Word, score));
                        });
                    i++;

                }

                matchList.Clear();

                if (i > 0)
                {
                    this.Delay((float)(i) * 0.5f,
                        () =>
                        {
                            AddNewLetters();
                        });
                }
            }

            GameState.MatchFound = false;
        }

        private bool IsFullCol(RangeWord rw)
        {
            if (rw.Range.IsVertical)
                if (rw.Range.StartLoc != rw.Range.EndLoc)
                    if (rw.Word.Length == GameState.Rows)
                        return true;

            return false;
        }

        private bool IsFullRow(RangeWord rw)
        {
            if (rw.Range.IsHorizontal)
                if (rw.Range.StartLoc != rw.Range.EndLoc)
                    if (rw.Word.Length == GameState.Cols)
                        return true;

            return false;
        }

        private void AddWordFound(string word)
        {
            GameState.FoundWords = word + "\r\n" + GameState.FoundWords;
            if (GameState.FoundWords.Length > 25)
                GameState.FoundWords = GameState.FoundWords.Substring(0, 25);
        }


        private IEnumerable<RangeWord> FindMatchedWords(List<Letter> movedLetters)
        {
            var allWordCombinations =
                    from letter in movedLetters
                    from rangeWord in FindWordCombinations(letter)
                    select rangeWord;
        
            var distinctWordCombinations = allWordCombinations.Distinct(new RangeWordComparer()).Where(rw => rw.Word.Length >= GameState.MinLetters);
            //distinctWordCombinations.ForEach(rw => Debug.WriteLine(rw));

            var wordRepository = MainGameLogic.wordRepository;

            var qmatchedWords =
                from distinctWord in distinctWordCombinations
                where wordRepository.IsMatch(distinctWord.Word)
                select distinctWord;

            var matchedWords = qmatchedWords.ToList();
            if (matchedWords.FirstOrDefault(rw => rw.Word.Length >= GameState.MinLetters) != null)
                return matchedWords;
            else
                return Enumerable.Empty<RangeWord>();
        }

        private IEnumerable<RangeWord> FindWordCombinations(Letter letter)
        {
            var hwords = from range in MainGameLogic.EnumWordsH(letter.Location) select new RangeWord() { Range = range, Word = MainGameLogic.GetWordH(range) };
            var vwords = from range in MainGameLogic.EnumWordsV(letter.Location) select new RangeWord() { Range = range, Word = MainGameLogic.GetWordV(range) };

            return hwords.Union(vwords);
        }

        private void LimitPositionOnBoard(Letter l)
        {
            var rpos = l.Position - MainGameLogic.TopLeft + GameState.CellSize / 2;
            rpos.Y = (rpos.Y + MainGameLogic.BoardSize.Y) % MainGameLogic.BoardSize.Y;
            rpos.X = (rpos.X + MainGameLogic.BoardSize.X) % MainGameLogic.BoardSize.X;
            l.Position = rpos + MainGameLogic.TopLeft - GameState.CellSize / 2;
        }

        private void InflateBubble(Bubble b)
        {
            b.Scale.Y = b.Scale.X = MathHelper.Clamp(b.Scale.X + 1f / 25f, 0.9f, 1.5f);
        }

        private bool Collides(Rainbow r, Bubble b)
        {
            var pos = r.Position - r.TextureCenter;
            return new Rectangle((int)pos.X, (int)pos.Y, (int)r.Texture.Width, (int)r.Texture.Height).Contains((int)b.Position.X, (int)b.Position.Y);
        }
    }

    class RangeWord : IEquatable<RangeWord>
    {
        public string Word;
        public MatrixRange Range;

        public override string ToString()
        {
            return Range + " \"" + Word + "\"";
        }

        #region IEquatable<RangeWord> Members

        public bool Equals(RangeWord other)
        {
            return this.Word == other.Word && this.Range == other.Range;
        }
        #endregion
    }

    class RangeWordComparer : EqualityComparer<RangeWord>
    {
        public override bool Equals(RangeWord x, RangeWord y)
        {
            return x.Word == y.Word;
        }

        public override int GetHashCode(RangeWord obj)
        {
            return obj.Word.GetHashCode();
        }
    }

    class RangeWordComparerFull : EqualityComparer<RangeWord>
    {
        public override bool Equals(RangeWord x, RangeWord y)
        {
            return x.Word == y.Word && x.Range == y.Range;
        }

        public override int GetHashCode(RangeWord obj)
        {
            return obj.Word.GetHashCode() ^ obj.Range.GetHashCode();
        }
    }


    class TimberWordsInitializer : BasePlayInitializer
    {
        public TimberWordsInitializer(MainGameLogic mainGameLogic)
            : base(mainGameLogic)
        {
        }

        internal override string GetTitle()
        {
            return "";
        }

        internal override void Initialize(GameState gameState)
        {
            TimberWordsState twState = new TimberWordsState();
            gameState.GamePlayState = twState;
            gameState.Letters.Clear();

            gameState.CountDownTimer = gameState.PlayTime;

            Vector2 topLeft = MainGameLogic.TopLeft;
            Vector2 bottomRight = MainGameLogic.BottomRight;
            bottomRight.X = 800 - 200;


            //for (int i = 0; i < 30; i++)
            //{
            //    gameState.Letters.Add(new Letter() { Location = new MatrixIndex(i%5, i/5), MachineState = InitialState.Instance, Char = (char)('A' + (i % 26)) });
            //}

            gameState.PlayState = PlayState.Playing;
        }
    }

    internal enum MoveDirection
    { 
        None = 0,
        Horizontal = 1,
        Vertical = 2
    }

    [XmlRoot(ElementName = "timberWordsState", Namespace = "")]
    public class TimberWordsState : BasePlayState
    {
        

        
        internal override void PrepareForScreen(global::GameBase.Screen.PlayScreen playScreen)
        {
            base.PrepareForScreen(playScreen);
            
        }
    }

    public class SlideInState : MachineState
    {
        public Vector2 TargetPosition;
    }

    public class FlyOutState : MachineState
    {
        public Vector2 TargetPosition;
    }

    public class FlyInState : MachineState
    {
        public Vector2 InitialPosition;
        public Vector2 TargetPosition;
        public float Delay;
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

    public class GemBeatState : MachineState
    {
    }

    public class GemRainState : MachineState
    {
        public Vector2 InitialSpeed;
        public Vector2 TargetPosition;
    }

    public class GemFlyState : MachineState
    {
        public Vector2 TargetPosition;
    }
    

    public class GemDisappearState : MachineState
    {
    }

    public class GemPickedState : MachineState
    {
    }

    public class RainbowUnfoldState : MachineState
    {
        public Vector2 InitialPos;
    }

    public class RainbowSuspendState : MachineState
    {
        [XmlAttribute]
        public float Duration = 4f;
    }

    public class RainbowDisappearState : MachineState
    {

    }

    public class LetterIdleState : MachineState
    { 
    }

    public class AppearState : MachineState
    {
        [XmlAttribute]
        public float FinalSize = 1f;
    }

    public class DisappearState : MachineState
    { 
    }

    public class SpotlightShowState : MachineState
    {
        [XmlAttribute]
        public float TimeOffet = 0f;
        public float AngleOffset = 0f;
    }

    public class DiscoLightShowState : MachineState
    {
    }
}
