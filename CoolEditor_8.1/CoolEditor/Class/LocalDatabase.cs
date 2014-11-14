using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CoolEditor.Resources;

namespace CoolEditor.Class
{
    public class LocalDatabase
    {
        public LocalDatabase()
        {
            using (var db = new FileItemDataContext(FileItemDataContext.DBConnectionString))
            {
                if (db.DatabaseExists() == false)
                {
                    db.CreateDatabase();
                }
                {
                    //debuger
                    //db.DeleteDatabase();
                    //db.CreateDatabase();
                }
            }
        }
    }

    public class FileItemDataContext : DataContext
    {
        public static string DBConnectionString = "Data Source=isostore:/FileItem_Cooleditor.sdf";

        public FileItemDataContext(string connectionString) : base(connectionString) { }

        public Table<FileItem> FileItems;
    }

    [Table]
    public class FileItem : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private int _id;

        [Column(IsPrimaryKey = true, IsDbGenerated = false, DbType = "INT NOT NULL", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int Id
        {
            get
            {
                return _id;
            }
            set
            {
                if (_id == value) return;
                NotifyPropertyChanging("Id");
                _id = value;
                NotifyPropertyChanged("Id");
            }
        }

        private int _revision;

        [Column]
        public int Revision
        {
            get
            {
                return _revision;
            }
            set
            {
                if (_revision == value) return;
                NotifyPropertyChanging("Revision");
                _revision = value;
                NotifyPropertyChanged("Revision");
            }
        }

        private string _fileName;
        [Column]
        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                if (_fileName == value) return;
                NotifyPropertyChanging("FileName");
                _fileName = value;
                NotifyPropertyChanged("FileName");
            }
        }

        private string _actualFileName;
        [Column]
        public string ActualFileName
        {
            get
            {
                return _actualFileName;
            }
            set
            {
                if (_actualFileName == value) return;
                NotifyPropertyChanging("ActualFileName");
                _actualFileName = value;
                NotifyPropertyChanged("ActualFileName");
            }
        }

        private string _onlineProvider;
        [Column]
        public string OnlineProvider
        {
            get
            {
                return _onlineProvider;
            }
            set
            {
                if (_onlineProvider == value) return;
                NotifyPropertyChanging("OnlineProvider");
                _onlineProvider = value;
                NotifyPropertyChanged("OnlineProvider");
            }
        }

        public string HasOnlineProvider
        {
            get { return (_onlineProvider == "dropbox" || _onlineProvider == "onedrive") ? "Visible" : "Collapsed"; }
        }

        public string IsDropbox
        {
            get { return (_onlineProvider == "dropbox") ? "Visible" : "Collapsed"; }
        }

        public string IsOnedrive
        {
            get { return (_onlineProvider == "onedrive") ? "Visible" : "Collapsed"; }
        }

        private string _onlinePath;

        [Column]
        public string OnlinePath
        {
            get { return _onlinePath; }
            set
            {
                if (_onlinePath == value) return;
                NotifyPropertyChanging("OnlinePath");
                _onlinePath = value;
                NotifyPropertyChanged("OnlinePath");
            }
        }

        public string OnlinePathStr
        {
            get { return OnlineProvider + ": " + OnlinePath; }
        }

        private string _localPath;

        [Column]
        public string LocalPath
        {
            get { return _localPath; }
            set
            {
                if (_localPath == value) return;
                NotifyPropertyChanging("LocalPath");
                _localPath = value;
                NotifyPropertyChanged("LocalPath");
            }
        }

        private DateTime _lastModifiedTime;
        public string LastModifiedTimeStr
        {
            get { return string.Format("{0}: {1}", AppResources.Last_modified, _lastModifiedTime.ToLocalTime()); }
        }
        [Column]
        public DateTime LastModifiedTime
        {
            get
            {
                return _lastModifiedTime;
            }
            set
            {
                if (_lastModifiedTime == value) return;
                NotifyPropertyChanging("LastModifiedTime");
                _lastModifiedTime = value;
                NotifyPropertyChanged("LastModifiedTime");
            }
        }

        private DateTime _lastSyncTime;
        public string LastSyncTimeStr
        {
            get { return string.Format("{0}: {1}", AppResources.Last_sync, _lastSyncTime.ToLocalTime()); }
        }
        [Column(CanBeNull = true)]
        public DateTime LastSyncTime
        {
            get
            {
                return _lastSyncTime;
            }
            set
            {
                if (_lastSyncTime == value) return;
                NotifyPropertyChanging("LastSyncTime");
                _lastSyncTime = value;
                NotifyPropertyChanged("LastSyncTime");
            }
        }

        private bool _modifiedSinceLastSync;

        [Column(CanBeNull = true)]
        public bool ModifiedSinceLastSync
        {
            get
            {
                return _modifiedSinceLastSync;
            }
            set
            {
                if (_modifiedSinceLastSync == value) return;
                NotifyPropertyChanging("ModifiedSinceLastSync");
                _modifiedSinceLastSync = value;
                NotifyPropertyChanged("ModifiedSinceLastSync");
            }
        }

        [Column(IsVersion = true)]
        private Binary _version;




        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;

        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        #endregion


    }
}
