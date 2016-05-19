using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Server_v2
{
    class FolderClass
    {
        private string folderpath;
        private UserClass user;
        private FolderClass parent;
        private bool present;

        #region CONSTRUCTOR
        public FolderClass()
        {
            this.folderpath = null;
            this.user = new UserClass();
            this.present = false;
            parent = new FolderClass(null);
        }

        private FolderClass(FolderClass parent)
        {
            this.folderpath = null;
            this.user = new UserClass();
            this.present = false;
            this.parent = parent;
        }
        /// <summary>
        /// Crea l'oggetto Folder impostando l'utente, il path e il mypath e creando i direttori.
        /// Tutto sanizzato per il DB.
        /// </summary>
        /// <param name="utente">Utente del direttorio</param>
        /// <param name="path">Stringa contenente il percorso</param>
        public FolderClass(UserClass user, string folderpath, FolderClass parent)
        {
            this.user = user;
            if (folderpath.Last() == '\\') this.folderpath = folderpath; else this.folderpath = folderpath + "\\";
            this.parent = parent;
            this.present = true;
        }

        /// <summary>
        /// Crea oggetto Folder
        /// </summary>
        /// <param name="folderpath"></param>
        /// <param name="user"></param>
        /// <param name="parent"></param>
        /// <param name="present"></param>
        public FolderClass(string folderpath, UserClass user, FolderClass parent, bool present)
        {
            if (folderpath.Last() == '\\') this.folderpath = folderpath; else this.folderpath = folderpath + "\\";
            this.user = user;
            this.parent = parent;
            this.present = present;
        }

        public FolderClass(string folderpath, UserClass user, bool present)
        {
            if (folderpath.Last() == '\\') this.folderpath = folderpath; else this.folderpath = folderpath + "\\";
            this.user = user;
            this.present = true;
        }

        #endregion

        #region EQUALS

        public override bool Equals(System.Object obj)
        {
            if (obj == null) return false;

            FolderClass folder = obj as FolderClass;
            return Equals(folder);
        }


        public bool Equals(FolderClass folder)
        {
            if ((object)folder == null) return false;

            bool bool1 = (this.User.Equals(folder.user));
            bool bool2 = (this.FolderPath == folder.FolderPath);

            return bool1 && bool2;
        }

        public override int GetHashCode()
        {
            return this.GetHashCode();
        }

        #endregion

        #region PROPERTIES

        public string FolderPath
        {
            set { if (value.Last() == '\\') folderpath = value; else folderpath = value + "\\"; }
            get { return this.folderpath; }
        }

        public UserClass User
        {
            set { this.user = value; }
            get { return user; }
        }

        public FolderClass Parent
        {
            set { this.parent = value; }
            get { return this.parent; }
        }

        public bool Present
        {
            set { present = value; }
            get { return this.present; }
        }

        public bool Absent
        {
            set { if (value == true) present = false; else if (value == false) present = true; }
            get { if (present == true) return false; else if (present == false) return true; return false; }
        }

        #endregion
    }
}
