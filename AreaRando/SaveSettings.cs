using System.Linq;
using Modding;
using AreaRando.Actions;
using SeanprCore;

namespace AreaRando
{
    public class SaveSettings : BaseSettings
    {

        private SerializableStringDictionary _itemPlacements = new SerializableStringDictionary();
        public SerializableStringDictionary _transitionPlacements = new SerializableStringDictionary();
        private SerializableStringDictionary _hintInformation = new SerializableStringDictionary();

        /// <remarks>item, location</remarks>
        public (string, string)[] ItemPlacements => _itemPlacements.Select(pair => (pair.Key, pair.Value)).ToArray();

        // index is how many hints, pair is item, location
        public (string, string)[] Hints => _hintInformation.Select(pair => (pair.Key, pair.Value)).ToArray();

        public SaveSettings()
        {
            AfterDeserialize += () =>
            {
                RandomizerAction.CreateActions(ItemPlacements, Seed);
            };
        }

        public int howManyHints
        {
            get => GetInt(0);
            set => SetInt(value);
        }

        public bool AllBosses
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool AllSkills
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool AllCharms
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool CharmNotch
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool Lemm
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public bool Jiji
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool PleasureHouse
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public bool EarlyGeo
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool Randomizer
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public bool SlyCharm
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public bool RandomizeDreamers
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeSkills
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeCharms
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeKeys
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeGeoChests
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeMaskShards
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeVesselFragments
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeCharmNotches
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizePaleOre
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeRancidEggs
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public bool RandomizeRelics
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public int LongItemTier
        {
            get => GetInt(1);
            set => SetInt(value);
        }
        public bool ShadeSkips
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool AcidSkips
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool SpikeTunnels
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool MiscSkips
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool FireballSkips
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool DarkRooms
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public int Seed
        {
            get => GetInt(-1);
            set => SetInt(value);
        }

        public bool NoClaw
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public void ResetItemPlacements()
        {
            _itemPlacements = new SerializableStringDictionary();
        }

        public void AddItemPlacement(string item, string location)
        {
            _itemPlacements[item] = location;
        }

        public void AddTransitionPlacement(string entrance, string exit)
        {
            _transitionPlacements[entrance] = exit;
        }

        public void AddNewHint(string item, string location)
        {
            _hintInformation[item] = location;
        }
    }
}