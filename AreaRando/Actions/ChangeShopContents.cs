using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using AreaRando.Extensions;
using AreaRando.FsmStateActions;

namespace AreaRando.Actions
{
    public struct ShopItemDef
    {
        // Values from ShopItemStats
        public string PlayerDataBoolName;
        public string NameConvo;
        public string DescConvo;
        public string RequiredPlayerDataBool;
        public string RemovalPlayerDataBool;
        public bool DungDiscount;
        public string NotchCostBool;
        public int Cost;

        // Sprite name in resources
        public string SpriteName;
    }


    public class ChangeShopContents : RandomizerAction, ISerializationCallbackReceiver
    {
        // Variable for serialization hack
        private List<string> _itemDefStrings;
        private ShopItemDef[] _items;

        // Variables that actually get used

        public ChangeShopContents(string sceneName, string objectName, ShopItemDef[] items)
        {
            SceneName = sceneName;
            ObjectName = objectName;
            _items = items;
        }

        public override ActionType Type => ActionType.GameObject;

        public string SceneName { get; }

        public string ObjectName { get; }

        public void OnBeforeSerialize()
        {
            _itemDefStrings = new List<string>();
            foreach (ShopItemDef item in _items)
            {
                _itemDefStrings.Add(JsonUtility.ToJson(item));
            }
        }

        public void OnAfterDeserialize()
        {
            List<ShopItemDef> itemDefList = new List<ShopItemDef>();

            foreach (string item in _itemDefStrings)
            {
                itemDefList.Add(JsonUtility.FromJson<ShopItemDef>(item));
            }

            _items = itemDefList.ToArray();
        }

        public void AddItemDefs(ShopItemDef[] newItems)
        {
            if (_items == null)
            {
                _items = newItems;
                return;
            }

            if (newItems == null)
            {
                return;
            }

            ShopItemDef[] combined = new ShopItemDef[_items.Length + newItems.Length];
            _items.CopyTo(combined, 0);
            newItems.CopyTo(combined, _items.Length);
            _items = combined;
        }

