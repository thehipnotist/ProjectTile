using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    class ClientFunctions
    {
        public static List<string> ManagersList(int entityID, bool includeAll)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<string> managerNames = new List<string>();
                    managerNames = (from c in existingPtDb.Clients
                                join s in existingPtDb.Staff on c.AccountManagerID equals s.ID  
                                where ((int)c.EntityID == entityID)
                                orderby s.FirstName, s.Surname
                                select s.FirstName + " " + s.Surname)
                                .Distinct().ToList();

                    if (includeAll) { managerNames.Add(PageFunctions.AllRecords); }
                    return managerNames;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving Account Managers' names", generalException);
                return null;
            }
        }              
        
        public static List<ClientGridRecord> ClientGridList(bool activeOnly, string nameContains, int managerID, int entityID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<ClientGridRecord> clientList = new List<ClientGridRecord>();
                    clientList = (from c in existingPtDb.Clients
                                  join s in existingPtDb.Staff on c.AccountManagerID equals s.ID
                                  join e in existingPtDb.Entities on c.EntityID equals e.ID
                                  where ((int) c.EntityID == entityID) 
                                    && (managerID == 0 || c.AccountManagerID == managerID)
                                    && (!activeOnly || c.Active)
                                    && (nameContains == "" || c.ClientName.Contains(nameContains))
                                  orderby c.ClientCode
                                  select (new ClientGridRecord() 
                                  { 
                                      ID = c.ID, 
                                      ClientCode = c.ClientCode, 
                                      ClientName = c.ClientName, 
                                      ManagerID = (int) c.AccountManagerID, 
                                      ManagerName = s.FirstName + " " + s.Surname, 
                                      ActiveClient = c.Active, 
                                      EntityID = c.EntityID, 
                                      EntityName = e.EntityName 
                                  }
                                  )).ToList();

                    return clientList;
				}
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving list of clients", generalException);
                return null;
            }          
        }



    } // class
} // namespace
