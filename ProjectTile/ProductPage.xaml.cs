using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectTile
{
    /// <summary>
    /// Interaction logic for ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {
        // ---------------------- //
        // -- Global Variables -- //
        // ---------------------- //

        // Global/page parameters //
        string pageMode;
        bool allowAdd = Globals.MyPermissions.Allow("AddProducts");

        // Current variables //
        string descContains = "";
        bool editMode = false;
        bool additionMade = false;

        // Current records //
        Products selectedProduct;
        List<Products> gridList;

        // ---------------------- //
        // -- Page Management --- //
        // ---------------------- //

        // Initialize and Load //
        public ProductPage()
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
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving query details", generalException);
                PageFunctions.ShowTilesPage();
            }            
            
            if (pageMode == PageFunctions.View)
            {
                CommitButton.Visibility = Visibility.Hidden;
                AmendmentsGrid.Visibility = AmendButton.Visibility = AddButton.Visibility = BackButton.Visibility = Visibility.Hidden;
            }
            else if (pageMode == PageFunctions.New)
            {
                additionMode();
                AmendButton.Visibility = AddButton.Visibility = BackButton.Visibility = Visibility.Hidden;
                PageHeader.Content = "Create New Product";
                HeaderImage2.SetResourceReference(Image.SourceProperty, "AddIcon");
            }
            else if (pageMode == PageFunctions.Amend)
            {
                ProductGrid.SelectionMode = DataGridSelectionMode.Single;
                PageHeader.Content = allowAdd? "Amend (or Create) Products" : "Amend Products";
                HeaderImage2.SetResourceReference(Image.SourceProperty, "AmendIcon");
                amendmentSetup();
            }
            
            refreshProductGrid();
        }

        // ---------------------- //
        // -- Data Management --- //
        // ---------------------- //

        // Data updates //
        private void refreshProductGrid()
        {
            gridList = ProductFunctions.ProductsList(descContains);
            ProductGrid.ItemsSource = gridList;
        }

        private void clearProductDetails()
        {
            ProductName.Text = Description.Text = Version.Text = "";
            AmendButton.IsEnabled = false;
        }

        // Data retrieval //

        // Other/shared functions //
        private void additionMode()
        {
            ProductGrid.SelectedItem = null; 
            ProductGrid.IsEnabled = false;
            AmendButton.IsEnabled = false;    
            Instructions.Content = "Enter the details for the new product. Existing products are shown for reference.";
            ProductName.Text = Description.Text = "";
            Version.Text = "1.00";
            editMode = true;
            CommitButton.IsEnabled = true;
            AddButton.Visibility = allowAdd ? Visibility.Visible : Visibility.Hidden;

            if (pageMode == PageFunctions.Amend || additionMade == true)
            {
                ProductName.IsEnabled = Description.IsEnabled = Version.IsEnabled = true;               

                AddButtonText.Text = "Cancel";
                AddImage.Visibility = Visibility.Collapsed;
                ReturnImage2.Visibility = Visibility.Visible;
            }
        }

        private void amendmentSetup()
        {            
            ProductName.IsEnabled = Description.IsEnabled = Version.IsEnabled = false;            
            editMode = false;
            CommitButton.IsEnabled = false;
            gridSelection();

            AmendButtonText.Text = "Amend";
            AmendImage.Visibility = Visibility.Visible;
            ReturnImage.Visibility = Visibility.Collapsed;

            AddButton.IsEnabled = true;
            AddButtonText.Text = additionMade? "Add Another" : "New Product";
            AddImage.Visibility = Visibility.Visible;
            BackButton.Visibility = (Globals.ProductSourcePage != Globals.TilesPageName)? Visibility.Visible : Visibility.Hidden; 
            ReturnImage2.Visibility = Visibility.Collapsed;

            if (pageMode != PageFunctions.New)
            {
                Instructions.Content = "Choose a record from the grid and click 'Amend' to change it, then 'Save' to submit.";
                ProductGrid.IsEnabled = true;
            }
        }

        private void amendMode()
        {
            ProductGrid.IsEnabled = false;
            ProductName.IsEnabled = Description.IsEnabled = Version.IsEnabled = true;
            editMode = true;

            AmendButtonText.Text = "Cancel";
            AmendImage.Visibility = Visibility.Collapsed;
            ReturnImage.Visibility = Visibility.Visible;

            AddButton.IsEnabled = false;
            CommitButton.IsEnabled = true;
            BackButton.Visibility = Visibility.Hidden;
        }

        private void descFilter()
        {
            descContains = SearchText.Text;
            refreshProductGrid();
        }

        private void gridSelection()
        {
            if (pageMode == PageFunctions.Amend && ProductGrid.SelectedItem != null)
            {
                try
                {
                    selectedProduct = (Products)ProductGrid.SelectedItem;
                    ProductName.Text = selectedProduct.ProductName;
                    Description.Text = selectedProduct.ProductDescription;
                    Version.Text = selectedProduct.LatestVersion.ToString();
                    AmendButton.IsEnabled = true;
                }
                catch (Exception generalException) { MessageFunctions.Error("Error processing selection change", generalException); }
            }
            else
            {
                selectedProduct = null;
                clearProductDetails();
            }
        }

        // ---------------------- //
        // -- Event Management -- //
        // ---------------------- //

        // Generic (shared) control events //

        // Control-specific events //

        private void DescContains_LostFocus(object sender, RoutedEventArgs e)
        {
            descFilter();
        }

        private void DescContains_KeyUp(object sender, KeyEventArgs e)
        {
            descFilter();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.ResetProductParameters();
            PageFunctions.ShowTilesPage();
        }

        private void ProductGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gridSelection();
        }

        private void Version_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Globals.LatestVersionFormat.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (editMode) { amendmentSetup(); }
            else { additionMode(); }
        }

        private void AmendButton_Click(object sender, RoutedEventArgs e)
        {
            if (editMode) { amendmentSetup(); }
            else { amendMode(); }
        }

        private void CommitButton_Click(object sender, RoutedEventArgs e)
        {
            if (editMode)
            {
                if (selectedProduct != null)
                {
                    bool success = ProductFunctions.AmendProduct(selectedProduct.ID, ProductName.Text, Description.Text, Version.Text);
                    if (success) 
                    { 
                        MessageFunctions.SuccessAlert("Your changes have been saved successfully.", "Product Amended");
                        refreshProductGrid();
                        amendmentSetup(); 
                    }
                }
                else
                {
                    int newID = ProductFunctions.NewProduct(ProductName.Text, Description.Text, Version.Text);
                    if (newID > 0)
                    {
                        additionMade = true;
                        refreshProductGrid();
                        if (pageMode == PageFunctions.Amend) 
                        { 
                            MessageFunctions.SuccessAlert("New product '" + ProductName.Text + "' saved successfully.", "Product Created");
                            amendmentSetup();
                            ProductGrid.SelectedValue = gridList.First(s => s.ID == newID);
                            ProductGrid.ScrollIntoView(ProductGrid.SelectedItem);
                        }
                        else 
                        {
                            MessageFunctions.SuccessAlert("New product '" + ProductName.Text + "' saved successfully. You can create further products using the 'Add Another' button.", "Product Created");
                            ProductName.IsEnabled = Description.IsEnabled = Version.IsEnabled = false;
                            editMode = false;
                            CommitButton.IsEnabled = false;

                            AddButton.Visibility = Visibility.Visible;
                            AddButton.IsEnabled = true;
                            AddImage.Visibility = Visibility.Visible;
                            ReturnImage2.Visibility = Visibility.Collapsed;
                        }
                        AddButtonText.Text = "Add Another";
                    }
                }
            }
            else { MessageFunctions.Error("Saving should not be possible.", null); }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Globals.ProductSourcePage == "ProjectProductsPage")
            {
                PageFunctions.ShowProjectProductsPage();
            }
            Globals.ResetProductParameters();
        }


    } // class
} // namespace
