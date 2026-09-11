using System;
using System.Collections.Generic;
using ArduinoBluetoothAPI;

namespace PulmoTrainer.Bluetooth
{
    internal static class PulmoBluetoothDeviceSelection
    {
        // Select only an explicitly configured sensor. Never connect to an arbitrary scan result.
        internal static bool TrySelect(IEnumerable<BluetoothDevice> devices, string name, string address,
            out BluetoothDevice selected, out bool ambiguous)
        {
            selected = null;
            ambiguous = false;
            string expectedAddress = (address ?? "").Trim();
            string expectedName = (name ?? "").Trim();
            if (expectedAddress.Length == 0 && expectedName.Length == 0) return false;

            foreach (BluetoothDevice device in devices)
            {
                if (device == null || string.IsNullOrEmpty(device.DeviceAddress)) continue;
                bool matches = expectedAddress.Length > 0
                    ? string.Equals(device.DeviceAddress, expectedAddress, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(device.DeviceName, expectedName, StringComparison.Ordinal);
                if (!matches) continue;
                if (selected != null && !string.Equals(selected.DeviceAddress, device.DeviceAddress,
                    StringComparison.OrdinalIgnoreCase))
                {
                    selected = null;
                    ambiguous = true;
                    return false;
                }
                selected = device;
            }
            return selected != null;
        }
    }
}
