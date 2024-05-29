using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Augmenta
{
    internal static class Utils
    {
        internal static int ReadInt(ReadOnlySpan<byte> data, int offset)
        {
            return MemoryMarshal.Cast<byte, int>(data.Slice(offset))[0];
        }

        internal static float ReadFloat(ReadOnlySpan<byte> data, int offset)
        {
            return MemoryMarshal.Cast<byte, float>(data.Slice(offset))[0];
        }


    }
}
