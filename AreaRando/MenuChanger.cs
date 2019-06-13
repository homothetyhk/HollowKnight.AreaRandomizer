using System;
using AreaRando.Extensions;
using SeanprCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static AreaRando.LogHelper;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace AreaRando
{
    internal static class MenuChanger
    {
        public static void EditUI()
        {
            // Reset settings
            AreaRando.Instance.Settings = new SaveSettings();

            // Fetch data from vanilla screen
            MenuScreen playScreen = Ref.UI.playModeMenuScreen;

            playScreen.title.gameObject.transform.localPosition = new Vector3(0, 520.56f);

            Object.Destroy(playScreen.topFleur.gameObject);

            MenuButton classic = (MenuButton) playScreen.defaultHighlight;
            MenuButton steel = (MenuButton) classic.FindSelectableOnDown();
            MenuButton back = (MenuButton) steel.FindSelectableOnDown();

            GameObject parent = steel.transform.parent.gameObject;

            Object.Destroy(parent.GetComponent<VerticalLayoutGroup>());

            // Create new buttons
            MenuButton startRandoBtn = classic.Clone("StartRando", MenuButton.MenuButtonType.Proceed,
                new Vector2(0, 200), "Start Game", "Randomizer v2", AreaRando.GetSprite("UI.logo"));
            MenuButton startNormalBtn = classic.Clone("StartNormal", MenuButton.MenuButtonType.Proceed,
                new Vector2(0, -200), "Start Game", "Non-Randomizer");
            MenuButton startSteelRandoBtn = steel.Clone("StartSteelRando", MenuButton.MenuButtonType.Proceed,
                new Vector2(10000, 10000), "Steel Soul", "Randomizer v2", AreaRando.GetSprite("UI.logo2"));
            MenuButton startSteelNormalBtn = steel.Clone("StartSteelNormal", MenuButton.MenuButtonType.Proceed,
                new Vector2(10000, 10000), "Steel Soul", "Non-Randomizer");

            startNormalBtn.transform.localScale = startRandoBtn.transform.localScale =
                startSteelNormalBtn.transform.localScale =
                    startSteelRandoBtn.transform.localScale = new Vector2(0.75f, 0.75f);

            MenuButton backBtn = back.Clone("Back", MenuButton.MenuButtonType.Proceed, new Vector2(0, -100), "Back");

            /*
            RandoMenuItem<string> gameTypeBtn = new RandoMenuItem<string>(back, new Vector2(0, 600), "Game Type", "Normal", "Steel Soul");

            RandoMenuItem<string> presetPoolsBtn = new RandoMenuItem<string>(back, new Vector2(900, 1040), "Preset", "Progressive", "Completionist", "Junk Pit", "Custom");
            RandoMenuItem<bool> RandoDreamersBtn = new RandoMenuItem<bool>(back, new Vector2(900, 960), "Dreamers", true, false);
            RandoMenuItem<bool> RandoSkillsBtn = new RandoMenuItem<bool>(back, new Vector2(900, 880), "Skills", true, false);
            RandoMenuItem<bool> RandoCharmsBtn = new RandoMenuItem<bool>(back, new Vector2(900, 800), "Charms", true, false);
            RandoMenuItem<bool> RandoKeysBtn = new RandoMenuItem<bool>(back, new Vector2(900, 720), "Keys", true, false);
            RandoMenuItem<bool> RandoGeoChestsBtn = new RandoMenuItem<bool>(back, new Vector2(900, 640), "Geo Chests", false, true);
            RandoMenuItem<bool> RandoMaskBtn = new RandoMenuItem<bool>(back, new Vector2(900, 560), "Mask Shards", false, true);
            RandoMenuItem<bool> RandoVesselBtn = new RandoMenuItem<bool>(back, new Vector2(900, 480), "Vessel Fragments", false, true);
            RandoMenuItem<bool> RandoOreBtn = new RandoMenuItem<bool>(back, new Vector2(900, 400), "Pale Ore", false, true);
            RandoMenuItem<bool> RandoNotchBtn = new RandoMenuItem<bool>(back, new Vector2(900, 320), "Charm Notches", false, true);
            RandoMenuItem<bool> RandoEggBtn = new RandoMenuItem<bool>(back, new Vector2(900, 240), "Rancid Eggs", false, true);
            RandoMenuItem<bool> RandoRelicsBtn = new RandoMenuItem<bool>(back, new Vector2(900, 160), "Relics", false, true);
            RandoMenuItem<string> RandoLongItemsBtn = new RandoMenuItem<string>(back, new Vector2(900, 80), "Longest Check", "Standard", "Longer Items", "Longer Quests", "All Locations", "Fast");

            RandoMenuItem<string> presetSkipsBtn = new RandoMenuItem<string>(back, new Vector2(-900, 1040), "Preset", "Easy", "Hard", "Moglar", "Custom");
            RandoMenuItem<bool> shadeSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 960), "Shade Skips", false, true);
            RandoMenuItem<bool> acidSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 880), "Acid Skips", false, true);
            RandoMenuItem<bool> spikeTunnelsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 800), "Spike Tunnels", false, true);
            RandoMenuItem<bool> miscSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 720), "Misc Skips", false, true);
            RandoMenuItem<bool> fireballSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 640), "Fireball Skips", false, true);
            RandoMenuItem<bool> magolorBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 560), "Mag Skips", false, true);
            */
            RandoMenuItem<bool> charmNotchBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 300), "Salubra Notches", true, false);
            RandoMenuItem<bool> lemmBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 220), "Lemm Sell All", true, false);
            RandoMenuItem<bool> jijiBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 140), "Jiji Hints", true, false);
            RandoMenuItem<bool> PleasureHouseBtn = new RandoMenuItem<bool>(back, new Vector2(-900, 60), "Pleasure House Geo", true, false);
            RandoMenuItem<bool> EarlyGeoBtn = new RandoMenuItem<bool>(back, new Vector2(-900, -20), "Early Geo", true, false);
            /*
            RandoMenuItem<string> modeBtn = new RandoMenuItem<string>(back, new Vector2(0, 1040), "Mode", "Standard"); //, "No Claw", "All Bosses", "All Skills", "All Charms"
            */
            // Create seed entry field
            GameObject seedGameObject = back.Clone("Seed", MenuButton.MenuButtonType.Activate, new Vector2(0, 1130),
                "Click to type a custom seed").gameObject;
            Object.DestroyImmediate(seedGameObject.GetComponent<MenuButton>());
            Object.DestroyImmediate(seedGameObject.GetComponent<EventTrigger>());
            Object.DestroyImmediate(seedGameObject.transform.Find("Text").GetComponent<AutoLocalizeTextUI>());
            Object.DestroyImmediate(seedGameObject.transform.Find("Text").GetComponent<FixVerticalAlign>());
            Object.DestroyImmediate(seedGameObject.transform.Find("Text").GetComponent<ContentSizeFitter>());

            RectTransform seedRect = seedGameObject.transform.Find("Text").GetComponent<RectTransform>();
            seedRect.anchorMin = seedRect.anchorMax = new Vector2(0.5f, 0.5f);
            seedRect.sizeDelta = new Vector2(337, 63.2f);

            InputField customSeedInput = seedGameObject.AddComponent<InputField>();
            customSeedInput.transform.localPosition = new Vector3(0, 1240);
            customSeedInput.textComponent = seedGameObject.transform.Find("Text").GetComponent<Text>();

            AreaRando.Instance.Settings.Seed = new Random().Next(999999999);
            customSeedInput.text = AreaRando.Instance.Settings.Seed.ToString();

            customSeedInput.caretColor = Color.white;
            customSeedInput.contentType = InputField.ContentType.IntegerNumber;
            customSeedInput.onEndEdit.AddListener(ParseSeedInput);
            customSeedInput.navigation = Navigation.defaultNavigation;
            customSeedInput.caretWidth = 8;
            customSeedInput.characterLimit = 9;

            customSeedInput.colors = new ColorBlock
            {
                highlightedColor = Color.yellow,
                pressedColor = Color.red,
                disabledColor = Color.black,
                normalColor = Color.white,
                colorMultiplier = 2f
            };

            // Create some labels
            CreateLabel(back, new Vector2(-900, 1130), "Required Skips: Hard");
            CreateLabel(back, new Vector2(-900, 400), "Quality of Life");
            CreateLabel(back, new Vector2(900, 1130), "Randomization: Progressive");
            CreateLabel(back, new Vector2(0, 1300), "Seed:");

            // We don't need these old buttons anymore
            Object.Destroy(classic.gameObject);
            Object.Destroy(steel.gameObject);
            Object.Destroy(parent.FindGameObjectInChildren("GGButton"));
            Object.Destroy(back.gameObject);

            // Gotta put something here, we destroyed the old default
            playScreen.defaultHighlight = startRandoBtn;

            // Apply navigation info (up, right, down, left)
            /*
            startNormalBtn.SetNavigation(gameTypeBtn.Button, presetPoolsBtn.Button, backBtn, presetSkipsBtn.Button);
            startRandoBtn.SetNavigation(modeBtn.Button, presetPoolsBtn.Button, gameTypeBtn.Button, presetSkipsBtn.Button);
            startSteelNormalBtn.SetNavigation(gameTypeBtn.Button, presetPoolsBtn.Button, backBtn, presetSkipsBtn.Button);
            startSteelRandoBtn.SetNavigation(modeBtn.Button, presetPoolsBtn.Button, gameTypeBtn.Button, presetSkipsBtn.Button);
            modeBtn.Button.SetNavigation(backBtn, modeBtn.Button, startRandoBtn, modeBtn.Button);
            gameTypeBtn.Button.SetNavigation(startRandoBtn, presetPoolsBtn.Button, startNormalBtn, presetSkipsBtn.Button);
            backBtn.SetNavigation(startNormalBtn, backBtn, modeBtn.Button, backBtn);

            presetSkipsBtn.Button.SetNavigation(PleasureHouseBtn.Button, startRandoBtn, shadeSkipsBtn.Button, presetSkipsBtn.Button);
            shadeSkipsBtn.Button.SetNavigation(presetSkipsBtn.Button, startRandoBtn, acidSkipsBtn.Button, shadeSkipsBtn.Button);
            acidSkipsBtn.Button.SetNavigation(shadeSkipsBtn.Button, startRandoBtn, spikeTunnelsBtn.Button, acidSkipsBtn.Button);
            spikeTunnelsBtn.Button.SetNavigation(acidSkipsBtn.Button, startRandoBtn, miscSkipsBtn.Button, spikeTunnelsBtn.Button);
            miscSkipsBtn.Button.SetNavigation(spikeTunnelsBtn.Button, startRandoBtn, fireballSkipsBtn.Button, miscSkipsBtn.Button);
            fireballSkipsBtn.Button.SetNavigation(miscSkipsBtn.Button, startRandoBtn, magolorBtn.Button, fireballSkipsBtn.Button);
            magolorBtn.Button.SetNavigation(fireballSkipsBtn.Button, startRandoBtn, charmNotchBtn.Button, magolorBtn.Button);
            */

            charmNotchBtn.Button.SetNavigation(EarlyGeoBtn.Button, startRandoBtn, lemmBtn.Button, charmNotchBtn.Button);
            lemmBtn.Button.SetNavigation(charmNotchBtn.Button, startRandoBtn, jijiBtn.Button, lemmBtn.Button);
            jijiBtn.Button.SetNavigation(lemmBtn.Button, startRandoBtn, PleasureHouseBtn.Button, jijiBtn.Button);
            PleasureHouseBtn.Button.SetNavigation(jijiBtn.Button, startRandoBtn, EarlyGeoBtn.Button, PleasureHouseBtn.Button);
            EarlyGeoBtn.Button.SetNavigation(PleasureHouseBtn.Button, startRandoBtn, charmNotchBtn.Button, EarlyGeoBtn.Button);

            /*
            presetPoolsBtn.Button.SetNavigation(RandoLongItemsBtn.Button, presetPoolsBtn.Button, RandoDreamersBtn.Button, startRandoBtn);
            RandoDreamersBtn.Button.SetNavigation(presetPoolsBtn.Button, RandoDreamersBtn.Button, RandoSkillsBtn.Button, startRandoBtn);
            RandoSkillsBtn.Button.SetNavigation(RandoDreamersBtn.Button, RandoSkillsBtn.Button, RandoCharmsBtn.Button, startRandoBtn);
            RandoCharmsBtn.Button.SetNavigation(RandoSkillsBtn.Button, RandoCharmsBtn.Button, RandoKeysBtn.Button, startRandoBtn);
            RandoKeysBtn.Button.SetNavigation(RandoCharmsBtn.Button, RandoKeysBtn.Button, RandoGeoChestsBtn.Button, startRandoBtn);
            RandoGeoChestsBtn.Button.SetNavigation(RandoKeysBtn.Button, RandoGeoChestsBtn.Button, RandoMaskBtn.Button, startRandoBtn);
            RandoMaskBtn.Button.SetNavigation(RandoGeoChestsBtn.Button, RandoMaskBtn.Button, RandoVesselBtn.Button, startRandoBtn);
            RandoVesselBtn.Button.SetNavigation(RandoMaskBtn.Button, RandoVesselBtn.Button, RandoOreBtn.Button, startRandoBtn);
            RandoOreBtn.Button.SetNavigation(RandoVesselBtn.Button, RandoOreBtn.Button, RandoNotchBtn.Button, startRandoBtn);
            RandoNotchBtn.Button.SetNavigation(RandoOreBtn.Button, RandoNotchBtn.Button, RandoEggBtn.Button, startRandoBtn);
            RandoEggBtn.Button.SetNavigation(RandoNotchBtn.Button, RandoEggBtn.Button, RandoRelicsBtn.Button, startRandoBtn);
            RandoRelicsBtn.Button.SetNavigation(RandoEggBtn.Button, RandoRelicsBtn.Button, RandoLongItemsBtn.Button, startRandoBtn);
            RandoLongItemsBtn.Button.SetNavigation(RandoRelicsBtn.Button, RandoLongItemsBtn.Button, presetPoolsBtn.Button, startRandoBtn);

            // Setup event for changing difficulty settings buttons
            void UpdateSkipsButtons(RandoMenuItem<string> item)
            {
                switch (item.CurrentSelection)
                {
                    case "Easy":
                        SetShadeSkips(false);
                        acidSkipsBtn.SetSelection(false);
                        spikeTunnelsBtn.SetSelection(false);
                        miscSkipsBtn.SetSelection(false);
                        fireballSkipsBtn.SetSelection(false);
                        magolorBtn.SetSelection(false);
                        break;
                    case "Hard":
                        SetShadeSkips(true);
                        acidSkipsBtn.SetSelection(true);
                        spikeTunnelsBtn.SetSelection(true);
                        miscSkipsBtn.SetSelection(true);
                        fireballSkipsBtn.SetSelection(true);
                        magolorBtn.SetSelection(false);
                        break;
                    case "Moglar":
                        SetShadeSkips(true);
                        acidSkipsBtn.SetSelection(true);
                        spikeTunnelsBtn.SetSelection(true);
                        miscSkipsBtn.SetSelection(true);
                        fireballSkipsBtn.SetSelection(true);
                        magolorBtn.SetSelection(true);
                        break;
                    case "Custom":
                        item.SetSelection("Easy");
                        goto case "Easy";

                    default:
                        LogWarn("Unknown value in preset button: " + item.CurrentSelection);
                        break;
                }
            }
            void UpdatePoolPreset(RandoMenuItem<string> item)
            {
                switch (item.CurrentSelection)
                {
                    case "Progressive":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(false);
                        RandoMaskBtn.SetSelection(false);
                        RandoVesselBtn.SetSelection(false);
                        RandoOreBtn.SetSelection(false);
                        RandoNotchBtn.SetSelection(false);
                        RandoEggBtn.SetSelection(false);
                        RandoRelicsBtn.SetSelection(false);
                        RandoLongItemsBtn.SetSelection("Standard");
                        break;
                    case "Completionist":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(false);
                        RandoRelicsBtn.SetSelection(false);
                        RandoLongItemsBtn.SetSelection("Standard");
                        break;
                    case "Junk Pit":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoLongItemsBtn.SetSelection("Standard");
                        break;
                    case "Custom":
                        item.SetSelection("Progressive");
                        goto case "Progressive";
                }
            }

            // Event for setting stuff to hard mode on no claw selected
            void SetMode(RandoMenuItem<string> item)
            {
                if (item.CurrentSelection == "No Claw")
                {
                    SetShadeSkips(true);
                    acidSkipsBtn.SetSelection(true);
                    spikeTunnelsBtn.SetSelection(true);
                    miscSkipsBtn.SetSelection(true);
                    fireballSkipsBtn.SetSelection(true);

                    presetSkipsBtn.SetSelection(magolorBtn.CurrentSelection ? "Moglar" : "Hard");
                }
                else if (item.CurrentSelection.StartsWith("All"))
                {
                    presetPoolsBtn.SetSelection("Classic");
                }
            }

            void SetShadeSkips(bool enabled)
            {
                if (enabled)
                {
                    gameTypeBtn.SetSelection("Normal");
                    SwitchGameType(false);
                }
                else
                {
                    modeBtn.SetSelection("Standard");
                }

                shadeSkipsBtn.SetSelection(enabled);
            }

            void SkipsSettingChanged(RandoMenuItem<bool> item)
            {
                presetSkipsBtn.SetSelection("Custom");

                if (!item.CurrentSelection && item.Name != "Mag Skips")
                {
                    modeBtn.SetSelection("Standard");
                }
            }

            void PoolSettingChanged(RandoMenuItem<bool> item)
            {
                presetPoolsBtn.SetSelection("Custom");
            }

            presetSkipsBtn.Changed += UpdateSkipsButtons;
            presetPoolsBtn.Changed += UpdatePoolPreset;
            modeBtn.Changed += SetMode;

            shadeSkipsBtn.Changed += SkipsSettingChanged;
            shadeSkipsBtn.Changed += SaveShadeVal;
            acidSkipsBtn.Changed += SkipsSettingChanged;
            spikeTunnelsBtn.Changed += SkipsSettingChanged;
            miscSkipsBtn.Changed += SkipsSettingChanged;
            fireballSkipsBtn.Changed += SkipsSettingChanged;
            magolorBtn.Changed += SkipsSettingChanged;

            RandoDreamersBtn.Changed += PoolSettingChanged;
            RandoSkillsBtn.Changed += PoolSettingChanged;
            RandoCharmsBtn.Changed += PoolSettingChanged;
            RandoKeysBtn.Changed += PoolSettingChanged;
            RandoGeoChestsBtn.Changed += PoolSettingChanged;
            RandoMaskBtn.Changed += PoolSettingChanged;
            RandoVesselBtn.Changed += PoolSettingChanged;
            RandoOreBtn.Changed += PoolSettingChanged;
            RandoNotchBtn.Changed += PoolSettingChanged;
            RandoEggBtn.Changed += PoolSettingChanged;
            RandoRelicsBtn.Changed += PoolSettingChanged;

            // Setup game type button changes
            void SaveShadeVal(RandoMenuItem<bool> item)
            {
                SetShadeSkips(shadeSkipsBtn.CurrentSelection);
            }

            void SwitchGameType(bool steelMode)
            {
                if (!steelMode)
                {
                    // Normal mode
                    startRandoBtn.transform.localPosition = new Vector2(0, 200);
                    startNormalBtn.transform.localPosition = new Vector2(0, -200);
                    startSteelRandoBtn.transform.localPosition = new Vector2(10000, 10000);
                    startSteelNormalBtn.transform.localPosition = new Vector2(10000, 10000);

                    //backBtn.SetNavigation(startNormalBtn, startNormalBtn, modeBtn.Button, startRandoBtn);
                   //magolorBtn.Button.SetNavigation(fireballSkipsBtn.Button, gameTypeBtn.Button, startNormalBtn, lemmBtn.Button);
                    //lemmBtn.Button.SetNavigation(charmNotchBtn.Button, shadeSkipsBtn.Button, startRandoBtn, allSkillsBtn.Button);
                }
                else
                {
                    // Steel Soul mode
                    startRandoBtn.transform.localPosition = new Vector2(10000, 10000);
                    startNormalBtn.transform.localPosition = new Vector2(10000, 10000);
                    startSteelRandoBtn.transform.localPosition = new Vector2(0, 200);
                    startSteelNormalBtn.transform.localPosition = new Vector2(0, -200);

                    SetShadeSkips(false);

                    //backBtn.SetNavigation(startSteelNormalBtn, startSteelNormalBtn, modeBtn.Button, startSteelRandoBtn);
                    //magolorBtn.Button.SetNavigation(fireballSkipsBtn.Button, gameTypeBtn.Button, startSteelNormalBtn, lemmBtn.Button);
                    //lemmBtn.Button.SetNavigation(charmNotchBtn.Button, shadeSkipsBtn.Button, startSteelRandoBtn, allSkillsBtn.Button);
                }
            }
            
            gameTypeBtn.Button.AddEvent(EventTriggerType.Submit,
                garbage => SwitchGameType(gameTypeBtn.CurrentSelection != "Normal"));
                */
            // Setup start game button events
            void StartGame(bool rando)
            {
                AreaRando.Instance.Settings.CharmNotch = charmNotchBtn.CurrentSelection;
                AreaRando.Instance.Settings.Lemm = lemmBtn.CurrentSelection;
                AreaRando.Instance.Settings.Jiji = jijiBtn.CurrentSelection;
                AreaRando.Instance.Settings.PleasureHouse = PleasureHouseBtn.CurrentSelection;
                AreaRando.Instance.Settings.EarlyGeo = EarlyGeoBtn.CurrentSelection;

                AreaRando.Instance.Settings.RandomizeDreamers = true;
                AreaRando.Instance.Settings.RandomizeSkills = true;
                AreaRando.Instance.Settings.RandomizeCharms = true;
                AreaRando.Instance.Settings.RandomizeKeys = true;
                AreaRando.Instance.Settings.RandomizeGeoChests = false;
                AreaRando.Instance.Settings.RandomizeMaskShards = false;
                AreaRando.Instance.Settings.RandomizeVesselFragments = false;
                AreaRando.Instance.Settings.RandomizePaleOre = false;
                AreaRando.Instance.Settings.RandomizeCharmNotches = false;
                AreaRando.Instance.Settings.RandomizeRancidEggs = false;
                AreaRando.Instance.Settings.RandomizeRelics = false;

                AreaRando.Instance.Settings.LongItemTier = 1;
                /*
                if (RandoLongItemsBtn.CurrentSelection == "Standard") AreaRando.Instance.Settings.LongItemTier = 1;
                if (RandoLongItemsBtn.CurrentSelection == "Longer Items") AreaRando.Instance.Settings.LongItemTier = 2;
                if (RandoLongItemsBtn.CurrentSelection == "Longer Quests") AreaRando.Instance.Settings.LongItemTier = 3;
                if (RandoLongItemsBtn.CurrentSelection == "All Locations") AreaRando.Instance.Settings.LongItemTier = 4;
                if (RandoLongItemsBtn.CurrentSelection == "Fast") AreaRando.Instance.Settings.LongItemTier = 0;
                */

                AreaRando.Instance.Settings.Randomizer = rando;

                if (AreaRando.Instance.Settings.Randomizer)
                {
                    AreaRando.Instance.Settings.NoClaw = false;
                    AreaRando.Instance.Settings.AllBosses = false;
                    AreaRando.Instance.Settings.AllSkills = false;
                    AreaRando.Instance.Settings.AllCharms = false;

                    AreaRando.Instance.Settings.ShadeSkips = true;
                    AreaRando.Instance.Settings.AcidSkips = true;
                    AreaRando.Instance.Settings.SpikeTunnels = true;
                    AreaRando.Instance.Settings.MiscSkips = true;
                    AreaRando.Instance.Settings.FireballSkips = true;
                    AreaRando.Instance.Settings.MagSkips = false;
                }

                AreaRando.Instance.StartNewGame();
            }

            startNormalBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(false));
            startRandoBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(true));
            startSteelNormalBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(false));
            startSteelRandoBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(true));
        }

        private static void ParseSeedInput(string input)
        {
            if (int.TryParse(input, out int newSeed))
            {
                AreaRando.Instance.Settings.Seed = newSeed;
            }
            else
            {
                LogWarn($"Seed input \"{input}\" could not be parsed to an integer");
            }
        }

        private static void CreateLabel(MenuButton baseObj, Vector2 position, string text)
        {
            GameObject label = baseObj.Clone(text + "Label", MenuButton.MenuButtonType.Activate, position, text)
                .gameObject;
            Object.Destroy(label.GetComponent<EventTrigger>());
            Object.Destroy(label.GetComponent<MenuButton>());
        }

        private class RandoMenuItem<T> where T : IEquatable<T>
        {
            public delegate void RandoMenuItemChanged(RandoMenuItem<T> item);

            private readonly FixVerticalAlign _align;
            private readonly T[] _selections;
            private readonly Text _text;
            private int _currentSelection;

            public RandoMenuItem(MenuButton baseObj, Vector2 position, string name, params T[] values)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException(nameof(name));
                }

                if (baseObj == null)
                {
                    throw new ArgumentNullException(nameof(baseObj));
                }

                if (values == null || values.Length == 0)
                {
                    throw new ArgumentNullException(nameof(values));
                }

                _selections = values;
                Name = name;

                Button = baseObj.Clone(name + "Button", MenuButton.MenuButtonType.Activate, position, string.Empty);

                _text = Button.transform.Find("Text").GetComponent<Text>();
                _text.fontSize = 36;
                _align = Button.gameObject.GetComponentInChildren<FixVerticalAlign>(true);

                Button.ClearEvents();
                Button.AddEvent(EventTriggerType.Submit, GotoNext);

                RefreshText();
            }

            public T CurrentSelection => _selections[_currentSelection];

            public MenuButton Button { get; }

            public string Name { get; }

            public event RandoMenuItemChanged Changed
            {
                add => ChangedInternal += value;
                remove => ChangedInternal -= value;
            }

            private event RandoMenuItemChanged ChangedInternal;

            public void SetSelection(T obj)
            {
                for (int i = 0; i < _selections.Length; i++)
                {
                    if (_selections[i].Equals(obj))
                    {
                        _currentSelection = i;
                        break;
                    }
                }

                RefreshText(false);
            }

            private void GotoNext(BaseEventData data = null)
            {
                _currentSelection++;
                if (_currentSelection >= _selections.Length)
                {
                    _currentSelection = 0;
                }

                RefreshText();
            }

            private void RefreshText(bool invokeEvent = true)
            {
                _text.text = Name + ": " + _selections[_currentSelection];
                _align.AlignText();

                if (invokeEvent)
                {
                    ChangedInternal?.Invoke(this);
                }
            }
        }
    }
}