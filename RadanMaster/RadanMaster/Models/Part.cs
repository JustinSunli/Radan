﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadanMaster.Models
{
    public class Part
    {
        public int ID { get; set; }
        [Required]
        public string FileName { get; set; }
        public string Description { get; set; }
        public string Material { get; set; }
        public double Thickness { get; set; }
    }
}
