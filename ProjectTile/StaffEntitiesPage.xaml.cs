using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for PageTemplate.xaml
    /// </summary>
    public partial class StaffEntitiesPage : Page
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //

        // Global/page parameters //
        string pageMode;
        string fromSource = "";
        string sourceMode = "";
        string backSource = "";
        int selectedStaffID = 0;

        // Current variables //
        bool activeOnly = false;
        string nameContains = "";
        int selectedEntityID = 0;
        string selectedStaffName = "";
        bool resetEntitySelection = false;

        string defaultInstructions = "Select a staff member and click 'Entities', or select an Entity and click 'Staff'.";
        string defaultPageHeader = "Staff Members in each Entity";
        
        enum modeType { EntitiesOfStaff, StaffInEntity }
        modeType ByStaff = modeType.EntitiesOfStaff;
        modeType ByEntity = modeType.StaffInEntity;
        modeType editMode;

        // Current records //
        StaffSummaryRecord selectedRecord;
        Entities selectedEntity;

        // Lists //
        List<Entities> entityComboList;

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public StaffEntitiesPage()
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
                sourceMode = PageFunctions.pageParameter(this, "SourceMode");
                selectedStaffID = Int32.Parse(PageFunctions.pageParameter(this, "StaffID"));
                refreshEntityCombo(true);
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                PageFunctions.ShowTilesPage();
            }

            CommitButton.Visibility = Visibility.Hidden;
            StaffFrom.Visibility = StaffTo.Visibility = Visibility.Hidden;
            StaffLabel.Margin = NameContainsLabel.Margin;
            StaffCombo.Margin = NameContains.Margin;

            if (selectedStaffID > 0) // Opened from the Staff Page
            {
                fromSource = "StaffPage";                
                Globals.SelectedStaffMember = StaffFunctions.GetStaffMember(selectedStaffID);
                StaffCombo.IsEnabled = false; // Cannot easily recreate the same selection list
                viewEntitiesByStaffMember();

                if (pageMode == PageFunctions.View) { Instructions.Content = "Note that only Entities you can access yourself are displayed."; }
                else { Instructions.Content = "Select the Entities this user should have, then click 'Save'. You can then choose other staff from the list."; }
            }
            else
            {
                fromSource = Globals.TilesPageName;
                StaffLabel.Visibility = StaffCombo.Visibility = Visibility.Hidden;
                BackButton.Visibility = Visibility.Hidden;
                EntitiesFrom.Visibility = EntitiesTo.Visibility =  Visibility.Hidden;

                AddButton.Visibility = DefaultButton.Visibility = RemoveButton.Visibility = Visibility.Hidden;
                FromLabel.Visibility = ToLabel.Visibility = Visibility.Hidden;

                if (pageMode == PageFunctions.View)
                {
                    StaffButton.Visibility = Visibility.Hidden;
                    //EntitiesButtonText.Text = "View Entities";
                    Instructions.Content = "Select a staff member and click 'Entities', or select an Entity to see its assigned staff.";
                }
                else
                {
                    Instructions.Content = defaultInstructions;
                }
            } 
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //
        private void refreshEntityCombo(bool includeAll)
        {
            try
            {
                entityComboList = EntityFunctions.EntityList(Globals.MyStaffID, includeAll);
                EntityCombo.ItemsSource = entityComboList;
                EntityCombo.SelectedItem = entityComboList.FirstOrDefault(p => p.ID == selectedEntityID);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating Entity filter list", generalException); }
        }

        // Data retrieval //

        // Other/shared functions //
        private void refreshStaffDataGrid()
        {
            try
            {
                var gridList = StaffFunctions.GetStaffGridData(activeOnly, nameContains, Globals.AllRecords, selectedEntityID);
                StaffDataGrid.ItemsSource = gridList;

                if (selectedStaffID > 0)
                {
                    try
                    {
                        if (gridList.Exists(s => s.ID == selectedStaffID))
                        {
                            StaffDataGrid.SelectedItem = gridList.First(s => s.ID == selectedStaffID);
                            StaffDataGrid.ScrollIntoView(StaffDataGrid.SelectedItem);
                        }
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error selecting record", generalException); }
                }

                // refreshStaffSummaries(true);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error filling staff grid", generalException); }
        }        
        
        private void nameFilter()
        {
            nameContains = NameContains.Text;
            refreshStaffDataGrid();
        }

        private void clearSelection()
        {
            selectedRecord = null;
            // selectedStaffID = 0; // Don't clear this automatically, as the refresh tries to reuse it
            Globals.SelectedStaffMember = null; // Ditto
            EntitiesButton.IsEnabled = false;
        }

        private void disableButtons()
        {
            AddButton.IsEnabled = DefaultButton.IsEnabled = RemoveButton.IsEnabled = false;
        }

        private void toggleSelectionControls(bool selectionMode)
        {
            Visibility selectionOnly;
            Visibility editOnly;

            if (selectionMode)
            {
                selectionOnly = Visibility.Visible;
                editOnly = Visibility.Hidden;
                backSource = fromSource;
                BackButton.Visibility = (fromSource == Globals.TilesPageName) ? Visibility.Hidden : Visibility.Visible;
                Instructions.Content = defaultInstructions;
                FromLabel.Visibility = ToLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                selectionOnly = Visibility.Hidden;
                editOnly = Visibility.Visible;
                backSource = (fromSource == Globals.TilesPageName) ? editMode.ToString() : fromSource;
                BackButton.Visibility = Visibility.Visible;
                ToLabel.Content = (editMode == ByStaff) ? "Linked Entities (Default in Bold)" : "Linked Staff";
                
                if (pageMode == PageFunctions.View)
                {
                    Instructions.Content = "";
                    FromLabel.Visibility = Visibility.Hidden;
                    ToLabel.Visibility = Visibility.Visible;
                }
                else
                {
                    Instructions.Content = (editMode == ByStaff) ? "Move Entities to the right to add them, or left to remove them." : "Move staff to the right to add them, or left to remove them.";
                    FromLabel.Visibility = ToLabel.Visibility = Visibility.Visible;
                    FromLabel.Content = (editMode == ByStaff) ? "Available Entities" : "Available Staff";

                    string borderBrush = (editMode == ByStaff) ? "PtBrushEntity3" : "PtBrushStaff3";
                    AddButton.BorderBrush = RemoveButton.BorderBrush = DefaultButton.BorderBrush = Application.Current.Resources[borderBrush] as SolidColorBrush;
                }

                disableButtons();                
            }

            StaffDataGrid.Visibility = selectionOnly;
            NameContainsLabel.Visibility = NameContains.Visibility = selectionOnly;
            ActiveOnlyCheckBox.Visibility = (editMode == ByEntity || selectionMode) ? Visibility.Visible : Visibility.Hidden;

            EntitiesButton.Visibility = selectionOnly;
            StaffButton.Visibility = selectionOnly;
            StaffButton.IsEnabled = (selectionMode && selectedEntityID != 0);

            CommitButton.Visibility = (pageMode == PageFunctions.View)? Visibility.Hidden : editOnly;
            StaffLabel.Visibility = StaffCombo.Visibility = (editMode == ByStaff && !selectionMode) ? Visibility.Visible : Visibility.Hidden;

            StaffFrom.Visibility = (editMode == ByEntity && !selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            StaffTo.Visibility = (editMode == ByEntity && !selectionMode) ? Visibility.Visible : Visibility.Hidden;
            EntitiesFrom.Visibility = (editMode == ByStaff && !selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            EntitiesTo.Visibility = (editMode == ByStaff && !selectionMode) ? Visibility.Visible : Visibility.Hidden;     
            
            AddButton.Visibility = DefaultButton.Visibility = RemoveButton.Visibility = (!selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            EntityLabel.Visibility = EntityCombo.Visibility = (editMode == ByEntity || selectionMode) ? Visibility.Visible : Visibility.Hidden;
        }

        private void viewEntitiesByStaffMember()
        {
            editMode = ByStaff;
            toggleSelectionControls(false);
            try
            {
                var staffComboList = StaffFunctions.GetStaffGridData(activeOnly, nameContains, Globals.AllRecords, 0);
                var staffNames = staffComboList.Select(sg => sg.StaffName);
                StaffCombo.ItemsSource = staffNames;
                selectedStaffName = StaffFunctions.GetSelectedName();
                StaffCombo.SelectedValue = selectedStaffName;                
                refreshEntitySummaries(true);                           
            }
            catch (Exception generalException) 
            { 
                MessageFunctions.Error("Error populating staff list", generalException);
                return;
            }
        }

        private void viewStaffByEntity()
        {
            refreshEntityCombo(false);
            editMode = ByEntity;
            toggleSelectionControls(false);
            refreshStaffSummaries(true);
        }

        private void refreshStaffSummaries(bool fromDatabase)
        {
            if (fromDatabase)
            {
                StaffFunctions.StaffNotForEntity = StaffFunctions.StaffNotInEntity(activeOnly, selectedEntityID);
                StaffFunctions.StaffForEntity = StaffFunctions.StaffInEntity(activeOnly, selectedEntityID);
            }            
            
            StaffFrom.ItemsSource = StaffFunctions.StaffNotForEntity;
            StaffFrom.Items.SortDescriptions.Clear();
            StaffFrom.Items.SortDescriptions.Add(new SortDescription("NameAndUser", ListSortDirection.Ascending));
            StaffFrom.Items.Refresh();
            StaffFrom.SelectedItem = null;

            StaffTo.ItemsSource = StaffFunctions.StaffForEntity;
            StaffTo.Items.SortDescriptions.Clear();
            StaffTo.Items.SortDescriptions.Add(new SortDescription("NameAndUser", ListSortDirection.Ascending));
            StaffTo.Items.Refresh();
            StaffTo.SelectedItem = null;

            disableButtons();
            if (selectedEntity != null)
            {
                PageHeader.Content = "Staff Members in Entity '" + selectedEntity.EntityName + "'";
            }            
        }

        private void refreshEntitySummaries(bool fromDatabase)
        {
            if (fromDatabase)
            {
                StaffFunctions.EntitiesNotForStaff = StaffFunctions.AllowedUnlinkedEntities(selectedStaffID);
                StaffFunctions.EntitiesForStaff = StaffFunctions.AllowedLinkedEntities(selectedStaffID);                
            }
            EntitiesFrom.ItemsSource = StaffFunctions.EntitiesNotForStaff;
            EntitiesFrom.Items.SortDescriptions.Clear();
            EntitiesFrom.Items.SortDescriptions.Add(new SortDescription("NameAndUser", ListSortDirection.Ascending));
            EntitiesFrom.Items.Refresh();
            EntitiesFrom.SelectedItem = null;

            EntitiesTo.ItemsSource = StaffFunctions.EntitiesForStaff;
            EntitiesTo.Items.SortDescriptions.Clear();
            EntitiesTo.Items.SortDescriptions.Add(new SortDescription("Default", ListSortDirection.Descending));
            EntitiesTo.Items.Refresh();
            EntitiesTo.SelectedItem = null;

            disableButtons();
            if (selectedStaffName != "")
            {
                PageHeader.Content = "Entities for " + selectedStaffName;
            }
        }

        private void fromActivated(bool StaffList)
        {
            try
            {
                AddButton.IsEnabled = (StaffList && StaffFrom.SelectedItems != null) || (!StaffList && EntitiesFrom.SelectedItems != null);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error activating the 'Add' button", generalException); }
        }

        private void toActivated(bool StaffList)
        {
            try
            {
                RemoveButton.IsEnabled = DefaultButton.IsEnabled = (StaffList && StaffTo.SelectedItems != null) || (!StaffList && EntitiesTo.SelectedItems != null);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error activating the 'Remove' and 'Default' buttons", generalException); }
        }

        private void addStaff()
        {
            try
            {
                if (StaffFrom.SelectedItems != null)
                {                                       
                    List<StaffSummarySmall> fromList = new List<StaffSummarySmall>();                    
                    foreach (var selectedRow in StaffFrom.SelectedItems)
                    {
                        fromList.Add((StaffSummarySmall)selectedRow);                        
                    }
                    
                    bool success = StaffFunctions.ToggleEntityStaff(fromList, true, selectedEntity);
                    if (success)
                    {
                        refreshStaffSummaries(false);
                        CommitButton.IsEnabled = true;
                        //AddButton.IsEnabled = DefaultButton.IsEnabled = RemoveButton.IsEnabled = false;
                    } 
                }
                else
                {
                    MessageFunctions.Error("Error adding staff to Entity: no staff selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error adding staff to Entity", generalException);
            }
        }

        private void addEntities()
        {
            try
            {
                if (EntitiesFrom.SelectedItems != null)
                {
                    List<EntitySummaryRecord> fromList = new List<EntitySummaryRecord>();
                    foreach (var selectedRow in EntitiesFrom.SelectedItems)
                    {
                        fromList.Add((EntitySummaryRecord)selectedRow);
                    }

                    bool success = StaffFunctions.ToggleStaffEntities(fromList, true, Globals.SelectedStaffMember);
                    if (success)
                    {
                        refreshEntitySummaries(false);
                        CommitButton.IsEnabled = true;
                        //AddButton.IsEnabled = DefaultButton.IsEnabled = RemoveButton.IsEnabled = false;
                    }
                }
                else
                {
                    MessageFunctions.Error("Error adding Entities to staff: no Entity selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error adding Entities to staff", generalException);
            }
        }

        private void removeStaff()
        {
            try
            {
                if (StaffTo.SelectedItems != null)
                {
                    List<StaffSummarySmall> toList = new List<StaffSummarySmall>();
                    foreach (var selectedRow in StaffTo.SelectedItems)
                    {
                        toList.Add((StaffSummarySmall)selectedRow);
                    }

                    bool success = StaffFunctions.ToggleEntityStaff(toList, false, selectedEntity);
                    if (success)
                    {
                        refreshStaffSummaries(false);
                        CommitButton.IsEnabled = true;
                        //AddButton.IsEnabled = DefaultButton.IsEnabled = RemoveButton.IsEnabled = false;
                    }
                }
                else
                {
                    MessageFunctions.Error("Error removing staff from Entity: no staff selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error removing staff from Entity", generalException);
            }
        }

        private void removeEntities()
        {
            try
            {
                if (EntitiesTo.SelectedItems != null)
                {
                    List<EntitySummaryRecord> toList = new List<EntitySummaryRecord>();
                    foreach (var selectedRow in EntitiesTo.SelectedItems)
                    {
                        toList.Add((EntitySummaryRecord)selectedRow);
                    }

                    bool success = StaffFunctions.ToggleStaffEntities(toList, false, Globals.SelectedStaffMember);
                    if (success)
                    {
                        refreshEntitySummaries(false);
                        CommitButton.IsEnabled = true;
                        //AddButton.IsEnabled = DefaultButton.IsEnabled = RemoveButton.IsEnabled = false;
                    }
                }
                else
                {
                    MessageFunctions.Error("Error removing Entities from staff: no Entity selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error removing Entities from staff", generalException);
            }
        }

        private void setDefault()
        {
            if (editMode == ByStaff)
            {
                try
                {
                    if (EntitiesTo.SelectedItems == null)
                    {
                        MessageFunctions.Error("Error setting default Entity: no Entity selected.", null);
                    }
                    else if (EntitiesTo.SelectedItems.Count > 1)
                    {
                        MessageFunctions.InvalidMessage("Cannot set default Entity. Please ensure only one Entity is selected.", "Multiple Entities selected");
                    }
                    else
                    {
                        EntitySummaryRecord thisRecord = (EntitySummaryRecord) EntitiesTo.SelectedItem;
                        if (thisRecord.Default == false)
                        {
                            int entityID = thisRecord.ID;
                            bool success = StaffFunctions.ChangeDefault(entityID, selectedStaffID);
                            if (success)
                            {
                                refreshEntitySummaries(false);
                                CommitButton.IsEnabled = true;
                            }
                        }
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error setting default Entity", generalException);
                }
            }
            else
            {
                try
                {
                    if (StaffTo.SelectedItems != null)
                    {
                        List<StaffSummarySmall> defaultList = new List<StaffSummarySmall>();
                        foreach (var selectedRow in StaffTo.SelectedItems)
                        {
                            defaultList.Add((StaffSummarySmall)selectedRow);
                        }

                        bool success = StaffFunctions.MakeDefault(defaultList, selectedEntity);
                        if (success)
                        {
                            refreshStaffSummaries(false);
                            CommitButton.IsEnabled = true;
                        }
                    }
                    else
                    {
                        MessageFunctions.Error("Error setting default Entity: no staff selected.", null);
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error setting default Entity", generalException);
                }
            }
        }

        private void togglActiveOnly(bool isChecked)
        {
            activeOnly = isChecked;
            refreshStaffDataGrid();
            if (StaffTo.Visibility == Visibility.Visible) 
            {
                if (StaffFunctions.IgnoreAnyChanges())
                {
                    StaffFunctions.ClearAnyChanges();
                    refreshStaffSummaries(true);
                }
            }
        }


        // ---------------------- //
        // -- Event Management -- //
        //   ---------------------- //

        // Generic (shared) control events //

 
        // Control-specific events //
        private void ActiveOnlyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            togglActiveOnly(true);
        }

        private void ActiveOnlyCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            togglActiveOnly(false);
        }

        private void NameContains_LostFocus(object sender, RoutedEventArgs e)
        {
            nameFilter();
        }

        private void NameContains_KeyUp(object sender, KeyEventArgs e)
        {
            nameFilter();
        }

        private void StaffDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (StaffDataGrid.SelectedItem != null)
                {
                    selectedRecord = (StaffSummaryRecord)StaffDataGrid.SelectedItem;
                    selectedStaffID = selectedRecord.ID;
                    Globals.SelectedStaffMember = StaffFunctions.GetStaffMember(selectedStaffID);
                    EntitiesButton.IsEnabled = true;
                }
                else // No record selected, e.g. because filter changed
                {
                    clearSelection();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error processing selection change", generalException);
                selectedStaffID = 0; // Avoid accidentally using the previous selection
                clearSelection();
            }
        }

        private void EntityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EntityCombo.SelectedItem == null) { return; } // Do nothing, won't be for long
            if (resetEntitySelection) // Avoids immediate re-processing if set back to previous selection
            {
                resetEntitySelection = false;
                return;
            }
            if (StaffFunctions.IgnoreAnyChanges())
            {
                StaffFunctions.ClearAnyChanges();
                Entities selectedRecord = (Entities)EntityCombo.SelectedItem;
                if (selectedRecord.EntityName == Globals.AllRecords)
                {
                    selectedEntityID = 0;
                    selectedEntity = null;
                    StaffButton.IsEnabled = false;
                }
                else
                {
                    try
                    {
                        selectedEntity = selectedRecord;
                        selectedEntityID = selectedEntity.ID;
                        StaffButton.IsEnabled = true;
                    }
                    catch (Exception generalException)
                    {
                        MessageFunctions.Error("Error changing entity selection", generalException);
                        selectedEntityID = 0;
                        selectedEntity = null;
                        StaffButton.IsEnabled = false;
                    }
                }
                refreshStaffDataGrid();
                refreshStaffSummaries(true);
            }
            else 
            {
                resetEntitySelection = true; // If the user cancels the change, don't re-process                
                EntityCombo.SelectedItem = entityComboList.FirstOrDefault(p => p.ID == selectedEntityID);
            }
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (StaffFunctions.IgnoreAnyChanges())
            {
                StaffFunctions.ClearAnyChanges();
                PageFunctions.ShowTilesPage();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (StaffFunctions.IgnoreAnyChanges())
            {
                StaffFunctions.ClearAnyChanges();
                if (backSource == "StaffPage")
                {
                    StaffFunctions.ReturnToStaffPage(selectedStaffID, sourceMode);
                }
                else
                {
                    refreshEntityCombo(true);
                    //refreshStaffDataGrid();                    
                    toggleSelectionControls(true);
                    PageHeader.Content = defaultPageHeader;
                }
            }
        }

        private void EntitiesButton_Click(object sender, RoutedEventArgs e)
        {
            viewEntitiesByStaffMember();
        }

        private void StaffButton_Click(object sender, RoutedEventArgs e)
        {
            viewStaffByEntity();
        }

        private void StaffFrom_GotFocus(object sender, RoutedEventArgs e)
        {
            fromActivated(true);
        }

        private void StaffFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fromActivated(true);
        }

        private void EntitiesFrom_GotFocus(object sender, RoutedEventArgs e)
        {
            fromActivated(false);
        }

        private void EntitiesFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fromActivated(false);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
           if (editMode == ByStaff) { addEntities(); }
            else { addStaff(); }
        }

        private void StaffTo_GotFocus(object sender, RoutedEventArgs e)
        {
            toActivated(true);
        }

        private void StaffTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            toActivated(true);
        }

        private void EntitiesTo_GotFocus(object sender, RoutedEventArgs e)
        {
            toActivated(false);
        }

        private void EntitiesTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            toActivated(false);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (editMode == ByStaff) { removeEntities(); }
            else { removeStaff(); }
        }

        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        {
            setDefault();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            bool confirm = MessageFunctions.ConfirmOKCancel("Are you sure you wish to save your amendments?", "Save changes?");
            if (!confirm) { return; }
            bool success = (editMode == ByStaff) ? StaffFunctions.SaveStaffEntitiesChanges(selectedStaffID) : StaffFunctions.SaveEntityStaffChanges(selectedEntityID);
            if (success)
            {
                MessageFunctions.SuccessMessage("Your changes have been saved successfully. You can make further changes, go back to the previous screen, or close the current page.", "Changes Saved");
                CommitButton.IsEnabled = false;            
            }
        }

        private void StaffCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StaffFunctions.IgnoreAnyChanges())
            {
                StaffFunctions.ClearAnyChanges();
                selectedStaffName = StaffCombo.SelectedItem.ToString();
                Globals.SelectedStaffMember = StaffFunctions.GetStaffMemberByName(selectedStaffName);
                selectedStaffID = Globals.SelectedStaffMember.ID;                
                refreshEntitySummaries(true);
            }
        }
 

    } // class
} // namespace
