﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        // Lists //
        List<Entities> entityList = new List<Entities>();
        List<StaffRoles> roleList = new List<StaffRoles>();

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
            refreshEntityList();
            toggleConfirm(false);
            if (!Globals.MyPermissions.Allow("ActivateStaff"))
            {
                ActiveCheckBox.IsEnabled = false;
                ActiveCheckBox.ToolTip = ActiveLabel.ToolTip = "Your current permissions do not allow activating or disabling staff members";
            }      

            try
            {
                pageMode = PageFunctions.pageParameter(this, "Mode");
                selectedStaffID = Int32.Parse(PageFunctions.pageParameter(this, "StaffID"));
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                StaffFunctions.ReturnToStaffPage(selectedStaffID);
            }

            if (pageMode == PageFunctions.New)
            {
                PageHeader.Content = "Create New Staff Member";
                HeaderImage2.SetResourceReference(Image.SourceProperty, "AddIcon");
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
                        displayRole();
                        if (thisStaffMember.StartDate != null) { StartDate.SelectedDate = thisStaffMember.StartDate; }
                        if (thisStaffMember.LeaveDate != null) { LeaveDate.SelectedDate = thisStaffMember.LeaveDate; }
                        displayDefaultEntity();
                        ActiveCheckBox.IsChecked = thisStaffMember.Active;
                        SSOCheckBox.IsChecked = thisStaffMember.SingleSignon;
                        DomainUser.Text = thisStaffMember.OSUser;
                    }

                    catch (Exception generalException) 
                    { 
                        MessageFunctions.Error("Error populating staff member data", generalException);
                        StaffFunctions.ReturnToStaffPage(selectedStaffID);
                    }
                }
                else
                {
                    MessageFunctions.Error("Load error: no staff member loaded.", null);
                    StaffFunctions.ReturnToStaffPage(selectedStaffID);
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
                roleList = StaffFunctions.ListStaffRoles(false);
                RoleCombo.ItemsSource = roleList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating roles list", generalException); }
        }

        private void refreshEntityList()
        {
            try
            {
                entityList = EntityFunctions.EntityList(Globals.MyStaffID, false);
                EntityCombo.ItemsSource = entityList;
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating Entities list", generalException); }
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

        private void displayDefaultEntity()
        {
            try { EntityCombo.SelectedItem = entityList.Find(el => el.ID == (int)thisStaffMember.DefaultEntity); }
            catch (Exception generalException) { MessageFunctions.Error("Error displaying default Entity", generalException); }
        }

        private void displayRole()
        {
            try { RoleCombo.SelectedItem = roleList.Find(rl => rl.RoleCode == thisStaffMember.RoleCode); }
            catch (Exception generalException) { MessageFunctions.Error("Error displaying this staff member's role", generalException); }
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
            StaffFunctions.ReturnToStaffPage(selectedStaffID);
        }

        private void NewPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            toggleConfirm(NewPassword.Password != "");
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //string roleDescription = "";
                string roleCode = "";
                //string defaultEntityName = "";
                int defaultEntityID = 0;
                bool active = (ActiveCheckBox.IsChecked == true);
                string passwd = NewPassword.Password;

                if (RoleCombo.SelectedItem != null) 
                {
                    StaffRoles role = (StaffRoles)RoleCombo.SelectedItem;
                    roleCode = role.RoleCode;
                }
                if (EntityCombo.SelectedItem != null) 
                { 
                    Entities defaultEntity = (Entities) EntityCombo.SelectedItem;
                    defaultEntityID = defaultEntity.ID; 
                }

                if (passwd != "" && passwd != ConfirmPassword.Password)
                {
                    MessageFunctions.InvalidMessage("New password does not match confirmation. Please check both fields and try again.", "Password Mismatch");
                    return;
                }

                int returnID = StaffFunctions.SaveStaffDetails(selectedStaffID, FirstName.Text, Surname.Text, roleCode, StartDate.SelectedDate, LeaveDate.SelectedDate,
                    UserID.Text, passwd, active, defaultEntityID, (SSOCheckBox.IsChecked == true), DomainUser.Text);

                if (returnID > 0)
                {
                    if (selectedStaffID == 0) 
                    { 
                        MessageFunctions.SuccessAlert("New staff member created successfully.", "Staff details saved");
                        selectedStaffID = returnID;
                    }
                    else { MessageFunctions.SuccessAlert("Changes saved successfully.", "Staff member amended"); }
                    StaffFunctions.ReturnToStaffPage(selectedStaffID);
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error saving details", generalException); }
        }

    }
}
