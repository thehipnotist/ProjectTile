namespace ProjectTile
{
    public class ClientProductSummary
    {
        public int ID { get; set; }
        public int ClientID { get; set; }
        public string ClientName { get; set; }
        public int ClientEntityID { get; set; }
        public bool ActiveClient { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal LatestVersion { get; set; }
        public bool Live { get; set; }
        public string Status { get; set; }
        public decimal ClientVersion { get; set; }
    }
}
