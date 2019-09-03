using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using System.Collections;
using System.Collections.Generic;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for StaffPage.xaml
    /// </summary>
    public partial class StaffPage : Page
    {
        /* ----------------------
           -- Global Variables --
           ---------------------- */

        /* Global/page parameters */
        string pageMode;

        /* Current variables */
        bool activeOnly = false;
        string nameContains = "";
        string roleDescription = PageFunctions.AllRecords;
        int selectedStaffID = 0;

        bool viewEntities = LoginFunctions.MyPermissions.Allow("ViewStaffEntities");
        bool editEntities = LoginFunctions.MyPermissions.Allow("EditStaffEntities");

        /* Current records */
        StaffGridRecord selectedRecord;

        /* ----------------------
           -- Page Management ---
           ---------------------- */

        /* Initialize and Load */            
        public StaffPage()
        {
            InitializeComponent();         
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string originalString = NavigationService.CurrentSource.OriginalString;
                pageMode = PageFunctions.pageParameter(originalString, "Mode");
                selectedStaffID = Int32.Parse(PageFunctions.pageParameter(originalString, "SelectedID"));
                refreshRoleList(); // This also runs LoadOrRefreshData to populate the main staff data grid
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error retrieving query details: " + generalException.Message);
                PageFunctions.ShowTilesPage();
            }

            if (pageMode == "View")
            {
                CommitButton.Visibility = Visibility.Hidden;
                DisableButton.Visibility = Visibility.Hidden;
                editEntities = false; // Override as it is a view-only screen
                EntitiesButton.Visibility = (viewEntities) ? Visibility.Visible : Visibility.Hidden;
            }
            else if (pageMode == "Amend")
            {
                PageHeader.Content = "Amend Staff Details";
                Instructions.Content = "Choose a staff member and then click the 'Amend' button to change their details.";
                StaffGrid.SelectionMode = DataGridSelectionMode.Single;
                DisableButton.Visibility = LoginFunctions.MyPermissions.Allow("ActivateStaff")? Visibility.Visible : Visibility.Hidden;

                EntitiesButton.Visibility = (viewEntities || editEntities) ? Visibility.Visible : Visibility.Hidden;
                if (editEntities) { EntitiesButtonText.Text = "Manage Entities"; }                
            }
        }

        /* ----------------------
           -- Data Management ---
           ---------------------- */

        /* Data updates */
        private void refreshRoleList()
        {
            try
            {
                RoleList.ItemsSource = StaffFunctions.ListUserRoles(true);
                RoleList.SelectedItem = PageFunctions.AllRecords;                 
            }
            catch (Exception generalException) { MessageFunctions.ErrorMessage("Error populating role filter list: " + generalException.Message); }
        }

        private void loadOrRefreshData()
        {
            try
            {
                var gridList = StaffFunctions.GetStaffGridData(activeOnly, nameContains, roleDescription, 0);
                StaffGrid.ItemsSource = gridList;
 
                if (selectedStaffID > 0)
                {
                    try
                    {
                        if (gridList.Exists(s => s.ID == selectedStaffID))
                        {
                            StaffGrid.SelectedItem = gridList.First(s => s.ID == selectedStaffID);
                            StaffGrid.ScrollIntoView(StaffGrid.SelectedItem);
                        }
                    }
                    catch (Exception generalException) { MessageFunctions.ErrorMessage("Error selecting record: " + generalException.Message); }
                }                    
            }
            catch (Exception generalException) { MessageFunctions.ErrorMessage("Error filling staff grid: " + generalException.Message); }
        }

        /* Other/shared functions */
        private void nameFilter()
        {
            nameContains = NameContains.Text;
            loadOrRefreshData();
        }

        private void toggleActiveButton(bool? active)
        {
            if (active == true && (DisableImage.Visibility != Visibility.Visible || !DisableButton.IsEnabled))
            {
                DisableButton.IsEnabled = true;
                DisableButtonText.Text = "Disable";
                DisableImage.Visibility = Visibility.Visible;
                EnableImage.Visibility = Visibility.Collapsed;
            }
            else if (active == false && EnableImage.Visibility != Visibility.Visible)
            {
                DisableButton.IsEnabled = true;
                DisableButtonText.Text = "Activate";
                DisableImage.Visibility = Visibility.Collapsed;
                EnableImage.Visibility = Visibility.Visible;
            }
            else if (active == null && DisableButton.IsEnabled)
            {
                DisableButton.IsEnabled = false;
                DisableButtonText.Text = "Disable";
                DisableImage.Visibility = Visibility.Visible;
                EnableImage.Visibility = Visibility.Collapsed;
            }
            else
            {
                // No change needed, already in the right status
            }
        }

        private void clearSelection()
        {
            selectedRecord = null;
            // selectedStaffID = 0; // Don't clear this automatically, as the refresh tries to reuse it
            StaffFunctions.SelectedStaffMember = null; // Ditto
            CommitButton.IsEnabled = false;
            toggleActiveButton(null);
            EntitiesButton.IsEnabled = false;
        }

        /* ----------------------
           -- Event Management ---
           ---------------------- */

        /* Shared functions */
        private void resize()
        {
            double targetWidth = TopBorder.ActualWidth - 30;
            double targetHeight = TopBorder.ActualHeight - 110;

            StaffGrid.Width = targetWidth > StaffGrid.MinWidth ? targetWidth : StaffGrid.MinWidth;
            StaffGrid.Height = targetHeight > StaffGrid.MinHeight ? targetHeight : StaffGrid.MinHeight;
        }        

        /* Control events */
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowTilesPage();
        }

        private void ActiveOnly_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            activeOnly = true;
            loadOrRefreshData();
        }

        private void ActiveOnly_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            activeOnly = false;
            loadOrRefreshData();
        }        

        private void NameContains_LostFocus(object sender, RoutedEventArgs e)
        {
            nameFilter();
        }

        private void NameContains_KeyUp(object sender, KeyEventArgs e)
        {
            nameFilter();
        }

        private void RoleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            roleDescription = RoleList.SelectedValue.ToString();
            loadOrRefreshData();
        }

        private void StaffGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StaffGrid.SelectedItem != null)
                {
                    selectedRecord = (StaffGridRecord) StaffGrid.SelectedItem;
                    selectedStaffID = selectedRecord.ID;
                    StaffFunctions.SelectedStaffMember = StaffFunctions.GetStaffMember(selectedStaffID);
                    CommitButton.IsEnabled = true;
                    toggleActiveButton(StaffFunctions.SelectedStaffMember.Active);
                    EntitiesButton.IsEnabled = true;
                }
                else // No record selected, e.g. because filter changed
                {
                    clearSelection();
                }
            }
            catch (Exception generalException) 
            {
                MessageFunctions.ErrorMessage("Error retrieving record: " + generalException.Message);
                clearSelection();
                selectedStaffID = 0; // Avoid accidentally using the previous selection
            }
        }

        private void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool? blnEnabled = StaffFunctions.EnableOrDisable(selectedStaffID);
                if (blnEnabled != null)
                {
                    loadOrRefreshData();
                }
            }
            catch (Exception generalException) { MessageFunctions.ErrorMessage("Error changing status: " + generalException.Message); }
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StaffFunctions.OpenAmendPage(selectedStaffID);
            }
            catch (Exception generalException)
            {
                MessageFunctions.ErrorMessage("Error amending record: " + generalException.Message);
            }
        }

        private void TopBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // resize(); // Doesn't work in this context
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // resize(); // Doesn't work in this context
        }

        private void EntitiesButton_Click(object sender, RoutedEventArgs e)
        {
            bool viewOnly = (!editEntities);
            PageFunctions.ShowStaffEntitiesPage(selectedStaffID, viewOnly, pageMode);
        }

    } // class
} // namespace
