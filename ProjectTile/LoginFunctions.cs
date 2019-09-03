using System;
using System.Linq;
using System.Data.SqlClient;

namespace ProjectTile
{
    public class LoginFunctions
    {
        private static MainWindow winMain = (MainWindow)App.Current.MainWindow;

        public static Staff CurrentUser;
        public static int CurrentStaffID;
        public static string CurrentStaffName = "";
        public static string CurrentUserID;
        private const string dBUserPrefix = "ProT_";

        public static TableSecurity MyPermissions;
        public static bool FirstLoad = true;

        //Password functions

        public static bool CheckPassword(string userID, string password)
        {
            try
            {
                string strDbUser = dBUserPrefix + userID;
                ProjectTileSqlDatabase defaultPtDb = SqlServerConnection.DefaultPtDbConnection();
                using (defaultPtDb)
                {
                    Staff thisUser = defaultPtDb.Staff.First(s => s.UserID == userID);
                    var checkPasswordResults = defaultPtDb.stf_CheckHashedPassword(userID, password).ToList();
                    bool passwordMatches = (checkPasswordResults[0] == true);
                    return passwordMatches;
                }
            }
            catch (SqlException connectException)
            {
                MessageFunctions.ErrorMessage("Error accessing the database: " + connectException.Message.Replace(dBUserPrefix, "") + " Please check the details and try again.");
                return false;
            }

            catch (Exception otherException)
            {
                MessageFunctions.ErrorMessage("Error checking existing login: " + otherException);
                return false;
            }
        }

        public static bool ChangeLoginDetails(int staffID, string userID, string newPassword, string confirmPassword)
        {
            bool passwordChange = (newPassword != "");
            bool userIDChange = false;

            if(userID == "")
            {
                MessageFunctions.ErrorMessage("UserID has not been passed to this function."); // UserID is required to check complexity so that userID cannot equal password
                return false;
            }
            
            if (passwordChange && newPassword != confirmPassword)
            {
                MessageFunctions.InvalidMessage("New password does not match confirmation. Please check both fields and try again.", "Password Mismatch");
                return false;
            }
            else if (passwordChange && !PasswordComplexityOK(userID, newPassword)) { return false; }
            else
            {
                try
                {
                    // Log in as the administration user to allow the change to be made
                    ProjectTileSqlDatabase defaultPtDb = SqlServerConnection.DefaultPtDbConnection();
                    using (defaultPtDb)
                    {
                        try
                        {
                            Staff thisUser = defaultPtDb.Staff.Find(staffID);
                            if (passwordChange) { thisUser.Passwd = newPassword; }
                            if (thisUser.UserID != userID) 
                            {
                                Staff checkUserID = defaultPtDb.Staff.FirstOrDefault(s => s.UserID == userID && s.ID != staffID);
                                if (checkUserID != null)
                                {
                                    MessageFunctions.InvalidMessage("A different staff member with UserID '" + userID +
                                        "' already exists. Please try a different one.", "Duplicate UserID");
                                    return false;
                                }                               
                                
                                userIDChange = true;
                                thisUser.UserID = userID; 
                            }
                            defaultPtDb.SaveChanges();

                            // Now amend any history records, to show that the user effectively made this change
                            DateTime timeFrom = System.DateTime.Now.AddMinutes(-5);
                            int[] auditEntryIDs = defaultPtDb.AuditEntries
                                .Where(ae => ae.TableName == "Staff"
                                    && ae.ChangeTime >= timeFrom
                                    && ae.ActionType == "Updated"
                                    && ae.PrimaryValue == staffID.ToString()
                                    && ae.UserName.Substring(0, 5) != dBUserPrefix
                                    && ( (passwordChange && ae.ChangeColumn == "PasswordHash") || (userIDChange && ae.ChangeColumn == "UserID") )
                                )
                                .OrderByDescending(ae => ae.ChangeTime)
                                .Select(ae => (int) ae.ID)
                                .ToArray();

                            foreach (int entry in auditEntryIDs)
                            {                                
                                AuditEntries lastAuditEntry = defaultPtDb.AuditEntries.Find(entry);
                                lastAuditEntry.UserName = dBUserPrefix + CurrentUserID;
                                defaultPtDb.SaveChanges();
                            }

                            if (staffID == CurrentStaffID)
                            {
                                string databaseLogin = dBUserPrefix + userID;
                                ProjectTileSqlDatabase userPtDb = SqlServerConnection.UserPtDbConnection(databaseLogin, newPassword); // Log in again so that future database calls have the new password
                            }

                            return true;
                        }
                        catch (SqlException connectException)
                        {
                            MessageFunctions.ErrorMessage("Error amending details in the database: " + connectException.Message.Replace(dBUserPrefix, "") + " Please check the details and try again.");
                            return false;
                        }
                        catch (Exception otherException)
                        {
                            MessageFunctions.ErrorMessage("Error amending details. " + otherException + " Please check your existing password and try again.");
                            return false;
                        }
                    }
                }
                catch (SqlException connectException)
                {
                    MessageFunctions.ErrorMessage("Error accessing the database: " + connectException.Message.Replace(dBUserPrefix, "") + " Please check the details and try again.");
                    return false;
                }
                catch (Exception otherException)
                {
                    MessageFunctions.ErrorMessage("Error checking existing login: " + otherException.Message);
                    return false;
                }
            }
        }

