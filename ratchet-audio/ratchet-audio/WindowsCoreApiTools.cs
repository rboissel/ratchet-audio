using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ratchet.Audio
{
    internal static class WindowsCoreApiTools
    {
        internal static unsafe string ReadString(ulong pStr)
        {
            if (pStr == 0) { return ""; }
            List<byte> array = new List<byte>();
            byte* p = (byte*)pStr;
            while (*p != 0 || *(p + 1) != 0)
            {
                array.Add(*p);
                p++;
                array.Add(*p);
                p++;
            }
            return System.Text.Encoding.Unicode.GetString(array.ToArray());
        }

    }
}
