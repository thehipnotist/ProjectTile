using System;
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
        /* ----------------------
           -- Global Variables --
           ---------------------- */

        /* Global/page parameters */		        
        MainWindow winMain = (MainWindow)App.Current.MainWindow;
        bool keepExpansion;
        
        Thickness fatBorder = new Thickness (5, 5, 5, 5);
        Thickness thinBorder = new Thickness (1, 1, 1, 1); 

        /* Current records */
        TableSecurity myPermissions; 

        // Create empty lists of associated buttons that are populated when loaded (below)
        // This is the most efficient way to show and hide only the necessary buttons without having to loop through all of them
        List<Button> activeButtons;
        List<Button> mainButtons = new List<Button>();

        List<Button> entityButtons = new List<Button>();
        List<Button> adminButtons = new List<Button>();
        List<Button> staffButtons = new List<Button>();
        List<Button> productButtons = new List<Button>();
        List<Button> clientButtons = new List<Button>();

        /* ----------------------
           -- Page Management ---
           ---------------------- */

        /* Initialize and Load */        
        
        public TilesPage()
        {
            InitializeComponent();
            myPermissions = LoginFunctions.MyPermissions;

            string demoCo = "";

            if (EntityFunctions.CurrentEntityID == 1) // Logged into SampleCo, possibly a new user
            {
                demoCo = ", a demonstration company";
                if (LoginFunctions.FirstLoad)
                {
                    Instructions.Content = "To create a new Entity to work in, go to the Entity menu or hover over 'Entity' below, and choose 'Create New Entity'.";
                    Instructions.Visibility = myPermissions.Allow("AddEntities") ? Visibility.Visible : Visibility.Hidden;
                    LoginFunctions.FirstLoad = false;
                }
                else
                {
                    Instructions.Visibility = Visibility.Visible;
                }
            }
            Welcome.Content = String.Format("Welcome to ProjectTile, {0}. You are logged into Entity {1}{2}.", LoginFunctions.CurrentStaffName, EntityFunctions.CurrentEntityName, demoCo);
        }  

        private void TilesPage1_Loaded(object sender, RoutedEventArgs e)
        {
            addAndGroupButtons();
        }

        /* Page changes */
        private void addAndGroupButtons()
        {
            try
            {
                mainButtons.Add(EntityButton);
                mainButtons.Add(AdminButton);
                mainButtons.Add(StaffButton);
                mainButtons.Add(ProductButton);
                mainButtons.Add(ClientButton);

                entityButtons.Add(EntityButton_Change);
                entityButtons.Add(EntityButton_Default);

                if (myPermissions.Allow("AddEntities")) { entityButtons.Add(EntityButton_New); }
                else { EntityButton_Amend.Margin = EntityButton_New.Margin; }
                if (myPermissions.Allow("EditEntities")) { entityButtons.Add(EntityButton_Amend); }

                adminButtons.Add(AdminButton_Login);
                adminButtons.Add(AdminButton_Password);
                adminButtons.Add(AdminButton_Exit);

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
                if (myPermissions.Allow("AddClients")) { clientButtons.Add(ClientButton_New); }
                if (myPermissions.Allow("EditClients")) { clientButtons.Add(ClientButton_Amend); }

                // More to come...
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
                    else if (thisButton.Name == "AdminButton") { toggleChildButtons(true, ref adminButtons); }
                    else if (thisButton.Name == "StaffButton") { toggleChildButtons(true, ref staffButtons); }
                    else if (thisButton.Name == "ProductButton") { toggleChildButtons(true, ref productButtons); }
                    else if (thisButton.Name == "ClientButton") { toggleChildButtons(true, ref clientButtons); }

                    // More to come...

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
            //MessageBox.Show("Hi");
            keepExpansion = false;
            movedElsewhere();
        }

        /* ----------------------
           -- Event Management ---
           ---------------------- */

        /* Generic (shared) events */
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

        /* Control-specific events */
        private void ChangeCurrentEntityButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage("Switch");
        }

        private void NewEntityButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage("New");
        }

        private void AmendEntityButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage("Amend");
        }

        private void ChangeDefaultEntityButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowEntityPage("Default");
        }

        private void AdminButton_Login_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowLoginPage("LogIn");
        }

        private void AdminButton_Password_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowLoginPage("PassChange");
        }

        private void AdminButton_Exit_Click(object sender, RoutedEventArgs e)
        {
            winMain.Close();
        }

        private void StaffButton_View_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffPage("View");
        }

        private void StaffButton_Amend_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffPage("Amend");
        }

        private void StaffButton_New_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffDetailsPage("New", 0);
        }

        private void StaffButton_Entities_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowStaffEntitiesPage();
        }

        private void ProductButton_View_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage("View");
        }

        private void ProductButton_New_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage("New");
        }

        private void ProductButton_Amend_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowProductPage("Amend");
        }

        private void ClientButton_View_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientPage("View");
        }

        private void ClientButton_New_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientPage("New");
        }

        private void ClientButton_Amend_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowClientPage("Amend");
        }
        
    } // class
} // namespace
