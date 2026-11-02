using SimHub.Plugins;
using SimHubSF1000UDP.DataStructures_F123;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SimHubSF1000UDP.Packets_F123
{
    internal static class LapDataPacket
    {
        private readonly static PluginManager pluginManager = PluginManager.GetInstance();
        private static uint FrameCount = 0;

        private static bool LastDataIsInPitLane = false;
        private static Stopwatch PitLaneStopwatch = new();

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
                m_packetId = 2,
                m_sessionUID = 0,
                m_sessionTime = 0,
                m_frameIdentifier = FrameCount,
                m_overallFrameIdentifier = FrameCount,
                m_playerCarIndex = 0,
                m_secondaryPlayerCarIndex = 255,
            };

            var packet = new PacketLapData
            {
                m_header = header,
                m_lapData = new LapData[22],
                m_timeTrialPBCarIdx = 255,
                m_timeTrialRivalCarIdx = 255,
            };

            float lapDistance = Utils.ClampFloatingValue<float>(Convert.ToDouble(data?.TrackPositionPercent) * Convert.ToDouble(data?.TrackLength));
            float totalDistance = lapDistance + Utils.ClampIntegerValue<ushort>(data?.CompletedLaps) * Utils.ClampIntegerValue<ushort>(data?.TrackLength);
            byte pitStatus = Utils.ClampIntegerValue<byte>(data?.IsInPit) == 1 ? (byte)2 : Utils.ClampIntegerValue<byte>(data?.IsInPitLane);

            Utils.SplitSectorTime(data?.Sector1Time, out var sector1Ms, out var sector1Minutes);
            Utils.SplitSectorTime(data?.Sector2Time, out var sector2Ms, out var sector2Minutes);

            CheckPitLaneTimer(data?.IsInPitLane == 1);

            packet.m_lapData[0] = new LapData
            {
                m_lastLapTimeInMS = Utils.ClampFloatingValue<uint>((data?.LastLapTime ?? new TimeSpan()).TotalMilliseconds),
                m_currentLapTimeInMS = Utils.ClampFloatingValue<uint>((data?.CurrentLapTime ?? new TimeSpan()).TotalMilliseconds),
                m_sector1TimeInMS = sector1Ms,
                m_sector1TimeMinutes = sector1Minutes,
                m_sector2TimeInMS = sector2Ms,
                m_sector2TimeMinutes = sector2Minutes,
                m_deltaToCarInFrontInMS = 0,
                m_deltaToRaceLeaderInMS = 0,
                m_lapDistance = lapDistance,
                m_totalDistance = totalDistance,
                m_safetyCarDelta = 0,
                m_carPosition = Utils.ClampIntegerValue<byte>(data?.Position),
                m_currentLapNum = Utils.ClampIntegerValue<byte>(data?.CurrentLap),
                m_pitStatus = pitStatus,
                m_numPitStops = 0,
                m_sector = Utils.ClampIntegerValue<byte>(data?.CurrentSectorIndex),
                m_currentLapInvalid = Utils.ClampIntegerValue<byte>((data?.LapInvalidated ?? false) ? 1 : 0),
                m_penalties = 0,
                m_totalWarnings = 0,
                m_cornerCuttingWarnings = 0,
                m_numUnservedDriveThroughPens = 0,
                m_numUnservedStopGoPens = 0,
                m_gridPosition = 1,
                m_driverStatus = 4,
                m_resultStatus = 2,
                m_pitLaneTimerActive = Utils.ClampIntegerValue<byte>(data?.IsInPitLane),
                m_pitLaneTimeInLaneInMS = Utils.ClampIntegerValue<ushort>(PitLaneStopwatch.ElapsedMilliseconds),
                m_pitStopTimerInMS = Utils.ClampIntegerValue<ushort>(data?.IsInPitSince),
                m_pitStopShouldServePen = 0,
            };

            if (pluginManager.GameName == "GranTurismo7" && packet.m_lapData[0].m_currentLapNum > 0)
            {
                packet.m_lapData[0].m_currentLapNum--;
            }

            return Utils.StructToBytes(packet);
        }

        private static void CheckPitLaneTimer(bool isInPitLane)
        {
            if (LastDataIsInPitLane != isInPitLane)
            {
                if (isInPitLane)
                {
                    if (!PitLaneStopwatch.IsRunning)
                    {
                        PitLaneStopwatch.Start();
                    }
                }
                else
                {
                    if (PitLaneStopwatch.IsRunning)
                    {
                        PitLaneStopwatch.Reset();
                    }
                }

                LastDataIsInPitLane = isInPitLane;
            }
        }
    }
}
