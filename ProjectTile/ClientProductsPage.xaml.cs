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
    /// Interaction logic for ClientProductsPage.xaml
    /// </summary>
    public partial class ClientProductsPage : Page
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
        bool resetProductSelection = false;

        string activeInstructions = "Select a client and click 'Products', or select an Product and click 'Clients'.";
        string activePageHeader = "Products for each Client";

        enum modeType { ProductsOfClient, ClientsForProduct }
        modeType ByClient = modeType.ProductsOfClient;
        modeType ByProduct = modeType.ClientsForProduct;
        modeType editMode;

        // Lists //
        List<Products> productComboList;
        List<ClientSummaryRecord> clientGridList;

        // Current records //
        ClientSummaryRecord selectedGridRecord;
        Products selectedProduct;
        ClientProductSummary selectedClientProduct;

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public ClientProductsPage()
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
                refreshProductCombo(true);
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                ClientFunctions.ReturnToTilesPage();
            }

            CommitButton.Visibility = Visibility.Hidden;
            ClientFrom.Visibility = ClientTo.Visibility = Visibility.Hidden;
            ClientLabel.Margin = NameContainsLabel.Margin;
            ClientCombo.Margin = NameContains.Margin;
            if (!Globals.MyPermissions.Allow("ActivateClientProducts"))
            {
                DisableButton.IsEnabled = false;
                DisableButton.ToolTip = "Your current permissions do not allow activating or disabling client products";
            }   

            if (Globals.SelectedClient != null) // Opened from the Clients Page
            {
                fromSource = "ClientPage";
                ClientCombo.IsEnabled = false; // Cannot easily recreate the same selection list
                refreshClientDataGrid(); // Ensure the record we want is listed, though
                viewProductsByClient();

                if (pageMode == PageFunctions.View) { Instructions.Content = "Note that only products you can access yourself are displayed."; }
                else { Instructions.Content = "Select the products this user should have, then click 'Save'. You can then choose other client from the list."; }
            }
            else
            {
                fromSource = Globals.TilesPageName;
                ClientLabel.Visibility = ClientCombo.Visibility = Visibility.Hidden;
                BackButton.Visibility = Visibility.Hidden;
                ProductFrom.Visibility = ProductTo.Visibility = Visibility.Hidden;

                AddButton.Visibility = RemoveButton.Visibility = Visibility.Hidden;
                VersionLabel.Visibility = Version.Visibility = DisableButton.Visibility = Visibility.Hidden;
                FromLabel.Visibility = ToLabel.Visibility = Visibility.Hidden;

                if (pageMode == PageFunctions.View)
                {
                    ClientButton.Visibility = Visibility.Hidden;
                    Instructions.Content = "Select a client and click 'Products', or select a product to see its clients.";
                }
                else { Instructions.Content = activeInstructions; }
            }
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //
        private void refreshProductCombo(bool includeAll)
        {
            try
            {
                productComboList = ProductFunctions.ProductsList("", includeAll);
                ProductCombo.ItemsSource = productComboList;
                ProductCombo.SelectedItem = productComboList.FirstOrDefault(p => p.ID == selectedProductID);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating Product filter list", generalException); }
        }

        // Other/shared functions //
        private void refreshClientDataGrid()
        {
            try
            {
                int selectedID = (Globals.SelectedClient != null) ? Globals.SelectedClient.ID : 0;

                clientGridList = ClientFunctions.ClientGridListByProduct(activeOnly, nameContains, selectedProductID, Globals.CurrentEntityID); 
                ClientDataGrid.ItemsSource = clientGridList;
                ClientDataGrid.Items.SortDescriptions.Clear();
                ClientDataGrid.Items.SortDescriptions.Add(new SortDescription("ClientCode", ListSortDirection.Ascending));

                if (selectedID > 0)
                {
                    try
                    {
                        if (clientGridList.Exists(c => c.ID == selectedID))
                        {
                            ClientDataGrid.SelectedItem = clientGridList.First(c => c.ID == selectedID);
                            ClientDataGrid.ScrollIntoView(ClientDataGrid.SelectedItem);
                        }
                    }
                    catch (Exception generalException) { MessageFunctions.Error("Error selecting record", generalException); }
                }

                // refreshClientSummaries(true);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error filling client grid", generalException); }
        }

        private void nameFilter()
        {
            nameContains = NameContains.Text;
            refreshClientDataGrid();
        }

        private void clearSelection()
        {
            selectedGridRecord = null;
            // selectedClientID = 0; // Don't clear this automatically, as the refresh tries to reuse it
            Globals.SelectedClient = null; // Ditto
            ProductButton.IsEnabled = false;
        }

        private void disableButtons()
        {
            AddButton.IsEnabled = DisableButton.IsEnabled = RemoveButton.IsEnabled = Version.IsEnabled = false;
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
                ToLabel.Content = (editMode == ByClient) ? "Linked Products (Live in Bold)" : "Linked Clients (Live in Bold)";

                if (pageMode == PageFunctions.View)
                {
                    Instructions.Content = "";
                    FromLabel.Visibility = Visibility.Hidden;
                    ToLabel.Visibility = Visibility.Visible;
                }
                else
                {
                    Instructions.Content = (editMode == ByClient) ? "Move products to the right to add them, or left to remove them." 
                        : "Move clients to the right to add them, or left to remove them.";
                    FromLabel.Visibility = ToLabel.Visibility = Visibility.Visible;
                    FromLabel.Content = (editMode == ByClient) ? "Available Products" : "Available Clients";

                    string borderBrush = (editMode == ByClient) ? "PtBrushProduct3" : "PtBrushClient3";
                    AddButton.BorderBrush = RemoveButton.BorderBrush = DisableButton.BorderBrush = Application.Current.Resources[borderBrush] as SolidColorBrush;
                    VersionLabel.HorizontalAlignment = Version.HorizontalAlignment = DisableButton.HorizontalAlignment = (editMode == ByClient) ? 
                        HorizontalAlignment.Right : HorizontalAlignment.Left;
                }

                disableButtons();
            }

            ClientDataGrid.Visibility = selectionOnly;
            NameContainsLabel.Visibility = NameContains.Visibility = selectionOnly;
            ActiveOnlyCheckBox.Visibility = (editMode == ByProduct || selectionMode) ? Visibility.Visible : Visibility.Hidden;

            ProductButton.Visibility = selectionOnly;
            ClientButton.Visibility = selectionOnly;
            ClientButton.IsEnabled = (selectionMode && selectedProductID != 0);

            CommitButton.Visibility = (pageMode == PageFunctions.View) ? Visibility.Hidden : editOnly;
            ClientLabel.Visibility = ClientCombo.Visibility = (editMode == ByClient && !selectionMode) ? Visibility.Visible : Visibility.Hidden;

            ClientFrom.Visibility = (editMode == ByProduct && !selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            ClientTo.Visibility = (editMode == ByProduct && !selectionMode) ? Visibility.Visible : Visibility.Hidden;
            ProductFrom.Visibility = (editMode == ByClient && !selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            ProductTo.Visibility = (editMode == ByClient && !selectionMode) ? Visibility.Visible : Visibility.Hidden;

            AddButton.Visibility = RemoveButton.Visibility = (!selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            VersionLabel.Visibility = Version.Visibility = DisableButton.Visibility = AddButton.Visibility;
            ProductLabel.Visibility = ProductCombo.Visibility = (editMode == ByProduct || selectionMode) ? Visibility.Visible : Visibility.Hidden;
        }

        private void viewProductsByClient()
        {
            editMode = ByClient;
            toggleSelectionControls(false);
            try
            {
                ClientCombo.ItemsSource = clientGridList;
                ClientCombo.SelectedItem = clientGridList.First(s => s.ID == Globals.SelectedClient.ID);
                refreshProductSummaries(true);
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error populating client list", generalException);
                return;
            }
        }

        private void viewClientsByProduct()
        {
            refreshProductCombo(false);
            editMode = ByProduct;
            toggleSelectionControls(false);
            refreshClientSummaries(true);
        }

        private void refreshClientSummaries(bool fromDatabase)
        {
            if (fromDatabase)
            {
                ClientFunctions.ClientsNotForProduct = ClientFunctions.ClientsWithoutProduct(activeOnly, selectedProductID);
                ClientFunctions.ClientsForProduct = ClientFunctions.ClientsWithProduct(activeOnly, selectedProductID);
            }

            ClientFrom.ItemsSource = ClientFunctions.ClientsNotForProduct;
            ClientFrom.Items.SortDescriptions.Clear();
            ClientFrom.Items.SortDescriptions.Add(new SortDescription("ClientName", ListSortDirection.Ascending));
            ClientFrom.Items.Refresh();
            ClientFrom.SelectedItem = null;

            ClientTo.ItemsSource = ClientFunctions.ClientsForProduct;
            ClientTo.Items.SortDescriptions.Clear();
            ClientTo.Items.SortDescriptions.Add(new SortDescription("ClientName", ListSortDirection.Ascending));
            ClientTo.Items.Refresh();
            ClientTo.SelectedItem = null;

            disableButtons();
            if (selectedProduct != null)
            {
                PageHeader.Content = "Clients with Product '" + selectedProduct.ProductName + "'";
                if (ClientFrom.Visibility == Visibility.Visible)
                {
                    ProductVersionLabel.Content = "The latest version of " + selectedProduct.ProductName + " is " + selectedProduct.LatestVersion.ToString() + ".";
                }
            }
        }

        private void refreshProductSummaries(bool fromDatabase)
        {
            if (fromDatabase)
            {
                ClientFunctions.ProductsNotForClient = ClientFunctions.UnlinkedProducts(Globals.SelectedClient.ID);
                ClientFunctions.ProductsForClient = ClientFunctions.LinkedProducts(Globals.SelectedClient.ID);
            }
            ProductFrom.ItemsSource = ClientFunctions.ProductsNotForClient;
            ProductFrom.Items.SortDescriptions.Clear();
            ProductFrom.Items.SortDescriptions.Add(new SortDescription("ProductName", ListSortDirection.Ascending));
            ProductFrom.Items.Refresh();
            ProductFrom.SelectedItem = null;

            ProductTo.ItemsSource = ClientFunctions.ProductsForClient;
            ProductTo.Items.SortDescriptions.Clear();
            ProductTo.Items.SortDescriptions.Add(new SortDescription("ProductName", ListSortDirection.Ascending));
            ProductTo.Items.Refresh();
            ProductTo.SelectedItem = null;

            disableButtons();
            if (Globals.SelectedClient != null)
            {
                PageHeader.Content = "Products for " + Globals.SelectedClient.ClientName;
            }
        }

        private void fromActivated(bool ClientList)
        {
            try
            {
                AddButton.IsEnabled = (ClientList && ClientFrom.SelectedItems != null) || (!ClientList && ProductFrom.SelectedItems != null);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error activating the 'Add' button", generalException); }
        }

        private void toggleDisableButton(bool IsLive)
        {
            if (IsLive)
            {
                DisableButtonText.Text = "Disable";
                DisableImage.Visibility = Visibility.Visible;
                EnableImage.Visibility = Visibility.Collapsed;
            }
            else
            {
                DisableButtonText.Text = "Activate";
                DisableImage.Visibility = Visibility.Collapsed;
                EnableImage.Visibility = Visibility.Visible;
            }
        }
        
        private void toActivated(bool ClientList)
        {
            try
            {
                bool clientSelected = (ClientList && ClientTo.SelectedItem != null); 
                bool productSelected = (!ClientList && ProductTo.SelectedItem != null);
                
                if (clientSelected) { selectedClientProduct = (ClientProductSummary) ClientTo.SelectedItem;  }
                else if (productSelected) { selectedClientProduct = (ClientProductSummary)ProductTo.SelectedItem; }
                else { selectedClientProduct = null; }

                if (selectedClientProduct != null)
                {
                    Version.Text = selectedClientProduct.ClientVersion.ToString("#.0");
                    RemoveButton.IsEnabled = Version.IsEnabled = true;
                    DisableButton.IsEnabled = (Globals.MyPermissions.Allow("ActivateClientProducts"));
                    toggleDisableButton(selectedClientProduct.Live);
                }
                else
                {
                    Version.Text = "";
                    RemoveButton.IsEnabled = DisableButton.IsEnabled = Version.IsEnabled = false;
                }

            }
            catch (Exception generalException) { MessageFunctions.Error("Error activating the 'Remove' and 'Active' buttons", generalException); }
        }

        private void addClient()
        {
            try
            {
                if (ClientFrom.SelectedItems != null)
                {
                    List<Clients> addList = new List<Clients>();
                    foreach (var selectedRow in ClientFrom.SelectedItems)
                    {
                        addList.Add((Clients)selectedRow);
                    }

                    bool success = ClientFunctions.ToggleProductClients(addList, true, selectedProduct);
                    if (success)
                    {
                        refreshClientSummaries(false);
                        CommitButton.IsEnabled = true;
                    }
                }
                else
                {
                    MessageFunctions.Error("Error adding client to Product: no client selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error adding client to Product", generalException);
            }
        }

        private void addProducts()
        {
            try
            {
                if (ProductFrom.SelectedItems != null)
                {
                    List<Products> addList = new List<Products>();
                    foreach (var selectedRow in ProductFrom.SelectedItems)
                    {
                        addList.Add((Products)selectedRow);
                    }

                    bool success = ClientFunctions.ToggleClientProducts(addList, true, Globals.SelectedClient);
                    if (success)
                    {
                        refreshProductSummaries(false);
                        CommitButton.IsEnabled = true;
                    }
                }
                else
                {
                    MessageFunctions.Error("Error adding products to client: no product selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error adding products to client", generalException);
            }
        }

        private void removeClient()
        {
            try
            {
                if (ClientTo.SelectedItem != null)
                {
                    List<Clients> removeList = new List<Clients>();
                    ClientProductSummary thisRecord = (ClientProductSummary) ClientTo.SelectedItem;
                    Clients thisClient = ClientFunctions.GetClientByID(thisRecord.ClientID);
                    removeList.Add(thisClient);

                    bool success = ClientFunctions.ToggleProductClients(removeList, false, selectedProduct);
                    if (success)
                    {
                        refreshClientSummaries(false);
                        CommitButton.IsEnabled = true;
                    }
                }
                else
                {
                    MessageFunctions.Error("Error removing client from Product: no client selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error removing client from Product", generalException);
            }
        }

        private void removeProduct()
        {
            try
            {
                if (ProductTo.SelectedItem != null)
                {
                    List<Products> removeList = new List<Products>();
                    ClientProductSummary thisRecord = (ClientProductSummary) ProductTo.SelectedItem;
                    Products thisProduct = ProductFunctions.GetProductByID(thisRecord.ProductID);
                    removeList.Add(thisProduct);

                    bool success = ClientFunctions.ToggleClientProducts(removeList, false, Globals.SelectedClient);
                    if (success)
                    {
                        refreshProductSummaries(false);
                        CommitButton.IsEnabled = true;
                    }
                }
                else
                {
                    MessageFunctions.Error("Error removing products from client: no product selected.", null);
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error removing products from client", generalException);
            }
        }
 
        private void clearChanges()
        {
            ClientFunctions.ClearAnyChanges();
            selectedClientProduct = null;
        }

        private void toggleActiveOnly(bool isChecked)
        {
            activeOnly = isChecked;
            refreshClientDataGrid();
            if (ClientTo.Visibility == Visibility.Visible)
            {
                if (ClientFunctions.IgnoreAnyChanges())
                {
                    clearChanges();
                    refreshClientSummaries(true);
                }
            }
        }

        private void refreshEditPage()
        {
            if (editMode == ByClient) { refreshProductSummaries(false); }
            else { refreshClientSummaries(false); }
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

        private void ClientDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ClientDataGrid.SelectedItem != null)
                {
                    selectedGridRecord = (ClientSummaryRecord)ClientDataGrid.SelectedItem;
                    ClientFunctions.SelectClient(selectedGridRecord.ID);
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
                Globals.SelectedClient = null; // Avoid accidentally using the previous selection
                clearSelection();
            }
        }

        private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductCombo.SelectedItem == null) { return; } // Do nothing, won't be for long
            if (resetProductSelection) // Avoids immediate re-processing if set back to previous selection
            {
                resetProductSelection = false;
                return;
            } 
            if (ClientFunctions.IgnoreAnyChanges())
            {
                clearChanges();
                Products selectedRecord = (Products) ProductCombo.SelectedItem;
                if (selectedRecord.ProductName == Globals.AllRecords)
                {
                    selectedProductID = 0;
                    selectedProduct = null;
                    ClientButton.IsEnabled = false;
                }
                else
                {
                    try
                    {
                        selectedProduct = selectedRecord;
                        selectedProductID = selectedProduct.ID;
                        ClientButton.IsEnabled = true;
                    }
                    catch (Exception generalException)
                    {
                        MessageFunctions.Error("Error changing product selection", generalException);
                        selectedProductID = 0;
                        selectedProduct = null;
                        ClientButton.IsEnabled = false;
                    }
                }
                refreshClientDataGrid();
                refreshClientSummaries(true);
            }
            else
            {
                resetProductSelection = true; // If the user cancels the change, don't re-process                
                ProductCombo.SelectedItem = productComboList.FirstOrDefault(p => p.ID == selectedProductID);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientFunctions.IgnoreAnyChanges())
            {
                clearChanges();
                ClientFunctions.ReturnToTilesPage();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientFunctions.IgnoreAnyChanges())
            {
                clearChanges();
                if (backSource == "ClientPage")
                {
                    PageFunctions.ShowClientPage(pageMode = Globals.ClientSourceMode);
                }
                else
                {
                    refreshProductCombo(true);
                    //refreshClientDataGrid();
                    toggleSelectionControls(true);
                    PageHeader.Content = activePageHeader;
                }
            }
        }

        private void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            viewProductsByClient();
        }

        private void ClientButton_Click(object sender, RoutedEventArgs e)
        {
            viewClientsByProduct();
        }

        private void ClientFrom_GotFocus(object sender, RoutedEventArgs e)
        {
            fromActivated(true);
        }

        private void ClientFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            if (editMode == ByClient) { addProducts(); }
            else { addClient(); }
        }

        private void ClientTo_GotFocus(object sender, RoutedEventArgs e)
        {
            toActivated(true);
        }

        private void ClientTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            if (editMode == ByClient) { removeProduct(); }
            else { removeClient(); }
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            bool confirm = MessageFunctions.ConfirmOKCancel("Are you sure you wish to save your amendments?", "Save changes?");
            if (!confirm) { return; }
            bool success = (editMode == ByClient) ? ClientFunctions.SaveClientProductChanges(Globals.SelectedClient.ID) : ClientFunctions.SaveProductClientChanges(selectedProductID);
            if (success)
            {
                MessageFunctions.SuccessMessage("Your changes have been saved successfully. You can make further changes, go back to the previous screen, or close the current page.", "Changes Saved");
                CommitButton.IsEnabled = false;
            }
        }

        private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientFunctions.IgnoreAnyChanges())
            {
                clearChanges();
                if (ClientCombo.SelectedItem != null)
                {
                    selectedGridRecord = (ClientSummaryRecord)ClientCombo.SelectedItem;
                    ClientFunctions.SelectClient(selectedGridRecord.ID);
                    refreshProductSummaries(true);
                }
            }
        }

        private void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedClientProduct == null) { MessageFunctions.Error("No client product record selected.", null); }
            try
            {
                bool success = false;
                bool byClient = (editMode == ByClient);
                bool enabling = (EnableImage.Visibility == Visibility.Visible);
                
                success = enabling ? ClientFunctions.ActivateProduct(selectedClientProduct, byClient) : ClientFunctions.DisableProduct(selectedClientProduct, byClient); 
                if (success) 
                { 
                    refreshEditPage();
                    CommitButton.IsEnabled = true;
                }
            }
            catch (Exception generalException) { MessageFunctions.Error("Error processing status change", generalException); }
        }

        private void Version_LostFocus(object sender, RoutedEventArgs e)
        {            
            if (selectedClientProduct == null) { MessageFunctions.Error("No client product record selected.", null); }
            bool success = ClientFunctions.AmendVersion(selectedClientProduct, Version.Text, (editMode == ByClient));
            if (success) 
            { 
                refreshEditPage();
                CommitButton.IsEnabled = true;
            }
            else { Version.Text = selectedClientProduct.ClientVersion.ToString("#.0"); }
        }

        private void Version_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Globals.ClientVersionFormat.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
        }


    } // class
} // namespace
