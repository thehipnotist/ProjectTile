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
	
        public static List<AuditProxy> LogEntries(DateTime fromDate, DateTime toDate, string tableName, string userID)
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

        // -------------- Data updates -------------- // 



    } // class
} // namespace
