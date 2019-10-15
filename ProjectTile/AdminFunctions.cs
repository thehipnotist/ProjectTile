using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class AdminFunctions : Globals
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        // --------- Global/page parameters --------- // 

        // ------------ Current variables ----------- // 

        // ------------- Current records ------------ //

        // ------------------ Lists ----------------- //

        // ---------------------------------------------------------- //
        // -------------------- Page Management --------------------- //
        // ---------------------------------------------------------- //

        // --------------- Navigation --------------- // 		

        // ------------- Data retrieval ------------- // 	
	
        public static List<AuditProxy> AllLogEntries(DateTime fromDate, DateTime toDate, string tableName, string userID)
        {
            DateTime maxTime = toDate.AddDays(1);
            
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return (from ae in existingPtDb.AuditEntries
                            join s in existingPtDb.Staff on ae.UserName.Replace(DbUserPrefix, "") equals s.UserID
                                into GroupJoin from ss in GroupJoin.DefaultIfEmpty()
                            where ae.ChangeTime >= fromDate && ae.ChangeTime < maxTime
                            && ae.TableName == tableName
                            && (userID == "" || userID == AllCodes || userID == ae.UserName.Replace(DbUserPrefix, ""))
                            orderby ae.ChangeTime descending 
                            select new AuditProxy
                            {
                                ID = ae.ID,
                                ActionType = ae.ActionType,
                                User = ss ?? null,
                                UserName = ae.UserName.Replace(DbUserPrefix, ""),
                                ChangeTime = (DateTime) ae.ChangeTime,
                                TableName = ae.TableName,
                                PrimaryColumn = ae.PrimaryColumn,
                                PrimaryValue = ae.PrimaryValue,
                                ChangeColumn = ae.ChangeColumn,
                                OldValue = ae.OldValue,
                                NewValue = ae.NewValue
                            }
                            ).ToList();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving log entry details", generalException);
                return null;
            }	
        }

        public static List<AuditProxy> DisplayLogEntries(DateTime fromDate, DateTime toDate, string tableName, string userID)
        {
            try
            {
                List<AuditProxy> allEntries = AllLogEntries(fromDate, toDate, tableName, userID);
                return allEntries.Where(ae => ae.EntityID == CurrentEntityID || ae.EntityID == 0).ToList();
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error filtering log entries by Entity", generalException);
                return null;
            }	
        }

        public static List<string> LogTables()
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<string> tables = existingPtDb.AuditEntries.OrderBy(ae => ae.TableName).Select(ae => ae.TableName).Distinct().ToList();
                    tables.Sort();
                    tables.Insert(0, PleaseSelect);
                    return tables;
				}
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving list of auditable tables", generalException);
                return null;
            }	 
        }

        public static TablePermissions GetPermission(int recordID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.TablePermissions.FirstOrDefault(tp => tp.ID == recordID);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving table permission details", generalException);
                return null;
            }
        }

        public static string GetDeletedValue(AuditProxy entry, string columnName)
        {            
            try
            {
                //if (entry.ChangeColumn == columnName) { return entry.OldValue; }
                
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    DateTime changeTime = (DateTime) entry.ChangeTime;
                    DateTime earliest = changeTime.AddMinutes(-1);
                    DateTime latest = changeTime.AddMinutes(1);
                    
                    List<AuditEntries> matchingEntries = existingPtDb.AuditEntries.Where(ae =>
                        ae.TableName == entry.TableName
                        && ae.PrimaryValue == entry.PrimaryValue
                        && ae.UserName.Replace(DbUserPrefix, "") == entry.UserName
                        &&  ae.ChangeTime >= earliest && ae.ChangeTime <= latest
                        && ae.ActionType == entry.ActionType                        
                        ).ToList();

                    if (matchingEntries.Exists(me => me.ChangeColumn == columnName))
                    {
                        return matchingEntries.Where(me => me.ChangeColumn == columnName).Select(me => me.OldValue).FirstOrDefault();
                    }
                    else { return ""; }
				}
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving details of deletion", generalException);
                return null;
            }
        }


        // -------------- Data updates -------------- // 



    } // class
} // namespace
