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
        public ObservableCollection<TriggerType> triggerTypes { get; set; } = new ObservableCollection<TriggerType>() {
            new TriggerType { moveToEnd=true, name="df_trigger_start" },
            new TriggerType { moveToEnd=true, name="df_trigger_finish" },
            new TriggerType { moveToEnd=true, name="df_trigger_checkpoint" },
            new TriggerType { moveToEnd=true, name="trigger_multiple" },
            new TriggerType { moveToEnd=true, name="trigger_hurt" },
        };
        public ObservableCollection<LightColor> LightColors { get; set; }
        public ObservableCollection<EntityGroup> EntityGroups { get; set; }
        public ExampleData()
        {
            LightColors = new ObservableCollection<LightColor>() { new LightColor() { }, new LightColor() { } };
            EntityGroup groupA = new EntityGroup() { };
            EntityGroup groupB = new EntityGroup() { };
            groupA.props.Add("classname", "trigger_multiple");
            groupA.props.Add("target", "blah");
            groupB.props.Add("classname", "trigger_hurt");
            EntityGroups = new ObservableCollection<EntityGroup>() { groupA , groupB };
        }
    }
}
