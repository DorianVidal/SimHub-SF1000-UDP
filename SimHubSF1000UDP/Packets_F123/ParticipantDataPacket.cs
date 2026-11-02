using SimHub.Plugins;
using SimHubSF1000UDP.DataStructures_F123;
using System;
using System.Runtime.InteropServices;

namespace SimHubSF1000UDP.Packets_F123
{
    internal static class ParticipantDataPacket
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
                m_packetId = 4,
                m_sessionUID = 0,
                m_sessionTime = 0,
                m_frameIdentifier = FrameCount,
                m_overallFrameIdentifier = FrameCount,
                m_playerCarIndex = 0,
                m_secondaryPlayerCarIndex = 255,
            };

            var packet = new PacketParticipantsData
            {
                m_header = header,
                m_numActiveCars = Utils.ClampIntegerValue<byte>(data?.OpponentsCount),
                m_participants = new ParticipantData[22]
            };

            return Utils.StructToBytes(packet);
        }
    }
}
