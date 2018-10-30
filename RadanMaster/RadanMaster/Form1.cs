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

namespace RadanMaster
{
    public partial class Form1 : RibbonForm
    {
        public Form1()
        {
            InitializeComponent();
            // This line of code is generated by Data Source Configuration Wizard
            // Instantiate a new DBContext
            RadanMaster.DAL.RadanMasterContext dbContext = new RadanMaster.DAL.RadanMasterContext();
            // Call the Load method to get the data for the given DbSet from the database.
            dbContext.Parts.Load();
            // This line of code is generated by Data Source Configuration Wizard
            partsBindingSource.DataSource = dbContext.Parts.Local.ToBindingList();
        }
    }
}
