using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace ProjectTile
{
    public class StaffFunctions : Globals
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //
        
        public static int newDefaultID = 0;
        public static StaffSummaryRecord SelectedTeamStaff = null;
        public delegate void ReturnToTeamsDelegate();
        public static ReturnToTeamsDelegate SelectStaffForTeam;
        public static ReturnToTeamsDelegate CancelTeamStaffSelection;

        // ------------------ Lists ----------------- //

        public static List<StaffSummarySmall> StaffForEntity; // Initiated later
        public static List<StaffSummarySmall> StaffNotForEntity; // Initiated later
        public static List<int> StaffIDsToAdd = new List<int>();
        public static List<int> StaffIDsToRemove = new List<int>();
        public static List<int> StaffDefaultsToSet = new List<int>();

        public static List<EntitySummaryRecord> EntitiesForStaff; // Initiated later
        public static List<EntitySummaryRecord> EntitiesNotForStaff; // Initiated later
        public static List<int> EntityIDsToAdd = new List<int>();
        public static List<int> EntityIDsToRemove = new List<int>();
        public static List<int> EntityDefaultsToSet = new List<int>();

        // --------------- Navigation --------------- // 	

        public static void SelectTeamStaff(StaffSummaryRecord selectedRecord)
        {
            try
            {
                SelectedTeamStaff = selectedRecord;
                SelectStaffForTeam();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error handling staff member selection", generalException); }
        }

        public static void BackToTeam()
        {
            CancelTeamStaffSelection();
        }

        // Get staff data
        
        public static List<StaffSummaryRecord> GetStaffGridData(bool activeOnly, string nameContains, string roleDescription, int entityID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    // Only get Entities that the current user can access; if entity is specified, check it is valid and replace the array with its ID
                    int[] myAllowedEntities = EntityFunctions.AllowedEntityIDs(MyStaffID);
                    if (entityID > 0)
                    {
                        if (!myAllowedEntities.Contains(entityID))
                        {
                            MessageFunctions.Error("Error retrieving staff grid data: you do not have access to the specified Entity.", null);
                            return null; 
                        }
                        else { myAllowedEntities = new int[] {entityID}; }
                    }

                    // Set the contents based on all of the filters
                    List<StaffSummaryRecord> gridList = (from s in existingPtDb.Staff
                               join sr in existingPtDb.StaffRoles on s.RoleCode equals sr.RoleCode
                               join se in existingPtDb.StaffEntities on s.ID equals se.StaffID
                               where (!activeOnly || s.Active)
                                    && (nameContains == "" || (s.FirstName + " " + s.Surname).Contains(nameContains))
                                    && (myAllowedEntities.Contains((int) se.EntityID))
                                    && (roleDescription == AllRecords || roleDescription == "" || sr.RoleDescription == roleDescription)
                               orderby new { s.FirstName, s.Surname, s.UserID }
                               select (new StaffSummaryRecord()
                               {
                                   ID = (int)s.ID,
                                   EmployeeID = s.EmployeeID,
                                   FirstName = s.FirstName,
                                   Surname = s.Surname,
                                   RoleCode = s.RoleCode,
                                   StartDate = (DateTime?)DbFunctions.TruncateTime(s.StartDate),
                                   LeaveDate = (DateTime?)DbFunctions.TruncateTime(s.LeaveDate),
                                   UserID = s.UserID,
                                   Active = (bool)s.Active,
                                   DefaultEntity = (int) s.DefaultEntity
                               } )
                               ).Distinct().ToList();

                    return gridList;
                }
                catch (Exception generalException) 
                { 
                    MessageFunctions.Error("Error retrieving staff grid data", generalException);
                    return null;                
                }
            }
        }

        public static List<Staff> GetStaffComboData(bool activeOnly, int entityID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                { 
                    // Only get Entities that the current user can access; if entity is specified, check it is valid and replace the array with its ID
                    int[] myAllowedEntities = EntityFunctions.AllowedEntityIDs(MyStaffID);
                    if (entityID > 0)
                    {
                        if (!myAllowedEntities.Contains(entityID))
                        {
                            MessageFunctions.Error("Error retrieving staff grid data: the specified Entity is not allowed for this user.", null);
                            return null;
                        }
                        else { myAllowedEntities = new int[] { entityID }; }
                    }

                    List<Staff> comboList = (from s in existingPtDb.Staff
                                join se in existingPtDb.StaffEntities on s.ID equals se.StaffID
                                where (!activeOnly || s.Active)                                     
                                     && (myAllowedEntities.Contains((int)se.EntityID))
                                orderby new { s.FirstName, s.Surname, s.UserID }
                                select s
                               ).Distinct().ToList();

                    return comboList;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving staff drop-down list data", generalException);
                    return null;
                }
            }
        }

        // Get individual staff data

        public static Staff GetStaffMember(int staffID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    return existingPtDb.Staff.Find(staffID);
                }
                catch (Exception generalException) 
                { 
                    MessageFunctions.Error("Error retrieving database record", generalException);
                    return null;
                }
            }
        }

        public static string GetStaffName(int staffID)
        {
            Staff thisPerson = GetStaffMember(staffID);
            if (thisPerson == null) { return ""; } // Errors will be thrown by GetStaffMember
            else { return thisPerson.FirstName + " " + thisPerson.Surname; }
        }

        public static StaffSummaryRecord GetStaffSummary(int staffID, int entityID = 0)
        {
            try
            {
                if (entityID == 0) { entityID = CurrentEntityID; }
                
                List<StaffSummaryRecord> allStaffinEntity = StaffFunctions.GetStaffGridData(activeOnly: true, nameContains: "", roleDescription: AllRecords, entityID: CurrentEntityID);
                if (allStaffinEntity.Exists(ase => ase.ID == staffID)) { return allStaffinEntity.First(ase => ase.ID == staffID); }
                else
                {
                    MessageFunctions.Error("Error retrieving summary data for staff member with ID " + staffID.ToString() + ": no matching record found.", null);
                    return null;
                }
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error retrieving summary data for staff member with ID " + staffID.ToString(), generalException);
                return null;
            }
        }

        public static Staff GetStaffMemberByName(string staffName)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    Staff foundStaff = existingPtDb.Staff.FirstOrDefault(s => s.FirstName + " " + s.Surname == staffName);
                    if (foundStaff == null)
                    {
                        MessageFunctions.Error("Error retrieving staff member " + staffName + ": no matching record found.", null);
                        return null;
                    }
                    else { return foundStaff; }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving database record", generalException);
                    return null;
                }
            }
        }

        public static string GetSelectedName()
        {            
            if (SelectedStaffMember != null)
            {
                return SelectedStaffMember.FirstName + " " + SelectedStaffMember.Surname;
            }
            else
            {
                MessageFunctions.Error("Error retrieving staff name: no staff member selected.", null);
                return "";
            }
        }

        // Changes

        public static bool? EnableOrDisable(int staffID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    SelectedStaffMember = GetStaffMember(staffID);
                    string errorMessage = "";

                    if (SelectedStaffMember == null) { return null; }
                    else if (MyStaffID == staffID)
                    {
                        errorMessage = "You cannot amend your own user account.|Changes Prohibited";
                    }
                    else if (!SelectedStaffMember.Active)
                    {
                        if (SelectedStaffMember.StartDate > (DateTime)System.DateTime.Today)
                        {
                            errorMessage = "You cannot activate a user before their start date. Please use the 'Amend' option to change their start date if it is incorrect.|Not Current User";
                        }
                        else if (SelectedStaffMember.LeaveDate < (DateTime)System.DateTime.Today)
                        {
                            errorMessage = "This user has left. Please use the 'Amend' option to change or remove their leave date if it is incorrect.|Not Current User";
                        }
                        else if (SelectedStaffMember.UserID == "")
                        {
                            errorMessage = "Only staff with a UserID can be activated. Please use the 'Amend' option to create a UserID if appropriate.|No Login Details";
                        }
                    }

                    if (errorMessage != "")
                    {
                        MessageFunctions.SplitInvalid(errorMessage);
                        return null;
                    }
                    else
                    {
                        string changeName = SelectedStaffMember.Active ? "Disable" : "Enable";
                        string changeAction = SelectedStaffMember.Active ? "disabling" : "enabling";

                        bool confirm = MessageFunctions.ConfirmOKCancel(
                                changeName + " " + SelectedStaffMember.FirstName + " " + SelectedStaffMember.Surname + "'s account? This will take effect immediately.",
                                changeName + " user?");
                        if (!confirm)
                        {
                            return null;
                        }
                        else
                        {
                            try
                            {
                                bool blnNewStatus = !SelectedStaffMember.Active;
                                SelectedStaffMember = existingPtDb.Staff.Find(staffID); // Required as so far we are using a copy
                                SelectedStaffMember.Active = blnNewStatus;
                                existingPtDb.SaveChanges();

                                return SelectedStaffMember.Active;
                            }
                            catch (Exception generalException)
                            {
                                MessageFunctions.Error("Error " + changeAction + " user", generalException);
                                return null;
                            }
                        }
                    }
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error changing status", generalException);
                return null;
            }
        }

        public static void OpenAmendPage(int staffID)
        {
            if (staffID == 0)
            {
                MessageFunctions.InvalidMessage("Please select a staff member in the list above.", "No Record Selected");
            }
            else if (MyStaffID == staffID)
            {
                MessageFunctions.InvalidMessage("You cannot amend your own user account.", "Changes Prohibited");
            }            
            else
            {
                PageFunctions.ShowStaffDetailsPage(PageFunctions.Amend, staffID);
            }
        }

        public static int SaveStaffDetails(int staffID, string firstName, string surname, string roleDesc, DateTime? start, DateTime? leave, string userID, string passwd, bool active, string defaultEnt)
        {
            string errorMessage = "";
            bool addEntity = false;

            if (firstName == "") { errorMessage = "Please provide the staff member's first name.|First Name Blank"; }
            else if (surname == "") { errorMessage = "Please provide the staff member's surname.|Surname Blank"; }
            else if (active && userID == "") { errorMessage = "Active users must have a UserID. Please check the details|No UserID Provided"; }
            else if (active && start > (DateTime)System.DateTime.Today) { errorMessage = "Users cannot be active before their start date. Please check the details.|User Not Started"; }
            else if (active && leave < (DateTime)System.DateTime.Today) { errorMessage = "Users cannot be active after their leave date. Please check the details.|User Has Left"; }
            else if (staffID == 0 && userID != "" && passwd == "") { errorMessage = "Please provide a password for the new user.|No Password Entered"; }
            else if (passwd != "" && !LoginFunctions.PasswordComplexityOK(userID, passwd)) { return 0; } // PasswordComplexity will throw its own error
            else if (userID == "" && passwd != "") { errorMessage = "Passwords cannot be set without a UserID.|No UserID Provided"; }
            else if (start == null) { errorMessage = "Please enter the date the user started working.|Start Date Blank"; }
            else if (leave < start) { errorMessage = "The user's start date cannot be after their leave date.|Invalid Date Combination"; } 
            else if (defaultEnt == "") { errorMessage = "Please select the staff member's default Entity from the drop-down list. Ask your system administrator if unsure.|No Entity Selected"; }
            else if (roleDesc == "") { errorMessage = "Please select the staff member's role from the drop-down list. Ask your system administrator if unsure what to choose.|No Role Selected"; }

            if (errorMessage != "")
            {
                MessageFunctions.SplitInvalid(errorMessage);
                return 0;
            }
            else
            {
                try
                {
                    ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                    using (existingPtDb)
                    {
                        string fullName = firstName + " " + surname;
                        int defaultEntityID = EntityFunctions.GetEntityByName(defaultEnt).ID;
                        string roleCode = GetRoleByDescription(roleDesc);
                        
                        Staff checkNewName = existingPtDb.Staff.FirstOrDefault(s => s.FirstName + " " + s.Surname == fullName && s.ID != staffID);
                        if (checkNewName != null)
                        {
                            MessageFunctions.InvalidMessage("A different staff member with name '" + fullName + 
                                "' already exists. Please try a different name (e.g. add a middle initial) to avoid confusion.", "Duplicate Name");
                            return 0;
                        }
                        
                        if (staffID == 0) // New user
                        {
                            Staff newStaff;
 
                            try
                            {
                                newStaff = new Staff();
                                newStaff.FirstName = firstName;
                                newStaff.Surname = surname;
                                newStaff.RoleCode = roleCode;
                                newStaff.StartDate = (DateTime)start;
                                newStaff.LeaveDate = leave;
                                newStaff.DefaultEntity = defaultEntityID;
                                newStaff.Active = active;
                            }
                            catch (Exception generalException)
                            {
                                MessageFunctions.Error("Error applying changes", generalException);
                                return 0;
                            }

                            try
                            {
                                existingPtDb.Staff.Add(newStaff);
                                existingPtDb.SaveChanges();
                                staffID = newStaff.ID;
                                EntityFunctions.AllowEntity(defaultEntityID, staffID);
                            }
                            catch (Exception generalException)
                            {
                                MessageFunctions.Error("Error applying changes", generalException);
                                return 0;
                            }

                            if (userID != "")
                            {
                                try
                                {
                                    bool success = LoginFunctions.ChangeLoginDetails(staffID, userID, passwd, passwd);  // Must be the default user to change UserID or password
                                    if (!success) { return 0; }
                                }
                                catch (Exception generalException)
                                {
                                    MessageFunctions.Error("Error creating new login", generalException);
                                    return 0;
                                }
                            }
                            
                            return staffID;
                        }
                        else // Amend existing user
                        {
                            Staff selectedStaff = existingPtDb.Staff.Find(staffID);
                            try
                            {
                                if (!selectedStaff.DefaultEntity.Equals(defaultEntityID))
                                {
                                    int[] allowedEntities = EntityFunctions.AllowedEntityIDs(staffID);
                                    if (!allowedEntities.Contains(defaultEntityID))
                                    {
                                        addEntity = MessageFunctions.WarningYesNo("This staff member does not currently have access to " + defaultEnt + ". Is this correct?", "Allow new Entity?");
                                        if (addEntity) { EntityFunctions.AllowEntity(defaultEntityID, staffID); }
                                        else { return 0; }
                                    }
                                    selectedStaff.DefaultEntity = defaultEntityID;
                                }
                            }
                            catch (Exception generalException)
                            {
                                MessageFunctions.Error("Error processing new default Entity", generalException);
                                return 0;
                            }

                            try
                            {
                                if (selectedStaff.FirstName != firstName) { selectedStaff.FirstName = firstName; }
                                if (selectedStaff.Surname != surname) { selectedStaff.Surname = surname; }
                                if (selectedStaff.RoleCode != roleCode) { selectedStaff.RoleCode = roleCode; }
                                if (selectedStaff.StartDate != (DateTime)start) { selectedStaff.StartDate = (DateTime)start; }
                                if (selectedStaff.LeaveDate != leave) { selectedStaff.LeaveDate = leave; }
                                if (selectedStaff.Active != active) { selectedStaff.Active = active; }
                            }
                            catch (Exception generalException)
                            {
                                MessageFunctions.Error("Error applying changes", generalException);
                                return 0;
                            }

                            try
                            {                                
                                if (userID != selectedStaff.UserID || passwd != "")
                                {
                                    if (passwd == "")
                                    {
                                        MessageFunctions.InvalidMessage("You must set a password when creating a new UserID.", "No Password Entered");
                                        return 0;
                                    }
                                    else
                                    {
                                        bool success = LoginFunctions.ChangeLoginDetails(staffID, userID, passwd, passwd);  // Must be the default user to change UserID or password
                                        if (!success) { return 0; }
                                    }
                                }
                                existingPtDb.SaveChanges();
                                return staffID;
                            }
                            catch (SqlException sqlException)
                            {
                                MessageFunctions.Error("SQL error saving changes to the database", sqlException);
                                return 0;
                            }
                            catch (Exception generalException)
                            {
                                MessageFunctions.Error("Error saving changes to the database", generalException);
                                return 0;
                            }
                        }
                    }
                }
                catch (SqlException sqlException)
                {
                    MessageFunctions.Error("SQL error saving changes", sqlException);
                    return 0;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving changes", generalException);
                    return 0;
                }
            }
        }

        // Entity-related functions
        
        public static bool CanAddStaffEntity(ref Staff thisPerson)
        {
            try
            {
                if (thisPerson.LeaveDate < System.DateTime.Today)
                {
                    MessageFunctions.InvalidMessage(thisPerson.FirstName + " " + thisPerson.Surname + " has left, and cannot be added to additional Entities.", "Not Current User");
                    return false;
                }
                else { return true; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking whether staff can be added", generalException);
                return false;
            }
        }

        public static bool CanRemoveStaffEntity(ref Staff thisPerson, int entityID)
        {
            // Don't allow if the Entity is the user's default, or the user has active projects in that Entity 
            try
            {
                if ((newDefaultID > 0 && newDefaultID == entityID) || (newDefaultID == 0 && thisPerson.DefaultEntity == entityID))
                {
                    MessageFunctions.InvalidMessage("Cannot remove " + thisPerson.FirstName + " " + thisPerson.Surname + " from their default Entity.", "Default Entity Required");
                    return false;
                }
                else
                {
                    ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                    using (existingPtDb)
                    {
                        try
                        {
                            int staffID = thisPerson.ID;
                            int openProjects = (from p in existingPtDb.Projects
                                                join pt in existingPtDb.ProjectTeams on p.ID equals pt.ProjectID
                                                where pt.StaffID == staffID && p.EntityID == entityID && p.ProjectStages.ProjectStatus != "Closed"
                                                select p.ID)
                                                .FirstOrDefault();

                            if (openProjects > 0)
                            {
                                MessageFunctions.InvalidMessage("Cannot remove " + thisPerson.FirstName + " " + thisPerson.Surname + " as they have open projects in this Entity.", "Open Projects Found");
                                return false;
                            }
                            else { return true; }
                        }
                        catch (Exception generalException) 
                        { 
                            MessageFunctions.Error("Error checking for open projects in this Entity", generalException);
                            return false;
                        }
                    }
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking whether staff can be removed", generalException);
                return false;
            }            
        }
       
        public static int[] EntityStaffIDs(bool activeOnly, int entityID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    return (from s in existingPtDb.Staff
                                   join se in existingPtDb.StaffEntities on s.ID equals se.StaffID
                                   where (!activeOnly || s.Active) && (se.EntityID == entityID)
                                   //orderby new { s.FirstName, s.Surname, s.UserID }
                                   select (int)s.ID
                               ).Distinct().ToArray();
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving list of staff IDs in Entity " + entityID.ToString() + "", generalException);
                    return null;
                }
            }
        }

        public static List<StaffSummarySmall> StaffInEntity(bool activeOnly, int entityID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    var includeList = EntityStaffIDs(activeOnly, entityID);
                    var listResults = new List<StaffSummarySmall>();

                    listResults = (from s in existingPtDb.Staff
                                join de in existingPtDb.Entities on s.DefaultEntity equals de.ID
                                where (!activeOnly || s.Active) && includeList.Contains(s.ID)
                                orderby new { s.FirstName, s.Surname, s.UserID }
                                select (new StaffSummarySmall()
                                {
                                    ID = (int)s.ID,
                                    NameAndUser = (string)s.FirstName + " " + s.Surname + (s.UserID == null ? "" : " (" + s.UserID + ")"),
                                    Status = (DbFunctions.TruncateTime(s.StartDate) > System.DateTime.Today) ? "Not started" :
                                            (DbFunctions.TruncateTime(s.LeaveDate) < System.DateTime.Today) ? "Left" :
                                            ((bool)s.Active) ? "Active" : 
                                            "Disabled",
                                    DefaultEntity = (string)de.EntityName
                                })
                               ).Distinct().ToList();

                    return listResults;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving summary data of staff in Entity " + entityID.ToString() + "", generalException);
                    return null;
                }
            }
        }

        public static List<StaffSummarySmall> StaffNotInEntity(bool activeOnly, int entityID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    var avoidList = EntityStaffIDs(activeOnly, entityID);
                    var listResults = new List<StaffSummarySmall>();

                    listResults = (from s in existingPtDb.Staff
                                   join de in existingPtDb.Entities on s.DefaultEntity equals de.ID
                                   where (!activeOnly || s.Active) && !avoidList.Contains(s.ID)
                                   orderby new { s.FirstName, s.Surname, s.UserID }
                                   select (new StaffSummarySmall()
                                   {
                                       ID = (int)s.ID,
                                       NameAndUser = (string)s.FirstName + " " + s.Surname + (s.UserID == null ? "" : " (" + s.UserID + ")"),
                                       Status = (DbFunctions.TruncateTime(s.StartDate) > System.DateTime.Today) ? "Not started" :
                                               (DbFunctions.TruncateTime(s.LeaveDate) < System.DateTime.Today) ? "Left" :
                                               ((bool)s.Active) ? "Active" :
                                               "Disabled",
                                       DefaultEntity = (string)de.EntityName
                                   })
                               ).Distinct().ToList();

                    return listResults;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving summary data of staff not in Entity " + entityID.ToString() + "", generalException);
                    return null;
                }
            }
        }

        public static int[] AllowedStaffEntityIDs(int staffID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    var myAllowedEntities = EntityFunctions.AllowedEntityIDs(MyStaffID);                    
                    return (from se in existingPtDb.StaffEntities
                            where se.StaffID == staffID && myAllowedEntities.Contains((int) se.EntityID)
                            select (int)se.EntityID
                            ).Distinct().ToArray();
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving list of Entities for " + staffID.ToString() + "", generalException);
                    return null;
                }
            }
        }
        
        public static List<EntitySummaryRecord> AllowedLinkedEntities (int staffID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    var includeList = AllowedStaffEntityIDs(staffID);
                    int defaultEntity = (int)GetStaffMember(staffID).DefaultEntity;

                    List<EntitySummaryRecord> entitiesList = (from e in existingPtDb.Entities
                            where includeList.Contains(e.ID)
                            //orderby new { Default = (e.ID == defaultEntity), e.EntityName }
                            select (new EntitySummaryRecord()
                            {                             
                                ID = (int)e.ID,
                                Name = e.EntityName,
                                Description = e.EntityDescription,
                                Default = (e.ID == defaultEntity)  
                            })
                            ).Distinct().ToList();
                    return entitiesList;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving summary data of Entities linked to staff member " + staffID.ToString() + "", generalException);
                    return null;
                }
            }
        }

        public static List<EntitySummaryRecord> AllowedUnlinkedEntities(int staffID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    var avoidList = AllowedStaffEntityIDs(staffID);
                    int defaultEntity = (int)GetStaffMember(staffID).DefaultEntity;

                    List<EntitySummaryRecord> entitiesList = (from e in existingPtDb.Entities
                            where !avoidList.Contains(e.ID)
                            //orderby new { Default = (e.ID == defaultEntity), e.EntityName }
                            select (new EntitySummaryRecord()
                            {
                                ID = (int)e.ID,
                                Name = e.EntityName,
                                Description = e.EntityDescription,
                                Default = false
                            })
                            ).Distinct().ToList();
                    return entitiesList;

                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error retrieving summary data of Entities linked to staff member " + staffID.ToString() + "", generalException);
                    return null;
                }
            }
        }     
        
        public static bool ToggleEntityStaff(List<StaffSummarySmall> affectedStaff, bool addition, Entities thisEntity)
        {
            try
            {
                int entityID = thisEntity.ID;
                string sqlName = thisEntity.EntityName;
                
                foreach (StaffSummarySmall thisRecord in affectedStaff)
                {
                    int selectedFromID = thisRecord.ID;
                    Staff thisPerson = GetStaffMember(selectedFromID);
                    bool canChange = addition ? CanAddStaffEntity(ref thisPerson) : CanRemoveStaffEntity(ref thisPerson, entityID);

                    if (!canChange) { return false; }
                    else if (addition)
                    {
                        try
                        {
                            StaffForEntity.Add(thisRecord);
                            StaffNotForEntity.Remove(thisRecord);

                            if (StaffIDsToRemove.Contains(selectedFromID))
                            {
                                StaffIDsToRemove.Remove(selectedFromID);
                            }
                            else
                            {
                                StaffIDsToAdd.Add(selectedFromID);
                            }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error adding " + thisPerson.FirstName + " " + thisPerson.Surname + " to entity " + sqlName + "", generalException);
                            return false;
                        }
                    }
                    else
                    {
                        try
                        {
                            StaffNotForEntity.Add(thisRecord);
                            StaffForEntity.Remove(thisRecord);

                            if (StaffIDsToAdd.Contains(selectedFromID))
                            {
                                StaffIDsToAdd.Remove(selectedFromID);
                            }
                            else
                            {
                                StaffIDsToRemove.Add(selectedFromID);
                            }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error removing " + thisPerson.FirstName + " " + thisPerson.Surname + " from entity " + sqlName + "", generalException);
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception generalException)
            {
                string change = addition ? "addition" : "removal";
                MessageFunctions.Error("Error processing " + change + "", generalException);
                return false;
            }
        }

        public static bool ToggleStaffEntities(List<EntitySummaryRecord> affectedEntities, bool addition, Staff thisPerson)
        {
            try
            {
                int staffID = thisPerson.ID;
                string staffName = thisPerson.FirstName + " " + thisPerson.Surname;

                foreach (EntitySummaryRecord thisRecord in affectedEntities)
                {
                    int selectedFromID = thisRecord.ID;
                    Entities thisEntity = EntityFunctions.GetEntity(selectedFromID);
                    bool canChange = addition ? CanAddStaffEntity(ref thisPerson) : CanRemoveStaffEntity(ref thisPerson, selectedFromID);

                    if (!canChange) { return false; }
                    else if (addition)
                    {
                        try
                        {
                            EntitiesForStaff.Add(thisRecord);
                            EntitiesNotForStaff.Remove(thisRecord);

                            if (EntityIDsToRemove.Contains(selectedFromID))
                            {
                                EntityIDsToRemove.Remove(selectedFromID);
                            }
                            else
                            {
                                EntityIDsToAdd.Add(selectedFromID);
                            }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error adding " + staffName + " to entity " + thisEntity.EntityName + "", generalException);
                            return false;
                        }
                    }
                    else
                    {
                        try
                        {
                            EntitiesNotForStaff.Add(thisRecord);
                            EntitiesForStaff.Remove(thisRecord);

                            if (EntityIDsToAdd.Contains(selectedFromID))
                            {
                                EntityIDsToAdd.Remove(selectedFromID);
                            }
                            else
                            {
                                EntityIDsToRemove.Add(selectedFromID);
                            }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error removing " + staffName + " from entity " + thisEntity.EntityName + "", generalException);
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception generalException)
            {
                string change = addition ? "addition" : "removal";
                MessageFunctions.Error("Error processing " + change + "", generalException);
                return false;
            }
        }

        public static bool MakeDefault(List<StaffSummarySmall> affectedStaff, Entities thisEntity)
        {
            try
            {
                foreach (StaffSummarySmall thisRecord in affectedStaff)
                {
                    int selectedStaffID = thisRecord.ID;
                    Staff thisPerson = GetStaffMember(selectedStaffID);

                    if (thisPerson.LeaveDate < System.DateTime.Today)
                    {
                        MessageFunctions.InvalidMessage(thisPerson.FirstName + " " + thisPerson.Surname + " has left, so their default Entity cannot be changed.", "Not Current User");
                        return false;
                    }

                    StaffDefaultsToSet.Add(thisPerson.ID);

                    int displayIndex = StaffForEntity.FindIndex(sfe => sfe.ID == selectedStaffID);
                    StaffSummarySmall displayRecord = StaffForEntity.ElementAt(displayIndex);
                    if (displayRecord == null)
                    {
                        MessageFunctions.Error("Error updating default Entity in display: display record not found.", null);
                        return false;
                    }
                    else { displayRecord.DefaultEntity = thisEntity.EntityName; }
                }    
                return true;
            }
            catch (Exception generalException)
            {                
                MessageFunctions.Error("Error setting default Entities", generalException);
                return false;
            }
        }

        public static bool ChangeDefault(int entityID, int staffID)
        {
            Entities thisEntity = EntityFunctions.GetEntity(entityID);
            try
            {
                int displayIndex;
                EntitySummaryRecord displayRecord;
                Staff thisPerson = GetStaffMember(staffID);
                if (thisPerson.DefaultEntity != entityID)
                {
                    newDefaultID = entityID;
                    
                    displayIndex = EntitiesForStaff.FindIndex(efs => efs.Default == true);
                    displayRecord = EntitiesForStaff.ElementAt(displayIndex);
                    if (displayRecord == null)
                    {
                        MessageFunctions.Error("Error setting default Entity in display: existing default record not found.", null);
                        return false;
                    }
                    else { displayRecord.Default = false; }                
                
                    displayIndex = EntitiesForStaff.FindIndex(efs => efs.ID == entityID);
                    displayRecord = EntitiesForStaff.ElementAt(displayIndex);
                    if (displayRecord == null)
                    {
                        MessageFunctions.Error("Error setting default Entity in display: new default record not found.", null);
                        return false;
                    }
                    else { displayRecord.Default = true; }
                }

                return true;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error setting default Entity", generalException);
                return false;
            }
        }

        public static bool SaveEntityStaffChanges(int entityID)
        {
            bool myDefaultChanged = false;
            
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    var recordsToRemove = (from se in existingPtDb.StaffEntities
                                         where se.EntityID == entityID && StaffIDsToRemove.Contains((int)se.StaffID)
                                         select se);
                    foreach (var removeSE in recordsToRemove)
                    {                        
                        existingPtDb.StaffEntities.Remove(removeSE);
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving staff removals from Entity", generalException);
                    return false;
                }

                try
                {
                    foreach (int addStaffID in StaffIDsToAdd)
                    {
                        StaffEntities se = new StaffEntities { EntityID = entityID, StaffID = addStaffID };
                        existingPtDb.StaffEntities.Add(se);
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving staff into Entity", generalException);
                    return false;
                }

                try
                {
                    var defaultsToSet = (from s in existingPtDb.Staff
                                         where StaffDefaultsToSet.Contains((int)s.ID)
                                         select s);
                    foreach (var staffRecord in defaultsToSet)
                    {
                        staffRecord.DefaultEntity = entityID;
                        if (staffRecord.ID == MyStaffID) { myDefaultChanged = true; }
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving staff defaults", generalException);
                    return false;
                }

                existingPtDb.SaveChanges();
                if (myDefaultChanged) 
                {
                    Entities newDefault = existingPtDb.Entities.Find(entityID);
                    EntityFunctions.UpdateMyDefaultEntity(ref newDefault); 
                }

                ClearAnyChanges();
                return true;
            }
        }

        public static bool SaveStaffEntitiesChanges(int staffID)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    var recordsToRemove = (from se in existingPtDb.StaffEntities
                                           where se.StaffID == staffID && EntityIDsToRemove.Contains((int)se.EntityID)
                                           select se);
                    foreach (var removeSE in recordsToRemove)
                    {
                        existingPtDb.StaffEntities.Remove(removeSE);
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving staff removals from Entity", generalException);
                    return false;
                }

                try
                {
                    foreach (int addEntityID in EntityIDsToAdd)
                    {
                        StaffEntities se = new StaffEntities { EntityID = addEntityID, StaffID = staffID };
                        existingPtDb.StaffEntities.Add(se);
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving staff into Entity", generalException);
                    return false;
                }

                if (newDefaultID > 0)
                {
                    Staff thisPerson = existingPtDb.Staff.Find(staffID);
                    thisPerson.DefaultEntity = newDefaultID;
                }

                existingPtDb.SaveChanges();

                if (staffID == MyStaffID && newDefaultID > 0)
                {
                    Entities newDefault = existingPtDb.Entities.Find(newDefaultID);
                    EntityFunctions.UpdateMyDefaultEntity(ref newDefault);
                }

                ClearAnyChanges();
                return true;
            }
        }

        public static bool IgnoreAnyChanges()
        {
            if (StaffIDsToAdd.Count > 0 || StaffIDsToRemove.Count > 0 || StaffDefaultsToSet.Count > 0 
                || EntityIDsToAdd.Count > 0 || EntityIDsToRemove.Count > 0 || newDefaultID > 0)
            {
                return MessageFunctions.WarningYesNo("This will undo any changes you made since you last saved. Continue?");
            }
            else
            {
                return true;
            }
        }

        public static void ClearAnyChanges()
        {
            StaffIDsToAdd.Clear();
            StaffIDsToRemove.Clear();
            StaffDefaultsToSet.Clear();
            EntityIDsToAdd.Clear();
            EntityIDsToRemove.Clear();
            newDefaultID = 0;
        }

        // Staff roles
        
        public static string[] ListUserRoles(bool includeAll)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    var rolesList = existingPtDb.StaffRoles
                                .Select(sr => sr.RoleDescription.ToString())
                                .ToList();

                    if (includeAll) { rolesList.Add(AllRecords); }

                    string[] rolesArray = rolesList.ToArray();
                    return rolesArray;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error listing staff roles", generalException);
                    return null;
                }
            }
        }

        public static string GetRoleDescription(string roleCode)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    StaffRoles thisRole = existingPtDb.StaffRoles.Find(roleCode);
                    return thisRole.RoleDescription;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving role description", generalException);
                return "";
            }
        }

        public static string GetRoleByDescription(string roleDescription)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    StaffRoles thisRole = existingPtDb.StaffRoles.First(sr => sr.RoleDescription == roleDescription);
                    return thisRole.RoleCode;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving role code", generalException);
                return "";
            }
        }

        // Navigation

        public static void ReturnToStaffPage(int staffID, string sourceMode = "Amend")
        {
            PageFunctions.ShowStaffPage(sourceMode, staffID);
        }

    } // class
} // namespace
