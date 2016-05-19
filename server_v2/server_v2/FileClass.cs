using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Server_v2
{
    class FileClass
    {
        private UserClass user;
        private string filename;
        private FolderClass folder;
        private string checksum;
        private string timestamp;

        #region COSTRUCTORS

        public FileClass()
        {
            this.user = null;
            this.filename = null;
            this.folder = new FolderClass();
            this.checksum = null;
        }

        public FileClass(UserClass user)
        {
            this.user = user;
        }

        public FileClass(UserClass user, string filename, FolderClass folder, string checksum)
        {
            this.user = user;
            this.filename = filename;
            this.folder = folder;
            this.checksum = checksum;
        }

        #endregion

        #region EQUALS

        public override bool Equals(System.Object obj)
        {
            if (obj == null) return false;

            FileClass file = obj as FileClass;
            return Equals(file);
        }


        public bool Equals(FileClass file)
        {
            if ((object)file == null) return false;

            bool bool1 = (this.user.Equals(file.user));
            bool bool2 = (this.checksum == file.checksum);
            bool bool3 = (this.filename == file.filename);
            bool bool4 = (this.folder.Equals(file.folder));

            return bool1 && bool2 && bool3 && bool4;
        }

        public override int GetHashCode()
        {
            return this.GetHashCode();
        }

        #endregion

        #region PROPERTIES

        public UserClass User
        {
            set { user = value; }
            get { return user; }
        }

        public string Filename
        {
            set { filename = value; }
            get { return filename; }
        }

        public FolderClass Folder
        {
            set { folder = value; }
            get { return folder; }
        }

        public string Checksum
        {
            set { checksum = value; }
            get { return checksum; }
        }

        public string Timestamp
        {
            set { timestamp = value; }
            get { return timestamp; }
        }

        #endregion

    }
}