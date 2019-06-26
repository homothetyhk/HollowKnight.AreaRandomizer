using System;
using Random = System.Random;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Modding;
using AreaRando.Actions;
using AreaRando.Randomization;
using SeanprCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using AreaRando.Extensions;
using AreaRando.FsmStateActions;
using static WorldNavigation;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

using Object = UnityEngine.Object;

namespace AreaRando
{
    public class AreaRando : Mod
    {
        private static Dictionary<string, Sprite> _sprites;
        private static Dictionary<string, string> _secondaryBools;

        private static Thread _logicParseThread;

        public static AreaRando Instance { get; private set; }

        public SaveSettings Settings { get; set; } = new SaveSettings();

        public override ModSettings SaveSettings
        {
            get => Settings = Settings ?? new SaveSettings();
            set => Settings = value is SaveSettings saveSettings ? saveSettings : Settings;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloaded)
        {
            if (Instance != null)
            {
                LogWarn("Attempting to make multiple instances of mod, ignoring");
                return;
            }

            // Set instance for outside use
            Instance = this;

            // Make sure the play mode screen is always unlocked
            Ref.GM.EnablePermadeathMode();

            // Unlock godseeker too because idk why not
            Ref.GM.SetStatusRecordInt("RecBossRushMode", 1);

            // Load embedded resources
            _sprites = ResourceHelper.GetSprites("AreaRando.Resources.");

            Assembly randoDLL = GetType().Assembly;
            try
            {
                LanguageStringManager.LoadLanguageXML(
                    randoDLL.GetManifestResourceStream("AreaRando.Resources.language.xml"));
            }
            catch (Exception e)
            {
                LogError("Could not process language xml:\n" + e);
            }

            _logicParseThread = new Thread(LogicManager.ParseXML);
            _logicParseThread.Start(randoDLL.GetManifestResourceStream("AreaRando.Resources.items.xml"));

            // Add hooks
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += HandleSceneChanges;
            ModHooks.Instance.LanguageGetHook += LanguageStringManager.GetLanguageString;
            ModHooks.Instance.GetPlayerIntHook += IntOverride;
            ModHooks.Instance.GetPlayerBoolHook += BoolGetOverride;
            ModHooks.Instance.SetPlayerBoolHook += BoolSetOverride;
            On.PlayMakerFSM.OnEnable += FixVoidHeart;
            On.GameManager.BeginSceneTransition += RandomizeTransition;

            RandomizerAction.Hook();
            MiscSceneChanges.Hook();

            // Setup preloaded objects
            ObjectCache.GetPrefabs(preloaded[SceneNames.Tutorial_01]);

            // Some items have two bools for no reason, gotta deal with that
            _secondaryBools = new Dictionary<string, string>
            {
                {nameof(PlayerData.hasDash), nameof(PlayerData.canDash)},
                {nameof(PlayerData.hasShadowDash), nameof(PlayerData.canShadowDash)},
                {nameof(PlayerData.hasSuperDash), nameof(PlayerData.canSuperDash)},
                {nameof(PlayerData.hasWalljump), nameof(PlayerData.canWallJump)},
                {nameof(PlayerData.gotCharm_23), nameof(PlayerData.fragileHealth_unbreakable)},
                {nameof(PlayerData.gotCharm_24), nameof(PlayerData.fragileGreed_unbreakable)},
                {nameof(PlayerData.gotCharm_25), nameof(PlayerData.fragileStrength_unbreakable)}
            };


            // Marking unbreakable charms as secondary too to make shade skips viable

            MenuChanger.EditUI();
        }

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                (SceneNames.Tutorial_01, "_Props/Chest/Item/Shiny Item (1)"),
                (SceneNames.Tutorial_01, "_Enemies/Crawler 1"),
                (SceneNames.Tutorial_01, "_Props/Cave Spikes (1)")
            };
        }

        public static Sprite GetSprite(string name)
        {
            if (_sprites != null && _sprites.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }
            return null;
        }

        public static bool LoadComplete()
        {
            return _logicParseThread == null || !_logicParseThread.IsAlive;
        }

        public void StartNewGame()
        {
            // Charm tutorial popup is annoying, get rid of it
            Ref.PD.hasCharm = true;

            //Lantern start for easy mode
            if (!AreaRando.Instance.Settings.MiscSkips && !AreaRando.Instance.Settings.RandomizeKeys)
            {
                PlayerData.instance.hasLantern = true;
            }
            if (AreaRando.Instance.Settings.EarlyGeo)
            {
                PlayerData.instance.AddGeo(300);
            }


            // Fast boss intros
            Ref.PD.unchainedHollowKnight = true;
            Ref.PD.encounteredMimicSpider = true;
            Ref.PD.infectedKnightEncountered = true;
            Ref.PD.mageLordEncountered = true;
            Ref.PD.mageLordEncountered_2 = true;

            if (!Settings.Randomizer)
            {
                return;
            }

            if (!LoadComplete())
            {
                _logicParseThread.Join();
            }

            try
            {
                Randomizer.Randomize();
                RandomizerAction.CreateActions(Settings.ItemPlacements, Settings.Seed);
            }
            catch (Exception e)
            {
                LogError("Error in randomization:\n" + e);
            }
        }

        public override string GetVersion()
        {
            string ver = "1.02";
            int minAPI = 51;

            bool apiTooLow = Convert.ToInt32(ModHooks.Instance.ModVersion.Split('-')[1]) < minAPI;
            if (apiTooLow)
            {
                return ver + " (Update API)";
            }

            return ver;
        }

        private void UpdateCharmNotches(PlayerData pd)
        {
            // Update charm notches
            if (Settings.CharmNotch)
            {
                if (pd == null)
                {
                    return;
                }

                pd.CountCharms();
                int charms = pd.charmsOwned;
                int notches = pd.charmSlots;

                if (!pd.salubraNotch1 && charms >= 5)
                {
                    pd.SetBool(nameof(PlayerData.salubraNotch1), true);
                    notches++;
                }

                if (!pd.salubraNotch2 && charms >= 10)
                {
                    pd.SetBool(nameof(PlayerData.salubraNotch2), true);
                    notches++;
                }

                if (!pd.salubraNotch3 && charms >= 18)
                {
                    pd.SetBool(nameof(PlayerData.salubraNotch3), true);
                    notches++;
                }

                if (!pd.salubraNotch4 && charms >= 25)
                {
                    pd.SetBool(nameof(PlayerData.salubraNotch4), true);
                    notches++;
                }

                pd.SetInt(nameof(PlayerData.charmSlots), notches);
                Ref.GM.RefreshOvercharm();
            }
        }

        private bool BoolGetOverride(string boolName)
        {
            // Fake spell bools
            if (boolName == "hasVengefulSpirit")
            {
                return Ref.PD.fireballLevel > 0;
            }

            if (boolName == "hasShadeSoul")
            {
                return Ref.PD.fireballLevel > 1;
            }

            if (boolName == "hasDesolateDive")
            {
                return Ref.PD.quakeLevel > 0;
            }

            if (boolName == "hasDescendingDark")
            {
                return Ref.PD.quakeLevel > 1;
            }

            if (boolName == "hasHowlingWraiths")
            {
                return Ref.PD.screamLevel > 0;
            }

            if (boolName == "hasAbyssShriek")
            {
                return Ref.PD.screamLevel > 1;
            }

            // This variable is incredibly stubborn, not worth the effort to make it cooperate
            // Just override it completely
            if (boolName == nameof(PlayerData.gotSlyCharm) && Settings.Randomizer)
            {
                return Settings.SlyCharm;
            }

            if (boolName.StartsWith("AreaRando."))
            {
                return Settings.GetBool(false, boolName.Substring(10));
            }

            return Ref.PD.GetBoolInternal(boolName);
        }

        private void BoolSetOverride(string boolName, bool value)
        {
            PlayerData pd = Ref.PD;

            // It's just way easier if I can treat spells as bools
            if (boolName == "hasVengefulSpirit" && value && pd.fireballLevel <= 0)
            {
                pd.SetInt("fireballLevel", 1);
            }
            else if (boolName == "hasVengefulSpirit" && !value)
            {
                pd.SetInt("fireballLevel", 0);
            }
            else if (boolName == "hasShadeSoul" && value)
            {
                pd.SetInt("fireballLevel", 2);
            }
            else if (boolName == "hasShadeSoul" && !value && pd.fireballLevel >= 2)
            {
                pd.SetInt("fireballLevel", 1);
            }
            else if (boolName == "hasDesolateDive" && value && pd.quakeLevel <= 0)
            {
                pd.SetInt("quakeLevel", 1);
            }
            else if (boolName == "hasDesolateDive" && !value)
            {
                pd.SetInt("quakeLevel", 0);
            }
            else if (boolName == "hasDescendingDark" && value)
            {
                pd.SetInt("quakeLevel", 2);
            }
            else if (boolName == "hasDescendingDark" && !value && pd.quakeLevel >= 2)
            {
                pd.SetInt("quakeLevel", 1);
            }
            else if (boolName == "hasHowlingWraiths" && value && pd.screamLevel <= 0)
            {
                pd.SetInt("screamLevel", 1);
            }
            else if (boolName == "hasHowlingWraiths" && !value)
            {
                pd.SetInt("screamLevel", 0);
            }
            else if (boolName == "hasAbyssShriek" && value)
            {
                pd.SetInt("screamLevel", 2);
            }
            else if (boolName == "hasAbyssShriek" && !value && pd.screamLevel >= 2)
            {
                pd.SetInt("screamLevel", 1);
            }
            else if (boolName.StartsWith("AreaRando."))
            {
                boolName = boolName.Substring(10);
                if (boolName.StartsWith("ShopFireball"))
                {
                    pd.IncrementInt("fireballLevel");
                }
                else if (boolName.StartsWith("ShopQuake"))
                {
                    pd.IncrementInt("quakeLevel");
                }
                else if (boolName.StartsWith("ShopScream"))
                {
                    pd.IncrementInt("screamLevel");
                }
                else if (boolName.StartsWith("ShopDash"))
                {
                    pd.SetBool(pd.hasDash ? "hasShadowDash" : "hasDash", true);
                }
                else if (boolName.StartsWith("ShopDreamNail"))
                {
                    pd.SetBool(pd.hasDreamNail ? nameof(PlayerData.hasDreamGate) : nameof(PlayerData.hasDreamNail),
                        true);
                }
                else if (boolName.StartsWith("ShopKingsoul") || boolName.StartsWith("QueenFragment") || boolName.StartsWith("VoidHeart"))
                {
                    pd.SetBoolInternal("gotCharm_36", true);
                    if (pd.royalCharmState == 1) pd.SetInt("royalCharmState", 3);
                    else pd.IncrementInt("royalCharmState");
                    if (pd.royalCharmState == 4) pd.SetBoolInternal("gotShadeCharm", true);
                }
                else if (boolName.StartsWith("KingFragment"))
                {
                    pd.SetBoolInternal("gotCharm_36", true);
                    if (pd.royalCharmState == 0) pd.SetInt("royalCharmState", 2);
                    else if (pd.royalCharmState == 1) pd.SetInt("royalCharmState", 3);
                    else pd.IncrementInt("royalCharmState");
                    if (pd.royalCharmState == 4) pd.SetBoolInternal("gotShadeCharm", true);
                }
                else if (boolName.StartsWith("Lurien"))
                {
                    pd.SetBoolInternal("lurienDefeated", true);
                    pd.SetBoolInternal("maskBrokenLurien", true);
                    pd.IncrementInt("guardiansDefeated");
                    if (pd.guardiansDefeated == 1)
                    {
                        pd.SetBoolInternal("hornetFountainEncounter", true);
                        pd.SetBoolInternal("marmOutside", true);
                        pd.SetBoolInternal("crossroadsInfected", true);
                    }
                    if (pd.lurienDefeated && pd.hegemolDefeated && pd.monomonDefeated)
                    {
                        pd.SetBoolInternal("dungDefenderSleeping", true);
                        pd.SetInt("mrMushroomState", 1);
                        pd.IncrementInt("brettaState");
                    }
                }
                else if (boolName.StartsWith("Monomon"))
                {
                    pd.SetBoolInternal("monomonDefeated", true);
                    pd.SetBoolInternal("maskBrokenMonomon", true);
                    pd.IncrementInt("guardiansDefeated");
                    if (pd.guardiansDefeated == 1)
                    {
                        pd.SetBoolInternal("hornetFountainEncounter", true);
                        pd.SetBoolInternal("marmOutside", true);
                        pd.SetBoolInternal("crossroadsInfected", true);
                    }
                    if (pd.lurienDefeated && pd.hegemolDefeated && pd.monomonDefeated)
                    {
                        pd.SetBoolInternal("dungDefenderSleeping", true);
                        pd.SetInt("mrMushroomState", 1);
                        pd.IncrementInt("brettaState");
                    }
                }
                else if (boolName.StartsWith("Herrah"))
                {
                    pd.SetBoolInternal("hegemolDefeated", true);
                    pd.SetBoolInternal("maskBrokenHegemol", true);
                    pd.IncrementInt("guardiansDefeated");
                    if (pd.guardiansDefeated == 1)
                    {
                        pd.SetBoolInternal("hornetFountainEncounter", true);
                        pd.SetBoolInternal("marmOutside", true);
                        pd.SetBoolInternal("crossroadsInfected", true);
                    }
                    if (pd.lurienDefeated && pd.hegemolDefeated && pd.monomonDefeated)
                    {
                        pd.SetBoolInternal("dungDefenderSleeping", true);
                        pd.SetInt("mrMushroomState", 1);
                        pd.IncrementInt("brettaState");
                    }
                }
                else if (boolName.StartsWith("BasinSimpleKey") || boolName.StartsWith("CitySimpleKey") || boolName.StartsWith("SlySimpleKey") || boolName.StartsWith("LurkerSimpleKey"))
                {
                    pd.IncrementInt("simpleKeys");
                }
                else if (boolName.StartsWith("hasWhite") || boolName.StartsWith("hasLove") || boolName.StartsWith("hasSly")) pd.SetBoolInternal(boolName, true);
                else if (boolName.StartsWith("MaskShard"))
                {
                    pd.SetBoolInternal("heartPieceCollected", true);
                    if (PlayerData.instance.heartPieces < 3) GameManager.instance.IncrementPlayerDataInt("heartPieces");
                    else
                    {
                        HeroController.instance.AddToMaxHealth(1);
                        if (PlayerData.instance.maxHealthBase < PlayerData.instance.maxHealthCap) PlayerData.instance.SetIntInternal("heartPieces", 0);
                        PlayMakerFSM.BroadcastEvent("MAX HP UP");
                    }
                }
                else if (boolName.StartsWith("VesselFragment"))
                {
                    pd.SetBoolInternal("vesselFragmentCollected", true);
                    if (PlayerData.instance.vesselFragments < 2) GameManager.instance.IncrementPlayerDataInt("vesselFragments");
                    else
                    {
                        HeroController.instance.AddToMaxMPReserve(33);
                        if (PlayerData.instance.MPReserveMax < PlayerData.instance.MPReserveCap) PlayerData.instance.SetIntInternal("vesselFragments", 0);
                        PlayMakerFSM.BroadcastEvent("NEW SOUL ORB");
                    }
                }
                else if (boolName.StartsWith("PaleOre"))
                {
                    pd.IncrementInt("ore");
                }
                else if (boolName.StartsWith("CharmNotch"))
                {
                    pd.IncrementInt("charmSlots");
                }
                else if (boolName.StartsWith("RancidEgg"))
                {
                    pd.IncrementInt("rancidEggs");
                }
                else if (boolName.StartsWith("WanderersJournal"))
                {
                    pd.IncrementInt("trinket1");
                    pd.SetBoolInternal("foundTrinket1", true);
                }
                else if (boolName.StartsWith("HallownestSeal"))
                {
                    pd.IncrementInt("trinket2");
                    pd.SetBoolInternal("foundTrinket2", true);
                }
                else if (boolName.StartsWith("KingsIdol"))
                {
                    pd.IncrementInt("trinket3");
                    pd.SetBoolInternal("foundTrinket3", true);
                }
                else if (boolName.StartsWith("ArcaneEgg"))
                {
                    pd.IncrementInt("trinket4");
                    pd.SetBoolInternal("foundTrinket4", true);
                }
                Settings.SetBool(value, boolName);
                return;
            }
            // Send the set through to the actual set
            pd.SetBoolInternal(boolName, value);

            // Check if there is a secondary bool for this item
            if (_secondaryBools.TryGetValue(boolName, out string secondaryBoolName))
            {
                pd.SetBool(secondaryBoolName, value);
            }

            if (boolName == nameof(PlayerData.hasCyclone) || boolName == nameof(PlayerData.hasUpwardSlash) ||
                boolName == nameof(PlayerData.hasDashSlash))
            {
                // Make nail arts work
                bool hasCyclone = pd.GetBool(nameof(PlayerData.hasCyclone));
                bool hasUpwardSlash = pd.GetBool(nameof(PlayerData.hasUpwardSlash));
                bool hasDashSlash = pd.GetBool(nameof(PlayerData.hasDashSlash));

                pd.SetBool(nameof(PlayerData.hasNailArt), hasCyclone || hasUpwardSlash || hasDashSlash);
                pd.SetBool(nameof(PlayerData.hasAllNailArts), hasCyclone && hasUpwardSlash && hasDashSlash);
            }
            else if (boolName == nameof(PlayerData.hasDreamGate) && value)
            {
                // Make sure the player can actually use dream gate after getting it
                FSMUtility.LocateFSM(Ref.Hero.gameObject, "Dream Nail").FsmVariables
                    .GetFsmBool("Dream Warp Allowed").Value = true;
            }
            else if (boolName == nameof(PlayerData.hasAcidArmour) && value)
            {
                // Gotta update the acid pools after getting this
                PlayMakerFSM.BroadcastEvent("GET ACID ARMOUR");
            }
            else if (boolName.StartsWith("gotCharm_"))
            {
                // Check for Salubra notches if it's a charm
                UpdateCharmNotches(pd);
            }
        }

        private int IntOverride(string intName)
        {
            if (intName == "AreaRando.Zero")
            {
                return 0;
            }

            return Ref.PD.GetIntInternal(intName);
        }

        private void FixVoidHeart(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            // Normal shade and sibling AI
            if ((self.FsmName == "Control" && self.gameObject.name.StartsWith("Shade Sibling")) || (self.FsmName == "Shade Control" && self.gameObject.name.StartsWith("Hollow Shade")))
            {
                self.FsmVariables.FindFsmBool("Friendly").Value = false;
                self.GetState("Pause").ClearTransitions();
                self.GetState("Pause").AddTransition("FINISHED", "Init");
            }
            // Make Void Heart unequippable
            else if (self.FsmName == "UI Charms" && self.gameObject.name == "Charms")
            {
                self.GetState("Equipped?").RemoveTransitionsTo("Black Charm? 2");
                self.GetState("Equipped?").AddTransition("EQUIPPED", "Return Points");
                self.GetState("Set Current Item Num").RemoveTransitionsTo("Black Charm?");
                self.GetState("Set Current Item Num").AddTransition("FINISHED", "Return Points");
            }
        }
        private static void RandomizeTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            if (string.IsNullOrEmpty(info.EntryGateName) || string.IsNullOrEmpty(info.SceneName))
            {
                orig(self, info);
                return;
            }

            TransitionPoint tp = Object.FindObjectsOfType<TransitionPoint>().FirstOrDefault(x => x.entryPoint == info.EntryGateName && x.targetScene == info.SceneName);
            string transitionName = string.Empty;

            if (tp == null)
            {
                if (self.sceneName == SceneNames.Fungus3_44 && info.EntryGateName == "left1") transitionName = self.sceneName + "[door1]";
                else if (self.sceneName == SceneNames.Crossroads_02 && info.EntryGateName == "left1") transitionName = self.sceneName + "[door1]";
                else if (self.sceneName == SceneNames.Crossroads_06 && info.EntryGateName == "left1") transitionName = self.sceneName + "[door1]";
                else if (self.sceneName == SceneNames.Deepnest_10 && info.EntryGateName == "left1") transitionName = self.sceneName + "[door1]";
                else
                {
                    orig(self, info);
                    return;
                }
            }
            else
            {
                string name = tp.name.Split(null).First();
                transitionName = self.sceneName + "[" + name + "]";
            }

            if (Instance.Settings._transitionPlacements.TryGetValue(transitionName, out string destination))
            {
                info.SceneName = LogicManager.GetTransitionDef(destination).sceneName;
                info.EntryGateName = LogicManager.GetTransitionDef(destination).doorName;
            }
            orig(self, info);
        }

        private void HandleSceneChanges(Scene from, Scene to)
        {
            if (Ref.GM.GetSceneNameString() == SceneNames.Menu_Title)
            {
                // Reset settings on menu load
                Settings = new SaveSettings();
                RandomizerAction.ClearActions();
                
                try
                {
                    MenuChanger.EditUI();
                }
                catch (Exception e)
                {
                    LogError("Error editing menu:\n" + e);
                }
            }
            if (Ref.GM.IsGameplayScene())
            {
                try
                {
                    // In rare cases, this is called before the previous scene has unloaded
                    // Deleting old randomizer shinies to prevent issues
                    GameObject oldShiny = GameObject.Find("Randomizer Shiny");
                    if (oldShiny != null)
                    {
                        Object.DestroyImmediate(oldShiny);
                    }

                    RandomizerAction.EditShinies();
                }
                catch (Exception e)
                {
                    LogError($"Error applying RandomizerActions to scene {to.name}:\n" + e);
                }
            }

            try
            {
                RestrictionManager.SceneChanged(to);
                MiscSceneChanges.SceneChanged(to);
            }
            catch (Exception e)
            {
                LogError($"Error applying changes to scene {to.name}:\n" + e);
            }
        }
    }
}