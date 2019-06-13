using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Language;
using static AreaRando.LogHelper;
using AreaRando.Randomization;

namespace AreaRando
{
    internal static class LanguageStringManager
    {
        private static readonly Dictionary<string, Dictionary<string, string>> LanguageStrings =
            new Dictionary<string, Dictionary<string, string>>();

        private static readonly Random Rnd = new Random();

        public static void LoadLanguageXML(Stream xmlStream)
        {
            // Load XmlDocument from resource stream
            XmlDocument xml = new XmlDocument();
            xml.Load(xmlStream);
            xmlStream.Dispose();

            XmlNodeList nodes = xml.SelectNodes("Language/entry");
            if (nodes == null)
            {
                LogWarn("Malformatted language xml, no nodes that match Language/entry");
                return;
            }

            foreach (XmlNode node in nodes)
            {
                string sheet = node.Attributes?["sheet"]?.Value;
                string key = node.Attributes?["key"]?.Value;

                if (sheet == null || key == null)
                {
                    LogWarn("Malformatted language xml, missing sheet or key on node");
                    continue;
                }

                SetString(sheet, key, node.InnerText.Replace("\\n", "\n"));
            }

            Log("Language xml processed");
        }

        public static void SetString(string sheetName, string key, string text)
        {
            if (string.IsNullOrEmpty(sheetName) || string.IsNullOrEmpty(key) || text == null)
            {
                return;
            }

            if (!LanguageStrings.TryGetValue(sheetName, out Dictionary<string, string> sheet))
            {
                sheet = new Dictionary<string, string>();
                LanguageStrings.Add(sheetName, sheet);
            }

            sheet[key] = text;
        }

        public static void ResetString(string sheetName, string key)
        {
            if (string.IsNullOrEmpty(sheetName) || string.IsNullOrEmpty(key))
            {
                return;
            }

            if (LanguageStrings.TryGetValue(sheetName, out Dictionary<string, string> sheet) && sheet.ContainsKey(key))
            {
                sheet.Remove(key);
            }
        }

        public static string GetLanguageString(string key, string sheetTitle)
        {
            if (sheetTitle == "Jiji" && key == "HIVE" && AreaRando.Instance.Settings.Jiji)
            {
                return NextJijiHint() + "<page>" + NextJijiHint() + "<page>" + NextJijiHint();
            }
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(sheetTitle))
            {
                return string.Empty;
            }

            if (LanguageStrings.ContainsKey(sheetTitle) && LanguageStrings[sheetTitle].ContainsKey(key))
            {
                return LanguageStrings[sheetTitle][key];
            }

            return Language.Language.GetInternal(key, sheetTitle);
        }
        public static void SetLanguageString(string key, string sheetTitle, string value)
        {
            LanguageStrings[sheetTitle][key] = value;
        }
        public static string NextJijiHint()
        {
            AreaRando.Instance.Log("Initial Hint Count: " + AreaRando.Instance.Settings.howManyHints);
            int hintMax = AreaRando.Instance.Settings.Hints.Length;
            string hintItemName = string.Empty;
            string hintItemSpot = string.Empty;
            while (AreaRando.Instance.Settings.howManyHints < hintMax - 1)
            {
                AreaRando.Instance.Settings.howManyHints++;
                if (!PlayerData.instance.GetBool(LogicManager.GetItemDef(AreaRando.Instance.Settings.Hints[AreaRando.Instance.Settings.howManyHints].Item1).boolName))
                {
                    hintItemName = AreaRando.Instance.Settings.Hints[AreaRando.Instance.Settings.howManyHints].Item1;
                    hintItemSpot = AreaRando.Instance.Settings.Hints[AreaRando.Instance.Settings.howManyHints].Item2;
                    break;
                }
            }
            if (hintItemName == string.Empty || hintItemSpot == string.Empty) return "Oh! I guess I couldn't find any items you left behind. Since you're doing so well, though, I think I'll be keeping this meal.";

            ReqDef hintItem = LogicManager.GetItemDef(hintItemName);
            ReqDef hintSpot = LogicManager.GetItemDef(hintItemSpot);
            bool good = false;
            int useful = 0;
            foreach ((string, string) p in AreaRando.Instance.Settings.Hints)
            {
                ReqDef item = LogicManager.GetItemDef(p.Item1);
                ReqDef location = LogicManager.GetItemDef(p.Item2);
                if (location.areaName == hintSpot.areaName)
                {
                    if (item.isGoodItem) good = true;
                    if (item.progression) useful++;
                }
            }
            string secondMessage;
            if (good) secondMessage = " The items there... just thinking about them is getting me excited.";
            else if (useful >= 2) secondMessage = " There are a few useful things waiting for you there.";
            else if (useful == 1) secondMessage = " I can't say whether it would be worth your time though.";
            else secondMessage = " Although it does seem awfully out of the way...";

            hintItemName = GetLanguageString(hintItem.nameKey, "UI");
            string hintItemArea = hintSpot.areaName;
            string firstMessage;

            if (hintItemArea == "Greenpath") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " in a lush, green land.";
            else if (hintItemArea == "Fungal_Wastes") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " nestled amongst strange fungus and bubbling lakes.";
            else if (hintItemArea == "Crystal_Peak") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " almost hidden by the glow of shimmering crystals around it.";
            else if (hintItemArea == "Abyss") firstMessage = "Yes, I can see the items you've left behind. Only faintly though... " + hintItemName + " deep below the world, surrounded by darkness. Almost a part of it...";
            else if (hintItemArea == "Royal_Waterways") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " surrounded by pipes and running water. It can not be washed away, though...";
            else if (hintItemArea == "Resting_Grounds") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " in a holy place of repose.";
            else if (hintItemArea == "Ancestral_Mound") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " in an ancestral mound... a place of strange worships.";
            else if (hintItemArea == "City_of_Tears") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " in the heart of the kingdom's capital. Rain can not wash it away, though...";
            else if (hintItemArea == "Fog_Canyon") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " lost in the fog of a strange land.";
            else if (hintItemArea == "Howling_Cliffs") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " high above us, surrounded by howling winds.";
            else if (hintItemArea == "Kingdoms_Edge") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " far away at the very edge of the world.";
            else if (hintItemArea == "Forgotten_Crossroads") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " just below us, lost amongst the kingdom's twisting roads and highways.";
            else if (hintItemArea == "Kings_Pass") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " nearby, right at the entrance to this kingdom.";
            else if (hintItemArea == "Deepnest") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + ", barely visible in the tunnels of a nest deep below this kingdom.";
            else if (hintItemArea == "Dirtmouth") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " just outside, in a town quietly fading away.";
            else if (hintItemArea == "Hive") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " surrounded by golden light, in a hive far away from here.";
            else if (hintItemArea == "Queens_Gardens") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + ", marring a garden's beauty.";
            else if (hintItemArea == "Colosseum") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + " surrounded by warriors and fools.";
            else if (hintItemArea == "Ancient_Basin") firstMessage = "Yes, I can see the items you've left behind. " + hintItemName + ", lying just outside the ruins of the king's palace.";
            else firstMessage = hintItemName + " is in " + hintItemArea + ".";

            return firstMessage + secondMessage;
        }
    }
}