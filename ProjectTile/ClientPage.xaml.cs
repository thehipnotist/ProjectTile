﻿using System;
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
        int selectedEntityID = EntityFunctions.CurrentEntityID; // This may be changed when copying a record

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
                refreshMainManagersCombo();
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
                refreshMainManagersCombo();
                resetAmendPage();
                ClientDataGrid.SelectionMode = DataGridSelectionMode.Single;
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

        private void refreshMainManagersCombo(string accountManager = "")
        {
            try
            {
                string newSelection = PageFunctions.AllRecords;
                string currentSelection = (MainManagersCombo.SelectedItem != null)? MainManagersCombo.SelectedItem.ToString() : "";
                List<string> managersList = ClientFunctions.CurrentManagersList(EntityFunctions.CurrentEntityID, true);
                if (currentSelection != "")
                {
                    if(managersList.Contains(currentSelection) && (accountManager == "" || currentSelection == accountManager))
                        { newSelection = currentSelection; } // the previous AM is re-selected only if it matches the current Client record's AM
                    MainManagersCombo.SelectedItem = newSelection;
                }
                MainManagersCombo.ItemsSource = managersList;
                MainManagersCombo.SelectedItem = newSelection;
 
                //refreshClientGrid(); // Not required as done by the automatic selection change
            }
            catch (Exception generalException) { MessageFunctions.Error("Error refreshing the list of current Account Managers", generalException); }
        }

        private void refreshEditManagersCombo(bool includeNonAMs, string currentManager = "")
        {
            try
            {
                List<String> managersList = ClientFunctions.AllManagersList(selectedEntityID, includeNonAMs, currentManager);
                EditManagersCombo.ItemsSource = managersList;
                if (currentManager == "" || managersList.Contains(currentManager)) { EditManagersCombo.SelectedItem = currentManager; }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error refreshing the list of available Account Managers", generalException); }
        }

        private void resetAmendPage(string accountManager = "")
        {
            try
            { 
                refreshMainManagersCombo(accountManager);
                CommitButton.Visibility = BackButton.Visibility = Visibility.Hidden;
                MainClientGrid.Visibility = Visibility.Visible;
                ButtonsGrid.Visibility = Visibility.Visible;
                EditGrid.Visibility = Visibility.Hidden;
                PageHeader.Content = "Amend Client Details";
                Instructions.Content = "Choose a client and then click 'Amend' to change their details, or use the other options as required.";
                EntityWarningLabel.Visibility = Visibility.Visible;
                EntityCombo.Visibility = EntityLabel.Visibility = Visibility.Hidden;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error resetting the page", generalException); }
        }

        private void toggleSideButtons(bool haveSelection)
        {
            ContactButton.IsEnabled = ProductButton.IsEnabled = ProjectButton.IsEnabled = haveSelection;
            if (pageMode == PageFunctions.Amend) 
            { 
                AmendButton.IsEnabled = CopyButton.IsEnabled = haveSelection; 
            }
        }

        private void nameFilter()
        {
            nameContains = NameContains.Text;
            refreshClientGrid();
        }

        private void setButtonSecurity()
        {
            try
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
            catch (Exception generalException) { MessageFunctions.Error("Error setting button security", generalException); }
        }

        private void clearSelection()
        {
            ClientFunctions.SelectedClient = null;
            //selectedRecord = null; // Don't clear this automatically, as the refresh tries to reuse it
            toggleSideButtons(false);
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

        private void editMode(ClientGridRecord gridRecord, bool copy = false)
        {
            try
            { 
                ButtonsGrid.Visibility = MainClientGrid.Visibility = Visibility.Hidden;
                EditGrid.Visibility = Visibility.Visible;
                CommitButton.Visibility = Visibility.Visible;
                NonAMs_CheckBox.IsChecked = false;
                EntityWarningLabel.Visibility = Visibility.Hidden;

                toggleSuggestionMode(false);
                        
                if (gridRecord != null) // Amending/copying an existing record
                {
                    try
                    { 
                        if (copy)
                        {
                            editRecordID = 0;
                            PageHeader.Content = "Copy Client Details";
                            Instructions.Content = "Amend the details as required for the new record, then click 'Save' to create it.";
                            EntityCombo.Visibility = EntityLabel.Visibility = Visibility.Visible;
                            refreshEntityList();
                        }
                        else
                        {
                            editRecordID = gridRecord.ID;
                            PageHeader.Content = "Amend Client Details";
                            Instructions.Content = "Amend the selected record as required and then click 'Save' to apply changes.";
                            EntityCombo.Visibility = EntityLabel.Visibility = Visibility.Hidden;
                        }
                
                        ClientCode.Text = gridRecord.ClientCode;
                        ClientName.Text = gridRecord.ClientName;
                        Active_CheckBox.IsChecked = gridRecord.ActiveClient;
                        BackButton.Visibility = Visibility.Visible;
                        refreshEditManagersCombo((bool) NonAMs_CheckBox.IsChecked, gridRecord.ManagerName);
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error setting up the page for client amendments", generalException); }
                }
                else // Brand new record
                {
                    try
                    { 
                        editRecordID = 0;
                        PageHeader.Content = "Create New Client";
                        Instructions.Content = "Fill in the details as required and then click 'Save' to create the record.";
                        EntityCombo.Visibility = EntityLabel.Visibility = Visibility.Hidden;
                        refreshEditManagersCombo((bool)NonAMs_CheckBox.IsChecked, "");
                        if (pageMode == PageFunctions.Amend) 
                        { 
                            BackButton.Visibility = Visibility.Visible;
                            ClientCode.Text = ClientName.Text = "";
                            Active_CheckBox.IsChecked = false;
                            EditManagersCombo.SelectedItem = "";
                        }
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error setting up the page for client creation", generalException); }
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting up the page for editing", generalException); }
        }

        private void refreshEntityList()
        {
            try
            {
                //EntityList.SelectedValue="";
                List<Entities> entityList = EntityFunctions.AllowedEntities(LoginFunctions.CurrentStaffID);
                EntityCombo.ItemsSource = entityList;
                int currentIndex = entityList.FindIndex(el => el.ID == EntityFunctions.CurrentEntity.ID);
                EntityCombo.SelectedValue = entityList.ElementAt(currentIndex);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating Entities list", generalException); }	
        }

        private void suggestFormat()
        {
            try
            {
                int avoidID = (selectedRecord != null) ? selectedRecord.ID : 0;
                string suggestedFormat = ClientFunctions.SuggestCode(selectedEntityID, avoidID, "");
                if (suggestedFormat == "")
                {
                    MessageFunctions.InvalidMessage(
                        "A format cannot be suggested as there are too few active client records in this Entity to analyse. Please view other client records manually if unsure.",
                        "Insufficient Data to Suggest Code Format");
                    if (CodeSuggestion.Visibility == Visibility.Visible) { toggleSuggestionMode(false); } // Possible if copying a record to another Entity
                }
                else
                {
                    CodeSuggestion.Text = suggestedFormat;
                    SuggestionTips.Text = ClientFunctions.SuggestionTips;
                    CodeSuggestion.ToolTip = ClientFunctions.ExplainCode;
                    toggleSuggestionMode(true);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error retrieving suggested code", generalException); }
        }

        private void createNewClient(string accountManagerName)
        {
            int newID = 0;
            bool inCurrentEntity = (selectedEntityID == EntityFunctions.CurrentEntityID);
            string savedInEntity = "";
            string contactsCopied = "";

            try { newID = ClientFunctions.NewClient(ClientCode.Text, ClientName.Text, accountManagerName, (bool)Active_CheckBox.IsChecked, selectedEntityID); }
            catch (Exception generalException) { MessageFunctions.Error("Error creating new client record", generalException); }

            if (newID > 0)
            {
                try
                {
                    if (!inCurrentEntity)
                    {                        
                        if (CopyContacts_CheckBox.IsChecked == true) 
                        { 
                            bool success = ClientFunctions.CopyContacts(selectedRecord.ID, newID);
                            contactsCopied = success ? " and all active linked contacts have been copied to it" : " but contacts could not be copied";
                        }
                        savedInEntity = " in Entity '" + EntityFunctions.GetEntityName(selectedEntityID) + "'" + contactsCopied + ". Switch to that Entity if you need to work with the new record";
                    }

                    MessageFunctions.SuccessMessage("New client '" + ClientName.Text + "' saved successfully" + savedInEntity + ".", "Client Created");
                    if (pageMode == PageFunctions.Amend)
                    {
                        resetAmendPage(accountManagerName);
                        if (inCurrentEntity)
                        {
                            refreshClientGrid(); // This is not necessarily done for us by the Account Managers list
                            ClientDataGrid.SelectedValue = gridList.First(c => c.ID == newID);
                            ClientDataGrid.ScrollIntoView(ClientDataGrid.SelectedItem);
                        }
                        AddButtonText.Text = "Add Another";
                    }
                    else { PageFunctions.ShowTilesPage(); }
                }
                catch (Exception generalException) { MessageFunctions.Error("Error updating page for new client record", generalException); }
            }
        }

        private void saveClientAmend(string accountManagerName)
        {
            bool success = false;
            
            try { success = ClientFunctions.AmendClient(editRecordID, ClientCode.Text, ClientName.Text, accountManagerName, (bool)Active_CheckBox.IsChecked); }
            catch (Exception generalException) { MessageFunctions.Error("Error saving amendments to client", generalException); }
            try
            {
                if (success)
                {
                    MessageFunctions.SuccessMessage("Your changes have been saved successfully.", "Client Amended");
                    resetAmendPage(accountManagerName);  // This is not necessarily done for us by the Account Managers list
                    refreshClientGrid();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error updating page for saved client amendments", generalException); }
        }

        private string getEditAMName()
        {
            return (EditManagersCombo.SelectedItem != null) ? EditManagersCombo.SelectedItem.ToString() : "";
        }

        /* ----------------------
           -- Event Management ---
           ---------------------- */

        /* Generic (shared) control events */

        /* Control-specific events */

        private void MainManagersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedName = MainManagersCombo.SelectedValue.ToString();
            if (selectedName != PageFunctions.AllRecords)
            {
                Staff accountManager = StaffFunctions.GetStaffMemberByName(selectedName);
                accountManagerID = accountManager.ID;
            }
            else { accountManagerID = 0; }
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

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            editMode(selectedRecord, true);
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
                    toggleSideButtons(selectedClient != null);
                }
                else { clearSelection(); }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error processing client selection", generalException);
                clearSelection();
            }
        }        

        private void NonAMs_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            refreshEditManagersCombo(true, EditManagersCombo.SelectedItem.ToString());
        }

        private void NonAMs_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            refreshEditManagersCombo(false, EditManagersCombo.SelectedItem.ToString());
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
            string accountManager = EditManagersCombo.SelectedItem.ToString();

            if (editRecordID == 0) { createNewClient(accountManager); }
            else { saveClientAmend(accountManager); }
        }

        private void SuggestButton_Click(object sender, RoutedEventArgs e)
        {
            suggestFormat();
        }

        private void EntityList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (EntityCombo.SelectedItem == null) { } // Do nothing - it won't be for long 
                else
                {
                    Entities newEntity = (Entities)EntityCombo.SelectedItem;
                    selectedEntityID = newEntity.ID;
                    string accountManager = getEditAMName();
                    refreshEditManagersCombo((bool)NonAMs_CheckBox.IsChecked, accountManager);
                    if (CodeSuggestion.Visibility == Visibility.Visible) { suggestFormat(); }
                    CopyContacts_CheckBox.Visibility = (selectedEntityID == EntityFunctions.CurrentEntityID) ? Visibility.Hidden : Visibility.Visible;
                    CopyContacts_CheckBox.IsChecked = false;
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error handling Entity selection change", generalException); }
        }            

    } // class
} // namespace