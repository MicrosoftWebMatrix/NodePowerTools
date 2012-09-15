using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NodePowerTools
{
    public class ConsoleLog
    {
        public string Path { get; set; }
        public long LogLegnth { get; set; }
        public StreamReader FileStream { get; set; } 
    }
}
