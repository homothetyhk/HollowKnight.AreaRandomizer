using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using AreaRando.FsmStateActions;
using SeanprCore;
using Object = UnityEngine.Object;

namespace AreaRando.Actions
{
    public class ChangeShinyIntoCharm : RandomizerAction
    {
        private readonly string _boolName;
        private readonly int _charmNum;
        private readonly string _fsmName;
        private readonly string _objectName;
        private readonly string _sceneName;
        private readonly string _location;

        public ChangeShinyIntoCharm(string sceneName, string objectName, string fsmName, string boolName, string location)
            : this(sceneName, objectName, fsmName, Convert.ToInt32(boolName.Substring(9)), location)
        {
        }

        public ChangeShinyIntoCharm(string sceneName, string objectName, string fsmName, int charmNum, string location)
        {
            _sceneName = sceneName;
            _objectName = objectName;
            _fsmName = fsmName;
            _charmNum = charmNum;
            _location = location;

            _boolName = $"gotCharm_{charmNum}";
        }

        public override ActionType Type => ActionType.PlayMakerFSM;

        public override void Process(string scene, Object changeObj)
        {
            if (scene != _sceneName || !(changeObj is PlayMakerFSM fsm) || fsm.FsmName != _fsmName ||
                fsm.gameObject.name != _objectName)
            {
                return;
            }

            FsmState pdBool = fsm.GetState("PD Bool?");
            FsmState charm = fsm.GetState("Charm?");
            FsmState getCharm = fsm.GetState("Get Charm");

            // Remove actions that stop shiny from spawning
            pdBool.RemoveActionsOfType<PlayerDataBoolTest>();
            pdBool.RemoveActionsOfType<StringCompare>();

            // Add action to potentially despawn the object
            pdBool.AddAction(new RandomizerBoolTest(_boolName, null, "COLLECTED", true));

            // Force the FSM into the charm state, set it to the correct charm
            charm.ClearTransitions();
            charm.AddTransition("FINISHED", "Get Charm");
            getCharm.RemoveActionsOfType<SetPlayerDataBool>();
            getCharm.AddFirstAction(new RandomizerSetBool(_boolName, true, true));
            getCharm.AddFirstAction(new RandomizerExecuteLambda(() => AreaRando.Instance.Settings.UpdateObtainedProgressionByBoolName(_boolName)));
            getCharm.AddAction(new RandomizerExecuteLambda(() => RandoLogger.UpdateHelperLog()));
            getCharm.AddAction(new RandomizerExecuteLambda(() => RandoLogger.LogItemToTrackerByBoolName(_boolName, _location)));
            fsm.GetState("Normal Msg").GetActionsOfType<SetFsmInt>()[0].setValue = _charmNum;
        }
    }
}