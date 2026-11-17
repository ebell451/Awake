using System;

namespace Awake
{
    public static class AwakePolicy
    {
        public static int CalculateIdleMinutes(uint systemUptimeTicks, uint lastInputTicks)
        {
            uint idleTicks = unchecked(systemUptimeTicks - lastInputTicks);
            return (int)(idleTicks / 1000 / 60) + 1;
        }

        public static bool ShouldDeactivate(int period, int idleMinutes, bool isLocked, bool disableOnLock)
        {
            return (period != -1 && idleMinutes > period) || (isLocked && disableOnLock);
        }
    }
}
