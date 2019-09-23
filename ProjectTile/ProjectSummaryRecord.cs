using System;
using System.ComponentModel;

namespace ProjectTile
{
    public class ProjectSummaryRecord : Globals, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ProjectTypes projectType;
        private ProjectStages projectStage;
        private StaffSummaryRecord projectManager;
        
        public int ProjectID { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public string ProjectSummary { get; set; }
        public ProjectTypes ProjectType
        {
            get { return projectType; }
            set
            {
                projectType = value;
                OnPropertyChanged("ProjectType");
            }
        }
        public int EntityID { get; set; }
        public int? ClientID { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public StaffSummaryRecord ProjectManager
        {
            get { return projectManager; }
            set
            {
                projectManager = value;
                OnPropertyChanged("ProjectManager");
            }
        }
        //public int PMStaffID { get; set; }
        //public string PMStaffName { get; set; }
        public ProjectStages ProjectStage
        {
            get { return projectStage;  }
            set
            {
                projectStage = value;
                OnPropertyChanged("ProjectStage");
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




