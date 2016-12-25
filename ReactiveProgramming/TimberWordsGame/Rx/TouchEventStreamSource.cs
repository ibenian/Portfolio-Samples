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
    #region TouchEventStreamSource

    internal class TouchEventStreamSource : Microsoft.Xna.Framework.GameComponent
    {
        ISubject<TouchLocation> touchSubject;

        public TouchEventStreamSource(Game game, bool autoAdd)
            : base(game)
        {
            if (autoAdd)
            {
                game.Components.Add(this);
            }

            this.touchSubject = new Subject<TouchLocation>();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            var touchCol = TouchPanel.GetState();

            foreach (var touchLoc in touchCol)
            {
                touchSubject.OnNext(touchLoc);
            }
        }

        public void HandleInput(TouchCollection touchCol)
        {
            foreach (var touchLoc in touchCol)
            {
                touchSubject.OnNext(touchLoc);
            }
        }

        public IObservable<TouchLocation> Touch
        {
            get { return this.touchSubject; }
        }

        public IObservable<TouchLocation> TouchDownOrUp
        {
            get { return from t in this.touchSubject where t.State == TouchLocationState.Pressed || t.State == TouchLocationState.Released select t; }
        }

        public IObservable<Timestamped<TouchLocation>> TouchWithTime
        {
            get { return Touch.Timestamp(); }
        }

        public IObservable<TouchLocation> TouchDown
        {
            get { return this.touchSubject.Where(t => t.State == TouchLocationState.Pressed); }
        }

        public IObservable<Timestamped<TouchLocation>> TouchDownWithTime
        {
            get { return TouchDown.Timestamp(); }
        }

        public IObservable<TouchLocation> TouchUp
        {
            get { return this.touchSubject.Where(t => t.State == TouchLocationState.Released); }
        }

        public IObservable<Timestamped<TouchLocation>> TouchUpWithTime
        {
            get { return TouchUp.Timestamp(); }
        }

        public IObservable<TouchLocation> TouchMove
        {
            get { return this.touchSubject.Where(t => t.State == TouchLocationState.Moved); }
        }

        public IObservable<Timestamped<TouchLocation>> TouchMoveWithTime
        {
            get { return TouchMove.Timestamp(); }
        }

        public IObservable<TouchPair> ArbitraryTouchPair
        {
            get { return TouchDownWithTime.Zip(TouchUpWithTime, (tl, tr) => new TouchPair { TouchDown = tl, TouchUp = tr }); }
        }

        public IObservable<IGroupedObservable<int, Timestamped<TouchLocation>>> Swipe
        {
            get { return this.TouchWithTime.GroupBy(tl => tl.Value.Id); }
        }

        public IObservable<TouchPair> GroupedTouchPair
        {
            get
            {
                return Swipe.SelectMany(swp =>
                      swp.Where(t => t.Value.State == TouchLocationState.Pressed)
                         .Zip(swp.Where(t => t.Value.State == TouchLocationState.Released),
                            (tl, tr) => new TouchPair() { TouchDown = tl, TouchUp = tr }));
            }
        }

        internal void PushTouchEvent(TouchLocation touchLoc)
        {
            touchSubject.OnNext(touchLoc);
        }
    }

    internal class TouchPair
    {
        public Timestamped<TouchLocation> TouchDown;
        public Timestamped<TouchLocation> TouchUp;

        public override string ToString()
        {
            return string.Format("{0} {1}", TouchDown.Value, TouchUp.Value);
        }
    }

    #endregion

}