        public override void Process(string scene, Object changeObj)
        {
            if (scene != SceneName)
            {
                return;
            }



            // Find the shop and save an item for use later
            GameObject shopObj = GameObject.Find(ObjectName);
            ShopMenuStock shop = shopObj.GetComponent<ShopMenuStock>();
            GameObject itemPrefab = Object.Instantiate(shop.stock[0]);
            itemPrefab.SetActive(false);

            // Remove all charm type items from the store
            List<GameObject> newStock = new List<GameObject>();

            foreach (ShopItemDef itemDef in _items)
            {
                // Create a new shop item for this item def
                GameObject newItemObj = Object.Instantiate(itemPrefab);
                newItemObj.SetActive(false);

                // Apply all the stored values
                ShopItemStats stats = newItemObj.GetComponent<ShopItemStats>();
                stats.playerDataBoolName = itemDef.PlayerDataBoolName;
                stats.nameConvo = itemDef.NameConvo;
                stats.descConvo = itemDef.DescConvo;
                stats.requiredPlayerDataBool = itemDef.RequiredPlayerDataBool;
                stats.removalPlayerDataBool = itemDef.RemovalPlayerDataBool;
                stats.dungDiscount = itemDef.DungDiscount;
                stats.notchCostBool = itemDef.NotchCostBool;
                stats.cost = itemDef.Cost;

                // Need to set all these to make sure the item doesn't break in one of various ways
                stats.priceConvo = string.Empty;
                stats.specialType = 2;
                stats.charmsRequired = 0;
                stats.relic = false;
                stats.relicNumber = 0;
                stats.relicPDInt = string.Empty;

                // Apply the sprite for the UI
                stats.transform.Find("Item Sprite").gameObject.GetComponent<SpriteRenderer>().sprite =
                    AreaRando.GetSprite(itemDef.SpriteName);

                newStock.Add(newItemObj);
            }
            
            // Save unchanged list for potential alt stock
            List<GameObject> altStock = new List<GameObject>();
            altStock.AddRange(newStock);

            // Update normal stock
            //specialType: 0 = lantern, elegant key, quill; 1 = mask, 2 = charm, 3 = vessel, 4-7 = relics, 8 = notch, 9 = map, 10 = simple key, 11 = egg, 12-14 = repair fragile, 15 = salubra blessing, 16 = map pin, 17 = map marker
            foreach (GameObject item in shop.stock)
            {
                // It would be cleaner to destroy the unused objects, but that breaks the shop on subsequent loads
                // TC must be reusing the shop items rather than destroying them on load
                List<int> randomizedTypes = new List<int>() { 0, 1, 2, 3, 8, 10, 11 };
                if (!randomizedTypes.Contains(item.GetComponent<ShopItemStats>().specialType))
                {
                    newStock.Add(item);
                }
                else if (item.GetComponent<ShopItemStats>().nameConvo == "INV_NAME_LANTERN" && !AreaRando.Instance.Settings.RandomizeKeys && !AreaRando.Instance.Settings.MiscSkips)
                {
                    // Easiest way to handle lantern on easy mode. Lantern is given automatically on new game load
                }
                else if (item.GetComponent<ShopItemStats>().specialType == 2 && !AreaRando.Instance.Settings.RandomizeCharms)
                {
                    newStock.Add(item);
                }
                else if ((item.GetComponent<ShopItemStats>().specialType == 0 || item.GetComponent<ShopItemStats>().specialType == 10) && !AreaRando.Instance.Settings.RandomizeKeys)
                {
                    newStock.Add(item);
                }
                else if (item.GetComponent<ShopItemStats>().nameConvo == "INV_NAME_QUILL" && AreaRando.Instance.Settings.RandomizeKeys)
                {
                    newStock.Add(item); //Special case: only nonrandomized item of special type 0
                }
                else if (item.GetComponent<ShopItemStats>().specialType == 1 && !AreaRando.Instance.Settings.RandomizeMaskShards)
                {
                    newStock.Add(item);
                }
                else if (item.GetComponent<ShopItemStats>().specialType == 3 && !AreaRando.Instance.Settings.RandomizeVesselFragments)
                {
                    newStock.Add(item);
                }
                else if (item.GetComponent<ShopItemStats>().specialType == 8 && !AreaRando.Instance.Settings.RandomizeCharmNotches)
                {
                    newStock.Add(item);
                }
                else if (item.GetComponent<ShopItemStats>().specialType == 11 && !AreaRando.Instance.Settings.RandomizeRancidEggs)
                {
                    newStock.Add(item);
                }
            }

            shop.stock = newStock.ToArray();

            // Update alt stock
            if (shop.stockAlt != null)
            {
                foreach (GameObject item in shop.stockAlt)
                {
                    // note we just have to handle the vanilla item types sly sells here
                    List<int> randomizedTypes = new List<int>() { 0, 1, 2, 3, 8, 10, 11 };
                    if (!randomizedTypes.Contains(item.GetComponent<ShopItemStats>().specialType))
                    {
                        altStock.Add(item);
                    }
                    else if (item.GetComponent<ShopItemStats>().nameConvo == "INV_NAME_LANTERN" && !AreaRando.Instance.Settings.RandomizeKeys && !AreaRando.Instance.Settings.MiscSkips)
                    {
                        // Easiest way to handle lantern on easy mode. Lantern is given automatically on new game load
                    }
                    else if (item.GetComponent<ShopItemStats>().specialType == 2 && !AreaRando.Instance.Settings.RandomizeCharms)
                    {
                        altStock.Add(item);
                    }
                    else if ((item.GetComponent<ShopItemStats>().specialType == 0 || item.GetComponent<ShopItemStats>().specialType == 10) && !AreaRando.Instance.Settings.RandomizeKeys)
                    {
                        altStock.Add(item);
                    }
                    else if (item.GetComponent<ShopItemStats>().specialType == 1 && !AreaRando.Instance.Settings.RandomizeMaskShards)
                    {
                        altStock.Add(item);
                    }
                    else if (item.GetComponent<ShopItemStats>().specialType == 3 && !AreaRando.Instance.Settings.RandomizeVesselFragments)
                    {
                        altStock.Add(item);
                    }
                    else if (item.GetComponent<ShopItemStats>().specialType == 11 && !AreaRando.Instance.Settings.RandomizeRancidEggs)
                    {
                        altStock.Add(item);
                    }
                }

                shop.stockAlt = altStock.ToArray();
            }
        }
    }
}