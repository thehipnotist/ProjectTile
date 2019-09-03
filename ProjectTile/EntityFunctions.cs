using System;
using System.Linq;
using System.Data.SqlClient;

namespace ProjectTile
{
    public class EntityFunctions
    {
        private static MainWindow winMain = (MainWindow)App.Current.MainWindow;
        
        public static int CurrentEntityID;
        public static string CurrentEntityName = "";
        public static Entities CurrentEntity;

        public static int DefaultEntityID;
        public static string DefaultEntityName = "";
        
        // Data retrieval

        public static int[] AllowedEntities(int thisUserID) 
        {     
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection(); 
                using (existingPtDb)
                {
                    int[] allowedEntities = existingPtDb.StaffEntities
                                .Where(se => se.StaffID == thisUserID)
                                .Select(se => (int)se.EntityID)
                                .ToArray();
                    return allowedEntities;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error listing valid Entity IDs: " + generalException.Message);
                return null;
            }
        }

        public static string[] EntityList(int thisUserID, bool includeAll)
        {
            try
            {
                int[] entityIDs = AllowedEntities(thisUserID);
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    var entityList = existingPtDb.Entities
                        .Where(ent => entityIDs.Contains(ent.ID))
                        .Select(ent => ent.EntityName)
                        .ToList();

                    if (includeAll) { entityList.Add(PageFunctions.AllRecords); }

                    string[] entityArray = entityList.ToArray();
                    return entityArray;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error listing valid Entity names: " + generalException.Message);
                return null;
            }
        }

        public static Entities GetEntity(int entityID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Entities selectedEntity = existingPtDb.Entities.Find(entityID);
                    return selectedEntity;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error retrieving Entity ID " + entityID.ToString() + " from the database: " + generalException.Message);
                return null;
            }
        }

