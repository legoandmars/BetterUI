﻿using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using flanne.Core;
using flanne;
using flanne.UI;
using TMPro;
using UnityEngine;

namespace BetterUI
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class BetterUI : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        private static Panel statsPanel = null;

        internal static ManualLogSource Log;
        
        private void Awake()
        {
            Log = base.Logger;

            // Plugin startup logic
            Log.LogInfo("Better UI loaded.");

            Harmony.CreateAndPatchAll(typeof(BetterUI), null);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerupMenuState), "PlayLevelUpAnimationCR")]
        private static bool PlayLevelUpAnimationCRPostPatch(ref GameController ___owner)
        {
            ___owner.powerupListUI.Show();

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerupMenuState), "EndLevelUpAnimationCR")]
        private static void EndLevelUpAnimationCRPostPatch(ref GameController ___owner)
        {
            ___owner.powerupListUI.Hide();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PauseState), "Enter")]
        private static void PauseStateEnterPostPatch(ref GameController ___owner)
        {
            if (statsPanel == null)
            {
                // copy the controls display so we don't have to manually add a bunch of tweening stuff

                var pausedTextObject = Instantiate(___owner.hud.transform.parent.Find("ControlsDisplay"), ___owner.hud.transform.parent);
                var rect = pausedTextObject.GetComponent<RectTransform>();
                var canvasGroup = pausedTextObject.GetComponent<CanvasGroup>();

                // this panel will be useful later for adding a stats display
                // for now we can just handle darkness level
                // todo: add an option to always display instead of just on pause
                statsPanel = pausedTextObject.GetComponent<Panel>();

                // some minor changes to make tweening work properly on first go
                Traverse.Create(pausedTextObject.GetComponent<AutoShowPanel>()).Field("startTime").SetValue(0f);
                Traverse.Create(statsPanel).Field("canvasGroup").SetValue(canvasGroup);
                canvasGroup.alpha = 0f;

                var allTextObjects = pausedTextObject.GetComponentsInChildren<TextMeshProUGUI>();

                for (var i = 0; i < allTextObjects.Length; i++)
                {
                    var text = allTextObjects[i];
                    text.alignment = TextAlignmentOptions.BottomLeft;
                    if (i == 0)
                    {
                        var difficultyText = allTextObjects[i];
                        difficultyText.text = $"{LocalizationSystem.GetLocalizedValue("difficulty_label")} {Loadout.difficultyLevel}"; // *should* be localized in the same way the game does it
                    }
                    text.gameObject.SetActive(i == 0);
                }

                // redo positioning so it comes up from the bottom left
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(50f+10f, 16f);
                rect.sizeDelta = new Vector2(100, 30f);
            }
            else statsPanel.Show();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CombatState), "Enter")]
        private static void CombatStateEnterPostPatch(ref GameController ___owner)
        {
            // we can't use PauseState.Exit because that will also disable things when going to settings
            if(statsPanel != null) statsPanel.Hide();
        }
    }
}
