using System;
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

                EntityList.ItemsSource = EntityFunctions.EntityList(Globals.CurrentStaffID, false);

                if (pageMode == PageFunctions.Switch)
                {
                    PageHeader.Content = "Change Current Entity";
                    HeaderImage2.SetResourceReference(Image.SourceProperty, "ChangeEntityIcon");
                    Instructions.Content = "Pick an Entity from the list to change to it.";
                    EntityName.Visibility = Visibility.Hidden;
                    SwitchTo_CheckBox.Visibility = Visibility.Hidden;
                    //MakeDefault_CheckBox.Margin = SwitchTo_CheckBox.Margin;
                    EntityDescription.IsEnabled = false;
                    EntityList.Margin = EntityName.Margin;
                    CommitButtonText.Text = "Change";
                    ChangeNameLabel.Visibility = Visibility.Hidden;
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
                else if (pageMode == PageFunctions.Default) 
                {
                    PageHeader.Content = "Change Default Entity";
                    HeaderImage2.SetResourceReference(Image.SourceProperty, "PinIcon");
                    HeaderImage2.Width = 25;
                    HeaderImage2.Stretch = System.Windows.Media.Stretch.UniformToFill;
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
                        EntityList.SelectedItem = Globals.CurrentEntityName;
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error setting current entity", generalException); }
                
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
                EntityFunctions.ChangeEntity(selectedEntityID, ref selectedEntity, (bool) MakeDefault_CheckBox.IsChecked);
            }
            else if (pageMode == PageFunctions.New)
            {
                EntityFunctions.NewEntity(displayName, displayDescription, (bool)SwitchTo_CheckBox.IsChecked, (bool)MakeDefault_CheckBox.IsChecked);              
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
                    if (selectedEntityID != Globals.DefaultEntityID)
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
                MessageFunctions.Error("Error changing entity selection", generalException);
                selectedEntityID = 0;
                selectedEntity = null;
            }
        }

    } // class
} // namespace
