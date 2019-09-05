using System;
using System.Collections;
using System.Linq;
using System.Windows;

namespace ProjectTile
{
    public class TableSecurity
    {
        public Hashtable userPermissions = new Hashtable();
       
        public TableSecurity(Staff currentUser = null, int userID = 0)
        {
            try
            {
                ProjectTileSqlDatabase defaultPtDb = SqlServerConnection.DefaultPtDbConnection();
                using (defaultPtDb)
                {                                       
                    
                    if (currentUser == null && userID == 0)
                    {
                        MessageFunctions.Error("A staff record or ID number must be provided to get Table Permissions.", null);
                    }
                    else if (currentUser == null)
                    {
                        currentUser = defaultPtDb.Staff.Find(userID);
                    }
                    
                    string thisUsersRole = currentUser.RoleCode;
                    var permissions = from tp in defaultPtDb.TablePermissions where tp.RoleCode == thisUsersRole select tp;

                    // Set all permissions that can be set based on the TablePermissions table
                    foreach (var tUP in permissions)
                    {
                        userPermissions.Add("View" + tUP.TableName, (bool)tUP.ViewTable);
                        userPermissions.Add("Add" + tUP.TableName, (bool)tUP.InsertRows);
                        userPermissions.Add("Edit" + tUP.TableName, (bool)tUP.UpdateRows);
                        userPermissions.Add("Activate" + tUP.TableName, (bool)tUP.ChangeStatus);                       
                    }

                    // ToDo - Now set other permissions based on other factors, e.g. can the user change Entity or do they only have one?
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error setting security permissions", generalException); ;
            }
        }

        public bool Allow (string keyName)
        {
            return (bool) userPermissions[keyName];
        }

        public Visibility ShowOrCollapse(string keyName)
        {
            return Allow(keyName)? Visibility.Visible : Visibility.Collapsed;
        }

        /*
        public static bool CanCreateEntities(int intStaffID) // No longer required but left in as an example
        {
            try
            {                          
                ProjectTileSqlDatabase defaultPtDb = SqlServerConnection.defaultPtDbConnection();
                using (defaultPtDb)
                {
                    var canCreate = from s in defaultPtDb.Staff
                                    join tp in defaultPtDb.TablePermissions on s.RoleCode equals tp.RoleCode
                                    where s.ID == intStaffID && tp.TableName == "Entities"                                    
                                    select tp.InsertRows;
                    var varCanCreate = canCreate.ToList();
                    bool blnCanCreate = (bool) varCanCreate[0];
                    return blnCanCreate;
                }
            }
            catch (Exception generalException)
            {
                PopUpMessage.errorMessage("Error setting security permissions", generalException);
                return false;
            }
        }
         */ 

    }
}
