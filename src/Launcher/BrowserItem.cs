using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal sealed class BrowserItem {
    public int Pid; public IntPtr Window; public string Name;
    public override string ToString() { return Name + " (PID " + Pid + ")"; }
}
