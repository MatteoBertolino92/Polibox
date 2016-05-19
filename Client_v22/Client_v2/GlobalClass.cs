using Client_v2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Client
{
    static class GlobalClass
    {
        internal static string sale = null;
        internal static string pswClear = null;
        internal static string pswSale = null;
        internal static string name = null;
        internal static SSLStream k = null;
        internal static string path = null;
        internal static FileSystemWatcher watcher;
        internal static bool firstActive = false;
        internal static bool mainActive = false;
        internal static Watcher fw;
        internal static TimerC timer = null;
        internal static object lockableObj = new object();
        internal static object locktry = new object();
        internal static string IP="";
        internal delegate void nomedelegate();
        internal static string pattern = @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$";
        internal static Window main;
        internal static bool regular = false;
        internal static volatile bool onGoing = false;
        internal static Dictionary<string, string> dictRecover = new Dictionary<string, string> ();

        internal static string calculateChecksum(string path)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }
    }
}