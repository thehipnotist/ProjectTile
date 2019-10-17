using System.Collections.Generic;
using System.Linq;

namespace ProjectTile
{
    public class ProjectProductProxy : Globals
    {
        public int ID { get; set; }
        public Projects Project { get; set; }
        public Products Product { get; set; }
        public decimal OldVersion { get; set; }
        public decimal NewVersion { get; set; }
        public bool JustAdded { get; set; }

        public int ProjectID
        {
            get { return (Project != null) ? Project.ID : 0; }           
        }

        public int EntityID 
        {
            get { return (Project != null) ? Project.EntityID : 0; }           
        }

        public int ClientID
        {
            get 
            { 
                if (Project == null) { return 0; }
                return (Project.ClientID != null) ? (int) Project.ClientID : 0; 
            }     
        }

        public string ClientCode
        {
            get
            {
                if (Project == null) { return ""; }
                else if (ClientID <= 0) { return ""; }
                return ClientFunctions.GetClientByID(ClientID).ClientCode;
            }
        }

        public int ProductID
        {
            get { return (Product != null) ? Product.ID : 0; }
        }

        public string ProductName
        {
            get { return (Product != null) ? Product.ProductName : ""; }
        }

        public decimal LatestVersion
        {
            get { return (Product != null) ? (decimal)Product.LatestVersion : 0; }
        }

        public ClientProducts ClientProduct()
        {
            if (Product == null || ClientID <= 0 ) { return null; }
            return ClientFunctions.GetClientProduct(ClientID, ProductID);
        }

        public decimal ClientVersion() 
        {
            if (ClientProduct() == null) { return 0; }
            return (decimal) ClientProduct().ProductVersion;
        }

        public bool LiveProduct
        {
            get
            {
                if (ClientProduct() == null) { return false; }
                return (ClientProduct().Live == true);
            }
        }

        public ProjectStages Stage()
        {
            if (Project == null) { return null; }
            return ProjectFunctions.GetStageByID(Project.StageID);
        }

        public string ClientName()
        {
            if (Project == null) { return ""; }
            else if (ClientID <= 0) { return ""; }
            return ClientFunctions.GetClientByID(ClientID).ClientName;
        }

        
    }
}
