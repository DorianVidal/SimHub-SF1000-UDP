using System;
using System.Runtime.InteropServices;

namespace SimHubSF1000UDP
{
    internal static class Utils
    {
        /**
         * <summary>Serialize a marshalled struct to its raw bytes. Memory is zeroed before use and always released</summary>
         */
        public static byte[] StructToBytes<T>(T value) where T : struct
        {
            int size = Marshal.SizeOf(value);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                for (int i = 0; i < size; i++)
                {
                    Marshal.WriteByte(ptr, i, 0);
                }

                Marshal.StructureToPtr(value, ptr, false);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return arr;
        }

        /**
         * <summary>Hybrid battery charge of the player's car in Le Mans Ultimate, from 0 to 1. Null if the car has no battery data</summary>
         */
        public static double? GetLMUBatteryFraction(SimHub.Plugins.PluginManager pluginManager)
        {
            var value = pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayerTelemetry.mBatteryChargeFraction");
            if (value == null)
            {
                return null;
            }

            try
            {
                var fraction = Convert.ToDouble(value);
                return double.IsNaN(fraction) ? null : Math.Max(0, Math.Min(1, fraction));
            }
            catch
            {
                return null;
            }
        }

        /**
         * <summary>True when the hybrid system of the player's car in Le Mans Ultimate is currently deploying energy (rF2 electric boost motor state 2 = propulsion)</summary>
         */
        public static bool IsLMUDeployingERS(SimHub.Plugins.PluginManager pluginManager)
        {
            return pluginManager.GameName == "LMU"
                && ClampIntegerValue<int>(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayerTelemetry.mElectricBoostMotorState")) == 2;
        }

        /**
         * <summary>True when the hybrid system of the player's car in Le Mans Ultimate is currently harvesting energy (rF2 electric boost motor state 3 = regeneration)</summary>
         */
        public static bool IsLMUHarvestingERS(SimHub.Plugins.PluginManager pluginManager)
        {
            return pluginManager.GameName == "LMU"
                && ClampIntegerValue<int>(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.CurrentPlayerTelemetry.mElectricBoostMotorState")) == 3;
        }

        /**
         * <summary>Side LEDs flag for the ABS/TC LED mode. Yellow flag (orange LED) keeps priority, then red = ABS / wheel lock, blue = TC / wheel spin, then green flag</summary>
         */
        public static sbyte GetABSTCFlag(GameReaderCommon.StatusDataBase data)
        {
            if (ClampIntegerValue<byte>(data?.Flag_Yellow) > 0)
                return 3;
            if (ClampIntegerValue<byte>(data?.ABSActive) > 0)
                return 4;
            if (ClampIntegerValue<byte>(data?.TCActive) > 0)
                return 2;
            if (ClampIntegerValue<byte>(data?.Flag_Green) > 0)
                return 1;
            return 0;
        }

        /**
         * <summary>Split a sector time into the whole minutes part and the milliseconds left under a minute, as the F1 23 format expects</summary>
         */
        public static void SplitSectorTime(TimeSpan? time, out ushort milliseconds, out byte minutes)
        {
            var value = time ?? TimeSpan.Zero;
            var wholeMinutes = (int)Math.Max(0, value.TotalMinutes);

            minutes = ClampIntegerValue<byte>(wholeMinutes);
            milliseconds = ClampIntegerValue<ushort>(Math.Max(0, (value - TimeSpan.FromMinutes(wholeMinutes)).TotalMilliseconds));
        }

        /**
         * <summary>Clamp the unknown object to the specified integer type min and max values</summary>
         */
        public static T ClampIntegerValue<T>(object value)
        {
            long returnValue = 0;

            try
            {
                if (typeof(T) == typeof(ulong))
                {
                    return (T)Convert.ChangeType(Convert.ToUInt64(value), typeof(T));
                }

                returnValue = Convert.ToInt64(value);

                if (typeof(T) == typeof(byte))
                {
                    returnValue = Math.Max(Math.Min(returnValue, byte.MaxValue), byte.MinValue);
                }
                else if (typeof(T) == typeof(sbyte))
                {
                    returnValue = Math.Max(Math.Min(returnValue, sbyte.MaxValue), sbyte.MinValue);
                }
                else if (typeof(T) == typeof(short))
                {
                    returnValue = Math.Max(Math.Min(returnValue, short.MaxValue), short.MinValue);
                }
                else if (typeof(T) == typeof(ushort))
                {
                    returnValue = Math.Max(Math.Min(returnValue, ushort.MaxValue), ushort.MinValue);
                }
                else if (typeof(T) == typeof(int))
                {
                    returnValue = Math.Max(Math.Min(returnValue, int.MaxValue), int.MinValue);
                }
                else if (typeof(T) == typeof(uint))
                {
                    returnValue = Math.Max(Math.Min(returnValue, uint.MaxValue), uint.MinValue);
                }
            }
            catch { }

            return (T)Convert.ChangeType(returnValue, typeof(T));
        }

        /**
         * <summary>Clamp the unknown object to the specified floating-point type min and max values</summary>
         */
        public static T ClampFloatingValue<T>(object value)
        {
            double returnValue = 0;

            try
            {
                returnValue = Convert.ToDouble(value);

                if (typeof(T) == typeof(float))
                {
                    returnValue = Math.Max(Math.Min(returnValue, float.MaxValue), float.MinValue);
                }
                else if (typeof(T) == typeof(decimal))
                {
                    returnValue = Math.Max(Math.Min(returnValue, Convert.ToDouble(decimal.MaxValue)), Convert.ToDouble(decimal.MinValue));
                }
            }
            catch { }

            return (T)Convert.ChangeType(returnValue, typeof(T));
        }
    }
}
