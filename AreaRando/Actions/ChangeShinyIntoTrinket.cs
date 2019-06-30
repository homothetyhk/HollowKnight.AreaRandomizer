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
        private readonly string _location;

        public ChangeShinyIntoTrinket(string sceneName, string objectName, string fsmName, int trinketNum, string boolName, string location)
        {
            _sceneName = sceneName;
            _objectName = objectName;
            _fsmName = fsmName;
            _trinketNum = trinketNum;
            _boolName = boolName;
            _location = location;
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
            FsmState giveTrinket = fsm.GetState("Store Key");

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
            trinkFlash.AddTransition("FINISHED", "Store Key");

            giveTrinket.AddFirstAction(new RandomizerExecuteLambda(() => AreaRando.Instance.Settings.UpdateObtainedProgressionByBoolName(_boolName)));
            giveTrinket.AddAction(new RandomizerExecuteLambda(() => RandoLogger.LogItemToTrackerByBoolName(_boolName, _location)));
            giveTrinket.AddAction(new RandomizerExecuteLambda(() => RandoLogger.UpdateHelperLog()));
            giveTrinket.GetActionsOfType<SetPlayerDataBool>().First().boolName = _boolName;

            // Makes sure the correct icon and text appear
            switch (_trinketNum)
            {

                case 1:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_TRINKET1";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.WanderersJournal");
                    break;
                case 2:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_TRINKET2";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.HallownestSeal");
                    break;
                case 3:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_TRINKET3";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.KingsIdol");
                    break;
                case 4:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_TRINKET4";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.ArcaneEgg");
                    break;
                case 6:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_TRAM_PASS";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.TramPass");
                    break;
                case 8:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_STOREKEY";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.ShopkeepersKey");
                    break;
                case 9:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_CITYKEY";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.CityKey");
                    break;
                case 10:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_LOVEKEY";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.LoveKey");
                    break;
                case 11:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_RANCIDEGG";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.RancidEgg");
                    break;
                case 12:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_SIMPLEKEY";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.SimpleKey");
                    break;
                case 13:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "RANDOMIZER_NAME_CHARM_NOTCH";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.CharmNotch");
                    break;
                case 14:
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "RANDOMIZER_NAME_PALE_ORE";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.PaleOre");
                    break;
                case 20: //lantern
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_LANTERN";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.Lantern");
                    break;
                case 21: //elegant key
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "INV_NAME_WHITEKEY";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.ElegantKey");
                    break;
                case 22: //mask shards
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "RANDOMIZER_NAME_MASK_SHARD";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.MaskShard");
                    break;
                case 23: //vessel fragments
                    giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = "RANDOMIZER_NAME_VESSEL_FRAGMENT";
                    giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = AreaRando.GetSprite("ShopIcons.VesselFragment");
                    break;
            }
        }
    }
}