using System;
using System.ComponentModel;

namespace ProjectTile
{
    public class ProjectSummaryRecord : Globals, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ProjectTypes type;
        private ProjectStages stage;
        private StaffSummaryRecord projectManager;
        private ClientSummaryRecord client;
        
        public int ProjectID { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public string ProjectSummary { get; set; }
        public ProjectTypes Type
        {
            get { return type; }
            set
            {
                type = value;
                OnPropertyChanged("Type");
            }
        }
        public int EntityID { get; set; }
        public ClientSummaryRecord Client
        {
            get { return client; }
            set
            {
                client = value;
                OnPropertyChanged("Client");
            }
        }
        //public int? ClientID { get; set; }
        //public string ClientCode { get; set; }
        //public string ClientName { get; set; }
        public StaffSummaryRecord ProjectManager
        {
            get { return projectManager; }
            set
            {
                projectManager = value;
                OnPropertyChanged("ProjectManager");
            }
        }
        public ProjectStages Stage
        {
            get { return stage;  }
            set
            {
                stage = value;
                OnPropertyChanged("Stage");
            }
        }
        public DateTime? StartDate { get; set; }

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
    
    
    
    }
}




