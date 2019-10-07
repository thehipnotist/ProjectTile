﻿using System;
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
    /// Interaction logic for ProjectProductsPage.xaml
    /// </summary>
    public partial class ProjectProductsPage : Page
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //

        // Global/page parameters //
        string pageMode;
        string fromSource = "";
        string backSource = "";

        // Current variables //
        bool activeOnly = false;
        string nameContains = "";
        int selectedProductID = 0;

        string activeInstructions = "Select a project and click 'Products', or select an Product and click 'Projects'.";
        string activePageHeader = "Products for each Project";

        enum modeType { ProductsOfProject, ProjectsForProduct }
        modeType ByProject = modeType.ProductsOfProject;
        modeType ByProduct = modeType.ProjectsForProduct;
        modeType editMode;

        // Lists //
        List<Products> productComboList;
        List<ProjectSummaryRecord> projectGridList;

        // Current records //
        ProjectSummaryRecord selectedGridRecord;
        Products selectedProduct;
        ProjectProductSummary selectedProjectProduct;

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public ProjectProductsPage()
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
                refreshProductCombo();
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                ProjectFunctions.ReturnToTilesPage();
            }

            CommitButton.Visibility = Visibility.Hidden;
            ProjectFrom.Visibility = ProjectTo.Visibility = Visibility.Hidden;
            ProjectLabel.Margin = NameContainsLabel.Margin;
            ProjectCombo.Margin = NameContains.Margin;

            if (Globals.SelectedProjectSummary != null && Globals.SelectedProjectSummary.ProjectID > 0) // Opened from the Projects Page
            {
                fromSource = "ProjectPage";
                ProjectCombo.IsEnabled = false; // Cannot easily recreate the same selection list
                refreshProjectDataGrid(); // Ensure the record we want is listed, though
                viewProductsByProject();

                if (pageMode == PageFunctions.View) { Instructions.Content = "Note that only products you can access yourself are displayed."; }
                else { Instructions.Content = "Select the products this user should have, then click 'Save'. You can then choose other project from the list."; }
            }
            else
            {
                fromSource = Globals.TilesPageName;
                ProjectLabel.Visibility = ProjectCombo.Visibility = Visibility.Hidden;
                BackButton.Visibility = Visibility.Hidden;
                ProductFrom.Visibility = ProductTo.Visibility = Visibility.Hidden;

                AddButton.Visibility = RemoveButton.Visibility = Visibility.Hidden;
                VersionLabel.Visibility = Version.Visibility = Visibility.Hidden;
                FromLabel.Visibility = ToLabel.Visibility = Visibility.Hidden;

                if (pageMode == PageFunctions.View)
                {
                    ProjectButton.Visibility = Visibility.Hidden;
                    Instructions.Content = "Select a project and click 'Products', or select a product to see its projects.";
                }
                else { Instructions.Content = activeInstructions; }
            }
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //
        private void refreshProductCombo()
        {
            try
            {
                productComboList = ProductFunctions.ProductsList("", true);
                ProductCombo.ItemsSource = productComboList;
                ProductCombo.SelectedItem = productComboList.FirstOrDefault(p => p.ProductName == Globals.AllRecords);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating product filter list", generalException); }
        }

        // Other/shared functions //
        private void refreshProjectDataGrid()
        {
            try
            {
                int selectedID = (Globals.SelectedProjectSummary != null) ? Globals.SelectedProjectSummary.ProjectID : 0;

                projectGridList = ProjectFunctions.ProjectGridListByProduct(activeOnly, nameContains, selectedProductID, Globals.CurrentEntityID); 
                ProjectDataGrid.ItemsSource = projectGridList;
                ProjectDataGrid.Items.SortDescriptions.Clear();
                ProjectDataGrid.Items.SortDescriptions.Add(new SortDescription("ProjectCode", ListSortDirection.Ascending));

                if (selectedID > 0)
                {
                    try
                    {
                        if (projectGridList.Exists(pgl => pgl.ProjectID == selectedID))
                        {
                            ProjectDataGrid.SelectedItem = projectGridList.First(pgl => pgl.ProjectID == selectedID);
                            ProjectDataGrid.ScrollIntoView(ProjectDataGrid.SelectedItem);
                        }
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error selecting record", generalException); }
                }

                // refreshProjectSummaries(true);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error filling project grid", generalException); }
        }

        private void nameFilter()
        {
            nameContains = NameContains.Text;
            refreshProjectDataGrid();
        }

        private void clearSelection()
        {
            selectedGridRecord = null;
            // selectedProjectID = 0; // Don't clear this automatically, as the refresh tries to reuse it
            Globals.SelectedProjectSummary = null; // Ditto
            ProductButton.IsEnabled = false;
        }

        private void disableButtons()
        {
            AddButton.IsEnabled = RemoveButton.IsEnabled = Version.IsEnabled = false;
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
                Instructions.Content = activeInstructions;
                FromLabel.Visibility = ToLabel.Visibility = Visibility.Hidden;
                ProductVersionLabel.Content = "";
            }
            else
            {
                selectionOnly = Visibility.Hidden;
                editOnly = Visibility.Visible;
                backSource = (fromSource == Globals.TilesPageName) ? editMode.ToString() : fromSource;
                BackButton.Visibility = Visibility.Visible;
                ToLabel.Content = (editMode == ByProject) ? "Linked Products (Live in Bold)" : "Linked Projects (Live Products in Bold)";

                if (pageMode == PageFunctions.View)
                {
                    Instructions.Content = "";
                    FromLabel.Visibility = Visibility.Hidden;
                    ToLabel.Visibility = Visibility.Visible;
                }
                else
                {
                    Instructions.Content = (editMode == ByProject) ? "Move products to the right to add them, or left to remove them." 
                        : "Move projects to the right to add them, or left to remove them.";
                    FromLabel.Visibility = ToLabel.Visibility = Visibility.Visible;
                    FromLabel.Content = (editMode == ByProject) ? "Available Client Products" : "Available Projects";

                    string borderBrush = (editMode == ByProject) ? "PtBrushProduct3" : "PtBrushProject3";
                    AddButton.BorderBrush = RemoveButton.BorderBrush = Application.Current.Resources[borderBrush] as SolidColorBrush;
                    VersionLabel.HorizontalAlignment = Version.HorizontalAlignment = (editMode == ByProject) ? 
                        HorizontalAlignment.Right : HorizontalAlignment.Left;
                }

                disableButtons();
            }

            ProjectDataGrid.Visibility = selectionOnly;
            NameContainsLabel.Visibility = NameContains.Visibility = selectionOnly;
            ActiveOnlyCheckBox.Visibility = (editMode == ByProduct || selectionMode) ? Visibility.Visible : Visibility.Hidden;

            ProductButton.Visibility = selectionOnly;
            ProjectButton.Visibility = selectionOnly;
            ProjectButton.IsEnabled = (selectionMode && selectedProductID != 0);

            CommitButton.Visibility = (pageMode == PageFunctions.View) ? Visibility.Hidden : editOnly;
            ProjectLabel.Visibility = ProjectCombo.Visibility = (editMode == ByProject && !selectionMode) ? Visibility.Visible : Visibility.Hidden;

            ProjectFrom.Visibility = (editMode == ByProduct && !selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            ProjectTo.Visibility = (editMode == ByProduct && !selectionMode) ? Visibility.Visible : Visibility.Hidden;
            ProductFrom.Visibility = (editMode == ByProject && !selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            ProductTo.Visibility = (editMode == ByProject && !selectionMode) ? Visibility.Visible : Visibility.Hidden;

            AddButton.Visibility = RemoveButton.Visibility = (!selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            VersionLabel.Visibility = Version.Visibility = AddButton.Visibility;
            ProductLabel.Visibility = ProductCombo.Visibility = (editMode == ByProduct || selectionMode) ? Visibility.Visible : Visibility.Hidden;
        }

        private void viewProductsByProject()
        {
            editMode = ByProject;
            toggleSelectionControls(false);
            try
            {
                ProjectCombo.ItemsSource = projectGridList;
                if (Globals.SelectedProjectSummary != null)
                {
                    ProjectCombo.SelectedItem = projectGridList.First(pgl => pgl.ProjectID == Globals.SelectedProjectSummary.ProjectID);
                }
                refreshProductSummaries(true);
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error populating project list", generalException);
                return;
            }
        }

        private void viewProjectsByProduct()
        {
            editMode = ByProduct;
            toggleSelectionControls(false);
            refreshProjectSummaries(true);
        }

        private void refreshProjectSummaries(bool fromDatabase)
        {
            if (fromDatabase)
            {
                ProjectFunctions.ProjectsNotForProduct = ProjectFunctions.ProjectsWithoutProduct(activeOnly, selectedProductID);
                ProjectFunctions.ProjectsForProduct = ProjectFunctions.ProjectsWithProduct(activeOnly, selectedProductID);
            }

            ProjectFrom.ItemsSource = ProjectFunctions.ProjectsNotForProduct;
            ProjectFrom.Items.SortDescriptions.Clear();
            ProjectFrom.Items.SortDescriptions.Add(new SortDescription("ProjectName", ListSortDirection.Ascending));
            ProjectFrom.Items.Refresh();
            ProjectFrom.SelectedItem = null;

            ProjectTo.ItemsSource = ProjectFunctions.ProjectsForProduct;
            ProjectTo.Items.SortDescriptions.Clear();
            ProjectTo.Items.SortDescriptions.Add(new SortDescription("ProjectName", ListSortDirection.Ascending));
            ProjectTo.Items.Refresh();
            ProjectTo.SelectedItem = null;

            disableButtons();
            if (selectedProduct != null)
            {
                PageHeader.Content = "Projects with Product '" + selectedProduct.ProductName + "'";
                if (ProjectFrom.Visibility == Visibility.Visible)
                {
                    ProductVersionLabel.Content = "The latest version of " + selectedProduct.ProductName + " is " + selectedProduct.LatestVersion.ToString() + ".";
                }
            }
        }

        private void refreshProductSummaries(bool fromDatabase)
        {
            if (fromDatabase)
            {
                ProjectFunctions.ProductsNotForProject = ProjectFunctions.UnlinkedProducts(Globals.SelectedProjectSummary.ProjectID);
                ProjectFunctions.ProductsForProject = ProjectFunctions.LinkedProducts(Globals.SelectedProjectSummary.ProjectID);
            }
            ProductFrom.ItemsSource = ProjectFunctions.ProductsNotForProject;
            ProductFrom.Items.SortDescriptions.Clear();
            ProductFrom.Items.SortDescriptions.Add(new SortDescription("ProductName", ListSortDirection.Ascending));
            ProductFrom.Items.Refresh();
            ProductFrom.SelectedItem = null;

            ProductTo.ItemsSource = ProjectFunctions.ProductsForProject;
            ProductTo.Items.SortDescriptions.Clear();
            ProductTo.Items.SortDescriptions.Add(new SortDescription("ProductName", ListSortDirection.Ascending));
            ProductTo.Items.Refresh();
            ProductTo.SelectedItem = null;

            disableButtons();
            if (Globals.SelectedProjectSummary != null)
            {
                PageHeader.Content = "Products for " + Globals.SelectedProjectSummary.ProjectName;
            }
        }

        private void fromActivated(bool ProjectList)
        {
            try
            {
                AddButton.IsEnabled = (ProjectList && ProjectFrom.SelectedItems != null) || (!ProjectList && ProductFrom.SelectedItems != null);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error activating the 'Add' button", generalException); }
        }

        //private void toggleDisableButton(bool IsLive)
        //{
        //    if (IsLive)
        //    {
        //        DisableButtonText.Text = "Disable";
        //        DisableImage.Visibility = Visibility.Visible;
        //        EnableImage.Visibility = Visibility.Collapsed;
        //    }
        //    else
        //    {
        //        DisableButtonText.Text = "Activate";
        //        DisableImage.Visibility = Visibility.Collapsed;
        //        EnableImage.Visibility = Visibility.Visible;
        //    }
        //}

        private void toActivated(bool ProjectList)
        {
            try
            {
                bool projectSelected = (ProjectList && ProjectTo.SelectedItem != null);
                bool productSelected = (!ProjectList && ProductTo.SelectedItem != null);

                if (projectSelected) { selectedProjectProduct = (ProjectProductSummary)ProjectTo.SelectedItem; }
                else if (productSelected) { selectedProjectProduct = (ProjectProductSummary)ProductTo.SelectedItem; }
                else { selectedProjectProduct = null; }

                if (selectedProjectProduct != null)
                {
                    Version.Text = selectedProjectProduct.NewVersion.ToString("#.0");
                    RemoveButton.IsEnabled = Version.IsEnabled = true;
                }
                else
                {
                    Version.Text = "";
                    RemoveButton.IsEnabled = Version.IsEnabled = false;
                }

            }
            catch (Exception generalException) { MessageFunctions.Error("Error activating the 'Remove' and 'Active' buttons", generalException); }
        }

        private void addProject()
        {
            try
            {
                if (ProjectFrom.SelectedItems != null)
                {
                    List<Projects> addList = new List<Projects>();
                    foreach (var selectedRow in ProjectFrom.SelectedItems)
                    {
                        addList.Add((Projects)selectedRow);
                    }

                    bool success = ProjectFunctions.ToggleProductProjects(addList, true, selectedProduct);
                    if (success)
                    {
                        refreshProjectSummaries(false);
                        CommitButton.IsEnabled = true;
                    }
                }
                else
                {
                    MessageFunctions.Error("Error adding project to Product: no project selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error adding project to Product", generalException);
            }
        }

        private void addProducts()
        {
            try
            {
                if (ProductFrom.SelectedItems != null)
                {
                    List<ClientProductSummary> addList = new List<ClientProductSummary>();
                    foreach (var selectedRow in ProductFrom.SelectedItems)
                    {
                        addList.Add((ClientProductSummary)selectedRow);
                    }

                    bool success = ProjectFunctions.ToggleProjectProducts(addList, true, Globals.SelectedProjectSummary);
                    if (success)
                    {
                        refreshProductSummaries(false);
                        CommitButton.IsEnabled = true;
                    }
                }
                else
                {
                    MessageFunctions.Error("Error adding products to project: no product selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error adding products to project", generalException);
            }
        }

        private void removeProject()
        {
            try
            {
                if (ProjectTo.SelectedItem != null)
                {
                    List<Projects> removeList = new List<Projects>();
                    ProjectProductSummary thisRecord = (ProjectProductSummary) ProjectTo.SelectedItem;
                    Projects thisProject = ProjectFunctions.GetProject(thisRecord.ProjectID);
                    removeList.Add(thisProject);

                    bool success = ProjectFunctions.ToggleProductProjects(removeList, false, selectedProduct);
                    if (success)
                    {
                        refreshProjectSummaries(false);
                        CommitButton.IsEnabled = true;
                    }
                }
                else
                {
                    MessageFunctions.Error("Error removing project from Product: no project selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error removing project from Product", generalException);
            }
        }

        private void removeProduct()
        {
            try
            {
                if (ProductTo.SelectedItem != null)
                {
                    List<ClientProductSummary> removeList = new List<ClientProductSummary>();
                    ClientProductSummary thisProduct = null;
                    ProjectProductSummary thisRecord = (ProjectProductSummary) ProductTo.SelectedItem;
                    if (thisRecord.ClientID > 0)
                    {
                        thisProduct = ClientFunctions.ClientsWithProduct(false, thisRecord.ProductID).FirstOrDefault(cwp => cwp.ClientID == thisRecord.ClientID);
                    }
                    else { thisProduct = ProjectFunctions.DummyClientProduct(thisRecord.Product); }

                    removeList.Add(thisProduct);

                    bool success = ProjectFunctions.ToggleProjectProducts(removeList, false, Globals.SelectedProjectSummary);
                    if (success)
                    {
                        refreshProductSummaries(false);
                        CommitButton.IsEnabled = true;
                    }
                }
                else
                {
                    MessageFunctions.Error("Error removing products from project: no product selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error removing products from project", generalException);
            }
        }
 
        private void clearChanges()
        {
            ProjectFunctions.ClearAnyChanges();
            selectedProjectProduct = null;
        }

        private void toggleActiveOnly(bool isChecked)
        {
            activeOnly = isChecked;
            refreshProjectDataGrid();
            if (ProjectTo.Visibility == Visibility.Visible)
            {
                if (ProjectFunctions.IgnoreAnyChanges())
                {
                    clearChanges();
                    refreshProjectSummaries(true);
                }
            }
        }

        private void refreshEditPage()
        {
            if (editMode == ByProject) { refreshProductSummaries(false); }
            else { refreshProjectSummaries(false); }
        }

        // ---------------------- //
        // -- Event Management -- //
        //   ---------------------- //

        // Generic (shared) control events //


        // Control-specific events //
        private void ActiveOnlyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            toggleActiveOnly(true);
        }

        private void ActiveOnlyCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            toggleActiveOnly(false);
        }

        private void NameContains_LostFocus(object sender, RoutedEventArgs e)
        {
            nameFilter();
        }

        private void NameContains_KeyUp(object sender, KeyEventArgs e)
        {
            nameFilter();
        }

        private void ProjectDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ProjectDataGrid.SelectedItem != null)
                {
                    selectedGridRecord = (ProjectSummaryRecord)ProjectDataGrid.SelectedItem;
                    Globals.SelectedProjectSummary = selectedGridRecord;
                    ProductButton.IsEnabled = true;
                }
                else // No record selected, e.g. because filter changed
                {
                    clearSelection();
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error processing selection change", generalException);
                Globals.SelectedProjectSummary = null; // Avoid accidentally using the previous selection
                clearSelection();
            }
        }

        private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectFunctions.IgnoreAnyChanges())
            {
                clearChanges();
                Products selectedRecord = (Products) ProductCombo.SelectedItem;
                if (selectedRecord.ProductName == Globals.AllRecords)
                {
                    selectedProductID = 0;
                    selectedProduct = null;
                    ProjectButton.IsEnabled = false;
                }
                else
                {
                    try
                    {
                        selectedProduct = selectedRecord;
                        selectedProductID = selectedProduct.ID;
                        ProjectButton.IsEnabled = true;
                    }
                    catch (Exception generalException)
                    {
                        MessageFunctions.Error("Error changing product selection", generalException);
                        selectedProductID = 0;
                        selectedProduct = null;
                        ProjectButton.IsEnabled = false;
                    }
                }
                refreshProjectDataGrid();
                refreshProjectSummaries(true);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectFunctions.IgnoreAnyChanges())
            {
                clearChanges();
                ProjectFunctions.ReturnToTilesPage();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectFunctions.IgnoreAnyChanges())
            {
                clearChanges();
                if (backSource == "ProjectPage")
                {
                    PageFunctions.ShowProjectPage(pageMode = Globals.ProjectSourceMode);
                }
                else
                {
                    refreshProjectDataGrid();
                    toggleSelectionControls(true);
                    PageHeader.Content = activePageHeader;
                }
            }
        }

        private void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            viewProductsByProject();
        }

        private void ProjectButton_Click(object sender, RoutedEventArgs e)
        {
            viewProjectsByProduct();
        }

        private void ProjectFrom_GotFocus(object sender, RoutedEventArgs e)
        {
            fromActivated(true);
        }

        private void ProjectFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fromActivated(true);
        }

        private void ProductFrom_GotFocus(object sender, RoutedEventArgs e)
        {
            fromActivated(false);
        }

        private void ProductFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fromActivated(false);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (editMode == ByProject) { addProducts(); }
            else { addProject(); }
        }

        private void ProjectTo_GotFocus(object sender, RoutedEventArgs e)
        {
            toActivated(true);
        }

        private void ProjectTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            toActivated(true);
        }

        private void ProductTo_GotFocus(object sender, RoutedEventArgs e)
        {
            toActivated(false);
        }

        private void ProductTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            toActivated(false);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (editMode == ByProject) { removeProduct(); }
            else { removeProject(); }
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            bool confirm = MessageFunctions.ConfirmOKCancel("Are you sure you wish to save your amendments?", "Save changes?");
            if (!confirm) { return; }
            //bool success = (editMode == ByProject) ? ProjectFunctions.SaveProjectProductChanges(Globals.SelectedProjectSummary.ProjectID) : ProjectFunctions.SaveProductProjectChanges(selectedProductID);
            //if (success)
            //{
            //    MessageFunctions.SuccessMessage("Your changes have been saved successfully. You can make further changes, go back to the previous screen, or close the current page.", "Changes Saved");
            //    CommitButton.IsEnabled = false;
            //}
        }

        private void ProjectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectFunctions.IgnoreAnyChanges())
            {
                clearChanges();
                if (ProjectCombo.SelectedItem != null)
                {
                    selectedGridRecord = (ProjectSummaryRecord)ProjectCombo.SelectedItem;
                    Globals.SelectedProjectSummary = selectedGridRecord;
                    refreshProductSummaries(true);
                }
            }
        }

        private void Version_LostFocus(object sender, RoutedEventArgs e)
        {            
            if (selectedProjectProduct == null) { MessageFunctions.Error("No project product record selected.", null); }
            bool success = ProjectFunctions.AmendVersion(selectedProjectProduct, Version.Text, (editMode == ByProject));
            if (success) 
            { 
                refreshEditPage();
                CommitButton.IsEnabled = true;
            }
            else { Version.Text = selectedProjectProduct.NewVersion.ToString("#.0"); }
        }

        private void Version_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Globals.ClientVersionFormat.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
        }


    } // class
} // namespace
