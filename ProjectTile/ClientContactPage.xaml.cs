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
    /// Interaction logic for ClientContactPage.xaml
    /// </summary>
    public partial class ClientContactPage : Page
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //   

        // Global/page parameters //
        string pageMode;
        //string fromSource = "";
        //string sourceMode = "";
        int selectedContactID = 0; // Only used for the initial page parameter, to handle returning to this page
        string originalInstructions;
        
        TableSecurity myPermissions = LoginFunctions.MyPermissions;
        bool canAmend;
        bool canAdd;
        bool canActivate;

        // Current variables //
        int accountManagerID = 0;
        bool clientActiveOnly = false;
        string nameContains = "";
        bool contactActiveOnly = false;
        string contactContains = ""; 

        // Current records //
        ClientGridRecord selectedClientGridRecord = null;
        ContactGridRecord selectedContactGridRecord = null;
        ClientStaff selectedContact = null;

        // Lists //
        List<ClientGridRecord> clientGridList;
        List<ContactGridRecord> contactGridList;

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public ClientContactPage()
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
                //sourceMode = PageFunctions.pageParameter(this, "SourceMode");
                selectedContactID = Int32.Parse(PageFunctions.pageParameter(this, "ContactID"));
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                ClientFunctions.ReturnToTilesPage();
            }

            try
            {
                originalInstructions = Instructions.Content.ToString();
                if (pageMode == PageFunctions.View) { canAmend = canAdd = canActivate = false; }
                else
                {
                    canAmend = myPermissions.Allow("EditClientStaff");
                    canAdd = myPermissions.Allow("AddClientStaff");
                    canActivate = myPermissions.Allow("ActivateClientStaff");
                    originalInstructions = originalInstructions.Replace("View", "View or Amend");
                }

                if (ClientFunctions.SelectedClient != null)
                {
                    contactMode();
                }
                else
                {
                    //fromSource = "TilesPage";
                    EntityWarningLabel.Content = ClientFunctions.ShortEntityWarning;
                    clientMode();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error setting up the client contacts page", generalException);
                ClientFunctions.ReturnToTilesPage();
            }
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //

        // Data retrieval //

        // Other/shared functions //
        private void refreshClientGrid()
        {
            try
            {
                int selectedID = 0;
                if (ClientFunctions.SelectedClient != null) { selectedID = ClientFunctions.SelectedClient.ID; } 
                if (selectedID == 0 && selectedClientGridRecord != null) { selectedID = selectedClientGridRecord.ID; } // Just in case
                
                clientGridList = ClientFunctions.ClientGridList(clientActiveOnly, nameContains, accountManagerID, EntityFunctions.CurrentEntityID);
                ClientDataGrid.ItemsSource = clientGridList;  
             
                try
                {
                    if (selectedID != 0 && clientGridList.Exists(c => c.ID == selectedID))
                    {
                        ClientDataGrid.SelectedItem = clientGridList.First(c => c.ID == selectedID);
                        ClientDataGrid.ScrollIntoView(ClientDataGrid.SelectedItem);
                    }
                }
                catch (Exception generalException) { MessageFunctions.Error("Error selecting the current client row", generalException); }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error refreshing client details in the grid", generalException); }
        }

        private void refreshManagersCombo(string accountManager = "")
        {            
            try
            {
                string newSelection = PageFunctions.AllRecords;
                string currentSelection = (ManagersCombo.SelectedItem != null)? ManagersCombo.SelectedItem.ToString() : "";
                List<string> managersList = ClientFunctions.CurrentManagersList(EntityFunctions.CurrentEntityID, true);
                if (currentSelection != "")
                {
                    if(managersList.Contains(currentSelection) && (accountManager == "" || currentSelection == accountManager))
                        { newSelection = currentSelection; } // the previous AM is re-selected only if it matches the current Client record's AM
                    //ManagersCombo.SelectedItem = newSelection;
                }
                ManagersCombo.ItemsSource = managersList;
                ManagersCombo.SelectedItem = newSelection; 
                //refreshClientGrid(); // Not required as done by the automatic selection change
            }
            catch (Exception generalException) { MessageFunctions.Error("Error refreshing the list of current Account Managers", generalException); }
        }

        private void clientMode()
        {
            try
            {                
                EntityWarningLabel.Visibility = Visibility.Visible;
                ContactGrid.Visibility = Visibility.Hidden;
                ClientGrid.Visibility = Visibility.Visible;
                AmendButton.Visibility = AddButton.Visibility = DisableButton.Visibility = BackButton.Visibility = Visibility.Hidden;
                ContactButton.Visibility = Visibility.Visible;
                ContactButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                refreshManagersCombo();
                Instructions.Content = originalInstructions;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error resetting the page", generalException); }
        }

        private void nameFilter()
        {
            nameContains = ClientContains.Text;
            refreshClientGrid();
        }

        private void contactFilter()
        {
            contactContains = ContactContains.Text;
            refreshContactGrid();
        }

        private void clearClientSelection()
        {
            ClientFunctions.SelectedClient = null;
            //selectedRecord = null; // Don't clear this automatically, as the refresh tries to reuse it
            ContactButton.IsEnabled = false;
        }

        private void clearContactSelection()
        {
            selectedContactGridRecord = null;
            selectedContact = null;
            AmendButton.IsEnabled = DisableButton.IsEnabled = false;
        }

        private void contactMode()
        {
            try
            {                
                if (canAdd || canAmend)
                { 
                    PageHeader.Content = "Manage Client Contacts";
                    if (canAmend && !canAdd) { Instructions.Content = "Choose a contacts from the list and click on 'Amend' to change it."; }
                    else if (canAdd & !canAmend) { Instructions.Content = "Existing contacts are shown for reference. Click 'Add' to create a new one."; }
                    else { Instructions.Content = "Click 'Add' to create a new contact, or choose one from the list and click 'Amend' to change it."; }
                }
                else
                {
                    PageHeader.Content = "View Client Contacts";
                    Instructions.Content = "This page is read-only.";
                }

                EntityWarningLabel.Visibility = Visibility.Hidden;
                ClientGrid.Visibility = ContactButton.Visibility = Visibility.Hidden;                
                BackButton.Visibility = Visibility.Visible;
                AmendButton.Visibility = canAmend? Visibility.Visible : Visibility.Hidden;
                AddButton.Visibility = canAmend ? Visibility.Visible : Visibility.Hidden;
                DisableButton.Visibility = canActivate ? Visibility.Visible : Visibility.Hidden;
                ContactGrid.Visibility = Visibility.Visible;
                refreshClientCombo();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error displaying client contacts", generalException); }
        }

        private void refreshContactGrid()
        {
            try
            {
                int selectedID = 0;
                if (selectedContactID > 0)
                {
                    selectedID = selectedContactID;
                    selectedContactID = 0; // Only used at page initiation, so this stops it interfering later
                }
                else if (selectedContactGridRecord != null) { selectedID = selectedContactGridRecord.ID; }

                contactGridList = ClientFunctions.ContactGridList(contactContains, contactActiveOnly, ClientFunctions.SelectedClient.ID);
                ContactDataGrid.ItemsSource = contactGridList;
                if (selectedID > 0)
                {
                    try
                    {
                        if (contactGridList.Exists(c => c.ID == selectedID))
                        {
                            ContactDataGrid.SelectedItem = contactGridList.First(c => c.ID == selectedID);
                            ContactDataGrid.ScrollIntoView(ContactDataGrid.SelectedItem);
                        }
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error selecting the current contact row", generalException); }
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error refreshing client contact details in the grid", generalException); }
        }

        private void refreshClientCombo()
        {
            try
            {
                if (ClientFunctions.SourcePage == "ClientPage") // Don't allow changing
                {
                    ClientCombo.IsEnabled = false; // Must come first as used in 'selection changed'                                        
                    ClientCombo.Items.Clear(); // Just in case!
                    ClientCombo.Items.Add(ClientFunctions.SelectedClient);
                    ClientCombo.SelectedItem = ClientFunctions.SelectedClient;
                }
                else if (selectedClientGridRecord != null) 
                {
                    ClientCombo.ItemsSource = clientGridList;
                    ClientCombo.SelectedItem = selectedClientGridRecord;
                }
                else if (ClientFunctions.SelectedClient != null) // Back from the client contact details page
                {
                    refreshClientGrid();                    
                    ClientCombo.Items.Clear(); // Just in case!
                    ClientCombo.ItemsSource = clientGridList;
                    ClientGridRecord thisRecord = clientGridList.FirstOrDefault(cgl => cgl.ID == ClientFunctions.SelectedClient.ID);
                    if (thisRecord != null) { ClientCombo.SelectedItem = thisRecord; }
                }                            

                //refreshContactGrid(); // Shouldn't be needed as the selection change does it for us
            }
            catch (Exception generalException) { MessageFunctions.Error("Error refreshing the clients drop-down", generalException); }
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

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Generic (shared) control events //

        // Control-specific events //

        private void ManagersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string selectedName = ManagersCombo.SelectedValue.ToString();
                if (selectedName != PageFunctions.AllRecords)
                {
                    Staff accountManager = StaffFunctions.GetStaffMemberByName(selectedName);
                    accountManagerID = accountManager.ID;
                }
                else { accountManagerID = 0; }
                refreshClientGrid();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error handling the Account Manager selection change", generalException); }
        }

        private void ClientContains_LostFocus(object sender, RoutedEventArgs e)
        {
            nameFilter();
        }

        private void ClientContains_KeyUp(object sender, KeyEventArgs e)
        {
            nameFilter();
        }

        private void ActiveClient_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            clientActiveOnly = true;
            refreshClientGrid();
        }

        private void ActiveClient_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            clientActiveOnly = false;
            refreshClientGrid();
        }

        private void ClientDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            try
            {
                if (ClientDataGrid.SelectedItem != null)
                {
                    selectedClientGridRecord = (ClientGridRecord)ClientDataGrid.SelectedItem;
                    ClientFunctions.GetClientByID(selectedClientGridRecord.ID, true); // Also updates 'master' SelectedClient
                    ContactButton.IsEnabled = true;
                }
                else { clearClientSelection(); }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error processing client grid selection", generalException);
                clearClientSelection();
            }
        }        

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClientFunctions.ReturnToTilesPage();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientFunctions.SourcePage == "ClientPage") { PageFunctions.ShowClientPage(pageMode = ClientFunctions.SourcePageMode); }
            else 
            {       
                clientMode();
                refreshClientGrid(); // Required to process a change of selected client in the drop-down, as doesn't happen automatically
            }
        }

        private void ContactButton_Click(object sender, RoutedEventArgs e)
        {
            contactMode();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowContactDetailsPage(0);
        }

        private void AmendButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowContactDetailsPage(selectedContact.ID);
        }

        private void ContactContains_LostFocus(object sender, RoutedEventArgs e)
        {
            contactFilter();
        }

        private void ContactContains_KeyUp(object sender, KeyEventArgs e)
        {
            contactFilter();
        }

        private void ActiveContact_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            contactActiveOnly = true;
            refreshContactGrid();
        }

        private void ActiveContact_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            contactActiveOnly = false;
            refreshContactGrid();
        }

        private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            try
            {
                if (ClientCombo.IsEnabled) // Otherwise just the one value, already 'selected'
                {
                    if (ClientCombo.SelectedItem != null) // No need for an 'else', won't be long...
                    {
                        ClientGridRecord thisRecord = (ClientGridRecord)ClientCombo.SelectedItem;
                        ClientFunctions.GetClientByID(thisRecord.ID, true);
                    }
                }
                refreshContactGrid();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing client drop-down selection", generalException); }
        }

        private void ContactDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ContactDataGrid.SelectedItem != null)
                {
                    selectedContactGridRecord = (ContactGridRecord) ContactDataGrid.SelectedItem;
                    AmendButton.IsEnabled = true;                    
                    selectedContact = ClientFunctions.GetContact(selectedContactGridRecord.ID);
                    toggleActiveButton(selectedContact.Active);
                }
                else { clearContactSelection(); }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error processing client selection", generalException);
                clearContactSelection();
            }
        }

        private void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool? blnEnabled = ClientFunctions.EnableOrDisable(selectedContact.ID);
                if (blnEnabled != null)
                {
                    refreshContactGrid();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error changing status", generalException); }
        }

    } // class
} // namespace
