﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraBars.Ribbon;
using System.Data.Entity;
using RadanMaster.Models;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using VaultItemProcessor;

using RadanInterface2;
using RadProject;
using System.IO;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace RadanMaster
{
    
    public partial class Form1 : RibbonForm
    {
        RadanMaster.DAL.RadanMasterContext dbContext = new RadanMaster.DAL.RadanMasterContext();
        string RadanProjectFile { get; set; }
        RadanProject rPrj = new RadanProject();
        string symFolder = @"M:\Radan Sym Files\Vault Sym Files\";

        public Form1()
        {
            InitializeComponent();
            // This line of code is generated by Data Source Configuration Wizard
            // Instantiate a new DBContext
            
            // This line of code is generated by Data Source Configuration Wizard
            dbContext.OrderItems.Load();
            dbContext.Parts.Load();
            dbContext.Orders.Load();

            // This line of code is generated by Data Source Configuration Wizard
            orderItemsBindingSource.DataSource = dbContext.OrderItems.Local.ToBindingList();
        }

        private void importXmlFile (string fileName)
        {
            DailyScheduleAggregate dSchedule = new DailyScheduleAggregate(fileName);
            dSchedule.LoadFromFile();

            DirectoryInfo di = new DirectoryInfo(fileName);
            string batchName = di.Parent.Name;

           

            foreach (AggregateLineItem lineItem in dSchedule.AggregateLineItemList)
            {
                bool isBatch = batchName.ToUpper().Contains("BATCH");

                if (lineItem.MaterialThickness != "")
                {
                    Part newPart = new Part();

                    newPart = dbContext.Parts.Where(p => p.FileName == lineItem.Number).FirstOrDefault();
                    if (newPart == null)    // create a new part if we don't have it in the list
                    {
                        newPart = new Part();
                        newPart.FileName = lineItem.Number;
                        newPart.Description = lineItem.ItemDescription;
                        string modifiedThickness = lineItem.MaterialThickness.Substring(0, lineItem.MaterialThickness.LastIndexOf(" "));
                        newPart.Thickness = double.Parse(modifiedThickness);
                        newPart.Material = lineItem.Material;

                        dbContext.Parts.Add(newPart);
                        dbContext.SaveChanges();
                    }

                    foreach (OrderData oData in lineItem.AssociatedOrders)
                    {
                        Order searchOrder = new Order();
                        if(isBatch)
                            searchOrder = dbContext.Orders.Where(o => o.OrderNumber == batchName).FirstOrDefault();
                        else
                            searchOrder = dbContext.Orders.Where(o => o.OrderNumber == oData.OrderNumber).FirstOrDefault();
                        if (searchOrder == null)    // create new order if it doesn't already exist
                        {
                            searchOrder = new Order();
                            if (isBatch)
                            {
                                searchOrder.OrderNumber = batchName;
                            }
                            else
                            {
                                searchOrder.OrderNumber = oData.OrderNumber;
                            }
                            searchOrder.IsComplete = false;
                            searchOrder.OrderDueDate = DateTime.Now;
                            searchOrder.OrderEntryDate = DateTime.Now;
                            searchOrder.IsBatch = isBatch;
                            dbContext.Orders.Add(searchOrder);
                            dbContext.SaveChanges();
                        }

                        OrderItem searchOrderItem = new OrderItem();
                        if (isBatch)
                            searchOrderItem = dbContext.OrderItems.Where(o => o.Order.OrderNumber == batchName).Where(o => o.Part.FileName == lineItem.Number).FirstOrDefault();
                        else
                            searchOrderItem = dbContext.OrderItems.Where(o => o.Order.OrderNumber == oData.OrderNumber).Where(o => o.Part.FileName == lineItem.Number).FirstOrDefault();
                        if (searchOrderItem == null)   // create a new order item if no match is found with this part number and order number
                        {
                            searchOrderItem = new OrderItem();
                            searchOrderItem.Order = searchOrder;
                            searchOrderItem.Part = newPart;
                            searchOrderItem.QtyRequired = oData.OrderQty*oData.UnitQty;
                            searchOrderItem.QtyNested = 0;
                            searchOrderItem.IsComplete = false;

                            dbContext.OrderItems.Add(searchOrderItem);
                            dbContext.SaveChanges();

                        }
                        else       // adjust existing order item with new quantities if it already exists
                        {
                            searchOrderItem.QtyRequired += oData.OrderQty * oData.UnitQty;
                            dbContext.SaveChanges();
                        }

                        
                        
                    }
                }
            }
            dbContext.SaveChanges();
        }

        private bool masterItemToRadanPart(OrderItem oItem)
        {
            try
            {
                string symName = symFolder + oItem.Part.FileName + ".sym";

                if (rPrj.Parts !=null)
                {
                    RadanPart rPart = new RadanPart();
                    bool matchFound = false;
                    for(int i = 0; i< rPrj.Parts.Count(); i++)
                    {
                        rPart = rPrj.Parts.Part[i];
                        if (rPart.Symbol == symName)
                        {
                            matchFound = true;
                            break;
                        }
                    }

                    if (matchFound)
                    {

                    }
                    else
                    {
                        //create new part in project
                        rPart = new RadanPart();
                        rPart.Symbol = symName;
                        rPart.Number = oItem.QtyRequired;
                        rPart.Made = 0;
                        rPart.ThickUnits = "in";
                        rPart.Thickness = oItem.Part.Thickness;
                        rPart.Material = oItem.Part.Material;
                        

                        rPrj.AddPart(rPart);
                        
                    }

                    
                }

                
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private void btnSendSelectionToProject_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < gridView1.DataRowCount; i++)
            {
                if (gridView1.IsRowSelected(i))
                {
                    
                    string partName = gridView1.GetRowCellValue(i, "Part.FileName").ToString();
                    string orderNumber = gridView1.GetRowCellValue(i, "Order.OrderNumber").ToString();

                    OrderItem orderItem = dbContext.OrderItems.Where(oi => oi.Part.FileName == partName).Where(oi => oi.Order.OrderNumber == orderNumber).FirstOrDefault();
                    if(orderItem!=null)
                        masterItemToRadanPart(orderItem);
                }
            }

            string path = barEditRadanProject.EditValue.ToString();
            rPrj.SaveData(path);
        }

        private void barButtonItemAdd_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string addItemFileName = "";
            // Show the dialog and get result.
            openFileDialogImport.Filter = "sym files (*.sym) | *.sym";
            DialogResult result = openFileDialogAddItem.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                AddItemDialog.AddItem addItemDialog = new AddItemDialog.AddItem();
                DialogResult addItemResult = addItemDialog.ShowDialog();


                if (addItemResult == DialogResult.OK)
                {
                    addItemFileName = (openFileDialogAddItem.FileName);
                    RadanInterface radanInterface = new RadanInterface();
                    //radanInterface.Initialize();

                    string name = System.IO.Path.GetFileNameWithoutExtension(addItemFileName);
                    string description = radanInterface.GetDescriptionFromSym(addItemFileName);
                    string thicknessStr = radanInterface.GetThicknessFromSym(addItemFileName);
                    double thickness = double.Parse(thicknessStr);
                    string material = radanInterface.GetMaterialTypeFromSym(addItemFileName);


                    Part newPart = dbContext.Parts.Where(p => p.FileName == name).FirstOrDefault();
                    if (newPart == null)
                    {
                        newPart = new Part();
                        newPart.FileName = name;
                        newPart.Description = description;
                        newPart.Thickness = thickness;
                        newPart.Material = material;

                        dbContext.Parts.Add(newPart);
                        dbContext.SaveChanges();


                    }

                    Order newOrder = dbContext.Orders.Where(o => o.OrderNumber == AddItemDialog.AddItem.lastOrderNumber).FirstOrDefault();
                    if (newOrder == null)
                    {
                        newOrder = new Order();
                        newOrder.IsBatch = false;
                        newOrder.IsComplete = false;
                        newOrder.OrderDueDate = DateTime.Now;
                        newOrder.OrderEntryDate = DateTime.Now;
                        newOrder.OrderItems = new List<OrderItem>();
                        newOrder.OrderNumber = AddItemDialog.AddItem.lastOrderNumber;
                        newOrder.IsBatch = AddItemDialog.AddItem.isBatch;

                    }

                    OrderItem newItem = dbContext.OrderItems.Where(oitem => oitem.Order.OrderNumber == AddItemDialog.AddItem.lastOrderNumber).Where(oitem => oitem.Part.FileName == name).FirstOrDefault();
                    if (newItem == null)
                    {
                        newItem = new OrderItem();
                        newItem.IsComplete = false;
                        newItem.Order = newOrder;
                        newItem.Part = newPart;
                        newItem.QtyRequired = int.Parse(AddItemDialog.AddItem.qty);
                        newItem.QtyNested = 0;

                        dbContext.OrderItems.Add(newItem);
                        dbContext.SaveChanges();
                    }
                    else
                    {
                        MessageBox.Show("This Order Item has already been entered. It will not be entered again");
                    }
                }
            }
        }

        private void barButtonItemImport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string importFileName = "";
            // Show the dialog and get result.
            openFileDialogImport.Filter = "xml files (*.xml) | *.xml";
            DialogResult result = openFileDialogImport.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                importFileName = (openFileDialogImport.FileName);
                importXmlFile(importFileName);
            }
        }

        private void barButtonBrowseRadanProject_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            openFileDialogProject.Filter = "rpd files (*.rpd) | *.rpd";
            DialogResult result = openFileDialogProject.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                string path = openFileDialogProject.FileName;

                RadanProjectFile = openFileDialogProject.FileName;
                barEditRadanProjectBrowse.EditValue = RadanProjectFile;

                rPrj = rPrj.LoadData(path);
            }
        }

        private void barButtonUpdateFromRadan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            List<RadanNest> nestList = rPrj.Nests.ToList();
            int nestsImported = 0;
            int nestsAlreadyInProject = 0;

            foreach(RadanNest nst in rPrj.Nests.ToList())
            {
                Nest searchNest = dbContext.Nests.Where(n => n.nestName == nst.FileName).FirstOrDefault();
                if (searchNest == null)
                {
                    Nest newNest = new Nest();
                    newNest.nestName = nst.FileName;
                    newNest.nestPath = System.IO.Path.GetDirectoryName(barEditRadanProject.EditValue.ToString());
                    dbContext.Nests.Add(newNest);
                    nestsImported++;
                }
                else
                {
                    nestsAlreadyInProject++;
                }
            }

            RadanPart rPart = new RadanPart();

            for (int i = 0; i < rPrj.Parts.Count(); i++)
            {
                rPart = rPrj.Parts.Part[i];
                RadanPartToMasterItem(rPart);
            }
        }

        private bool RadanPartToMasterItem(RadanPart radPart)
        {
            // not at all tested yet
            try
            {
                
                List<OrderItem> masterItemList = dbContext.OrderItems.Where(i => i.Part.FileName == radPart.Symbol).ToList();

                if (masterItemList != null)
                {
                    if (radPart.Made > 0)
                    {
                        masterItemList[0].QtyRequired -= radPart.Made;
                        masterItemList[0].QtyNested += radPart.Made;
                        radPart.Number -= radPart.Made;
                        radPart.Made = 0;
                    }
                    masterItemList[0].QtyRequired = radPart.Number;
                    masterItemList[0].QtyNested = radPart.Made;
                }


                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
