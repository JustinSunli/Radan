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

namespace RadanMaster
{
    public partial class Form1 : RibbonForm
    {
        RadanMaster.DAL.RadanMasterContext dbContext = new RadanMaster.DAL.RadanMasterContext();
        string RadanProjectFile { get; set; }
        RadanProject rPrj = new RadanProject();



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

        private void btnImport_Click(object sender, EventArgs e)
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

        private void importXmlFile (string fileName)
        {
            DailyScheduleAggregate dSchedule = new DailyScheduleAggregate(fileName, "M:\\PDF Files");
            dSchedule.LoadFromFile();
            
            foreach(AggregateLineItem lineItem in dSchedule.AggregateLineItemList)
            {
                if (lineItem.MaterialThickness != "")
                {
                    Part newPart = new Part();
                    newPart.FileName = lineItem.Number;
                    newPart.Description = lineItem.ItemDescription;
                    string modifiedThickness = lineItem.MaterialThickness.Substring(0, lineItem.MaterialThickness.LastIndexOf(" "));
                    newPart.Thickness = double.Parse(modifiedThickness);
                    newPart.Material = lineItem.Material;

                    dbContext.Parts.Add(newPart);
                    dbContext.SaveChanges();

                    foreach (OrderData oData in lineItem.AssociatedOrders)
                    {
                        Order o = new Order();
                        o.OrderNumber = oData.OrderNumber;
                        o.IsComplete = false;
                        o.OrderDueDate = DateTime.Now;
                        o.OrderEntryDate = DateTime.Now;
                        dbContext.Orders.Add(o);
                        dbContext.SaveChanges();

                        OrderItem oItem = new OrderItem();
                        oItem.Order = o;
                        oItem.Part = newPart;
                        oItem.QtyRequired = oData.OrderQty;
                        oItem.QtyNested = 0;
                        oItem.IsComplete = false;
                        dbContext.OrderItems.Add(oItem);
                        dbContext.SaveChanges();
                    }
                }
            }
        }

        private void btnAddItem_Click(object sender, EventArgs e)
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
                        newPart.FileName = System.IO.Path.GetFileNameWithoutExtension(addItemFileName);
                        newPart.Description = description;
                        newPart.Thickness = thickness;
                        newPart.Material = material;

                        dbContext.Parts.Add(newPart);
                        dbContext.SaveChanges();

                        
                    }

                    Order newOrder = dbContext.Orders.Where(o => o.OrderNumber == AddItemDialog.AddItem.lastOrderNumber).FirstOrDefault();
                    if(newOrder == null)
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

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            openFileDialogProject.Filter = "rpd files (*.rpd) | *.rpd";
            DialogResult result = openFileDialogProject.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                string path = openFileDialogProject.FileName;

                System.IO.Stream stream = System.IO.File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                stream.Flush();

                RadanProjectFile = openFileDialogProject.FileName;
                txtBoxRadanProject.Text = RadanProjectFile;

                using (StreamReader reader = new StreamReader(stream))
                {
                    XmlSerializer prjSerializer = new XmlSerializer(typeof(RadanProject));
                    rPrj = (RadanProject)prjSerializer.Deserialize(reader);
                }
            }

            btnSendSelectionToProject.Enabled = true;
            btnSyncProject.Enabled = true;
        }

        private void btnSyncProject_Click(object sender, EventArgs e)
        {

        }
    }
}
