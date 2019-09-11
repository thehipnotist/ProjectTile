using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    class ClientFunctions
    {
        public static Clients SelectedClient;
        public const string ManagerRole = "AM";
        public static string EntityWarning = "Note that only clients in the current Entity ('" + EntityFunctions.CurrentEntityName + "') are displayed.";
        
        public static string ClientCodeFormat = "";
        public const char AlphaChar = '\u0040'; // @
        public const char NumChar = '\u0023'; // #
        public const char AlphaNum = '\u0026'; // &
        public const char Symbol = '\u0025'; // %
        public const char AnyChar = '\u0024'; // $
        public const char OtherChar = '\u003F'; // ?
        public static string SuggestionTips = "";
        public static string ExplainCode;
        
        // Data retrieval

        public static List<string> CurrentManagersList(int entityID, bool includeAll)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<string> managerNames = new List<string>();
                    managerNames = (from c in existingPtDb.Clients
                                join s in existingPtDb.Staff on c.AccountManagerID equals s.ID  
                                where (int)c.EntityID == entityID
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

        public static List<string> AllManagersList(int entityID, bool includeNonAMs, string includeIfInEntity)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<string> managerNames = new List<string>();
                    managerNames = (from s in existingPtDb.Staff
                                    join se in existingPtDb.StaffEntities on s.ID equals se.StaffID
                                    where (se.EntityID == entityID) && (includeNonAMs || s.RoleCode == ManagerRole || s.FirstName + " " + s.Surname == includeIfInEntity)
                                    orderby s.FirstName, s.Surname
                                    select s.FirstName + " " + s.Surname)
                                .Distinct().ToList();

                    //if (!managerNames.Contains(includeIfInEntity)) { managerNames.Add(includeIfInEntity); } // Now handled above

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

        public static Clients GetClientByID(int clientID, bool isSelectedClient)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    Clients thisClient = existingPtDb.Clients.Find(clientID);
                    if (isSelectedClient) { SelectedClient = thisClient; }
                    return thisClient;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving Client with ID " + clientID.ToString(), generalException);
                return null;
            }	
        }

        // Data amendment

        public static bool ValidateClient(ref Clients thisClient, int existingID, bool managerChanged)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    int entityID = thisClient.EntityID;
                    string trySuggestion = "";
                    
                    string clientCode = thisClient.ClientCode;
                    if (!PageFunctions.SqlInputOK(clientCode, true, "Client code", "Client Code", "!£$%^&*()=~#{[}]:;@'<,>.?/|¬`¦€")) { return false; }
                    Clients checkNewCode = existingPtDb.Clients.FirstOrDefault(c => c.ID != existingID && c.ClientCode == clientCode && c.EntityID == entityID);
                    if (checkNewCode == null) { thisClient.ClientCode = clientCode; }
                    else
                    {
                        string errorText = (existingID > 0) ?
                            "Could not amend client. Another client with code '" + clientCode + "' already exists." :
                            "Could not create new client. A client with code '" + clientCode + "' already exists.";

                        MessageFunctions.InvalidMessage(errorText, "Duplicate Code");
                        return false;
                    }

                    if (thisClient.Active || existingID == 0) // Unless deactivating old codes, check that the Client Code format is in line with others
                    {
                        if (ClientCodeFormat == "")
                        {
                            trySuggestion = " If in doubt, use the 'Suggest' function, which explains the suggested format.";
                        }
                        SuggestCode(entityID, existingID, ""); // Ensure the master ClientCodeFormat is set and up to date
                        string checkFormat = SuggestCode(entityID, existingID, clientCode);
                        if (checkFormat != "")
                        {
                            bool keepSaving = MessageFunctions.QuestionYesNo("The entered Client Code of '" + clientCode + "' does not fit the suggested format of '" + checkFormat
                                + "'. Are you sure this is correct?" + trySuggestion);
                            if (!keepSaving) { return false; }
                        }
                    }

                    string clientName = thisClient.ClientName;
                    if (!PageFunctions.SqlInputOK(clientName, true, "Client name")) { return false; }
                    Clients checkNewName = existingPtDb.Clients.FirstOrDefault(c => c.ID != existingID && c.ClientName == clientName && c.EntityID == entityID);
                    if (checkNewName == null) { thisClient.ClientName = clientName; }
                    else
                    {
                        string errorText = (existingID > 0) ?
                            "Could not amend client. Another client with name '" + clientName + "' already exists." :
                            "Could not create new client. A client with name '" + clientName + "' already exists.";

                        MessageFunctions.InvalidMessage(errorText, "Duplicate Name");
                        return false;
                    }

                    if (managerChanged)
                    {
                        int managerID = (int)thisClient.AccountManagerID;
                        Staff accountManager = StaffFunctions.GetStaffMember(managerID);
                        string managerRole = accountManager.RoleCode;
                        if (managerRole != ManagerRole)
                        {
                            bool confirmOK = MessageFunctions.QuestionYesNo(accountManager.FirstName + " " + accountManager.Surname + " is not normally an Account Manager. Is this correct?");
                            if (!confirmOK) { return false; }
                        }
                    }

                    return true;
                }
            }
            catch (SqlException sqlException)
            {
                MessageFunctions.Error("SQL error saving changes to the database", sqlException);
                return false;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error saving changes to the database", generalException);
                return false;
            }
        }

        public static int NewClient(string clientCode, string clientName, string accountManager, bool active, int entityID)
        {
            try
            {
                if (accountManager == "")
                {
                    MessageFunctions.InvalidMessage("Please select an Account Manager from the drop-down list.", "No Account Manager Selected");
                    return 0;
                }

                int accountManagerID = StaffFunctions.GetStaffMemberByName(accountManager).ID;
                Clients newClient = new Clients() { ClientCode = clientCode, ClientName = clientName, AccountManagerID = accountManagerID, Active = active, EntityID = entityID};
                if (ValidateClient(ref newClient, 0, true))
                {
                    try
                    {
                        ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                        using (existingPtDb)
                        {
                            existingPtDb.Clients.Add(newClient);
                            existingPtDb.SaveChanges();
                            return newClient.ID;
                        }
                    }
                    catch (Exception generalException)
                    {
                        MessageFunctions.Error("Problem saving new client", generalException);
                        return 0;
                    }
                }
                else { return 0; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error creating new client", generalException);
                return 0;
            }
        }

        public static bool AmendClient(int clientID, string clientCode, string clientName, string accountManager, bool active)
        {
            try
            {
                if (accountManager == "")
                {
                    MessageFunctions.InvalidMessage("Please select an Account Manager from the drop-down list.", "No Account Manager Selected");
                    return false;
                }                
                
                int accountManagerID = StaffFunctions.GetStaffMemberByName(accountManager).ID;
                int entityID = EntityFunctions.CurrentEntityID; // Always amending in the current Entity only

                try
                {
                    ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                    using (existingPtDb)
                    {
                        bool managerChanged = false;
                        
                        Clients thisClient = existingPtDb.Clients.Find(clientID);
                        thisClient.ClientCode = clientCode;
                        thisClient.ClientName = clientName;
                        thisClient.Active = active;

                        if (thisClient.AccountManagerID != accountManagerID)
                        {
                            thisClient.AccountManagerID = accountManagerID;
                            managerChanged = true;
                        }

                        if (ValidateClient(ref thisClient, clientID, managerChanged))
                        {
                            existingPtDb.SaveChanges();
                            return true;
                        }
                        else { return false; }
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Problem saving changes to client '" + clientName + "'", generalException);
                    return false;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error amending client '" + clientName + "'", generalException);
                return false;
            }
        }

        // Utilities
        
        private static string compareStrings(string string1, string string2, bool check)
        {
            try
            {
                string result = "";
                char[] array1 = string1.ToCharArray(); // string1 is the 'master'
                char[] array2 = string2.ToCharArray();
                int maxAttempts = Math.Min(array1.Length, array2.Length);

                for (int i = 0; i <= maxAttempts - 1; i++)
                {
                    char c1 = string1[i];
                    char c2 = string2[i];
                    char r = '\u0000';

                    if (char.IsDigit(c2) && (c1 == NumChar || char.IsDigit(c1))) { r = NumChar; } // This goes first as numbers shouldn't be expected to match
                    else if (c1 == c2) { r = c1; }
                    else if (char.IsLetter(c2) && (c1 == AlphaChar || char.IsLetter(c1))) { r = AlphaChar; }
                    else if (char.IsLetterOrDigit(c2) && (c1 == AlphaNum || c1 == NumChar || c1 == AlphaChar || char.IsLetterOrDigit(c1))) { r = AlphaNum; }
                    else if ((char.IsSymbol(c2) || char.IsPunctuation(c2))  && (c1 == Symbol || char.IsSymbol(c1) || char.IsPunctuation(c1))) { r = Symbol; }
                    else if ((char.IsLetterOrDigit(c2) || char.IsSymbol(c2) || char.IsPunctuation(c2))
                        && (c1 == AnyChar || char.IsLetterOrDigit(c1) || char.IsSymbol(c1) || char.IsPunctuation(c1)))
                        { r = AnyChar; }
                    else { r = OtherChar; }

                    result = result + r.ToString();
                }
                return result;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error comparing strings", generalException);
                return "";
            }
        }
        
        public static string SuggestCode(int entityID, int avoidID, string check = "")
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    string suggestedFormat = "";
                    List<string> clientCodes = (from c in existingPtDb.Clients
                                                where (int) c.ID != avoidID && (bool) c.Active == true && c.EntityID == entityID 
                                                select c.ClientCode).ToList();

                    if (clientCodes.ToArray().Length <= 3) // Not enough to compare
                    {
                        ClientCodeFormat = "";
                        SuggestionTips = "";
                        ExplainCode = "";
                        return ""; 
                    } 

                    if (check != "") { clientCodes.Add(check); } // Add the check string to get the result including that string
                    
                    foreach (string thisCode in clientCodes)
                    {
                        if (suggestedFormat == "") { suggestedFormat = thisCode; }
                        else { suggestedFormat = compareStrings(suggestedFormat, thisCode, false);  }
                    }

                    if (check != "") // Compare the string to the 'master' to see if it matches, returning the suggested format if not
                    {
                        //MessageFunctions.InvalidMessage(suggestedFormat + "   " + ClientCodeFormat, "Testing");
                        
                        if (suggestedFormat == ClientCodeFormat && check.Length == ClientCodeFormat.Length) { return ""; }
                        else { return ClientCodeFormat; }
                    }
                    else // Set and return the suggested format
                    {
                        ClientCodeFormat = suggestedFormat;
                        SuggestionTips = String.Format("In the format suggestion, {0} indicates a letter, {1} a digit, {2} an alphanumeric, {3} punctuation or a symbol, "
                            + "{4} any of the above, and {5} any character. Note that characters {0}, {1}, {2}, {3}, {4} and {5} themselves are invalid. Hover over the suggestion for more detail.",
                            AlphaChar, NumChar, AlphaNum, Symbol, AnyChar, OtherChar);

                        // Set a tooltip to explain what on earth this all means
                        try
                        {
                            ExplainCode = "";
                            char[] formatArray = ClientCodeFormat.ToCharArray();
                            for (int i = 0; i <= formatArray.Length - 1; i++)
                            {
                                string type = "";
                                switch (formatArray[i])
                                {
                                    case AlphaChar: type = "should be a letter of the alphabet"; break;
                                    case NumChar: type = "should be a numerical digit"; break;
                                    case AlphaNum: type = "should be a letter or digit (alphanumeric)"; break;
                                    case Symbol: type = "should be a valid punctuation mark or symbol"; break;
                                    case AnyChar: type = "should be a letter, digit, punctuation mark or symbol"; break;
                                    case OtherChar: type = "can be any type of character"; break;
                                    default: type = "should be '" + formatArray[i].ToString() + "'"; break;
                                }
                                if (ExplainCode == "") { ExplainCode = "Based on the Client Codes of all other active clients:"; }
                                ExplainCode = ExplainCode + "\nCharacter " + i.ToString() + " " + type + ".";
                            }
                        }
                        catch 
                        { // Do nothing, it's only a tooltip
                        }

                        return suggestedFormat;
                    }    
				}
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error suggesting a client code", generalException);
                return "";
            }
        }

        public static List<ClientStaff> GetContactsByClientID(int clientID, bool activeOnly)
        {           
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return (from cs in existingPtDb.ClientStaff
                            where cs.ClientID == clientID && (!activeOnly || cs.Active)
                            select cs).ToList();
				}
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error obtaining contact records for client with ID " + clientID.ToString(), generalException);
                return null;
            }	               
        }

        public static bool CopyContacts(int sourceClientID, int newClientID)
        {
            try
            {
                List<ClientStaff> sourceContacts = GetContactsByClientID(sourceClientID, true);
 
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {                   
                    foreach (ClientStaff contactRecord in sourceContacts)
                    {
                        ClientStaff newContact = new ClientStaff
                        {
                            ClientID = newClientID,
                            FirstName = contactRecord.FirstName,
                            Surname = contactRecord.Surname,
                            JobTitle = contactRecord.JobTitle,
                            PhoneNumber = contactRecord.PhoneNumber,
                            Email = contactRecord.Email,
                            Active = contactRecord.Active
                        };

                        existingPtDb.ClientStaff.Add(newContact);
                    }
                    existingPtDb.SaveChanges();
                    return true;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error copying client contacts from client ID " + sourceClientID.ToString() + " to client ID " + newClientID.ToString(), generalException);
                return false;
            }	
        }

    } // class
} // namespace