        public static bool PasswordComplexityOK(string userID, string newPassword)
        {
            bool passOK = false;
  
            if (newPassword.Length < 8)
            {
                MessageFunctions.InvalidMessage("Your new password must be at least 8 characters long. Please try again.", "Password Too Short");
            }
            else if (userID == newPassword)
            {
                MessageFunctions.InvalidMessage("Your new password cannot equal your UserID. Please try again.", "Duplicate Password");
            }
            else { passOK = true; }

            return passOK;
        }

        // Login functions

        public static bool AttemptLogin(string userID, string password)
        {
            try
            {
                int entityID;
                string databaseLogin = dBUserPrefix + userID;

                ProjectTileSqlDatabase userPtDb = SqlServerConnection.UserPtDbConnection(databaseLogin, password);
                using (userPtDb)
                {
                    Staff thisUser = userPtDb.Staff.First(s => s.UserID == userID);
                    entityID = (int)thisUser.DefaultEntity;
                    Entities currentEntity = userPtDb.Entities.Find(entityID);

                    if (thisUser.FirstName != "")
                    {
                        if (!thisUser.Active) { MessageFunctions.InvalidMessage("User is not active. Please contact your system administrator.", "Inactive User"); }
                        else if (thisUser.LeaveDate < DateTime.Now) { MessageFunctions.InvalidMessage("User has left. Please contact your system administrator.", "Not Current User"); }
                        else if (thisUser.StartDate > DateTime.Now) { MessageFunctions.InvalidMessage("User has not yet started. Please contact your system administrator.", "Not Current User"); }
                        else { LogIn(thisUser, currentEntity); }
                    }

                    return true;
                }
            }

            catch (SqlException connectException)
            {
                MessageFunctions.ErrorMessage("Error accessing the database: " + connectException.Message.Replace(dBUserPrefix, "") + " Please check the details and try again.");
                return false;
            }

            catch (Exception otherException)
            {
                MessageFunctions.ErrorMessage("Error logging in: " + otherException.Message);
                return false;
            }
        }

        public static void LogIn(Staff thisUser, Entities thisEntity)
        {
            CurrentUser = thisUser;
            CurrentStaffName = thisUser.FirstName + " " + thisUser.Surname;
            CurrentStaffID = thisUser.ID;
            CurrentUserID = thisUser.UserID;

            EntityFunctions.UpdateCurrentEntity(ref thisEntity);
            EntityFunctions.UpdateMyDefaultEntity(ref thisEntity);

            MyPermissions = new TableSecurity(CurrentUser);
            winMain.MenuSecurity(ref MyPermissions);
            winMain.toggleMainMenu(true);

            PageFunctions.ShowTilesPage();
        }

    } // class
} // namespace
