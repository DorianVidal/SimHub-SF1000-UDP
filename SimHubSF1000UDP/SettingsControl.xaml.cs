using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimHubSF1000UDP
{
    public partial class SettingsControl : UserControl
    {
        public SimHubSF1000UDP Plugin { get; }

        public SettingsControl()
        {
            InitializeComponent();
            HeaderSection.Title = "SimHub SF1000 UDP v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        }

        public SettingsControl(SimHubSF1000UDP plugin) : this()
        {
            this.Plugin = plugin;

            // First load from plugin settings
            if (System.Net.IPAddress.TryParse(Plugin.Settings.ReceiverIP, out var ipAddress))
            {
                NewReceiverIP.IPAddress = ipAddress;
                CurrentReceiverIP.Text = ipAddress.ToString();
            }

            NewReceiverPort.Text = Plugin.Settings.ReceiverPort.ToString();
            CurrentReceiverPort.Text = Plugin.Settings.ReceiverPort.ToString();
            OnlySendDataIfGameRunning.IsChecked = Plugin.Settings.OnlySendDataIfGameRunning;

            UDPFormat_Select.SelectedItem = Plugin.Settings.UDPFormat switch
            {
                UDPFormats.F12020 => UDPFormat_F12020,
                UDPFormats.F123 => UDPFormat_F123,
                _ => UDPFormat_F123,
            };

            LEDMode_Select.SelectedItem = Plugin.Settings.LEDMode switch
            {
                LEDModes.ABSTC => LEDMode_ABSTC,
                _ => LEDMode_Battery,
            };

            SendRate.SelectedItem = Plugin.Settings.UDPSendRate switch
            {
                20 => SendRate_20,
                30 => SendRate_30,
                60 => SendRate_60,
                120 => SendRate_120,
                _ => SendRate_60,
            };
        }

        private void SaveButtonPrimary_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Save to plugin settings
            // Keep the previous IP/port when the new value is empty or incomplete
            if (NewReceiverIP.IPAddress != null)
            {
                Plugin.Settings.ReceiverIP = NewReceiverIP.IPAddress.ToString();
            }
            CurrentReceiverIP.Text = Plugin.Settings.ReceiverIP;

            var newPort = Utils.ClampIntegerValue<ushort>(NewReceiverPort.Text);
            if (newPort > 0)
            {
                Plugin.Settings.ReceiverPort = newPort;
            }
            CurrentReceiverPort.Text = Plugin.Settings.ReceiverPort.ToString();

            Plugin.Settings.OnlySendDataIfGameRunning = OnlySendDataIfGameRunning.IsChecked ?? true;

            if (UDPFormat_Select.SelectedItem == UDPFormat_F12020)
            {
                Plugin.Settings.UDPFormat = UDPFormats.F12020;
            }
            else if (UDPFormat_Select.SelectedItem == UDPFormat_F123)
            {
                Plugin.Settings.UDPFormat = UDPFormats.F123;
            }

            if (LEDMode_Select.SelectedItem == LEDMode_ABSTC)
            {
                Plugin.Settings.LEDMode = LEDModes.ABSTC;
            }
            else if (LEDMode_Select.SelectedItem == LEDMode_Battery)
            {
                Plugin.Settings.LEDMode = LEDModes.Battery;
            }

            if (SendRate.SelectedItem == SendRate_20)
            {
                Plugin.Settings.UDPSendRate = 20;
            }
            else if (SendRate.SelectedItem == SendRate_30)
            {
                Plugin.Settings.UDPSendRate = 30;
            }
            else if (SendRate.SelectedItem == SendRate_60)
            {
                Plugin.Settings.UDPSendRate = 60;
            }
            else if (SendRate.SelectedItem == SendRate_120)
            {
                Plugin.Settings.UDPSendRate = 120;
            }

            Plugin.SaveSettings();

            UDPServer.Instance.Update();
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
