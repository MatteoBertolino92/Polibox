using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Server_v2
{
    class UserClass
    {
        private string username;
        private string password;
        private string sale;
        private string clientPath;
        private string serverPath;
        private string ip;

        #region CONSTRUCTOR

        public UserClass()
        {

        }

        public UserClass(string username)
        {
            username = username.ToUpper();
            this.username = username;
        }

        public UserClass(string username, string password, string sale, string path)
        {
            username = username.ToUpper();
            this.username = username;
            this.password = password;
            this.sale = sale;
            if (path.Last() == '\\') this.clientPath = path; else this.clientPath = path + "\\";
            this.ClientPath = path;
            this.ServerPath = ".\\users\\" + this.username;
        }

        #endregion

        #region EQUALS

        public override bool Equals(System.Object obj)
        {
            if (obj == null) return false;

            UserClass user = obj as UserClass;
            return Equals(user);
        }

        public bool Equals(UserClass user)
        {
            if ((object)user == null) return false;

            return this.Username == user.Username;
        }

        public override int GetHashCode()
        {
            return this.GetHashCode();
        }

        #endregion

        #region PROPERTIES

        public string Username
        {
            set { this.username = value.ToUpper(); }
            get { return this.username; }
        }

        public string ClientPath
        {
            set
            {
                if (value.Last() == '\\') this.clientPath = value; else this.clientPath = value + "\\";
                this.serverPath = ".\\users\\" + this.username;
                Directory.CreateDirectory(this.serverPath);
            }
            get { return this.clientPath; }
        }

        public string Sale
        {
            set { this.sale = value; }
            get { return this.sale; }
        }

        public string Password
        {
            set { this.password = value; }
            get { return this.password; }
        }

        public string ServerPath
        {
            set { this.serverPath = value; }
            get { return this.serverPath; }
        }

        public string Ip
        {
            set { this.ip = value; }
            get { return this.ip; }
        }

        #endregion

    }
}
