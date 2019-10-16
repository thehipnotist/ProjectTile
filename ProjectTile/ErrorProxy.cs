using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    public class ErrorProxy : Globals
    {
        //public int ID { get; set; }
        public string CustomMessage { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionType { get; set; }
        public string TargetSite { get; set; }
        public DateTime? LoggedAt { get; set; }
        public string LoggedBy { get; set; }
        public Staff User { get; set; }
        public string InnerException { get; set; }

        public string ShortType 
        { 
            get { return ExceptionType.Replace("System.", ""); } 
        }
        public string StaffName
        {
            get { return (User != null) ? User.FullName : ""; }
        }

    } // class
} // namespace
