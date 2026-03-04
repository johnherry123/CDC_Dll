using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models.Database
{
    public class Logs
    {
        [Key, ForeignKey(nameof(Model))]
        public int ModelId { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int trayId {  get; set; }
        public int row {  get; set; }
        public int column {  get; set; }
        public double width {  get; set; }
        public double height { get; set; }  
        public bool status {  get; set; }
        public Model? Model { get; set; }
    }
}
