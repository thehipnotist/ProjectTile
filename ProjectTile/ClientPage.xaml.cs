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
        TableSecurity myPermissions = LoginFunctions.MyPermissions;
        bool viewContacts;
        bool amendContacts;
        bool viewProducts;
        bool amendProducts;
        bool viewProjects;
        bool amendProjects;
        int editRecordID = 0;
        List<ClientGridRecord> gridList;

        /* Current variables */
        int accountManagerID = 0;

        /* Current records */
        bool activeOnly = false;
        string nameContains = "";
        ClientGridRecord selectedRecord = null;

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

            BackButton.Visibility = Visibility.Hidden;
            EntityWarningLabel.Content = ClientFunctions.EntityWarning;
            SuggestionTips.Text = "";

            if (pageMode == PageFunctions.View)
            {
                ButtonsGrid.Visibility = EditGrid.Visibility = Visibility.Hidden;
                MainClientGrid.Visibility = Visibility.Visible;
                double gridWidth = CentreGrid.ActualWidth - 30;
                ClientDataGrid.Width = gridWidth;
                CommitButton.Visibility = Visibility.Hidden;
                setButtonSecurity();
                refreshMainManagersList();
            }
            else if (pageMode == PageFunctions.New)
            {
                PageHeader.Content = "Create New Client";
                editMode(null);
                EntityWarningLabel.Visibility = Visibility.Hidden;
            }

            else if (pageMode == PageFunctions.Amend)
            {
                PageHeader.Content = "Amend Clients";
                AddButton.Visibility = myPermissions.Allow("AddClients")? Visibility.Visible : Visibility.Hidden;
                setButtonSecurity();
                refreshMainManagersList();
                resetAmendPage();
            }
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
                gridList = ClientFunctions.ClientGridList(activeOnly, nameContains, accountManagerID, EntityFunctions.CurrentEntityID);
                ClientDataGrid.ItemsSource = gridList;
               
                if (selectedRecord != null)
                {
                    try
                    {
                        
                        if (gridList.Exists(c => c.ID == selectedRecord.ID))
                        {
                            ClientDataGrid.SelectedItem = gridList.First(c => c.ID == selectedRecord.ID);
                            ClientDataGrid.ScrollIntoView(ClientDataGrid.SelectedItem);
                        }
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error selecting the current row", generalException); }
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error refreshing client details in the grid", generalException); }
        }

        private void refreshMainManagersList(string accountManager = "")
        {
            try
            {
                string newSelection = PageFunctions.AllRecords;
                string currentSelection = (AMList.SelectedItem != null)? AMList.SelectedItem.ToString() : "";
                List<string> managersList = ClientFunctions.CurrentManagersList(EntityFunctions.CurrentEntityID, true);
                if (currentSelection != "")
                {
                    if(managersList.Contains(currentSelection) && (accountManager == "" || currentSelection == accountManager))
                        { newSelection = currentSelection; } // the previous AM is re-selected only if it matches the current Client record's AM
                    AMList.SelectedItem = newSelection;
                }
                AMList.ItemsSource = managersList;
                AMList.SelectedItem = newSelection;
/*
                if (currentSelection != "" && managersList.Contains(currentSelection) && (accountManager == "" || currentSelection == accountManager)) 
                    { AMList.SelectedItem = currentSelection; } // the previous AM is re-selected only if it matches the current Client record's AM
                else { AMList.SelectedItem = PageFunctions.AllRecords; }
*/ 
 
                //refreshClientGrid(); // Not required as done by the automatic selection change
            }
            catch (Exception generalException) { MessageFunctions.Error("Error refreshing the list of current Account Managers", generalException); }
        }

        private void refreshEditManagersList(bool includeNonAMs, string CurrentManager = "")
        {
            try
            {
                AMList2.ItemsSource = ClientFunctions.AllManagersList(EntityFunctions.CurrentEntityID, includeNonAMs, CurrentManager);
                AMList2.SelectedItem = CurrentManager;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error refreshing the list of available Account Managers", generalException); }
        }

        private void resetAmendPage(string accountManager = "")
        {
            refreshMainManagersList(accountManager);
            CommitButton.Visibility = BackButton.Visibility = Visibility.Hidden;
            MainClientGrid.Visibility = Visibility.Visible;
            ButtonsGrid.Visibility = Visibility.Visible;
            EditGrid.Visibility = Visibility.Hidden;
            PageHeader.Content = "Amend Client Details";
            Instructions.Content = "Choose a client and then click the 'Amend' button to change their details.";
            EntityWarningLabel.Visibility = Visibility.Visible;
        }

        private void toggleButtons(bool haveSelection)
        {
            ContactButton.IsEnabled = ProductButton.IsEnabled = ProjectButton.IsEnabled = haveSelection;
            if (pageMode == PageFunctions.Amend) { AmendButton.IsEnabled = haveSelection; }
        }

        private void nameFilter()
        {
            nameContains = NameContains.Text;
            refreshClientGrid();
        }

        private void setButtonSecurity()
        {
            Visibility shown = Visibility.Visible;
            Visibility hidden = Visibility.Hidden;

            viewContacts = myPermissions.Allow("ViewClientStaff");
            amendContacts = myPermissions.Allow("EditClientStaff");
            ContactButton.Visibility = (viewContacts || amendContacts) ? shown : hidden;

            viewProducts = myPermissions.Allow("ViewClientProducts");
            amendProducts = myPermissions.Allow("EditClientProducts");
            ContactButton.Visibility = (viewProducts || amendProducts) ? shown : hidden;

            viewProjects = myPermissions.Allow("ViewProjects");
            amendProjects = myPermissions.Allow("EditProjects");
            ContactButton.Visibility = (viewProjects || amendProjects) ? shown : hidden;
        }

        private void clearSelection()
        {
            ClientFunctions.SelectedClient = null;
            //selectedRecord = null; // Don't clear this automatically, as the refresh tries to reuse it
            toggleButtons(false);
        }

        private void toggleSuggestionMode(bool clicked)
        {
            Visibility beforeClick = (!clicked)? Visibility.Visible : Visibility.Hidden;
            Visibility afterClick = (clicked) ? Visibility.Visible : Visibility.Hidden;

            SuggestButton.Visibility = beforeClick;
            CodeSuggestionLabel.Visibility = afterClick;
            CodeSuggestion.Visibility = afterClick;
            SuggestionTips.Visibility = afterClick;

            if (!clicked) { ClientFunctions.ClientCodeFormat = ""; }
        }

        private void editMode(ClientGridRecord gridRecord)
        {
            ButtonsGrid.Visibility = MainClientGrid.Visibility = Visibility.Hidden;
            EditGrid.Visibility = Visibility.Visible;
            CommitButton.Visibility = Visibility.Visible;
            NonAMs_CheckBox.IsChecked = false;
            EntityWarningLabel.Visibility = Visibility.Hidden;

            toggleSuggestionMode(false);
                        
            if (gridRecord != null) // Are we amending an existing record?
            {
                editRecordID = gridRecord.ID;
                ClientCode.Text = gridRecord.ClientCode;
                ClientName.Text = gridRecord.ClientName;
                Active_CheckBox.IsChecked = gridRecord.ActiveClient;

                BackButton.Visibility = Visibility.Visible;
                PageHeader.Content = "Amend Client Details";
                Instructions.Content = "Amend the selected record as required and then click 'Save' to apply changes.";
                refreshEditManagersList((bool) NonAMs_CheckBox.IsChecked, gridRecord.ManagerName);
            }
            else
            {
                editRecordID = 0;
                PageHeader.Content = "Create New Client";
                Instructions.Content = "Fill in the details as required and then click 'Save' to create the record.";
                refreshEditManagersList((bool)NonAMs_CheckBox.IsChecked, "");
                if (pageMode == PageFunctions.Amend) 
                { 
                    BackButton.Visibility = Visibility.Visible;
                    ClientCode.Text = ClientName.Text = "";
                    Active_CheckBox.IsChecked = false;
                    AMList2.SelectedItem = "";
                }
            }
        }

        /* ----------------------
           -- Event Management ---
           ---------------------- */

        /* Generic (shared) control events */

        /* Control-specific events */

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
            editMode(selectedRecord);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            editMode(null);
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

        private void ClientDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            try
            {
                if (ClientDataGrid.SelectedItem != null)
                {
                    selectedRecord = (ClientGridRecord)ClientDataGrid.SelectedItem;
                    Clients selectedClient = ClientFunctions.GetClientByID(selectedRecord.ID, true);
                    toggleButtons(selectedClient != null);
                }
                else
                {
                    clearSelection();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error processing client selection", generalException);
                clearSelection();
            }
        }        

        private void NonAMs_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            refreshEditManagersList(true, AMList2.SelectedItem.ToString());
        }

        private void NonAMs_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            refreshEditManagersList(false, AMList2.SelectedItem.ToString());
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // To do: check for changes if appropriate

            PageFunctions.ShowTilesPage();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            resetAmendPage();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            string accountManager = AMList2.SelectedItem.ToString();            
            
            if (editRecordID == 0) // new
            {
                int newID = ClientFunctions.NewClient(ClientCode.Text, ClientName.Text, accountManager, (bool)Active_CheckBox.IsChecked);
                if (newID > 0)
                {
                    if (pageMode == PageFunctions.Amend) 
                    { 
                        MessageFunctions.SuccessMessage("New client '" + ClientName.Text + "' saved successfully.", "Client Created");
                        resetAmendPage(accountManager);
                        refreshClientGrid(); // This is not necessarily done for us by the Account Managers list
                        ClientDataGrid.SelectedValue = gridList.First(c => c.ID == newID);
                        ClientDataGrid.ScrollIntoView(ClientDataGrid.SelectedItem);
                    }
                    else 
                    {
                        MessageFunctions.SuccessMessage("New client '" + ClientName.Text + "' saved successfully.", "Client Created");
                        PageFunctions.ShowTilesPage();
                    }
                    AddButtonText.Text = "Add Another";
                }
            }
            else // amending existing
            {
                bool success = ClientFunctions.AmendClient(editRecordID, ClientCode.Text, ClientName.Text, accountManager, (bool)Active_CheckBox.IsChecked);
                if (success)
                {
                    MessageFunctions.SuccessMessage("Your changes have been saved successfully.", "Client Amended");
                    resetAmendPage(accountManager);  // This is not necessarily done for us by the Account Managers list
                    refreshClientGrid();
                }
            }
        }

        private void SuggestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int avoidID = (selectedRecord != null) ? selectedRecord.ID : 0;

                //MessageBox.Show(avoidID.ToString());

                CodeSuggestion.Text = ClientFunctions.SuggestCode(EntityFunctions.CurrentEntityID, avoidID, "");
                SuggestionTips.Text = ClientFunctions.SuggestionTips;
                CodeSuggestion.ToolTip = ClientFunctions.ExplainCode;

                toggleSuggestionMode(true);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving suggested code", generalException); }	

        }
                

    } // class
} // namespace
