using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace Measurement_MC_App.Views.Theme
{
    public sealed class NavItem
    {
        public string Title { get; }
        public Geometry Icon { get; }
        public UserControl View { get; }

        public NavItem(string title, Geometry icon, UserControl view)
        {
            Title = title;
            Icon = icon;
            View = view;
        }
    }
}
