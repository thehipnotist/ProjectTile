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
        public Hashtable DateHash = new Hashtable();
        public Hashtable InitialDates = new Hashtable();
        private ProjectStages stage;

        public TimelineType TimeType { get; set; }
        public ProjectStages Stage
        {
            get { return stage; }
            set
            {
                stage = value;
                OnPropertyChanged("Stage");
            }
        }

        public int StageID { get { return (stage == null) ? 0 : stage.ID; } }
        public int StageNumber { get { return (stage == null) ? 0 : stage.StageNumber; } }

        public DateTime? Stage0Date 
        { 
            get { return (DateTime?) DateHash[0]; }
            set { DateHash[0] = value; }
        }
        public DateTime? Stage1Date
        {
            get { return (DateTime?)DateHash[1]; }
            set { DateHash[1] = value; }
        }
        public DateTime? Stage2Date
        {
            get { return (DateTime?)DateHash[2]; }
            set { DateHash[2] = value; }
        }
        public DateTime? Stage3Date
        {
            get { return (DateTime?)DateHash[3]; }
            set { DateHash[3] = value; }
        }
        public DateTime? Stage4Date
        {
            get { return (DateTime?)DateHash[4]; }
            set { DateHash[4] = value; }
        }
        public DateTime? Stage5Date
        {
            get { return (DateTime?)DateHash[5]; }
            set { DateHash[5] = value; }
        }
        public DateTime? Stage6Date
        {
            get { return (DateTime?)DateHash[6]; }
            set { DateHash[6] = value; }
        }
        public DateTime? Stage7Date
        {
            get { return (DateTime?)DateHash[7]; }
            set { DateHash[7] = value; }
        }
        public DateTime? Stage8Date
        {
            get { return (DateTime?)DateHash[8]; }
            set { DateHash[8] = value; }
        }
        public DateTime? Stage9Date
        {
            get { return (DateTime?)DateHash[9]; }
            set { DateHash[9] = value; }
        }
        public DateTime? Stage10Date
        {
            get { return (DateTime?)DateHash[10]; }
            set { DateHash[10] = value; }
        }
        public DateTime? Stage11Date
        {
            get { return (DateTime?)DateHash[11]; }
            set { DateHash[11] = value; }
        }
        public DateTime? Stage12Date
        {
            get { return (DateTime?)DateHash[12]; }
            set { DateHash[12] = value; }
        }
        public DateTime? Stage13Date
        {
            get { return (DateTime?)DateHash[13]; }
            set { DateHash[13] = value; }
        }
        public DateTime? Stage14Date
        {
            get { return (DateTime?)DateHash[14]; }
            set { DateHash[14] = value; }
        }
        public DateTime? Stage15Date
        {
            get { return (DateTime?)DateHash[15]; }
            set { DateHash[15] = value; }
        }
        public DateTime? Stage16Date
        {
            get { return (DateTime?)DateHash[16]; }
            set { DateHash[16] = value; }
        }
        public DateTime? Stage99Date
        {
            get { return (DateTime?)DateHash[99]; }
            set { DateHash[99] = value; }
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

        public void StoreOriginalValues()
        {
            InitialDates = (Hashtable) DateHash.Clone();
        }


    } // class
} // namespace
