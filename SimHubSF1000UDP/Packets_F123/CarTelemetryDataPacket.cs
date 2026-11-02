using SimHub.Plugins;
using SimHubSF1000UDP.DataStructures_F123;
using System;
using System.Runtime.InteropServices;

namespace SimHubSF1000UDP.Packets_F123
{
    internal static class CarTelemetryDataPacket
    {
        private readonly static PluginManager pluginManager = PluginManager.GetInstance();
        private static uint FrameCount = 0;

        public static byte[] Read()
        {
            var last = pluginManager.LastData;
            var data = last?.NewData;
            var start = !SimHubSF1000UDPSettings.Instance.OnlySendDataIfGameRunning || (last?.GameRunning ?? false);

            if (!start)
            {
                return new byte[0];
            }

            FrameCount = (FrameCount + 1) % uint.MaxValue;

            var header = new PacketHeader
            {
                m_packetFormat = 2023,
                m_gameYear = 23,
                m_gameMajorVersion = 1,
                m_gameMinorVersion = 18,
                m_packetVersion = 1,
                m_packetId = 6,
                m_sessionUID = 0,
                m_sessionTime = 0,
                m_frameIdentifier = FrameCount,
                m_overallFrameIdentifier = FrameCount,
                m_playerCarIndex = 0,
                m_secondaryPlayerCarIndex = 255,
            };

            var packet = new PacketCarTelemetryData
            {
                m_header = header,
                m_carTelemetryData = new CarTelemetryData[22],
                m_mfdPanelIndex = 255,
                m_mfdPanelIndexSecondaryPlayer = 255,
                m_suggestedGear = 0
            };

            var _gear = data?.Gear;
            sbyte gear = _gear == "N" ? (sbyte)0 : (_gear == "R" ? (sbyte)-1 : Utils.ClampIntegerValue<sbyte>(_gear));

            var redlinePercent = Math.Max(0, Convert.ToDouble(data?.Rpms) - Convert.ToDouble(data?.CarSettings_CurrentGearRedLineRPM))
                                / (Convert.ToDouble(data?.CarSettings_MaxRPM) - Convert.ToDouble(data?.CarSettings_CurrentGearRedLineRPM));
            var revLightsPercent = Convert.ToDouble(data?.CarSettings_RPMShiftLight1) / 3.0
                                    + Convert.ToDouble(data?.CarSettings_RPMShiftLight2) / 3.0
                                    + redlinePercent / 3.0;

            var engineTemp = Utils.ClampIntegerValue<ushort>(data?.OilTemperature);
            if (engineTemp == 0)
            {
                engineTemp = 108;
            }

            packet.m_carTelemetryData[0] = new CarTelemetryData
            {
                m_speed = Utils.ClampIntegerValue<ushort>(data?.SpeedKmh),
                m_throttle = Utils.ClampFloatingValue<float>(data?.Throttle) / 100f,
                m_steer = 0,
                m_brake = Utils.ClampFloatingValue<float>(data?.Brake) / 100f,
                m_clutch = Utils.ClampIntegerValue<byte>(Math.Max(0, Math.Min(100, Convert.ToDouble(data?.Clutch)))),     // Clutch value is different each game, some returns percentage, some returns raw value (3700). We don't have MaxClutch property so unable to find out the percentage, raw values are capped to 100
                m_gear = gear,
                m_engineRPM = Utils.ClampIntegerValue<ushort>(data?.Rpms),
                m_drs = Utils.ClampIntegerValue<byte>(data?.DRSEnabled),
                m_revLightsPercent = Utils.ClampIntegerValue<byte>(revLightsPercent * 100),
                m_revLightsBitValue = 0b0000000000000000,
                m_brakesTemperature = new ushort[4]
                {
                    Utils.ClampIntegerValue<ushort>(data?.BrakeTemperatureRearLeft),
                    Utils.ClampIntegerValue<ushort>(data?.BrakeTemperatureRearRight),
                    Utils.ClampIntegerValue<ushort>(data?.BrakeTemperatureFrontLeft),
                    Utils.ClampIntegerValue<ushort>(data?.BrakeTemperatureFrontRight),
                },
                m_tyresSurfaceTemperature = new byte[4]
                {
                    Utils.ClampIntegerValue<byte>(data?.TyreTemperatureRearLeft),
                    Utils.ClampIntegerValue<byte>(data?.TyreTemperatureRearRight),
                    Utils.ClampIntegerValue<byte>(data?.TyreTemperatureFrontLeft),
                    Utils.ClampIntegerValue<byte>(data?.TyreTemperatureFrontRight),
                },
                m_tyresInnerTemperature = new byte[4]
                {
                    Utils.ClampIntegerValue<byte>(data?.TyreTemperatureRearLeftInner),
                    Utils.ClampIntegerValue<byte>(data?.TyreTemperatureRearRightInner),
                    Utils.ClampIntegerValue<byte>(data?.TyreTemperatureFrontLeftInner),
                    Utils.ClampIntegerValue<byte>(data?.TyreTemperatureFrontRightInner),
                },
                m_engineTemperature = engineTemp,
                m_tyresPressure = new float[4]
                {
                    Utils.ClampFloatingValue<float>(data?.TyrePressureRearLeft),
                    Utils.ClampFloatingValue<float>(data?.TyrePressureRearRight),
                    Utils.ClampFloatingValue<float>(data?.TyrePressureFrontLeft),
                    Utils.ClampFloatingValue<float>(data?.TyrePressureFrontRight),
                },
                m_surfaceType = new byte[4] { 0, 0, 0, 0 },
            };

            return Utils.StructToBytes(packet);
        }
    }
}
