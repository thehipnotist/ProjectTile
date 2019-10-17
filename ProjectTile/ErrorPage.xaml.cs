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
    /// Interaction logic for ErrorPage.xaml
    /// </summary>
    public partial class ErrorPage : Page
    {
        // ---------------------------------------------------------- //
        // -------------------- Global Variables -------------------- //
        // ---------------------------------------------------------- //

        // --------- Global/page parameters --------- // 

        //string pageMode;


        // ------------ Current variables ----------- // 

        DateTime fromDate = Globals.InfiniteDate;
        DateTime toDate = Globals.StartOfTime;
        string typeName = "";
        StaffProxy loggedBy = Globals.AllStaff;

        // ------------- Current records ------------ //

        ErrorProxy selectedError = null;

        // ------------------ Lists ----------------- //
        List<string> typeNames = null;
        List<StaffProxy> staffComboList;
        List<ErrorProxy> errorGridList;

        // ---------------------------------------------------------- //
        // -------------------- Page Management --------------------- //
        // ---------------------------------------------------------- //

        // ---------- Initialize and Load ----------- //

        public ErrorPage()
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

            FromDate.SelectedDate = fromDate = Globals.StartOfMonth;
            ToDate.SelectedDate = toDate = Globals.Today;
            refreshTypeNames();
            refreshStaffCombo();
            FromDate.Focus();
            ToDate.Focus();
            TypeCombo.Focus();
        }



        // ---------------------------------------------------------- //
        // -------------------- Data Management --------------------- //
        // ---------------------------------------------------------- //  

        // ------------- Data retrieval ------------- // 		

        private void refreshTypeNames()
        {
            typeNames = AdminFunctions.ErrorTypes();
            TypeCombo.ItemsSource = typeNames;
            TypeCombo.SelectedItem = Globals.AllRecords;
        }

        private void refreshStaffCombo()
        {
            staffComboList = StaffFunctions.GetStaffGridData(activeOnly: false, nameContains: "", roleDescription: "", entityID: Globals.CurrentEntityID);
            staffComboList.Insert(0, Globals.AllStaff);
            StaffCombo.ItemsSource = staffComboList;
            StaffCombo.SelectedItem = Globals.AllStaff;
        }

        private void refreshErrorDataGrid()
        {
            try
            {                
                AdminFunctions.SetErrorLogEntries(fromDate, toDate, typeName, loggedBy.UserID);
                errorGridList = AdminFunctions.ErrorLogEntries.OrderByDescending(ele => ele.LoggedAt).ToList();
                ErrorDataGrid.ItemsSource = errorGridList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error displaying error data", generalException); }	
        }

        // -------------- Data updates -------------- // 



        // --------- Other/shared functions --------- // 

        private void toggleInnerButton(bool enable)
        {
            InnerButton.IsEnabled = enable;
        }

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

        private void ErrorDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ErrorDataGrid.SelectedItem == null) { toggleInnerButton(false); }
            else
            {
                selectedError = (ErrorProxy)ErrorDataGrid.SelectedItem;
                toggleInnerButton(selectedError.InnerException != "");
            }
        }

        private void StaffCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StaffCombo.SelectedItem != null)
            {
                loggedBy = (StaffProxy)StaffCombo.SelectedItem;
                refreshErrorDataGrid();
            }                
        }

        private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypeCombo.SelectedItem != null)
            {
                typeName = (string) TypeCombo.SelectedValue;
                refreshErrorDataGrid();
            }
        }

        private void FromDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FromDate.SelectedDate != null) 
            { 
                fromDate = (DateTime) FromDate.SelectedDate;
                if (toDate != null && toDate < fromDate) { ToDate.SelectedDate = fromDate; }
            }
            else { fromDate = Globals.InfiniteDate; }
            refreshErrorDataGrid();
        }

        private void ToDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ToDate.SelectedDate != null) 
            { 
                toDate = (DateTime)ToDate.SelectedDate;
                if (fromDate != null && fromDate > toDate) { FromDate.SelectedDate = toDate; }
            }
            else { toDate = Globals.StartOfTime; }
            refreshErrorDataGrid();
        }

        private void InnerButton_Click(object sender, RoutedEventArgs e)
        {
            MessageFunctions.InfoBox(selectedError.InnerException, "Inner Exception");
        }

    } // class
} // namespace
