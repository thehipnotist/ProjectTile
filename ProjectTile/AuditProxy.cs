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
                if (TableName == clientProducts && !AType.Equals("D")) { return ClientFunctions.GetClientProductProxy(RecordID); }
                else { return null; }
            }
        }

        public ContactProxy ClientStaff
        {
            get
            {
                if (TableName == clientStaff && !AType.Equals("D")) { return ClientFunctions.GetContactProxy(RecordID); }
                else { return null; }
            }
        }

        public ClientTeamRoles ClientTeamRole
        {
            get
            {
                if (TableName == clientTeamRoles && !AType.Equals("D")) { return ProjectFunctions.GetClientRole(PrimaryValue); }
                else { return null; }
            }
        }

        public ProjectContactProxy ClientTeam
        {
            get
            {
                if (TableName == clientTeams && !AType.Equals("D")) { return ProjectFunctions.GetProjectContact(RecordID); }
                else { return null; }
            }
        }
        
        public ProjectProductProxy ProjectProduct
        {
            get
            {
                if (TableName == projectProducts && !AType.Equals("D")) { return ProjectFunctions.GetProjectProduct(RecordID); }
                else { return null; }
            }
        }

        public ProjectRoles ProjectRole
        {
            get
            {
                if (TableName == projectRoles && !AType.Equals("D")) { return ProjectFunctions.GetInternalRole(PrimaryValue); }
                else { return null; }
            }
        }

        public ProjectStages ProjectStage
        {
            get
            {
                if (TableName == projectStages && !AType.Equals("D")) { return ProjectFunctions.GetStageByCode(RecordID); }
                else { return null; }
            }
        }

        public TeamProxy ProjectTeam
        {
            get
            {
                if (TableName == projectTeams && !AType.Equals("D")) { return ProjectFunctions.GetProjectTeam(RecordID); }
                else { return null; }
            }
        }

        public ProjectTypes ProjectType
        {
            get
            {
                if (TableName == projectTypes && !AType.Equals("D")) { return ProjectFunctions.GetProjectType(PrimaryValue); }
                else { return null; }
            }
        }

        public StaffEntities StaffEntity
        {
            get
            {
                if (TableName == staffEntities && !AType.Equals("D")) { return StaffFunctions.GetStaffEntity(RecordID); }
                else { return null; }
            }
        }

        public StaffRoles StaffRole
        {
            get
            {
                if (TableName == staffRoles && !AType.Equals("D")) { return StaffFunctions.GetRole(PrimaryValue); }
                else if (TableName == tablePermissions && !AType.Equals("D")) { return StaffFunctions.GetRole(TablePermission.RoleCode); }
                else { return null; }
            }
        }

        public TablePermissions TablePermission
        {
            get
            {
                if (TableName == tablePermissions && !AType.Equals("D")) { return AdminFunctions.GetPermission(RecordID) ; }
                else { return null; }
            }
        }

        public int ClientID
        {
            get
            {
                if (!AType.Equals("D"))
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
                        case projectProducts:
                            return (Project.ClientID ?? 0);
                        default:
                            return 0;
                    }
                }
                else { return 0; }
            }
        }

        public Clients Client
        {
            get
            {
                if (!AType.Equals("D"))
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
                        case projects:
                            return (ClientID > 0) ? ClientFunctions.GetClientByID(ClientID) : null;
                        default:
                            return null;
                    }
                }
                else { return null; }
            }
        }

        public Products Product
        {
            get
            {
                if (TableName == products && !AType.Equals("D")) { return ProductFunctions.GetProductByID(RecordID); }
                else if (TableName == projectProducts && !AType.Equals("D")) { return ProjectProduct.Product; }
                else { return null; }
            }
        }

        public Projects Project
        {
            get
            {
                if (!AType.Equals("D"))
                {
                    switch (TableName)
                    {
                        case clientTeams:
                            return ClientTeam.Project;
                        case projectProducts:
                            return ProjectProduct.Project;
                        case projects:
                            return ProjectFunctions.GetProject(RecordID);
                        case projectTeams:
                            return ProjectTeam.Project;
                        default:
                            return null;
                    }
                }
                else { return null; }
            }
        }

        public Staff StaffMember
        {
            get
            {
                if (TableName == staff && !AType.Equals("D")) { return StaffFunctions.GetStaffMember(RecordID); }
                else if (TableName == staffEntities && !AType.Equals("D")) { return StaffFunctions.GetStaffMember((int)StaffEntity.StaffID); }
                else { return null; }
            }
        }

        public int EntityID
        {
            get
            {
                if (!AType.Equals("D"))
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
                        case projectProducts:
                        case projects:
                        case projectTeams:
                            return Project.EntityID;
                        case staff:
                            return (EntityFunctions.AllowedEntityIDs(StaffMember.ID).Contains(CurrentEntityID)) ? CurrentEntityID : (int) StaffMember.DefaultEntity;
                        case staffEntities:
                            return (int)StaffEntity.EntityID;
                        default:
                            return 0;
                    }
                }
                else { return 0; }
            }
        }

        public Entities Entity
        {
            get
            {
                return (EntityID > 0)? EntityFunctions.GetEntity(EntityID) : null;
            }
        }

        public string RecordDescription 
        {
            get
            {
                if (!AType.Equals("D"))
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
                            return ClientTeam.Contact.ContactName + " on " + Project.ProjectCode;
                        case entities:
                            return Entity.EntityName;
                        case products:
                            return Product.ProductName;
                        case projectProducts:
                            return Product.ProductName + " in " + Project.ProjectCode;
                        case projectRoles:
                            return ProjectRole.RoleDescription;
                        case projects:
                            return Project.ProjectCode;
                        case projectStages:
                            return ProjectStage.StageName;
                        case projectTeams:
                            return ProjectTeam.StaffMember.StaffName + " on " + Project.ProjectCode;
                        case projectTypes:
                            return ProjectType.TypeName;
                        case staff:
                            return StaffMember.FullName + ((StaffMember.UserID != "") ? " (" + StaffMember.UserID + ")" : "");
                        case staffEntities:
                            return StaffMember.FullName + " in " + Entity.EntityName;
                        case staffRoles:
                            return StaffRole.RoleDescription;
                        case tablePermissions:
                            return StaffRole.RoleDescription + " with " + TablePermission.TableName;
                        default:
                            return "";
                    }
                }
                else { return ""; }
            }
        }
        
        
    } // class
} // namespace
