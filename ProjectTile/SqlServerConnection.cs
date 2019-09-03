using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Data.Entity.Core.EntityClient;

namespace ProjectTile
{
    public class SqlServerConnection
    {
	    private static string savedUser;
        private static string savedPassword;
        
        /*
        public static SqlConnection Connection (string strUserID, string strPassword)
	    {
            
            SqlConnection DBConnection = new SqlConnection();

            SqlConnectionStringBuilder SqlBuilder = new SqlConnectionStringBuilder();
            SqlBuilder.DataSource = ".\\SQLExpress";
            SqlBuilder.InitialCatalog = "ProjectTile";
            //SqlBuilder.IntegratedSecurity = true;
            SqlBuilder.UserID = strUserID;
            SqlBuilder.Password = strPassword;
            DBConnection.ConnectionString = SqlBuilder.ConnectionString;

            return DBConnection;
        }
        */

        public static ProjectTileSqlDatabase DefaultPtDbConnection()
        {
            try
            {
                ProjectTileSqlDatabase dB = new ProjectTileSqlDatabase();
                return dB;
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error opening default SQL connection " + generalException.Message);
                return null;
            }
        }

        public static ProjectTileSqlDatabase UserPtDbConnection (string userID, string password)
        {
            SqlConnectionStringBuilder SqlBuilder = new SqlConnectionStringBuilder();
            SqlBuilder.DataSource = ".\\SQLExpress";
            SqlBuilder.InitialCatalog = "ProjectTile";
            SqlBuilder.IntegratedSecurity = false;
            SqlBuilder.UserID = userID;
            SqlBuilder.Password = password;
            SqlBuilder.PersistSecurityInfo = true;
            SqlBuilder.MultipleActiveResultSets = true;

            string connectionString = SqlBuilder.ToString();

            EntityConnectionStringBuilder ecsBuilder = new EntityConnectionStringBuilder();
            ecsBuilder.Provider = "System.Data.SqlClient";
            ecsBuilder.ProviderConnectionString = connectionString;

            ecsBuilder.Metadata = @"res://*/ProjectTileDataModel.csdl|res://*/ProjectTileDataModel.ssdl|res://*/ProjectTileDataModel.msl";

            connectionString = ecsBuilder.ToString();

            try
            {
                ProjectTileSqlDatabase dB = new ProjectTileSqlDatabase(connectionString);
                savedUser = userID;       
                savedPassword = password;
                return dB;
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error opening SQL connection for user " + userID + ": " + generalException.Message);
                return null;
            }
        }

        public static ProjectTileSqlDatabase ExistingPtDbConnection()
        {
            try
            {
                ProjectTileSqlDatabase dB = UserPtDbConnection(savedUser, savedPassword);
                return dB;
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error using existing SQL connection: " + generalException.Message);
                return null;
            }
        }

    }

    public partial class ProjectTileSqlDatabase : DbContext
    {
        public ProjectTileSqlDatabase(String connString)
            : base(connString)
        {
            // Required to extend (via overload) the DbContext so that the connection string can be overridden
        }

    }
}
