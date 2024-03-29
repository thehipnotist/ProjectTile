﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        int initialContactID = 0; // Only used for the initial page parameter, to handle returning to this page
        string originalInstructions;
        
        TableSecurity myPermissions = Globals.MyPermissions;
        bool canAmend;
        bool canAdd;
        bool canActivate;

        // Current variables //
        //int accountManagerID = 0;
        bool clientActiveOnly = false;
        string clientContains = "";
        bool contactActiveOnly = false;
        string contactContains = ""; 

        // Current records //
        ClientProxy selectedClientGridRecord = null;
        ContactProxy selectedContactGridRecord = null;
        ClientStaff selectedContact = null;

        // Lists //
        List<ClientProxy> clientGridList;
        List<ContactProxy> contactGridList;
        List<string> contactDropList;

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
                initialContactID = Int32.Parse(PageFunctions.pageParameter(this, "ContactID"));
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                ClientFunctions.ReturnToTilesPage();
            }

            try
            {
                originalInstructions = Instructions.Content.ToString();
                if (pageMode == PageFunctions.View || pageMode == PageFunctions.Lookup) 
                { 
                    canAmend = canAdd = canActivate = false;
                    CommitButton.Visibility = (pageMode == PageFunctions.Lookup)? Visibility.Visible : Visibility.Hidden;
                    CommitButton.Margin = AddButton.Margin;
                    ClientCombo.IsEnabled = (pageMode == PageFunctions.Lookup); 
                }
                else
                {
                    canAmend = myPermissions.Allow("EditClientStaff");
                    canAdd = myPermissions.Allow("AddClientStaff");
                    canActivate = myPermissions.Allow("ActivateClientStaff");
                    originalInstructions = originalInstructions.Replace("View", "View or Amend");
                    CommitButton.Visibility = Visibility.Hidden;
                }

                if (Globals.SelectedClient != null) { contactMode(); }
                else
                {
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
                if (Globals.SelectedClient != null) { selectedID = Globals.SelectedClient.ID; } 
                if (selectedID == 0 && selectedClientGridRecord != null) { selectedID = selectedClientGridRecord.ID; } // Just in case

                clientGridList = ClientFunctions.ClientGridListByContact(clientActiveOnly, clientContains, contactContains, Globals.CurrentEntityID);
                ClientDataGrid.ItemsSource = clientGridList;
                ClientDataGrid.Items.SortDescriptions.Clear();
                ClientDataGrid.Items.SortDescriptions.Add(new SortDescription("ClientCode", ListSortDirection.Ascending));
             
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
                Instructions.Content = originalInstructions;
                refreshClientGrid();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error resetting the page", generalException); }
        }

        private void nameFilter()
        {
            clientContains = ClientContains.Text;
            refreshClientGrid();
        }

        private void contactFilter(bool clientMode)
        {            
            if (clientMode)
            {
                PossibleContacts.Visibility = Visibility.Hidden;
                contactContains = ContactLike.Text;
                refreshClientGrid();                
            }
            else 
            {
                contactContains = ContactContains.Text;
                refreshContactGrid(); 
            }
        }

        private void clearClientSelection()
        {
            Globals.SelectedClient = null;
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
                    else if (canAdd & !canAmend) { Instructions.Content = "Existing contacts are shown for reference. Click 'New Contact' to create a new one."; }
                    else { Instructions.Content = "Click 'New Contact' to create a contact, or choose a record from the list and click 'Amend' to change it."; }
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
                ContactContains.Text = contactContains;
                refreshClientCombo();
            }
            catch (Exception generalException) { MessageFunctions.Error("Error displaying client contacts", generalException); }
        }

        private void refreshContactGrid()
        {
            try
            {
                int selectedID = 0;
                if (initialContactID > 0)
                {
                    selectedID = initialContactID;
                    initialContactID = 0; // Only used at page initiation, so this stops it interfering later
                }
                else if (selectedContactGridRecord != null) { selectedID = selectedContactGridRecord.ID; }

                contactGridList = ClientFunctions.ContactGridList(contactContains, contactActiveOnly, Globals.SelectedClient.ID, includeJob: true);
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
                if (Globals.ClientSourcePage == "ClientPage") // Don't allow changing
                {
                    ClientCombo.IsEnabled = false; // Must come first as used in 'selection changed'                                        
                    ClientCombo.Items.Clear(); // Just in case!
                    ClientCombo.Items.Add(Globals.SelectedClient);
                    ClientCombo.SelectedItem = Globals.SelectedClient;
                }
                else if (selectedClientGridRecord != null) 
                {
                    ClientCombo.ItemsSource = clientGridList;
                    ClientCombo.SelectedItem = selectedClientGridRecord;
                }
                else if (Globals.SelectedClient != null) // Back from the client contact details page
                {
                    refreshClientGrid();                    
                    ClientCombo.Items.Clear(); // Just in case!
                    ClientCombo.ItemsSource = clientGridList;
                    ClientProxy thisRecord = clientGridList.FirstOrDefault(cgl => cgl.ID == Globals.SelectedClient.ID);
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

        private void refreshNamesList()
        {
            string contactLike = ContactLike.Text;
            if (contactLike == "") 
            { 
                PossibleContacts.Visibility = Visibility.Hidden;
                AmendButton.Visibility = Visibility.Hidden;
            }
            else
            {
                PossibleContacts.Visibility = Visibility.Visible;
                contactDropList = ClientFunctions.ContactDropList(contactLike);
                PossibleContacts.ItemsSource = contactDropList;
            }
        }

        private void chooseContactName()
        {
            ContactLike.Text = PossibleContacts.SelectedItem.ToString();
            contactFilter(true);
            checkForSingleContact(); // In case the client selection doesn't change automatically
        }

        private void checkForSingleContact()
        {
            try
            {
                if (ContactLike.Text != "" && (clientGridList.Count == 1 || ClientDataGrid.SelectedItem != null))
                {
                    bool singleContact = chooseSingleContact();
                    if (singleContact)
                    {                                                
                        AmendButton.Visibility = canAmend ? Visibility.Visible : Visibility.Hidden;
                        AmendButton.IsEnabled = canAmend;
                        AmendButton.ToolTip = "Amend " + selectedContact.FullName + "'s details";
                    }
                    else
                    {
                        AmendButton.Visibility = Visibility.Hidden;
                    }
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing contact selection via the drop-down", generalException); }
        }

        private bool chooseSingleContact()
        {
            try
            {
                if (selectedClientGridRecord == null) { ClientDataGrid.SelectedItem = clientGridList.ElementAt(0); }
                selectedContact = ClientFunctions.GetContactByName(selectedClientGridRecord.ID, ContactLike.Text);
                return (selectedContact != null);
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error selecting the displayed contact name", generalException);
                return false;
            }
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Generic (shared) control events //

        // Control-specific events //
        private void ClientContains_LostFocus(object sender, RoutedEventArgs e)
        {
            nameFilter();
        }

        private void ClientContains_KeyUp(object sender, KeyEventArgs e)
        {
            nameFilter();
        }

        private void ActiveClientCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            clientActiveOnly = true;
            refreshClientGrid();
        }

        private void ActiveClientCheckBox_Unchecked(object sender, RoutedEventArgs e)
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
                    selectedClientGridRecord = (ClientProxy)ClientDataGrid.SelectedItem;
                    if (pageMode != PageFunctions.Lookup) { ClientFunctions.SelectClient(selectedClientGridRecord.ID); }
                    ContactButton.IsEnabled = true;
                    checkForSingleContact();
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
            if (pageMode == PageFunctions.Lookup) { ClientFunctions.CancelTeamContactSelection(); }
            else { ClientFunctions.ReturnToTilesPage(); }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.ClientSourcePage == "ClientPage") { PageFunctions.ShowClientPage(pageMode = Globals.ClientSourceMode); }
            else 
            {       
                clientMode();
                //refreshClientGrid(); // Required to process a change of selected client in the drop-down, as doesn't happen automatically
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
            contactFilter(false);
        }

        private void ContactContains_KeyUp(object sender, KeyEventArgs e)
        {
            contactFilter(false);
        }

        private void ActiveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            contactActiveOnly = true;
            refreshContactGrid();
        }

        private void ActiveCheckBox_Unchecked(object sender, RoutedEventArgs e)
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
                        ClientProxy thisRecord = (ClientProxy)ClientCombo.SelectedItem;
                        ClientFunctions.SelectClient(thisRecord.ID);
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
                    selectedContactGridRecord = (ContactProxy) ContactDataGrid.SelectedItem;
                    AmendButton.IsEnabled = true;                    
                    selectedContact = ClientFunctions.GetContact(selectedContactGridRecord.ID);
                    AmendButton.ToolTip = "Amend " + selectedContact.FullName + "'s details";
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

        private void ContactLike_LostFocus(object sender, RoutedEventArgs e)
        {
            contactFilter(true);
        }

        private void ContactLike_KeyUp(object sender, KeyEventArgs e)
        {
            refreshNamesList();
        }

        private void ContactLike_GotFocus(object sender, RoutedEventArgs e)
        {
            refreshNamesList();
        }

        private void PossibleContacts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PossibleContacts.SelectedItem != null) { chooseContactName(); }
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            ClientFunctions.SelectTeamContact(selectedContactGridRecord);
        }

    } // class
} // namespace
