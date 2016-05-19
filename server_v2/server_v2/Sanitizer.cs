using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_v2
{
    class Sanitizer
    {
        public static string sanitize(string stringa)
        {
            stringa = stringa.Replace('\'', '|');
            stringa = stringa.Replace('%', '*');
            return stringa;
        }

        public static string desanitize(string stringa)
        {
            stringa = stringa.Replace('|', '\'');
            stringa = stringa.Replace('*', '%');
            return stringa;
        }
    }
}
