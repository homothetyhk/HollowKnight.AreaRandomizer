using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using SeanprCore;
using static AreaRando.LogHelper;
using System.Text.RegularExpressions;

namespace AreaRando.Randomization
{
    internal enum ItemType
    {
        Big,
        Charm,
        Trinket,
        Shop,
        Spell,
        Geo
    }

    // ReSharper disable InconsistentNaming
#pragma warning disable 0649 // Assigned via reflection
    internal struct TransitionDef
    {
        public string sceneName;
        public string doorName;
        public string areaName;

        public string destinationScene;
        public string destinationGate;

        public string[] logic;
        public List<(int, int)> processedLogic;

        public bool isolated;
        public bool deadEnd;
        public int oneWay; // 0 == 2-way, 1 == can only go in, 2 == can only come out
    }
    internal struct ReqDef
    {
        // Control variables
        public string boolName;
        public string sceneName;
        public string objectName;
        public string altObjectName;
        public string fsmName;
        public bool replace;
        public string[] logic;
        public List<(int, int)> processedLogic;

        public ItemType type;
        public string pool;
        public string areaName;

        public bool newShiny;
        public int x;
        public int y;

        // Big item variables
        public string bigSpriteKey;
        public string takeKey;
        public string nameKey;
        public string buttonKey;
        public string descOneKey;
        public string descTwoKey;

        // Shop variables
        public string shopDescKey;
        public string shopSpriteKey;
        public string notchCost;

        public string shopName;

        // Trinket variables
        public int trinketNum;

        // Item tier flags
        public bool progression;
        public bool itemCandidate; // Excludes progression items which are unlikely to open new locations in a pinch, such as charms that only kill baldurs or assist with spiketunnels
        public bool areaCandidate; // Only those items which are very likely to open new transitions
        public bool isGoodItem;
        public bool isFake;
        public int longItemTier; // For excluding undesirable locations

        // Geo flags
        public bool inChest;
        public int geo;

        public string chestName;
        public string chestFsmName;

        // For pricey items such as dash slash location
        public int cost;
        public int costType;
    }

    internal struct ShopDef
    {
        public string sceneName;
        public string objectName;
        public string[] logic;
        public List<(int, int)> processedLogic;
        public string requiredPlayerDataBool;
        public bool dungDiscount;
    }
#pragma warning restore 0649
    // ReSharper restore InconsistentNaming

    internal static class LogicManager
    {
        private static Dictionary<string, TransitionDef> _transitions;
        private static Dictionary<string, ReqDef> _items;
        private static Dictionary<string, ShopDef> _shops;
        private static Dictionary<string, string[]> _additiveItems;
        private static Dictionary<string, string[]> _macros;

        public static Dictionary<string, (int,int)> progressionBitMask;
        public static int bitMaskMax;
        public static string[] TransitionNames => _transitions.Keys.ToArray();
        public static string[] ItemNames => _items.Keys.ToArray();

        public static string[] ShopNames => _shops.Keys.ToArray();

        public static string[] AdditiveItemNames => _additiveItems.Keys.ToArray();

        public static void ParseXML(object streamObj)
        {
            if (!(streamObj is Stream xmlStream))
            {
                LogWarn("Non-Stream object passed to ParseXML");
                return;
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(xmlStream);
                xmlStream.Dispose();

                _macros = new Dictionary<string, string[]>();
                _additiveItems = new Dictionary<string, string[]>();
                _transitions = new Dictionary<string, TransitionDef>();
                _items = new Dictionary<string, ReqDef>();
                _shops = new Dictionary<string, ShopDef>();

                ParseAdditiveItemXML(xml.SelectNodes("randomizer/additiveItemSet"));
                ParseMacroXML(xml.SelectNodes("randomizer/macro"));
                ParseTransitionXML(xml.SelectNodes("randomizer/transition"));
                ParseItemXML(xml.SelectNodes("randomizer/item"));
                ParseShopXML(xml.SelectNodes("randomizer/shop"));
                ProcessLogic();
            }
            catch (Exception e)
            {
                LogError("Could not parse items.xml:\n" + e);
            }

            watch.Stop();
            LogDebug("Parsed items.xml in " + watch.Elapsed.TotalSeconds + " seconds");
        }