        public static Entities GetEntityByName(string entityName)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Entities selectedEntity = existingPtDb.Entities.First(ent => ent.EntityName == entityName);
                    return selectedEntity;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error retrieving an Entity called " + entityName + " from the database: " + generalException.Message);
                return null;
            }
        }

        public static string GetEntityName(int entityID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Entities selectedEntity = existingPtDb.Entities.Find(entityID);
                    return selectedEntity.EntityName;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error retrieving an Entity with ID " + entityID.ToString() + " from the database: " + generalException.Message);
                return "";
            }
        }

        // Entity changes

        public static bool AllowEntity(int entityID, int staffID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    StaffEntities createStaffEntity = new StaffEntities();
                    createStaffEntity.EntityID = entityID;
                    createStaffEntity.StaffID = staffID;
                    existingPtDb.StaffEntities.Add(createStaffEntity);

                    existingPtDb.SaveChanges();
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error retrieving an Entity with ID " + entityID.ToString() + " from the database: " + generalException.Message);
                return false;
            }
        }

        public static void ChangeEntity(int entityID, ref Entities selectedEntity, bool makeDefault)
        {
            if (entityID >= 0)
            {
                try
                {                    
                    UpdateCurrentEntity(ref selectedEntity);
                    if (makeDefault) 
                    {
                        string strEntityName = selectedEntity.EntityName;
                        SetDefaultEntity(ref selectedEntity, 0, true);
                        MessageFunctions.SuccessMessage("Your default Entity has now been set to '" + strEntityName + "'.", "Default Entity Changed");
                    }
                    PageFunctions.ShowTilesPage();
                }
                catch (Exception generalException) { MessageFunctions.ErrorMessage("Error changing entity: " + generalException.Message); }
            }
            else { MessageFunctions.InvalidMessage("Please select an Entity from the drop-down list.", "No Entity Selected"); }
        }

        public static void NewEntity(string entityName, string entityDescription, bool switchTo, bool makeDefault)
        {
            int newEntityID;
            Entities newEntity;

            if (entityName == "")
            {
                MessageFunctions.InvalidMessage("Please enter a name for the new Entity in the 'Entity Name' text box.", "No Name Entered");
                return;
            }

            if (entityDescription == "")
            {
                MessageFunctions.InvalidMessage("Please enter a description for the new Entity in the 'Entity Description' text box.", "No Description Entered");
                return;
            }

            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Entities checkNewName = existingPtDb.Entities.FirstOrDefault(ent => ent.EntityName == entityName);
                    if (checkNewName != null)
                    {
                        MessageFunctions.InvalidMessage("Could not create new Entity. An Entity with name '" + entityName + "' already exists.", "Duplicate Name");
                        return;
                    }

                    Entities checkNewDescription = existingPtDb.Entities.FirstOrDefault(ent => ent.EntityDescription == entityDescription);
                    if (checkNewDescription != null)
                    {
                        MessageFunctions.InvalidMessage("Could not create new Entity. An Entity with description '" + entityDescription + "' already exists.", "Duplicate Description");
                        return;
                    }

                    try
                    {
                        try
                        {
                            newEntity = new Entities();
                            newEntity.EntityName = entityName;
                            newEntity.EntityDescription = entityDescription;
                            
                            try
                            {
                                existingPtDb.Entities.Add(newEntity);
                                existingPtDb.SaveChanges();
                                newEntityID = newEntity.ID;
                            }
                            catch (Exception generalException)
                            {
                                MessageFunctions.ErrorMessage("Problem creating entity ID: " + generalException.Message);
                                return;
                            }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.ErrorMessage("Error creating database record: " + generalException.Message);
                            return;
                        }

                        try
                        {
                            Staff currentUser = LoginFunctions.CurrentUser;
                            AllowEntity(newEntityID, currentUser.ID);
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.ErrorMessage("Error providing access to the new database: " + generalException.Message);
                            return;
                        }

                        try
                        {
                            existingPtDb.SaveChanges();
                            string switched = ". Use the 'Change Current Entity' function to log into it if you wish to work in this Entity.";

                            if (switchTo)
                            {
                                UpdateCurrentEntity(ref newEntity);
                                switched = " and you are now logged into it.";
                            }

                            if (makeDefault) { SetDefaultEntity(ref newEntity, 0, true); }

                            MessageFunctions.SuccessMessage("Entity '" + entityName + "' has been created" + switched, "New Entity Created");
                            PageFunctions.ShowTilesPage();
                        }
                        catch (SqlException sqlException)
                        {
                            MessageFunctions.ErrorMessage("SQL error saving changes to the database: " + sqlException.Message);
                            return;
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.ErrorMessage("Error saving changes to the database: " + generalException.Message);
                            return;
                        }
                    }
                    catch (Exception generalException) { MessageFunctions.ErrorMessage("Error creating new database: " + generalException.Message); }
                }
            }
            catch (Exception generalException) { MessageFunctions.ErrorMessage("Error checking new database details: " + generalException.Message); }
        }

        public static void AmendEntity(ref Entities selectedEntity, string entityName, string entityDescription)
        {
            int intSelectedEntityID;
            
            if (selectedEntity == null)
            {
                MessageFunctions.InvalidMessage("Please select an Entity to amend from the drop-down list.", "No Entity Selected");
                return;
            }
            else if (entityName == "")
            {
                MessageFunctions.InvalidMessage("Please enter a name for the Entity in the 'Entity Name' text box.", "No Name Entered");
                return;
            }
            else if (entityDescription == "")
            {
                MessageFunctions.InvalidMessage("Please enter a description for the Entity in the 'Entity Description' text box.", "No Description Entered");
                return;
            }

            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    intSelectedEntityID = selectedEntity.ID;
                    
                    Entities checkNewName = existingPtDb.Entities.FirstOrDefault(ent => ent.EntityName == entityName && ent.ID != intSelectedEntityID);
                    if (checkNewName != null)
                    {
                        MessageFunctions.InvalidMessage("Could not amend Entity. Another Entity with name '" + entityName + "' already exists.", "Duplicate Name");
                        return;
                    }

                    Entities checkNewDescription = existingPtDb.Entities.FirstOrDefault(ent => ent.EntityDescription == entityDescription && ent.ID != intSelectedEntityID);
                    if (checkNewDescription != null)
                    {
                        MessageFunctions.InvalidMessage("Could not amend Entity. Another Entity with description '" + entityDescription + "' already exists.", "Duplicate Description");
                        return;
                    }

                    try
                    {
                        try
                        {
                            string strNameChange = "";
                            string strOriginalName = selectedEntity.EntityName;

                            if (strOriginalName != entityName)
                            {
                                strNameChange = " to '" + entityName + "'";
                            };

                            Entities changeDbEntity = existingPtDb.Entities.Find(intSelectedEntityID);
                            changeDbEntity.EntityName = entityName;
                            changeDbEntity.EntityDescription = entityDescription;
                            existingPtDb.SaveChanges();

                            MessageFunctions.SuccessMessage("Entity '" + strOriginalName + "' has been amended" + strNameChange + ".", "Entity Amended");
                            if (changeDbEntity.ID == CurrentEntityID) { UpdateCurrentEntity(ref changeDbEntity); }
                            if (changeDbEntity.ID == DefaultEntityID) { UpdateMyDefaultEntity(ref changeDbEntity); }
                            PageFunctions.ShowTilesPage();
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.ErrorMessage("Error amending database record: " + generalException.Message);
                            return;
                        }

                    }
                    catch (Exception generalException) { MessageFunctions.ErrorMessage("Error creating new database: " + generalException.Message); }
                }
            }
            catch (Exception generalException) { MessageFunctions.ErrorMessage("Error checking new database details: " + generalException.Message); }
        }
 
        public static void UpdateCurrentEntity(ref Entities targetEntity)
        {
            CurrentEntity = targetEntity;
            CurrentEntityID = CurrentEntity.ID;
            CurrentEntityName = CurrentEntity.EntityName;
            winMain.updateDetailsBlock();
        }

        // Default Entity functions

        public static void SetDefaultEntity(ref Entities selectedEntity, int staffID = 0, bool save = true)
        {
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    if (staffID == 0) { staffID = LoginFunctions.CurrentStaffID; }                    
                    Staff thisUser = existingPtDb.Staff.Find(staffID);                    
                    thisUser.DefaultEntity = DefaultEntityID;

                    if (save)
                    {
                        existingPtDb.SaveChanges();
                        if (staffID == LoginFunctions.CurrentStaffID) { UpdateMyDefaultEntity(ref selectedEntity); }
                    }
                }
                catch (SqlException sqlException)
                {
                    MessageFunctions.ErrorMessage("SQL error saving new default Entity preference to the database: " + sqlException.Message);
                    return;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.ErrorMessage("Error saving new default Entity preference to the database: " + generalException.Message);
                    return;
                }
            }
        }

        public static void ChangeDefaultEntity(ref Entities selectedEntity, string entityName)
        {
            if (selectedEntity != null)
            {
                try
                {
                    SetDefaultEntity(ref selectedEntity, 0, true);
                    string notCurrent = "";
                    if (entityName != CurrentEntityName) { notCurrent = " Note that you are still currently connected to '" + CurrentEntityName + "'."; }

                    MessageFunctions.SuccessMessage("Your default Entity has now been set to '" + entityName + "'." + notCurrent, "Default Entity Changed");
                    winMain.updateDetailsBlock();
                    PageFunctions.ShowTilesPage();
                }
                catch (Exception generalException) { MessageFunctions.ErrorMessage("Error changing entity: " + generalException.Message); }
            }
            else { MessageFunctions.InvalidMessage("Please select an Entity from the drop-down list.", "No Entity Selected"); }
        }

        public static void UpdateMyDefaultEntity(ref Entities targetEntity)
        {
            Entities defaultEntity = targetEntity;
            DefaultEntityID = targetEntity.ID;
            DefaultEntityName = targetEntity.EntityName;
            winMain.updateDetailsBlock();
        }

    } // class
} // namespace
