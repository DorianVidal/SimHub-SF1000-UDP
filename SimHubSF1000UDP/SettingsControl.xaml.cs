using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimHubSF1000UDP
{
    public partial class SettingsControl : UserControl
    {
        private const int MaxPercent = 100;

        // Each dropdown item and the setting value it stands for
        private readonly Dictionary<ComboBoxItem, UDPFormats> _udpFormats;
        private readonly Dictionary<ComboBoxItem, LEDModes> _ledModes;
        private readonly Dictionary<ComboBoxItem, byte> _sendRates;

        public SimHubSF1000UDP Plugin { get; }

        private SimHubSF1000UDPSettings Settings => Plugin.Settings;

        public SettingsControl()
        {
            InitializeComponent();
            HeaderSection.Title = "SimHub SF1000 UDP v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

            _udpFormats = new()
            {
                [UDPFormat_F123] = UDPFormats.F123,
                [UDPFormat_F12020] = UDPFormats.F12020,
            };

            _ledModes = new()
            {
                [LEDMode_Battery] = LEDModes.Battery,
                [LEDMode_ABSTC] = LEDModes.ABSTC,
                [LEDMode_BatteryLevel] = LEDModes.BatteryLevel,
            };

            _sendRates = new()
            {
                [SendRate_20] = 20,
                [SendRate_30] = 30,
                [SendRate_60] = 60,
                [SendRate_120] = 120,
            };
        }

        public SettingsControl(SimHubSF1000UDP plugin) : this()
        {
            Plugin = plugin;

            LoadReceiver();
            OnlySendDataIfGameRunning.IsChecked = Settings.OnlySendDataIfGameRunning;
            Select(UDPFormat_Select, _udpFormats, Settings.UDPFormat, UDPFormat_F123);
            Select(LEDMode_Select, _ledModes, Settings.LEDMode, LEDMode_Battery);
            ShowBatteryThresholds();
            Select(SendRate, _sendRates, Settings.UDPSendRate, SendRate_60);
        }

        private void SaveButtonPrimary_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveReceiver();
            Settings.OnlySendDataIfGameRunning = OnlySendDataIfGameRunning.IsChecked ?? true;

            if (TryGetSelected(UDPFormat_Select, _udpFormats, out var udpFormat))
            {
                Settings.UDPFormat = udpFormat;
            }

            if (TryGetSelected(LEDMode_Select, _ledModes, out var ledMode))
            {
                Settings.LEDMode = ledMode;
            }
            SaveBatteryThresholds();

            if (TryGetSelected(SendRate, _sendRates, out var sendRate))
            {
                Settings.UDPSendRate = sendRate;
            }

            Plugin.SaveSettings();

            UDPServer.Instance.Update();
        }

        private void LoadReceiver()
        {
            if (System.Net.IPAddress.TryParse(Settings.ReceiverIP, out var ipAddress))
            {
                NewReceiverIP.IPAddress = ipAddress;
                CurrentReceiverIP.Text = ipAddress.ToString();
            }

            NewReceiverPort.Text = Settings.ReceiverPort.ToString();
            CurrentReceiverPort.Text = Settings.ReceiverPort.ToString();
        }

        // Keep the previous IP/port when the new value is empty or incomplete
        private void SaveReceiver()
        {
            if (NewReceiverIP.IPAddress != null)
            {
                Settings.ReceiverIP = NewReceiverIP.IPAddress.ToString();
            }
            CurrentReceiverIP.Text = Settings.ReceiverIP;

            var newPort = Utils.ClampIntegerValue<ushort>(NewReceiverPort.Text);
            if (newPort > 0)
            {
                Settings.ReceiverPort = newPort;
            }
            CurrentReceiverPort.Text = Settings.ReceiverPort.ToString();
        }

        // Keep the previous thresholds when a new value is empty or the red one is not below the blue one
        private void SaveBatteryThresholds()
        {
            var high = ParsePercent(LEDBatteryHigh.Text);
            var low = ParsePercent(LEDBatteryLow.Text);

            if (high.HasValue && low.HasValue && low.Value < high.Value)
            {
                Settings.LEDBatteryHighPercent = high.Value;
                Settings.LEDBatteryLowPercent = low.Value;
            }

            ShowBatteryThresholds();
        }

        private void ShowBatteryThresholds()
        {
            LEDBatteryHigh.Text = Settings.LEDBatteryHighPercent.ToString();
            LEDBatteryLow.Text = Settings.LEDBatteryLowPercent.ToString();
        }

        private static byte? ParsePercent(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : Math.Min(Utils.ClampIntegerValue<byte>(text), (byte)MaxPercent);
        }

        private static void Select<T>(ComboBox comboBox, Dictionary<ComboBoxItem, T> items, T value, ComboBoxItem fallback)
        {
            var match = items.FirstOrDefault(item => EqualityComparer<T>.Default.Equals(item.Value, value));
            comboBox.SelectedItem = match.Key ?? fallback;
        }

        private static bool TryGetSelected<T>(ComboBox comboBox, Dictionary<ComboBoxItem, T> items, out T value)
        {
            value = default;
            return comboBox.SelectedItem is ComboBoxItem item && items.TryGetValue(item, out value);
        }

        private void ReceiverPort_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key < Key.D0 || e.Key > Key.D9) && (e.Key < Key.NumPad0 || e.Key > Key.NumPad9))
            {
                e.Handled = true;
            }
        }
    }
}
