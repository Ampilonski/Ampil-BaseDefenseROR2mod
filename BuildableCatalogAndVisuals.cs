using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RoR2BaseDefense
{
    public enum BuildCategory
    {
        BlocksAndTraps = 0, // Укрепления, блоки и ловушки (Без лимита)
        Turrets = 1,        // Боевые турели
        Stations = 2        // Станции и механизмы (Макс 2 на игрока)
    }

    public enum BuildableId
    {
        // Категория 0: Укрепления и ловушки (9 видов)
        LightBarricade = 0,
        HeavyBlock = 1,
        WallBlock = 10,
        RampBlock = 11,
        WindowBlock = 12,
        DoorBlock = 13,
        LongWallBlock = 14,
        SpikePlatform = 15,
        SpringTrap = 16,

        // Категория 1: Турели (5 видов)
        MachineGunTurret = 2,
        RocketTurret = 3,
        LaserTurret = 4,
        TeslaTurret = 5,
        FlameTurret = 6,

        // Категория 2: Станции (4 вида)
        DroneStation = 7,
        MoneyGenerator = 8,
        HealStation = 9,
        RepairStation = 17
    }

    public class BuildableDef
    {
        public BuildableId Id;
        public BuildCategory Category;
        public string Name;
        public string ShortDesc;
        public int Cost;
        public float MaxHealth;
        public float BaseDamage;
        public float BaseArmor;
        public int MaxLimitPerPlayer; // 0 = Без лимита (для блоков)
        public bool IsBlockOrTrap;
        public string IconAddress;
        public Color ThemeColor;
        public Texture2D CachedIcon;
    }

    public static class BuildableCatalog
    {
        public static List<BuildableDef> Definitions = new List<BuildableDef>();

        public static void InitCatalog()
        {
            Definitions.Clear();

            // ==================== КАТЕГОРИЯ 1: БЛОКИ И ЛОВУШКИ (БЕЗ ЛИМИТА) ====================
            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.LightBarricade,
                Category = BuildCategory.BlocksAndTraps,
                Name = "Низкая Баррикада",
                ShortDesc = "Преграда по пояс: задерживает врагов, но позволяет стрелять поверх неё",
                Cost = BaseDefensePlugin.CostLightBarricade.Value,
                MaxHealth = 850f,
                BaseArmor = 10f,
                MaxLimitPerPlayer = 0,
                IsBlockOrTrap = true,
                IconAddress = "RoR2/Base/ArmorPlate/texRepulsionArmorPlateIcon.png",
                ThemeColor = new Color(0.55f, 0.65f, 0.75f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.WallBlock,
                Category = BuildCategory.BlocksAndTraps,
                Name = "Крепкая Стена",
                ShortDesc = "Стандартная высокая стена для перекрытия коридоров",
                Cost = BaseDefensePlugin.CostWallBlock.Value,
                MaxHealth = 1400f,
                BaseArmor = 15f,
                MaxLimitPerPlayer = 0,
                IsBlockOrTrap = true,
                IconAddress = "RoR2/Base/Buckler/texRoseBucklerIcon.png",
                ThemeColor = new Color(0.45f, 0.55f, 0.68f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.LongWallBlock,
                Category = BuildCategory.BlocksAndTraps,
                Name = "Длинная Стена",
                ShortDesc = "Широкая 7-метровая крепостная стена для быстрой застройки периметра",
                Cost = BaseDefensePlugin.CostLongWallBlock.Value,
                MaxHealth = 2200f,
                BaseArmor = 20f,
                MaxLimitPerPlayer = 0,
                IsBlockOrTrap = true,
                IconAddress = "RoR2/Base/BarrierOnOverheal/texAegisIcon.png",
                ThemeColor = new Color(0.40f, 0.50f, 0.62f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.WindowBlock,
                Category = BuildCategory.BlocksAndTraps,
                Name = "Стена с Бойницей (Окно)",
                ShortDesc = "Блокирует проход монстрам, но имеет центральное окно для прострела",
                Cost = BaseDefensePlugin.CostWindowBlock.Value,
                MaxHealth = 1250f,
                BaseArmor = 15f,
                MaxLimitPerPlayer = 0,
                IsBlockOrTrap = true,
                IconAddress = "RoR2/Base/CritGlasses/texGlassesIcon.png",
                ThemeColor = new Color(0.50f, 0.68f, 0.82f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.DoorBlock,
                Category = BuildCategory.BlocksAndTraps,
                Name = "Функциональная Дверь",
                ShortDesc = $"Открывается и закрывается игроками по кнопке [{BaseDefensePlugin.InteractDoorKey.Value}]",
                Cost = BaseDefensePlugin.CostDoorBlock.Value,
                MaxHealth = 1600f,
                BaseArmor = 20f,
                MaxLimitPerPlayer = 0,
                IsBlockOrTrap = true,
                IconAddress = "RoR2/Base/TreasureCache/texRustedKeyIcon.png",
                ThemeColor = new Color(0.25f, 0.78f, 0.88f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.RampBlock,
                Category = BuildCategory.BlocksAndTraps,
                Name = "Уклон (Рампа 30°)",
                ShortDesc = "Наклонный пандус для подъема на возвышенности и крыши укреплений",
                Cost = BaseDefensePlugin.CostRampBlock.Value,
                MaxHealth = 1200f,
                BaseArmor = 15f,
                MaxLimitPerPlayer = 0,
                IsBlockOrTrap = true,
                IconAddress = "RoR2/Base/Feather/texFeatherIcon.png",
                ThemeColor = new Color(0.60f, 0.65f, 0.55f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.HeavyBlock,
                Category = BuildCategory.BlocksAndTraps,
                Name = "Силовой Блок-Бастион",
                ShortDesc = "Массивный бронированный куб с огромным запасом прочности против Титанов",
                Cost = BaseDefensePlugin.CostHeavyBlock.Value,
                MaxHealth = 2800f,
                BaseArmor = 35f,
                MaxLimitPerPlayer = 0,
                IsBlockOrTrap = true,
                IconAddress = "RoR2/Base/Knurl/texKnurlIcon.png",
                ThemeColor = new Color(0.35f, 0.50f, 0.85f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.SpikePlatform,
                Category = BuildCategory.BlocksAndTraps,
                Name = "Шипастая Платформа",
                ShortDesc = "Мало ХП, но наносит периодический урон и кровотечение врагам, стоящим на ней",
                Cost = BaseDefensePlugin.CostSpikePlatform.Value,
                MaxHealth = 380f,
                BaseDamage = 18f,
                BaseArmor = 5f,
                MaxLimitPerPlayer = 0,
                IsBlockOrTrap = true,
                IconAddress = "RoR2/Base/BleedOnHit/texTriTipIcon.png",
                ThemeColor = new Color(0.85f, 0.25f, 0.25f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.SpringTrap,
                Category = BuildCategory.BlocksAndTraps,
                Name = "Пружинная Установка (5x)",
                ShortDesc = "5 раз высоко подбрасывает наступивших врагов в воздух, после чего разрушается",
                Cost = BaseDefensePlugin.CostSpringTrap.Value,
                MaxHealth = 420f,
                BaseDamage = 15f,
                BaseArmor = 5f,
                MaxLimitPerPlayer = 0,
                IsBlockOrTrap = true,
                IconAddress = "RoR2/Base/JumpBoost/texWaxQuailIcon.png",
                ThemeColor = new Color(0.95f, 0.65f, 0.15f)
            });

            // ==================== КАТЕГОРИЯ 2: БОЕВЫЕ ТУРЕЛИ ====================
            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.MachineGunTurret,
                Category = BuildCategory.Turrets,
                Name = "Пулеметная Турель",
                ShortDesc = "Скорострельная турель базовой огневой поддержки",
                Cost = BaseDefensePlugin.CostMachineGunTurret.Value,
                MaxHealth = 480f,
                BaseDamage = 14f,
                BaseArmor = 10f,
                MaxLimitPerPlayer = BaseDefensePlugin.LimitPerTurretType.Value,
                IsBlockOrTrap = false,
                IconAddress = "RoR2/Base/Syringe/texSyringeIcon.png",
                ThemeColor = new Color(0.3f, 0.85f, 0.4f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.FlameTurret,
                Category = BuildCategory.Turrets,
                Name = "Турель-Огнемет",
                ShortDesc = "Сжигает группы врагов и 'Рой' на ближней дистанции + Поджог",
                Cost = BaseDefensePlugin.CostFlameTurret.Value,
                MaxHealth = 700f,
                BaseDamage = 13f,
                BaseArmor = 20f,
                MaxLimitPerPlayer = BaseDefensePlugin.LimitPerTurretType.Value,
                IsBlockOrTrap = false,
                IconAddress = "RoR2/Base/IgniteOnKill/texGasolineIcon.png",
                ThemeColor = new Color(1.0f, 0.45f, 0.1f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.TeslaTurret,
                Category = BuildCategory.Turrets,
                Name = "Электро-Турель (Дуга)",
                ShortDesc = "Стреляет только цепным разрядом, отскакивающим по 5 врагам",
                Cost = BaseDefensePlugin.CostTeslaTurret.Value,
                MaxHealth = 500f,
                BaseDamage = 17f,
                BaseArmor = 10f,
                MaxLimitPerPlayer = BaseDefensePlugin.LimitPerTurretType.Value,
                IsBlockOrTrap = false,
                IconAddress = "RoR2/Base/ChainLightning/texUkuleleIcon.png",
                ThemeColor = new Color(0.2f, 0.85f, 1.0f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.RocketTurret,
                Category = BuildCategory.Turrets,
                Name = "Ракетная Турель",
                ShortDesc = "Стреляет только самонаводящимися ракетами с взрывным AoE-уроном",
                Cost = BaseDefensePlugin.CostRocketTurret.Value,
                MaxHealth = 540f,
                BaseDamage = 22f,
                BaseArmor = 15f,
                MaxLimitPerPlayer = BaseDefensePlugin.LimitPerTurretType.Value,
                IsBlockOrTrap = false,
                IconAddress = "RoR2/Base/Missile/texAtgMissileIcon.png",
                ThemeColor = new Color(0.95f, 0.75f, 0.2f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.LaserTurret,
                Category = BuildCategory.Turrets,
                Name = "Лазерная Турель",
                ShortDesc = "Стреляет только мощным энергетическим лучом по самым толстым врагам",
                Cost = BaseDefensePlugin.CostLaserTurret.Value,
                MaxHealth = 500f,
                BaseDamage = 26f,
                BaseArmor = 10f,
                MaxLimitPerPlayer = BaseDefensePlugin.LimitPerTurretType.Value,
                IsBlockOrTrap = false,
                IconAddress = "RoR2/Base/LaserTurbine/texLaserTurbineIcon.png",
                ThemeColor = new Color(1.0f, 0.2f, 0.35f)
            });

            // ==================== КАТЕГОРИЯ 3: СТАНЦИИ И МЕХАНИЗМЫ (ЛИМИТ 2 ШТ.) ====================
            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.HealStation,
                Category = BuildCategory.Stations,
                Name = "Хил-Станция (Игроки)",
                ShortDesc = "Аура 12м: регенерирует здоровье ТОЛЬКО игрокам (не лечит постройки)",
                Cost = BaseDefensePlugin.CostHealStation.Value,
                MaxHealth = 550f,
                BaseArmor = 15f,
                MaxLimitPerPlayer = BaseDefensePlugin.LimitHealStation.Value,
                IsBlockOrTrap = false,
                IconAddress = "RoR2/Base/Mushroom/texMushroomIcon.png",
                ThemeColor = new Color(0.25f, 1.0f, 0.45f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.RepairStation,
                Category = BuildCategory.Stations,
                Name = "Ремонтная Станция (База)",
                ShortDesc = "Аура 14м: плавно чинит укрепительные блоки и турели (не лечит игроков)",
                Cost = BaseDefensePlugin.CostRepairStation.Value,
                MaxHealth = 550f,
                BaseArmor = 15f,
                MaxLimitPerPlayer = BaseDefensePlugin.LimitRepairStation.Value,
                IsBlockOrTrap = false,
                IconAddress = "RoR2/Base/Medkit/texMedkitIcon.png",
                ThemeColor = new Color(0.20f, 0.65f, 1.0f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.MoneyGenerator,
                Category = BuildCategory.Stations,
                Name = "Денежный Генератор",
                ShortDesc = "Выдает золото игрокам, которые стоят внутри рабочей зоны (8м)",
                Cost = BaseDefensePlugin.CostMoneyGen.Value,
                MaxHealth = 420f,
                BaseArmor = 5f,
                MaxLimitPerPlayer = BaseDefensePlugin.LimitMoneyGenerator.Value,
                IsBlockOrTrap = false,
                IconAddress = "RoR2/Base/BonusGoldPackOnKill/texTomeIcon.png",
                ThemeColor = new Color(1.0f, 0.85f, 0.15f)
            });

            Definitions.Add(new BuildableDef
            {
                Id = BuildableId.DroneStation,
                Category = BuildCategory.Stations,
                Name = "Дрон-Станция",
                ShortDesc = "Каждые 10 сек выпускает боевого дрона с малым запасом ХП (до 3 шт.)",
                Cost = BaseDefensePlugin.CostDroneStation.Value,
                MaxHealth = 600f,
                BaseDamage = 12f,
                BaseArmor = 15f,
                MaxLimitPerPlayer = BaseDefensePlugin.LimitDroneStation.Value,
                IsBlockOrTrap = false,
                IconAddress = "RoR2/Base/DroneBackup/texStrikeDroneIcon.png",
                ThemeColor = new Color(0.4f, 0.7f, 0.9f)
            });
        }

        public static void UpdateFromConfig()
        {
            foreach (var d in Definitions)
            {
                switch (d.Id)
                {
                    case BuildableId.LightBarricade: d.Cost = BaseDefensePlugin.CostLightBarricade.Value; break;
                    case BuildableId.WallBlock: d.Cost = BaseDefensePlugin.CostWallBlock.Value; break;
                    case BuildableId.LongWallBlock: d.Cost = BaseDefensePlugin.CostLongWallBlock.Value; break;
                    case BuildableId.WindowBlock: d.Cost = BaseDefensePlugin.CostWindowBlock.Value; break;
                    case BuildableId.DoorBlock: d.Cost = BaseDefensePlugin.CostDoorBlock.Value; break;
                    case BuildableId.RampBlock: d.Cost = BaseDefensePlugin.CostRampBlock.Value; break;
                    case BuildableId.HeavyBlock: d.Cost = BaseDefensePlugin.CostHeavyBlock.Value; break;
                    case BuildableId.SpikePlatform: d.Cost = BaseDefensePlugin.CostSpikePlatform.Value; break;
                    case BuildableId.SpringTrap: d.Cost = BaseDefensePlugin.CostSpringTrap.Value; break;

                    case BuildableId.MachineGunTurret:
                        d.Cost = BaseDefensePlugin.CostMachineGunTurret.Value;
                        d.MaxLimitPerPlayer = BaseDefensePlugin.LimitPerTurretType.Value;
                        break;
                    case BuildableId.FlameTurret:
                        d.Cost = BaseDefensePlugin.CostFlameTurret.Value;
                        d.MaxLimitPerPlayer = BaseDefensePlugin.LimitPerTurretType.Value;
                        break;
                    case BuildableId.TeslaTurret:
                        d.Cost = BaseDefensePlugin.CostTeslaTurret.Value;
                        d.MaxLimitPerPlayer = BaseDefensePlugin.LimitPerTurretType.Value;
                        break;
                    case BuildableId.RocketTurret:
                        d.Cost = BaseDefensePlugin.CostRocketTurret.Value;
                        d.MaxLimitPerPlayer = BaseDefensePlugin.LimitPerTurretType.Value;
                        break;
                    case BuildableId.LaserTurret:
                        d.Cost = BaseDefensePlugin.CostLaserTurret.Value;
                        d.MaxLimitPerPlayer = BaseDefensePlugin.LimitPerTurretType.Value;
                        break;

                    case BuildableId.HealStation:
                        d.Cost = BaseDefensePlugin.CostHealStation.Value;
                        d.MaxLimitPerPlayer = BaseDefensePlugin.LimitHealStation.Value;
                        break;
                    case BuildableId.RepairStation:
                        d.Cost = BaseDefensePlugin.CostRepairStation.Value;
                        d.MaxLimitPerPlayer = BaseDefensePlugin.LimitRepairStation.Value;
                        break;
                    case BuildableId.MoneyGenerator:
                        d.Cost = BaseDefensePlugin.CostMoneyGen.Value;
                        d.MaxLimitPerPlayer = BaseDefensePlugin.LimitMoneyGenerator.Value;
                        break;
                    case BuildableId.DroneStation:
                        d.Cost = BaseDefensePlugin.CostDroneStation.Value;
                        d.MaxLimitPerPlayer = BaseDefensePlugin.LimitDroneStation.Value;
                        break;
                }
            }
        }

        public static BuildableDef GetDef(BuildableId id) => Definitions.Find(d => d.Id == id);

        public static Texture2D GetOrLoadIcon(BuildableDef def)
        {
            if (def.CachedIcon != null) return def.CachedIcon;
            try
            {
                var tex = Addressables.LoadAssetAsync<Texture2D>(def.IconAddress).WaitForCompletion();
                if (tex != null)
                {
                    def.CachedIcon = tex;
                    return tex;
                }
            }
            catch { }

            Texture2D fallback = new Texture2D(32, 32);
            Color[] cols = new Color[32 * 32];
            for (int i = 0; i < cols.Length; i++) cols[i] = def.ThemeColor;
            fallback.SetPixels(cols);
            fallback.Apply();
            def.CachedIcon = fallback;
            return fallback;
        }
    }

    public static class VanillaVisualBuilder
    {
        private static Shader cachedSafeShader;

        public static Shader GetSafeRoR2Shader()
        {
            if (cachedSafeShader != null) return cachedSafeShader;
            try
            {
                cachedSafeShader = Addressables.LoadAssetAsync<Shader>("RoR2/Base/Shaders/hgStandard.shader").WaitForCompletion();
            }
            catch { }

            if (cachedSafeShader == null)
            {
                GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cachedSafeShader = temp.GetComponent<Renderer>().sharedMaterial.shader;
                Object.Destroy(temp);
            }
            return cachedSafeShader;
        }

        // ИСПРАВЛЕНИЕ ПУНКТОВ 4, 5 и 6:
        // Полностью отключает стандартный пулемет и ИИ турели Инженера, убирает капсулу CharacterMotor (чтобы блоки не съезжали)
        // и скрывает меш турели внутри блоков/станций!
        public static void StripTurretMechanicsAndHideMesh(GameObject bodyObj, bool disableVanillaGun)
        {
            if (bodyObj == null) return;

            CharacterBody body = bodyObj.GetComponent<CharacterBody>();
            if (body != null && body.master != null && disableVanillaGun)
            {
                foreach (var ai in body.master.GetComponents<BaseAI>())
                {
                    ai.enabled = false;
                    Object.Destroy(ai);
                }
                foreach (var driver in body.master.GetComponents<AISkillDriver>())
                {
                    driver.enabled = false;
                    Object.Destroy(driver);
                }
            }

            if (disableVanillaGun)
            {
                // Сбрасываем зажатую кнопку стрельбы в InputBankTest
                InputBankTest inputBank = bodyObj.GetComponent<InputBankTest>();
                if (inputBank != null)
                {
                    inputBank.skill1.PushState(false);
                    inputBank.skill2.PushState(false);
                }

                // Отключаем стандартный пулемет Гаусса в SkillLocator
                SkillLocator skillLoc = bodyObj.GetComponent<SkillLocator>();
                if (skillLoc != null)
                {
                    if (skillLoc.primary != null) skillLoc.primary.enabled = false;
                    if (skillLoc.secondary != null) skillLoc.secondary.enabled = false;
                    skillLoc.enabled = false;
                }

                // Принудительно переводим стейт-машину оружия в Idle, чтобы она никогда не стреляла пулеметом!
                foreach (var esm in bodyObj.GetComponents<EntityStateMachine>())
                {
                    if (esm.customName == "Weapon" || esm.customName == "Body")
                    {
                        esm.SetNextState(new Idle());
                    }
                }
            }
        }

        public static void DisableMotorAndSnapTransform(GameObject bodyObj, Vector3 exactPos, Quaternion exactRot)
        {
            if (bodyObj == null) return;

            if (bodyObj.TryGetComponent<CharacterMotor>(out var motor))
            {
                motor.enabled = false;
            }
            if (bodyObj.TryGetComponent<KinematicCharacterController.KinematicCharacterMotor>(out var kMotor))
            {
                kMotor.enabled = false;
            }
            if (bodyObj.TryGetComponent<CapsuleCollider>(out var cap))
            {
                cap.enabled = false;
            }

            bodyObj.transform.position = exactPos;
            bodyObj.transform.rotation = exactRot;
        }

        public static void ApplyEnergyCoreVisuals(GameObject coreBodyObj)
        {
            if (coreBodyObj == null) return;
            if (coreBodyObj.transform.Find("CoreCustomVisualRoot") != null) return;

            HideVanillaTurretMesh(coreBodyObj);
            DisableMotorAndSnapTransform(coreBodyObj, coreBodyObj.transform.position, Quaternion.identity);

            GameObject root = new GameObject("CoreCustomVisualRoot");
            root.transform.SetParent(coreBodyObj.transform, false);
            root.transform.localPosition = Vector3.zero;

            Material coreMat = CreateGlowingMaterial(new Color(0.1f, 0.85f, 1.0f), 2.5f);
            Material baseMat = CreateSolidMaterial(new Color(0.18f, 0.22f, 0.30f));

            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(pedestal.GetComponent<Collider>());
            pedestal.transform.SetParent(root.transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            pedestal.transform.localScale = new Vector3(3.4f, 0.4f, 3.4f);
            pedestal.GetComponent<Renderer>().sharedMaterial = baseMat;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(root.transform, false);
            sphere.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            sphere.transform.localScale = new Vector3(2.4f, 2.4f, 2.4f);
            sphere.GetComponent<Renderer>().sharedMaterial = coreMat;

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(pillar.GetComponent<Collider>());
                pillar.transform.SetParent(root.transform, false);
                pillar.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1.4f, 1.8f, Mathf.Sin(angle) * 1.4f);
                pillar.transform.localScale = new Vector3(0.35f, 2.6f, 0.35f);
                pillar.GetComponent<Renderer>().sharedMaterial = baseMat;
            }

            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.2f, 0.85f, 1.0f);
            light.range = 18f;
            light.intensity = 4f;
        }

        public static void ApplyStructureVisualsAndColliders(GameObject bodyObj, BuildableDef def, Vector3 exactPos, Quaternion exactRot)
        {
            if (bodyObj == null || def == null) return;

            // Фиксируем точную позицию и поворот на поверхности (Пункт 4)
            DisableMotorAndSnapTransform(bodyObj, exactPos, exactRot);

            if (bodyObj.transform.Find("CustomStructureVisual") != null) return;

            GameObject root = new GameObject("CustomStructureVisual");
            root.transform.SetParent(bodyObj.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            if (def.Category == BuildCategory.BlocksAndTraps)
            {
                // Пункт 5: Полностью скрываем модель турели внутри строительных блоков!
                HideVanillaTurretMesh(bodyObj);
                BuildBlockOrTrapGeometry(bodyObj, root.transform, def, false);
            }
            else if (def.Category == BuildCategory.Stations)
            {
                HideVanillaTurretMesh(bodyObj);
                BuildStationGeometry(root.transform, def, false);
            }
            else
            {
                // Боевые турели: оставляем модель башни, но ставим сверху тактический индикатор типа оружия
                GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(badge.GetComponent<Collider>());
                badge.transform.SetParent(root.transform, false);
                badge.transform.localPosition = new Vector3(0f, 2.15f, 0f);
                badge.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                badge.GetComponent<Renderer>().sharedMaterial = CreateGlowingMaterial(def.ThemeColor, 2.2f);

                TintVanillaRenderers(bodyObj, def.ThemeColor);
            }
        }

        // Универсальный конструктор геометрии для всех 9 типов строительных блоков (используется и для реальной постройки, и для зеленой голограммы!)
        public static void BuildBlockOrTrapGeometry(GameObject bodyObj, Transform visualRoot, BuildableDef def, bool isGhostPreview)
        {
            Material mainMat = CreateSolidMaterial(def.ThemeColor);
            Material accentMat = CreateGlowingMaterial(def.ThemeColor, 1.6f);

            switch (def.Id)
            {
                case BuildableId.LightBarricade:
                    CreateBoxPart(visualRoot, new Vector3(0f, 0.7f, 0f), new Vector3(3.0f, 1.4f, 0.7f), Quaternion.identity, mainMat);
                    CreateBoxPart(visualRoot, new Vector3(0f, 1.25f, 0f), new Vector3(3.05f, 0.18f, 0.75f), Quaternion.identity, accentMat);
                    if (!isGhostPreview) AddWorldColliderAndHurtBox(bodyObj, new Vector3(0f, 0.7f, 0f), new Vector3(3.0f, 1.4f, 0.7f), Quaternion.identity);
                    break;

                case BuildableId.WallBlock:
                    CreateBoxPart(visualRoot, new Vector3(0f, 1.6f, 0f), new Vector3(3.2f, 3.2f, 0.7f), Quaternion.identity, mainMat);
                    CreateBoxPart(visualRoot, new Vector3(0f, 2.8f, 0f), new Vector3(3.25f, 0.2f, 0.75f), Quaternion.identity, accentMat);
                    if (!isGhostPreview) AddWorldColliderAndHurtBox(bodyObj, new Vector3(0f, 1.6f, 0f), new Vector3(3.2f, 3.2f, 0.7f), Quaternion.identity);
                    break;

                case BuildableId.LongWallBlock:
                    CreateBoxPart(visualRoot, new Vector3(0f, 1.6f, 0f), new Vector3(7.0f, 3.2f, 0.75f), Quaternion.identity, mainMat);
                    CreateBoxPart(visualRoot, new Vector3(0f, 2.8f, 0f), new Vector3(7.05f, 0.22f, 0.8f), Quaternion.identity, accentMat);
                    if (!isGhostPreview) AddWorldColliderAndHurtBox(bodyObj, new Vector3(0f, 1.6f, 0f), new Vector3(7.0f, 3.2f, 0.75f), Quaternion.identity);
                    break;

                case BuildableId.WindowBlock:
                    // Нижняя, верхняя и боковые части стены с открытым окном посередине для стрельбы
                    CreateBoxPart(visualRoot, new Vector3(0f, 0.6f, 0f), new Vector3(3.4f, 1.2f, 0.7f), Quaternion.identity, mainMat);
                    CreateBoxPart(visualRoot, new Vector3(0f, 2.8f, 0f), new Vector3(3.4f, 0.8f, 0.7f), Quaternion.identity, mainMat);
                    CreateBoxPart(visualRoot, new Vector3(-1.25f, 1.8f, 0f), new Vector3(0.9f, 1.2f, 0.7f), Quaternion.identity, mainMat);
                    CreateBoxPart(visualRoot, new Vector3(1.25f, 1.8f, 0f), new Vector3(0.9f, 1.2f, 0.7f), Quaternion.identity, mainMat);
                    if (!isGhostPreview)
                    {
                        AddWorldColliderAndHurtBox(bodyObj, new Vector3(0f, 0.6f, 0f), new Vector3(3.4f, 1.2f, 0.7f), Quaternion.identity);
                        AddExtraWorldCollider(bodyObj, new Vector3(0f, 2.8f, 0f), new Vector3(3.4f, 0.8f, 0.7f), Quaternion.identity);
                        AddExtraWorldCollider(bodyObj, new Vector3(-1.25f, 1.8f, 0f), new Vector3(0.9f, 1.2f, 0.7f), Quaternion.identity);
                        AddExtraWorldCollider(bodyObj, new Vector3(1.25f, 1.8f, 0f), new Vector3(0.9f, 1.2f, 0.7f), Quaternion.identity);
                    }
                    break;

                case BuildableId.DoorBlock:
                    // Рама двери + подвижная створка DoorLeaf
                    CreateBoxPart(visualRoot, new Vector3(-1.45f, 1.6f, 0f), new Vector3(0.5f, 3.2f, 0.75f), Quaternion.identity, mainMat);
                    CreateBoxPart(visualRoot, new Vector3(1.45f, 1.6f, 0f), new Vector3(0.5f, 3.2f, 0.75f), Quaternion.identity, mainMat);
                    CreateBoxPart(visualRoot, new Vector3(0f, 3.0f, 0f), new Vector3(3.4f, 0.4f, 0.75f), Quaternion.identity, mainMat);

                    GameObject doorLeaf = CreateBoxPart(visualRoot, new Vector3(0f, 1.4f, 0f), new Vector3(2.4f, 2.8f, 0.35f), Quaternion.identity, accentMat);
                    doorLeaf.name = "DoorLeafMesh";

                    if (!isGhostPreview)
                    {
                        GameObject blocker = AddWorldColliderAndHurtBox(bodyObj, new Vector3(0f, 1.6f, 0f), new Vector3(3.4f, 3.2f, 0.7f), Quaternion.identity);
                        blocker.name = "DoorWorldBlocker";
                    }
                    break;

                case BuildableId.RampBlock:
                    Quaternion rampRot = Quaternion.Euler(-28f, 0f, 0f);
                    CreateBoxPart(visualRoot, new Vector3(0f, 1.05f, 0f), new Vector3(3.2f, 0.45f, 4.2f), rampRot, mainMat);
                    if (!isGhostPreview) AddWorldColliderAndHurtBox(bodyObj, new Vector3(0f, 1.05f, 0f), new Vector3(3.2f, 0.45f, 4.2f), rampRot);
                    break;

                case BuildableId.HeavyBlock:
                    CreateBoxPart(visualRoot, new Vector3(0f, 1.3f, 0f), new Vector3(2.6f, 2.6f, 2.6f), Quaternion.identity, mainMat);
                    CreateBoxPart(visualRoot, new Vector3(0f, 2.4f, 0f), new Vector3(2.68f, 0.25f, 2.68f), Quaternion.identity, accentMat);
                    if (!isGhostPreview) AddWorldColliderAndHurtBox(bodyObj, new Vector3(0f, 1.3f, 0f), new Vector3(2.6f, 2.6f, 2.6f), Quaternion.identity);
                    break;

                case BuildableId.SpikePlatform:
                    CreateBoxPart(visualRoot, new Vector3(0f, 0.12f, 0f), new Vector3(3.4f, 0.24f, 3.4f), Quaternion.identity, mainMat);
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            CreateBoxPart(visualRoot, new Vector3(x * 1.0f, 0.4f, z * 1.0f), new Vector3(0.25f, 0.55f, 0.25f), Quaternion.Euler(0f, 45f, 0f), accentMat);
                        }
                    }
                    if (!isGhostPreview) AddWorldColliderAndHurtBox(bodyObj, new Vector3(0f, 0.15f, 0f), new Vector3(3.4f, 0.3f, 3.4f), Quaternion.identity);
                    break;

                case BuildableId.SpringTrap:
                    CreateBoxPart(visualRoot, new Vector3(0f, 0.15f, 0f), new Vector3(2.8f, 0.3f, 2.8f), Quaternion.identity, mainMat);
                    GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Object.Destroy(pad.GetComponent<Collider>());
                    pad.transform.SetParent(visualRoot, false);
                    pad.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                    pad.transform.localScale = new Vector3(2.2f, 0.15f, 2.2f);
                    pad.GetComponent<Renderer>().sharedMaterial = accentMat;
                    if (!isGhostPreview) AddWorldColliderAndHurtBox(bodyObj, new Vector3(0f, 0.2f, 0f), new Vector3(2.8f, 0.4f, 2.8f), Quaternion.identity);
                    break;
            }
        }

        public static void BuildStationGeometry(Transform visualRoot, BuildableDef def, bool isGhostPreview)
        {
            GameObject baseCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(baseCyl.GetComponent<Collider>());
            baseCyl.transform.SetParent(visualRoot, false);
            baseCyl.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            baseCyl.transform.localScale = new Vector3(1.8f, 0.3f, 1.8f);
            baseCyl.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial(new Color(0.25f, 0.28f, 0.32f));

            GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(pylon.GetComponent<Collider>());
            pylon.transform.SetParent(visualRoot, false);
            pylon.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            pylon.transform.localScale = new Vector3(0.65f, 1.2f, 0.65f);
            pylon.GetComponent<Renderer>().sharedMaterial = CreateGlowingMaterial(def.ThemeColor, 1.8f);

            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(orb.GetComponent<Collider>());
            orb.transform.SetParent(visualRoot, false);
            orb.transform.localPosition = new Vector3(0f, 2.75f, 0f);
            orb.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            orb.GetComponent<Renderer>().sharedMaterial = CreateGlowingMaterial(def.ThemeColor, 2.2f);

            if (!isGhostPreview)
            {
                if (def.Id == BuildableId.MoneyGenerator)
                    CreateGroundRadiusIndicator(visualRoot, 8f, new Color(1.0f, 0.85f, 0.1f, 0.35f));
                else if (def.Id == BuildableId.HealStation)
                    CreateGroundRadiusIndicator(visualRoot, 12f, new Color(0.2f, 1.0f, 0.4f, 0.25f));
                else if (def.Id == BuildableId.RepairStation)
                    CreateGroundRadiusIndicator(visualRoot, 14f, new Color(0.2f, 0.65f, 1.0f, 0.25f));
            }
        }

        private static GameObject CreateBoxPart(Transform parent, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(cube.GetComponent<Collider>());
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localRotation = localRot;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().sharedMaterial = mat;
            return cube;
        }

        private static GameObject AddWorldColliderAndHurtBox(GameObject bodyObj, Vector3 localCenter, Vector3 size, Quaternion localRot)
        {
            GameObject blockerObj = new GameObject("BarricadeWorldBlocker");
            blockerObj.transform.SetParent(bodyObj.transform, false);
            blockerObj.transform.localPosition = localCenter;
            blockerObj.transform.localRotation = localRot;
            blockerObj.layer = LayerIndex.world.intVal;

            BoxCollider worldCol = blockerObj.AddComponent<BoxCollider>();
            worldCol.size = size;
            worldCol.center = Vector3.zero;

            ExpandHurtBoxForBarricade(bodyObj, size);
            return blockerObj;
        }

        private static void AddExtraWorldCollider(GameObject bodyObj, Vector3 localCenter, Vector3 size, Quaternion localRot)
        {
            GameObject blockerObj = new GameObject("BarricadeExtraBlocker");
            blockerObj.transform.SetParent(bodyObj.transform, false);
            blockerObj.transform.localPosition = localCenter;
            blockerObj.transform.localRotation = localRot;
            blockerObj.layer = LayerIndex.world.intVal;

            BoxCollider worldCol = blockerObj.AddComponent<BoxCollider>();
            worldCol.size = size;
            worldCol.center = Vector3.zero;
        }

        private static void HideVanillaTurretMesh(GameObject bodyObj)
        {
            ModelLocator ml = bodyObj.GetComponent<ModelLocator>();
            if (ml != null && ml.modelTransform != null)
            {
                foreach (var r in ml.modelTransform.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = false;
                }
            }
        }

        private static void TintVanillaRenderers(GameObject bodyObj, Color tint)
        {
            ModelLocator ml = bodyObj.GetComponent<ModelLocator>();
            if (ml != null && ml.modelTransform != null)
            {
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                foreach (var r in ml.modelTransform.GetComponentsInChildren<Renderer>(true))
                {
                    r.GetPropertyBlock(propBlock);
                    propBlock.SetColor("_Color", Color.Lerp(Color.white, tint, 0.65f));
                    propBlock.SetColor("_EmColor", tint * 1.5f);
                    r.SetPropertyBlock(propBlock);
                }
            }
        }

        private static void ExpandHurtBoxForBarricade(GameObject bodyObj, Vector3 size)
        {
            ModelLocator ml = bodyObj.GetComponent<ModelLocator>();
            if (ml != null && ml.modelTransform != null)
            {
                foreach (var hb in ml.modelTransform.GetComponentsInChildren<HurtBox>(true))
                {
                    BoxCollider box = hb.GetComponent<BoxCollider>();
                    if (box != null) box.size = size * 1.15f;
                    SphereCollider sphere = hb.GetComponent<SphereCollider>();
                    if (sphere != null) sphere.radius = Mathf.Max(size.x, size.y) * 0.65f;
                }
            }
        }

        private static void CreateGroundRadiusIndicator(Transform parent, float radius, Color color)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            ring.transform.localScale = new Vector3(radius * 2f, 0.015f, radius * 2f);
            ring.GetComponent<Renderer>().sharedMaterial = CreateGlowingMaterial(color, 1.1f);
        }

        public static Material CreateSolidMaterial(Color color)
        {
            Material mat = new Material(GetSafeRoR2Shader());
            mat.color = color;
            return mat;
        }

        public static Material CreateGlowingMaterial(Color color, float emissionMultiplier)
        {
            Material mat = new Material(GetSafeRoR2Shader());
            mat.color = color;
            if (mat.HasProperty("_EmColor")) mat.SetColor("_EmColor", color * emissionMultiplier);
            if (mat.HasProperty("_EmPower")) mat.SetFloat("_EmPower", emissionMultiplier);
            return mat;
        }
    }
}