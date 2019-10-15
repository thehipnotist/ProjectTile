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
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //

        // Global/page parameters //		
        string pageMode;

        // Current variables //
        int selectedEntityID = 0;

        // Current records //
        Entities selectedEntity = null;

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public EntityPage()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Page));
            KeepAlive = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                try 
                {
                    pageMode = PageFunctions.pageParameter(this, "Mode");
                }
                catch (Exception generalException) 
                { 
                    MessageFunctions.Error("Error retrieving query details", generalException);
                    PageFunctions.ShowTilesPage();    
                }

                if (pageMode == PageFunctions.Switch)
                {
                    PageHeader.Content = "Change Current Entity";
                    HeaderImage2.SetResourceReference(Image.SourceProperty, "ChangeEntityIcon");
                    Instructions.Content = "Pick an Entity from the list to change to it.";
                    EntityName.Visibility = Visibility.Hidden;
                    SwitchToCheckBox.Visibility = Visibility.Hidden;
                    EntityDescription.IsEnabled = false;
                    EntityList.Margin = EntityName.Margin;
                    CommitButtonText.Text = "Change";
                    ChangeNameLabel.Visibility = Visibility.Hidden;
                    EntityList.ItemsSource = EntityFunctions.EntityNameList(Globals.MyStaffID, false, Globals.CurrentEntityID);
                    if (EntityList.Items.Count == 1) { EntityList.SelectedIndex = 0; }
                }
                else if (pageMode == PageFunctions.New)
                {
                    EntityList.Visibility = Visibility.Hidden;
                    ChangeNameLabel.Visibility = Visibility.Hidden;
                }
                else if (pageMode == PageFunctions.Amend)
                {
                    PageHeader.Content = "Amend Existing Entity";
                    HeaderImage2.SetResourceReference(Image.SourceProperty, "AmendIcon");
                    Instructions.Content = "Pick an Entity from the list to amend it.";
                    SwitchToCheckBox.Visibility = Visibility.Hidden;
                    MakeDefaultCheckBox.Visibility = Visibility.Hidden;
                    EntityDescription.IsEnabled = false;

                    Thickness nameMargin = EntityName.Margin;
                    EntityName.Margin = EntityList.Margin;
                    EntityList.Margin = nameMargin;
                    CommitButtonText.Text = "Amend";
                    EntityList.ItemsSource = EntityFunctions.EntityNameList(Globals.MyStaffID, false);
                }
                else if (pageMode == PageFunctions.Default) 
                {
                    PageHeader.Content = "Change Default Entity";
                    HeaderImage2.SetResourceReference(Image.SourceProperty, "PinIcon");
                    HeaderImage2.Width = 25;
                    HeaderImage2.Stretch = System.Windows.Media.Stretch.UniformToFill;
                    Instructions.Content = "Pick an Entity from the list to set it as your default.";
                    EntityName.Visibility = Visibility.Hidden;
                    SwitchToCheckBox.Visibility = Visibility.Hidden;
                    MakeDefaultCheckBox.Visibility = Visibility.Hidden;
                    EntityDescription.IsEnabled = false;
                    EntityList.Margin = EntityName.Margin;
                    CommitButtonText.Text = "Set Default";
                    ChangeNameLabel.Visibility = Visibility.Hidden;
                    EntityList.ItemsSource = EntityFunctions.EntityNameList(Globals.MyStaffID, false, Globals.MyDefaultEntityID);

                    if (Globals.MyDefaultEntityID != Globals.CurrentEntityID)
                    {
                        try { EntityList.SelectedItem = Globals.CurrentEntityName; }
                        catch (Exception generalException) { MessageFunctions.Error("Error setting current entity", generalException); }
                    }
                    else { if (EntityList.Items.Count == 1) { EntityList.SelectedIndex = 0; } }
                
                }
                else // Not sure
                {
                    EntityList.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error setting initial values", generalException); }
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Control-specific events //
        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            string displayName = EntityName.Text;
            string displayDescription = EntityDescription.Text;

            if (pageMode == PageFunctions.Switch)
            {
                EntityFunctions.SwitchEntity(ref selectedEntity, (bool) MakeDefaultCheckBox.IsChecked);
            }
            else if (pageMode == PageFunctions.New)
            {
                EntityFunctions.NewEntity(displayName, displayDescription, (bool)SwitchToCheckBox.IsChecked, (bool)MakeDefaultCheckBox.IsChecked);              
            }
            else if (pageMode == PageFunctions.Amend)
            {
                EntityFunctions.AmendEntity(ref selectedEntity, displayName, displayDescription);                
               
            }
            else if (pageMode == PageFunctions.Default)
            {
                EntityFunctions.ChangeDefaultEntity(ref selectedEntity, displayName);
            }  
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowTilesPage();
        }

        private void EntityList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string displayName = EntityList.SelectedValue.ToString();
            EntityName.Text = displayName;

            try
            {
                selectedEntity = EntityFunctions.GetEntityByName(displayName);
                selectedEntityID = selectedEntity.ID;

                EntityDescription.Text = selectedEntity.EntityDescription;
                if (pageMode == PageFunctions.Amend)
                {
                    EntityDescription.IsEnabled = true;
                }
                else if (pageMode == PageFunctions.Switch)
                {
                    if (selectedEntityID != Globals.MyDefaultEntityID)
                    {
                        MakeDefaultCheckBox.IsEnabled = true;
                    }
                    else
                    {
                        MakeDefaultCheckBox.IsEnabled = false;
                        MakeDefaultCheckBox.IsChecked = false;
                    }
                }
            }
            catch (Exception generalException) 
            {
                MessageFunctions.Error("Error changing entity selection", generalException);
                selectedEntityID = 0;
                selectedEntity = null;
            }
        }

    } // class
} // namespace
