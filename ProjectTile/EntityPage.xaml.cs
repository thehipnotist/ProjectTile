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
        //int selectedEntityID = 0;

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
                    EntityCombo.Margin = EntityName.Margin;
                    CommitButtonText.Text = "Change";
                    ChangeNameLabel.Visibility = Visibility.Hidden;
                    EntityCombo.ItemsSource = EntityFunctions.EntityList(Globals.MyStaffID, false, Globals.CurrentEntityID);
                    if (EntityCombo.Items.Count == 1) { EntityCombo.SelectedIndex = 0; }
                }
                else if (pageMode == PageFunctions.New)
                {
                    EntityCombo.Visibility = Visibility.Hidden;
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
                    EntityName.Margin = EntityCombo.Margin;
                    EntityCombo.Margin = nameMargin;
                    CommitButtonText.Text = "Amend";
                    EntityCombo.ItemsSource = EntityFunctions.EntityList(Globals.MyStaffID, false);
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
                    EntityCombo.Margin = EntityName.Margin;
                    CommitButtonText.Text = "Set Default";
                    ChangeNameLabel.Visibility = Visibility.Hidden;
                    EntityCombo.ItemsSource = EntityFunctions.EntityList(Globals.MyStaffID, false, Globals.MyDefaultEntityID);

                    if (Globals.MyDefaultEntityID != Globals.CurrentEntityID)
                    {
                        try { EntityCombo.SelectedItem = Globals.CurrentEntityName; }
                        catch (Exception generalException) { MessageFunctions.Error("Error setting current entity", generalException); }
                    }
                    else { if (EntityCombo.Items.Count == 1) { EntityCombo.SelectedIndex = 0; } }
                
                }
                else // Not sure
                {
                    EntityCombo.Visibility = Visibility.Hidden;
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
            string name = EntityName.Text;
            string description = EntityDescription.Text;

            if (pageMode == PageFunctions.Switch)
            {
                EntityFunctions.SwitchEntity(ref selectedEntity, (bool) MakeDefaultCheckBox.IsChecked);
            }
            else if (pageMode == PageFunctions.New)
            {
                EntityFunctions.NewEntity(name, description, (bool)SwitchToCheckBox.IsChecked, (bool)MakeDefaultCheckBox.IsChecked);              
            }
            else if (pageMode == PageFunctions.Amend)
            {
                EntityFunctions.AmendEntity(ref selectedEntity, name, description);                
               
            }
            else if (pageMode == PageFunctions.Default)
            {
                EntityFunctions.ChangeDefaultEntity(ref selectedEntity, name);
            }  
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PageFunctions.ShowTilesPage();
        }

        private void EntityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (EntityCombo.SelectedValue == null) { return; }

                selectedEntity = (Entities)EntityCombo.SelectedValue;
                //selectedEntityID = selectedEntity.ID;
                EntityName.Text = selectedEntity.EntityName;
                EntityDescription.Text = selectedEntity.EntityDescription;
                if (pageMode == PageFunctions.Amend)
                {
                    EntityDescription.IsEnabled = true;
                }
                else if (pageMode == PageFunctions.Switch)
                {
                    if (selectedEntity.ID != Globals.MyDefaultEntityID) { MakeDefaultCheckBox.IsEnabled = true; }
                    else { MakeDefaultCheckBox.IsChecked = MakeDefaultCheckBox.IsEnabled = false; }
                }
            }
            catch (Exception generalException) 
            {
                MessageFunctions.Error("Error changing entity selection", generalException);
                //selectedEntityID = 0;
                selectedEntity = null;
            }
        }

    } // class
} // namespace