        public static TransitionDef GetTransitionDef(string name)
        {
            if (!_transitions.TryGetValue(name, out TransitionDef def))
            {
                LogWarn($"Nonexistent item \"{name}\" requested");
            }

            return def;
        }
        public static ReqDef GetItemDef(string name)
        {
            string newName = Regex.Replace(name, @"_\(\d+\)$", ""); // an item name ending in _(1) is processed as a duplicate
            if (!_items.TryGetValue(newName, out ReqDef def))
            {
                LogWarn($"Nonexistent item \"{name}\" requested");
            }
            if (newName != name) def.boolName += name.Substring(name.Length - 4); // duplicate items need distinct bools

            return def;
        }

        public static ShopDef GetShopDef(string name)
        {
            if (!_shops.TryGetValue(name, out ShopDef def))
            {
                LogWarn($"Nonexistent shop \"{name}\" requested");
            }

            return def;
        }



        public static bool ParseProcessedLogic(string item, int[] obtained)
        {
            List<(int,int)> logic;

            if (_items.TryGetValue(item, out ReqDef reqDef))
            {
                logic = reqDef.processedLogic;
            }
            else if (_shops.TryGetValue(item, out ShopDef shopDef))
            {
                logic = shopDef.processedLogic;
            }
            else if (_transitions.TryGetValue(item, out TransitionDef transitionDef))
            {
                logic = transitionDef.processedLogic;
            }
            else
            {
                AreaRando.Instance.LogWarn($"ParseLogic called for non-existent item/shop \"{item}\"");
                return false;
            }

            if (logic == null || logic.Count == 0)
            {
                return true;
            }

            Stack<bool> stack = new Stack<bool>();

            for (int i = 0; i < logic.Count; i++)
            {
                switch (logic[i].Item1)
                {
                    //AND
                    case -2:
                        if (stack.Count < 2)
                        {
                            AreaRando.Instance.LogWarn($"Could not parse logic for \"{item}\": Found + when stack contained less than 2 items");
                            return false;
                        }

                        stack.Push(stack.Pop() & stack.Pop());
                        break;
                    //OR
                    case -1:
                        if (stack.Count < 2)
                        {
                            AreaRando.Instance.LogWarn($"Could not parse logic for \"{item}\": Found | when stack contained less than 2 items");
                            return false;
                        }

                        stack.Push(stack.Pop() | stack.Pop());
                        break;
                    //EVERYTHING
                    case 0:
                        stack.Push(false);
                        break;
                    default:
                        stack.Push((logic[i].Item1 & obtained[logic[i].Item2]) == logic[i].Item1);
                        break;
                }
            }

            if (stack.Count == 0)
            {
                LogWarn($"Could not parse logic for \"{item}\": Stack empty after parsing");
                return false;
            }

            if (stack.Count != 1)
            {
                LogWarn($"Extra items in stack after parsing logic for \"{item}\"");
            }

            return stack.Pop();
        }

        // This is handy for debugging
        public static string ListObtainedProgression(int[] obtained)
        {
            string progression = string.Empty;
            foreach (string item in ItemNames)
            {
                if (_items[item].progression && CheckForProgressionItem(obtained, item)) progression += item + ", ";
            }
            foreach (string transition in TransitionNames)
            {
                if (CheckForProgressionItem(obtained, transition)) progression += transition + ", ";
            }
            return progression;
        }
        public static int[] AddObtainedProgression(int[] obtained, string newItem)
        {
            (int, int) a = progressionBitMask[newItem];
            obtained[a.Item2] |= a.Item1;
            return obtained;
        }
        public static int[] RemoveObtainedProgression(int[] obtained, string removedItem)
        {
            (int, int) a = progressionBitMask[removedItem];
            obtained[a.Item2] &= ~a.Item1;
            return obtained;
        }
        public static bool CheckForProgressionItem(int[] obtained, string inventoryItem)
        {
            (int, int) a = progressionBitMask[inventoryItem];
            return (obtained[a.Item2] & a.Item1) == a.Item1;
        }

        public static int[] AddDifficultySettings(int[] obtained)
        {
            if (AreaRando.Instance.Settings.ShadeSkips) obtained = AddObtainedProgression(obtained, "SHADESKIPS");
            if (AreaRando.Instance.Settings.AcidSkips) obtained = AddObtainedProgression(obtained, "ACIDSKIPS");
            if (AreaRando.Instance.Settings.SpikeTunnels) obtained = AddObtainedProgression(obtained, "SPIKETUNNELS");
            if (AreaRando.Instance.Settings.MiscSkips) obtained = AddObtainedProgression(obtained, "MISCSKIPS");
            if (AreaRando.Instance.Settings.FireballSkips) obtained = AddObtainedProgression(obtained, "FIREBALLSKIPS");
            if (AreaRando.Instance.Settings.DarkRooms) obtained = AddObtainedProgression(obtained, "DARKROOMS");
            return obtained;
        }

