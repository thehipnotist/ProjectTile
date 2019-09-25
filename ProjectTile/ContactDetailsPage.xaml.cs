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
    /// Interaction logic for ContactDetailsPage.xaml
    /// </summary>
    public partial class ContactDetailsPage : Page
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //

        // Global/page parameters //
        MainWindow winMain = (MainWindow)App.Current.MainWindow;
        string pageMode;
        int selectedContactID = 0;

        // Current variables //

        // Current records //
        ClientStaff thisContact;

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public ContactDetailsPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            FirstName.Focus();
            if (!Globals.MyPermissions.Allow("ActivateClientStaff"))
            {
                Active_CheckBox.IsEnabled = false;
                Active_CheckBox.ToolTip = ActiveLabel.ToolTip = "Your current permissions do not allow activating or disabling contacts";
            }
            try
            {
                pageMode = PageFunctions.pageParameter(this, "Mode");
                selectedContactID = Int32.Parse(PageFunctions.pageParameter(this, "ContactID"));
                //selectedClientID = Int32.Parse(PageFunctions.pageParameter(this, "ClientID"));
                
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                ClientFunctions.ReturnToTilesPage();
            }

            //Clients thisClient = ClientFunctions.GetClientByID(selectedClientID, true);
            ClientCode.Text = Globals.SelectedClient.ClientCode;
            ClientName.Text = Globals.SelectedClient.ClientName;

            if (pageMode == PageFunctions.New)
            {
                PageHeader.Content = "Create New Contact";
                Instructions.Content = "Fill in the details as required and then click 'Save' to create the record.";
            }
            else if (pageMode == PageFunctions.Amend)
            {
                if (selectedContactID > 0)
                {
                    try
                    {
                        thisContact = ClientFunctions.GetContact(selectedContactID);
                        FirstName.Text = thisContact.FirstName;
                        Surname.Text = thisContact.Surname;
                        JobTitle.Text = thisContact.JobTitle;                        
                        Active_CheckBox.IsChecked = thisContact.Active;
                        PhoneNumber.Text = thisContact.PhoneNumber;
                        Email.Text = thisContact.Email;
                    }

                    catch (Exception generalException)
                    {
                        MessageFunctions.Error("Error populating contact data", generalException);
                        ClientFunctions.ReturnToContactPage(selectedContactID);
                    }
                }
                else
                {
                    MessageFunctions.Error("Load error: no contact loaded.", null);
                    ClientFunctions.ReturnToContactPage(selectedContactID);
                }
            }
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Shared functions //
        private void createNewContact()
        {
            int newID = 0;
            try { newID = ClientFunctions.NewContact(FirstName.Text, Surname.Text, JobTitle.Text, PhoneNumber.Text, Email.Text, (bool) Active_CheckBox.IsChecked); }
            catch (Exception generalException) { MessageFunctions.Error("Error creating new contact record", generalException); }
            if (newID > 0)
            {
                MessageFunctions.SuccessMessage("New contact '" + FirstName.Text + " " + Surname.Text + "' saved successfully.", "Contact Created");
                selectedContactID = newID;
                goBack();
            }
        }

        private void saveContactAmend()
        {
            bool success = false;

            try { success = ClientFunctions.AmendContact(selectedContactID, FirstName.Text, Surname.Text, JobTitle.Text, PhoneNumber.Text, Email.Text, 
                (bool)Active_CheckBox.IsChecked); }
            catch (Exception generalException) { MessageFunctions.Error("Error saving amendments to contact", generalException); }
            try
            {
                if (success)
                {
                    MessageFunctions.SuccessMessage("Your changes have been saved successfully.", "Contact Amended");
                    goBack();
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error updating page for saved contact amendments", generalException); }
        }
			
        private void goBack()
        {
            ClientFunctions.ReturnToContactPage(selectedContactID);
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Control-specific events //
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            bool confirm = MessageFunctions.WarningYesNo("This returns back to the application's Main Menu without saving any changes. Are you sure?", "Return to main menu?");
            if (confirm) { ClientFunctions.ReturnToTilesPage(); }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            goBack();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedContactID == 0) { createNewContact(); }
                else { saveContactAmend(); }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error saving details", generalException); }
        }

    }
}
