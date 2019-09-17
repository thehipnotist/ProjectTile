namespace ProjectTile
{
    public class ClientProductSummary
    {
        public enum StatusType { Added = 1, New = 2, InProgress = 3, Live = 4, Updates = 5, Inactive = 6, Retired = 7}
        private string status;
        private StatusType statusID;
        
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

        public StatusType StatusID
        {
            get { return statusID; }
            set
            {
                if (value == StatusType.InProgress) { status = "In Progress"; }
                else { status = value.ToString(); };
                statusID = value;
            }
        }
    }
}
