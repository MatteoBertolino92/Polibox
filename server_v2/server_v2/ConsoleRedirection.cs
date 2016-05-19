using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.ComponentModel;

namespace Server_v2
{
    class ConsoleRedirection : TextWriter
    {
        BackgroundWorker bw;
        string stringa;
        private static object lockBW = new object();

        public ConsoleRedirection(BackgroundWorker bw)
        {
            this.bw = bw;
        }

        public override void Write(char value)
        {
            base.Write(value);
            stringa += value.ToString();
            if (value == '\n')
            {
                lock (lockBW)
                {
                    bw.ReportProgress(1, stringa);
                    stringa = "";
                }
            }
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }

    }
}
