using SimHub.Plugins;
using SimHubSF1000UDP.DataStructures_F12020;
using System;
using System.Runtime.InteropServices;

namespace SimHubSF1000UDP.Packets_F12020
{
    internal static class LapDataPacket
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
                m_packetId = 2
            };

            var packet = new PacketLapData
            {
                m_header = header,
                m_lapData = new LapData[22]
            };

            float lapDistance = Utils.ClampFloatingValue<float>(Convert.ToDouble(data?.TrackPositionPercent) * Convert.ToDouble(data?.TrackLength));
            float totalDistance = lapDistance + Utils.ClampIntegerValue<ushort>(data?.CompletedLaps) * Utils.ClampIntegerValue<ushort>(data?.TrackLength);
            byte pitStatus = Utils.ClampIntegerValue<byte>(data?.IsInPit) == 1 ? (byte)2 : Utils.ClampIntegerValue<byte>(data?.IsInPitLane);

            packet.m_lapData[0] = new LapData
            {
                m_lastLapTime = Utils.ClampFloatingValue<float>((data?.LastLapTime ?? new TimeSpan()).TotalSeconds),
                m_currentLapTime = Utils.ClampFloatingValue<float>((data?.CurrentLapTime ?? new TimeSpan()).TotalSeconds),
                m_sector1TimeInMS = Utils.ClampIntegerValue<ushort>((data?.Sector1Time ?? new TimeSpan()).TotalMilliseconds),
                m_sector2TimeInMS = Utils.ClampIntegerValue<ushort>((data?.Sector2Time ?? new TimeSpan()).TotalMilliseconds),
                m_bestLapTime = Utils.ClampFloatingValue<float>((data?.BestLapTime ?? new TimeSpan()).TotalSeconds),
                m_bestLapNum = 0,
                m_bestLapSector1TimeInMS = Utils.ClampIntegerValue<ushort>((data?.Sector1BestLapTime ?? new TimeSpan()).TotalMilliseconds),
                m_bestLapSector2TimeInMS = Utils.ClampIntegerValue<ushort>((data?.Sector2BestLapTime ?? new TimeSpan()).TotalMilliseconds),
                m_bestLapSector3TimeInMS = Utils.ClampIntegerValue<ushort>((data?.Sector3BestLapTime ?? new TimeSpan()).TotalMilliseconds),
                m_bestOverallSector1TimeInMS = Utils.ClampIntegerValue<ushort>((data?.Sector1BestTime ?? new TimeSpan()).TotalMilliseconds),
                m_bestOverallSector1LapNum = 0,
                m_bestOverallSector2TimeInMS = Utils.ClampIntegerValue<ushort>((data?.Sector2BestTime ?? new TimeSpan()).TotalMilliseconds),
                m_bestOverallSector2LapNum = 0,
                m_bestOverallSector3TimeInMS = Utils.ClampIntegerValue<ushort>((data?.Sector3BestTime ?? new TimeSpan()).TotalMilliseconds),
                m_bestOverallSector3LapNum = 0,
                m_lapDistance = lapDistance,
                m_totalDistance = totalDistance,
                m_safetyCarDelta = 0,
                m_carPosition = Utils.ClampIntegerValue<byte>(data?.Position),
                m_currentLapNum = Utils.ClampIntegerValue<byte>(data?.CurrentLap),
                m_pitStatus = pitStatus,
                m_sector = Utils.ClampIntegerValue<byte>(data?.CurrentSectorIndex),
                m_currentLapInvalid = 0,
                m_penalties = 0,
                m_gridPosition = 1,
                m_driverStatus = 4,
                m_resultStatus = 2,
            };

            if (pluginManager.GameName == "GranTurismo7" && packet.m_lapData[0].m_currentLapNum > 0)
            {
                packet.m_lapData[0].m_currentLapNum--;
            }

            return Utils.StructToBytes(packet);
        }
    }
}
