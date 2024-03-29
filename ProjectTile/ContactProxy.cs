﻿namespace ProjectTile
{
    public class ContactProxy : Globals
    {
        public int ID {get; set; }
        public int ClientID {get; set; }
        public string ContactName {get; set; }
        public string JobTitle {get; set; }
        public string PhoneNumber {get; set; }
        public string Email {get; set; }
        public bool Active {get; set; }     

        public Clients Client 
        {
            get { return (ClientID > 0) ? ClientFunctions.GetClientByID(ClientID) : null; }
        }

        public string NameAndClient
        {
            get { return (Client != null) ? ContactName + " (" + Client.ClientCode + ")" : ContactName;}
        }
    }
}
