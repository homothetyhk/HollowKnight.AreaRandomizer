using System;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using AreaRando.FsmStateActions;
using AreaRando.Extensions;
using SeanprCore;
using UnityEngine;

namespace AreaRando.Actions
{
    // ReSharper disable once UnusedMember.Global
    public class ChangeShinyIntoTrinket : RandomizerAction
    {
        private readonly string _fsmName;
        private readonly string _objectName;
        private readonly string _sceneName;
        private readonly int _trinketNum;
        private readonly string _boolName;

        public ChangeShinyIntoTrinket(string sceneName, string objectName, string fsmName, int trinketNum, string boolName)
        {
            _sceneName = sceneName;
            _objectName = objectName;
            _fsmName = fsmName;
            _trinketNum = trinketNum;
            _boolName = boolName;
        }

        public override ActionType Type => ActionType.PlayMakerFSM;

        public override void Process(string scene, UnityEngine.Object changeObj)
        {
            if (scene != _sceneName || !(changeObj is PlayMakerFSM fsm) || fsm.FsmName != _fsmName ||
                fsm.gameObject.name != _objectName)
            {
                return;
            }


            FsmState pdBool = fsm.GetState("PD Bool?");
            FsmState charm = fsm.GetState("Charm?");
            FsmState trinkFlash = fsm.GetState("Trink Flash");

            // Remove actions that stop shiny from spawning
            pdBool.RemoveActionsOfType<PlayerDataBoolTest>();
            pdBool.RemoveActionsOfType<StringCompare>();

            // Add our own check to stop the shiny from being grabbed twice
            pdBool.AddAction(new RandomizerBoolTest(_boolName, null, "COLLECTED", true));

            // Force the FSM to follow the path for the correct trinket
            charm.ClearTransitions();
            charm.AddTransition("FINISHED", "Trink Flash");
            trinkFlash.ClearTransitions();
            fsm.GetState("Trinket Type").ClearTransitions();

            // Originally the idea below was to use the existing paths of the shiny control fsm, but it's important to set a bool for each item collected, so it's easier to do it this way

            if (_trinketNum == 1)
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_TRINKET1";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.WanderersJournal");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 2) trinkFlash.AddTransition("FINISHED", "Trink 2");
            else if (_trinketNum == 3) trinkFlash.AddTransition("FINISHED", "Trink 3");
            else if (_trinketNum == 4)
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "RANDOMIZER_NAME_ARCANE_EGG";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.ArcaneEgg");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 6) trinkFlash.AddTransition("FINISHED", "Tram pass");
            else if (_trinketNum == 8)
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_STOREKEY";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.ShopkeepersKey");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 9)
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_CITYKEY";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.CityKey");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 10)
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_LOVEKEY";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.LoveKey");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 11)
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_RANCIDEGG";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.RancidEgg");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 12)
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_SIMPLEKEY";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.SimpleKey");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 13)
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "RANDOMIZER_NAME_CHARM_NOTCH";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.CharmNotch");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 14)
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "RANDOMIZER_NAME_PALE_ORE";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.PaleOre");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 20) //lantern
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = "hasLantern";
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_LANTERN";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.Lantern");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 21) //elegant key
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = "hasWhiteKey";
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_WHITEKEY";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.ElegantKey");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 22) //mask shards
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "RANDOMIZER_NAME_MASK_SHARD";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.MaskShard");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
            else if (_trinketNum == 23) //vessel fragments
            {
                fsm.GetState("Store Key").GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;
                fsm.GetState("Store Key").GetActionsOfType<GetLanguageString>().First().convName = "RANDOMIZER_NAME_VESSEL_FRAGMENT";
                fsm.GetState("Store Key").GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.VesselFragment");
                trinkFlash.AddTransition("FINISHED", "Store Key");
            }
        }
    }
}