using System;
using System.ComponentModel;

namespace ProjectTile
{
    public class ProjectSummaryRecord : Globals, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string typeName;
        private string typeDescription;
        private ProjectTypes projectType;
        
        
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
        //public string TypeCode { get; set; }
        //public string TypeName 
        //{
        //    get { return typeName; } 
        //    set
        //    {
        //        typeName = value;
        //        ProjectTypes thisType = ProjectFunctions.GetTypeFromName(typeName);
        //        TypeCode = thisType.TypeCode;
        //        TypeDescription = thisType.TypeDescription;
        //        OnPropertyChanged("TypeName");
        //    }
        //}
        //public string TypeDescription 
        //{
        //    get { return typeDescription;  } 
        //    set
        //    {
        //        typeDescription = value;
        //        OnPropertyChanged("TypeDescription");
        //    }
        //}
        public int EntityID { get; set; }
        public int? ClientID { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int PMStaffID { get; set; }
        public string PMStaffName { get; set; }
        public int StageCode { get; set; }
        public string StageName { get; set; }
        public string StageDescription { get; set; }
        public string ProjectStatus { get; set; }
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