        public static string[] GetAdditiveItems(string name)
        {
            if (!_additiveItems.TryGetValue(name, out string[] items))
            {
                LogWarn($"Nonexistent additive item set \"{name}\" requested");
                return null;
            }

            return (string[]) items.Clone();
        }

        private static string[] ShuntingYard(string infix)
        {
            int i = 0;
            Stack<string> stack = new Stack<string>();
            List<string> postfix = new List<string>();

            while (i < infix.Length)
            {
                string op = GetNextOperator(infix, ref i);

                // Easiest way to deal with whitespace between operators
                if (op.Trim(' ') == string.Empty)
                {
                    continue;
                }

                if (op == "+" || op == "|")
                {
                    while (stack.Count != 0 && (op == "|" || op == "+" && stack.Peek() != "|") && stack.Peek() != "(")
                    {
                        postfix.Add(stack.Pop());
                    }

                    stack.Push(op);
                }
                else if (op == "(")
                {
                    stack.Push(op);
                }
                else if (op == ")")
                {
                    while (stack.Peek() != "(")
                    {
                        postfix.Add(stack.Pop());
                    }

                    stack.Pop();
                }
                else
                {
                    // Parse macros
                    if (_macros.TryGetValue(op, out string[] macro))
                    {
                        postfix.AddRange(macro);
                    }
                    else
                    {
                        postfix.Add(op);
                    }
                }
            }

            while (stack.Count != 0)
            {
                postfix.Add(stack.Pop());
            }

            return postfix.ToArray();
        }

        private static void ProcessLogic()
        {
            progressionBitMask = new Dictionary<string, (int,int)>();
            progressionBitMask.Add("SHADESKIPS", (1, 0));
            progressionBitMask.Add("ACIDSKIPS", (2,0));
            progressionBitMask.Add("SPIKETUNNELS", (4,0));
            progressionBitMask.Add("MISCSKIPS", (8,0));
            progressionBitMask.Add("FIREBALLSKIPS", (16,0));
            progressionBitMask.Add("DARKROOMS", (32,0));

            int i = 6;

            foreach (string itemName in ItemNames)
            {
                if (_items[itemName].progression)
                {
                    progressionBitMask.Add(itemName, ((int)Math.Pow(2, i),bitMaskMax));
                    i++;
                    if (i == 31)
                    {
                        i = 0;
                        bitMaskMax++;
                    }
                }
            }
            foreach (string transitionName in TransitionNames)
            {
                progressionBitMask.Add(transitionName, ((int)Math.Pow(2, i), bitMaskMax));
                i++;
                if (i == 31)
                {
                    i = 0;
                    bitMaskMax++;
                }
            }

            foreach (string itemName in ItemNames)
            {
                ReqDef def = _items[itemName];
                string[] infix = def.logic;
                List<(int, int)> postfix = new List<(int, int)>();
                i = 0;
                while (i < infix.Length)
                {
                    if (infix[i] == "|") postfix.Add((-1,0));
                    else if (infix[i] == "+") postfix.Add((-2,0));
                    else
                    {
                        postfix.Add(progressionBitMask[infix[i]]);
                    }
                    i++;
                }

                def.processedLogic = postfix;
                _items[itemName] = def;
            }

            foreach (string shopName in ShopNames)
            {
                ShopDef def = _shops[shopName];
                string[] infix = def.logic;
                List<(int, int)> postfix = new List<(int, int)>();
                i = 0;
                while (i < infix.Length)
                {
                    if (infix[i] == "|") postfix.Add((-1, 0));
                    else if (infix[i] == "+") postfix.Add((-2, 0));
                    else
                    {
                        postfix.Add(progressionBitMask[infix[i]]);
                    }
                    i++;
                }

                def.processedLogic = postfix;
                _shops[shopName] = def;
            }
            foreach (string transitionName in TransitionNames)
            {
                TransitionDef def = _transitions[transitionName];
                if ((def.oneWay == 2) || def.isolated) continue;
                string[] infix = def.logic;
                List<(int, int)> postfix = new List<(int, int)>();
                i = 0;
                while (i < infix.Length)
                {
                    if (infix[i] == "|") postfix.Add((-1, 0));
                    else if (infix[i] == "+") postfix.Add((-2, 0));
                    else
                    {
                        postfix.Add(progressionBitMask[infix[i]]);
                    }
                    i++;
                }

                def.processedLogic = postfix;
                _transitions[transitionName] = def;
            }
        }

