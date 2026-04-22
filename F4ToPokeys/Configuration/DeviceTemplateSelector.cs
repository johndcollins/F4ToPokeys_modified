using System.Windows;
using System.Windows.Controls;

namespace F4ToPokeys
{
    // Picks the group-summary DataTemplate when the content is a DeviceGroupViewModel;
    // returns null otherwise so WPF falls back to implicit DataType dispatch for device
    // models (PoKeys / PololuMaestro / DEDuino) via the templates merged from Views/*.xaml.
    //
    // Using a selector rather than a DataTrigger on Style.ContentTemplate avoids a race
    // where, during a group-to-device selection change, WPF updates Content before the
    // trigger's bound IsGroupSelected change propagates, causing the stale group template
    // to briefly bind its GroupName / AddDeviceCommand / Kind / Devices paths against the
    // new PoKeys content and emit a burst of System.Windows.Data binding warnings.
    public class DeviceTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GroupTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is DeviceGroupViewModel)
                return GroupTemplate;
            return null;
        }
    }
}
