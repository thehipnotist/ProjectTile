using System;
using System.Linq;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace ProjectTile
{
    public class EntityFunctions : Globals
    {
        // Data retrieval

        public static int[] AllowedEntityIDs(int staffID) 
        {     
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection(); 
                using (existingPtDb)
                {
                    int[] allowedEntities = existingPtDb.StaffEntities
                                .Where(se => se.StaffID == staffID)
                                .Select(se => (int)se.EntityID)
                                .ToArray();
                    return allowedEntities;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error listing valid Entity IDs", generalException);
                return null;
            }
        }

        public static List<Entities> AllowedEntities(int staffID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<Entities> allowedEntities = (from se in existingPtDb.StaffEntities
                                join e in existingPtDb.Entities on se.EntityID equals e.ID
                                where se.StaffID == staffID
                                orderby e.EntityName
                                select e)
                                .ToList();
                    return allowedEntities;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving valid Entities", generalException);
                return null;
            }
        }

        public static List<Entities> EntityList(int thisUserID, bool includeAll, int excludeID = 0)
        {
            try
            {
                var entityList = AllowedEntities(thisUserID).Where(ae => !ae.ID.Equals(excludeID)).ToList();
                if (includeAll) { entityList.Add(AllEntities); }
                return entityList;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error listing valid Entity names", generalException);
                return null;
            }
        }        
        
        //public static string[] EntityNameList(int thisUserID, bool includeAll, int excludeID = 0)
        //{
        //    try
        //    {
        //        int[] entityIDs = AllowedEntityIDs(thisUserID).Where(aei => !aei.Equals(excludeID)).ToArray();
        //        ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
        //        using (existingPtDb)
        //        {
        //            var entityList = existingPtDb.Entities
        //                .Where(ent => entityIDs.Contains(ent.ID))
        //                .Select(ent => ent.EntityName)
        //                .ToList();

        //            if (includeAll) { entityList.Add(AllRecords); }
        //            string[] entityArray = entityList.ToArray();
        //            return entityArray;
        //        }
        //    }
        //    catch (Exception generalException)
        //    {
        //        MessageFunctions.Error("Error listing valid Entity names", generalException);
        //        return null;
        //    }
        //}


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
                MessageFunctions.Error("Error retrieving Entity ID " + entityID.ToString() + " from the database", generalException);
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
                MessageFunctions.Error("Error retrieving an Entity called " + entityName + " from the database", generalException);
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
                MessageFunctions.Error("Error retrieving an Entity with ID " + entityID.ToString() + " from the database", generalException);
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
                MessageFunctions.Error("Error retrieving an Entity with ID " + entityID.ToString() + " from the database", generalException);
                return false;
            }
        }

        public static void SwitchEntity(ref Entities selectedEntity, bool makeDefault = false)
        {
            if (selectedEntity == null)
            {
                MessageFunctions.InvalidMessage("Please select an Entity from the drop-down list.", "No Entity Selected");
                return;
            }            
            try
            {                    
                UpdateCurrentEntity(ref selectedEntity);
                if (makeDefault) 
                {
                    SetDefaultEntity(ref selectedEntity);
                    MessageFunctions.SuccessAlert("Your default Entity has now been set to '" + selectedEntity.EntityName + "'.", "Default Entity Changed");
                }
                PageFunctions.ShowTilesPage();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error changing current Entity", generalException); }
        }

        public static void NewEntity(string entityName, string entityDescription, bool switchTo, bool makeDefault)
        {
            int newEntityID;
            Entities newEntity;

            if (!PageFunctions.SqlInputOK(entityName, true, "Entity name")) { return; }
            else if (!PageFunctions.SqlInputOK(entityDescription, true, "Entity description")) { return; }

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
                                MessageFunctions.Error("Problem creating entity ID", generalException);
                                return;
                            }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error creating database record", generalException);
                            return;
                        }

                        try
                        {
                            Staff currentUser = MyStaffRecord;
                            AllowEntity(newEntityID, currentUser.ID);
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error providing access to the new database", generalException);
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

                            if (makeDefault) { SetDefaultEntity(ref newEntity); }

                            MessageFunctions.SuccessAlert("Entity '" + entityName + "' has been created" + switched, "New Entity Created");
                            PageFunctions.ShowTilesPage();
                        }
                        catch (SqlException sqlException)
                        {
                            MessageFunctions.Error("SQL error saving changes to the database", sqlException);
                            return;
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error saving changes to the database", generalException);
                            return;
                        }
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error creating new database", generalException); }
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error checking new database details", generalException); }
        }

        public static void AmendEntity(ref Entities selectedEntity, string entityName, string entityDescription)
        {
            int intSelectedEntityID;
            
            if (selectedEntity == null)
            {
                MessageFunctions.InvalidMessage("Please select an Entity to amend from the drop-down list.", "No Entity Selected");
                return;
            }

            if (!PageFunctions.SqlInputOK(entityName, true, "Entity name")) { return; }
            else if (!PageFunctions.SqlInputOK(entityDescription, true, "Entity description")) { return; }

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
                            string nameChange = "";
                            string originalName = selectedEntity.EntityName;

                            if (originalName != entityName)
                            {
                                nameChange = " to '" + entityName + "'";
                            };

                            Entities changeDbEntity = existingPtDb.Entities.Find(intSelectedEntityID);
                            changeDbEntity.EntityName = entityName;
                            changeDbEntity.EntityDescription = entityDescription;
                            existingPtDb.SaveChanges();

                            MessageFunctions.SuccessAlert("Entity '" + originalName + "' has been amended" + nameChange + ".", "Entity Amended");
                            if (changeDbEntity.ID == CurrentEntityID) { UpdateCurrentEntity(ref changeDbEntity); }
                            if (changeDbEntity.ID == MyDefaultEntityID) { UpdateMyDefaultEntity(ref changeDbEntity); }
                            PageFunctions.ShowTilesPage();
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error amending database record", generalException);
                            return;
                        }

                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error creating new database", generalException); }
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error checking new database details", generalException); }
        }
 
        public static void UpdateCurrentEntity(ref Entities targetEntity)
        {
            CurrentEntity = targetEntity;
            CurrentEntityID = CurrentEntity.ID;
            CurrentEntityName = CurrentEntity.EntityName;
            PageFunctions.UpdateDetailsBlock();
        }

        // Default Entity functions

        public static void SetDefaultEntity(ref Entities selectedEntity, int staffID = 0)
        {
            if (selectedEntity == null)
            {
                MessageFunctions.InvalidMessage("Please select an Entity to amend from the drop-down list.", "No Entity Selected");
                return;
            }
            
            ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
            using (existingPtDb)
            {
                try
                {
                    if (staffID == 0) { staffID = MyStaffID; }

                    Staff thisUser = existingPtDb.Staff.Find(staffID);                    
                    thisUser.DefaultEntity = selectedEntity.ID;
                    
                    existingPtDb.SaveChanges();
                    if (staffID == MyStaffID) { UpdateMyDefaultEntity(ref selectedEntity); }

                }
                catch (SqlException sqlException)
                {
                    MessageFunctions.Error("SQL error saving new default Entity preference to the database", sqlException);
                    return;
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error saving new default Entity preference to the database", generalException);
                    return;
                }
            }
        }

        public static void ChangeDefaultEntity(ref Entities selectedEntity, string displayName)
        {
            if (selectedEntity != null)
            {
                try
                {
                    SetDefaultEntity(ref selectedEntity);
                    string notCurrent = "";
                    if (displayName != CurrentEntityName) { notCurrent = " Note that you are still currently connected to '" + CurrentEntityName + "'."; }

                    MessageFunctions.SuccessAlert("Your default Entity has now been set to '" + displayName + "'." + notCurrent, "Default Entity Changed");
                    PageFunctions.UpdateDetailsBlock();
                    PageFunctions.ShowTilesPage();
                }
                catch (Exception generalException) { MessageFunctions.Error("Error changing entity", generalException); }
            }
            else { MessageFunctions.InvalidMessage("Please select an Entity from the drop-down list.", "No Entity Selected"); }
        }

        public static void UpdateMyDefaultEntity(ref Entities targetEntity)
        {
            Entities defaultEntity = targetEntity;
            MyDefaultEntityID = targetEntity.ID;
            MyDefaultEntityName = targetEntity.EntityName;
            PageFunctions.UpdateDetailsBlock();
        }

    } // class
} // namespace