        private static string GetNextOperator(string infix, ref int i)
        {
            int start = i;

            if (infix[i] == '(' || infix[i] == ')' || infix[i] == '+' || infix[i] == '|')
            {
                i++;
                return infix[i - 1].ToString();
            }

            while (i < infix.Length && infix[i] != '(' && infix[i] != ')' && infix[i] != '+' && infix[i] != '|')
            {
                i++;
            }

            return infix.Substring(start, i - start).Trim(' ');
        }

        private static void ParseAdditiveItemXML(XmlNodeList nodes)
        {
            foreach (XmlNode setNode in nodes)
            {
                XmlAttribute nameAttr = setNode.Attributes?["name"];
                if (nameAttr == null)
                {
                    LogWarn("Node in items.xml has no name attribute");
                    continue;
                }

                string[] additiveSet = new string[setNode.ChildNodes.Count];
                for (int i = 0; i < additiveSet.Length; i++)
                {
                    additiveSet[i] = setNode.ChildNodes[i].InnerText;
                }

                LogDebug($"Parsed XML for item set \"{nameAttr.InnerText}\"");
                _additiveItems.Add(nameAttr.InnerText, additiveSet);
                _macros.Add(nameAttr.InnerText, ShuntingYard(string.Join(" | ", additiveSet)));
            }
        }

        private static void ParseMacroXML(XmlNodeList nodes)
        {
            foreach (XmlNode macroNode in nodes)
            {
                XmlAttribute nameAttr = macroNode.Attributes?["name"];
                if (nameAttr == null)
                {
                    LogWarn("Node in items.xml has no name attribute");
                    continue;
                }

                LogDebug($"Parsed XML for macro \"{nameAttr.InnerText}\"");
                _macros.Add(nameAttr.InnerText, ShuntingYard(macroNode.InnerText));
            }
        }

        private static void ParseTransitionXML(XmlNodeList nodes)
        {
            Dictionary<string, FieldInfo> transitionFields = new Dictionary<string, FieldInfo>();
            typeof(TransitionDef).GetFields().ToList().ForEach(f => transitionFields.Add(f.Name, f));

            foreach (XmlNode transitionNode in nodes)
            {
                XmlAttribute nameAttr = transitionNode.Attributes?["name"];
                if (nameAttr == null)
                {
                    LogWarn("Node in items.xml has no name attribute");
                    continue;
                }

                // Setting as object prevents boxing in FieldInfo.SetValue calls
                object def = new TransitionDef();

                foreach (XmlNode fieldNode in transitionNode.ChildNodes)
                {
                    if (!transitionFields.TryGetValue(fieldNode.Name, out FieldInfo field))
                    {
                        LogWarn(
                            $"Xml node \"{fieldNode.Name}\" does not map to a field in struct ReqDef");
                        continue;
                    }

                    if (field.FieldType == typeof(string))
                    {
                        field.SetValue(def, fieldNode.InnerText);
                    }
                    else if (field.FieldType == typeof(string[]))
                    {
                        if (field.Name == "logic")
                        {
                            field.SetValue(def, ShuntingYard(fieldNode.InnerText));
                        }
                        else
                        {
                            LogWarn(
                                "string[] field not named \"logic\" found in ReqDef, ignoring");
                        }
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        if (bool.TryParse(fieldNode.InnerText, out bool xmlBool))
                        {
                            field.SetValue(def, xmlBool);
                        }
                        else
                        {
                            LogWarn($"Could not parse \"{fieldNode.InnerText}\" to bool");
                        }
                    }
                    else if (field.FieldType == typeof(ItemType))
                    {
                        if (fieldNode.InnerText.TryToEnum(out ItemType type))
                        {
                            field.SetValue(def, type);
                        }
                        else
                        {
                            LogWarn($"Could not parse \"{fieldNode.InnerText}\" to ItemType");
                        }
                    }
                    else if (field.FieldType == typeof(int))
                    {
                        if (int.TryParse(fieldNode.InnerText, out int xmlInt))
                        {
                            field.SetValue(def, xmlInt);
                        }
                        else
                        {
                            LogWarn($"Could not parse \"{fieldNode.InnerText}\" to int");
                        }
                    }
                    else
                    {
                        LogWarn("Unsupported type in ReqDef: " + field.FieldType.Name);
                    }
                }

                LogDebug($"Parsed XML for transition \"{nameAttr.InnerText}\"");
                _transitions.Add(nameAttr.InnerText, (TransitionDef)def);
            }
        }

