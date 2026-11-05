namespace SimHubSF1000UDP
{
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class SimHubSF1000UDPSettings
    {
        public static SimHubSF1000UDPSettings Instance;

        public string ReceiverIP = "192.168.1.2";
        public int ReceiverPort = 20777;
        public bool OnlySendDataIfGameRunning = true;
        public UDPFormats UDPFormat = UDPFormats.F123;
        public byte UDPSendRate = 120;
        public LEDModes LEDMode = LEDModes.Battery;
        public byte LEDBatteryHighPercent = 90;
        public byte LEDBatteryLowPercent = 10;

        public SimHubSF1000UDPSettings()
        {
            Instance = this;
        }
    }

    /// <summary>
    /// What the blue and red side LEDs of the wheel are used for
    /// </summary>
    public enum LEDModes
    {
        /// <summary>Blue = hybrid deploying, red = hybrid harvesting (flags still apply)</summary>
        Battery,
        /// <summary>Red = ABS / wheel lock, blue = TC / wheel spin</summary>
        ABSTC,
        /// <summary>Blue = battery above LEDBatteryHighPercent, red = battery below LEDBatteryLowPercent</summary>
        BatteryLevel,
    }

    public enum UDPFormats
    {
        F12020,
        F123,
    }
}