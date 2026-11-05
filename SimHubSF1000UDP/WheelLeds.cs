using GameReaderCommon;
using SimHub.Plugins;

namespace SimHubSF1000UDP
{
    /// <summary>
    /// Values of m_vehicleFiaFlags the wheel maps to its side LEDs. The wheel only has blue, orange and red LEDs
    /// </summary>
    internal enum WheelLedFlag : sbyte
    {
        None = 0,
        Green = 1,
        Blue = 2,
        Orange = 3,
        Red = 4,
    }

    /// <summary>
    /// Decides what the side LEDs of the wheel show, depending on the selected <see cref="LEDModes"/>
    /// </summary>
    internal static class WheelLeds
    {
        /// <summary>
        /// Value to send as m_vehicleFiaFlags. A yellow flag (orange LED) always takes priority and a green flag only shows when the mode has nothing to show.
        /// F1 2020 also turns a black flag red, as SimHub has no red flag
        /// </summary>
        public static sbyte GetFlag(PluginManager pluginManager, StatusDataBase data, bool blackFlagIsRed)
        {
            if (IsActive(data?.Flag_Yellow))
            {
                return (sbyte)WheelLedFlag.Orange;
            }

            var settings = SimHubSF1000UDPSettings.Instance;
            var flag = settings.LEDMode switch
            {
                LEDModes.ABSTC => GetABSTCFlag(data),
                LEDModes.BatteryLevel => GetBatteryLevelFlag(pluginManager, data, settings.LEDBatteryHighPercent, settings.LEDBatteryLowPercent),
                _ => GetHybridFlag(pluginManager, data, blackFlagIsRed),
            };

            if (flag == WheelLedFlag.None && IsActive(data?.Flag_Green))
            {
                flag = WheelLedFlag.Green;
            }

            return (sbyte)flag;
        }

        /// <summary>
        /// Blue = hybrid deploying (LMU) or blue flag, red = hybrid harvesting (LMU)
        /// </summary>
        private static WheelLedFlag GetHybridFlag(PluginManager pluginManager, StatusDataBase data, bool blackFlagIsRed)
        {
            if (Utils.IsLMUDeployingERS(pluginManager) || IsActive(data?.Flag_Blue))
                return WheelLedFlag.Blue;
            if (Utils.IsLMUHarvestingERS(pluginManager) || (blackFlagIsRed && IsActive(data?.Flag_Black)))
                return WheelLedFlag.Red;
            return WheelLedFlag.None;
        }

        /// <summary>
        /// Red = ABS intervention or wheel lock, blue = TC intervention or wheel spin. ABS takes priority
        /// </summary>
        private static WheelLedFlag GetABSTCFlag(StatusDataBase data)
        {
            if (IsActive(data?.ABSActive))
                return WheelLedFlag.Red;
            if (IsActive(data?.TCActive))
                return WheelLedFlag.Blue;
            return WheelLedFlag.None;
        }

        /// <summary>
        /// Blue = battery above highPercent, red = battery below lowPercent
        /// </summary>
        private static WheelLedFlag GetBatteryLevelFlag(PluginManager pluginManager, StatusDataBase data, int highPercent, int lowPercent)
        {
            var battery = Utils.GetBatteryFraction(pluginManager, data);
            if (!battery.HasValue)
                return WheelLedFlag.None;

            var percent = battery.Value * 100;
            if (percent > highPercent)
                return WheelLedFlag.Blue;
            if (percent < lowPercent)
                return WheelLedFlag.Red;
            return WheelLedFlag.None;
        }

        private static bool IsActive(object value)
        {
            return Utils.ClampIntegerValue<byte>(value) > 0;
        }
    }
}
