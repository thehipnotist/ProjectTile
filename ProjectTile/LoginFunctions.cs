using System;
using System.Linq;
using System.Data.SqlClient;

namespace ProjectTile
{
    public class LoginFunctions : Globals
    {
        private static MainWindow winMain = (MainWindow)App.Current.MainWindow;
                
        public static bool FirstLoad = true;

        //Password functions

        public static bool CheckPassword(string userID, string password)
        {
            try
            {
                string strDbUser = DbUserPrefix + userID;
                ProjectTileSqlDatabase defaultPtDb = SqlServerConnection.DefaultPtDbConnection();
                using (defaultPtDb)
                {
                    Staff thisUser = defaultPtDb.Staff.First(s => s.UserID == userID);
                    var checkPasswordResults = defaultPtDb.stf_CheckHashedPassword(userID, password).ToList();
                    bool passwordMatches = (checkPasswordResults[0] == true);
                    return passwordMatches;
                }
            }
            catch (SqlException sqlException)
            {
                MessageFunctions.Error("Error accessing the database", sqlException);
                return false;
            }

            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking existing login", generalException);
                return false;
            }
        }

        public static bool ChangeLoginDetails(int staffID, string userID, string newPassword, string confirmPassword)
        {
            bool passwordChange = (newPassword != "");
            bool userIDChange = false;

            if(userID == "")
            {
                MessageFunctions.Error("UserID has not been passed to this function.", null); // UserID is required to check complexity so that userID cannot equal password
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
                                    && ae.UserName.Substring(0, 5) != DbUserPrefix
                                    && ( (passwordChange && ae.ChangeColumn == "PasswordHash") || (userIDChange && ae.ChangeColumn == "UserID") )
                                )
                                .OrderByDescending(ae => ae.ChangeTime)
                                .Select(ae => (int) ae.ID)
                                .ToArray();

                            foreach (int entry in auditEntryIDs)
                            {                                
                                AuditEntries lastAuditEntry = defaultPtDb.AuditEntries.Find(entry);
                                lastAuditEntry.UserName = DbUserPrefix + MyUserID;
                                defaultPtDb.SaveChanges();
                            }

                            if (staffID == MyStaffID)
                            {
                                string databaseLogin = DbUserPrefix + userID;
                                ProjectTileSqlDatabase userPtDb = SqlServerConnection.UserPtDbConnection(databaseLogin, newPassword); // Log in again so that future database calls have the new password
                            }

                            return true;
                        }
                        catch (SqlException sqlException)
                        {
                            MessageFunctions.Error("Error amending details in the database", sqlException);
                            return false;
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error amending details", generalException);
                            return false;
                        }
                    }
                }
                catch (SqlException sqlException)
                {
                    MessageFunctions.Error("Error accessing the database", sqlException);
                    return false;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error checking existing login", generalException);
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
                string databaseLogin = DbUserPrefix + userID;

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

            catch (SqlException sqlException)
            {
                MessageFunctions.Error("Error accessing the database", sqlException);
                return false;
            }

            catch (Exception generalException)
            {
                MessageFunctions.Error("Error logging in", generalException);
                return false;
            }
        }

        public static void LogIn(Staff thisUser, Entities thisEntity)
        {
            MyStaffRecord = thisUser;
            MayName = thisUser.FirstName + " " + thisUser.Surname;
            MyStaffID = thisUser.ID;
            MyUserID = thisUser.UserID;

            EntityFunctions.UpdateCurrentEntity(ref thisEntity);
            EntityFunctions.UpdateMyDefaultEntity(ref thisEntity);

            MyPermissions = new TableSecurity(MyStaffRecord);
        }

        public static void CompleteLogIn()
        {
            winMain.HideMessage();
            winMain.MenuSecurity(ref MyPermissions);
            winMain.ToggleMainMenus(true);
            PageFunctions.ShowTilesPage();
        }

    } // class
} // namespace
