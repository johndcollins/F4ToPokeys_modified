using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace F4ToPokeys
{
    public enum DeviceGroupKind
    {
        PoKeys,
        PololuMaestro,
        DEDuino
    }

    public class DeviceGroupViewModel : BindableObject
    {
        public DeviceGroupViewModel(
            string groupName,
            DeviceGroupKind kind,
            IEnumerable sourceCollection,
            ICommand addDeviceCommand,
            Action<IList> onDevicesRemoved)
        {
            GroupName = groupName;
            Kind = kind;
            AddDeviceCommand = addDeviceCommand;
            this.source = sourceCollection;
            this.onDevicesRemoved = onDevicesRemoved;
            Devices = new ObservableCollection<IDevice>();

            Rebuild();

            INotifyCollectionChanged notifying = sourceCollection as INotifyCollectionChanged;
            if (notifying != null)
                notifying.CollectionChanged += OnSourceCollectionChanged;
        }

        public string GroupName { get; }

        public DeviceGroupKind Kind { get; }

        public ICommand AddDeviceCommand { get; }

        public ObservableCollection<IDevice> Devices { get; }

        private readonly IEnumerable source;
        private readonly Action<IList> onDevicesRemoved;

        private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (object item in e.NewItems)
                    {
                        IDevice device = item as IDevice;
                        if (device != null)
                            Devices.Add(device);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (object item in e.OldItems)
                    {
                        IDevice device = item as IDevice;
                        if (device != null)
                            Devices.Remove(device);
                    }
                    if (onDevicesRemoved != null)
                        onDevicesRemoved(e.OldItems);
                    break;

                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Move:
                    Rebuild();
                    if (e.OldItems != null && onDevicesRemoved != null)
                        onDevicesRemoved(e.OldItems);
                    break;
            }
        }

        private void Rebuild()
        {
            Devices.Clear();
            foreach (object item in source)
            {
                IDevice device = item as IDevice;
                if (device != null)
                    Devices.Add(device);
            }
        }
    }
}
