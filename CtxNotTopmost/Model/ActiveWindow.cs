using System;

namespace CtxNotTopmost.Model
{
    internal class ActiveWindow
    {
        public string Title { get; set; }
        public IntPtr wHnd { get; set; }
        public string ProcessName { get; set; }
    }
}
