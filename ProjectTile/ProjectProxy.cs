﻿using System;
using System.ComponentModel;

namespace ProjectTile
{
    public class ProjectProxy : Globals, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ProjectTypes type;
        private ProjectStages stage;
        private StaffProxy projectManager;
        private ClientProxy client;
        
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
        public ClientProxy Client
        {
            get { return client; }
            set
            {
                client = value;
                OnPropertyChanged("Client");
            }
        }
        public StaffProxy ProjectManager
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
            get { return (stage == null)? false : (stage.StageNumber == CancelledStage); }
        }

        public bool IsOld
        {
            get { return (stage == null)? false : (stage.ProjectStatus == ClosedStatus); }
        }
    
        public bool IsInternal
        {
            get { return (type == null)? false : (type.TypeCode == InternalProjectCode); }
        }

        public bool IsNew
        {
            get { return (ProjectID == 0); }
        }

        public int StageID
        {
            get { return (stage == null)? 0 : stage.ID; }
        }

        public int StageNumber
        {
            get { return (stage == null) ? 0 : stage.StageNumber; }
        }

        public string CodeName
        {
            get { return (ProjectCode == ProjectName)? ProjectCode : ProjectCode + ": " + ProjectName; }
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
                project.StageID = Stage.ID;
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




