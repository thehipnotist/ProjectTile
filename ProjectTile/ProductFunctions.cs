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
                    search = PageFunctions.FormatSqlInput(search);
                    
                    List<Products> productGridList = new List<Products>();
                    productGridList = (from p in existingPtDb.Products
                                       where search == "" || p.ProductName.Contains(search) || p.ProductDescription.Contains(search)
                                       orderby p.ProductName
                                       select p).ToList();

                    foreach (Products thisProduct in productGridList)
                    {
                        thisProduct.ProductName = PageFunctions.FormatSqlOutput(thisProduct.ProductName);
                        thisProduct.ProductDescription = PageFunctions.FormatSqlOutput(thisProduct.ProductDescription);
                    }
                    
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
                    string displayName = thisProduct.ProductName;
                    string sqlName = (PageFunctions.SqlInput(displayName, true, "Product name"));
                    if (sqlName == PageFunctions.InvalidString) { return false; }
                    Products checkNewName = existingPtDb.Products.FirstOrDefault(p => p.ID != existingID && p.ProductName == sqlName);
                    if (checkNewName == null) { thisProduct.ProductName = sqlName; }
                    else
                    {
                        string errorText = (existingID > 0) ? 
                            "Could not amend Product. Another Product with name '" + displayName + "' already exists." :
                            "Could not create new Product. A Product with name '" + displayName + "' already exists." ;

                        MessageFunctions.InvalidMessage(errorText, "Duplicate Name");
                        return false;
                    }

                    string displayDescription = thisProduct.ProductDescription;
                    string sqlDescription = (PageFunctions.SqlInput(displayDescription, true, "Product description"));
                    if (sqlDescription == PageFunctions.InvalidString) { return false; }
                    Products checkNewDescription = existingPtDb.Products.FirstOrDefault(p => p.ID != existingID && p.ProductDescription == sqlDescription);
                    if (checkNewDescription == null) { thisProduct.ProductDescription = sqlDescription; }
                    else
                    {
                        string errorText = (existingID > 0) ?
                            "Could not amend Product. Another Product with description '" + displayName + "' already exists." :
                            "Could not create new Product. A Product with description '" + displayName + "' already exists.";
                        
                        MessageFunctions.InvalidMessage("Could not create new Product. A Product with description '" + displayDescription + "' already exists.", "Duplicate Description");
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
