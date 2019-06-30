using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using AreaRando.Components;
using AreaRando.FsmStateActions;
using SeanprCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace AreaRando
{
    internal static class MiscSceneChanges
    {
        private static readonly Random Rnd = new Random();
        private static int _rndNum;

        public static void Hook()
        {
            UnHook();

            ModHooks.Instance.ObjectPoolSpawnHook += FixExplosionPogo;
            On.EnemyHitEffectsArmoured.RecieveHitEffect += FalseKnightNoises;
            On.PlayMakerFSM.OnEnable += ModifyFSM;
        }

        public static void UnHook()
        {
            ModHooks.Instance.ObjectPoolSpawnHook -= FixExplosionPogo;
            On.EnemyHitEffectsArmoured.RecieveHitEffect -= FalseKnightNoises;
            On.PlayMakerFSM.OnEnable -= ModifyFSM;
        }

        public static void SceneChanged(Scene newScene)
        {
            RecalculateRandom();

            ApplyGeneralChanges(newScene);

            if (AreaRando.Instance.Settings.Randomizer)
            {
                ApplyRandomizerChanges(newScene);
            }

            if (AreaRando.Instance.Settings.NoClaw)
            {
                ApplyNoClawChanges(newScene);
            }
        }

        private static void RecalculateRandom()
        {
            _rndNum = Rnd.Next(25);
        }

        private static void ApplyRandomizerChanges(Scene newScene)
        {
            string sceneName = newScene.name;

            // Remove quake floors in Soul Sanctum to prevent soft locks
            if (Ref.PD.quakeLevel <= 0 && Ref.PD.killedMageLord &&
                (sceneName == SceneNames.Ruins1_23 || sceneName == SceneNames.Ruins1_30 ||
                 sceneName == SceneNames.Ruins1_32))
            {
                Ref.PD.SetBool(nameof(PlayerData.brokenMageWindow), true);
                Ref.PD.SetBool(nameof(PlayerData.brokenMageWindowGlass), true);

                foreach (GameObject obj in newScene.GetRootGameObjects())
                {
                    if (obj.name.Contains("Quake Floor"))
                    {
                        Object.Destroy(obj);
                    }
                }
            }

            // Make baldurs always able to spit rollers
            if (sceneName == SceneNames.Crossroads_11_alt || sceneName == SceneNames.Crossroads_ShamanTemple ||
                sceneName == SceneNames.Fungus1_28)
            {
                foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
                {
                    if (obj.name.Contains("Blocker"))
                    {
                        PlayMakerFSM fsm = FSMUtility.LocateFSM(obj, "Blocker Control");
                        if (fsm != null)
                        {
                            fsm.GetState("Can Roller?").RemoveActionsOfType<IntCompare>();
                        }
                    }
                }
            }

            switch (sceneName)
            {
                case SceneNames.Room_Ouiji:
                    if (PlayerData.instance.shadeScene != "None")
                    {
                        PlayMakerFSM jijiFsm = GameObject.Find("Jiji NPC").LocateMyFSM("Conversation Control");
                        FsmState HasShade = jijiFsm.GetState("Has Shade?");
                        HasShade.RemoveTransitionsTo("Check Location");
                        HasShade.AddTransition("YES", "Offer");
                    }
                    else if (AreaRando.Instance.Settings.Jiji)
                    {
                        PlayerData.instance.SetString("shadeMapZone", "HIVE");
                        PlayMakerFSM jijiFsm = GameObject.Find("Jiji NPC").LocateMyFSM("Conversation Control");
                        FsmState BoxUp = jijiFsm.GetState("Box Up");
                        BoxUp.ClearTransitions();
                        BoxUp.AddFirstAction(jijiFsm.GetState("Convo Choice").GetActionsOfType<GetPlayerDataInt>()[0]);
                        BoxUp.AddTransition("FINISHED", "Offer");
                        FsmState SendText = jijiFsm.GetState("Send Text");
                        SendText.RemoveTransitionsTo("Yes");
                        SendText.AddTransition("YES", "Check Location");
                        FsmState CheckLocation = jijiFsm.GetState("Check Location");
                        CheckLocation.AddFirstAction(BoxUp.GetActionsOfType<SendEventByName>()[0]);
                        CheckLocation.AddFirstAction(jijiFsm.GetState("Convo Choice").GetActionsOfType<GetPlayerDataInt>()[0]);
                        CheckLocation.AddFirstAction(jijiFsm.GetState("Yes").GetActionsOfType<PlayerDataIntAdd>()[0]);
                        CheckLocation.AddFirstAction(jijiFsm.GetState("Yes").GetActionsOfType<SendEventByName>()[0]);
                    }
                    break;
                case SceneNames.Grimm_Main_Tent:
                    PlayerData.instance.metGrimm = true;
                    break;
                case SceneNames.Room_Final_Boss_Atrium:
                    if (AreaRando.Instance.Settings.RandomizeDreamers)
                    {
                        GameObject.Find("Tut_tablet_top").LocateMyFSM("Inspection").GetState("Init").ClearTransitions();
                    }
                    break;
                case SceneNames.Abyss_04:
                    if (AreaRando.Instance.Settings.RandomizeVesselFragments)
                    {
                        Object.Destroy(GameObject.Find("Fountain Donation"));
                    }
                    break;
                case SceneNames.Abyss_06_Core:
                    // Opens door to LBC
                    if (PlayerData.instance.healthBlue > 0 || PlayerData.instance.joniHealthBlue > 0 || GameManager.instance.entryGateName == "left1")
                    {
                        PlayerData.instance.SetBoolInternal("blueVineDoor", true);
                        PlayMakerFSM BlueDoorFSM = GameObject.Find("Blue Door").LocateMyFSM("Control");
                        BlueDoorFSM.GetState("Init").RemoveTransitionsTo("Got Charm");
                    }
                    break;
                case SceneNames.Abyss_12:
                    // Destroy shriek pickup if the player doesn't have wraiths
                    if (Ref.PD.screamLevel == 0)
                    {
                        Object.Destroy(GameObject.Find("Randomizer Shiny"));
                    }
                    break;
                case SceneNames.Abyss_15:
                    GameObject.Find("Dream Enter Abyss").LocateMyFSM("Control").GetState("Init").RemoveTransitionsTo("Idle");
                    GameObject.Find("Dream Enter Abyss").LocateMyFSM("Control").GetState("Init").AddTransition("FINISHED", "Inactive");
                    if (!PlayerData.instance.hasDreamNail) Object.Destroy(GameObject.Find("New Shiny"));
                    break;
                case SceneNames.GG_Waterways:
                    PlayerData.instance.SetBool("godseekerUnlocked", true);
                    if (PlayerData.instance.simpleKeys < 1 || (!PlayerData.instance.openedWaterwaysManhole && PlayerData.instance.simpleKeys < 2)) Object.Destroy(GameObject.Find("Randomizer Shiny"));
                    else GameObject.Find("Randomizer Shiny").LocateMyFSM("Shiny Control").GetState("Charm?").AddFirstAction(new RandomizerExecuteLambda(() => PlayerData.instance.DecrementInt("simpleKeys")));
                    break;
                case SceneNames.Crossroads_09:
                    // Mawlek shard
                    if (AreaRando.Instance.Settings.RandomizeMaskShards) Object.Destroy(GameObject.Find("Heart Piece"));
                    break;
                case SceneNames.Crossroads_38:

                    if (AreaRando.Instance.Settings.RandomizeMaskShards) Object.Destroy(GameObject.Find("Reward 5"));

                    if (AreaRando.Instance.Settings.RandomizeCharms)
                    {
                        Object.Destroy(GameObject.Find("Reward 10"));
                        Object.Destroy(GameObject.Find("Reward 46"));
                    }

                    if (AreaRando.Instance.Settings.RandomizeRancidEggs) Object.Destroy(GameObject.Find("Reward 16"));

                    if (AreaRando.Instance.Settings.RandomizeRelics)
                    {
                        Object.Destroy(GameObject.Find("Reward 23"));
                        Object.Destroy(GameObject.Find("Reward 38"));
                    }

                    if (AreaRando.Instance.Settings.RandomizePaleOre) Object.Destroy(GameObject.Find("Reward 31"));

                    break;
                case SceneNames.Deepnest_East_02:
                    if (GameManager.instance.entryGateName.StartsWith("bot2"))
                    {
                        Object.Destroy(GameObject.Find("Quake Floor").FindGameObjectInChildren("Active").FindGameObjectInChildren("msk_generic"));
                        Object.Destroy(GameObject.Find("Quake Floor").FindGameObjectInChildren("Active").FindGameObjectInChildren("msk_generic (1)"));
                        Object.Destroy(GameObject.Find("Quake Floor").FindGameObjectInChildren("Active").FindGameObjectInChildren("msk_generic (2)"));
                        Object.Destroy(GameObject.Find("Quake Floor").FindGameObjectInChildren("Active").FindGameObjectInChildren("msk_generic (3)"));
                    }
                    break;
                case SceneNames.Crossroads_ShamanTemple:
                    // Remove gate in shaman hut
                    Object.Destroy(GameObject.Find("Bone Gate"));

                    // Add hard save to shaman shiny
                    FSMUtility.LocateFSM(GameObject.Find("Randomizer Shiny"), "Shiny Control").GetState("Finish")
                        .AddAction(new RandomizerSetHardSave());

                    break;
                case SceneNames.Deepnest_Spider_Town:
                    // Make it so the first part of Beast's Den can't become inaccessible
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Deepnest_Spider_Town",
                        id = "Collapser Small (12)",
                        activated = true,
                        semiPersistent = false
                    });
                    if (AreaRando.Instance.Settings.RandomizeDreamers)
                    {
                        Object.Destroy(GameObject.Find("Dreamer Hegemol"));
                        Object.Destroy(GameObject.Find("Dream Enter"));
                        Object.Destroy(GameObject.Find("Dream Impact"));
                        Object.Destroy(GameObject.Find("Shield"));
                        if (!PlayerData.instance.hasDreamNail) Object.Destroy(GameObject.Find("New Shiny"));
                    }
                    break;
                case SceneNames.Dream_Nailcollection:
                    // Make picking up shiny load new scene
                    FSMUtility.LocateFSM(GameObject.Find("Randomizer Shiny"), "Shiny Control").GetState("Finish")
                        .AddAction(new RandomizerChangeScene("RestingGrounds_07", "right1"));
                    break;
                case SceneNames.Fungus2_15:
                    if (GameManager.instance.entryGateName.StartsWith("left"))
                    {
                        Object.Destroy(GameObject.Find("deepnest_mantis_gate").FindGameObjectInChildren("Collider"));
                        Object.Destroy(GameObject.Find("deepnest_mantis_gate"));
                    }
                    break;
                case SceneNames.Fungus2_21:
                    // Make city crest gate openable infinite times and not hard save
                    FSMUtility.LocateFSM(GameObject.Find("City Gate Control"), "Conversation Control")
                        .GetState("Activate").RemoveActionsOfType<SetPlayerDataBool>();

                    FsmState gateSlam = FSMUtility.LocateFSM(GameObject.Find("Ruins_gate_main"), "Open")
                        .GetState("Slam");
                    gateSlam.RemoveActionsOfType<SetPlayerDataBool>();
                    gateSlam.RemoveActionsOfType<CallMethodProper>();
                    gateSlam.RemoveActionsOfType<SendMessage>();
                    break;
                case SceneNames.Fungus2_25:
                    if (GameManager.instance.entryGateName.StartsWith("right"))
                    {
                        Object.Destroy(GameObject.Find("mantis_big_door"));
                    }
                    break;
                case SceneNames.Fungus2_26:
                    // Prevent leg eater from doing anything but opening the shop
                    PlayMakerFSM legEater = FSMUtility.LocateFSM(GameObject.Find("Leg Eater"), "Conversation Control");
                    FsmState legEaterChoice = legEater.GetState("Convo Choice");
                    legEaterChoice.RemoveTransitionsTo("Convo 1");
                    legEaterChoice.RemoveTransitionsTo("Convo 2");
                    legEaterChoice.RemoveTransitionsTo("Convo 3");
                    legEaterChoice.RemoveTransitionsTo("Infected Crossroad");
                    legEaterChoice.RemoveTransitionsTo("Bought Charm");
                    legEaterChoice.RemoveTransitionsTo("Gold Convo");
                    legEaterChoice.RemoveTransitionsTo("All Gold");
                    legEaterChoice.RemoveTransitionsTo("Ready To Leave");
                    legEater.GetState("All Gold?").RemoveTransitionsTo("No Shop");

                    // Just in case something other than the "Ready To Leave" state controls this
                    Ref.PD.legEaterLeft = false;
                    break;
                case SceneNames.Fungus3_archive_02:
                    if (AreaRando.Instance.Settings.RandomizeDreamers)
                    {
                        PlayerData.instance.SetBool("summonedMonomon", true);
                        Object.Destroy(GameObject.Find("Inspect Region"));
                        Object.Destroy(GameObject.Find("Quirrel Wounded"));
                        Object.Destroy(GameObject.Find("Quirrel"));
                        Object.Destroy(GameObject.Find("Monomon"));
                        Object.Destroy(GameObject.Find("Dream Enter"));
                        Object.Destroy(GameObject.Find("Dream Impact"));
                        Object.Destroy(GameObject.Find("Shield"));
                        if (!PlayerData.instance.hasDreamNail) Object.Destroy(GameObject.Find("New Shiny"));
                    }
                    break;
                case SceneNames.Fungus3_44:
                        GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                        {
                            sceneName = "Fungus3_44",
                            id = "Secret Mask",
                            activated = true,
                            semiPersistent = false
                        });
                    break;
                case SceneNames.Mines_33:
                    // Make tolls always interactable
                    if (AreaRando.Instance.Settings.MiscSkips && !AreaRando.Instance.Settings.RandomizeKeys)
                    {
                        GameObject[] tolls = new GameObject[] { GameObject.Find("Toll Gate Machine"), GameObject.Find("Toll Gate Machine (1)") };
                        foreach (GameObject toll in tolls)
                        {
                            Object.Destroy(FSMUtility.LocateFSM(toll, "Disable if No Lantern"));
                        }
                    }
                    break;
                case SceneNames.RestingGrounds_07:
                    // Make Moth NPC not give items since those are now shinies
                    PlayMakerFSM moth = FSMUtility.LocateFSM(GameObject.Find("Dream Moth"), "Conversation Control");
                    if (AreaRando.Instance.Settings.RandomizeRelics)
                    {
                        PlayerData.instance.dreamReward1 = true;
                        moth.FsmVariables.GetFsmBool("Got Reward 1").Value = true;
                        PlayerData.instance.dreamReward6 = true;
                        moth.FsmVariables.GetFsmBool("Got Reward 6").Value = true;
                    }
                    if (AreaRando.Instance.Settings.RandomizePaleOre)
                    {
                        PlayerData.instance.dreamReward3 = true;
                        moth.FsmVariables.GetFsmBool("Got Reward 3").Value = true;
                    }
                    if (AreaRando.Instance.Settings.RandomizeCharms)
                    {
                        PlayerData.instance.dreamReward4 = true;
                        moth.FsmVariables.GetFsmBool("Got Reward 4").Value = true;
                    }
                    if (AreaRando.Instance.Settings.RandomizeVesselFragments)
                    {
                        PlayerData.instance.dreamReward5 = true;
                        moth.FsmVariables.GetFsmBool("Got Reward 5").Value = true;
                    }
                    if (AreaRando.Instance.Settings.RandomizeSkills)
                    {
                        PlayerData.instance.dreamReward5b = true;
                        moth.FsmVariables.GetFsmBool("Got Reward 5b").Value = true;
                    }
                    if (AreaRando.Instance.Settings.RandomizeMaskShards)
                    {
                        PlayerData.instance.dreamReward7 = true;
                        moth.FsmVariables.GetFsmBool("Got Reward 7").Value = true;
                    }
                    break;
                case SceneNames.Room_Sly_Storeroom:
                    // Make Sly pickup send Sly back upstairs
                    FsmState slyFinish = FSMUtility.LocateFSM(GameObject.Find("Randomizer Shiny"), "Shiny Control")
                        .GetState("Finish");
                    slyFinish.AddAction(new RandomizerSetBool("SlyCharm", true));

                    // The game breaks if you leave the storeroom after this, so just send the player out of the shop completely
                    // People will think it's an intentional feature to cut out pointless walking anyway
                    slyFinish.AddAction(new RandomizerChangeScene("Town", "door_sly"));
                    break;

                case SceneNames.Ruins1_05:
                    // Slight adjustment to breakable so wings is enough to progress, just like on old patches
                    GameObject chandelier = GameObject.Find("ruind_dressing_light_02 (10)");
                    chandelier.transform.SetPositionX(chandelier.transform.position.x - 2);
                    chandelier.GetComponent<NonBouncer>().active = false;

                    break;
                case SceneNames.Ruins1_09:
                    if (GameManager.instance.entryGateName.StartsWith("t"))
                    {
                        Object.Destroy(GameObject.Find("Battle Gate"));
                        Object.Destroy(GameObject.Find("Battle Scene"));
                    }
                    break;
                case SceneNames.Ruins1_24:
                    // Pickup (Quake Pickup) -> Idle -> GetPlayerDataInt (quakeLevel)
                    // Quake (Quake Item) -> Get -> SetPlayerDataInt (quakeLevel)
                    // Stop spell container from destroying itself
                    PlayMakerFSM quakePickup = FSMUtility.LocateFSM(GameObject.Find("Quake Pickup"), "Pickup");
                    quakePickup.GetState("Idle").RemoveActionsOfType<IntCompare>();
                    foreach (PlayMakerFSM childFSM in quakePickup.gameObject.GetComponentsInChildren<PlayMakerFSM>(true)
                    )
                    {
                        if (childFSM.FsmName == "Shiny Control")
                        {
                            // Make spell container spawn shiny instead
                            quakePickup.GetState("Appear").GetActionsOfType<ActivateGameObject>()[1].gameObject
                                .GameObject.Value = childFSM.gameObject;

                            // Make shiny open gates on pickup/destroy
                            SendEvent openGate = new SendEvent
                            {
                                eventTarget = new FsmEventTarget
                                {
                                    target = FsmEventTarget.EventTarget.BroadcastAll,
                                    excludeSelf = true
                                },
                                sendEvent = FsmEvent.FindEvent("BG OPEN"),
                                delay = 0,
                                everyFrame = false
                            };
                            childFSM.GetState("Destroy").AddFirstAction(openGate);
                            childFSM.GetState("Finish").AddFirstAction(openGate);

                            // Add hard save after picking up item
                            childFSM.GetState("Finish").AddFirstAction(new RandomizerSetHardSave());

                            break;
                        }
                    }

                    // Stop the weird invisible floor from appearing if dive has been obtained
                    // I don't think it really serves any purpose, so destroying it should be fine
                    if (Ref.PD.quakeLevel > 0)
                    {
                        Object.Destroy(GameObject.Find("Roof Collider Battle"));
                    }

                    // Change battle gate to be destroyed if Soul Master is dead instead of it the player has quake
                    FsmState checkQuake = FSMUtility.LocateFSM(GameObject.Find("Battle Gate (1)"), "Destroy if Quake")
                        .GetState("Check");
                    checkQuake.RemoveActionsOfType<FsmStateAction>();
                    checkQuake.AddAction(new RandomizerBoolTest(nameof(PlayerData.killedMageLord), null, "DESTROY",
                        true));
                    break;
                case SceneNames.Ruins1_32 when !Ref.PD.hasWalljump:
                    // Platform after soul master
                    GameObject plat = Object.Instantiate(GameObject.Find("ruind_int_plat_float_02 (3)"));
                    plat.SetActive(true);
                    plat.transform.position = new Vector2(40.5f, 72f);
                    break;
                case SceneNames.Ruins2_04:
                    // Shield husk doesn't walk as far as on old patches, making something pogoable to make up for this
                    GameObject.Find("Direction Pole White Palace").GetComponent<NonBouncer>().active = false;

                    // Prevent simple key softlocks
                    FsmState hotSpringsKey = GameObject.Find("Inspect").LocateMyFSM("Conversation Control").GetState("Got Key?");
                    hotSpringsKey.RemoveActionsOfType<IntCompare>();
                    hotSpringsKey.AddAction(new RandomizerExecuteLambda(() =>
                    {
                        if (GameManager.instance.GetPlayerDataInt("simpleKeys") > 1 || (PlayerData.instance.openedWaterwaysManhole && GameManager.instance.GetPlayerDataInt("simpleKeys") > 0)) PlayMakerFSM.BroadcastEvent("YES");
                        else PlayMakerFSM.BroadcastEvent("NO");
                    }));
                    break;
                case SceneNames.Ruins2_11:
                    // Prevent the jars below Collector from being permanently destroyed
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (1)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (2)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (3)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (4)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (5)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (6)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (7)",
                        activated = false,
                        semiPersistent = false
                    });
                    GameManager.instance.sceneData.SaveMyState(new PersistentBoolData
                    {
                        sceneName = "Ruins2_11",
                        id = "Break Jar (8)",
                        activated = false,
                        semiPersistent = false
                    });
                    break;
                case SceneNames.Ruins2_Watcher_Room:
                    if (AreaRando.Instance.Settings.RandomizeDreamers)
                    {
                        Object.Destroy(GameObject.Find("Dreamer Lurien"));
                        Object.Destroy(GameObject.Find("Dream Enter"));
                        Object.Destroy(GameObject.Find("Dream Impact"));
                        Object.Destroy(GameObject.Find("Shield"));
                        if (!PlayerData.instance.hasDreamNail) Object.Destroy(GameObject.Find("New Shiny"));
                    }
                    break;
                case SceneNames.Room_Mansion:
                    if (!PlayerData.instance.xunFlowerGiven) Object.Destroy(GameObject.Find("Randomizer Shiny")); //Should not actually be necessary, but left in as a precaution
                    break;
                case SceneNames.Room_Wyrm:
                    //Make King's Brand cutscene function properly
                    //This stops only stops the cutscene, not the avalanche itself
                    Object.Destroy(GameObject.Find("Avalanche End"));
                    break;
                case SceneNames.Room_Colosseum_Bronze:
                    GameObject.Find("Colosseum Manager").LocateMyFSM("Geo Pool").GetState("Open Gates").AddFirstAction(new RandomizerSetBool("colosseumBronzeCompleted", true, true));
                    break;
                case SceneNames.Room_Colosseum_Silver:
                    GameObject.Find("Colosseum Manager").LocateMyFSM("Geo Pool").GetState("Open Gates").AddFirstAction(new RandomizerSetBool("colosseumSilverCompleted", true, true));
                    break;
                case SceneNames.Town:
                    // Prevent simple key softlocks
                    FsmState jijiKey = GameObject.Find("Jiji Door").LocateMyFSM("Conversation Control").GetState("Key?");
                    jijiKey.RemoveActionsOfType<GetPlayerDataInt>();
                    jijiKey.RemoveActionsOfType<IntCompare>();
                    jijiKey.AddAction(new RandomizerExecuteLambda(() =>
                    {
                        if (GameManager.instance.GetPlayerDataInt("simpleKeys") > 1 || (PlayerData.instance.openedWaterwaysManhole && GameManager.instance.GetPlayerDataInt("simpleKeys") > 0)) PlayMakerFSM.BroadcastEvent("KEY");
                        else PlayMakerFSM.BroadcastEvent("NOKEY");
                    }));
                    break;
                case SceneNames.Waterways_01:
                    if (GameManager.instance.entryGateName.StartsWith("t"))
                    {
                        Object.Destroy(GameObject.Find("waterways_manhole_open"));
                    }
                    break;
                case SceneNames.Waterways_03:
                    if (AreaRando.Instance.Settings.Jiji)
                    {
                        LanguageStringManager.SetLanguageString("TUK_RANCIDEGG_MIN", "Prices", "800");
                        LanguageStringManager.SetLanguageString("TUK_RANCIDEGG_MAX", "Prices", "1000");
                    }
                    else
                    {
                        LanguageStringManager.SetLanguageString("TUK_RANCIDEGG_MIN", "Prices", "80");
                        LanguageStringManager.SetLanguageString("TUK_RANCIDEGG_MAX", "Prices", "100");
                    }
                    break;
                case SceneNames.Waterways_09:
                    if (GameManager.instance.entryGateName.StartsWith("left"))
                    {
                        Object.Destroy(GameObject.Find("Waterways Gate"));
                    }
                    GameObject.Find("Ruins Lever").LocateMyFSM("Switch Control").GetState("Hit").AddFirstAction(new RandomizerExecuteLambda(() => GameManager.instance.SetPlayerDataBool("waterwaysGate", true)));
                    break;
            }
        }

        private static void ApplyGeneralChanges(Scene newScene)
        {
            switch (newScene.name)
            {
                case SceneNames.Crossroads_38:
                    // Faster daddy
                    PlayMakerFSM grubDaddy = FSMUtility.LocateFSM(GameObject.Find("Grub King"), "King Control");
                    grubDaddy.GetState("Final Reward?").RemoveTransitionsTo("Recover");
                    grubDaddy.GetState("Final Reward?").AddTransition("FINISHED", "Recheck");
                    grubDaddy.GetState("Recheck").RemoveTransitionsTo("Gift Anim");
                    grubDaddy.GetState("Recheck").AddTransition("FINISHED", "Activate Reward");

                    // Terrible code to make a terrible fsm work not terribly
                    int geoTotal = 0;
                    grubDaddy.GetState("All Given").AddAction(new RandomizerAddGeo(grubDaddy.gameObject, 0, true));
                    grubDaddy.GetState("Recheck").AddAction(new RandomizerExecuteLambda(() =>
                        grubDaddy.GetState("All Given").GetActionsOfType<RandomizerAddGeo>()[0].SetGeo(geoTotal)));

                    foreach (PlayMakerFSM grubFsm in grubDaddy.gameObject.GetComponentsInChildren<PlayMakerFSM>(true))
                    {
                        if (grubFsm.FsmName == "grub_reward_geo")
                        {
                            FsmState grubGeoState = grubFsm.GetState("Remaining?");
                            int geo = grubGeoState.GetActionsOfType<IntCompare>()[0].integer1.Value;

                            grubGeoState.RemoveActionsOfType<FsmStateAction>();
                            grubGeoState.AddAction(new RandomizerExecuteLambda(() => geoTotal += geo));
                            grubGeoState.AddTransition("FINISHED", "End");
                        }
                    }

                    break;
                case SceneNames.Deepnest_East_16:
                    // Great Hopper%
                    GameObject hopper1 = newScene.FindGameObject("Giant Hopper");
                    GameObject hopper2 = newScene.FindGameObject("Giant Hopper (1)");

                    for (int i = 0; i < 10; i++)
                    {
                        GameObject newHopper1 = Object.Instantiate(hopper1, hopper1.transform.parent);
                        GameObject newHopper2 = Object.Instantiate(hopper2, hopper2.transform.parent);

                        // Don't want people abusing the easter egg as a geo farm
                        HealthManager hopper1HM = newHopper1.GetComponent<HealthManager>();
                        hopper1HM.SetGeoSmall(0);
                        hopper1HM.SetGeoMedium(0);
                        hopper1HM.SetGeoLarge(0);

                        HealthManager hopper2HM = newHopper2.GetComponent<HealthManager>();
                        hopper2HM.SetGeoSmall(0);
                        hopper2HM.SetGeoMedium(0);
                        hopper2HM.SetGeoLarge(0);

                        Vector3 hopper1Pos = newHopper1.transform.localPosition;
                        hopper1Pos = new Vector3(
                            hopper1Pos.x + Rnd.Next(5),
                            hopper1Pos.y,
                            hopper1Pos.z);
                        newHopper1.transform.localPosition = hopper1Pos;

                        Vector3 hopper2Pos = newHopper2.transform.localPosition;
                        hopper2Pos = new Vector3(
                            hopper2Pos.x + Rnd.Next(5) - 4,
                            hopper2Pos.y,
                            hopper2Pos.z);
                        newHopper2.transform.localPosition = hopper2Pos;
                    }

                    break;
                case SceneNames.Fungus1_04:
                    // Open gates after Hornet fight
                    foreach (PlayMakerFSM childFSM in GameObject.Find("Cloak Corpse")
                        .GetComponentsInChildren<PlayMakerFSM>(true))
                    {
                        if (childFSM.FsmName == "Shiny Control")
                        {
                            SendEvent openGate = new SendEvent
                            {
                                eventTarget = new FsmEventTarget
                                {
                                    target = FsmEventTarget.EventTarget.BroadcastAll,
                                    excludeSelf = true
                                },
                                sendEvent = FsmEvent.FindEvent("BG OPEN"),
                                delay = 0,
                                everyFrame = false
                            };
                            childFSM.GetState("Destroy").AddFirstAction(openGate);
                            childFSM.GetState("Finish").AddFirstAction(openGate);

                            break;
                        }
                    }

                    // Destroy everything relating to the dreamer cutscene
                    // This stuff is in another scene and doesn't exist immediately, so I can't use Object.Destroy
                    ObjectDestroyer.Destroy("Dreamer Scene 1");
                    ObjectDestroyer.Destroy("Hornet Saver");
                    ObjectDestroyer.Destroy("Cutscene Dreamer");
                    ObjectDestroyer.Destroy("Dream Scene Activate");

                    // Fix the camera lock zone by removing the FSM that destroys it
                    if (!Ref.PD.hornet1Defeated)
                    {
                        Object.Destroy(FSMUtility.LocateFSM(GameObject.Find("Camera Locks Boss"), "FSM"));
                    }

                    break;
                case SceneNames.Mines_35:
                    // Make descending dark spikes pogoable like on old patches
                    foreach (NonBouncer nonBounce in Object.FindObjectsOfType<NonBouncer>())
                    {
                        if (nonBounce.gameObject.name.StartsWith("Spike Collider"))
                        {
                            nonBounce.active = false;
                            nonBounce.gameObject.AddComponent<RandomizerTinkEffect>();
                        }
                    }

                    break;
                case SceneNames.RestingGrounds_04:
                    // Make dream nail plaque not take 20 years to activate
                    FsmState dreamerPlaqueInspect = FSMUtility
                        .LocateFSM(GameObject.Find("Dreamer Plaque Inspect"), "Conversation Control")
                        .GetState("Hero Anim");
                    dreamerPlaqueInspect.RemoveActionsOfType<ActivateGameObject>();
                    dreamerPlaqueInspect.RemoveTransitionsTo("Fade Up");
                    dreamerPlaqueInspect.AddTransition("FINISHED", "Map Msg?");

                    PlayMakerFSM dreamerScene2 = FSMUtility.LocateFSM(GameObject.Find("Dreamer Scene 2"), "Control");
                    dreamerScene2.GetState("Take Control").RemoveTransitionsTo("Blast");
                    dreamerScene2.GetState("Take Control").AddTransition("FINISHED", "Fade Out");
                    dreamerScene2.GetState("Fade Out").RemoveTransitionsTo("Dial Wait");
                    dreamerScene2.GetState("Fade Out").AddTransition("FINISHED", "Set Compass Point");
                    break;
                case SceneNames.Ruins1_05b when AreaRando.Instance.Settings.Lemm:
                    // Lemm sell all
                    PlayMakerFSM lemm = FSMUtility.LocateFSM(GameObject.Find("Relic Dealer"), "npc_control");
                    lemm.GetState("Convo End").AddAction(new RandomizerSellRelics());
                    break;

            }
        }

        private static void ApplyNoClawChanges(Scene newScene)
        {
            switch (newScene.name)
            {
                case SceneNames.Abyss_04: // Hidden Station
                    GameObject.Find("Direction Pole White Palace (1)").GetComponent<NonBouncer>().active = false;
                    break;
                case SceneNames.Deepnest_Spider_Town: // Beast's Den
                    StickyWall.Create(26.75f, 70f, 2.6f, 15f);
                    StickyWall.Create(5.75f, 92f, 2.6f, 3f);
                    StickyWall.Create(15.75f, 105f, 2.6f, 3f);
                    StickyWall.Create(2.7f, 125f, 2.6f, 10f);
                    StickyWall.Create(15.7f, 145f, 2.6f, 5f);
                    StickyWall.Create(79.85f, 75f, 2.6f, 2f);
                    break;
                case SceneNames.Fungus1_09: // Great slash
                    Object.Instantiate(GameObject.Find("fung_plat_float_05")).transform.position =
                        new Vector3(205f, 11f, 0f);
                    Object.Instantiate(GameObject.Find("fung_plat_float_05")).transform.position =
                        new Vector3(175f, 12.25f, 0f);
                    Object.Instantiate(GameObject.Find("fung_plat_float_05")).transform.position =
                        new Vector3(116.5f, 12f, 0f);
                    Object.Instantiate(GameObject.Find("fung_plat_float_05")).transform.position =
                        new Vector3(4.75f, 17f, 0f);
                    break;
                case SceneNames.Fungus3_archive_02: // Monomon
                    StickyWall.Create(49.75f, 55f, 2.6f, 5f);
                    StickyWall.Create(76.75f, 78.5f, 2.6f, 50f);
                    StickyWall.Create(49.75f, 140f, 2.6f, 5f);
                    break;
                case SceneNames.Mines_37: // Crystal peak chest
                    StickyWall.Create(0.7f, 8.5f, 2.6f, 2f);
                    break;
                case SceneNames.Ruins2_03: // Watcher Knights
                    StickyWall.Create(67.9f, 90f, 2.6f, 25f);

                    // Add a platform in addition to the sticky wall to get up to Lurien
                    GameObject plat = Object.Instantiate(GameObject.Find("ruins_mage_building_0011_a_royal_plat"));
                    plat.transform.position = new Vector3(44f, 112f, plat.transform.position.z);
                    break;
                case SceneNames.Town: // Dirtmouth
                    StickyWall.Create(11.66f, 26.3f, 2.6f, 30.8f);
                    break;
                case SceneNames.Tutorial_01: // King's Pass
                    StickyWall.Create(5.7f, 18f, 2.6f, 2f);
                    StickyWall.Create(3.75f, 41.5f, 2.6f, 2f);
                    break;
            }
        }

        private static void FalseKnightNoises(On.EnemyHitEffectsArmoured.orig_RecieveHitEffect orig,
            EnemyHitEffectsArmoured self, float dir)
        {
            orig(self, dir);

            if (_rndNum != 17 || self.gameObject.name != "False Knight New")
            {
                return;
            }

            AudioPlayerOneShot hitPlayer = FSMUtility.LocateFSM(self.gameObject, "FalseyControl").GetState("Hit")
                .GetActionsOfType<AudioPlayerOneShot>()[0];
            AudioClip clip = hitPlayer.audioClips[Rnd.Next(hitPlayer.audioClips.Length)];

            AudioClip temp = self.enemyDamage.Clip;
            self.enemyDamage.Clip = clip;
            self.enemyDamage.SpawnAndPlayOneShot(self.audioPlayerPrefab, self.transform.position);
            self.enemyDamage.Clip = temp;
        }

        private static GameObject FixExplosionPogo(GameObject go)
        {
            if (!go.name.StartsWith("Gas Explosion Recycle M"))
            {
                return go;
            }

            go.layer = (int) PhysLayers.ENEMIES;
            NonBouncer noFun = go.GetComponent<NonBouncer>();
            if (noFun)
            {
                noFun.active = false;
            }

            return go;
        }

        private static void ModifyFSM(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            if (self.Fsm.FsmComponent.FsmName == "Check Zote Death")
            {
                Object.Destroy(self);
                return;
            }

            orig(self);

            if (self.gameObject.name != "Knight" || self.FsmName != "Dream Nail")
            {
                return;
            }

            self.GetState("Cancelable").GetActionsOfType<ListenForDreamNail>()[0].activeBool = true;
            self.GetState("Cancelable Dash").GetActionsOfType<ListenForDreamNail>()[0].activeBool = true;
            self.GetState("Queuing").GetActionsOfType<ListenForDreamNail>()[0].activeBool = true;
            self.GetState("Queuing").RemoveActionsOfType<BoolTest>();
        }
    }
}