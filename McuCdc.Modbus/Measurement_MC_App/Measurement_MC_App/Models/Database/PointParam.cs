using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models.Database
{
    public class PointParam
    {
        [Key, ForeignKey(nameof(Model))]
        public int ModelId { get; set; }
        public int point1X {  get; set; }
        public int point1Y { get; set; }
        public int point1Z {  get; set; }   
        public int point2X { get; set; }
        public int point2Y { get; set; }
        public int point2Z { get; set; }
        public int point3X { get; set; }
        public int point3Y { get; set; }
        public int point3Z {  get; set; }
        public int point4X { get; set; }
        public int point4Y { get; set; }
        public int point4Z { get; set; }
        public int point5X { get; set; }
        public int point5Y { get; set; }
        public int point5Z { get; set; }
        public int point6X { get; set; }
        public int point6Y { get; set; }
        public int point6Z { get; set; }
        public Model? Model { get; set; }
    }
}
