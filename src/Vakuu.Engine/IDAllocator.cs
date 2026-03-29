using System.Threading;

namespace Vakuu.Engine
{
    sealed class IDAllocator
    {
        ulong lastID;

        public ulong Allocate() => Interlocked.Increment(ref lastID);
    }
}
