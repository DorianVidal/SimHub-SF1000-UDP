using SimHub.Plugins;
using SimHubSF1000UDP.DataStructures_F123;
using System;
using System.Runtime.InteropServices;

namespace SimHubSF1000UDP.Packets_F123
{
    internal static class CarDamageDataPacket
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
                m_packetId = 10,
                m_sessionUID = 0,
                m_sessionTime = 0,
                m_frameIdentifier = FrameCount,
                m_overallFrameIdentifier = FrameCount,
                m_playerCarIndex = 0,
                m_secondaryPlayerCarIndex = 255,
            };

            var packet = new PacketCarDamageData
            {
                m_header = header,
                m_carDamageData = new CarDamageData[22]
            };

            packet.m_carDamageData[0] = new CarDamageData
            {
                m_tyresWear = new float[4]
                {
                    100f - Utils.ClampFloatingValue<float>(data?.TyreWearRearLeft),
                    100f - Utils.ClampFloatingValue<float>(data?.TyreWearRearRight),
                    100f - Utils.ClampFloatingValue<float>(data?.TyreWearFrontLeft),
                    100f - Utils.ClampFloatingValue<float>(data?.TyreWearFrontRight),
                },
                m_tyresDamage = new byte[4]
                {
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearRearLeft)),
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearRearRight)),
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearFrontLeft)),
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearFrontRight)),
                },
                m_brakesDamage = new byte[4] { 0, 0, 0, 0 },
                m_frontLeftWingDamage = 0,
                m_frontRightWingDamage = 0,
                m_rearWingDamage = 0,
                m_floorDamage = 0,
                m_diffuserDamage = 0,
                m_sidepodDamage = 0,
                m_drsFault = 0,
                m_ersFault = 0,
                m_gearBoxDamage = 0,
                m_engineDamage = 0,
                m_engineMGUHWear = 0,
                m_engineESWear = 0,
                m_engineCEWear = 0,
                m_engineICEWear = 0,
                m_engineMGUKWear = 0,
                m_engineTCWear = 0,
                m_engineBlown = 0,
                m_engineSeized = 0,
            };

            return Utils.StructToBytes(packet);
        }
    }
}
