using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class AuditProxy : Globals
    {
        private const string clientProducts = "ClientProducts";
        private const string clients = "Clients";
        private const string clientStaff = "ClientStaff";
        
        
        public int ID { get; set; }
        public string ActionType { get; set; }
        public Staff User { get; set; }
        public string UserName { get; set; }
        public DateTime ChangeTime { get; set; }
        public string TableName { get; set; }
        public string PrimaryColumn { get; set; }
        public string PrimaryValue { get; set; }
        public string ChangeColumn { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }

        public char AType { get { return Char.Parse(ActionType.Substring(0, 1)); } }

        public int RecordID
        {
            get
            {
                int tryID = 0;
                Int32.TryParse(PrimaryValue, out tryID);
                return tryID;
            }
        }

        public ClientProductProxy ClientProduct
        {
            get
            {
                if (TableName == clientProducts) {return ClientFunctions.GetClientProductProxy(RecordID); }
                else { return null; }
            }
        }

        public ContactProxy ClientStaff
        {
            get
            {
                if (TableName == clientStaff) { return ClientFunctions.GetContactProxy(RecordID); }
                else { return null; }
            }
        }
        
        public int ClientID
        {
            get
            {
                switch (TableName)
                {
                    case clientProducts:
                        return ClientProduct.ClientID;
                    case clients:                        
                        return Client.ID;
                    case clientStaff:
                        return ClientStaff.ClientID;
                    default: 
                        return 0;
                }
            }
        }

        public Clients Client
        {
            get
            {
                switch (TableName)
                {
                    case clientProducts:
                        return ClientFunctions.GetClientByID(ClientID);
                    case clients:
                        return ClientFunctions.GetClientByID(RecordID);
                    case clientStaff:
                        return ClientStaff.Client;
                    default: 
                        return null;
                }
            }
        }

        public int EntityID
        {
            get
            {
                switch (TableName)
                {
                    case clientProducts:
                    case clients:
                    case clientStaff:
                        return Client.EntityID;
                    default:
                        return 0;
                }
            }
        }

        public string RecordDescription 
        {
            get
            { 
                switch (TableName)
                {
                    case clientProducts: 
                        return ClientProduct.ProductName + " for " + Client.ClientName;
                    case clients:
                        return Client.ClientName;
                    case clientStaff:
                        return ClientStaff.ContactName + " at " + Client.ClientName; 
                    default: 
                        return ""; 
                }
            }
        }
        
        
    }
}
