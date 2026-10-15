using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2BaseDefense
{
    public static class NetworkMessages
    {
        public static void RegisterMessages()
        {
            NetworkingAPI.RegisterMessageType<NetRequestPlaceStructure>();
            NetworkingAPI.RegisterMessageType<NetRequestDemolishStructure>();
            NetworkingAPI.RegisterMessageType<NetRequestToggleDoor>();
            NetworkingAPI.RegisterMessageType<NetRequestStartNextWave>();
            NetworkingAPI.RegisterMessageType<NetRequestRestartMatch>();
            NetworkingAPI.RegisterMessageType<NetSyncCoreAndWaveState>();
            NetworkingAPI.RegisterMessageType<NetSyncStructureMetadata>();
            NetworkingAPI.RegisterMessageType<NetSyncDoorState>();
            NetworkingAPI.RegisterMessageType<NetSyncLobbyConfig>();
            NetworkingAPI.RegisterMessageType<NetSyncPlayerRespawnTimer>();
            NetworkingAPI.RegisterMessageType<NetSyncGameOverStats>();
        }
    }

    // Синхронизация всех настроек лобби от Хоста к Клиентам
    public struct NetSyncLobbyConfig : INetMessage
    {
        public bool EnableMode;
        public int MaxWavesToWin;
        public int StartingMoney;
        public int BaseWaveMoneyReward;
        public float CoreBaseHealth;
        public float CoreHealthPerWave;
        public float CoreBaseShield;
        public float CoreShieldPerWave;
        public float CoreShieldRechargeDelay;
        public float EnemyStructureDamagePerWave;
        public float RefundPercentage;

        public int LimitHealStation;
        public int LimitMoneyGenerator;
        public int LimitRepairStation;
        public int LimitDroneStation;
        public int LimitPerTurretType;
        public int LimitTotalTurretsPerPlayer;

        public static NetSyncLobbyConfig CreateFromCurrentConfig()
        {
            return new NetSyncLobbyConfig
            {
                EnableMode = BaseDefensePlugin.EnableMode.Value,
                MaxWavesToWin = BaseDefensePlugin.MaxWavesToWin.Value,
                StartingMoney = BaseDefensePlugin.StartingMoney.Value,
                BaseWaveMoneyReward = BaseDefensePlugin.BaseWaveMoneyReward.Value,
                CoreBaseHealth = BaseDefensePlugin.CoreBaseHealth.Value,
                CoreHealthPerWave = BaseDefensePlugin.CoreHealthPerWave.Value,
                CoreBaseShield = BaseDefensePlugin.CoreBaseShield.Value,
                CoreShieldPerWave = BaseDefensePlugin.CoreShieldPerWave.Value,
                CoreShieldRechargeDelay = BaseDefensePlugin.CoreShieldRechargeDelay.Value,
                EnemyStructureDamagePerWave = BaseDefensePlugin.EnemyStructureDamagePerWave.Value,
                RefundPercentage = BaseDefensePlugin.RefundPercentage.Value,

                LimitHealStation = BaseDefensePlugin.LimitHealStation.Value,
                LimitMoneyGenerator = BaseDefensePlugin.LimitMoneyGenerator.Value,
                LimitRepairStation = BaseDefensePlugin.LimitRepairStation.Value,
                LimitDroneStation = BaseDefensePlugin.LimitDroneStation.Value,
                LimitPerTurretType = BaseDefensePlugin.LimitPerTurretType.Value,
                LimitTotalTurretsPerPlayer = BaseDefensePlugin.LimitTotalTurretsPerPlayer.Value
            };
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(EnableMode);
            writer.Write(MaxWavesToWin);
            writer.Write(StartingMoney);
            writer.Write(BaseWaveMoneyReward);
            writer.Write(CoreBaseHealth);
            writer.Write(CoreHealthPerWave);
            writer.Write(CoreBaseShield);
            writer.Write(CoreShieldPerWave);
            writer.Write(CoreShieldRechargeDelay);
            writer.Write(EnemyStructureDamagePerWave);
            writer.Write(RefundPercentage);

            writer.Write(LimitHealStation);
            writer.Write(LimitMoneyGenerator);
            writer.Write(LimitRepairStation);
            writer.Write(LimitDroneStation);
            writer.Write(LimitPerTurretType);
            writer.Write(LimitTotalTurretsPerPlayer);
        }

        public void Deserialize(NetworkReader reader)
        {
            EnableMode = reader.ReadBoolean();
            MaxWavesToWin = reader.ReadInt32();
            StartingMoney = reader.ReadInt32();
            BaseWaveMoneyReward = reader.ReadInt32();
            CoreBaseHealth = reader.ReadSingle();
            CoreHealthPerWave = reader.ReadSingle();
            CoreBaseShield = reader.ReadSingle();
            CoreShieldPerWave = reader.ReadSingle();
            CoreShieldRechargeDelay = reader.ReadSingle();
            EnemyStructureDamagePerWave = reader.ReadSingle();
            RefundPercentage = reader.ReadSingle();

            LimitHealStation = reader.ReadInt32();
            LimitMoneyGenerator = reader.ReadInt32();
            LimitRepairStation = reader.ReadInt32();
            LimitDroneStation = reader.ReadInt32();
            LimitPerTurretType = reader.ReadInt32();
            LimitTotalTurretsPerPlayer = reader.ReadInt32();
        }

        public void OnReceived()
        {
            if (NetworkServer.active) return;

            BaseDefensePlugin.EnableMode.Value = EnableMode;
            BaseDefensePlugin.MaxWavesToWin.Value = MaxWavesToWin;
            BaseDefensePlugin.StartingMoney.Value = StartingMoney;
            BaseDefensePlugin.BaseWaveMoneyReward.Value = BaseWaveMoneyReward;
            BaseDefensePlugin.CoreBaseHealth.Value = CoreBaseHealth;
            BaseDefensePlugin.CoreHealthPerWave.Value = CoreHealthPerWave;
            BaseDefensePlugin.CoreBaseShield.Value = CoreBaseShield;
            BaseDefensePlugin.CoreShieldPerWave.Value = CoreShieldPerWave;
            BaseDefensePlugin.CoreShieldRechargeDelay.Value = CoreShieldRechargeDelay;
            BaseDefensePlugin.EnemyStructureDamagePerWave.Value = EnemyStructureDamagePerWave;
            BaseDefensePlugin.RefundPercentage.Value = RefundPercentage;

            BaseDefensePlugin.LimitHealStation.Value = LimitHealStation;
            BaseDefensePlugin.LimitMoneyGenerator.Value = LimitMoneyGenerator;
            BaseDefensePlugin.LimitRepairStation.Value = LimitRepairStation;
            BaseDefensePlugin.LimitDroneStation.Value = LimitDroneStation;
            BaseDefensePlugin.LimitPerTurretType.Value = LimitPerTurretType;
            BaseDefensePlugin.LimitTotalTurretsPerPlayer.Value = LimitTotalTurretsPerPlayer;

            BaseDefensePlugin.RefreshPricesInCatalog();
        }
    }

    public struct NetRequestPlaceStructure : INetMessage
    {
        public NetworkInstanceId BuilderMasterNetId;
        public int BuildableIdInt;
        public Vector3 Position;
        public Quaternion Rotation;

        public NetRequestPlaceStructure(NetworkInstanceId builderMasterNetId, BuildableId id, Vector3 pos, Quaternion rot)
        {
            BuilderMasterNetId = builderMasterNetId;
            BuildableIdInt = (int)id;
            Position = pos;
            Rotation = rot;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(BuilderMasterNetId);
            writer.Write(BuildableIdInt);
            writer.Write(Position);
            writer.Write(Rotation);
        }

        public void Deserialize(NetworkReader reader)
        {
            BuilderMasterNetId = reader.ReadNetworkId();
            BuildableIdInt = reader.ReadInt32();
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
        }

        public void OnReceived()
        {
            if (!NetworkServer.active) return;
            GameObject masterObj = Util.FindNetworkObject(BuilderMasterNetId);
            if (masterObj != null && masterObj.TryGetComponent<CharacterMaster>(out var builderMaster))
            {
                BuildingServerManager.ServerPlaceStructure(builderMaster, (BuildableId)BuildableIdInt, Position, Rotation);
            }
        }
    }

    public struct NetRequestDemolishStructure : INetMessage
    {
        public NetworkInstanceId PlayerMasterNetId;
        public NetworkInstanceId TargetBodyNetId;

        public NetRequestDemolishStructure(NetworkInstanceId playerMasterNetId, NetworkInstanceId targetBodyNetId)
        {
            PlayerMasterNetId = playerMasterNetId;
            TargetBodyNetId = targetBodyNetId;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(PlayerMasterNetId);
            writer.Write(TargetBodyNetId);
        }

        public void Deserialize(NetworkReader reader)
        {
            PlayerMasterNetId = reader.ReadNetworkId();
            TargetBodyNetId = reader.ReadNetworkId();
        }

        public void OnReceived()
        {
            if (!NetworkServer.active) return;
            GameObject masterObj = Util.FindNetworkObject(PlayerMasterNetId);
            GameObject targetBodyObj = Util.FindNetworkObject(TargetBodyNetId);
            if (masterObj != null && targetBodyObj != null)
            {
                var playerMaster = masterObj.GetComponent<CharacterMaster>();
                var structure = targetBodyObj.GetComponent<BuildableStructure>();
                if (playerMaster != null && structure != null)
                {
                    BuildingServerManager.ServerDemolishStructure(playerMaster, structure);
                }
            }
        }
    }

    // Запрос на открытие/закрытие функциональной двери (Пункт 8)
    public struct NetRequestToggleDoor : INetMessage
    {
        public NetworkInstanceId DoorBodyNetId;

        public NetRequestToggleDoor(NetworkInstanceId doorBodyNetId)
        {
            DoorBodyNetId = doorBodyNetId;
        }

        public void Serialize(NetworkWriter writer) => writer.Write(DoorBodyNetId);
        public void Deserialize(NetworkReader reader) => DoorBodyNetId = reader.ReadNetworkId();

        public void OnReceived()
        {
            if (!NetworkServer.active) return;
            GameObject doorObj = Util.FindNetworkObject(DoorBodyNetId);
            if (doorObj != null && doorObj.TryGetComponent<BuildableStructure>(out var structure))
            {
                structure.ServerToggleDoor();
            }
        }
    }

    public struct NetSyncDoorState : INetMessage
    {
        public NetworkInstanceId DoorBodyNetId;
        public bool IsOpen;

        public NetSyncDoorState(NetworkInstanceId netId, bool isOpen)
        {
            DoorBodyNetId = netId;
            IsOpen = isOpen;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(DoorBodyNetId);
            writer.Write(IsOpen);
        }

        public void Deserialize(NetworkReader reader)
        {
            DoorBodyNetId = reader.ReadNetworkId();
            IsOpen = reader.ReadBoolean();
        }

        public void OnReceived()
        {
            GameObject doorObj = Util.FindNetworkObject(DoorBodyNetId);
            if (doorObj != null && doorObj.TryGetComponent<BuildableStructure>(out var structure))
            {
                structure.ApplyDoorStateVisualAndCollider(IsOpen);
            }
        }
    }

    // Запрос от игрока на запуск следующей волны по кнопке (Пункт 12)
    public struct NetRequestStartNextWave : INetMessage
    {
        public void Serialize(NetworkWriter writer) { }
        public void Deserialize(NetworkReader reader) { }

        public void OnReceived()
        {
            if (NetworkServer.active && BaseDefenseManager.Instance != null)
            {
                BaseDefenseManager.Instance.TriggerStartNextWave();
            }
        }
    }

    // Запрос на перезапуск матча после уничтожения Ядра (Пункт 13)
    public struct NetRequestRestartMatch : INetMessage
    {
        public void Serialize(NetworkWriter writer) { }
        public void Deserialize(NetworkReader reader) { }

        public void OnReceived()
        {
            if (NetworkServer.active && BaseDefenseManager.Instance != null)
            {
                BaseDefenseManager.Instance.ServerRestartMatch();
            }
        }
    }

    public struct NetSyncCoreAndWaveState : INetMessage
    {
        public int CurrentWave;
        public int MaxWaves;
        public int AliveEnemiesCount;
        public bool IsWaveActive;
        public bool WaitingForManualStart;

        public float CoreHealth;
        public float CoreMaxHealth;
        public float CoreShield;
        public float CoreMaxShield;
        public float ShieldRechargeTimer;
        public bool IsShieldBroken;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(CurrentWave);
            writer.Write(MaxWaves);
            writer.Write(AliveEnemiesCount);
            writer.Write(IsWaveActive);
            writer.Write(WaitingForManualStart);

            writer.Write(CoreHealth);
            writer.Write(CoreMaxHealth);
            writer.Write(CoreShield);
            writer.Write(CoreMaxShield);
            writer.Write(ShieldRechargeTimer);
            writer.Write(IsShieldBroken);
        }

        public void Deserialize(NetworkReader reader)
        {
            CurrentWave = reader.ReadInt32();
            MaxWaves = reader.ReadInt32();
            AliveEnemiesCount = reader.ReadInt32();
            IsWaveActive = reader.ReadBoolean();
            WaitingForManualStart = reader.ReadBoolean();

            CoreHealth = reader.ReadSingle();
            CoreMaxHealth = reader.ReadSingle();
            CoreShield = reader.ReadSingle();
            CoreMaxShield = reader.ReadSingle();
            ShieldRechargeTimer = reader.ReadSingle();
            IsShieldBroken = reader.ReadBoolean();
        }

        public void OnReceived()
        {
            ClientHudState.CurrentWave = CurrentWave;
            ClientHudState.MaxWaves = MaxWaves;
            ClientHudState.AliveEnemiesCount = AliveEnemiesCount;
            ClientHudState.IsWaveActive = IsWaveActive;
            ClientHudState.WaitingForManualStart = WaitingForManualStart;

            ClientHudState.CoreHealth = CoreHealth;
            ClientHudState.CoreMaxHealth = CoreMaxHealth;
            ClientHudState.CoreShield = CoreShield;
            ClientHudState.CoreMaxShield = CoreMaxShield;
            ClientHudState.ShieldRechargeTimer = ShieldRechargeTimer;
            ClientHudState.IsShieldBroken = IsShieldBroken;
        }
    }

    public struct NetSyncStructureMetadata : INetMessage
    {
        public NetworkInstanceId StructureBodyNetId;
        public NetworkInstanceId OwnerMasterNetId;
        public int BuildableIdInt;
        public int OriginalCost;
        public Vector3 ExactPosition;
        public Quaternion ExactRotation;
        public bool IsEnergyCore;

        public NetSyncStructureMetadata(NetworkInstanceId bodyNetId, NetworkInstanceId ownerNetId, BuildableId id, int cost, Vector3 pos, Quaternion rot, bool isCore = false)
        {
            StructureBodyNetId = bodyNetId;
            OwnerMasterNetId = ownerNetId;
            BuildableIdInt = (int)id;
            OriginalCost = cost;
            ExactPosition = pos;
            ExactRotation = rot;
            IsEnergyCore = isCore;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(StructureBodyNetId);
            writer.Write(OwnerMasterNetId);
            writer.Write(BuildableIdInt);
            writer.Write(OriginalCost);
            writer.Write(ExactPosition);
            writer.Write(ExactRotation);
            writer.Write(IsEnergyCore);
        }

        public void Deserialize(NetworkReader reader)
        {
            StructureBodyNetId = reader.ReadNetworkId();
            OwnerMasterNetId = reader.ReadNetworkId();
            BuildableIdInt = reader.ReadInt32();
            OriginalCost = reader.ReadInt32();
            ExactPosition = reader.ReadVector3();
            ExactRotation = reader.ReadQuaternion();
            IsEnergyCore = reader.ReadBoolean();
        }

        public void OnReceived()
        {
            GameObject bodyObj = Util.FindNetworkObject(StructureBodyNetId);
            if (bodyObj == null) return;

            if (IsEnergyCore)
            {
                VanillaVisualBuilder.StripTurretMechanicsAndHideMesh(bodyObj, true);
                VanillaVisualBuilder.ApplyEnergyCoreVisuals(bodyObj);
                return;
            }

            BuildableId id = (BuildableId)BuildableIdInt;
            BuildableDef def = BuildableCatalog.GetDef(id);
            if (def == null) return;

            var structure = bodyObj.GetComponent<BuildableStructure>();
            if (structure == null)
            {
                structure = bodyObj.AddComponent<BuildableStructure>();
            }
            structure.Id = id;
            structure.OriginalCost = OriginalCost;
            structure.OwnerMasterNetId = OwnerMasterNetId;

            GameObject ownerObj = Util.FindNetworkObject(OwnerMasterNetId);
            if (ownerObj != null) structure.OwnerMaster = ownerObj.GetComponent<CharacterMaster>();

            CharacterBody body = bodyObj.GetComponent<CharacterBody>();
            if (body != null)
            {
                body.baseNameToken = def.Name;
            }

            // Пункты 4, 5, 6: Убираем внутреннюю турель и выравниваем координаты на клиенте
            VanillaVisualBuilder.StripTurretMechanicsAndHideMesh(bodyObj, id != BuildableId.MachineGunTurret);
            VanillaVisualBuilder.ApplyStructureVisualsAndColliders(bodyObj, def, ExactPosition, ExactRotation);
        }
    }

    // Пункт 13: Синхронизация 10-секундного таймера возрождения погибшего игрока
    public struct NetSyncPlayerRespawnTimer : INetMessage
    {
        public NetworkInstanceId PlayerMasterNetId;
        public float RemainingSeconds;

        public NetSyncPlayerRespawnTimer(NetworkInstanceId masterNetId, float remaining)
        {
            PlayerMasterNetId = masterNetId;
            RemainingSeconds = remaining;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(PlayerMasterNetId);
            writer.Write(RemainingSeconds);
        }

        public void Deserialize(NetworkReader reader)
        {
            PlayerMasterNetId = reader.ReadNetworkId();
            RemainingSeconds = reader.ReadSingle();
        }

        public void OnReceived()
        {
            ClientHudState.LocalRespawnMasterNetId = PlayerMasterNetId;
            ClientHudState.LocalRespawnTimer = RemainingSeconds;
        }
    }

    // Пункт 13: Вывод персональной статистики каждому игроку при уничтожении Ядра или Победе
    public struct NetSyncGameOverStats : INetMessage
    {
        public NetworkInstanceId TargetMasterNetId;
        public bool IsVictory;
        public int ReachedWave;
        public int Kills;
        public float DamageDealt;
        public int StructuresBuilt;
        public int StructuresDemolished;
        public int TotalMoneyEarned;
        public int DeathsCount;

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(TargetMasterNetId);
            writer.Write(IsVictory);
            writer.Write(ReachedWave);
            writer.Write(Kills);
            writer.Write(DamageDealt);
            writer.Write(StructuresBuilt);
            writer.Write(StructuresDemolished);
            writer.Write(TotalMoneyEarned);
            writer.Write(DeathsCount);
        }

        public void Deserialize(NetworkReader reader)
        {
            TargetMasterNetId = reader.ReadNetworkId();
            IsVictory = reader.ReadBoolean();
            ReachedWave = reader.ReadInt32();
            Kills = reader.ReadInt32();
            DamageDealt = reader.ReadSingle();
            StructuresBuilt = reader.ReadInt32();
            StructuresDemolished = reader.ReadInt32();
            TotalMoneyEarned = reader.ReadInt32();
            DeathsCount = reader.ReadInt32();
        }

        public void OnReceived()
        {
            GameOverStatsUI.ReceiveStatsPacket(this);
        }
    }

    public static class ClientHudState
    {
        public static int CurrentWave = 0;
        public static int MaxWaves = 15;
        public static int AliveEnemiesCount = 0;
        public static bool IsWaveActive = false;
        public static bool WaitingForManualStart = true;

        public static float CoreHealth = 3500f;
        public static float CoreMaxHealth = 3500f;
        public static float CoreShield = 1500f;
        public static float CoreMaxShield = 1500f;
        public static float ShieldRechargeTimer = 0f;
        public static bool IsShieldBroken = false;

        public static NetworkInstanceId LocalRespawnMasterNetId;
        public static float LocalRespawnTimer = 0f;
    }
}