using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniRadiant
{
    public class ExampleData
    {
        public ObservableCollection<LightColor> LightColors { get; set; }
        public ExampleData()
        {
            LightColors = new ObservableCollection<LightColor>() { new LightColor() { }, new LightColor() { } };
        }
    }
}
