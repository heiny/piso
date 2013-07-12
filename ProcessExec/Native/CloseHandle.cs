using System;
using System.Runtime.InteropServices;

namespace ProcessExec.Native
{
    internal partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError=true)]
        public static extern bool CloseHandle(IntPtr handle);
    }
}
