namespace ProjectTile

{
    public class ClientSummaryRecord : Globals
    {
        public int ID { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public int ManagerID { get; set; }
        public string ManagerName { get; set; }
        public bool ActiveClient { get; set; }
        public int EntityID { get; set; }
        public string EntityName { get; set; }
    }
}
