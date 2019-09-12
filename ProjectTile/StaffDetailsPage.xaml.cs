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
    /// Interaction logic for StaffDetailsPage.xaml
    /// </summary>
    public partial class StaffDetailsPage : Page
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //

        // Global/page parameters //

        MainWindow winMain = (MainWindow)App.Current.MainWindow;
        string pageMode;
        int selectedStaffID = 0;

        // Current variables //

        // Current records //
        Staff thisStaffMember;

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public StaffDetailsPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            FirstName.Focus();
            CapsLockLabel.Visibility = Visibility.Hidden;
            refreshRoleList();
            EntityList.ItemsSource = EntityFunctions.EntityList(LoginFunctions.CurrentStaffID, false);
            toggleConfirm(false);
            if (!LoginFunctions.MyPermissions.Allow("ActivateStaff"))
            {
                Active_CheckBox.IsEnabled = false;
                Active_CheckBox.ToolTip = ActiveLabel.ToolTip = "Your current permissions do not allow activating or disabling staff members";
            }      

            try
            {
                pageMode = PageFunctions.pageParameter(this, "Mode");
                selectedStaffID = Int32.Parse(PageFunctions.pageParameter(this, "SelectedID"));
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                StaffFunctions.returnToStaffPage(selectedStaffID);
            }

            if (pageMode == PageFunctions.New)
            {
                PageHeader.Content = "Create New Staff Member";
                Instructions.Content = "Fill in the details as required and then click 'Save' to create the record.";
                BackButton.Visibility = Visibility.Hidden;          
            }            
            else if (pageMode == PageFunctions.Amend)
            {
                
                if (selectedStaffID > 0)
                {
                    try
                    {                        
                        thisStaffMember = StaffFunctions.GetStaffMember(selectedStaffID);
                        FirstName.Text = thisStaffMember.FirstName;
                        Surname.Text = thisStaffMember.Surname;
                        UserID.Text = thisStaffMember.UserID;
                        if (thisStaffMember.UserID != null && thisStaffMember.UserID != "") { UserID.IsEnabled = false; }                        
                        RoleList.SelectedItem = StaffFunctions.GetRoleDescription(thisStaffMember.RoleCode);
                        if (thisStaffMember.StartDate != null) { StartDate.SelectedDate = thisStaffMember.StartDate; }
                        if (thisStaffMember.LeaveDate != null) { LeaveDate.SelectedDate = thisStaffMember.LeaveDate; }
                        EntityList.SelectedItem = EntityFunctions.GetEntityName((int) thisStaffMember.DefaultEntity);
                        Active_CheckBox.IsChecked = thisStaffMember.Active;
                    }

                    catch (Exception generalException) 
                    { 
                        MessageFunctions.Error("Error populating staff member data", generalException);
                        StaffFunctions.returnToStaffPage(selectedStaffID);
                    }
                }
                else
                {
                    MessageFunctions.Error("Load error: no staff member loaded.", null);
                    StaffFunctions.returnToStaffPage(selectedStaffID);
                }
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
                RoleList.ItemsSource = StaffFunctions.ListUserRoles(false);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating roles list", generalException); }
        }

        // Shared functions //
        private void check_CapsLock()
        {
            try
            {
                if (Keyboard.IsKeyToggled(Key.CapsLock)) { CapsLockLabel.Visibility = Visibility.Visible; }
                else { CapsLockLabel.Visibility = Visibility.Hidden; }
            }
            catch (Exception generalException) { MessageFunctions.Error("Caps lock error", generalException); }
        }

        private void toggleConfirm(bool show)
        {
            if (show && ConfirmPassword.Visibility == Visibility.Hidden)
            {
                ConfirmLabel.Visibility = Visibility.Visible;
                ConfirmPassword.Visibility = Visibility.Visible;
            }
            else if (!show)
            {
                ConfirmLabel.Visibility = Visibility.Hidden;
                ConfirmPassword.Visibility = Visibility.Hidden;
                ConfirmPassword.Password = "";
            }
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Control-specific events //
        private void Password_Changed(object sender, RoutedEventArgs e)
        {
            //check_CapsLock(); // Removed as this can cause errors on load and isn't needed
            toggleConfirm(NewPassword.Password != "");
        }

        private void Password_KeyUp(object sender, KeyEventArgs e)
        {
            check_CapsLock();
        }

        private void Password_GotFocus(object sender, RoutedEventArgs e)
        {
            check_CapsLock();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowTilesPage();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            StaffFunctions.returnToStaffPage(selectedStaffID);
        }

        private void NewPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            toggleConfirm(NewPassword.Password != "");
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string roleDescription = "";
                string defaultEntityName = "";
                bool active = (Active_CheckBox.IsChecked == true);
                string passwd = NewPassword.Password;

                if (RoleList.SelectedValue != null) { roleDescription = RoleList.SelectedValue.ToString(); }
                if (EntityList.SelectedItem != null) { defaultEntityName = EntityList.SelectedItem.ToString(); }

                if (passwd != "" && passwd != ConfirmPassword.Password)
                {
                    MessageFunctions.InvalidMessage("New password does not match confirmation. Please check both fields and try again.", "Password Mismatch");
                    return;
                }

                int returnID = StaffFunctions.SaveStaffDetails(selectedStaffID, FirstName.Text, Surname.Text, roleDescription, StartDate.SelectedDate, LeaveDate.SelectedDate,
                    UserID.Text, passwd, active, defaultEntityName);

                if (returnID > 0)
                {
                    if (selectedStaffID == 0) 
                    { 
                        MessageFunctions.SuccessMessage("New staff member created successfully.", "Staff details saved");
                        selectedStaffID = returnID;
                    }
                    else { MessageFunctions.SuccessMessage("Changes saved successfully.", "Staff member amended"); }
                    StaffFunctions.returnToStaffPage(selectedStaffID);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error saving details", generalException); }
        }

    }
}
