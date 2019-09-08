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
    /// Interaction logic for ClientPage.xaml
    /// </summary>
    public partial class ClientPage : Page
    {
        /* ----------------------
           -- Global Variables --
           ---------------------- */   

        /* Global/page parameters */
        string pageMode;

        /* Current variables */
        int accountManagerID = 0;

        /* Current records */
        bool activeOnly = false;
        string nameContains = "";        

        /* ----------------------
           -- Page Management ---
           ---------------------- */

        /* Initialize and Load */
        public ClientPage()
        {
            InitializeComponent();           
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string originalString = NavigationService.CurrentSource.OriginalString;
                pageMode = PageFunctions.pageParameter(originalString, "Mode");

            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                PageFunctions.ShowTilesPage();
            }

            //CommitButton.Visibility = Visibility.Hidden;
            CommitButton.IsEnabled = false;
            refreshManagersList();

            if (pageMode == "View")
            {
                AmendmentsGrid.Visibility = Visibility.Hidden;
                double gridWidth = CentreGrid.ActualWidth - 30;
                ClientGrid.Width = gridWidth;
            }
            else if (pageMode == "New")
            {
                PageHeader.Content = "Create New Client";
            }

            else if (pageMode == "Amend")
            {
                PageHeader.Content = "Amend Clients";
            }
        }

        private void nameFilter()
        {
            nameContains = NameContains.Text;
            refreshClientGrid();
        }

        /* ----------------------
           -- Data Management ---
           ---------------------- */

        /* Data updates */

        /* Data retrieval */

        /* Other/shared functions */
        private void refreshClientGrid()
        {
            try
            {
                ClientGrid.ItemsSource = ClientFunctions.ClientGridList(activeOnly, nameContains, accountManagerID, EntityFunctions.CurrentEntityID);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error refreshing client details in the grid", generalException); }
        }

        private void refreshManagersList()
        {
            try
            {
                AMList.ItemsSource = ClientFunctions.ManagersList(EntityFunctions.CurrentEntityID, true);
                AMList.SelectedItem = PageFunctions.AllRecords;
                refreshClientGrid();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error refreshing the list of Account Managers", generalException); }		
        }

        /* ----------------------
           -- Event Management ---
           ---------------------- */

        /* Generic (shared) control events */

        /* Control-specific events */




        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // To do: check for changes if appropriate

            PageFunctions.ShowTilesPage();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AMList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedName = AMList.SelectedValue.ToString();
            if (selectedName != PageFunctions.AllRecords)
            {
                Staff accountManager = StaffFunctions.GetStaffMemberByName(selectedName);
                accountManagerID = accountManager.ID;
            }
            else
            {
                accountManagerID = 0;
            }
            refreshClientGrid();
        }

        private void NameContains_LostFocus(object sender, RoutedEventArgs e)
        {
            nameFilter();
        }

        private void NameContains_KeyUp(object sender, KeyEventArgs e)
        {
            nameFilter();
        }

        private void ActiveOnly_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            activeOnly = true;
            refreshClientGrid();
        }

        private void ActiveOnly_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            activeOnly = false;
            refreshClientGrid();
        }

        private void AmendButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ProductButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ContactButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {

        }



    } // class
} // namespace
