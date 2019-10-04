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
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //

        // Global/page parameters //
        string pageMode;

        // Current variables //
        bool activeOnly = false;
        string nameContains = "";
        string roleDescription = Globals.AllRecords;
        int selectedStaffID = 0;

        bool viewEntities = Globals.MyPermissions.Allow("ViewStaffEntities");
        bool editEntities = Globals.MyPermissions.Allow("EditStaffEntities");
        bool viewProjects = Globals.MyPermissions.Allow("ViewProjects");
        bool editProjects = Globals.MyPermissions.Allow("EditProjects");

        // Current records //
        StaffSummaryRecord selectedRecord;

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //            
        public StaffPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {                
                pageMode = PageFunctions.pageParameter(this, "Mode");
                selectedStaffID = Int32.Parse(PageFunctions.pageParameter(this, "StaffID"));
                refreshRoleList(); // This also runs LoadOrRefreshData to populate the main staff data grid
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                PageFunctions.ShowTilesPage();
            }

            if (pageMode == PageFunctions.View)
            {
                AmendButton.Visibility = CommitButton.Visibility = Visibility.Hidden;
                DisableButton.Visibility = Visibility.Hidden;
                editEntities = false; // Override as it is a view-only screen
                EntitiesButton.Visibility = (viewEntities) ? Visibility.Visible : Visibility.Hidden;
                ProjectButton.Visibility = (viewProjects) ? Visibility.Visible : Visibility.Hidden;
            }
            else if (pageMode == PageFunctions.Amend)
            {
                PageHeader.Content = "Amend Staff Details";
                HeaderImage2.SetResourceReference(Image.SourceProperty, "AmendIcon");
                Instructions.Content = "Choose a staff member and then click the 'Amend' button to change their details.";
                StaffDataGrid.SelectionMode = DataGridSelectionMode.Single;
                DisableButton.Visibility = Globals.MyPermissions.Allow("ActivateStaff") ? Visibility.Visible : Visibility.Hidden;
                CommitButton.Visibility = Visibility.Hidden;
                EntitiesButton.Visibility = (viewEntities || editEntities) ? Visibility.Visible : Visibility.Hidden;
                //if (editEntities) { EntitiesButtonText.Text = "Entities"; }                
                ProjectButton.Visibility = (viewProjects || editProjects) ? Visibility.Visible : Visibility.Hidden;
            }
            else if (pageMode == PageFunctions.Lookup)
            {
                CommitButton.Margin = AmendButton.Margin;
                PageHeader.Content = "Select Staff Member";
                HeaderImage2.SetResourceReference(Image.SourceProperty, "SearchIcon");
                Instructions.Content = "Choose a staff member and then click the 'Select' button to return their details.";
                StaffDataGrid.SelectionMode = DataGridSelectionMode.Single;
                DisableButton.Visibility = AmendButton.Visibility = EntitiesButton.Visibility = ProjectButton.Visibility = Visibility.Hidden;
                CancelButtonText.Text = "Cancel";
                ActiveOnly_CheckBox.IsChecked = true;
            }
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //
        private void refreshRoleList()
        {
            try
            {
                RoleList.ItemsSource = StaffFunctions.ListUserRoles(true);
                RoleList.SelectedItem = Globals.AllRecords;                 
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating role filter list", generalException); }
        }

        private void refreshStaffGrid()
        {
            try
            {
                int entityFilterID = (pageMode == PageFunctions.Lookup) ? Globals.CurrentEntityID : 0;
                List<StaffSummaryRecord> gridList = StaffFunctions.GetStaffGridData(activeOnly, nameContains, roleDescription, entityFilterID);
                StaffDataGrid.ItemsSource = gridList.OrderBy(gl => gl.UserID).OrderBy(gl => gl.StaffName);
 
                if (selectedStaffID > 0)
                {
                    try
                    {
                        if (gridList.Exists(s => s.ID == selectedStaffID))
                        {
                            StaffDataGrid.SelectedItem = gridList.First(s => s.ID == selectedStaffID);
                            StaffDataGrid.ScrollIntoView(StaffDataGrid.SelectedItem);
                        }
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error selecting record", generalException); }
                }                    
            }
            catch (Exception generalException) { MessageFunctions.Error("Error filling staff grid", generalException); }
        }

        // Other/shared functions //
        private void nameFilter()
        {
            nameContains = NameContains.Text;
            refreshStaffGrid();
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
            pushSelection();
            AmendButton.IsEnabled = EntitiesButton.IsEnabled = ProjectButton.IsEnabled = false;
            toggleActiveButton(null);
        }

        private void pushSelection()
        {
            try
            {
                if (pageMode != PageFunctions.Lookup) { Globals.SelectedStaffMember = (selectedRecord == null) ? null : StaffFunctions.GetStaffMember(selectedRecord.ID); }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing selection", generalException); }	
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Shared functions //
        private void resize()
        {
            double targetWidth = TopBorder.ActualWidth - 30;
            double targetHeight = TopBorder.ActualHeight - 110;

            StaffDataGrid.Width = Math.Max(targetWidth, StaffDataGrid.MinWidth);
            StaffDataGrid.Height = Math.Max(targetHeight, StaffDataGrid.MinHeight);
        }        

        // Control events //

        private void ActiveOnly_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            activeOnly = true;
            refreshStaffGrid();
        }

        private void ActiveOnly_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            activeOnly = false;
            refreshStaffGrid();
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
            refreshStaffGrid();
        }

        private void StaffGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StaffDataGrid.SelectedItem != null)
                {
                    selectedRecord = (StaffSummaryRecord) StaffDataGrid.SelectedItem;
                    selectedStaffID = (selectedRecord == null)? 0 : selectedRecord.ID;
                    pushSelection();
                    toggleActiveButton(selectedRecord.Active);
                    AmendButton.IsEnabled = EntitiesButton.IsEnabled = ProjectButton.IsEnabled = true;
                }
                else // No record selected, e.g. because filter changed
                {
                    clearSelection();
                }
            }
            catch (Exception generalException) 
            {
                MessageFunctions.Error("Error retrieving record", generalException);
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
                    refreshStaffGrid();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error changing status", generalException); }
        }

        private void AmendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StaffFunctions.OpenAmendPage(selectedStaffID);
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error amending record", generalException);
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (pageMode == PageFunctions.Lookup) { StaffFunctions.CancelTeamStaffSelection(); }
            else { PageFunctions.ShowTilesPage(); }
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            StaffFunctions.SelectTeamStaff(selectedRecord);
        }

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectTeamsPage(pageMode, selectedStaffID, "StaffPage");
        }

    } // class
} // namespace
