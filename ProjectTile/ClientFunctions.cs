﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace ProjectTile
{

    class ClientFunctions
    {
        
        public const string ManagerRole = "AM";
        public static string EntityWarning = "Note that only clients in the current Entity ('" + EntityFunctions.CurrentEntityName + "') are displayed.";
        public static string ShortEntityWarning = "Only clients in the current Entity are displayed.";
        
        public static string ClientCodeFormat = "";
        public const char AlphaChar = '\u0040'; // @
        public const char NumChar = '\u0023'; // #
        public const char AlphaNum = '\u0026'; // &
        public const char Symbol = '\u0025'; // %
        public const char AnyChar = '\u0024'; // $
        public const char OtherChar = '\u003F'; // ?
        public static string SuggestionTips = "";
        public static string ExplainCode;

        public static List<Clients> ClientsNotForProduct;
        public static List<ClientProductSummary> ClientsForProduct;
        public static List<int> ClientIDsToAdd = new List<int>();
        public static List<int> ClientIDsToRemove = new List<int>();

        public static List<Products> ProductsNotForClient;
        public static List<ClientProductSummary> ProductsForClient;
        public static List<int> ProductIDsToAdd = new List<int>();
        public static List<int> ProductIDsToRemove = new List<int>();

        // The following must be updated when opening a page from the menu and choosing a client, or cleared when returning to the menu or clearing client selection
        public static string SourcePage = "TilesPage";
        public static string SourcePageMode = PageFunctions.None;
        public static Clients SelectedClient = null;    
        
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
                    if (checkNewCode != null) 
                    {
                        string errorText = (existingID > 0) ?
                            "Could not amend client. Another client with code '" + clientCode + "' already exists in this Entity." :
                            "Could not create new client. A client with code '" + clientCode + "' already exists in this Entity.";

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
                    if (checkNewName != null) 
                    {
                        string errorText = (existingID > 0) ?
                            "Could not amend client. Another client with name '" + clientName + "' already exists in this Entity." :
                            "Could not create new client. A client with name '" + clientName + "' already exists in this Entity.";

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
                if (ValidateClient(ref newClient, newClient.ID, true))
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

        // Contacts functions
        public static ClientStaff GetContactByName(int clientID, string contactName)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.ClientStaff.FirstOrDefault(cs => cs.ClientID == clientID && cs.FirstName + " " + cs.Surname == contactName);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving contact with name '" + contactName + "'", generalException);
                return null;
            }	
        }

        public static List<ClientGridRecord> ClientGridListByContact(bool activeOnly, string clientContains, string contactContains, int entityID)
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
                                  join cs in existingPtDb.ClientStaff on c.ID equals cs.ClientID
                                    into GroupJoin from scs in GroupJoin.DefaultIfEmpty()
                                  where ((int)c.EntityID == entityID)
                                    && contactContains == "" || (scs.FirstName + " " + scs.Surname).Contains(contactContains)
                                    && (!activeOnly || c.Active)
                                    && (clientContains == "" || c.ClientName.Contains(clientContains))
                                  orderby c.ClientCode
                                  select (new ClientGridRecord()
                                  {
                                      ID = c.ID,
                                      ClientCode = c.ClientCode,
                                      ClientName = c.ClientName,
                                      ManagerID = (int)c.AccountManagerID,
                                      ManagerName = s.FirstName + " " + s.Surname,
                                      ActiveClient = c.Active,
                                      EntityID = c.EntityID,
                                      EntityName = e.EntityName
                                  }
                                  )).Distinct().ToList();

                    return clientList;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving list of clients", generalException);
                return null;
            }
        }

        public static List<ContactGridRecord> ContactGridList (string contactContains, bool ActiveOnly, int clientID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return (from cs in existingPtDb.ClientStaff
                            where ( (clientID == 0 || cs.ClientID == clientID)
                                && (!ActiveOnly || cs.Active)
                                && (contactContains == "" || (cs.FirstName + " " + cs.Surname).Contains(contactContains) || cs.JobTitle.Contains(contactContains)) )
                            select (new ContactGridRecord 
                            {
                                ID = cs.ID,
                                ContactName = cs.FirstName + " " + cs.Surname,
                                JobTitle = cs.JobTitle,
                                PhoneNumber = cs.PhoneNumber,
                                Email = cs.Email,
                                ActiveContact = cs.Active
                            }) ).ToList();                                                
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving list of contacts", generalException);
                return null;
            }	
        }

        public static List<string> ContactDropList(string contactContains)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return (from cs in existingPtDb.ClientStaff
                            where contactContains == "" || (cs.FirstName + " " + cs.Surname).Contains(contactContains)
                            select cs.FirstName + " " + cs.Surname
                                /*
                                JobTitle = cs.JobTitle,
                                PhoneNumber = cs.PhoneNumber,
                                Email = cs.Email,
                                ActiveContact = cs.Active
                                */
                            ).Distinct().ToList();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving drop-down list of contacts", generalException);
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

        public static ClientStaff GetContact(int contactID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.ClientStaff.Find(contactID);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving details for client contact with ID " + contactID.ToString(), generalException);
                return null;
            }		
        }

        public static bool? EnableOrDisable(int contactID)
        {
            try
            {
                ClientStaff selectedContact = GetContact(contactID);
                if (selectedContact == null) { return null; }

                string changeName = selectedContact.Active ? "Disable" : "Enable";
                string changeAction = selectedContact.Active ? "disabling" : "enabling";
                bool confirm = MessageFunctions.QuestionYesNo(
                        changeName + " " + selectedContact.FirstName + " " + selectedContact.Surname + "'s record? This will take effect immediately.",
                        changeName + " user?");
                    
                if (!confirm) { return null; }
                else
                {
                    try
                    {                            
                        ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                        using (existingPtDb)
                        {                            
                            bool blnNewStatus = !selectedContact.Active;
                            selectedContact = existingPtDb.ClientStaff.Find(contactID); // Required as so far we are using a copy
                            selectedContact.Active = blnNewStatus;
                            existingPtDb.SaveChanges();
                            return selectedContact.Active;
                        }
                    }
                    catch (Exception generalException)
                    {
                        MessageFunctions.Error("Error " + changeAction + " contact", generalException);
                        return null;
                    }                    
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error changing contact status", generalException);
                return null;
            }
        }

        public static bool ValidateContact(ref ClientStaff thisContact, int existingID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    int clientID = thisContact.ClientID;

                    if (!PageFunctions.SqlInputOK(thisContact.FirstName, true, "First name")) { return false; }
                    if (!PageFunctions.SqlInputOK(thisContact.FirstName, true, "Surname")) { return false; }

                    string contactName = thisContact.FirstName + " " + thisContact.Surname;                    
                    ClientStaff checkNewName = existingPtDb.ClientStaff.FirstOrDefault(c => c.ID != existingID && c.FirstName + " " + c.Surname == contactName && c.ClientID == clientID);
                    if (checkNewName != null) 
                    {
                        string errorText = (existingID > 0) ?
                            "Could not amend contact. Another contact with name '" + contactName + "' already exists for this client." :
                            "Could not create new contact. A contact with name '" + contactName + "' already exists for this client.";

                        MessageFunctions.InvalidMessage(errorText, "Duplicate Name");
                        return false;
                    }

                    if (thisContact.JobTitle == "")
                    {
                        bool keepSaving = MessageFunctions.QuestionYesNo("You have not entered a job title for this contact, which may make them difficult to identify later. " + 
                            "Is this intentional? If in doubt enter your best guess, followed by '(To be confirmed)' for example.");
                        if (!keepSaving) { return false; }
                    } 

                    string email = thisContact.Email;
                    if (email == "" && thisContact.PhoneNumber == "")
                    {
                        bool keepSaving = MessageFunctions.QuestionYesNo("You have not entered any contact details for this contact. Is this intentional?");
                        if (!keepSaving) { return false; }
                    }
                    if ((email != "" && !email.Contains("@") || !email.Contains(".")))
                    {
                        bool keepSaving = MessageFunctions.QuestionYesNo("The entered e-mail address of '" + email + "' does not appear to be valid. Are you sure this is correct?");
                        if (!keepSaving) { return false; }
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

        public static int NewContact(string firstName, string surname, string jobTitle, string phoneNumber, string email, bool active)
        {
            try
            {
                ClientStaff newContact = new ClientStaff() { ClientID = SelectedClient.ID, FirstName = firstName, Surname = surname, JobTitle = jobTitle, PhoneNumber = phoneNumber,
                    Email = email, Active = active};
                if (ValidateContact(ref newContact, 0))
                {
                    try
                    {
                        ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                        using (existingPtDb)
                        {
                            existingPtDb.ClientStaff.Add(newContact);
                            existingPtDb.SaveChanges();
                            return newContact.ID;
                        }
                    }
                    catch (Exception generalException)
                    {
                        MessageFunctions.Error("Problem saving new contact", generalException);
                        return 0;
                    }
                }
                else { return 0; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error creating new contact", generalException);
                return 0;
            }
        }

        public static bool AmendContact(int contactID, string firstName, string surname, string jobTitle, string phoneNumber, string email, bool active)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    ClientStaff thisContact = existingPtDb.ClientStaff.Find(contactID);
                    thisContact.ClientID = SelectedClient.ID;
                    thisContact.FirstName = firstName;
			        thisContact.Surname = surname;
			        thisContact.JobTitle = jobTitle;
			        thisContact.PhoneNumber = phoneNumber;
			        thisContact.Email = email;
                    thisContact.Active = active;

                    if (ValidateContact(ref thisContact, contactID))
                    {
                        try
                        {
                            existingPtDb.SaveChanges();
                            return true;
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Problem saving changes to contact '" + firstName + " " + surname + "'", generalException);
                            return false;
                        }
                    }
                    else { return false; }
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error amending contact '" + firstName + " " + surname + "'", generalException);
                return false;
            }
        }

        // Client Products
        public static List<ClientGridRecord> ClientGridListByProduct(bool activeOnly, string clientContains, int productID, int entityID)
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
                                  join cp in existingPtDb.ClientProducts on c.ID equals cp.ClientID
                                    into GroupJoin from scp in GroupJoin.DefaultIfEmpty()
                                  where ((int)c.EntityID == entityID)
                                    && (productID == 0 || scp.ProductID == productID)
                                    && (!activeOnly || c.Active)
                                    && (clientContains == "" || c.ClientName.Contains(clientContains))
                                  orderby c.ClientCode
                                  select (new ClientGridRecord()
                                  {
                                      ID = c.ID,
                                      ClientCode = c.ClientCode,
                                      ClientName = c.ClientName,
                                      ManagerID = (int)c.AccountManagerID,
                                      ManagerName = s.FirstName + " " + s.Surname,
                                      ActiveClient = c.Active,
                                      EntityID = c.EntityID,
                                      EntityName = e.EntityName
                                  }
                                  )).Distinct().ToList();

                    return clientList;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving list of clients", generalException);
                return null;
            }
        }

        public static List<ClientProductSummary> ClientsWithProduct(bool activeOnly, int productID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return (from p in existingPtDb.Products
                            join cp in existingPtDb.ClientProducts on p.ID equals cp.ProductID
                            join c in existingPtDb.Clients on cp.ClientID equals c.ID
                            where (productID == 0 || p.ID == productID)  && (!activeOnly || c.Active) && c.EntityID == EntityFunctions.CurrentEntityID
                            orderby c.ClientName
                            select new ClientProductSummary 
                            {
                                ID = cp.ID,
                                ClientID = c.ID,
                                ClientName = c.ClientName,
                                ClientEntityID = c.EntityID,
                                ActiveClient = c.Active,
                                ProductID = p.ID,
                                ProductName = p.ProductName,
                                LatestVersion = (decimal)p.LatestVersion,
                                Live = (bool) cp.Live,
                                Status = "TBC",                                     // To do: set status
                                ClientVersion = (decimal)cp.ProductVersion
                            }
                            ).ToList();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error listing clients with product ID" + productID.ToString(), generalException);
                return null;
            }		
        }

        public static List<Clients> ClientsWithoutProduct(bool activeOnly, int productID)
        {
            try
            {
                List<int> clientIDsWithProduct = ClientsWithProduct(false, productID).Select(cwp => (int) cwp.ClientID).ToList();
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return (from c in existingPtDb.Clients
                            where (!activeOnly || c.Active) && !clientIDsWithProduct.Contains(c.ID) && c.EntityID == EntityFunctions.CurrentEntityID
                            orderby c.ClientCode
                            select c).ToList();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error listing clients without product ID" + productID.ToString(), generalException);
                return null;
            }		
        }

        public static List<ClientProductSummary> LinkedProducts(int clientID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return (from p in existingPtDb.Products
                            join cp in existingPtDb.ClientProducts on p.ID equals cp.ProductID
                            join c in existingPtDb.Clients on cp.ClientID equals c.ID
                            where c.ID == clientID
                            orderby p.ProductName
                            select new ClientProductSummary
                            {
                                ID = cp.ID,
                                ClientID = c.ID,
                                ClientName = c.ClientName,
                                ClientEntityID = c.EntityID,
                                ActiveClient = c.Active,
                                ProductID = p.ID,
                                ProductName = p.ProductName,
                                LatestVersion = (decimal)p.LatestVersion,
                                Live = (bool) cp.Live,
                                Status = "TBC",                                     // To do: set status
                                ClientVersion = (decimal)cp.ProductVersion
                            }
                            ).ToList();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error listing products for client ID" + clientID.ToString(), generalException);
                return null;
            }		
        }

        public static List<Products> UnlinkedProducts(int clientID)
        {
            try
            {
                List<int> productIDsForClient = LinkedProducts(clientID).Select(lp => (int)lp.ProductID).ToList();

                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return (from p in existingPtDb.Products
                            where !productIDsForClient.Contains(p.ID)
                            orderby p.ProductName
                            select p).ToList();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error listing products not linked to client ID" + clientID.ToString(), generalException);
                return null;
            }		
        }

        public static bool CanRemoveClientProduct(ref Clients thisClient, int productID)
        {
            // Don't allow if the client has projects with this product
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    try
                    {
                        int clientID = thisClient.ID;
                        int openProjects = (from p in existingPtDb.Projects
                                            join pp in existingPtDb.ProjectProducts on p.ID equals pp.ProjectID                                            
                                            where p.ClientID == clientID && p.ID == productID
                                            select p.ID)
                                            .FirstOrDefault();

                        if (openProjects > 0)
                        {
                            MessageFunctions.InvalidMessage("Cannot remove this product from " + thisClient.ClientName + " as they have projects involving it.", "Projects Found");
                            return false;
                        }
                        else { return true; }
                    }
                    catch (Exception generalException)
                    {
                        MessageFunctions.Error("Error checking for projects with this client", generalException);
                        return false;
                    }
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error checking whether client product can be removed", generalException);
                return false;
            }            
        }

        public static bool ToggleProductClients(List<Clients> affectedClients, bool addition, Products thisProduct)
        {
            try
            {
                int productID = thisProduct.ID;
                string productName = thisProduct.ProductName;

                foreach (Clients thisRecord in affectedClients)
                {
                    int clientID = thisRecord.ID;
                    Clients thisClient = GetClientByID(clientID, false);
                    bool canChange = addition ? true : CanRemoveClientProduct(ref thisClient, productID);

                    if (!canChange) { return false; }
                    else if (addition)
                    {
                        try
                        {
                            ClientProductSummary addRecord = new ClientProductSummary
                                {
                                    ClientID = clientID,
                                    ClientName = thisClient.ClientName,
                                    ClientEntityID = thisClient.EntityID,
                                    ActiveClient = thisClient.Active,
                                    ProductID = thisProduct.ID,
                                    ProductName = thisProduct.ProductName,
                                    LatestVersion = (decimal)thisProduct.LatestVersion,
                                    Live = false,
                                    Status = "New",
                                    ClientVersion = Math.Round((decimal)thisProduct.LatestVersion, 1)
                                };                            
                            ClientsForProduct.Add(addRecord);
                            ClientsNotForProduct.Remove(thisRecord);

                            if (ClientIDsToRemove.Contains(clientID)) { ClientIDsToRemove.Remove(clientID); }
                            else { ClientIDsToAdd.Add(clientID); }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error adding product " + productName + " to client " + thisClient.ClientName, generalException);
                            return false;
                        }
                    }
                    else
                    {
                        try
                        {
                            ClientsNotForProduct.Add(thisRecord);
                            ClientProductSummary removeRecord = ClientsForProduct.FirstOrDefault(cps => cps.ClientID == clientID && cps.ProductID == thisProduct.ID);
                            ClientsForProduct.Remove(removeRecord);

                            if (ClientIDsToAdd.Contains(clientID)) { ClientIDsToAdd.Remove(clientID); }
                            else { ClientIDsToRemove.Add(clientID); }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error removing product " + productName + " from client " + thisClient.ClientName, generalException);
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

        public static bool ToggleClientProducts(List<Products> affectedProducts, bool addition, Clients thisClient)
        {
            try
            {
                int clientID = thisClient.ID;

                foreach (Products thisRecord in affectedProducts)
                {
                    int productID = thisRecord.ID;
                    Products thisProduct = ProductFunctions.GetProductByID (productID);
                    bool canChange = addition ? true : CanRemoveClientProduct(ref thisClient, productID);

                    if (!canChange) { return false; }
                    else if (addition)
                    {
                        try
                        {
                            ClientProductSummary addRecord = new ClientProductSummary
                            {
                                ClientID = thisClient.ID,
                                ClientName = thisClient.ClientName,
                                ClientEntityID = thisClient.EntityID,
                                ActiveClient = thisClient.Active,
                                ProductID = productID,
                                ProductName = thisProduct.ProductName,
                                LatestVersion = (decimal)thisProduct.LatestVersion,
                                Live = false,
                                Status = "New",
                                ClientVersion = Math.Round((decimal)thisProduct.LatestVersion,1)
                            };   
                            ProductsForClient.Add(addRecord);
                            ProductsNotForClient.Remove(thisRecord);

                            if (ProductIDsToRemove.Contains(productID))
                            {
                                ProductIDsToRemove.Remove(productID);
                            }
                            else
                            {
                                ProductIDsToAdd.Add(productID);
                            }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error adding " + thisClient.ClientName + " to product " + thisProduct.ProductName + "", generalException);
                            return false;
                        }
                    }
                    else
                    {
                        try
                        {
                            ProductsNotForClient.Add(thisRecord);
                            ClientProductSummary removeRecord = ProductsForClient.FirstOrDefault(cps => cps.ClientID == thisClient.ID && cps.ProductID == productID);
                            ProductsForClient.Remove(removeRecord);

                            if (ProductIDsToAdd.Contains(productID)) { ProductIDsToAdd.Remove(productID); }
                            else { ProductIDsToRemove.Add(productID); }
                        }
                        catch (Exception generalException)
                        {
                            MessageFunctions.Error("Error removing " + thisClient.ClientName + " from product " + thisProduct.ProductName + "", generalException);
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

        public static bool IgnoreAnyChanges()
        {
            if (ClientIDsToAdd.Count > 0 || ClientIDsToRemove.Count > 0 // || ClientDefaultsToSet.Count > 0
                || ProductIDsToAdd.Count > 0 || ProductIDsToRemove.Count > 0 // || newDefaultID > 0
                )
            {
                return MessageFunctions.QuestionYesNo("This will undo any changes you made since you last saved. Continue?");
            }
            else
            {
                return true;
            }
        }

        public static void ClearAnyChanges()
        {
            ClientIDsToAdd.Clear();
            ClientIDsToRemove.Clear();
//            ClientDefaultsToSet.Clear();
            ProductIDsToAdd.Clear();
            ProductIDsToRemove.Clear();
//            newDefaultID = 0;
        }

        public static bool SaveClientProductChanges(int clientID)
        {
            throw new NotImplementedException();
        }

        public static bool SaveProductClientChanges(int clientID)
        {
            throw new NotImplementedException();
        }

        // Navigation

        public static void ReturnToContactPage(int contactID)
        {            
            PageFunctions.ShowClientContactPage(contactID);
        }

        public static void ReturnToTilesPage()
        {
            SelectedClient = null;
            SourcePage = "TilesPage";
            SourcePageMode = PageFunctions.None;   
            PageFunctions.ShowTilesPage();
        }

    } // class
} // namespace
