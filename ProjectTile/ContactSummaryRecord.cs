namespace ProjectTile
{
    public class ContactSummaryRecord : Globals
    {
        public int ID {get; set; }
        public int ClientID {get; set; }
        public string ContactName {get; set; }
        public string JobTitle {get; set; }
        public string PhoneNumber {get; set; }
        public string Email {get; set; }
        public bool Active {get; set; }     
    }
}
