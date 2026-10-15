using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2BaseDefense
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(PrefabAPI.PluginGUID)]
    [BepInDependency(NetworkingAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class BaseDefensePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.daniil.ror2basedefense";
        public const string PluginName = "RoR2 Base Defense Mode";
        public const string PluginVersion = "2.0.0";

        public static BaseDefensePlugin Instance { get; private set; }
        public static ManualLogSource ModLogger { get; private set; }

        // --- 1) ОБЩИЕ НАСТРОЙКИ И УПРАВЛЕНИЕ ---
        public static ConfigEntry<bool> EnableMode;
        public static ConfigEntry<int> MaxWavesToWin;
        public static ConfigEntry<int> StartingMoney;
        public static ConfigEntry<int> BaseWaveMoneyReward;
        public static ConfigEntry<float> CoreHealBetweenWavesFraction;
        public static ConfigEntry<float> PlayerRespawnDelay;

        // --- 2) НАСТРОЙКИ ЯДРА (Уменьшенный линейный рост - Пункт 7) ---
        public static ConfigEntry<float> CoreBaseHealth;
        public static ConfigEntry<float> CoreHealthPerWave;
        public static ConfigEntry<float> CoreBaseShield;
        public static ConfigEntry<float> CoreShieldPerWave;
        public static ConfigEntry<float> CoreShieldRechargeDelay;

        // --- 3) КЛАВИШИ УПРАВЛЕНИЯ ---
        public static ConfigEntry<KeyCode> ToggleBuildMenuKey;
        public static ConfigEntry<KeyCode> DemolishKey;
        public static ConfigEntry<KeyCode> StartNextWaveKey;
        public static ConfigEntry<KeyCode> InteractDoorKey;
        public static ConfigEntry<float> RefundPercentage;

        // --- 4) ЛИМИТЫ ПОСТРОЕК (Пункт 1: блоки без лимита, станции и турели ограничены) ---
        public static ConfigEntry<int> LimitHealStation;
        public static ConfigEntry<int> LimitMoneyGenerator;
        public static ConfigEntry<int> LimitRepairStation;
        public static ConfigEntry<int> LimitDroneStation;
        public static ConfigEntry<int> LimitPerTurretType;
        public static ConfigEntry<int> LimitTotalTurretsPerPlayer;

        // --- 5) БАЛАНС И РОСТ УРОНА МОНСТРОВ ПО БАЗЕ (Пункт 9) ---
        public static ConfigEntry<float> EnemyStructureDamagePerWave;
        public static ConfigEntry<float> DemolitionDamageMultiplier;
        public static ConfigEntry<float> DroneMaxHealth;
        public static ConfigEntry<int> MaxDronesPerStation;
        public static ConfigEntry<int> MoneyGenTickAmount;
        public static ConfigEntry<float> HealStationPercentPerSec;
        public static ConfigEntry<float> RepairStationPercentPerSec;

        // --- 6) ЦЕНЫ ПОСТРОЕК ---
        public static ConfigEntry<int> CostLightBarricade;
        public static ConfigEntry<int> CostHeavyBlock;
        public static ConfigEntry<int> CostWallBlock;
        public static ConfigEntry<int> CostRampBlock;
        public static ConfigEntry<int> CostWindowBlock;
        public static ConfigEntry<int> CostDoorBlock;
        public static ConfigEntry<int> CostLongWallBlock;
        public static ConfigEntry<int> CostSpikePlatform;
        public static ConfigEntry<int> CostSpringTrap;

        public static ConfigEntry<int> CostMachineGunTurret;
        public static ConfigEntry<int> CostFlameTurret;
        public static ConfigEntry<int> CostTeslaTurret;
        public static ConfigEntry<int> CostRocketTurret;
        public static ConfigEntry<int> CostLaserTurret;

        public static ConfigEntry<int> CostHealStation;
        public static ConfigEntry<int> CostMoneyGen;
        public static ConfigEntry<int> CostDroneStation;
        public static ConfigEntry<int> CostRepairStation;

        private void Awake()
        {
            Instance = this;
            ModLogger = Logger;

            InitConfig();
            NetworkMessages.RegisterMessages();
            BuildableCatalog.InitCatalog();
            HooksInit();

            ModLogger.LogInfo($"{PluginName} v{PluginVersion} успешно загружен!");
        }

        private void InitConfig()
        {
            EnableMode = Config.Bind("1. General", "EnableBaseDefenseMode", true, "Включить режим Защиты Базы.");
            MaxWavesToWin = Config.Bind("1. General", "WavesToWin", 15, "Количество волн для победы.");
            StartingMoney = Config.Bind("1. General", "StartingMoney", 200, "Стартовое золото каждого игрока.");
            BaseWaveMoneyReward = Config.Bind("1. General", "BaseWaveMoneyReward", 85, "Базовая награда золотом за волну.");
            CoreHealBetweenWavesFraction = Config.Bind("1. General", "CoreHealBetweenWaves", 0.08f, "Доля восстановления ХП Ядра между волнами (0.08 = 8%).");
            PlayerRespawnDelay = Config.Bind("1. General", "PlayerRespawnSeconds", 10f, "Время возрождения игрока возле Ядра после смерти (Пункт 13).");

            // Пункт 7: Уменьшенный сбалансированный рост Ядра
            CoreBaseHealth = Config.Bind("2. Energy Core", "CoreBaseHealth", 3500f, "Базовое здоровье Энергетического Ядра.");
            CoreHealthPerWave = Config.Bind("2. Energy Core", "CoreHealthPerWave", 350f, "Прирост макс. здоровья Ядра за каждую волну.");
            CoreBaseShield = Config.Bind("2. Energy Core", "CoreBaseShield", 1500f, "Базовый дополнительный щит Ядра.");
            CoreShieldPerWave = Config.Bind("2. Energy Core", "CoreShieldPerWave", 150f, "Прирост макс. щита Ядра за каждую волну.");
            CoreShieldRechargeDelay = Config.Bind("2. Energy Core", "ShieldRechargeTime", 30f, "Время перезарядки пробитого щита (в секундах).");

            ToggleBuildMenuKey = Config.Bind("3. Controls", "BuildMenuKey", KeyCode.B, "Кнопка открытия магазина строительства.");
            DemolishKey = Config.Bind("3. Controls", "DemolishKey", KeyCode.X, "Кнопка сноса постройки (возврат 50%).");
            StartNextWaveKey = Config.Bind("3. Controls", "StartWaveKey", KeyCode.G, "Кнопка запуска следующей волны (Пункт 12).");
            InteractDoorKey = Config.Bind("3. Controls", "DoorToggleKey", KeyCode.F, "Кнопка открытия/закрытия функциональной двери (Пункт 8).");
            RefundPercentage = Config.Bind("3. Controls", "RefundPercentage", 0.5f, "Процент возврата при сносе (0.5 = 50%).");

            // Пункт 1: Лимиты на станции и турели
            LimitHealStation = Config.Bind("4. Build Limits", "MaxHealStations", 2, "Максимум Хил-станций на игрока.");
            LimitMoneyGenerator = Config.Bind("4. Build Limits", "MaxMoneyGenerators", 2, "Максимум Денежных генераторов на игрока.");
            LimitRepairStation = Config.Bind("4. Build Limits", "MaxRepairStations", 2, "Максимум Ремонтных станций на игрока.");
            LimitDroneStation = Config.Bind("4. Build Limits", "MaxDroneStations", 2, "Максимум Дрон-станций на игрока.");
            LimitPerTurretType = Config.Bind("4. Build Limits", "MaxPerTurretType", 4, "Максимум турелей одного вида на игрока.");
            LimitTotalTurretsPerPlayer = Config.Bind("4. Build Limits", "MaxTotalTurrets", 10, "Общий лимит боевых турелей на одного игрока.");

            // Пункт 9: Масштабирование урона монстров по постройкам и ядру от волны
            EnemyStructureDamagePerWave = Config.Bind("5. Balance", "EnemyStructureDamageBonusPerWave", 0.15f, "Прирост урона монстров по Ядру и постройкам за каждую волну (0.15 = +15% за волну).");
            DemolitionDamageMultiplier = Config.Bind("5. Balance", "DemolitionDamageMultiplier", 2.2f, "Доп. множитель урона врагов класса 'Демонтаж' по постройкам.");
            DroneMaxHealth = Config.Bind("5. Balance", "SpawnedDroneHealth", 45f, "Здоровье дронов Дрон-станции.");
            MaxDronesPerStation = Config.Bind("5. Balance", "MaxDronesPerStation", 3, "Максимум живых дронов от одной Дрон-станции.");
            MoneyGenTickAmount = Config.Bind("5. Balance", "MoneyGenAmountPerTick", 6, "Золото за тик (2 сек) от Денежного генератора.");
            HealStationPercentPerSec = Config.Bind("5. Balance", "HealStationPercentPerSec", 0.06f, "Исцеление игроков от Хил-станции в сек (0.06 = 6%).");
            RepairStationPercentPerSec = Config.Bind("5. Balance", "RepairStationPercentPerSec", 0.025f, "Лечение построек от Ремонтной станции в сек (0.025 = 2.5%).");

            // Цены блоков (Пункт 8)
            CostLightBarricade = Config.Bind("6. Costs", "LightBarricadeCost", 20, "Цена: Низкая баррикада.");
            CostWallBlock = Config.Bind("6. Costs", "WallBlockCost", 30, "Цена: Стена.");
            CostRampBlock = Config.Bind("6. Costs", "RampBlockCost", 30, "Цена: Уклон (Рампа).");
            CostWindowBlock = Config.Bind("6. Costs", "WindowBlockCost", 35, "Цена: Стена с окном для стрельбы.");
            CostDoorBlock = Config.Bind("6. Costs", "DoorBlockCost", 45, "Цена: Функциональная дверь.");
            CostLongWallBlock = Config.Bind("6. Costs", "LongWallBlockCost", 55, "Цена: Длинная стена.");
            CostHeavyBlock = Config.Bind("6. Costs", "HeavyBlockCost", 65, "Цена: Силовой куб-бастион.");
            CostSpikePlatform = Config.Bind("6. Costs", "SpikePlatformCost", 45, "Цена: Шипастая платформа.");
            CostSpringTrap = Config.Bind("6. Costs", "SpringTrapCost", 40, "Цена: Пружинная установка (5 срабатываний).");

            // Цены турелей и станций
            CostMachineGunTurret = Config.Bind("6. Costs", "MachineGunTurretCost", 60, "Цена: Пулеметная турель.");
            CostFlameTurret = Config.Bind("6. Costs", "FlameTurretCost", 85, "Цена: Турель-огнемет.");
            CostTeslaTurret = Config.Bind("6. Costs", "TeslaTurretCost", 100, "Цена: Электро-турель.");
            CostRocketTurret = Config.Bind("6. Costs", "RocketTurretCost", 120, "Цена: Ракетная турель.");
            CostLaserTurret = Config.Bind("6. Costs", "LaserTurretCost", 150, "Цена: Лазерная турель.");

            CostHealStation = Config.Bind("6. Costs", "HealStationCost", 90, "Цена: Хил-станция (только для игроков).");
            CostRepairStation = Config.Bind("6. Costs", "RepairStationCost", 95, "Цена: Ремонтная станция (только для построек).");
            CostMoneyGen = Config.Bind("6. Costs", "MoneyGeneratorCost", 110, "Цена: Денежный генератор.");
            CostDroneStation = Config.Bind("6. Costs", "DroneStationCost", 130, "Цена: Дрон-станция.");
        }

        public static void RefreshPricesInCatalog()
        {
            BuildableCatalog.UpdateFromConfig();
        }

        private void HooksInit()
        {
            On.RoR2.UI.CharacterSelectController.Awake += (orig, self) =>
            {
                orig(self);
                if (self.gameObject.GetComponent<LobbyConfiguratorUI>() == null)
                {
                    self.gameObject.AddComponent<LobbyConfiguratorUI>();
                }
            };

            On.RoR2.Stage.Start += (orig, self) =>
            {
                var enumerator = orig(self);
                if (EnableMode.Value && BaseDefenseManager.Instance == null)
                {
                    var managerObj = new GameObject("BaseDefenseManager_Singleton");
                    managerObj.AddComponent<BaseDefenseManager>();
                }
                return enumerator;
            };

            On.RoR2.SceneDirector.Start += (orig, self) =>
            {
                if (EnableMode.Value)
                {
                    self.teleporterSpawnCard = null;
                    self.interactableCredit = Mathf.RoundToInt(self.interactableCredit * 0.25f);
                }
                orig(self);
            };

            On.RoR2.PlayerCharacterMasterController.Awake += (orig, self) =>
            {
                orig(self);
                if (self.gameObject.GetComponent<PlayerBuildController>() == null)
                {
                    self.gameObject.AddComponent<PlayerBuildController>();
                }
            };

            // Пункт 2: Полная блокировка боевых навыков персонажа (ЛКМ/ПКМ/Shift/R) во время режима строительства или открытого магазина!
            On.RoR2.PlayerCharacterMasterController.PollButtonInput += (orig, self) =>
            {
                orig(self);
                if (!EnableMode.Value || self == null) return;

                var buildCtrl = self.GetComponent<PlayerBuildController>();
                if (buildCtrl != null && buildCtrl.ShouldBlockCombatSkills() && self.bodyInputs != null)
                {
                    self.bodyInputs.skill1.PushState(false);
                    self.bodyInputs.skill2.PushState(false);
                    self.bodyInputs.skill3.PushState(false);
                    self.bodyInputs.skill4.PushState(false);
                }
            };

            // Пункт 9 и 3: Обработка урона по постройкам и Энергетическому Ядру
            On.RoR2.HealthComponent.TakeDamage += CustomDamageHook;
        }

        private void CustomDamageHook(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (NetworkServer.active && self != null && damageInfo != null && EnableMode.Value)
            {
                var victimStructure = self.GetComponent<BuildableStructure>();
                var coreShield = self.GetComponent<CoreShieldController>();
                bool isStructureOrCore = victimStructure != null || coreShield != null;

                if (damageInfo.attacker != null)
                {
                    var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    var attackerMaster = attackerBody != null ? attackerBody.master : null;

                    // Игроки и их турели не наносят урон своим постройкам и Ядру
                    if (isStructureOrCore && TeamComponent.GetObjectTeam(damageInfo.attacker) == TeamIndex.Player)
                    {
                        damageInfo.damage = 0f;
                        damageInfo.rejected = true;
                        orig(self, damageInfo);
                        return;
                    }

                    // Статистика нанесенного урона игроком для финального экрана (Пункт 13)
                    if (attackerMaster != null && attackerMaster.playerCharacterMasterController != null && self.body != null && self.body.teamComponent.teamIndex == TeamIndex.Monster)
                    {
                        BaseDefenseManager.Instance?.RecordPlayerDamage(attackerMaster, damageInfo.damage);
                    }

                    // Пункт 9: С увеличением волны урон монстров по постройкам и Ядру растет (настраиваемо)
                    if (isStructureOrCore && attackerBody != null && attackerBody.teamComponent.teamIndex == TeamIndex.Monster)
                    {
                        int wave = BaseDefenseManager.Instance != null ? Mathf.Max(1, BaseDefenseManager.Instance.CurrentWave) : 1;
                        float waveStructureMult = 1f + (wave - 1) * EnemyStructureDamagePerWave.Value;
                        damageInfo.damage *= waveStructureMult;

                        if (attackerMaster != null)
                        {
                            var enemyMarker = attackerMaster.GetComponent<WaveEnemyMarker>();
                            if (enemyMarker != null && enemyMarker.Archetype == EnemyArchetype.Demolition && victimStructure != null)
                            {
                                damageInfo.damage *= DemolitionDamageMultiplier.Value;
                            }
                        }
                    }
                }

                if (coreShield != null)
                {
                    coreShield.ProcessIncomingDamage(damageInfo);
                }
            }

            orig(self, damageInfo);
        }
    }
}