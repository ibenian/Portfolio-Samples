using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Phone.Reactive;
using System.Text;
using Microsoft.Xna.Framework;
using GameBase.Helper;

namespace GameBase.Rx
{
    enum BoundaryCheckResult
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }

    static class MotionGenerator
    {
        static internal IObservable<Vector2> LinearMotion(Vector2 initPos, Vector2 speed, GameLoopScheduler scheduler)
        {
            return ObservableRx.GeneratePerFrame<Vector2, Vector2>(initPos,
                /*null,*/ p => p, p => p + speed * scheduler.Elapsed,
                scheduler);
        }

        static internal IObservable<PosSpeed> LinearMotion(PosSpeed init, Rectangle bounds, GameLoopScheduler scheduler)
        {
            return ObservableRx.GeneratePerFrame<PosSpeed, PosSpeed>(init,
                /*null,*/ ps => ps,
                ps =>
                {
                    if (ps.Pos.X < bounds.Left) ps.Speed = new Vector2(Math.Abs(ps.Speed.X), ps.Speed.Y);
                    else if (ps.Pos.X > bounds.Right) ps.Speed = new Vector2(-Math.Abs(ps.Speed.X), ps.Speed.Y);
                    else if (ps.Pos.Y < bounds.Top) ps.Speed = new Vector2(ps.Speed.X, Math.Abs(ps.Speed.Y));
                    else if (ps.Pos.Y > bounds.Bottom) ps.Speed = new Vector2(ps.Speed.X, -Math.Abs(ps.Speed.Y));
                    return new PosSpeed(ps.Pos + ps.Speed * scheduler.Elapsed, ps.Speed);
                },
                scheduler);
        }

        static internal IObservable<PosSpeed> LinearMotion(PosSpeed init, Func<Rectangle> getBounds, GameLoopScheduler scheduler)
        {
            return ObservableRx.GeneratePerFrame<PosSpeed, PosSpeed>(init,
                /*null,*/ ps => ps,
                ps =>
                {
                    var bounds = getBounds();
                    if (ps.Pos.X < bounds.Left) ps.Speed = new Vector2(Math.Abs(ps.Speed.X), ps.Speed.Y);
                    else if (ps.Pos.X > bounds.Right) ps.Speed = new Vector2(-Math.Abs(ps.Speed.X), ps.Speed.Y);
                    else if (ps.Pos.Y < bounds.Top) ps.Speed = new Vector2(ps.Speed.X, Math.Abs(ps.Speed.Y));
                    else if (ps.Pos.Y > bounds.Bottom) ps.Speed = new Vector2(ps.Speed.X, -Math.Abs(ps.Speed.Y));
                    return new PosSpeed(ps.Pos + ps.Speed * scheduler.Elapsed, ps.Speed);
                },
                scheduler);
        }

        static internal IObservable<PosSpeed> LinearMotion(PosSpeed init, Func<PosSpeed, BoundaryCheckResult> checkBounds, /* Func<PosSpeed, PosSpeed> iterateCallback, */ GameLoopScheduler scheduler)
        {
            return ObservableRx.GeneratePerFrame<PosSpeed, PosSpeed>(init,
                /*null,*/ ps => ps,
                ps =>
                {
                    var result = checkBounds(ps);
                    var deviation = RandomGenerator.Instance.NextFloat((float)-Math.PI / 20f, (float)Math.PI / 20f);
                    switch (result)
                    {
                        case BoundaryCheckResult.Left: ps.Speed = new Vector2(Math.Abs(ps.Speed.X), ps.Speed.Y).Rotate(deviation); break;
                        case BoundaryCheckResult.Right: ps.Speed = new Vector2(-Math.Abs(ps.Speed.X), ps.Speed.Y).Rotate(deviation); break;
                        case BoundaryCheckResult.Top: ps.Speed = new Vector2(ps.Speed.X, Math.Abs(ps.Speed.Y)).Rotate(deviation); break;
                        case BoundaryCheckResult.Bottom: ps.Speed = new Vector2(ps.Speed.X, -Math.Abs(ps.Speed.Y)).Rotate(deviation); break;
                    }
                    //return iterateCallback(new PosSpeed(ps.Pos + ps.Speed * scheduler.Elapsed, ps.Speed));
                    return new PosSpeed(ps.Pos + ps.Speed * scheduler.Elapsed, ps.Speed);
                },
                scheduler);
        }

        static internal IObservable<PosSpeed> Attractor(PosSpeed init, Func<PosSpeed, PosSpeed> getTarget, Func<PosSpeed, PosSpeed, float, PosSpeed> attractor, GameLoopScheduler scheduler, bool immediate)
        {
            if (immediate)
                return Observable.Generate<PosSpeed, PosSpeed>(init, ps => true,
                ps => ps, ps => attractor(ps, getTarget(ps), scheduler.Elapsed),
                Scheduler.Immediate);
            else
                return ObservableRx.GeneratePerFrame<PosSpeed, PosSpeed>(init,
                    /*null,*/ ps => ps, ps => attractor(ps, getTarget(ps), scheduler.Elapsed),
                scheduler);
        }

        static internal IObservable<float> Attractor(float init, Func<float, float> getTarget, Func<float, float, float, float> attractor, GameLoopScheduler scheduler, bool immediate)
        {
            if (immediate)
                return Observable.Generate<float, float>(init, ps => true,
                ps => ps, ps => attractor(ps, getTarget(ps), scheduler.Elapsed),
                Scheduler.Immediate);
            else
                return ObservableRx.GeneratePerFrame<float, float>(init,
                    /*null,*/ v => v, v => attractor(v, getTarget(v), scheduler.Elapsed),
                scheduler);
        }

        static internal IObservable<PosSpeed> LinearAttractor(PosSpeed init, Func<PosSpeed, PosSpeed> getTarget, GameLoopScheduler scheduler, bool immediate)
        {
            return Attractor(init, getTarget, LinearAttractor, scheduler, immediate);
        }

        static internal IObservable<PosSpeed> EasingAttractor(PosSpeed init, Func<PosSpeed, PosSpeed> getTarget, float speedFactor, GameLoopScheduler scheduler, bool immediate)
        {
            return Attractor(init, getTarget, (i, t, s) => EasingAttractor(i, t, speedFactor, s), scheduler, immediate);
        }

        static internal IObservable<PosSpeed> EasingAttractor(PosSpeed init, Func<PosSpeed, PosSpeed> getTarget, GameLoopScheduler scheduler, bool immediate)
        {
            return Attractor(init, getTarget, EasingAttractor, scheduler, immediate);
        }

        static internal IObservable<float> EasingAttractor(float init, Func<float, float> getTarget, float speedFactor, GameLoopScheduler scheduler, bool immediate)
        {
            return Attractor(init, getTarget, (v, t, e) => EasingAttractor(v, t, speedFactor, e), scheduler, immediate);
        }

        internal static PosSpeed LinearAttractor(PosSpeed ps, PosSpeed target, float elapsed)
        {
            if (Vector2.Distance(ps.Pos, target.Pos) <= ps.Speed.Length() * elapsed)
                return target;
            else
                return new PosSpeed(ps.Pos + ps.Speed.Length() * Vector2Helper.UnitDirectionWithTolerence(ps.Pos, target.Pos) * elapsed, ps.Speed);
        }

        internal static PosSpeed EasingAttractor(PosSpeed ps, PosSpeed target, float elapsed)
        {
            return EasingAttractor(ps, target, 1f, elapsed);
        }

        internal static PosSpeed EasingAttractor(PosSpeed ps, PosSpeed target, float speedFactor, float elapsed)
        {
            var speed = (target.Pos - ps.Pos) * speedFactor;
            if (Vector2.Distance(ps.Pos, target.Pos) <= speedFactor * elapsed)
                return target;
            else
                return new PosSpeed(ps.Pos +  speed * elapsed, speed);
        }

        internal static float EasingAttractor(float value, float target, float speedFactor, float elapsed)
        {
            var speed = (target - value) * speedFactor;
            if (Math.Abs(target - value) <= speedFactor * elapsed)
                return target;
            else
                return value + speed * elapsed;
        }

        //static internal IObservable<Vector2> LinearAttractor(Vector2 initPos, float speed, Vector2 targetPos, GameLoopScheduler scheduler)
        //{
        //    return ObservableExtensions.GeneratePerFrame<Vector2, Vector2>(initPos,
        //        null, p => p, p => LinearAttract(p, speed, targetPos, scheduler.Elapsed),
        //        scheduler);
        //}

        static internal IObservable<Vector2> LinearAttractor(Vector2 initPos, float speed, Func<Vector2, Vector2> getTargetPos, GameLoopScheduler scheduler)
        {
            return ObservableRx.GeneratePerFrame<Vector2, Vector2>(initPos,
                /*null,*/ p => p, p => LinearAttract(p, speed, getTargetPos(p), scheduler.Elapsed),
                scheduler);
        }

        internal static Vector2 LinearAttract(Vector2 p, float speed, Vector2 targetPos, float elapsed)
        {
            if (Vector2.Distance(p, targetPos) <= speed * elapsed)
                return targetPos;
            else
                return p + speed * Vector2Helper.UnitDirectionWithTolerence(p, targetPos) * elapsed;
        }

        


        static internal IObservable<PosSpeed> AcceleratedMotion(PosSpeed init, Vector2 acceleration, GameLoopScheduler scheduler)
        {
            return ObservableRx.GeneratePerFrame<PosSpeed, PosSpeed>(init,
                /*null,*/ ps => ps,
                ps => 
                {
                    return new PosSpeed(ps.Pos + ps.Speed * scheduler.Elapsed, ps.Speed + acceleration * scheduler.Elapsed);
                },
                scheduler);
        }

        static internal IObservable<PosSpeed> BounceMotion(PosSpeed init, Vector2 acceleration, Func<PosSpeed, bool> bounceCoundition, Func<PosSpeed, Vector2> bounceSpeed, GameLoopScheduler scheduler)
        {
            return ObservableRx.GeneratePerFrame<PosSpeed, PosSpeed>(init,
                /*null,*/ ps => ps,
                ps =>
                {
                    if (bounceCoundition(ps))
                        ps.Speed = bounceSpeed(ps);
                    return new PosSpeed(ps.Pos + ps.Speed * scheduler.Elapsed, ps.Speed + acceleration * scheduler.Elapsed);
                },
                scheduler);
        }

        static internal IObservable<PosSpeed> BounceMotion(PosSpeed init, Vector2 acceleration, Func<PosSpeed, PosSpeed> bounceCheck, GameLoopScheduler scheduler)
        {
            return ObservableRx.GeneratePerFrame<PosSpeed, PosSpeed>(init,
                /*null,*/ ps => ps,
                ps =>
                {
                    var bc = bounceCheck(ps);
                    if (bc.Speed != Vector2.Zero)
                        ps = bc;
                    return new PosSpeed(ps.Pos + ps.Speed * scheduler.Elapsed, ps.Speed + acceleration * scheduler.Elapsed);
                },
                scheduler);
        }

        static internal IObservable<AngleAndValue> SineMotion(float m, float f, float ph, GameLoopScheduler scheduler)
        {
            float t0 = scheduler.TotalTime;
            return ObservableRx.GeneratePerFrame<float, AngleAndValue>(0f,
                /*null,*/ p => 
                    {
                        var angle = 2 * (float)Math.PI * f * (scheduler.TotalTime - t0) + ph;
                        return new AngleAndValue(angle,  m * (float)Math.Sin((float)angle));
                    }, p => p, 
                scheduler);
        }

        static internal IObservable<Vector2> CircularMotion(Vector2 m, Vector2 f, Vector2 ph, GameLoopScheduler scheduler)
        {
            float t0 = scheduler.TotalTime;
            return ObservableRx.GeneratePerFrame<float, Vector2>(0f,
                /*null,*/ t => new Vector2(m.X * (float)Math.Cos(2 * (float)Math.PI * f.X * (scheduler.TotalTime - t0) + ph.X), m.Y * (float)Math.Sin(2 * (float)Math.PI * f.Y * (scheduler.TotalTime - t0) + ph.Y)), t => t,
                scheduler);
        }

        static internal IObservable<TState> StatefulMotion<TState>(TState state, Action<TState> iterate, GameLoopScheduler scheduler)
        {
            return ObservableRx.GeneratePerFrame<TState>(state, iterate, scheduler);
        }

        public static IObservable<Vector2> OffsetBy(this IObservable<Vector2> path1, Vector2 offset)
        {
            return from p1 in path1
                   select p1 + offset;
        }

        public static IObservable<Vector2> OffsetBy(this IObservable<Vector2> path1, Func<Vector2, Vector2> getOffset)
        {
            return from p1 in path1
                   select p1 + getOffset(p1);
        }

        public static IObservable<Vector2> OffsetBy(this IObservable<Vector2> path1, IObservable<Vector2> path2)
        {
            return path1.Zip(path2, (l, r) => l + r);
        }

        public static IObservable<PosSpeed> OffsetBy(this IObservable<PosSpeed> path1, IObservable<PosSpeed> path2)
        {
            return path1.Zip(path2, (p1, p2) => new PosSpeed(p1.Pos + p2.Pos, p1.Speed + p2.Speed));
        }

        public static IObservable<PosSpeed> OffsetPosBy(this IObservable<PosSpeed> path1, IObservable<PosSpeed> path2)
        {
            return path1.Zip(path2, (p1, p2) => new PosSpeed(p1.Pos + p2.Pos, p1.Speed));
        }

        public static IObservable<PosSpeed> OffsetXPosBy(this IObservable<PosSpeed> path1, IObservable<PosSpeed> path2)
        {
            return path1.Zip(path2, (p1, p2) => new PosSpeed(new Vector2(p1.Pos.X + p2.Pos.X, p1.Pos.Y), p1.Speed));
        }

        public static IObservable<PosSpeed> OffsetYPosBy(this IObservable<PosSpeed> path1, IObservable<PosSpeed> path2)
        {
            return path1.Zip(path2, (p1, p2) => new PosSpeed(new Vector2(p1.Pos.X, p1.Pos.Y + p2.Pos.Y), p1.Speed));
        }

        public static IObservable<PosSpeed> SetXFrom(this IObservable<PosSpeed> path1, IObservable<PosSpeed> path2)
        {
            return path1.Zip(path2, (p1, p2) => new PosSpeed(new Vector2(p2.Pos.X, p1.Pos.Y), p1.Speed));
        }

        public static IObservable<PosSpeed> AsPosSpeed(this IObservable<Vector2> path, Vector2 speed)
        {
            return from p in path
                   select new PosSpeed(p, speed);
        }

        public static IObservable<Vector2> AsPos(this IObservable<PosSpeed> path)
        {
            return from p in path
                   select p.Pos;
        }

    }

    internal struct PosSpeed
    {
        public Vector2 Pos;
        public Vector2 Speed;

        public PosSpeed(Vector2 pos, Vector2 speed)
        {
            this.Pos = pos;
            this.Speed = speed;
        }

        public static readonly PosSpeed Zero = new PosSpeed();
    }

    internal struct PosSize
    {
        public Vector2 Pos;
        public float Size;

        public PosSize(Vector2 pos, float size)
        {
            this.Pos = pos;
            this.Size = size;
        }
    }

    internal struct AngleAndValue
    {
        public float Angle;
        public float Value;

        public AngleAndValue(float angle, float value)
        {
            this.Angle = angle;
            this.Value = value;
        }
    }
}
