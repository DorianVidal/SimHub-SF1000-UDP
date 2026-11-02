using GameReaderCommon;
using SimHub.Plugins;
using SimHubSF1000UDP.DataStructures_F123;
using System;
using System.Runtime.InteropServices;

namespace SimHubSF1000UDP.Packets_F123
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
                m_packetFormat = 2023,
                m_gameYear = 23,
                m_gameMajorVersion = 1,
                m_gameMinorVersion = 18,
                m_packetVersion = 1,
                m_packetId = 7,
                m_sessionUID = 0,
                m_sessionTime = 0,
                m_frameIdentifier = FrameCount,
                m_overallFrameIdentifier = FrameCount,
                m_playerCarIndex = 0,
                m_secondaryPlayerCarIndex = 255,
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
                m_actualTyreCompound = 16,
                m_visualTyreCompound = 16,
                m_tyresAgeLaps = 0,
                m_vehicleFiaFlags = GetFlag(data, Utils.IsLMUDeployingERS(pluginManager), Utils.IsLMUHarvestingERS(pluginManager)),
                m_enginePowerICE = Utils.ClampFloatingValue<float>(data?.EngineTorque),
                m_enginePowerMGUK = 0,
                m_ersStoreEnergy = Utils.ClampFloatingValue<float>(data?.ERSStored),
                m_ersDeployMode = 1,
                // Note: HarvestingPercent on wheel is m_ersHarvestedThisLapMGUK / m_ersHarvestedThisLapMGUH. Data below is incorrectly implemented on the wheel and so cannot be used
                m_ersHarvestedThisLapMGUK = 100f,
                m_ersHarvestedThisLapMGUH = 1f,
                m_ersDeployedThisLap = Utils.ClampFloatingValue<float>(data?.ERSPercent),
                m_networkPaused = 0,
            };

            if (pluginManager.GameName == "LMU")
            {
                // SimHub doesn't fill ERSStored/ERSMax for LMU. The store is sent in joules (relative to the F1 battery capacity of 4 MJ),
                // the deployed meter as a percentage
                var battery = Utils.GetLMUBatteryFraction(pluginManager);
                if (battery.HasValue)
                {
                    packet.m_carStatusData[0].m_ersStoreEnergy = (float)(battery.Value * 4_000_000);
                    packet.m_carStatusData[0].m_ersDeployedThisLap = (float)(battery.Value * 100);
                }
            }
            else if (pluginManager.GameName == "AssettoCorsa")
            {
                packet.m_carStatusData[0].m_ersStoreEnergy = Utils.ClampFloatingValue<float>(Utils.ClampFloatingValue<float>(data?.ERSMax) - packet.m_carStatusData[0].m_ersStoreEnergy);

                byte ersMode;
                if (Utils.ClampIntegerValue<byte>(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.KersInput")) == 1)
                {
                    ersMode = 3;
                }
                else
                {
                    ersMode = Utils.ClampIntegerValue<byte>(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Physics.ErsPowerLevel"));
                    ersMode = ersMode switch
                    {
                        1 or 2 => 1,
                        3 => 3,
                        4 or 5 => 2,
                        _ => 0,
                    };
                }

                packet.m_carStatusData[0].m_ersDeployMode = ersMode;
            }

            return Utils.StructToBytes(packet);
        }

        // The wheel's side LEDs only have blue (value 2), orange (3) and red (4). For LMU: blue = hybrid deploying, red = hybrid harvesting.
        // A yellow flag takes priority. In ABS/TC LED mode: blue = TC, red = ABS / wheel lock
        private static sbyte GetFlag(StatusDataBase data, bool ersDeploying, bool ersHarvesting)
        {
            if (SimHubSF1000UDPSettings.Instance.LEDMode == LEDModes.ABSTC)
                return Utils.GetABSTCFlag(data);
            if (Utils.ClampIntegerValue<byte>(data?.Flag_Yellow) > 0)
                return 3;
            if (ersDeploying || Utils.ClampIntegerValue<byte>(data?.Flag_Blue) > 0)
                return 2;
            if (ersHarvesting)
                return 4;
            if (Utils.ClampIntegerValue<byte>(data?.Flag_Green) > 0)
                return 1;
            return 0;
        }
    }
}
