using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models.Database
{
    public class Model
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]   
        [Key]
        public int ModelId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "Default";

        public double NominalX { get; set; } = 14.16;
        public double NominalY { get; set; } = 2.50;

        public double TolXPlus { get; set; } = 0.04;
        public double TolXMinus { get; set; } = 0.05;

      
        public VisionParam? VisionParam { get; set; }
        public PointParam? PointParam { get; set; }
        public Logs? Logs { get; set; }
    }
}
