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

        public string GetDeletedValue(string columnName)
        {
            return AdminFunctions.GetDeletedValue(this, columnName);
        }

        public int GetDeletedID(string columnName)
        {
            try
            {
                string foundValue = GetDeletedValue(columnName);
                if (foundValue != "")
                {
                    int foundID = 0;
                    Int32.TryParse(foundValue, out foundID);
                    return foundID;
                }
                else { return 0; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving deleted ID for column " + columnName, generalException);
                return 0;
            }
        }

        public ClientProductProxy ClientProduct
        {
            get
            {
                if (TableName == clientProducts && !AType.Equals('D')) { return ClientFunctions.GetClientProductProxy(RecordID); }
                else { return null; }
            }
        }

        public ContactProxy ClientStaff
        {
            get
            {
                if (TableName == clientStaff && !AType.Equals('D')) { return ClientFunctions.GetContactProxy(RecordID); }
                else if (TableName == clientTeams)
                {
                    if (!AType.Equals('D') && ClientTeam != null) { return ClientTeam.Contact; }
                    else
                    {
                        int clientStaffID = GetDeletedID("ClientStaffID");
                        return (clientStaffID <= 0) ? null : ClientFunctions.GetContactProxy(clientStaffID);
                    }
                }
                else { return null; }
            }
        }

        public ClientTeamRoles ClientTeamRole
        {
            get
            {
                if (TableName == clientTeamRoles && !AType.Equals('D')) { return ProjectFunctions.GetClientRole(PrimaryValue); }
                else { return null; }
            }
        }

        public ProjectContactProxy ClientTeam
        {
            get
            {
                if (TableName == clientTeams && !AType.Equals('D')) { return ProjectFunctions.GetProjectContact(RecordID); }
                else { return null; }
            }
        }
        
        public ProjectProductProxy ProjectProduct
        {
            get
            {
                if (TableName == projectProducts && !AType.Equals('D')) { return ProjectFunctions.GetProjectProduct(RecordID); }
                else { return null; }
            }
        }

        public ProjectRoles ProjectRole
        {
            get
            {
                if (TableName == projectRoles && !AType.Equals('D')) { return ProjectFunctions.GetInternalRole(PrimaryValue); }
                else { return null; }
            }
        }

        public ProjectStages ProjectStage
        {
            get
            {
                if (TableName == projectStages && !AType.Equals('D')) { return ProjectFunctions.GetStageByID(RecordID); }
                else { return null; }
            }
        }

        public TeamProxy ProjectTeam
        {
            get
            {
                if (TableName == projectTeams && !AType.Equals('D')) { return ProjectFunctions.GetProjectTeam(RecordID); }
                else { return null; }
            }
        }

        public ProjectTypes ProjectType
        {
            get
            {
                if (TableName == projectTypes && !AType.Equals('D')) { return ProjectFunctions.GetProjectType(PrimaryValue); }
                else { return null; }
            }
        }

        public StaffEntities StaffEntity
        {
            get
            {
                if (TableName == staffEntities && !AType.Equals('D')) { return StaffFunctions.GetStaffEntity(RecordID); }
                else { return null; }
            }
        }

        public StaffRoles StaffRole
        {
            get
            {
                try
                {
                    if (TableName == staffRoles && !AType.Equals('D')) { return StaffFunctions.GetRole(PrimaryValue); }
                    else if (TableName == tablePermissions)
                    {
                        if (!AType.Equals('D') && TablePermission != null) { return StaffFunctions.GetRole(TablePermission.RoleCode); }
                        else
                        {
                            string roleCode = GetDeletedValue("RoleCode");
                            return (roleCode == "") ? null : StaffFunctions.GetRole(roleCode);
                        }
                    }
                    else { return null; }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving staff role details", generalException);
                    return null;
                }
            }
        }

        public TablePermissions TablePermission
        {
            get
            {
                if (TableName == tablePermissions && !AType.Equals('D')) { return AdminFunctions.GetPermission(RecordID) ; }
                else { return null; }
            }
        }

        public int ClientID
        {
            get
            {
                try
                {
                    if (!AType.Equals('D'))
                    {
                        switch (TableName)
                        {
                            case clientProducts:
                                return (ClientProduct == null)? 0 : ClientProduct.ClientID;
                            case clients:
                                return Client.ID;
                            case clientStaff:
                                return (ClientStaff == null) ? 0 : ClientStaff.ClientID;
                            case clientTeams:
                                return (ClientTeam == null) ? 0 : ClientTeam.ClientID;
                            case projectProducts:
                                return (Project == null) ? 0 : Project.ClientID ?? 0;
                            default:
                                return 0;
                        }
                    }
                    else { return GetDeletedID("ClientID"); }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving Client ID for audit record", generalException);
                    return 0;
                }		
            }
        }

        public Clients Client
        {
            get
            {
                try
                {
                    switch (TableName)
                    {
                        case clientProducts:
                        case clientTeams:
                        case projects:
                            return (ClientID > 0) ? ClientFunctions.GetClientByID(ClientID) : null;
                        case clients:
                            return (!AType.Equals('D'))? ClientFunctions.GetClientByID(RecordID) : null;
                        case clientStaff:
                            return (ClientStaff != null)? ClientStaff.Client : null;
                        default:
                            return null;
                    }
                }
                catch { return null; }
            }
        }

        public Products Product
        {
            get
            {
                try
                {
                    if (TableName == products && !AType.Equals('D')) { return ProductFunctions.GetProductByID(RecordID); }
                    else if (TableName == clientProducts)
                    {
                        if (!AType.Equals('D') && ClientProduct != null) { return ProductFunctions.GetProductByID(ClientProduct.ProductID); }
                        else
                        {
                            int productID = GetDeletedID("ProductID");
                            return (productID > 0) ? ProductFunctions.GetProductByID(productID) : null;
                        }
                    }
                    else if (TableName == projectProducts)
                    {
                        if (!AType.Equals('D') && ProjectProduct != null) { return ProjectProduct.Product; }
                        else
                        {
                            int productID = GetDeletedID("ProductID");
                            return (productID > 0) ? ProductFunctions.GetProductByID(productID) : null;
                        }
                    }
                    else { return null; }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving product details for audit record", generalException);
                    return null;
                }		
            }
        }

        public Projects Project
        {
            get
            {
                try
                { 
                    if (!AType.Equals('D'))
                    {
                        switch (TableName)
                        {
                            case clientTeams:
                                return (ClientTeam == null) ? null : ClientTeam.Project;
                            case projectProducts:
                                return (ProjectProduct == null) ? null : ProjectProduct.Project;
                            case projects:
                                return ProjectFunctions.GetProject(RecordID);
                            case projectTeams:
                                return (ProjectTeam == null) ? null : ProjectTeam.Project;
                            default:
                                return null;
                        }
                    }
                    else
                    {
                        int projectID = GetDeletedID("ProjectID");
                        return (projectID > 0) ? ProjectFunctions.GetProject(projectID) : null;
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving project details for audit record", generalException);
                    return null;
                }	
            }
        }

        public Staff StaffMember
        {
            get
            {
                try
                {
                    if (TableName == staff && !AType.Equals('D')) { return StaffFunctions.GetStaffMember(RecordID); }
                    else if (TableName == staffEntities)
                    {
                        if (!AType.Equals('D') && StaffEntity != null) { return StaffFunctions.GetStaffMember((int)StaffEntity.StaffID); }
                        else
                        {
                            int staffID = GetDeletedID("StaffID");
                            return (staffID > 0) ? StaffFunctions.GetStaffMember(staffID) : null;
                        }
                    }
                    else if (TableName == projectTeams)
                    {
                        if (!AType.Equals('D') && ProjectTeam != null) { return StaffFunctions.GetStaffMember((int)ProjectTeam.StaffID); }
                        else
                        {
                            int staffID = GetDeletedID("StaffID");
                            return (staffID > 0) ? StaffFunctions.GetStaffMember(staffID) : null;
                        }
                    }
                    else { return null; }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving staff details for audit record", generalException);
                    return null;
                }	
            }
        }

        public int EntityID
        {
            get
            {
                try
                {
                    if (!AType.Equals('D'))
                    {
                        switch (TableName)
                        {
                            case clientProducts:
                            case clients:
                            case clientStaff:
                            case clientTeams:
                                return (Client == null) ? NoID : Client.EntityID;
                            case entities:
                                return RecordID;
                            case projectProducts:
                            case projects:
                            case projectTeams:
                                return (Project == null) ? NoID : Project.EntityID;
                            case staff:
                                if (StaffMember == null) { return NoID; }
                                else { return (EntityFunctions.AllowedEntityIDs(StaffMember.ID).Contains(CurrentEntityID)) ? CurrentEntityID : (int)StaffMember.DefaultEntity; }
                            case staffEntities:
                                return (StaffEntity == null) ? NoID : (int)StaffEntity.EntityID;
                            default:
                                return 0;
                        }
                    }
                    else { return GetDeletedID("EntityID"); }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving Entity ID for audit record", generalException);
                    return 0;
                }	
            }
        }

        public Entities Entity
        {
            get { return (EntityID > 0)? EntityFunctions.GetEntity(EntityID) : null; }
        }

        public string RecordDescription 
        {
            get
            {
                try
                {
                    switch (TableName)
                    {
                        case clientProducts:
                            return Product.ProductName + " for " + Client.ClientName;
                        case clients:
                            return (AType.Equals('D'))? GetDeletedValue("ClientName") : Client.ClientName;
                        case clientStaff:
                            return ((AType.Equals('D')) ? GetDeletedValue("FirstName") + " " + GetDeletedValue("Surname") : ClientStaff.ContactName) + " at " + Client.ClientName;
                        case clientTeamRoles:
                            return (AType.Equals('D')) ? GetDeletedValue("RoleDescription") : ClientTeamRole.RoleDescription;
                        case clientTeams:
                            return ClientStaff.ContactName + " on " + Project.ProjectCode;
                        case entities:
                            return (AType.Equals('D')) ? GetDeletedValue("EntityName") : Entity.EntityName;
                        case products:
                            return (AType.Equals('D')) ? GetDeletedValue("ProductName") : Product.ProductName;
                        case projectProducts:
                            return Product.ProductName + " in " + Project.ProjectCode;
                        case projectRoles:
                            return (AType.Equals('D')) ? GetDeletedValue("RoleDescription") : ProjectRole.RoleDescription;
                        case projects:
                            return (AType.Equals('D')) ? GetDeletedValue("ProjectCode") : Project.ProjectCode;
                        case projectStages:
                            return (AType.Equals('D')) ? GetDeletedValue("StageName") : ProjectStage.StageName;
                        case projectTeams:
                            return StaffMember.FullName + " on " + Project.ProjectCode;
                        case projectTypes:
                            return (AType.Equals('D')) ? GetDeletedValue("TypeName") : ProjectType.TypeName;
                        case staff:
                            string fullName = (AType.Equals('D')) ? GetDeletedValue("FullName") : StaffMember.FullName;
                            string userID = (AType.Equals('D')) ? GetDeletedValue("UserID") : StaffMember.UserID;
                            return fullName + ((userID != "") ? " (" + userID + ")" : "");
                        case staffEntities:
                            return StaffMember.FullName + " in " + Entity.EntityName;
                        case staffRoles:
                            return (AType.Equals('D')) ? GetDeletedValue("RoleDescription") : StaffRole.RoleDescription;
                        case tablePermissions:
                            return StaffRole.RoleDescription + " with " + ((AType.Equals('D')) ? GetDeletedValue("TableName") : TablePermission.TableName);
                        default:
                            return "";
                    }
                }
                catch // (Exception generalException)
                {
                    //MessageFunctions.Error("Error retrieving description for audit record", generalException);
                    return "";
                }	
            }
        }
        
        
    } // class
} // namespace
