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
        string sourceMode = "";
        string backSource = "";
        //int selectedClientID = 0;

        // Current variables //
        bool activeOnly = false;
        string nameContains = "";
        int selectedProductID = 0;
        string selectedClientName = "";

        string activeInstructions = "Select a client and click 'Products', or select an Product and click 'Clients'.";
        string activePageHeader = "Products for each Client";

        enum modeType { ProductsOfClient, ClientsForProduct }
        modeType ByClient = modeType.ProductsOfClient;
        modeType ByProduct = modeType.ClientsForProduct;
        modeType editMode;

        // Lists //
        List<Products> productComboList;
        List<ClientGridRecord> clientGridList;

        // Current records //
        ClientGridRecord selectedRecord;
        Products selectedProduct;

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
                sourceMode = PageFunctions.pageParameter(this, "SourceMode");
                //selectedClientID = Int32.Parse(PageFunctions.pageParameter(this, "ClientID"));
                refreshProductCombo();
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

            if (ClientFunctions.SelectedClient != null) // Opened from the Clients Page
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
                fromSource = "TilesPage";
                ClientLabel.Visibility = ClientCombo.Visibility = Visibility.Hidden;
                BackButton.Visibility = Visibility.Hidden;
                ProductFrom.Visibility = ProductTo.Visibility = Visibility.Hidden;

                AddButton.Visibility = ActiveButton.Visibility = RemoveButton.Visibility = Visibility.Hidden;
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
        private void refreshProductCombo()
        {
            try
            {
                productComboList = ProductFunctions.ProductsList("", true);
                ProductCombo.ItemsSource = productComboList;
                ProductCombo.SelectedItem = productComboList.FirstOrDefault(p => p.ProductName == PageFunctions.AllRecords);
            }
            catch (Exception generalException) { MessageFunctions.Error("Error populating Product filter list", generalException); }
        }

        // Data retrieval //

        // Other/shared functions //
        private void refreshClientDataGrid()
        {
            try
            {
                int selectedID = (ClientFunctions.SelectedClient != null) ? ClientFunctions.SelectedClient.ID : 0;
                
                clientGridList = ClientFunctions.ClientGridListByProduct(activeOnly, nameContains, selectedProductID, EntityFunctions.CurrentEntityID); 
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
            selectedRecord = null;
            // selectedClientID = 0; // Don't clear this automatically, as the refresh tries to reuse it
            ClientFunctions.SelectedClient = null; // Ditto
            ProductButton.IsEnabled = false;
        }

        private void disableButtons()
        {
            AddButton.IsEnabled = ActiveButton.IsEnabled = RemoveButton.IsEnabled = false;
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
                BackButton.Visibility = (fromSource == "TilesPage") ? Visibility.Hidden : Visibility.Visible;
                Instructions.Content = activeInstructions;
                FromLabel.Visibility = ToLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                selectionOnly = Visibility.Hidden;
                editOnly = Visibility.Visible;
                backSource = (fromSource == "TilesPage") ? editMode.ToString() : fromSource;
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
                    Instructions.Content = (editMode == ByClient) ? "Move products to the right to add them, or left to remove them." : "Move clients to the right to add them, or left to remove them.";
                    FromLabel.Visibility = ToLabel.Visibility = Visibility.Visible;
                    FromLabel.Content = (editMode == ByClient) ? "Available Products" : "Available Clients";

                    string borderBrush = (editMode == ByClient) ? "PtBrushProduct3" : "PtBrushClient3";
                    AddButton.BorderBrush = RemoveButton.BorderBrush = ActiveButton.BorderBrush = Application.Current.Resources[borderBrush] as SolidColorBrush;
                }

                disableButtons();
            }

            ClientDataGrid.Visibility = selectionOnly;
            NameContainsLabel.Visibility = NameContains.Visibility = selectionOnly;
            ActiveOnly_CheckBox.Visibility = (editMode == ByProduct || selectionMode) ? Visibility.Visible : Visibility.Hidden;

            ProductButton.Visibility = selectionOnly;
            ClientButton.Visibility = selectionOnly;
            ClientButton.IsEnabled = (selectionMode && selectedProductID != 0);

            CommitButton.Visibility = (pageMode == PageFunctions.View) ? Visibility.Hidden : editOnly;
            ClientLabel.Visibility = ClientCombo.Visibility = (editMode == ByClient && !selectionMode) ? Visibility.Visible : Visibility.Hidden;

            ClientFrom.Visibility = (editMode == ByProduct && !selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            ClientTo.Visibility = (editMode == ByProduct && !selectionMode) ? Visibility.Visible : Visibility.Hidden;
            ProductFrom.Visibility = (editMode == ByClient && !selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            ProductTo.Visibility = (editMode == ByClient && !selectionMode) ? Visibility.Visible : Visibility.Hidden;

            AddButton.Visibility = ActiveButton.Visibility = RemoveButton.Visibility = (!selectionMode && pageMode == PageFunctions.Amend) ? Visibility.Visible : Visibility.Hidden;
            ProductLabel.Visibility = ProductCombo.Visibility = (editMode == ByProduct || selectionMode) ? Visibility.Visible : Visibility.Hidden;
        }

        private void viewProductsByClient()
        {
            editMode = ByClient;
            toggleSelectionControls(false);
            try
            {
                ClientCombo.ItemsSource = clientGridList;
                ClientCombo.SelectedItem = clientGridList.First(s => s.ID == ClientFunctions.SelectedClient.ID);
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
                PageHeader.Content = "Clients in Product '" + selectedProduct.ProductName + "'";
            }
        }

        private void refreshProductSummaries(bool fromDatabase)
        {
            if (fromDatabase)
            {
                ClientFunctions.ProductsNotForClient = ClientFunctions.UnlinkedProducts(ClientFunctions.SelectedClient.ID);
                ClientFunctions.ProductsForClient = ClientFunctions.LinkedProducts(ClientFunctions.SelectedClient.ID);
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
            if (selectedClientName != "")
            {
                PageHeader.Content = "Products for " + selectedClientName;
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

        private void toActivated(bool ClientList)
        {
            try
            {
                RemoveButton.IsEnabled = ActiveButton.IsEnabled = (ClientList && ClientTo.SelectedItems != null) || (!ClientList && ProductTo.SelectedItems != null);
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

                    bool success = ClientFunctions.ToggleClientProducts(addList, true, ClientFunctions.SelectedClient);
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
                if (ClientTo.SelectedItems != null)
                {
                    List<Clients> removeList = new List<Clients>();
                    foreach (ClientProductSummary selectedRow in ClientTo.SelectedItems)
                    {
                        Clients thisClient = ClientFunctions.GetClientByID(selectedRow.ClientID, false);
                        removeList.Add(thisClient);
                    }

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

        private void removeProducts()
        {
            try
            {
                if (ProductTo.SelectedItems != null)
                {
                    List<Products> removeList = new List<Products>();
                    foreach (ClientProductSummary selectedRow in ProductTo.SelectedItems)
                    {
                        Products thisProduct = ProductFunctions.GetProductByID(selectedRow.ProductID);
                        removeList.Add(thisProduct);
                    }

                    bool success = ClientFunctions.ToggleClientProducts(removeList, false, ClientFunctions.SelectedClient);
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

/*
        private void setActive()
        {
            if (editMode == ByClient)
            {
                try
                {
                    if (ProductTo.SelectedItems == null)
                    {
                        MessageFunctions.Error("Error setting active Product: no Product selected.", null);
                    }
                    else if (ProductTo.SelectedItems.Count > 1)
                    {
                        MessageFunctions.InvalidMessage("Cannot set active product. Please ensure only one product is selected.", "Multiple Products selected");
                    }
                    else
                    {
                        Products thisRecord = (Products)ProductTo.SelectedItem;
                        if (thisRecord.Active == false)
                        {
                            int productID = thisRecord.ID;
                            bool success = ClientFunctions.changeActive(productID, selectedClientID);
                            if (success)
                            {
                                refreshProductSummaries(false);
                                CommitButton.IsEnabled = true;
                            }
                        }
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error setting active Product", generalException);
                }
            }
            else
            {
                try
                {
                    if (ClientTo.SelectedItems != null)
                    {
                        List<ClientSummaryRecord> activeList = new List<ClientSummaryRecord>();
                        foreach (var selectedRow in ClientTo.SelectedItems)
                        {
                            activeList.Add((ClientSummaryRecord)selectedRow);
                        }

                        bool success = ClientFunctions.makeActive(activeList, selectedProduct);
                        if (success)
                        {
                            refreshClientSummaries(false);
                            CommitButton.IsEnabled = true;
                        }
                    }
                    else
                    {
                        MessageFunctions.Error("Error setting active Product: no client selected.", null);
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Error setting active Product", generalException);
                }
            }
        }

        private void togglActiveOnly(bool isChecked)
        {
            activeOnly = isChecked;
            refreshClientDataGrid();
            if (ClientTo.Visibility == Visibility.Visible)
            {
                if (ClientFunctions.ignoreAnyChanges())
                {
                    ClientFunctions.clearAnyChanges();
                    refreshClientSummaries(true);
                }
            }
        }
*/

        // ---------------------- //
        // -- Event Management -- //
        //   ---------------------- //

        // Generic (shared) control events //


        // Control-specific events //
        private void ActiveOnly_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
//            togglActiveOnly(true);
        }

        private void ActiveOnly_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
//            togglActiveOnly(false);
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
                    selectedRecord = (ClientGridRecord)ClientDataGrid.SelectedItem;
                    ClientFunctions.GetClientByID(selectedRecord.ID, true);
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
                ClientFunctions.SelectedClient = null; // Avoid accidentally using the previous selection
                clearSelection();
            }
        }

        private void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientFunctions.IgnoreAnyChanges())
            {
                ClientFunctions.ClearAnyChanges();
                Products selectedRecord = (Products) ProductCombo.SelectedItem;
                if (selectedRecord.ProductName == PageFunctions.AllRecords)
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
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientFunctions.IgnoreAnyChanges())
            {
                ClientFunctions.ClearAnyChanges();
                ClientFunctions.ReturnToTilesPage();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientFunctions.IgnoreAnyChanges())
            {
                ClientFunctions.ClearAnyChanges();
                if (backSource == "ClientPage")
                {
                    PageFunctions.ShowClientPage(sourceMode);
                }
                else
                {
                    refreshClientDataGrid();
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
            if (editMode == ByClient) { removeProducts(); }
            else { removeClient(); }
        }

        private void ActiveButton_Click(object sender, RoutedEventArgs e)
        {
//            setActive();
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            bool confirm = MessageFunctions.QuestionYesNo("Are you sure you wish to save your amendments?", "Save changes?");
            if (!confirm) { return; }
            bool success = (editMode == ByClient) ? ClientFunctions.SaveClientProductChanges(ClientFunctions.SelectedClient.ID) : ClientFunctions.SaveProductClientChanges(selectedProductID);
            if (success)
            {
                MessageFunctions.SuccessMessage("Your changes have been saved successfully. You can make further changes, go back to the previous screen, or close this window.", "Changes Saved");
                CommitButton.IsEnabled = false;
            }
        }

        private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientFunctions.IgnoreAnyChanges())
            {
                ClientFunctions.ClearAnyChanges();
                if (ClientCombo.SelectedItem != null)
                {
                    selectedRecord = (ClientGridRecord)ClientCombo.SelectedItem;
                    ClientFunctions.GetClientByID(selectedRecord.ID, true);
                    refreshProductSummaries(true);
                }
            }
        }


    } // class
} // namespace