        private static void ParseItemXML(XmlNodeList nodes)
        {
            Dictionary<string, FieldInfo> reqFields = new Dictionary<string, FieldInfo>();
            typeof(ReqDef).GetFields().ToList().ForEach(f => reqFields.Add(f.Name, f));

            foreach (XmlNode itemNode in nodes)
            {
                XmlAttribute nameAttr = itemNode.Attributes?["name"];
                if (nameAttr == null)
                {
                    LogWarn("Node in items.xml has no name attribute");
                    continue;
                }
                // Setting as object prevents boxing in FieldInfo.SetValue calls
                object def = new ReqDef();

                foreach (XmlNode fieldNode in itemNode.ChildNodes)
                {
                    if (!reqFields.TryGetValue(fieldNode.Name, out FieldInfo field))
                    {
                        LogWarn(
                            $"Xml node \"{fieldNode.Name}\" does not map to a field in struct ReqDef");
                        continue;
                    }

                    if (field.FieldType == typeof(string))
                    {
                        field.SetValue(def, fieldNode.InnerText);
                    }
                    else if (field.FieldType == typeof(string[]))
                    {
                        if (field.Name == "logic")
                        {
                            field.SetValue(def, ShuntingYard(fieldNode.InnerText));
                        }
                        else
                        {
                            LogWarn(
                                "string[] field not named \"logic\" found in ReqDef, ignoring");
                        }
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        if (bool.TryParse(fieldNode.InnerText, out bool xmlBool))
                        {
                            field.SetValue(def, xmlBool);
                        }
                        else
                        {
                            LogWarn($"Could not parse \"{fieldNode.InnerText}\" to bool");
                        }
                    }
                    else if (field.FieldType == typeof(ItemType))
                    {
                        if (fieldNode.InnerText.TryToEnum(out ItemType type))
                        {
                            field.SetValue(def, type);
                        }
                        else
                        {
                            LogWarn($"Could not parse \"{fieldNode.InnerText}\" to ItemType");
                        }
                    }
                    else if (field.FieldType == typeof(int))
                    {
                        if (int.TryParse(fieldNode.InnerText, out int xmlInt))
                        {
                            field.SetValue(def, xmlInt);
                        }
                        else
                        {
                            LogWarn($"Could not parse \"{fieldNode.InnerText}\" to int");
                        }
                    }
                    else
                    {
                        LogWarn("Unsupported type in ReqDef: " + field.FieldType.Name);
                    }
                }

                LogDebug($"Parsed XML for item \"{nameAttr.InnerText}\"");
                _items.Add(nameAttr.InnerText, (ReqDef) def);
            }
        }

        private static void ParseShopXML(XmlNodeList nodes)
        {
            Dictionary<string, FieldInfo> shopFields = new Dictionary<string, FieldInfo>();
            typeof(ShopDef).GetFields().ToList().ForEach(f => shopFields.Add(f.Name, f));

            foreach (XmlNode shopNode in nodes)
            {
                XmlAttribute nameAttr = shopNode.Attributes?["name"];
                if (nameAttr == null)
                {
                    LogWarn("Node in items.xml has no name attribute");
                    continue;
                }

                // Setting as object prevents boxing in FieldInfo.SetValue calls
                object def = new ShopDef();

                foreach (XmlNode fieldNode in shopNode.ChildNodes)
                {
                    if (!shopFields.TryGetValue(fieldNode.Name, out FieldInfo field))
                    {
                        LogWarn(
                            $"Xml node \"{fieldNode.Name}\" does not map to a field in struct ReqDef");
                        continue;
                    }

                    if (field.FieldType == typeof(string))
                    {
                        field.SetValue(def, fieldNode.InnerText);
                    }
                    else if (field.FieldType == typeof(string[]))
                    {
                        if (field.Name == "logic")
                        {
                            field.SetValue(def, ShuntingYard(fieldNode.InnerText));
                        }
                        else
                        {
                            LogWarn(
                                "string[] field not named \"logic\" found in ShopDef, ignoring");
                        }
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        if (bool.TryParse(fieldNode.InnerText, out bool xmlBool))
                        {
                            field.SetValue(def, xmlBool);
                        }
                        else
                        {
                            LogWarn($"Could not parse \"{fieldNode.InnerText}\" to bool");
                        }
                    }
                    else
                    {
                        LogWarn("Unsupported type in ShopDef: " + field.FieldType.Name);
                    }
                }

                LogDebug($"Parsed XML for shop \"{nameAttr.InnerText}\"");
                _shops.Add(nameAttr.InnerText, (ShopDef) def);
            }
        }
    }
}