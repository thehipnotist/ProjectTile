using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class AuditProxy : Globals
    {
        private const string clientProducts = "ClientProducts";
        private const string clients = "Clients";
        private const string clientStaff = "ClientStaff";
        private const string clientTeamRoles = "ClientTeamRoles";
        private const string clientTeams = "ClientTeams";
        private const string entities = "Entities";
        private const string products = "Products";
        private const string projectProducts = "ProjectProducts";
        private const string projectRoles = "ProjectRoles";
        private const string projects = "Projects";
        private const string projectStages = "ProjectStages";
        private const string projectTeams = "ProjectTeams";
        private const string projectTypes = "ProjectTypes";
        private const string staff = "Staff";
        private const string staffEntities = "StaffEntities";
        private const string staffRoles = "StaffRoles";
        private const string tablePermissions = "TablePermissions";

        public int ID { get; set; }
        public string ActionType { get; set; }
        public Staff User { get; set; }
        public string UserName { get; set; }
        public DateTime ChangeTime { get; set; }
        public string TableName { get; set; }
        public string PrimaryColumn { get; set; }
        public string PrimaryValue { get; set; }
        public string ChangeColumn { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }

        public char AType { get { return Char.Parse(ActionType.Substring(0, 1)); } }

        public int RecordID
        {
            get
            {
                int tryID = 0;
                Int32.TryParse(PrimaryValue, out tryID);
                return tryID;
            }
        }

        public ClientProductProxy ClientProduct
        {
            get
            {
                if (TableName == clientProducts) {return ClientFunctions.GetClientProductProxy(RecordID); }
                else { return null; }
            }
        }

        public ContactProxy ClientStaff
        {
            get
            {
                if (TableName == clientStaff) { return ClientFunctions.GetContactProxy(RecordID); }
                else { return null; }
            }
        }

        public ClientTeamRoles ClientTeamRole
        {
            get
            {
                if (TableName == clientTeamRoles) { return ProjectFunctions.GetClientRole(PrimaryValue); }
                else { return null; }
            }
        }

        public ProjectContactProxy ClientTeam
        {
            get
            {
                if (TableName == clientTeams) { return ProjectFunctions.GetProjectContact(RecordID); }
                else { return null; }
            }
        }
        
        public int ClientID
        {
            get
            {
                switch (TableName)
                {
                    case clientProducts:
                        return ClientProduct.ClientID;
                    case clients:                        
                        return Client.ID;
                    case clientStaff:
                        return ClientStaff.ClientID;
                    case clientTeams:
                        return ClientTeam.ClientID;
                    default: 
                        return 0;
                }
            }
        }

        public Clients Client
        {
            get
            {
                switch (TableName)
                {
                    case clientProducts:
                    case clientTeams:
                        return ClientFunctions.GetClientByID(ClientID);
                    case clients:
                        return ClientFunctions.GetClientByID(RecordID);
                    case clientStaff:
                        return ClientStaff.Client;
                    default: 
                        return null;
                }
            }
        }

        public Products Product
        {
            get
            {
                if (TableName == products) { return ProductFunctions.GetProductByID(RecordID); }
                else { return null; }
            }
        }

        public int EntityID
        {
            get
            {
                switch (TableName)
                {
                    case clientProducts:
                    case clients:
                    case clientStaff:
                    case clientTeams:
                        return Client.EntityID;
                    case entities:
                        return RecordID;
                    default:
                        return 0;
                }
            }
        }

        public Entities Entity
        {
            get
            {
                return EntityFunctions.GetEntity(EntityID);
            }
        }

        public string RecordDescription 
        {
            get
            { 
                switch (TableName)
                {
                    case clientProducts: 
                        return ClientProduct.ProductName + " for " + Client.ClientName;
                    case clients:
                        return Client.ClientName;
                    case clientStaff:
                        return ClientStaff.ContactName + " at " + Client.ClientName;
                    case clientTeamRoles:
                        return ClientTeamRole.RoleDescription;
                    case clientTeams:
                        return ClientTeam.Contact.ContactName + " on " + ClientTeam.Project.ProjectCode;
                    case entities:
                        return Entity.EntityName;
                    case products:
                        return Product.ProductName;
                    default: 
                        return ""; 
                }
            }
        }
        
        
    }
}
