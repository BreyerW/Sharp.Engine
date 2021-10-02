using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sharp
{
    public static class Time
    {
        private static Stopwatch Clock = new Stopwatch();

        public static int startTime = Process.GetCurrentProcess().StartTime.Millisecond; //Environment.TickCount;
        public static double deltaTime;

        static Time()
        {
            Clock.Start();
        }

        internal static void SetTime()
        {
            Clock.Stop();
            deltaTime = Clock.Elapsed.TotalMilliseconds;
            Clock.Reset();
            Clock.Start();
        }
    }
}