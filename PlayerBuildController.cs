using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RoR2BaseDefense
{
    public class PlayerBuildController : MonoBehaviour
    {
        private PlayerCharacterMasterController pcmc;
        private bool showBuildMenu;
        private BuildableDef selectedBuildable;
        private BuildableId lastGhostId = (BuildableId)(-1);

        private GameObject ghostPreviewRoot;
        private Material ghostMatValidGreen;
        private Material ghostMatInvalidRed;
        private float customRotationYaw;

        private BuildableStructure hoveredStructure;

        private Texture2D darkPanelTex;
        private Texture2D hpBarTex;
        private Texture2D shieldBarTex;
        private Texture2D brokenShieldBarTex;
        private Texture2D cardBgTex;
        private Texture2D cardHoverTex;

        private void Awake()
        {
            pcmc = GetComponent<PlayerCharacterMasterController>();
            InitGhostMaterialsAndUiTextures();
        }

        private void OnDestroy()
        {
            if (ghostPreviewRoot != null)
            {
                Destroy(ghostPreviewRoot);
            }
        }

        private void InitGhostMaterialsAndUiTextures()
        {
            // ИСПРАВЛЕНО: Вместо Shader.Find("Sprites/Default"), который крашил хук LegacyShaderAPI в RoR2 1.5.0,
            // используем нативный полупрозрачный материал голограммы Башни Инженера или безопасный шейдер hgStandard!
            Material baseGhostMat = null;
            try
            {
                baseGhostMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/Engi/matBlueprintsOk.mat").WaitForCompletion();
            }
            catch
            {
            }

            if (baseGhostMat != null)
            {
                ghostMatValidGreen = new Material(baseGhostMat);
                ghostMatValidGreen.color = new Color(0.10f, 0.95f, 0.30f, 0.55f);

                ghostMatInvalidRed = new Material(baseGhostMat);
                ghostMatInvalidRed.color = new Color(1.00f, 0.15f, 0.15f, 0.55f);
            }
            else
            {
                ghostMatValidGreen = VanillaVisualBuilder.CreateGlowingMaterial(new Color(0.10f, 0.95f, 0.30f, 0.55f), 2.0f);
                ghostMatInvalidRed = VanillaVisualBuilder.CreateGlowingMaterial(new Color(1.00f, 0.15f, 0.15f, 0.55f), 2.0f);
            }

            darkPanelTex = MakeSolidTex(new Color(0.08f, 0.10f, 0.14f, 0.90f));
            hpBarTex = MakeSolidTex(new Color(0.20f, 0.85f, 0.30f, 0.95f));
            shieldBarTex = MakeSolidTex(new Color(0.15f, 0.70f, 1.00f, 0.95f));
            brokenShieldBarTex = MakeSolidTex(new Color(0.95f, 0.35f, 0.15f, 0.95f));
            cardBgTex = MakeSolidTex(new Color(0.14f, 0.18f, 0.24f, 0.95f));
            cardHoverTex = MakeSolidTex(new Color(0.22f, 0.32f, 0.44f, 0.98f));
        }

        private static Texture2D MakeSolidTex(Color col)
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { col, col, col, col });
            tex.Apply();
            return tex;
        }

        private bool IsLocalPlayer()
        {
            return pcmc != null && pcmc.networkUser != null && pcmc.networkUser.isLocalPlayer;
        }

        private void Update()
        {
            if (!IsLocalPlayer() || !BaseDefensePlugin.EnableMode.Value) return;

            if (Input.GetKeyDown(BaseDefensePlugin.ToggleBuildMenuKey.Value))
            {
                SetMenuState(!showBuildMenu);
            }

            if (Input.GetKeyDown(KeyCode.Escape) && showBuildMenu)
            {
                SetMenuState(false);
            }

            if (selectedBuildable != null && !showBuildMenu)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    customRotationYaw += Mathf.Sign(scroll) * 22.5f;
                }
                if (Input.GetKeyDown(KeyCode.R))
                {
                    customRotationYaw += 45f;
                }
            }

            UpdateHoveredStructureForDemolish();
            UpdateGhostPreviewModel();
            HandleBuildAndDemolishActions();
        }

        private void SetMenuState(bool open)
        {
            showBuildMenu = open;
            Cursor.visible = showBuildMenu;
            Cursor.lockState = showBuildMenu ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void UpdateGhostPreviewModel()
        {
            if (selectedBuildable == null || showBuildMenu || pcmc.master == null || !pcmc.master.hasBody)
            {
                if (ghostPreviewRoot != null) ghostPreviewRoot.SetActive(false);
                return;
            }

            if (ghostPreviewRoot == null || lastGhostId != selectedBuildable.Id)
            {
                RebuildGhostMesh(selectedBuildable);
                lastGhostId = selectedBuildable.Id;
            }

            if (TryGetPlacementPoint(out Vector3 point, out Quaternion rot))
            {
                ghostPreviewRoot.SetActive(true);
                ghostPreviewRoot.transform.position = point;
                ghostPreviewRoot.transform.rotation = rot;

                bool canAfford = pcmc.master.money >= (uint)selectedBuildable.Cost;
                Material targetMat = canAfford ? ghostMatValidGreen : ghostMatInvalidRed;

                foreach (var r in ghostPreviewRoot.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = targetMat;
                }
            }
            else
            {
                ghostPreviewRoot.SetActive(false);
            }
        }

        private void RebuildGhostMesh(BuildableDef def)
        {
            if (ghostPreviewRoot != null) Destroy(ghostPreviewRoot);

            ghostPreviewRoot = new GameObject("BaseDefense_GhostPreview");

            if (def.IsBarricade)
            {
                Vector3 wallSize = (def.Id == BuildableId.LightBarricade)
                    ? new Vector3(3.2f, 1.8f, 0.75f)
                    : new Vector3(2.6f, 2.5f, 2.2f);

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(cube.GetComponent<Collider>());
                cube.transform.SetParent(ghostPreviewRoot.transform, false);
                cube.transform.localPosition = new Vector3(0f, wallSize.y * 0.5f, 0f);
                cube.transform.localScale = wallSize;
            }
            else if (def.Id == BuildableId.DroneStation || def.Id == BuildableId.MoneyGenerator || def.Id == BuildableId.HealStation)
            {
                GameObject baseCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(baseCyl.GetComponent<Collider>());
                baseCyl.transform.SetParent(ghostPreviewRoot.transform, false);
                baseCyl.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                baseCyl.transform.localScale = new Vector3(1.8f, 0.3f, 1.8f);

                GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(pylon.GetComponent<Collider>());
                pylon.transform.SetParent(ghostPreviewRoot.transform, false);
                pylon.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                pylon.transform.localScale = new Vector3(0.65f, 1.2f, 0.65f);

                GameObject topOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(topOrb.GetComponent<Collider>());
                topOrb.transform.SetParent(ghostPreviewRoot.transform, false);
                topOrb.transform.localPosition = new Vector3(0f, 2.75f, 0f);
                topOrb.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            }
            else
            {
                GameObject tripod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(tripod.GetComponent<Collider>());
                tripod.transform.SetParent(ghostPreviewRoot.transform, false);
                tripod.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                tripod.transform.localScale = new Vector3(1.1f, 0.7f, 1.1f);

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(head.GetComponent<Collider>());
                head.transform.SetParent(ghostPreviewRoot.transform, false);
                head.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                head.transform.localScale = new Vector3(1.0f, 0.8f, 1.2f);

                GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(barrel.GetComponent<Collider>());
                barrel.transform.SetParent(ghostPreviewRoot.transform, false);
                barrel.transform.localPosition = new Vector3(0f, 1.6f, 0.85f);
                barrel.transform.localScale = new Vector3(0.28f, 0.28f, 1.1f);
            }
        }

        private void HandleBuildAndDemolishActions()
        {
            if (showBuildMenu || pcmc.master == null || !pcmc.master.hasBody) return;

            if (selectedBuildable != null && Input.GetMouseButtonDown(0))
            {
                if (pcmc.master.money >= (uint)selectedBuildable.Cost && TryGetPlacementPoint(out Vector3 pos, out Quaternion rot))
                {
                    var masterNetId = pcmc.master.GetComponent<NetworkIdentity>().netId;
                    if (NetworkServer.active)
                    {
                        BuildingServerManager.ServerPlaceStructure(pcmc.master, selectedBuildable.Id, pos, rot);
                    }
                    else
                    {
                        new NetRequestPlaceStructure(masterNetId, selectedBuildable.Id, pos, rot).Send(NetworkDestination.Server);
                    }
                }
            }

            if (selectedBuildable != null && Input.GetMouseButtonDown(1))
            {
                selectedBuildable = null;
            }

            if (Input.GetKeyDown(BaseDefensePlugin.DemolishKey.Value) && hoveredStructure != null && hoveredStructure.Body != null)
            {
                var masterNetId = pcmc.master.GetComponent<NetworkIdentity>().netId;
                var targetBodyNetId = hoveredStructure.Body.GetComponent<NetworkIdentity>().netId;

                if (NetworkServer.active)
                {
                    BuildingServerManager.ServerDemolishStructure(pcmc.master, hoveredStructure);
                }
                else
                {
                    new NetRequestDemolishStructure(masterNetId, targetBodyNetId).Send(NetworkDestination.Server);
                }
            }
        }

        private bool TryGetPlacementPoint(out Vector3 point, out Quaternion rot)
        {
            point = Vector3.zero;
            rot = Quaternion.identity;

            CharacterBody body = pcmc.master?.GetBody();
            if (body == null || body.inputBank == null) return false;

            Ray ray = new Ray(body.inputBank.aimOrigin, body.inputBank.aimDirection);
            if (Physics.Raycast(ray, out RaycastHit hit, 32f, LayerIndex.world.mask))
            {
                point = hit.point;
                Vector3 flatForward = body.inputBank.aimDirection;
                flatForward.y = 0f;
                if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;

                rot = Quaternion.LookRotation(flatForward.normalized) * Quaternion.Euler(0f, customRotationYaw, 0f);
                return true;
            }
            return false;
        }

        private void UpdateHoveredStructureForDemolish()
        {
            hoveredStructure = null;
            CharacterBody body = pcmc.master?.GetBody();
            if (body == null || body.inputBank == null) return;

            Ray ray = new Ray(body.inputBank.aimOrigin, body.inputBank.aimDirection);
            if (Physics.Raycast(ray, out RaycastHit hit, 22f))
            {
                hoveredStructure = hit.collider.GetComponentInParent<BuildableStructure>();
            }
        }

        private void OnGUI()
        {
            if (!IsLocalPlayer() || !BaseDefensePlugin.EnableMode.Value) return;
            if (Stage.instance == null) return; // Рисуем боевой HUD только внутри этапа

            DrawTopCoreAndWaveHud();
            DrawHoverDemolishAndBuildHints();

            if (showBuildMenu)
            {
                DrawIconBuildMenuWindow();
            }
        }

        private void DrawTopCoreAndWaveHud()
        {
            float panelW = 460f;
            float panelH = 88f;
            float panelX = (Screen.width - panelW) * 0.5f;
            float panelY = 12f;

            GUI.DrawTexture(new Rect(panelX, panelY, panelW, panelH), darkPanelTex);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            string waveStatus = ClientHudState.IsWaveActive
                ? $"<color=#FF6633>ВОЛНА {ClientHudState.CurrentWave} / {ClientHudState.MaxWaves}</color>  |  Живых врагов: <b>{ClientHudState.AliveEnemiesCount}</b>"
                : $"<color=#FFEE44>ПОДГОТОВКА К ВОЛНЕ {ClientHudState.CurrentWave + 1} / {ClientHudState.MaxWaves}</color>  ({ClientHudState.WavePrepTimer:0.0} сек)";

            GUI.Label(new Rect(panelX, panelY + 4f, panelW, 24f), waveStatus, titleStyle);

            float barX = panelX + 20f;
            float barW = panelW - 40f;
            float hpFraction = Mathf.Clamp01(ClientHudState.CoreHealth / Mathf.Max(1f, ClientHudState.CoreMaxHealth));
            GUI.DrawTexture(new Rect(barX, panelY + 32f, barW, 20f), cardBgTex);
            GUI.DrawTexture(new Rect(barX, panelY + 32f, barW * hpFraction, 20f), hpBarTex);
            GUI.Label(new Rect(barX, panelY + 30f, barW, 22f),
                $"<b>ЯДРО HP: {Mathf.CeilToInt(ClientHudState.CoreHealth)} / {Mathf.CeilToInt(ClientHudState.CoreMaxHealth)}</b>", titleStyle);

            float shFraction = Mathf.Clamp01(ClientHudState.CoreShield / Mathf.Max(1f, ClientHudState.CoreMaxShield));
            GUI.DrawTexture(new Rect(barX, panelY + 58f, barW, 20f), cardBgTex);

            if (ClientHudState.IsShieldBroken)
            {
                float rechargeProgress = 1f - Mathf.Clamp01(ClientHudState.ShieldRechargeTimer / Mathf.Max(1f, BaseDefensePlugin.CoreShieldRechargeDelay.Value));
                GUI.DrawTexture(new Rect(barX, panelY + 58f, barW * rechargeProgress, 20f), brokenShieldBarTex);
                GUI.Label(new Rect(barX, panelY + 56f, barW, 22f),
                    $"<b>ЩИТ ПРОБИТ! ПЕРЕЗАРЯДКА: {ClientHudState.ShieldRechargeTimer:0.0} СЕК</b>", titleStyle);
            }
            else
            {
                GUI.DrawTexture(new Rect(barX, panelY + 58f, barW * shFraction, 20f), shieldBarTex);
                GUI.Label(new Rect(barX, panelY + 56f, barW, 22f),
                    $"<b>ЭНЕРГО-ЩИТ: {Mathf.CeilToInt(ClientHudState.CoreShield)} / {Mathf.CeilToInt(ClientHudState.CoreMaxShield)}</b>", titleStyle);
            }
        }

        private void DrawHoverDemolishAndBuildHints()
        {
            GUIStyle hintStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                richText = true
            };

            if (hoveredStructure != null && hoveredStructure.Body != null)
            {
                int refund = Mathf.RoundToInt(hoveredStructure.OriginalCost * BaseDefensePlugin.RefundPercentage.Value);
                float curHp = Mathf.CeilToInt(hoveredStructure.Body.healthComponent.health);
                float maxHp = Mathf.CeilToInt(hoveredStructure.Body.healthComponent.fullHealth);

                Rect demolishRect = new Rect(Screen.width * 0.5f - 210f, Screen.height * 0.5f + 38f, 420f, 52f);
                GUI.Box(demolishRect,
                    $"<b>{hoveredStructure.Body.GetDisplayName()}</b> (HP: {curHp}/{maxHp})\n" +
                    $"Нажмите <color=#FFCC00>[{BaseDefensePlugin.DemolishKey.Value}]</color> чтобы снести и вернуть <color=#55FF55>+{refund}$ (50%)</color>",
                    hintStyle);
            }

            if (selectedBuildable != null && !showBuildMenu)
            {
                Rect buildHintRect = new Rect(Screen.width * 0.5f - 230f, Screen.height - 95f, 460f, 54f);
                GUI.Box(buildHintRect,
                    $"Строительство: <color=#44FF66><b>{selectedBuildable.Name}</b> ({selectedBuildable.Cost}$)</color>\n" +
                    "[ЛКМ] Установить  |  [Колесо / R] Повернуть  |  [ПКМ] Отмена  |  [B] Меню",
                    hintStyle);
            }
        }

        private void DrawIconBuildMenuWindow()
        {
            float winW = 760f;
            float winH = 510f;
            float winX = (Screen.width - winW) * 0.5f;
            float winY = (Screen.height - winH) * 0.5f;

            GUI.DrawTexture(new Rect(winX, winY, winW, winH), darkPanelTex);

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            uint playerMoney = pcmc.master != null ? pcmc.master.money : 0;
            GUI.Label(new Rect(winX, winY + 12f, winW, 28f),
                $"<color=#FFEE55>МЕНЮ СТРОИТЕЛЬСТВА БАЗЫ</color>   |   Ваш баланс: <color=#55FF55>{playerMoney}$</color>   |   Закрыть: [{BaseDefensePlugin.ToggleBuildMenuKey.Value}]",
                headerStyle);

            GUIStyle nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, richText = true };
            GUIStyle descStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, richText = true };

            int cols = 2;
            float cardW = 350f;
            float cardH = 78f;
            float startX = winX + 22f;
            float startY = winY + 52f;

            for (int i = 0; i < BuildableCatalog.Definitions.Count; i++)
            {
                BuildableDef def = BuildableCatalog.Definitions[i];
                int row = i / cols;
                int col = i % cols;

                Rect cardRect = new Rect(startX + col * (cardW + 16f), startY + row * (cardH + 10f), cardW, cardH);
                bool canAfford = playerMoney >= (uint)def.Cost;
                bool isHovered = cardRect.Contains(Event.current.mousePosition);

                GUI.DrawTexture(cardRect, isHovered ? cardHoverTex : cardBgTex);

                Texture2D iconTex = BuildableCatalog.GetOrLoadIcon(def);
                if (iconTex != null)
                {
                    GUI.DrawTexture(new Rect(cardRect.x + 8f, cardRect.y + 9f, 60f, 60f), iconTex);
                }

                string priceColor = canAfford ? "#55FF55" : "#FF5555";
                GUI.Label(new Rect(cardRect.x + 76f, cardRect.y + 6f, cardW - 84f, 22f),
                    $"<color=#FFFFFF>{def.Name}</color> — <color={priceColor}>{def.Cost}$</color>", nameStyle);

                GUI.Label(new Rect(cardRect.x + 76f, cardRect.y + 28f, cardW - 84f, 44f),
                    $"<color=#CCCCCC>{def.ShortDesc}</color>\n<color=#88CCFF>Базовое HP: {def.MaxHealth}</color>", descStyle);

                if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
                {
                    selectedBuildable = def;
                    SetMenuState(false);
                }
            }
        }
    }
}
