using SimHub.Plugins;
using SimHubSF1000UDP.DataStructures_F12020;
using System;
using System.Runtime.InteropServices;

namespace SimHubSF1000UDP.Packets_F12020
{
    internal static class SessionDataPacket
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
                m_packetFormat = 2020,
                m_gameMajorVersion = 1,
                m_gameMinorVersion = 16,
                m_packetVersion = 1,
                m_sessionUID = 0,
                m_sessionTime = 0,
                m_frameIdentifier = FrameCount,
                m_playerCarIndex = 0,
                m_secondaryPlayerCarIndex = 255,
                m_packetId = 1
            };

            var packet = new PacketSessionData
            {
                m_header = header,
                m_weather = 0,
                m_trackTemperature = Utils.ClampIntegerValue<sbyte>(data?.RoadTemperature),
                m_airTemperature = Utils.ClampIntegerValue<sbyte>(data?.AirTemperature),
                m_totalLaps = Utils.ClampIntegerValue<byte>(data?.TotalLaps),
                m_trackLength = Utils.ClampIntegerValue<ushort>(data?.TrackLength),
                m_sessionType = 0,
                m_trackId = -1,
                m_formula = 0,
                m_sessionTimeLeft = Utils.ClampIntegerValue<ushort>((data?.SessionTimeLeft ?? new TimeSpan()).TotalSeconds),
                m_sessionDuration = 600,
                m_pitSpeedLimit = 80,
                m_gamePaused = Utils.ClampIntegerValue<byte>(last?.GamePaused),
                m_isSpectating = Utils.ClampIntegerValue<byte>(data?.Spectating),
                m_spectatorCarIndex = 0,
                m_sliProNativeSupport = 0,
                m_numMarshalZones = 0,
                m_marshalZones = new MarshalZone[21],
                m_safetyCarStatus = 0,
                m_networkGame = 0,
                m_numWeatherForecastSamples = 0,
                m_weatherForecastSamples = new WeatherForecastSample[20]
            };

            return Utils.StructToBytes(packet);
        }
    }
}
