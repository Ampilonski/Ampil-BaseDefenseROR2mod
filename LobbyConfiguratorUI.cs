using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2BaseDefense
{
    // Меню-конфигуратор в лобби выбора персонажа перед стартом раунда
    public class LobbyConfiguratorUI : MonoBehaviour
    {
        private bool isWindowOpen = false;
        private Vector2 scrollPos = Vector2.zero;

        private Texture2D windowBgTex;
        private Texture2D headerBgTex;
        private Texture2D sectionBgTex;
        private Texture2D btnNormalTex;
        private Texture2D btnHoverTex;
        private Texture2D toggleOnTex;
        private Texture2D toggleOffTex;

        private void Awake()
        {
            windowBgTex = MakeTex(new Color(0.07f, 0.09f, 0.13f, 0.96f));
            headerBgTex = MakeTex(new Color(0.12f, 0.18f, 0.28f, 0.98f));
            sectionBgTex = MakeTex(new Color(0.11f, 0.14f, 0.20f, 0.92f));
            btnNormalTex = MakeTex(new Color(0.18f, 0.26f, 0.38f, 0.95f));
            btnHoverTex = MakeTex(new Color(0.28f, 0.42f, 0.60f, 0.98f));
            toggleOnTex = MakeTex(new Color(0.15f, 0.65f, 0.30f, 0.95f));
            toggleOffTex = MakeTex(new Color(0.65f, 0.18f, 0.18f, 0.95f));
        }

        private static Texture2D MakeTex(Color col)
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { col, col, col, col });
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            DrawLobbyTopButton();

            if (isWindowOpen)
            {
                DrawConfiguratorModalWindow();
            }
        }

        private void DrawLobbyTopButton()
        {
            float btnW = 340f;
            float btnH = 44f;
            float btnX = Screen.width * 0.5f - btnW * 0.5f;
            float btnY = 18f;

            Rect btnRect = new Rect(btnX, btnY, btnW, btnH);
            bool hovered = btnRect.Contains(Event.current.mousePosition);

            GUI.DrawTexture(btnRect, hovered ? btnHoverTex : headerBgTex);

            GUIStyle btnTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            string modeStatus = BaseDefensePlugin.EnableMode.Value
                ? "<color=#44FF66>[ВКЛ]</color>"
                : "<color=#FF5555>[ВЫКЛ]</color>";

            GUI.Label(btnRect, $"⚙ НАСТРОЙКИ ЗАЩИТЫ БАЗЫ {modeStatus}", btnTextStyle);

            if (GUI.Button(btnRect, GUIContent.none, GUIStyle.none))
            {
                isWindowOpen = !isWindowOpen;
            }
        }

        private void DrawConfiguratorModalWindow()
        {
            bool isHostOrSingleplayer = NetworkServer.active || !NetworkClient.active;

            float winW = 780f;
            float winH = 600f;
            float winX = (Screen.width - winW) * 0.5f;
            float winY = (Screen.height - winH) * 0.5f;

            GUI.DrawTexture(new Rect(winX, winY, winW, winH), windowBgTex);
            GUI.DrawTexture(new Rect(winX, winY, winW, 48f), headerBgTex);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            GUI.Label(new Rect(winX, winY + 10f, winW, 28f),
                "<color=#FFEE55>КОНФИГУРАТОР РЕЖИМА «ЗАЩИТА БАЗЫ» ПЕРЕД РАУНДОМ</color>", titleStyle);

            // Кнопка закрытия (X)
            Rect closeRect = new Rect(winX + winW - 42f, winY + 8f, 32f, 32f);
            GUI.DrawTexture(closeRect, toggleOffTex);
            GUI.Label(closeRect, "<b>X</b>", titleStyle);
            if (GUI.Button(closeRect, GUIContent.none, GUIStyle.none))
            {
                isWindowOpen = false;
                SyncIfHost();
            }

            // Область прокрутки настроек
            Rect scrollViewRect = new Rect(winX + 16f, winY + 58f, winW - 32f, winH - 122f);
            Rect contentRect = new Rect(0f, 0f, winW - 56f, 890f);

            scrollPos = GUI.BeginScrollView(scrollViewRect, scrollPos, contentRect);

            float y = 8f;

            // --- Переключатель активности режима ---
            DrawSectionHeader(ref y, contentRect.width, "1. ОСНОВНЫЕ ПАРАМЕТРЫ МАТЧА");

            Rect toggleRect = new Rect(10f, y, contentRect.width - 20f, 38f);
            GUI.DrawTexture(toggleRect, BaseDefensePlugin.EnableMode.Value ? toggleOnTex : toggleOffTex);
            GUI.Label(toggleRect, BaseDefensePlugin.EnableMode.Value
                ? "<b>РЕЖИМ «ЗАЩИТА БАЗЫ» АКТИВЕН (Нажмите для отключения)</b>"
                : "<b>РЕЖИМ «ЗАЩИТА БАЗЫ» ОТКЛЮЧЕН — Стандартная игра (Нажмите для включения)</b>", titleStyle);

            if (isHostOrSingleplayer && GUI.Button(toggleRect, GUIContent.none, GUIStyle.none))
            {
                BaseDefensePlugin.EnableMode.Value = !BaseDefensePlugin.EnableMode.Value;
                SyncIfHost();
            }
            y += 48f;

            DrawIntStepper(ref y, contentRect.width, "Количество волн для Победы (Цель раунда):", BaseDefensePlugin.MaxWavesToWin, 5, 100, 1, 5, isHostOrSingleplayer, "волн");
            DrawFloatStepper(ref y, contentRect.width, "Время передышки между волнами:", BaseDefensePlugin.TimeBetweenWaves, 5f, 120f, 5f, 15f, isHostOrSingleplayer, "сек");
            DrawIntStepper(ref y, contentRect.width, "Стартовое золото каждого игрока:", BaseDefensePlugin.StartingMoney, 0, 2000, 20, 100, isHostOrSingleplayer, "$");
            DrawIntStepper(ref y, contentRect.width, "Базовая награда золотом за волну:", BaseDefensePlugin.BaseWaveMoneyReward, 10, 1000, 15, 50, isHostOrSingleplayer, "$");
            DrawFloatStepper(ref y, contentRect.width, "Возврат денег при сносе постройки (0.5 = 50%):", BaseDefensePlugin.RefundPercentage, 0.1f, 1.0f, 0.1f, 0.25f, isHostOrSingleplayer, "доля");

            y += 10f;
            DrawSectionHeader(ref y, contentRect.width, "2. ЭНЕРГЕТИЧЕСКОЕ ЯДРО И 30-СЕКУНДНЫЙ ЩИТ");

            DrawFloatStepper(ref y, contentRect.width, "Базовое здоровье Энергетического Ядра:", BaseDefensePlugin.CoreBaseHealth, 1000f, 50000f, 500f, 2500f, isHostOrSingleplayer, "HP");
            DrawFloatStepper(ref y, contentRect.width, "Прирост здоровья Ядра за каждую волну:", BaseDefensePlugin.CoreHealthPerWave, 0f, 10000f, 200f, 1000f, isHostOrSingleplayer, "HP");
            DrawFloatStepper(ref y, contentRect.width, "Базовый дополнительный Щит Ядра:", BaseDefensePlugin.CoreBaseShield, 500f, 30000f, 250f, 1000f, isHostOrSingleplayer, "Щит");
            DrawFloatStepper(ref y, contentRect.width, "Прирост Щита Ядра за каждую волну:", BaseDefensePlugin.CoreShieldPerWave, 0f, 5000f, 100f, 500f, isHostOrSingleplayer, "Щит");
            DrawFloatStepper(ref y, contentRect.width, "Время перезарядки пробитого Щита:", BaseDefensePlugin.CoreShieldRechargeDelay, 5f, 120f, 5f, 10f, isHostOrSingleplayer, "сек");

            y += 10f;
            DrawSectionHeader(ref y, contentRect.width, "3. СТОИМОСТЬ УКРЕПЛЕНИЙ, ТУРЕЛЕЙ И МЕХАНИЗМОВ");

            DrawIntStepper(ref y, contentRect.width, "Легкая баррикада (850 HP):", BaseDefensePlugin.CostLightBarricade, 5, 500, 5, 25, isHostOrSingleplayer, "$");
            DrawIntStepper(ref y, contentRect.width, "Силовой барьер / Блок (2600 HP):", BaseDefensePlugin.CostHeavyBlock, 10, 800, 10, 50, isHostOrSingleplayer, "$");
            DrawIntStepper(ref y, contentRect.width, "Пулеметная турель:", BaseDefensePlugin.CostMachineGunTurret, 10, 800, 10, 50, isHostOrSingleplayer, "$");
            DrawIntStepper(ref y, contentRect.width, "Турель-Огнемет (AoE ожог):", BaseDefensePlugin.CostFlameTurret, 10, 800, 10, 50, isHostOrSingleplayer, "$");
            DrawIntStepper(ref y, contentRect.width, "Электро-Турель (Отскакивающий разряд):", BaseDefensePlugin.CostTeslaTurret, 10, 800, 10, 50, isHostOrSingleplayer, "$");
            DrawIntStepper(ref y, contentRect.width, "Ракетная турель:", BaseDefensePlugin.CostRocketTurret, 10, 800, 10, 50, isHostOrSingleplayer, "$");
            DrawIntStepper(ref y, contentRect.width, "Лазерная турель (Анти-Титан):", BaseDefensePlugin.CostLaserTurret, 10, 1000, 10, 50, isHostOrSingleplayer, "$");
            DrawIntStepper(ref y, contentRect.width, "Дрон-станция (Спавн каждые 10с):", BaseDefensePlugin.CostDroneStation, 10, 1000, 10, 50, isHostOrSingleplayer, "$");
            DrawIntStepper(ref y, contentRect.width, "Денежный генератор (Аура золота):", BaseDefensePlugin.CostMoneyGen, 10, 1000, 10, 50, isHostOrSingleplayer, "$");
            DrawIntStepper(ref y, contentRect.width, "Хил-станция (Регенерация игроков):", BaseDefensePlugin.CostHealStation, 10, 1000, 10, 50, isHostOrSingleplayer, "$");

            GUI.EndScrollView();

            // Нижняя панель с кнопками "Сбросить по умолчанию" и "Применить и синхронизировать"
            float footerY = winY + winH - 52f;
            Rect resetRect = new Rect(winX + 20f, footerY, 260f, 38f);
            GUI.DrawTexture(resetRect, btnNormalTex);
            GUI.Label(resetRect, "<b>СБРОСИТЬ НА СТАНДАРТНЫЕ</b>", titleStyle);
            if (isHostOrSingleplayer && GUI.Button(resetRect, GUIContent.none, GUIStyle.none))
            {
                ResetToBalancedDefaults();
                SyncIfHost();
            }

            Rect saveRect = new Rect(winX + winW - 280f, footerY, 260f, 38f);
            GUI.DrawTexture(saveRect, toggleOnTex);
            GUI.Label(saveRect, "<b>СОХРАНИТЬ И ЗАКРЫТЬ</b>", titleStyle);
            if (GUI.Button(saveRect, GUIContent.none, GUIStyle.none))
            {
                BaseDefensePlugin.RefreshPricesInCatalog();
                SyncIfHost();
                isWindowOpen = false;
            }
        }

        private void DrawSectionHeader(ref float y, float width, string title)
        {
            Rect r = new Rect(0f, y, width, 28f);
            GUI.DrawTexture(r, headerBgTex);
            GUIStyle s = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            GUI.Label(new Rect(12f, y + 3f, width - 20f, 22f), $"<color=#88DDFF>{title}</color>", s);
            y += 34f;
        }

        private void DrawIntStepper(ref float y, float width, string label, BepInEx.Configuration.ConfigEntry<int> entry, int min, int max, int stepSmall, int stepBig, bool editable, string unit)
        {
            Rect rowRect = new Rect(6f, y, width - 12f, 30f);
            GUI.DrawTexture(rowRect, sectionBgTex);

            GUIStyle lblStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, alignment = TextAnchor.MiddleLeft };
            GUIStyle valStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, richText = true, alignment = TextAnchor.MiddleCenter };

            GUI.Label(new Rect(16f, y + 3f, 380f, 24f), label, lblStyle);

            float rightStart = width - 310f;
            if (editable && DrawMiniBtn(new Rect(rightStart, y + 3f, 48f, 24f), $"-{stepBig}"))
            {
                entry.Value = Mathf.Clamp(entry.Value - stepBig, min, max);
                BaseDefensePlugin.RefreshPricesInCatalog();
            }
            if (editable && DrawMiniBtn(new Rect(rightStart + 52f, y + 3f, 42f, 24f), $"-{stepSmall}"))
            {
                entry.Value = Mathf.Clamp(entry.Value - stepSmall, min, max);
                BaseDefensePlugin.RefreshPricesInCatalog();
            }

            GUI.Label(new Rect(rightStart + 98f, y + 3f, 105f, 24f), $"<color=#55FF55>{entry.Value} {unit}</color>", valStyle);

            if (editable && DrawMiniBtn(new Rect(rightStart + 206f, y + 3f, 42f, 24f), $"+{stepSmall}"))
            {
                entry.Value = Mathf.Clamp(entry.Value + stepSmall, min, max);
                BaseDefensePlugin.RefreshPricesInCatalog();
            }
            if (editable && DrawMiniBtn(new Rect(rightStart + 252f, y + 3f, 48f, 24f), $"+{stepBig}"))
            {
                entry.Value = Mathf.Clamp(entry.Value + stepBig, min, max);
                BaseDefensePlugin.RefreshPricesInCatalog();
            }

            y += 34f;
        }

        private void DrawFloatStepper(ref float y, float width, string label, BepInEx.Configuration.ConfigEntry<float> entry, float min, float max, float stepSmall, float stepBig, bool editable, string unit)
        {
            Rect rowRect = new Rect(6f, y, width - 12f, 30f);
            GUI.DrawTexture(rowRect, sectionBgTex);

            GUIStyle lblStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, alignment = TextAnchor.MiddleLeft };
            GUIStyle valStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, richText = true, alignment = TextAnchor.MiddleCenter };

            GUI.Label(new Rect(16f, y + 3f, 380f, 24f), label, lblStyle);

            float rightStart = width - 310f;
            if (editable && DrawMiniBtn(new Rect(rightStart, y + 3f, 48f, 24f), $"-{stepBig:0.##}"))
            {
                entry.Value = Mathf.Clamp((float)System.Math.Round(entry.Value - stepBig, 2), min, max);
            }
            if (editable && DrawMiniBtn(new Rect(rightStart + 52f, y + 3f, 42f, 24f), $"-{stepSmall:0.##}"))
            {
                entry.Value = Mathf.Clamp((float)System.Math.Round(entry.Value - stepSmall, 2), min, max);
            }

            GUI.Label(new Rect(rightStart + 98f, y + 3f, 105f, 24f), $"<color=#55FF55>{entry.Value:0.##} {unit}</color>", valStyle);

            if (editable && DrawMiniBtn(new Rect(rightStart + 206f, y + 3f, 42f, 24f), $"+{stepSmall:0.##}"))
            {
                entry.Value = Mathf.Clamp((float)System.Math.Round(entry.Value + stepSmall, 2), min, max);
            }
            if (editable && DrawMiniBtn(new Rect(rightStart + 252f, y + 3f, 48f, 24f), $"+{stepBig:0.##}"))
            {
                entry.Value = Mathf.Clamp((float)System.Math.Round(entry.Value + stepBig, 2), min, max);
            }

            y += 34f;
        }

        private bool DrawMiniBtn(Rect r, string text)
        {
            bool hov = r.Contains(Event.current.mousePosition);
            GUI.DrawTexture(r, hov ? btnHoverTex : btnNormalTex);
            GUIStyle st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold };
            GUI.Label(r, text, st);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        private void ResetToBalancedDefaults()
        {
            BaseDefensePlugin.EnableMode.Value = true;
            BaseDefensePlugin.MaxWavesToWin.Value = 15;
            BaseDefensePlugin.TimeBetweenWaves.Value = 25f;
            BaseDefensePlugin.StartingMoney.Value = 160;
            BaseDefensePlugin.BaseWaveMoneyReward.Value = 75;
            BaseDefensePlugin.RefundPercentage.Value = 0.5f;

            BaseDefensePlugin.CoreBaseHealth.Value = 5000f;
            BaseDefensePlugin.CoreHealthPerWave.Value = 1200f;
            BaseDefensePlugin.CoreBaseShield.Value = 2500f;
            BaseDefensePlugin.CoreShieldPerWave.Value = 600f;
            BaseDefensePlugin.CoreShieldRechargeDelay.Value = 30f;

            BaseDefensePlugin.CostLightBarricade.Value = 25;
            BaseDefensePlugin.CostHeavyBlock.Value = 60;
            BaseDefensePlugin.CostMachineGunTurret.Value = 50;
            BaseDefensePlugin.CostFlameTurret.Value = 75;
            BaseDefensePlugin.CostHealStation.Value = 80;
            BaseDefensePlugin.CostTeslaTurret.Value = 90;
            BaseDefensePlugin.CostMoneyGen.Value = 100;
            BaseDefensePlugin.CostRocketTurret.Value = 110;
            BaseDefensePlugin.CostDroneStation.Value = 120;
            BaseDefensePlugin.CostLaserTurret.Value = 140;

            BaseDefensePlugin.RefreshPricesInCatalog();
        }

        private void SyncIfHost()
        {
            BaseDefensePlugin.RefreshPricesInCatalog();
            if (NetworkServer.active)
            {
                NetSyncLobbyConfig.CreateFromCurrentConfig().Send(NetworkDestination.Clients);
            }
        }
    }
}
