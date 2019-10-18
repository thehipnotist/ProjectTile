using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class TimelineProxy : Globals, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private ProjectStages stage;
        
        public Hashtable dateHash = new Hashtable();
        public ProjectStages Stage
        {
            get { return stage; }
            set
            {
                stage = value;
                OnPropertyChanged("Stage");
            }
        }

        public int StageID
        {
            get { return (stage == null) ? 0 : stage.ID; }
        }

        public int StageNumber
        {
            get { return (stage == null) ? 0 : stage.StageNumber; }
        }

        public DateTime? Stage0Date 
        { 
            get { return (DateTime?) dateHash[0]; }
            set { dateHash[0] = value; }
        }
        public DateTime? Stage1Date
        {
            get { return (DateTime?)dateHash[1]; }
            set { dateHash[1] = value; }
        }
        public DateTime? Stage2Date
        {
            get { return (DateTime?)dateHash[2]; }
            set { dateHash[2] = value; }
        }
        public DateTime? Stage3Date
        {
            get { return (DateTime?)dateHash[3]; }
            set { dateHash[3] = value; }
        }
        public DateTime? Stage4Date
        {
            get { return (DateTime?)dateHash[4]; }
            set { dateHash[4] = value; }
        }
        public DateTime? Stage5Date
        {
            get { return (DateTime?)dateHash[5]; }
            set { dateHash[5] = value; }
        }
        public DateTime? Stage6Date
        {
            get { return (DateTime?)dateHash[6]; }
            set { dateHash[6] = value; }
        }
        public DateTime? Stage7Date
        {
            get { return (DateTime?)dateHash[7]; }
            set { dateHash[7] = value; }
        }
        public DateTime? Stage8Date
        {
            get { return (DateTime?)dateHash[8]; }
            set { dateHash[8] = value; }
        }
        public DateTime? Stage9Date
        {
            get { return (DateTime?)dateHash[9]; }
            set { dateHash[9] = value; }
        }
        public DateTime? Stage10Date
        {
            get { return (DateTime?)dateHash[10]; }
            set { dateHash[10] = value; }
        }
        public DateTime? Stage11Date
        {
            get { return (DateTime?)dateHash[11]; }
            set { dateHash[11] = value; }
        }
        public DateTime? Stage12Date
        {
            get { return (DateTime?)dateHash[12]; }
            set { dateHash[12] = value; }
        }
        public DateTime? Stage13Date
        {
            get { return (DateTime?)dateHash[13]; }
            set { dateHash[13] = value; }
        }
        public DateTime? Stage14Date
        {
            get { return (DateTime?)dateHash[14]; }
            set { dateHash[14] = value; }
        }
        public DateTime? Stage15Date
        {
            get { return (DateTime?)dateHash[15]; }
            set { dateHash[15] = value; }
        }
        public DateTime? Stage16Date
        {
            get { return (DateTime?)dateHash[16]; }
            set { dateHash[16] = value; }
        }
        public DateTime? Stage17Date
        {
            get { return (DateTime?)dateHash[17]; }
            set { dateHash[17] = value; }
        }

        protected void OnPropertyChanged(string eventName)
        {
            try
            {
                PropertyChangedEventHandler thisHandler = PropertyChanged;
                if (thisHandler != null)
                {
                    thisHandler(this, new PropertyChangedEventArgs(eventName));
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error handling changed property", generalException); }
        }

    } // class
} // namespace
