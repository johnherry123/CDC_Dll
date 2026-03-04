using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public static class McuState
    {
        private static readonly object _gate = new();
        private static McuStatusSnapshot _current = new();

        public static McuStatusSnapshot Current
        {
            get { lock (_gate) return _current; }
        }

        public static event Action<McuStatusSnapshot>? Changed;


        public static void UpdateFrom(McuStatus st)
        {
            var snap = McuStatusSnapshot.From(st);
            lock (_gate) _current = snap;
            Changed?.Invoke(snap);
        }
    }
}
