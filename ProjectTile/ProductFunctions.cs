using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ProjectTile
{
    class ProductFunctions
    {
        // Data retrieval
        
        public static List<Products> ProductsList(string search)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    List<Products> productGridList = new List<Products>();
                    productGridList = (from p in existingPtDb.Products
                                       where search == "" || p.ProductName.Contains(search) || p.ProductDescription.Contains(search)
                                       orderby p.ProductName
                                       select p).ToList();
                    return productGridList;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error retrieving products list", generalException);
                return null;
            }
        }
        
        public static Products GetProductByID (int productID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    return existingPtDb.Products.Find(productID);
                }
           }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error finding product with ID " + productID.ToString() + "", generalException);
                return null;
            }
        }

        // Changes

        public static bool ValidateProduct(ref Products thisProduct, int existingID)
        {
            try
            {
                ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                using (existingPtDb)
                {
                    string productName = thisProduct.ProductName;
                    if (!PageFunctions.SqlInputOK(productName, true, "Product name")) { return false; }
                    Products checkNewName = existingPtDb.Products.FirstOrDefault(p => p.ID != existingID && p.ProductName == productName);
                    if (checkNewName == null) { thisProduct.ProductName = productName; }
                    else
                    {
                        string errorText = (existingID > 0) ? 
                            "Could not amend Product. Another Product with name '" + productName + "' already exists." :
                            "Could not create new Product. A Product with name '" + productName + "' already exists." ;

                        MessageFunctions.InvalidMessage(errorText, "Duplicate Name");
                        return false;
                    }

                    string productDescription = thisProduct.ProductDescription;
                    if (!PageFunctions.SqlInputOK(productDescription, true, "Product description")) { return false; }
                    Products checkNewDescription = existingPtDb.Products.FirstOrDefault(p => p.ID != existingID && p.ProductDescription == productDescription);
                    if (checkNewDescription == null) { thisProduct.ProductDescription = productDescription; }
                    else
                    {
                        string errorText = (existingID > 0) ?
                            "Could not amend Product. Another Product with description '" + productDescription + "' already exists." :
                            "Could not create new Product. A Product with description '" + productDescription + "' already exists.";

                        MessageFunctions.InvalidMessage(errorText, "Duplicate Description");
                        return false;
                    }

                    return true;
                }
            }
            catch (SqlException sqlException)
            {
                MessageFunctions.Error("SQL error saving changes to the database", sqlException);
                return false;
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error saving changes to the database", generalException);
                return false;
            }

        }

        public static int NewProduct(string displayName, string displayDescription, string version)
        {
            try
            {
                decimal versionNumber;
                
                if (!Decimal.TryParse(version, out versionNumber))
                {
                    MessageFunctions.InvalidMessage("Cannot create new product: version number is not a decimal.", "Invalid Version");
                    return 0;
                }

                Products newProduct = new Products() { ProductName = displayName, ProductDescription = displayDescription, LatestVersion = versionNumber };
                if (ValidateProduct(ref newProduct, 0))
                {
                    try
                    {
                        ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                        using (existingPtDb)
                        {                                          
                            existingPtDb.Products.Add(newProduct);
                            existingPtDb.SaveChanges();
                            return newProduct.ID;
                        }
                    }                          
                    catch (Exception generalException)
                    {
                        MessageFunctions.Error("Problem saving new product", generalException);
                        return 0;
                    }
                }
                else { return 0; }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error creating new product", generalException);
                return 0;
            }
        }

        public static bool AmendProduct(int productID, string displayName, string displayDescription, string version)
        {
            try
            {
                decimal versionNumber;

                if (!Decimal.TryParse(version, out versionNumber))
                {
                    MessageFunctions.InvalidMessage("Cannot amend product '" + displayName + "': new version number is not a decimal.", "Invalid Version");
                    return false;
                }

                try
                {
                    ProjectTileSqlDatabase existingPtDb = SqlServerConnection.ExistingPtDbConnection();
                    using (existingPtDb)
                    {
                        Products thisProduct = existingPtDb.Products.Find(productID);
                        if (thisProduct.LatestVersion > versionNumber)
                        {
                            MessageFunctions.Error("Error amending product '" + displayName + "': new version number is lower than the existing one.", null);
                            return false;
                        }
                        thisProduct.ProductName = displayName;
                        thisProduct.ProductDescription = displayDescription;
                        thisProduct.LatestVersion = versionNumber;

                        if (ValidateProduct(ref thisProduct, productID))
                        {
                            existingPtDb.SaveChanges();
                            return true;
                        }
                        else { return false; }
                    }
                }
                catch (Exception generalException)
                {
                    MessageFunctions.Error("Problem saving changes to product '" + displayName + "'", generalException);
                    return false;
                }
            }
            catch (Exception generalException)
            {
                MessageFunctions.Error("Error amending product '" + displayName + "'", generalException);
                return false;
            }
        }

    } // class
} // namespace
