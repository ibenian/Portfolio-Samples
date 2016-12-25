using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Phone.Reactive;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework.Input.Touch;

namespace GameBase.Rx
{
    #region GameLoopScheduler

    internal class Task
    {
        public TimeSpan dueTime;
        public readonly Action action;
        //public object Tag;

        //public Task(TimeSpan dueTime, Action action, Object tag)
        public Task(TimeSpan dueTime, Action action)
        {
            this.dueTime = dueTime;
            this.action = action;
            //this.Tag = tag;
        }

    }

    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    internal class GameLoopScheduler : Microsoft.Xna.Framework.GameComponent, IScheduler 
    {
        private int minTasksPerFrame = 1;
        private TimeSpan maxTaskTimePerFrame = TimeSpan.FromTicks((int)(333333 * 0.5f));
        long totalGameFrames;
        TimeSpan elapsedGameTime;
        TimeSpan totalGameTime;
        float elapsedSeconds;
        float totalSeconds;
        //LinkedList<Task> tasks = new LinkedList<Task>();//(100);
        //LinkedList<Task> toRun = new LinkedList<Task>();//30);

        List<Task> tasks = new List<Task>(100);
        List<Task> toRun = new List<Task>(50);

        public float TimeFactor = 1f;

        //public GameLoopScheduler()
        //{ 
        //}

        public GameLoopScheduler(Game game, bool autoAdd)
            : base(game)
        {
            if (autoAdd)
            {
                game.Components.Add(this);
            }
        }

         //<summary>
         //Allows the game component to perform any initialization it needs to before starting
         //to run.  This is where it can query for any required services and load content.
         //</summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            DateTime startTime = DateTime.Now;      // Measure time it takes to execute scheduled tasks

            totalGameFrames++;
            elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds * TimeFactor;
            elapsedGameTime = TimeSpan.FromSeconds(elapsedSeconds);
            totalGameTime += elapsedGameTime;
            totalSeconds = (float)totalGameTime.TotalSeconds;


            //for (int i = tasks.Count - 1; i >= 0; i--)
            //{
            //    var task = tasks[i];
            //    tasks.RemoveAt(i);
            //    task.action();
            //}
            //return;


            //for (int i = tasks.Count - 1; i >= 0; i--)
            for (int i = 0; i < tasks.Count; i++)       // execute in the scheduled order
            //foreach (var task in tasks)
            {
                if (i < tasks.Count)
                {
                    var task = tasks[i];
                    //var task = tasks[i];
                    if (task.dueTime == TimeSpan.MinValue)
                        toRun.Add(task);
                    else if (task.dueTime.Ticks < 0)
                    {
                        var dueFrame = -task.dueTime.Ticks;
                        if (totalGameFrames >= dueFrame)
                        {
                            //Debug.WriteLine("Running frame scheduled task " + dueFrame.ToString());
                            toRun.Add(task);
                            //tasks.RemoveAt(i);
                            //tasks.Remove(tasks[i]);
                        }
                    }
                    else if (task.dueTime == TimeSpan.Zero || totalGameTime > task.dueTime)
                    {
                        //Debug.WriteLine("Running time scheduled task " + task.dueTime.ToString());
                        toRun.Add(task);
                        //tasks.RemoveAt(i);
                        //tasks.Remove(tasks[i]);
                    }
                }
            }


            {
                int i = 0;
                //for (int i = 0; i < toRun.Count; i++)
                foreach (var task in toRun)
                {
                    if (i >= minTasksPerFrame && DateTime.Now - startTime > maxTaskTimePerFrame)        // Did scheduled tasks take more time time to execute than allowed per frame
                    {
                        //// maxTaskTimePerFrame exceeded
                        Debug.WriteLine("maxTaskTimePerFrame exceeded.  Will run remaining tasks in the upcoming frames");

                        //// reschedule remaining tasks to execute in the next frames
                        //for (int j = i; j < toRun.Count; j++)
                        //{
                        //    tasks.Insert(0, toRun[j]);
                        //}
                        break;
                    }
                    else
                    {
                        // Execute and be done
                        tasks.Remove(task); //toRun[i]);
                        if (task /*toRun[i]*/.dueTime != TimeSpan.MinValue)        // if cancelled, this becomes null and won't schedule again
                            task /*toRun[i]*/.action();  // execute scheduled task
                    }
                    i++;
                }
            }

            // Clear all tasks that are execute/rescheduled
            toRun.Clear();
        }

        public float Elapsed
        {
            get { return this.elapsedSeconds; }
        }

        public float TotalTime
        {
            get { return this.totalSeconds; }
        }

        public DateTimeOffset Now
        {
            get { return new DateTimeOffset() + totalGameTime; }
        }

        public long TotalGameFrames
        {
            get { return this.totalGameFrames; }
        }

        public int TaskCount
        {
            get { return this.tasks.Count; }
        }

        public IDisposable Schedule(Action action, TimeSpan dueTime)
        {
            Task task;
            if (dueTime.Ticks < 0)
            {
                task = new Task(-TimeSpan.FromTicks(this.totalGameFrames - dueTime.Ticks), action); //, new StackTrace());
            }
            else
            {
                task = new Task(dueTime + totalGameTime, action); //, new StackTrace());
            }
            //task.tag = new StackTrace();
            tasks.Add(task);
            //return CreateDisp(task, node);
            return Disposable.Create(() => tasks.Remove(task));          // Slows down almost 100 times!!!
            //return Disposable.Create(() => task.dueTime = TimeSpan.MinValue);          // Slows down almost 100 times!!!
            //return Disposable.Empty;
        }

        //private IDisposable CreateDisp(Task task, LinkedListNode<Task> node)
        //{
        //    return Disposable.Create(() => 
        //    {
        //        if (node.Value.action != null)
        //        {
        //            tasks.Remove(node);
        //            node.Value = new Task();
        //        }
        //    } 
        //    );          // Slows down almost 100 times!!!
        //}

        public IDisposable Schedule(Action action)
        {
            action();
            return Disposable.Empty;
        }

        public override string ToString()
        {
            return string.Format("Tasks: {0}, TotalGameFrames: {1}", this.tasks.Count, this.totalGameFrames);
        }

        public void Reset()
        {
            this.tasks.Clear();
        }

        internal float ElapsedSince(float startTime)
        {
            return this.TotalTime - startTime;
        }
    }

    #endregion
}
