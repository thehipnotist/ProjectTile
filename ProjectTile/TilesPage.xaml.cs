﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for TilesPage.xaml
    /// </summary>
    public partial class TilesPage : Page
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //

        // Global/page parameters //		        
        MainWindow winMain = (MainWindow)App.Current.MainWindow;
        bool keepExpansion;
        TableSecurity myPermissions = Globals.MyPermissions;
        Thickness fatBorder = new Thickness (5, 5, 5, 5);
        Thickness thinBorder = new Thickness (1, 1, 1, 1); 

        // Create empty lists of associated buttons that are populated when loaded (below)
        // This is the most efficient way to show and hide only the necessary buttons without having to loop through all of them
        List<Button> activeButtons;
        List<Button> mainButtons = new List<Button>();

        List<Button> entityButtons = new List<Button>();
        List<Button> loginButtons = new List<Button>();
        List<Button> staffButtons = new List<Button>();
        List<Button> productButtons = new List<Button>();
        List<Button> clientButtons = new List<Button>();
        List<Button> projectButtons = new List<Button>();
        List<Button> helpButtons = new List<Button>();
        List<Button> adminButtons = new List<Button>();

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //        
        
        public TilesPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
        }  

        private void TilesPage_Loaded(object sender, RoutedEventArgs e)
        {
            addAndGroupButtons();

            string demoCo = "";
            if (Globals.CurrentEntityID == 1) // Logged into SampleCo, possibly a new user
            {
                demoCo = ", a demonstration company";
                if (LoginFunctions.FirstLoad)
                {
                    Instructions.Content = "To create a new empty Entity, choose the Entity menu or 'Entity' tile below, then 'Create New Entity'.";
                    Instructions.Visibility = myPermissions.Allow("AddEntities") ? Visibility.Visible : Visibility.Hidden;
                    LoginFunctions.FirstLoad = false;
                }
                else
                {
                    Instructions.Visibility = Visibility.Visible;
                }
            }
            string staffName = (Globals.MyUserID == "pjadmin") ? Globals.MyName : Globals.MyStaffRecord.FirstName;
            Welcome.Content = String.Format("Welcome to ProjectTile, {0}. You are logged into Entity {1}{2}.", staffName, Globals.CurrentEntityName, demoCo);
        }

        // Page changes //
        private void addAndGroupButtons()
        {
            try
            {
                mainButtons.Add(EntityButton);
                mainButtons.Add(LoginButton);
                mainButtons.Add(StaffButton);
                mainButtons.Add(ProductButton);
                mainButtons.Add(ClientButton);
                mainButtons.Add(ProjectButton);
                mainButtons.Add(HelpButton);
                mainButtons.Add(AdminButton);

                entityButtons.Add(EntityButton_Switch);
                entityButtons.Add(EntityButton_Default);

                if (myPermissions.Allow("AddEntities")) { entityButtons.Add(EntityButton_New); }
                else { EntityButton_Amend.Margin = EntityButton_New.Margin; }
                if (myPermissions.Allow("EditEntities")) { entityButtons.Add(EntityButton_Amend); }

                loginButtons.Add(LoginButton_Login);
                loginButtons.Add(LoginButton_Password);
                loginButtons.Add(LoginButton_Exit);

                if (myPermissions.Allow("ViewStaff")) { staffButtons.Add(StaffButton_View); }
                if (myPermissions.Allow("AddStaff")) { staffButtons.Add(StaffButton_New); }
                else { StaffButton_Amend.Margin = StaffButton_New.Margin; }
                if (myPermissions.Allow("EditStaff")) { staffButtons.Add(StaffButton_Amend); }
                if (myPermissions.Allow("ViewStaffEntities")) { staffButtons.Add(StaffButton_Entities); }

                if (myPermissions.Allow("ViewProducts")) { productButtons.Add(ProductButton_View); }
                if (myPermissions.Allow("AddProducts")) { productButtons.Add(ProductButton_New); }
                else { ProductButton_Amend.Margin = ProductButton_New.Margin; }
                if (myPermissions.Allow("EditProducts")) { productButtons.Add(ProductButton_Amend); }

                if (myPermissions.Allow("ViewClients")) { clientButtons.Add(ClientButton_View); }
                else { ClientButton_Contact.Margin = ClientButton_View.Margin; }
                if (myPermissions.Allow("AddClients")) { clientButtons.Add(ClientButton_New); }
                else { ClientButton_Amend.Margin = ClientButton_New.Margin; }
                if (myPermissions.Allow("EditClients")) { clientButtons.Add(ClientButton_Amend); }
                if (myPermissions.Allow("ViewClientStaff")) { clientButtons.Add(ClientButton_Contact); }
                if (myPermissions.Allow("ViewClientProducts")) { clientButtons.Add(ClientButton_Product); }

                if (myPermissions.Allow("ViewProjects")) { projectButtons.Add(ProjectButton_View); }
                if (myPermissions.Allow("AddProjects")) { projectButtons.Add(ProjectButton_New); }
                else 
                {
                    ProjectButton_Action.Margin = ProjectButton_Amend.Margin;
                    ProjectButton_Amend.Margin = ProjectButton_New.Margin;
                }
                if (myPermissions.Allow("EditProjects")) { projectButtons.Add(ProjectButton_Amend); }
                if (myPermissions.Allow("ViewProjectTeams")) { projectButtons.Add(ProjectButton_Staff); }
                if (myPermissions.Allow("ViewProjectProducts")) { projectButtons.Add(ProjectButton_Product); }
                else { ProjectButton_Contact.Margin = ProjectButton_Product.Margin; }
                if (myPermissions.Allow("ViewClientTeams")) { projectButtons.Add(ProjectButton_Contact); }
                if (myPermissions.Allow("ViewStageHistory")) { projectButtons.Add(ProjectButton_Timeline); }
                if (myPermissions.Allow("ViewActions")) { projectButtons.Add(ProjectButton_Action); }

                helpButtons.Add(HelpButton_About);
                helpButtons.Add(HelpButton_FAQ);

                if (myPermissions.Allow("ViewAuditEntries")) { adminButtons.Add(AdminButton_Audit); }
                if (myPermissions.Allow("ViewErrorLog")) { adminButtons.Add(AdminButton_Error); }

                if (staffButtons.Count == 0) 
                { 
                    StaffButton.IsEnabled = false;
                    StaffText.Text = StaffText.Text + "\n (Disabled)";
                }
                if (productButtons.Count == 0)
                {
                    ProductButton.IsEnabled = false;
                    ProductText.Text = ProductText.Text + "\n (Disabled)";
                }
                if (clientButtons.Count == 0)
                {
                    ClientButton.IsEnabled = false;
                    ClientText.Text = ClientText.Text + "\n (Disabled)";
                }
                if (projectButtons.Count == 0)
                {
                    ProjectButton.IsEnabled = false;
                    ProjectText.Text = ProjectText.Text + "\n (Disabled)";
                }
                if (adminButtons.Count == 0)
                {
                    AdminButton.IsEnabled = false;
                    AdminText.Text = AdminText.Text + "\n (Disabled)";
                }

            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting tile permissions", generalException); }
        }

        private void toggleMainButtons(Button thisButton, bool showItem)
        {
            foreach (Button aButton in mainButtons)
            {
                if (aButton != thisButton)
                {
                    aButton.BorderThickness = thinBorder;
                    if (showItem) { aButton.Opacity = 1.0; }
                    else { aButton.Opacity = 0.2; }
                }
            }
        }

        private void toggleOtherButtons(Button thisButton, bool showItem, ref List<Button> buttonList)
        {
            Visibility vToggle = Visibility.Hidden; // Set this now for efficiency
            if (showItem) { vToggle = Visibility.Visible; }
            
            foreach (Button aButton in buttonList)
            {
                if (aButton != thisButton) {aButton.Visibility = vToggle; }
            }
        }
        
        private void toggleChildButtons (bool showItem, ref List<Button> buttonList)
        {
            toggleOtherButtons(null, showItem, ref buttonList);
            if (showItem) { activeButtons = buttonList; }
            else { activeButtons = null; }
        }

        private void expandButton(ref Button thisButton)
        {
            if (keepExpansion) { return; }
            
            if (thisButton is Button)
            {
                try
                {
                    toggleMainButtons(thisButton, false);

                    if (thisButton.Name == "EntityButton") { toggleChildButtons(true, ref entityButtons); }
                    else if (thisButton.Name == "LoginButton") { toggleChildButtons(true, ref loginButtons); }
                    else if (thisButton.Name == "StaffButton") { toggleChildButtons(true, ref staffButtons); }
                    else if (thisButton.Name == "ProductButton") { toggleChildButtons(true, ref productButtons); }
                    else if (thisButton.Name == "ClientButton") { toggleChildButtons(true, ref clientButtons); }
                    else if (thisButton.Name == "ProjectButton") { toggleChildButtons(true, ref projectButtons); }
                    else if (thisButton.Name == "HelpButton") { toggleChildButtons(true, ref helpButtons); }
                    else if (thisButton.Name == "AdminButton") { toggleChildButtons(true, ref adminButtons); }
                }
                catch
                {
                    // Do nothing
                }
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {            
            if (sender is Button)
            {
                Button thisButton = (Button)sender;
                thisButton.BorderThickness = fatBorder;                    
                keepExpansion = true;                   
            }
        }

        private void buttonGotFocus(object sender, RoutedEventArgs e)
        {            
            if (sender is Button)
            {            
                Button thisButton = (Button)sender;
                thisButton.BorderThickness = fatBorder;
                expandButton(ref thisButton);
                keepExpansion = true;
            }            
        }

        private void buttonLostFocus(object sender, RoutedEventArgs e)
        {                        
            keepExpansion = false;
            movedElsewhere();             
        }

        private void movedElsewhere()
        {
            if (keepExpansion) { return; }            
            try
            {
                bool resetButtons = false;
                if (activeButtons != null)
                {                   
                    resetButtons = true;                    
                    foreach (Button aButton in activeButtons)
                    {
                        if ((aButton.Visibility == Visibility.Visible) && (aButton.IsMouseOver))
                        {
                            resetButtons = false;
                        }
                    }
                }                
                
                if (resetButtons)
                {
                    toggleMainButtons(null, true);
                    toggleChildButtons(false, ref activeButtons);
                }    
            }

            catch
            {
                // Do nothing
            }
        }

        private void mouseOffButtons(object sender, MouseButtonEventArgs e)
        {
            keepExpansion = false;
            movedElsewhere();
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Generic (shared) events //
        private void buttonMouseOver(object sender, MouseEventArgs e)
        {
            if (sender is Button)
            {
                Button thisButton = (Button)sender;
                expandButton(ref thisButton);
            }
        }

        private void buttonMouseOut(object sender, MouseEventArgs e)
        {
            if (sender is Button) { movedElsewhere(); }
        }

        // Control-specific events //
        private void EntityButton_Switch_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage(PageFunctions.Switch);
        }

        private void EntityButton_New_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage(PageFunctions.New);
        }

        private void EntityButton_Amend_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage(PageFunctions.Amend);
        }

        private void EntityButton_Default_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage(PageFunctions.Default);
        }

        private void LoginButton_Login_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowLoginPage(PageFunctions.LogIn);
        }

        private void LoginButton_Password_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowLoginPage(PageFunctions.PassChange);
        }

        private void LoginButton_Exit_Click(object sender, RoutedEventArgs e)
        {
            winMain.Close();
        }

        private void StaffButton_View_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffPage(PageFunctions.View);
        }

        private void StaffButton_Amend_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffPage(PageFunctions.Amend);
        }

        private void StaffButton_New_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffDetailsPage(PageFunctions.New, 0);
        }

        private void StaffButton_Entities_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffEntitiesPage();
        }

        private void ProductButton_View_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage(PageFunctions.View);
        }

        private void ProductButton_New_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage(PageFunctions.New);
        }

        private void ProductButton_Amend_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage(PageFunctions.Amend);
        }

        private void ClientButton_View_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientPage(PageFunctions.View);
        }

        private void ClientButton_New_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientPage(PageFunctions.New);
        }

        private void ClientButton_Amend_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientPage(PageFunctions.Amend);
        }

        private void ClientButton_Contact_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientContactPage();
        }

        private void ClientButton_Product_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientProductsPage();
        }

        private void ProjectButton_View_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectPage(PageFunctions.View);
        }

        private void ProjectButton_New_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectDetailsPage(PageFunctions.New);
        }

        private void ProjectButton_Amend_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectPage(PageFunctions.Amend);
        }

        private void ProjectButton_Contact_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectContactsPage();
        }

        private void ProjectButton_Product_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectProductsPage();
        }

        private void ProjectButton_Staff_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProjectTeamsPage();
        }

        private void ProjectButton_Timeline_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStageHistoryPage();
        }

        private void ProjectButton_Action_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowActionsPage();
        }        

        private void HelpButton_About_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowAboutPage();
        }

        private void HelpButton_FAQ_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowFAQPage();
        }

        private void AdminButton_Audit_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowAuditPage();
        }

        private void AdminButton_Error_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowErrorPage();
        }


    } // class
} // namespace
