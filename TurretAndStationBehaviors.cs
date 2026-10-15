using System.Collections.Generic;
using System.Linq;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RoR2BaseDefense
{
    public class BuildableStructure : MonoBehaviour
    {
        public BuildableId Id;
        public int OriginalCost;
        public NetworkInstanceId OwnerMasterNetId;
        public CharacterMaster OwnerMaster;
        public CharacterBody Body;

        public bool IsDoorOpen = false;
        public int SpringUsesRemaining = 5;

        private float actionTimer;
        private List<CharacterMaster> spawnedDrones = new List<CharacterMaster>();

        private static GameObject rocketProjectilePrefab;
        private static GameObject laserHitEffectPrefab;

        private void Awake()
        {
            Body = GetComponent<CharacterBody>();
            if (BaseDefenseManager.Instance != null && !BaseDefenseManager.Instance.ActiveStructures.Contains(this))
            {
                BaseDefenseManager.Instance.ActiveStructures.Add(this);
            }
            EnsureVanillaAssetsLoaded();
        }

        private void OnDestroy()
        {
            if (BaseDefenseManager.Instance != null)
            {
                BaseDefenseManager.Instance.ActiveStructures.Remove(this);
            }
        }

        private static void EnsureVanillaAssetsLoaded()
        {
            if (rocketProjectilePrefab == null)
            {
                rocketProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/MissileProjectile.prefab").WaitForCompletion();
            }
            if (laserHitEffectPrefab == null)
            {
                laserHitEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Golem/HitsparkGolemLaser.prefab").WaitForCompletion();
            }
        }

        // Функциональная дверь (Пункт 8)
        public void ServerToggleDoor()
        {
            if (!NetworkServer.active || Id != BuildableId.DoorBlock) return;
            IsDoorOpen = !IsDoorOpen;
            ApplyDoorStateVisualAndCollider(IsDoorOpen);

            if (Body != null && Body.TryGetComponent<NetworkIdentity>(out var netId))
            {
                new NetSyncDoorState(netId.netId, IsDoorOpen).Send(NetworkDestination.Clients);
            }
        }

        public void ApplyDoorStateVisualAndCollider(bool open)
        {
            IsDoorOpen = open;
            Transform blocker = transform.Find("DoorWorldBlocker");
            if (blocker != null && blocker.TryGetComponent<BoxCollider>(out var col))
            {
                col.enabled = !open;
            }

            Transform visRoot = transform.Find("CustomStructureVisual");
            if (visRoot != null)
            {
                Transform leaf = visRoot.Find("DoorLeafMesh");
                if (leaf != null)
                {
                    leaf.gameObject.SetActive(!open);
                }
            }
        }

        private void FixedUpdate()
        {
            if (Body == null || Body.healthComponent == null || !Body.healthComponent.alive) return;

            // Гарантируем, что стандартное оружие турели Инженера выключено на всех кастомных объектах (Пункты 5 и 6)
            if (Id != BuildableId.MachineGunTurret && Body.inputBank != null)
            {
                Body.inputBank.skill1.PushState(false);
            }

            if (!NetworkServer.active) return;

            actionTimer -= Time.fixedDeltaTime;
            if (actionTimer <= 0f)
            {
                ExecuteCustomAction();
            }
        }

        private void ExecuteCustomAction()
        {
            switch (Id)
            {
                case BuildableId.RocketTurret:
                    actionTimer = 2.1f;
                    FireHomingRocket();
                    break;

                case BuildableId.LaserTurret:
                    actionTimer = 1.5f;
                    FireHighPowerLaser();
                    break;

                case BuildableId.TeslaTurret:
                    actionTimer = 1.15f;
                    FireBouncingLightning();
                    break;

                case BuildableId.FlameTurret:
                    actionTimer = 0.35f;
                    FireFlamethrowerCone();
                    break;

                case BuildableId.DroneStation:
                    actionTimer = 10.0f;
                    MaintainDroneSquad();
                    break;

                case BuildableId.MoneyGenerator:
                    actionTimer = 2.0f;
                    DistributeGeneratorGold();
                    break;

                case BuildableId.HealStation:
                    actionTimer = 0.5f; // Пункт 10: Лечит ТОЛЬКО игроков!
                    PulseHealPlayersOnly();
                    break;

                case BuildableId.RepairStation:
                    actionTimer = 1.0f; // Пункт 11: Ремонтная станция чинит ТОЛЬКО постройки и турели!
                    PulseRepairStructuresOnly();
                    break;

                case BuildableId.SpikePlatform:
                    actionTimer = 0.5f; // Пункт 8: Периодический урон врагам, стоящим на платформе
                    DamageEnemiesOnSpikePlatform();
                    break;

                case BuildableId.SpringTrap:
                    actionTimer = 0.25f; // Пункт 8: Проверка врагов на пружине (5 подбрасываний)
                    CheckAndLaunchSpringTrap();
                    break;

                default:
                    actionTimer = 1.0f;
                    break;
            }
        }

        private HurtBox FindBestEnemyTarget(float maxRange, bool prioritizeHighHp = false)
        {
            BullseyeSearch search = new BullseyeSearch
            {
                teamMaskFilter = TeamMask.GetEnemyTeams(TeamIndex.Player),
                searchOrigin = Body.corePosition + Vector3.up * 1.4f,
                maxDistanceFilter = maxRange,
                filterByLoS = true,
                sortMode = BullseyeSearch.SortMode.Distance
            };
            search.RefreshCandidates();
            var results = search.GetResults().ToList();
            if (results.Count == 0) return null;

            if (prioritizeHighHp)
            {
                return results
                    .Where(h => h != null && h.healthComponent != null)
                    .OrderByDescending(h => h.healthComponent.health)
                    .FirstOrDefault();
            }
            return results.FirstOrDefault();
        }

        private void FireHomingRocket()
        {
            HurtBox target = FindBestEnemyTarget(65f, false);
            if (target == null || rocketProjectilePrefab == null) return;

            Vector3 muzzlePos = Body.corePosition + Vector3.up * 1.8f;
            Vector3 dir = (target.transform.position - muzzlePos).normalized;

            ProjectileManager.instance.FireProjectile(
                rocketProjectilePrefab,
                muzzlePos,
                Util.QuaternionSafeLookRotation(dir),
                Body.gameObject,
                Body.damage * 3.5f,
                120f,
                Body.RollCrit(),
                DamageColorIndex.Default,
                target.gameObject
            );
        }

        private void FireHighPowerLaser()
        {
            HurtBox target = FindBestEnemyTarget(80f, true);
            if (target == null) return;

            Vector3 origin = Body.corePosition + Vector3.up * 1.8f;
            Vector3 aimDir = (target.transform.position - origin).normalized;
            GameObject tracerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/Tracers/TracerGolem");

            BulletAttack laserAttack = new BulletAttack
            {
                owner = Body.gameObject,
                weapon = Body.gameObject,
                origin = origin,
                aimVector = aimDir,
                minSpread = 0f,
                maxSpread = 0f,
                bulletCount = 1,
                damage = Body.damage * 4.8f,
                force = 1200f,
                tracerEffectPrefab = tracerPrefab,
                hitEffectPrefab = laserHitEffectPrefab,
                isCrit = Body.RollCrit(),
                radius = 0.65f,
                maxDistance = 85f,
                stopperMask = LayerIndex.world.mask,
                damageType = DamageType.SlowOnHit
            };
            laserAttack.Fire();
        }

        private void FireBouncingLightning()
        {
            HurtBox target = FindBestEnemyTarget(32f, false);
            if (target == null) return;

            LightningOrb orb = new LightningOrb
            {
                origin = Body.corePosition + Vector3.up * 1.8f,
                damageValue = Body.damage * 1.9f,
                isCrit = Body.RollCrit(),
                bouncesRemaining = 4,
                teamIndex = TeamIndex.Player,
                attacker = Body.gameObject,
                procCoefficient = 0.75f,
                target = target,
                lightningType = LightningOrb.LightningType.Ukulele,
                range = 20f,
                damageColorIndex = DamageColorIndex.Item
            };
            OrbManager.instance.AddOrb(orb);
        }

        private void FireFlamethrowerCone()
        {
            HurtBox target = FindBestEnemyTarget(16f, false);
            if (target == null) return;

            Vector3 origin = Body.corePosition + Vector3.up * 1.3f;
            Vector3 dir = (target.transform.position - origin).normalized;

            BlastAttack flameBlast = new BlastAttack
            {
                attacker = Body.gameObject,
                inflictor = Body.gameObject,
                teamIndex = TeamIndex.Player,
                position = origin + dir * 6.5f,
                radius = 7.5f,
                falloffModel = BlastAttack.FalloffModel.None,
                baseDamage = Body.damage * 0.75f,
                baseForce = 80f,
                crit = Body.RollCrit(),
                damageType = DamageType.IgniteOnHit,
                procCoefficient = 0.5f
            };
            flameBlast.Fire();

            EffectManager.SimpleEffect(
                LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OmniEffect/OmniExplosionVFXQuick"),
                origin + dir * 5f,
                Quaternion.LookRotation(dir),
                true
            );
        }

        // Пункт 8: Шипастая платформа бьет монстров, стоящих на ней
        private void DamageEnemiesOnSpikePlatform()
        {
            BlastAttack spikeAttack = new BlastAttack
            {
                attacker = Body.gameObject,
                inflictor = Body.gameObject,
                teamIndex = TeamIndex.Player,
                position = transform.position + Vector3.up * 0.6f,
                radius = 2.6f,
                falloffModel = BlastAttack.FalloffModel.None,
                baseDamage = Body.damage * 0.85f,
                baseForce = 0f,
                crit = false,
                damageType = DamageType.BleedOnHit,
                procCoefficient = 0.3f
            };
            spikeAttack.Fire();
        }

        // Пункт 8: Пружинная установка (5 раз высоко подкидывает врагов в воздух и ломается)
        private void CheckAndLaunchSpringTrap()
        {
            BullseyeSearch search = new BullseyeSearch
            {
                teamMaskFilter = TeamMask.GetEnemyTeams(TeamIndex.Player),
                searchOrigin = transform.position + Vector3.up * 0.5f,
                maxDistanceFilter = 2.5f,
                filterByLoS = false
            };
            search.RefreshCandidates();
            var targets = search.GetResults().ToList();
            if (targets.Count == 0) return;

            bool launchedAny = false;
            foreach (var hb in targets)
            {
                if (hb != null && hb.healthComponent != null && hb.healthComponent.body != null)
                {
                    CharacterBody enemyBody = hb.healthComponent.body;
                    if (enemyBody.characterMotor != null)
                    {
                        enemyBody.characterMotor.Motor.ForceUnground();
                        enemyBody.characterMotor.velocity = new Vector3(Random.Range(-6f, 6f), 42f, Random.Range(-6f, 6f));
                        launchedAny = true;
                    }
                    else if (enemyBody.rigidbody != null)
                    {
                        enemyBody.rigidbody.velocity = new Vector3(0f, 38f, 0f);
                        launchedAny = true;
                    }
                }
            }

            if (launchedAny)
            {
                SpringUsesRemaining--;
                actionTimer = 1.4f; // Небольшая задержка перед следующим срабатыванием

                EffectManager.SimpleEffect(
                    LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OmniEffect/OmniRecycleEffect"),
                    transform.position + Vector3.up * 0.5f,
                    Quaternion.identity,
                    true
                );

                if (SpringUsesRemaining <= 0)
                {
                    if (Body.master != null) Body.master.TrueKill();
                    else NetworkServer.Destroy(Body.gameObject);
                }
            }
        }

        private void MaintainDroneSquad()
        {
            spawnedDrones.RemoveAll(d => d == null || !d.hasBody || !d.GetBody().healthComponent.alive);
            if (spawnedDrones.Count >= BaseDefensePlugin.MaxDronesPerStation.Value) return;

            var droneMasterPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Drones/Drone1Master.prefab").WaitForCompletion();
            if (droneMasterPrefab == null) return;

            var summon = new