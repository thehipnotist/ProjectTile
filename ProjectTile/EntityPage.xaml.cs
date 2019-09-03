﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for EntityPage.xaml
    /// </summary>
    public partial class EntityPage : Page
    {
        /* ----------------------
           -- Global Variables --
           ---------------------- */

        /* Global/page parameters */		
        string pageMode;

        /* Current variables */
        int selectedEntityID = 0;

        /* Current records */
        Entities selectedEntity = null;

        /* ----------------------
           -- Page Management ---
           ---------------------- */

        /* Initialize and Load */
        public EntityPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                try 
                {
                    string originalString = NavigationService.CurrentSource.OriginalString;
                    pageMode = PageFunctions.pageParameter(originalString, "Mode"); //, ref winMain
                }
                catch (Exception generalException) 
                { 
                    MessageFunctions.ErrorMessage("Error retrieving query details: " + generalException.Message);
                    PageFunctions.ShowTilesPage();    
                }

                EntityList.ItemsSource = EntityFunctions.EntityList(LoginFunctions.CurrentStaffID, false);

                if (pageMode == "Switch")
                {
                    PageHeader.Content = "Change Current Entity";
                    Instructions.Content = "Pick an Entity from the list to change to it.";
                    EntityName.Visibility = Visibility.Hidden;
                    SwitchTo_CheckBox.Visibility = Visibility.Hidden;
                    //MakeDefault_CheckBox.Margin = SwitchTo_CheckBox.Margin;
                    EntityDescription.IsEnabled = false;
                    EntityList.Margin = EntityName.Margin;
                    CommitButtonText.Text = "Change";
                    ChangeNameLabel.Visibility = Visibility.Hidden;
                }
                else if (pageMode == "New")
                {
                    EntityList.Visibility = Visibility.Hidden;
                    ChangeNameLabel.Visibility = Visibility.Hidden;
                }
                else if (pageMode == "Amend")
                {
                    PageHeader.Content = "Amend Existing Entity";
                    Instructions.Content = "Pick an Entity from the list to amend it.";
                    SwitchTo_CheckBox.Visibility = Visibility.Hidden;
                    MakeDefault_CheckBox.Visibility = Visibility.Hidden;
                    EntityDescription.IsEnabled = false;

                    Thickness nameMargin = EntityName.Margin;
                    EntityName.Margin = EntityList.Margin;
                    EntityList.Margin = nameMargin;
                    //nameMargin.Left = 420;
                    //EntityName.Margin = nameMargin;
                    CommitButtonText.Text = "Amend";
                }
                else if (pageMode == "Default") 
                {
                    PageHeader.Content = "Change Default Entity";
                    Instructions.Content = "Pick an Entity from the list to set it as your default.";
                    EntityName.Visibility = Visibility.Hidden;
                    SwitchTo_CheckBox.Visibility = Visibility.Hidden;
                    MakeDefault_CheckBox.Visibility = Visibility.Hidden;
                    EntityDescription.IsEnabled = false;
                    EntityList.Margin = EntityName.Margin;
                    CommitButtonText.Text = "Set Default";
                    ChangeNameLabel.Visibility = Visibility.Hidden;
               
                    try
                    {
                        EntityList.SelectedItem = EntityFunctions.CurrentEntityName;
                    }
                    catch (Exception generalException) { MessageFunctions.ErrorMessage("Error setting current entity: " + generalException.Message); }
                
                }
                else // Not sure
                {
                    EntityList.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception generalException) { MessageFunctions.ErrorMessage("Error setting initial values: " + generalException.Message); }
        }

        /* ----------------------
           -- Event Management ---
           ---------------------- */

        /* Control-specific events */
        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            string entityName = EntityName.Text;
            string entityDescription = EntityDescription.Text;

            if (pageMode == "Switch")
            {
                EntityFunctions.ChangeEntity(selectedEntityID, ref selectedEntity, (bool) MakeDefault_CheckBox.IsChecked);
            }
            else if (pageMode == "New")
            {
                EntityFunctions.NewEntity(entityName, entityDescription, (bool)SwitchTo_CheckBox.IsChecked, (bool)MakeDefault_CheckBox.IsChecked);              
            }
            else if (pageMode == "Amend")
            {
                EntityFunctions.AmendEntity(ref selectedEntity, entityName, entityDescription);                
               
            }
            else if (pageMode == "Default")
            {
                EntityFunctions.ChangeDefaultEntity(ref selectedEntity, entityName);
            }
            else { MessageFunctions.ErrorMessage("Not yet implemented."); }    
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowTilesPage();
        }

        private void EntityList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string entityName = EntityList.SelectedValue.ToString();
            EntityName.Text = entityName;

            try
            {
                selectedEntity = EntityFunctions.GetEntityByName(entityName);
                selectedEntityID = selectedEntity.ID;

                EntityDescription.Text = selectedEntity.EntityDescription;
                if (pageMode == "Amend")
                {
                    EntityDescription.IsEnabled = true;
                }
                else if (pageMode == "Switch")
                {
                    if (selectedEntityID != EntityFunctions.DefaultEntityID)
                    {
                        MakeDefault_CheckBox.IsEnabled = true;
                    }
                    else
                    {
                        MakeDefault_CheckBox.IsEnabled = false;
                        MakeDefault_CheckBox.IsChecked = false;
                    }
                }
            }
            catch (Exception generalException) 
            {
                MessageFunctions.ErrorMessage("Error changing entity selection: " + generalException.Message);
                selectedEntityID = 0;
                selectedEntity = null;
            }
        }

    } // class
} // namespace
