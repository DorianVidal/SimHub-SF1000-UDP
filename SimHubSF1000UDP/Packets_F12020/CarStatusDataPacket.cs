using SimHub.Plugins;
using SimHubSF1000UDP.DataStructures_F12020;
using System;
using System.Runtime.InteropServices;

namespace SimHubSF1000UDP.Packets_F12020
{
    internal static class CarStatusDataPacket
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
                m_packetId = 7
            };

            var packet = new PacketCarStatusData
            {
                m_header = header,
                m_carStatusData = new CarStatusData[22]
            };

            packet.m_carStatusData[0] = new CarStatusData
            {
                m_tractionControl = Utils.ClampIntegerValue<byte>(Math.Min(Utils.ClampIntegerValue<int>(data?.TCLevel), 2)),
                m_antiLockBrakes = Utils.ClampIntegerValue<byte>(Math.Min(Utils.ClampIntegerValue<int>(data?.ABSLevel), 1)),
                m_fuelMix = 1,
                m_frontBrakeBias = Utils.ClampIntegerValue<byte>(data?.BrakeBias),
                m_pitLimiterStatus = Utils.ClampIntegerValue<byte>(data?.PitLimiterOn),
                m_fuelInTank = Utils.ClampFloatingValue<float>(data?.Fuel),
                m_fuelCapacity = Utils.ClampFloatingValue<float>(data?.MaxFuel),
                m_fuelRemainingLaps = Utils.ClampFloatingValue<float>(pluginManager.GetPropertyValue("DataCorePlugin.Computed.Fuel_RemainingLaps")),
                m_maxRPM = Utils.ClampIntegerValue<ushort>(data?.CarSettings_MaxRPM),
                m_idleRPM = Utils.ClampIntegerValue<ushort>(Convert.ToDouble(data?.CarSettings_MaxRPM) * 0.27),
                m_maxGears = Utils.ClampIntegerValue<byte>(data?.CarSettings_MaxGears),
                m_drsAllowed = Utils.ClampIntegerValue<byte>(data?.DRSAvailable),
                m_drsActivationDistance = (ushort)(Convert.ToBoolean(data?.DRSAvailable) ? 0 : 50),
                m_tyresWear = new byte[4]
                {
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearRearLeft)),
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearRearRight)),
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearFrontLeft)),
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearFrontRight)),
                },
                m_actualTyreCompound = 16,
                m_visualTyreCompound = 16,
                m_tyresAgeLaps = 0,
                m_tyresDamage = new byte[4]
                {
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearRearLeft)),
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearRearRight)),
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearFrontLeft)),
                    (byte)(100 - Utils.ClampIntegerValue<byte>(data?.TyreWearFrontRight)),
                },
                m_frontLeftWingDamage = 0,
                m_frontRightWingDamage = 0,
                m_rearWingDamage = 0,
                m_drsFault = 0,
                m_engineDamage = 0,
                m_gearBoxDamage = 0,
                m_vehicleFiaFlags = WheelLeds.GetFlag(pluginManager, data, blackFlagIsRed: true),
                m_ersStoreEnergy = Utils.ClampFloatingValue<float>(data?.ERSStored),
                m_ersDeployMode = 1,
                m_ersHarvestedThisLapMGUK = 100f,
                m_ersHarvestedThisLapMGUH = 1f,
                m_ersDeployedThisLap = 100f
            };

            if (pluginManager.GameName == "LMU")
            {
                // SimHub doesn't fill ERSStored/ERSMax for LMU. The wheel reads the deployed meter as a percentage and never displayed
                // the energy store in any scale tried, so the same percentage is sent to both
                var battery = Utils.GetLMUBatteryFraction(pluginManager);
                if (battery.HasValue)
                {
                    packet.m_carStatusData[0].m_ersStoreEnergy = (float)(battery.Value * 100);
                    packet.m_carStatusData[0].m_ersDeployedThisLap = (float)(battery.Value * 100);
                }
            }
            else if (pluginManager.GameName == "AssettoCorsa")
            {
                packet.m_carStatusData[0].m_ersDeployedThisLap = 100 - (Utils.ClampFloatingValue<float>(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.KersCurrentKJ")) * 100000 / Utils.ClampFloatingValue<float>(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.StaticInfo.ErsMaxJ")));

                byte ersMode;
                if (Utils.ClampIntegerValue<byte>(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.KersInput")) == 1)
                {
                    ersMode = 2;
                }
                else
                {
                    ersMode = Utils.ClampIntegerValue<byte>(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.ErsPowerLevel"));
                    ersMode = ersMode switch
                    {
                        1 or 2 => 1,
                        3 => 2,
                        4 or 5 => 3,
                        _ => 0,
                    };
                }

                packet.m_carStatusData[0].m_ersDeployMode = ersMode;
            }

            return Utils.StructToBytes(packet);
        }
    }
}
