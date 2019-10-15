using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for AuditPage.xaml
    /// </summary>
    public partial class AuditPage : Page
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        // --------- Global/page parameters --------- // 

        //string pageMode;


        // ------------ Current variables ----------- // 

        DateTime fromDate = Globals.InfiniteDate;
        DateTime toDate = Globals.StartOfTime;
        string tableName = "";
        StaffProxy changeBy = Globals.AllStaff;

        // ------------- Current records ------------ //



        // ------------------ Lists ----------------- //
        List<string> tableNames = null;
        List<StaffProxy> staffComboList;
        List<AuditProxy> auditGridList;

        // ---------------------------------------------------------- //
        // -------------------- Page Management --------------------- //
        // ---------------------------------------------------------- //

        // ---------- Initialize and Load ----------- //

        public AuditPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    pageMode = PageFunctions.pageParameter(this, "Mode");
            //}
            //catch (Exception generalException)
            //{
            //    MessageFunctions.Error("Error retrieving query details", generalException);
            //    PageFunctions.ShowTilesPage();
            //}

            FromDate.SelectedDate = Globals.StartOfMonth;
            ToDate.SelectedDate = Globals.Today;
            refreshTableNames();
            refreshStaffCombo();
            FromDate.Focus();
            ToDate.Focus();
            TableCombo.Focus();
            MessageFunctions.InfoMessage("Only records within or relevant to the current Entity (" + Globals.CurrentEntityName + ") are displayed.", "Please note:");
        }



        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- // 		

        private void refreshTableNames()
        {
            tableNames = AdminFunctions.LogTables();
            TableCombo.ItemsSource = tableNames;
            TableCombo.SelectedItem = Globals.PleaseSelect;
        }

        private void refreshStaffCombo()
        {
            staffComboList = StaffFunctions.GetStaffGridData(activeOnly: false, nameContains: "", roleDescription: "", entityID: Globals.CurrentEntityID);
            staffComboList.Insert(0, Globals.AllStaff);
            StaffCombo.ItemsSource = staffComboList;
            StaffCombo.SelectedItem = Globals.AllStaff;
        }

        private void refreshAuditDataGrid()
        {
            try
            {                
                auditGridList = AdminFunctions.DisplayLogEntries(fromDate, toDate, tableName, changeBy.UserID);
                AuditDataGrid.ItemsSource = auditGridList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error displaying audit data", generalException); }	
        }

        // -------------- Data updates -------------- // 



        // --------- Other/shared functions --------- // 



        // ---------- Links to other pages ---------- //		



        // ---------------------------------------------------------- //
        // -------------------- Event Management -------------------- //
        // ---------------------------------------------------------- //  

        // ---- Generic (shared) control events ----- // 		   



        // -------- Control-specific events --------- // 




        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowTilesPage();
        }

        private void AuditDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void StaffCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StaffCombo.SelectedItem != null)
            {
                changeBy = (StaffProxy)StaffCombo.SelectedItem;
                refreshAuditDataGrid();
            }                
        }

        private void TableCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TableCombo.SelectedItem != null)
            {
                tableName = (string) TableCombo.SelectedValue;
                refreshAuditDataGrid();
            }
        }

        private void FromDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FromDate.SelectedDate != null) { fromDate = (DateTime) FromDate.SelectedDate; }
            else { fromDate = Globals.InfiniteDate; }
            refreshAuditDataGrid();
        }

        private void ToDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ToDate.SelectedDate != null) { toDate = (DateTime)ToDate.SelectedDate; }
            else { toDate = Globals.StartOfTime; }
            refreshAuditDataGrid();
        }

    } // class
} // namespace
