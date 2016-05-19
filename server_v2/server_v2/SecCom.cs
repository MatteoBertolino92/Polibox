using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Server_v2
{
    class SecCom
    {
        public static MD5 md5HashVar = MD5.Create();
        public static List<SSLStream> listSock = new List<SSLStream>();

        public static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        public static string RandomString(int length)
        {
            char[] AvailableCharacters = {
                 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
                 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
                 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
                 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                 '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_'
             };
            char[] identifier = new char[length];
            byte[] randomData = new byte[length];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomData);
            }

            for (int idx = 0; idx < identifier.Length; idx++)
            {
                int pos = randomData[idx] % AvailableCharacters.Length;
                identifier[idx] = AvailableCharacters[pos];
            }

            return new string(identifier);
        }

        public static string createSale()
        {
            return RandomString(12);
        }

        public static string getRealPassword(string psw, string sale)
        {
            return SecCom.GetMd5Hash(SecCom.md5HashVar, (psw + sale));
        }
    }
}
