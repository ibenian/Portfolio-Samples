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
    #region Extensions

    internal static class ObservableRx
    {
        public static IObservable<TResult> GeneratePerFrame<TState, TResult>(TState initialState, Func<TState, TResult> resultSelector, Func<TState, TState> iterate, IScheduler scheduler)
        {
            return Observable.CreateWithDisposable<TResult>(delegate(IObserver<TResult> observer)
            {
                TState state = initialState;
                //bool first = true;
                //bool hasResult = false;
                TResult result = default(TResult);
                return scheduler.Schedule(delegate(Action<TimeSpan> self)
                {
                    //if (hasResult)
                    //if (!first)
                    //{
                    //    observer.OnNext(result);
                    //}
                    try
                    {
                        //if (first)
                        //{
                        //    first = false;
                        //}
                        //else
                        {
                            result = resultSelector(state);
                            observer.OnNext(result);
                            state = iterate(state);
                        }
                        //hasResult = condition == null || condition(state);
                        //if (hasResult)
                        {
                            
                        }
                    }
                    catch (Exception exception)
                    {
                        observer.OnError(exception);
                        return;
                    }
                    //if (hasResult)
                    {
                        self(-TimeSpan.FromTicks(1));
                    }
                    //else
                    //{
                    //    observer.OnCompleted();
                    //}
                }, -TimeSpan.FromTicks(1));
            });

        }


        public static IObservable<TState> GeneratePerFrame<TState>(TState state, Action<TState> iterate, IScheduler scheduler)
        {
            return GeneratePerFrame(state, /*null,*/ t => t, t => { iterate(t); return t; }, scheduler);
        }


    }

    #endregion
}
