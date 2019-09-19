using System;
using System.Collections;
using System.Linq;
using System.Windows;

namespace ProjectTile
{
    public class TableSecurity
    {
        private Hashtable userPermissions = new Hashtable();
       
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
            if (userPermissions.Contains(keyName))
            {
                return (bool)userPermissions[keyName];
            }
            else
            {
                MessageFunctions.Error("Error retrieving table security settings; the connection does not contain a '" + keyName + "' entry", null);
                return false;
            }
        }

        public Visibility ShowOrCollapse(string keyName)
        {
            return Allow(keyName)? Visibility.Visible : Visibility.Collapsed;
        }

    } // class
} // namespace
