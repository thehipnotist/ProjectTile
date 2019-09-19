namespace ProjectTile
{
    public class ClientProductSummary : Globals
    {        
        private string status;
        private ClientProductStatus statusID;
        
        public int ID { get; set; }
        public int ClientID { get; set; }
        public string ClientName { get; set; }
        public int ClientEntityID { get; set; }
        public bool ActiveClient { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal LatestVersion { get; set; }
        public bool Live { get; set; }
        public string Status { get { return status; } }
        public decimal ClientVersion { get; set; }

        public ClientProductStatus StatusID
        {
            get { return statusID; }
            set
            {
                if (value == ClientProductStatus.InProgress) { status = "In Progress"; }
                else { status = value.ToString(); };
                statusID = value;
            }
        }
    }
}
