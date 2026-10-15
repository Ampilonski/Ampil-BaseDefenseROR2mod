using System.Collections;
using System.Collections.Generic;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RoR2BaseDefense
{
    public enum EnemyArchetype
    {
        Swarm,      // Рой: большая группа легких врагов, атакуют и игроков и Ядро, в приоритете игроки
        Titan,      // Титаны: толстые медленные враги, строго идут ломать Ядро, полностью игнорируют игроков
        Demolition, // Демонтаж: нацелены на уничтожение построек игроков (x2.5 урон по ним) и Ядра
        Basic       // Базовый: обычные враги, приоритет 50 на 50 (Игрок или Ядро)
    }

    public class WaveEnemyMarker : MonoBehaviour
    {
        public EnemyArchetype Archetype;
        private BaseAI ai;
        private CharacterMaster master;
        private float retargetTimer;
        private bool basicTargetCoreAssigned;
        private bool basicTargetsCore;

        private void Awake()
        {
            master = GetComponent<CharacterMaster>();
            ai = GetComponent<BaseAI>();
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active || ai == null || BaseDefenseManager.Instance == null) return;

            retargetTimer -= Time.fixedDeltaTime;
            if (retargetTimer <= 0f)
            {
                retargetTimer = 0.8f;
                UpdateTargetPriority();
            }
        }

        private void UpdateTargetPriority()
        {
            GameObject coreObj = BaseDefenseManager.Instance.EnergyCoreBodyObject;
            if (coreObj == null) return;

            Vector3 myPos = (ai.body != null) ? ai.body.corePosition : transform.position;

            switch (Archetype)
            {
                case EnemyArchetype.Titan:
                    ai.neverRetaliateFriendlies = true;
                    SetAITarget(coreObj);
                    break;

                case EnemyArchetype.Demolition:
                    GameObject nearestStructure = BaseDefenseManager.Instance.FindNearestStructure(myPos, 120f);
                    SetAITarget(nearestStructure != null ? nearestStructure : coreObj);
                    break;

                case EnemyArchetype.Swarm:
                    GameObject nearestPlayer = BaseDefenseManager.Instance.FindNearestPlayer(myPos);
                    SetAITarget(nearestPlayer != null ? nearestPlayer : coreObj);
                    break;

                case EnemyArchetype.Basic:
                    if (!basicTargetCoreAssigned)
                    {
                        basicTargetCoreAssigned = true;
                        basicTargetsCore = Random.value >= 0.5f;
                    }
                    if (basicTargetsCore)
                    {
                        SetAITarget(coreObj);
                    }
                    else
                    {
                        GameObject p = BaseDefenseManager.Instance.FindNearestPlayer(myPos);
                        SetAITarget(p != null ? p : coreObj);
                    }
                    break;
            }
        }

        private void SetAITarget(GameObject targetBodyObj)
        {
            if (targetBodyObj == null || ai.currentEnemy == null) return;
            if (ai.currentEnemy.gameObject != targetBodyObj)
            {
                ai.currentEnemy.gameObject = targetBodyObj;
                CharacterBody targetBody = targetBodyObj.GetComponent<CharacterBody>();
                if (targetBody != null && targetBody.mainHurtBox != null)
                {
                    ai.currentEnemy.bestHurtBox = targetBody.mainHurtBox;
                }
            }
        }
    }

    public class CoreShieldController : MonoBehaviour
    {
        public float MaxCustomShield;
        public float CurrentCustomShield;
        public float RechargeDelay = 30f;
        public float RechargeTimer;
        public bool IsBroken;

        private CharacterBody body;

        public void Init(float maxShield, float delay)
        {
            body = GetComponent<CharacterBody>();
            MaxCustomShield = maxShield;
            CurrentCustomShield = maxShield;
            RechargeDelay = delay;
            IsBroken = false;
            RechargeTimer = 0f;
        }

        public void UpgradeShieldForWave(float bonusShield)
        {
            MaxCustomShield += bonusShield;
            if (!IsBroken)
            {
                CurrentCustomShield = Mathf.Min(MaxCustomShield, CurrentCustomShield + bonusShield);
            }
        }

        public void ProcessIncomingDamage(DamageInfo damageInfo)
        {
            if (damageInfo.rejected || damageInfo.damage <= 0f) return;

            if (damageInfo.attacker != null)
            {
                var attackerTeam = TeamComponent.GetObjectTeam(damageInfo.attacker);
                if (attackerTeam == TeamIndex.Player)
                {
                    damageInfo.damage = 0f;
                    damageInfo.rejected = true;
                    return;
                }
            }

            if (!IsBroken && CurrentCustomShield > 0f)
            {
                if (CurrentCustomShield >= damageInfo.damage)
                {
                    CurrentCustomShield -= damageInfo.damage;
                    damageInfo.damage = 0f;
                    damageInfo.rejected = true;

                    EffectManager.SimpleImpactEffect(
                        LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/ImpactShield"),
                        damageInfo.position,
                        Vector3.up,
                        true
                    );
                }
                else
                {
                    damageInfo.damage -= CurrentCustomShield;
                    CurrentCustomShield = 0f;
                    IsBroken = true;
                    RechargeTimer = RechargeDelay;

                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = $"<color=#FF4444>[ТРЕВОГА] Щит Энергетического Ядра пробит! Перезарядка щита: {RechargeDelay:0} сек!</color>"
                    });
                }
            }
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active) return;

            if (IsBroken)
            {
                RechargeTimer -= Time.fixedDeltaTime;
                if (RechargeTimer <= 0f)
                {
                    IsBroken = false;
                    RechargeTimer = 0f;
                    CurrentCustomShield = MaxCustomShield;

                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = "<color=#33DDFF>[ЯДРО] Энергетический щит полностью перезаряжен!</color>"
                    });
                }
            }
        }
    }

    public class BaseDefenseManager : MonoBehaviour
    {
        public static BaseDefenseManager Instance { get; private set; }
        public GameObject EnergyCoreBodyObject { get; private set; }
        public CoreShieldController CoreShield { get; private set; }
        public List<BuildableStructure> ActiveStructures = new List<BuildableStructure>();

        public int CurrentWave = 0;
        public int MaxWaves => BaseDefensePlugin.MaxWavesToWin.Value;
        public float PrepTimer = 0f;
        public bool IsWaveActive = false;

        private List<CharacterMaster> aliveWaveEnemies = new List<CharacterMaster>();
        private Vector3 mapCenterPosition;
        private float hudSyncTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeathGlobal;
            if (Instance == this) Instance = null;
        }

        private IEnumerator Start()
        {
            if (!NetworkServer.active) yield break;

            // Ждем полной загрузки карты и спавна всех игроков
            yield return new WaitForSeconds(2.5f);

            // Синхронизируем конфиг хоста со всеми клиентами при старте матча
            NetSyncLobbyConfig.CreateFromCurrentConfig().Send(NetworkDestination.Clients);

            DisableVanillaDirectors();
            mapCenterPosition = FindPlayableMapCenter();
            SpawnEnergyCore(mapCenterPosition);
            TeleportPlayersAroundCore(mapCenterPosition);

            GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;

            StartCoroutine(WaveLoopCoroutine());
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active) return;

            hudSyncTimer -= Time.fixedDeltaTime;
            if (hudSyncTimer <= 0f)
            {
                hudSyncTimer = 0.2f;
                BroadcastHudState();
            }
        }

        private void BroadcastHudState()
        {
            float coreHp = 0f;
            float coreMaxHp = BaseDefensePlugin.CoreBaseHealth.Value;
            float coreSh = 0f;
            float coreMaxSh = BaseDefensePlugin.CoreBaseShield.Value;
            float shTimer = 0f;
            bool shBroken = false;

            if (EnergyCoreBodyObject != null)
            {
                var hc = EnergyCoreBodyObject.GetComponent<HealthComponent>();
                if (hc != null)
                {
                    coreHp = hc.health;
                    coreMaxHp = hc.fullHealth;
                }
                if (CoreShield != null)
                {
                    coreSh = CoreShield.CurrentCustomShield;
                    coreMaxSh = CoreShield.MaxCustomShield;
                    shTimer = CoreShield.RechargeTimer;
                    shBroken = CoreShield.IsBroken;
                }
            }

            // Обновляем локальный стейт для Хоста/Одиночной игры напрямую + рассылаем Клиентам
            ClientHudState.CurrentWave = CurrentWave;
            ClientHudState.MaxWaves = MaxWaves;
            ClientHudState.AliveEnemiesCount = aliveWaveEnemies.Count;
            ClientHudState.WavePrepTimer = PrepTimer;
            ClientHudState.IsWaveActive = IsWaveActive;
            ClientHudState.CoreHealth = coreHp;
            ClientHudState.CoreMaxHealth = coreMaxHp;
            ClientHudState.CoreShield = coreSh;
            ClientHudState.CoreMaxShield = coreMaxSh;
            ClientHudState.ShieldRechargeTimer = shTimer;
            ClientHudState.IsShieldBroken = shBroken;

            var syncMsg = new NetSyncCoreAndWaveState
            {
                CurrentWave = CurrentWave,
                MaxWaves = MaxWaves,
                AliveEnemiesCount = aliveWaveEnemies.Count,
                WavePrepTimer = PrepTimer,
                IsWaveActive = IsWaveActive,
                CoreHealth = coreHp,
                CoreMaxHealth = coreMaxHp,
                CoreShield = coreSh,
                CoreMaxShield = coreMaxSh,
                ShieldRechargeTimer = shTimer,
                IsShieldBroken = shBroken
            };

            syncMsg.Send(NetworkDestination.Clients);
        }

        private void DisableVanillaDirectors()
        {
            // ИСПРАВЛЕНО: Копируем список в отдельный массив перед отключением,
            // так как director.enabled = false вызывает OnDisable() и удаляет элемент из CombatDirector.instancesList!
            var directorsCopy = new List<CombatDirector>(CombatDirector.instancesList);
            foreach (var director in directorsCopy)
            {
                if (director != null)
                {
                    director.monsterCredit = 0f;
                    director.enabled = false;
                }
            }
        }

        private Vector3 FindPlayableMapCenter()
        {
            NodeGraph groundNodes = SceneInfo.instance != null ? SceneInfo.instance.groundNodes : null;
            if (groundNodes != null && groundNodes.GetNodeCount() > 0)
            {
                int count = groundNodes.GetNodeCount();
                Vector3 sum = Vector3.zero;
                for (int i = 0; i < count; i++)
                {
                    groundNodes.GetNodePosition(new NodeGraph.NodeIndex(i), out Vector3 pos);
                    sum += pos;
                }
                Vector3 avgPos = sum / count;

                NodeGraph.NodeIndex closestIdx = groundNodes.FindClosestNode(avgPos, HullClassification.Golem);
                if (groundNodes.GetNodePosition(closestIdx, out Vector3 centerNodePos))
                {
                    if (Physics.Raycast(centerNodePos + Vector3.up * 8f, Vector3.down, out RaycastHit hit, 30f, LayerIndex.world.mask))
                    {
                        return hit.point;
                    }
                    return centerNodePos;
                }
            }
            return Vector3.zero;
        }

        private void SpawnEnergyCore(Vector3 position)
        {
            var turretMasterPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiTurretMaster.prefab").WaitForCompletion();
            var summon = new MasterSummon
            {
                masterPrefab = turretMasterPrefab,
                position = position,
                rotation = Quaternion.identity,
                teamIndexOverride = TeamIndex.Player,
                ignoreTeamMemberLimit = true
            };

            CharacterMaster coreMaster = summon.Perform();
            if (coreMaster != null)
            {
                foreach (var ai in coreMaster.GetComponents<BaseAI>()) Destroy(ai);

                CharacterBody coreBody = coreMaster.GetBody();
                if (coreBody != null)
                {
                    EnergyCoreBodyObject = coreBody.gameObject;
                    coreBody.baseNameToken = "ЭНЕРГЕТИЧЕСКОЕ ЯДРО";
                    coreBody.baseMaxHealth = BaseDefensePlugin.CoreBaseHealth.Value;
                    coreBody.levelMaxHealth = 0f;
                    coreBody.baseRegen = 0f;
                    coreBody.levelRegen = 0f;
                    coreBody.baseArmor = 15f;
                    coreBody.RecalculateStats();
                    coreBody.healthComponent.health = coreBody.maxHealth;

                    CoreShield = coreBody.gameObject.AddComponent<CoreShieldController>();
                    CoreShield.Init(BaseDefensePlugin.CoreBaseShield.Value, BaseDefensePlugin.CoreShieldRechargeDelay.Value);

                    VanillaVisualBuilder.ApplyEnergyCoreVisuals(coreBody.gameObject);

                    var netId = coreBody.GetComponent<NetworkIdentity>();
                    if (netId != null)
                    {
                        new NetSyncStructureMetadata(netId.netId, BuildableId.LightBarricade, 0, true).Send(NetworkDestination.Clients);
                    }
                }
            }
        }

        private void TeleportPlayersAroundCore(Vector3 center)
        {
            int count = PlayerCharacterMasterController.instances.Count;
            int idx = 0;
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc.master != null)
                {
                    pcmc.master.money = (uint)BaseDefensePlugin.StartingMoney.Value;
                    CharacterBody body = pcmc.master.GetBody();
                    if (body != null)
                    {
                        float angle = idx * (360f / Mathf.Max(1, count)) * Mathf.Deg2Rad;
                        Vector3 targetPos = center + new Vector3(Mathf.Cos(angle) * 6.5f, 2.0f, Mathf.Sin(angle) * 6.5f);
                        TeleportHelper.TeleportBody(body, targetPos);
                        idx++;
                    }
                }
            }
        }

        private IEnumerator WaveLoopCoroutine()
        {
            while (CurrentWave < MaxWaves)
            {
                IsWaveActive = false;
                PrepTimer = BaseDefensePlugin.TimeBetweenWaves.Value;

                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=#FFEE44>[БАЗА] Подготовка к Волне {CurrentWave + 1}/{MaxWaves}! Нажмите [{BaseDefensePlugin.ToggleBuildMenuKey.Value}] для постройки укреплений и турелей.</color>"
                });

                while (PrepTimer > 0f)
                {
                    PrepTimer -= Time.deltaTime;
                    yield return null;
                }

                CurrentWave++;
                IsWaveActive = true;
                UpgradeCoreStatsForWave(CurrentWave);

                yield return StartCoroutine(SpawnWaveEnemiesCoroutine(CurrentWave));

                while (aliveWaveEnemies.Count > 0)
                {
                    aliveWaveEnemies.RemoveAll(m => m == null || !m.hasBody || !m.GetBody().healthComponent.alive);
                    yield return new WaitForSeconds(0.5f);
                }

                IsWaveActive = false;
                RewardPlayersAfterWave(CurrentWave);
            }

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"<color=#00FF66>[ПОБЕДА!] Все {MaxWaves} волн отражены! Энергетическое Ядро в безопасности!</color>"
            });

            yield return new WaitForSeconds(3f);
            if (Run.instance != null)
            {
                Run.instance.BeginGameOver(RoR2Content.GameEndings.MainEnding);
            }
        }

        private void UpgradeCoreStatsForWave(int wave)
        {
            if (EnergyCoreBodyObject == null || wave <= 1) return;

            var body = EnergyCoreBodyObject.GetComponent<CharacterBody>();
            if (body != null)
            {
                body.baseMaxHealth += BaseDefensePlugin.CoreHealthPerWave.Value;
                body.RecalculateStats();
                body.healthComponent.HealFraction(BaseDefensePlugin.CoreHealBetweenWavesFraction.Value, default);
            }

            if (CoreShield != null)
            {
                CoreShield.UpgradeShieldForWave(BaseDefensePlugin.CoreShieldPerWave.Value);
            }
        }

        private void RewardPlayersAfterWave(int clearedWave)
        {
            uint goldReward = (uint)(BaseDefensePlugin.BaseWaveMoneyReward.Value + clearedWave * 30);
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc.master != null)
                {
                    pcmc.master.GiveMoney(goldReward);
                    if (!pcmc.master.hasBody || !pcmc.master.GetBody().healthComponent.alive)
                    {
                        pcmc.master.Respawn(mapCenterPosition + new Vector3(4f, 2f, 0f), Quaternion.identity);
                    }
                }
            }

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"<color=#55FF55>[ВОЛНА {clearedWave} ЗАЧИЩЕНА!] Каждый игрок получил +{goldReward}$ на развитие обороны!</color>"
            });
        }

        private IEnumerator SpawnWaveEnemiesCoroutine(int wave)
        {
            int playerCount = Mathf.Max(1, PlayerCharacterMasterController.instances.Count);
            int totalSpawns = Mathf.RoundToInt((10 + wave * 5) * (1f + (playerCount - 1) * 0.35f));

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = $"<color=#FF6600>=== ВОЛНА {wave}/{MaxWaves} НАЧАЛАСЬ! Врагов в волне: {totalSpawns} ===</color>"
            });

            for (int i = 0; i < totalSpawns; i++)
            {
                EnemyArchetype archetype = ChooseWaveArchetype(wave);

                if (archetype == EnemyArchetype.Swarm)
                {
                    int swarmPack = Random.Range(2, 4);
                    for (int s = 0; s < swarmPack; s++)
                    {
                        SpawnSingleWaveEnemy(EnemyArchetype.Swarm, wave);
                    }
                }
                else
                {
                    SpawnSingleWaveEnemy(archetype, wave);
                }

                yield return new WaitForSeconds(Mathf.Max(0.25f, 0.85f - wave * 0.03f));
            }
        }

        private EnemyArchetype ChooseWaveArchetype(int wave)
        {
            float r = Random.value;
            if (r < 0.38f) return EnemyArchetype.Swarm;
            if (r < 0.68f) return EnemyArchetype.Basic;
            if (r < 0.86f && wave >= 2) return EnemyArchetype.Demolition;
            return wave >= 3 ? EnemyArchetype.Titan : EnemyArchetype.Basic;
        }

        private void SpawnSingleWaveEnemy(EnemyArchetype archetype, int wave)
        {
            string address = archetype switch
            {
                EnemyArchetype.Swarm => Random.value > 0.45f
                    ? "RoR2/Base/Beetle/BeetleMaster.prefab"
                    : "RoR2/Base/Lemurian/LemurianMaster.prefab",
                EnemyArchetype.Demolition => (wave >= 5 && Random.value > 0.5f)
                    ? "RoR2/Base/ClayBruiser/ClayBruiserMaster.prefab"
                    : (Random.value > 0.5f ? "RoR2/Base/BeetleGland/BeetleGuardMaster.prefab" : "RoR2/Base/Bison/BisonMaster.prefab"),
                EnemyArchetype.Titan => (wave >= 6 && Random.value > 0.45f)
                    ? "RoR2/Base/Titan/TitanMaster.prefab"
                    : "RoR2/Base/Golem/GolemMaster.prefab",
                _ => Random.value > 0.5f
                    ? "RoR2/Base/Wisp/WispMaster.prefab"
                    : "RoR2/Base/Imp/ImpMaster.prefab"
            };

            GameObject masterPrefab = Addressables.LoadAssetAsync<GameObject>(address).WaitForCompletion();
            if (masterPrefab == null) return;

            Vector3 spawnPos = FindPerimeterSpawnPoint(42f, 72f);
            var summon = new MasterSummon
            {
                masterPrefab = masterPrefab,
                position = spawnPos + Vector3.up * 1.0f,
                rotation = Quaternion.identity,
                teamIndexOverride = TeamIndex.Monster,
                ignoreTeamMemberLimit = true
            };

            CharacterMaster master = summon.Perform();
            if (master != null)
            {
                int hpBoost = wave * 2;
                int dmgBoost = Mathf.Max(1, Mathf.RoundToInt(wave * 1.2f));

                if (archetype == EnemyArchetype.Swarm)
                {
                    master.inventory.GiveItem(RoR2Content.Items.Hoof, 2);
                }
                else if (archetype == EnemyArchetype.Titan)
                {
                    hpBoost += 18 + wave * 3;
                    master.inventory.GiveItem(RoR2Content.Items.ArmorPlate, 3);
                }
                else if (archetype == EnemyArchetype.Demolition)
                {
                    master.inventory.GiveItem(RoR2Content.Items.Syringe, 3);
                }

                master.inventory.GiveItem(RoR2Content.Items.BoostHp, hpBoost);
                master.inventory.GiveItem(RoR2Content.Items.BoostDamage, dmgBoost);

                CharacterBody body = master.GetBody();
                if (body != null)
                {
                    if (archetype == EnemyArchetype.Titan)
                    {
                        body.baseMoveSpeed *= 0.65f;
                        body.baseNameToken = "[ТИТАН] " + body.GetDisplayName();
                    }
                    else if (archetype == EnemyArchetype.Demolition)
                    {
                        body.baseNameToken = "[ДЕМОНТАЖ] " + body.GetDisplayName();
                    }
                    else if (archetype == EnemyArchetype.Swarm)
                    {
                        body.baseMaxHealth *= 0.65f;
                        body.baseNameToken = "[РОЙ] " + body.GetDisplayName();
                    }
                    body.RecalculateStats();
                }

                var marker = master.gameObject.AddComponent<WaveEnemyMarker>();
                marker.Archetype = archetype;

                aliveWaveEnemies.Add(master);
            }
        }

        private Vector3 FindPerimeterSpawnPoint(float minRadius, float maxRadius)
        {
            Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(minRadius, maxRadius);
            Vector3 desired = mapCenterPosition + new Vector3(dir.x, 4f, dir.y);

            NodeGraph groundNodes = SceneInfo.instance != null ? SceneInfo.instance.groundNodes : null;
            if (groundNodes != null)
            {
                NodeGraph.NodeIndex idx = groundNodes.FindClosestNode(desired, HullClassification.Golem);
                if (groundNodes.GetNodePosition(idx, out Vector3 nodePos))
                {
                    return nodePos;
                }
            }
            return desired;
        }

        private void OnCharacterDeathGlobal(DamageReport report)
        {
            if (!NetworkServer.active || report == null) return;

            if (EnergyCoreBodyObject != null && report.victimBody != null && report.victimBody.gameObject == EnergyCoreBodyObject)
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = "<color=#FF1111>[ПОРАЖЕНИЕ] Энергетическое Ядро было уничтожено натиском врагов!</color>"
                });
                if (Run.instance != null)
                {
                    Run.instance.BeginGameOver(RoR2Content.GameEndings.StandardLoss);
                }
                return;
            }

            if (report.victimTeamIndex == TeamIndex.Monster)
            {
                uint killGold = (uint)Mathf.Max(3, 5 + CurrentWave);
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    if (pcmc.master != null)
                    {
                        pcmc.master.GiveMoney(killGold);
                    }
                }
            }
        }

        public GameObject FindNearestStructure(Vector3 fromPos, float maxRadius)
        {
            BuildableStructure best = null;
            float bestDistSqr = maxRadius * maxRadius;
            foreach (var s in ActiveStructures)
            {
                if (s == null || s.Body == null || !s.Body.healthComponent.alive) continue;
                float d = (s.Body.corePosition - fromPos).sqrMagnitude;
                if (d < bestDistSqr)
                {
                    bestDistSqr = d;
                    best = s;
                }
            }
            return best != null ? best.Body.gameObject : null;
        }

        public GameObject FindNearestPlayer(Vector3 fromPos)
        {
            GameObject best = null;
            float bestDistSqr = float.MaxValue;
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc.master != null && pcmc.master.hasBody)
                {
                    var b = pcmc.master.GetBody();
                    if (b != null && b.healthComponent != null && b.healthComponent.alive)
                    {
                        float d = (b.corePosition - fromPos).sqrMagnitude;
                        if (d < bestDistSqr)
                        {
                            bestDistSqr = d;
                            best = b.gameObject;
                        }
                    }
                }
            }
            return best;
        }
    }
}
