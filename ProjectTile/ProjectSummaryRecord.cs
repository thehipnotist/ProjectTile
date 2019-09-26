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
            get { return stage; }
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
    
        public bool IsCancelled
        {
            get { return stage.StageCode == CancelledStage; }
        }
    
        public bool IsInternal
        {
            get { return type.TypeCode == InternalProjectType; }
        }

        public bool IsNew
        {
            get { return (ProjectID == 0); }
        }

        public int StageID
        {
            get { return stage.StageCode; }
        }

        public bool ConvertToProject(ref Projects project) // Uses a reference to easily amend an existing database record
        {
            try
            {
                project.ID = ProjectID;
                project.EntityID = EntityID;
                project.ProjectCode = ProjectCode;
                project.TypeCode = Type.TypeCode;
                project.ProjectName = ProjectName;
                project.StartDate = StartDate;
                project.StageCode = Stage.StageCode;
                project.ProjectSummary = ProjectSummary;
                if (Client.ID != NoID) { project.ClientID = Client.ID; } // 'No client' is null in the database)

                return true;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error converting project summary to project record", generalException);
                return false;
            }
        }

    }
}




