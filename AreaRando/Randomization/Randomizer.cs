using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using AreaRando.Actions;
using static AreaRando.LogHelper;

namespace AreaRando.Randomization
{
    internal static class Randomizer
    {
        private static Dictionary<string, List<string>> shopItems;
        private static Dictionary<string, string> nonShopItems;
        private static Dictionary<string, string> transitionPlacements;

        private static List<string> unplacedTransitions;
        private static List<string> leftTransitions;
        private static List<string> rightTransitions;
        private static List<string> topTransitions;
        private static List<string> botTransitions;

        private static List<string> unobtainedLocations;
        private static List<string> unobtainedItems;
        private static int[] obtainedProgression;
        public static List<string> randomizedItems; //Non-geo, non-shop randomized items. Mainly used as a candidates list for the hint shop.
        private static List<string> shopNames;
        private static List<string> junkStandby;
        private static List<string> progressionStandby;
        private static List<string> locationStandby;
        private static Dictionary<string, string> deepProgressionTransitions; // Gives the transition which along with a given item results in progression

        private static int randomizerAttempts;

        private static bool overflow;
        private static bool firstPassDone;
        private static bool validated;
        private static bool randomizationError;
        public static bool Done { get; private set; }
        public static Random rand = new Random(AreaRando.Instance.Settings.Seed);
        public static void Randomize()
        {
            AreaRando.Instance.Settings.ResetItemPlacements();
            Log("Randomizing with seed: " + AreaRando.Instance.Settings.Seed);
            rand = new Random(AreaRando.Instance.Settings.Seed);

            Stopwatch areaWatch = new Stopwatch();
            Stopwatch itemWatch = new Stopwatch();
            areaWatch.Start();

            validated = false;
            randomizerAttempts = 0;

            Randomizer:
            while (true)
            {
                Log("Beginning transition randomization");
                SetupTransitionVariables();
                BuildSpanningTree();
                PlaceSpecialTransitions();
                CompleteTransitionGraph();
                if (!randomizationError) break;
                else
                {
                    continue;
                }
            }

            areaWatch.Stop();
            Log("Finished transition randomization in " + areaWatch.Elapsed.TotalSeconds + " seconds.");
            areaWatch.Reset();
            itemWatch.Start();

            while (true)
            {
                Log(".");
                Log("Beginning first pass of item randomization...");
                Log(".");
                SetupItemVariables();
                randomizerAttempts++;

                while (!firstPassDone)
                {
                    PlaceNextItem();
                }

                Log(".");
                Log("First pass item randomization complete.");
                Log("Beginning second pass...");
                Log(".");

                PlaceRemainingItems();
                validated = CheckPlacementValidity();
                if (!validated) goto Randomizer;
                else break;
            }

            itemWatch.Stop();
            AreaRando.Instance.Log("Finished item randomization in " + itemWatch.Elapsed.TotalSeconds + " seconds.");
            itemWatch.Reset();
            LogAllPlacements();

            //Create a randomly ordered list of all "real" items in floor locations
            List<string> goodPools = new List<string> { "Dreamer", "Skill", "Charm", "Key" };
            List<string> possibleHintItems = nonShopItems.Values.Where(val => goodPools.Contains(LogicManager.GetItemDef(val).pool)).ToList();
            Dictionary<string, string> inverseNonShopItems = nonShopItems.ToDictionary(x => x.Value, x=> x.Key); // There can't be two items at the same location, so this inversion is safe
            while (possibleHintItems.Count > 0)
            {
                string item = possibleHintItems[rand.Next(possibleHintItems.Count)];
                AreaRando.Instance.Settings.AddNewHint(item, inverseNonShopItems[item]);
                possibleHintItems.Remove(item);
            }

            Done = true;
            AreaRando.Instance.Log("Randomization done");
        }


        private static void SetupTransitionVariables()
        {
            randomizationError = false;

            transitionPlacements = new Dictionary<string, string>();

            unplacedTransitions = LogicManager.TransitionNames.ToList();
            leftTransitions = new List<string>();
            rightTransitions = new List<string>();
            topTransitions = new List<string>();
            botTransitions = new List<string>();

            unobtainedItems = new List<string>();
            unobtainedLocations = new List<string>();

            foreach (string item in LogicManager.ItemNames)
            {
                ReqDef def = LogicManager.GetItemDef(item);
                if (!def.isFake && def.type != ItemType.Shop) unobtainedLocations.Add(item);
                if (!def.isFake) unobtainedItems.Add(item);
            }
            unobtainedLocations.AddRange(LogicManager.ShopNames.ToList());
        }
        private static void SetupItemVariables()
        {
            nonShopItems = new Dictionary<string, string>();
            shopItems = new Dictionary<string, List<string>>();

            foreach (string shopName in LogicManager.ShopNames)
            {
                shopItems.Add(shopName, new List<string>());
            }

            unobtainedLocations = new List<string>();
            foreach (string itemName in LogicManager.ItemNames)
            {
                if (LogicManager.GetItemDef(itemName).type != ItemType.Shop)
                {
                    unobtainedLocations.Add(itemName);
                }
            }

            unobtainedLocations.AddRange(shopItems.Keys);
            unobtainedItems = LogicManager.ItemNames.ToList();
            shopNames = LogicManager.ShopNames.ToList();
            randomizedItems = new List<string>();
            junkStandby = new List<string>();
            progressionStandby = new List<string>();
            locationStandby = new List<string>();
            obtainedProgression = new int[LogicManager.bitMaskMax + 1];

            foreach (string _itemName in LogicManager.ItemNames)
            {
                if (LogicManager.GetItemDef(_itemName).isFake)
                {
                    unobtainedLocations.Remove(_itemName);
                    unobtainedItems.Remove(_itemName);
                }
            }

            randomizedItems = unobtainedLocations.Except(LogicManager.ShopNames).ToList();
            int eggCount = 1;
            foreach (string location in randomizedItems)
            {
                if (LogicManager.GetItemDef(location).longItemTier > AreaRando.Instance.Settings.LongItemTier)
                {
                    unobtainedLocations.Remove(location);
                    nonShopItems.Add(location, "Bonus_Arcane_Egg_(" + eggCount + ")");
                    eggCount++;
                }
            }

            if (AreaRando.Instance.Settings.PleasureHouse) nonShopItems.Add("Pleasure_House", "Small_Reward_Geo");

            randomizedItems = unobtainedLocations.Where(name => !LogicManager.ShopNames.Contains(name) && LogicManager.GetItemDef(name).type != ItemType.Geo).ToList();

            firstPassDone = false;
            overflow = false;
            validated = false;
            Done = false;
        }

        private static void BuildSpanningTree()
        {
            List<string> areas = new List<string>();
            Dictionary<string, List<string>> areaTransitions = new Dictionary<string, List<string>>();
            
            foreach (string transition in LogicManager.TransitionNames)
            {
                TransitionDef def = LogicManager.GetTransitionDef(transition);
                if (def.areaName == "Kings_Pass") continue;
                if (!def.deadEnd && def.oneWay == 0 && !areas.Contains(def.areaName))
                {
                    areas.Add(def.areaName);
                    areaTransitions.Add(def.areaName, new List<string>());
                }
                if (!def.deadEnd && def.oneWay == 0) areaTransitions[def.areaName].Add(transition);
            }
            
            List<string> remainingAreas = areas;
            string firstArea = "Dirtmouth"; // It's almost impossible to be locked out of Dirtmouth, so this is a good choice for basepoint, wrt picking isolated and deadend transitions
            remainingAreas.Remove(firstArea);
            AddToDirectedTransitions(areaTransitions[firstArea]);
            int failsafe = 0;

            while (remainingAreas.Any())
            {
                failsafe++;
                if (failsafe > 100)
                {
                    randomizationError = true;
                    return;
                }

                string nextArea = remainingAreas[rand.Next(remainingAreas.Count)];
                List<string> newAreaTransitions = areaTransitions[nextArea].Where(transition => !LogicManager.GetTransitionDef(transition).isolated && TestLegalDirections(LogicManager.GetTransitionDef(transition).doorName)).ToList();
                if (newAreaTransitions.Count < 1) continue;

                string nextTransition = newAreaTransitions[rand.Next(newAreaTransitions.Count)];
                string transitionSource = GetNextTransition(LogicManager.GetTransitionDef(nextTransition).doorName);

                transitionPlacements.Add(transitionSource, nextTransition);
                transitionPlacements.Add(nextTransition, transitionSource);
                unplacedTransitions.Remove(transitionSource);
                unplacedTransitions.Remove(nextTransition);
                remainingAreas.Remove(nextArea);

                List<string> newTransitions = areaTransitions[nextArea];
                AddToDirectedTransitions(newTransitions);
                RemoveFromDirectedTransitions(new List<string> { nextTransition, transitionSource });
            }
        }

        private static void PlaceSpecialTransitions()
        {
            if (randomizationError) return;

            Stopwatch specialTransitions = new Stopwatch();
            specialTransitions.Start();
            List<string> oneWayEntrances = LogicManager.TransitionNames.Where(transition => LogicManager.GetTransitionDef(transition).oneWay == 1).ToList();
            List<string> oneWayExits = LogicManager.TransitionNames.Where(transition => LogicManager.GetTransitionDef(transition).oneWay == 2).ToList();

            // Special case for the only horizontal one-way
            string downExit = oneWayExits[rand.Next(oneWayExits.Count)];
            transitionPlacements.Add("Cliffs_02[right1]", downExit);
            oneWayEntrances.Remove("Cliffs_02[right1]");
            oneWayExits.Remove(downExit);
            unplacedTransitions.Remove("Cliffs_02[right1]");
            unplacedTransitions.Remove(downExit);

            ClearDirectedTransitions();
            AddToDirectedTransitions(oneWayExits);
            

            while (oneWayEntrances.Any())
            {
                string entrance = oneWayEntrances[rand.Next(oneWayEntrances.Count)];
                string exit = GetNextTransition(LogicManager.GetTransitionDef(entrance).doorName);
                RemoveFromDirectedTransitions(new List<string> { exit });
                transitionPlacements.Add(entrance, exit);
                oneWayEntrances.Remove(entrance);
                unplacedTransitions.Remove(entrance);
                unplacedTransitions.Remove(exit);
            }

            ClearDirectedTransitions();
            List<string> isolatedTransitions = unplacedTransitions.Where(transition => LogicManager.GetTransitionDef(transition).isolated).ToList();
            List<string> nonisolatedTransitions = unplacedTransitions.Where(transition => !LogicManager.GetTransitionDef(transition).isolated).ToList();
            AddToDirectedTransitions(nonisolatedTransitions);
            RemoveFromDirectedTransitions(new List<string> { "Tutorial_01[right1]", "Tutorial_01[top2]" });
            while (isolatedTransitions.Any())
            {
                string transition1 = isolatedTransitions[rand.Next(isolatedTransitions.Count)];
                string transition2 = GetNextTransition(LogicManager.GetTransitionDef(transition1).doorName);
                transitionPlacements.Add(transition1, transition2);
                transitionPlacements.Add(transition2, transition1);
                isolatedTransitions.Remove(transition1);
                RemoveFromDirectedTransitions(new List<string> { transition2 });
                unplacedTransitions.Remove(transition1);
                unplacedTransitions.Remove(transition2);
            }
        }

        private static void CompleteTransitionGraph()
        {
            if (randomizationError) return;
            Log("Beginning full placement of transitions...");

            int[] obtained = new int[LogicManager.bitMaskMax + 1];
            List<string> reachableTransitions = new List<string>();
            List<string> reachableLocations = new List<string>();

            ClearDirectedTransitions();
            AddToDirectedTransitions(unplacedTransitions);
            int failsafe = 0;
            while (unplacedTransitions.Any())
            {
                reachableTransitions = GetReachableTransitions(obtained);
                foreach (string transition in reachableTransitions)
                {
                    obtained = LogicManager.AddObtainedProgression(obtained, transition);
                }
                reachableTransitions = reachableTransitions.Intersect(unplacedTransitions).ToList();
                reachableLocations = GetReachableLocations(obtained);

                if (reachableLocations.Count > 1)
                {
                    List<string> candidateItems = GetAreaCandidateItems();
                    if (candidateItems.Count > 0)
                    {
                        string placeLocation = reachableLocations[rand.Next(reachableLocations.Count)];
                        string placeItem = candidateItems[rand.Next(candidateItems.Count)];
                        unobtainedItems.Remove(placeItem);
                        reachableLocations.Remove(placeLocation);
                        unobtainedLocations.Remove(placeLocation);
                        obtained = LogicManager.AddObtainedProgression(obtained, placeItem);
                    }
                }

                List<string> placeableTransitions = reachableTransitions.Where(transition => TestLegalDirections(LogicManager.GetTransitionDef(transition).doorName)).ToList();

                failsafe++;
                if (failsafe > 50)
                {
                    Log("Aborted randomization on too many passes. At the time, there were:");
                    Log("Unplaced transitions: " + unplacedTransitions.Count);
                    Log("Reachable unplaced transitions: " + reachableTransitions.Count);
                    Log("Reachable unplaced transitions, directionally compatible: " + placeableTransitions.Count);
                    Log("Reachable item locations: " + reachableLocations.Count);
                    randomizationError = true;
                    return;
                }

                if (placeableTransitions.Count == 0 && reachableLocations.Count == 0)
                {
                    Log("Ran out of locations?!?");
                    randomizationError = true;
                    return;
                }
                else if (placeableTransitions.Count > 2)
                {
                    string transition1 = placeableTransitions[rand.Next(placeableTransitions.Count)];
                    string transition2 = GetNextTransition(LogicManager.GetTransitionDef(transition1).doorName);
                    transitionPlacements.Add(transition1, transition2);
                    transitionPlacements.Add(transition2, transition1);
                    RemoveFromDirectedTransitions(new List<string> { transition1, transition2 });
                    unplacedTransitions.Remove(transition1);
                    unplacedTransitions.Remove(transition2);
                    obtained = LogicManager.AddObtainedProgression(obtained, transition2);
                    continue;
                }
                else if (unplacedTransitions.Count == 2)
                {
                    string transition1 = unplacedTransitions[0];
                    string transition2 = unplacedTransitions[1];
                    transitionPlacements.Add(transition1, transition2);
                    transitionPlacements.Add(transition2, transition1);
                    RemoveFromDirectedTransitions(new List<string> { transition1, transition2 });
                    unplacedTransitions.Remove(transition1);
                    unplacedTransitions.Remove(transition2);
                    obtained = LogicManager.AddObtainedProgression(obtained, transition2);
                    continue;
                }
                else if (placeableTransitions.Count != 0)
                {
                    List<string> progressionTransitions = GetProgressionTransitions(obtained);
                    if (progressionTransitions.Count > 0)
                    {
                        ClearDirectedTransitions();
                        AddToDirectedTransitions(progressionTransitions);
                        bool placed = false;
                        foreach (string transition1 in placeableTransitions)
                        {
                            if (TestLegalDirections(LogicManager.GetTransitionDef(transition1).doorName))
                            {
                                string transition2 = GetNextTransition(LogicManager.GetTransitionDef(transition1).doorName);
                                transitionPlacements.Add(transition1, transition2);
                                transitionPlacements.Add(transition2, transition1);
                                unplacedTransitions.Remove(transition1);
                                unplacedTransitions.Remove(transition2);
                                RemoveFromDirectedTransitions(new List<string> { transition1, transition2 });
                                obtained = LogicManager.AddObtainedProgression(obtained, transition2);
                                placed = true;
                                continue;
                            }
                        }
                        ClearDirectedTransitions();
                        AddToDirectedTransitions(unplacedTransitions);
                        if (placed) continue;
                    }
                }
                obtained = AddReachableTransitions(obtained);
                reachableLocations = GetReachableLocations(obtained);
                if (reachableLocations.Count > 0)
                {
                    List<string> progressionItems = GetDeepProgression(obtained);

                    // Strongly discourage building map around claw start, if possible
                    if (progressionItems.Count > 1 && reachableLocations.First() == "Fury_of_the_Fallen")
                    {
                        progressionItems.Remove("Mantis_Claw");
                    }
                    
                    if (progressionItems.Count > 0)
                    {
                        string placeItem = progressionItems[rand.Next(progressionItems.Count)];
                        string placeLocation = reachableLocations[rand.Next(reachableLocations.Count)];
                        unobtainedItems.Remove(placeItem);
                        reachableLocations.Remove(placeLocation);
                        unobtainedLocations.Remove(placeLocation);
                        obtained = LogicManager.AddObtainedProgression(obtained, placeItem);

                        if (deepProgressionTransitions.TryGetValue(placeItem, out string transition2))
                        {
                            ClearDirectedTransitions();
                            AddToDirectedTransitions(GetReachableTransitions(obtained).Intersect(unplacedTransitions).ToList());
                            string transition1 = GetNextTransition(LogicManager.GetTransitionDef(transition2).doorName);
                            transitionPlacements.Add(transition1, transition2);
                            transitionPlacements.Add(transition2, transition1);
                            unplacedTransitions.Remove(transition1);
                            unplacedTransitions.Remove(transition2);
                            obtained = LogicManager.AddObtainedProgression(obtained, transition2);
                            ClearDirectedTransitions();
                            AddToDirectedTransitions(unplacedTransitions);
                        }


                        continue;
                    }
                    // Last ditch effort to save the seed
                    else if (unobtainedItems.Contains("Mantis_Claw"))
                    {
                        string placeItem = "Mantis_Claw";
                        string placeLocation = reachableLocations[rand.Next(reachableLocations.Count)];
                        unobtainedItems.Remove(placeItem);
                        reachableLocations.Remove(placeLocation);
                        unobtainedLocations.Remove(placeLocation);
                        obtained = LogicManager.AddObtainedProgression(obtained, placeItem);
                        continue;
                    }
                }
            }
        }

        private static void ClearDirectedTransitions()
        {
            leftTransitions = new List<string>();
            rightTransitions = new List<string>();
            topTransitions = new List<string>();
            botTransitions = new List<string>();
        }

        private static void AddToDirectedTransitions(List<string> newTransitions)
        {
            leftTransitions.AddRange(newTransitions.Where(transition => LogicManager.GetTransitionDef(transition).doorName.StartsWith("left")));
            rightTransitions.AddRange(newTransitions.Where(transition => LogicManager.GetTransitionDef(transition).doorName.StartsWith("right") || LogicManager.GetTransitionDef(transition).doorName.StartsWith("door")));
            topTransitions.AddRange(newTransitions.Where(transition => LogicManager.GetTransitionDef(transition).doorName.StartsWith("top")));
            botTransitions.AddRange(newTransitions.Where(transition => LogicManager.GetTransitionDef(transition).doorName.StartsWith("bot")));
        }
        private static void RemoveFromDirectedTransitions(List<string> usedTransitions)
        {
            leftTransitions = leftTransitions.Except(usedTransitions).ToList();
            rightTransitions = rightTransitions.Except(usedTransitions).ToList();
            topTransitions = topTransitions.Except(usedTransitions).ToList();
            botTransitions = botTransitions.Except(usedTransitions).ToList();
        }

        private static List<string> GetReachableTransitions(int[] externalObtained)
        {
            List<string> reachableTransitions = new List<string>();

            int[] obtained;
            int[] newObtained = new List<int>(externalObtained).ToArray();
            bool done = false;

            while (!done)
            {
                obtained = new List<int>(newObtained).ToArray();
                foreach (string transition in LogicManager.TransitionNames)
                {   
                    if (LogicManager.GetTransitionDef(transition).oneWay == 2)
                    {
                        string s = transitionPlacements.FirstOrDefault(x => x.Value == transition).Key;
                        if (s != null && LogicManager.ParseProcessedLogic(s, obtained)) reachableTransitions.Add(transition); 
                    }
                    else if (!LogicManager.GetTransitionDef(transition).isolated 
                        && LogicManager.ParseProcessedLogic(transition, obtained)) reachableTransitions.Add(transition);
                    else if (transitionPlacements.TryGetValue(transition, out string altTransition)
                        && LogicManager.GetTransitionDef(altTransition).oneWay != 2 
                        && !LogicManager.GetTransitionDef(altTransition).isolated 
                        && LogicManager.ParseProcessedLogic(altTransition, obtained)) reachableTransitions.Add(transition);
                }

                foreach (string transition in reachableTransitions) newObtained = LogicManager.AddObtainedProgression(newObtained, transition);
                done = newObtained.SequenceEqual(obtained);
            }
            return reachableTransitions;
        }

        private static List<string> GetReachableLocations(int[] obtained)
        {
            List<string> reachable = new List<string>();
            foreach (string location in unobtainedLocations)
            {
                if (LogicManager.ParseProcessedLogic(location, obtained)) reachable.Add(location);
            }
            return reachable;
        }

        private static int[] AddReachableTransitions(int[] obtained)
        {
            List<string> reachable = GetReachableTransitions(obtained);
            foreach (string transition in reachable) LogicManager.AddObtainedProgression(obtained, transition);
            return obtained;
        }

        private static bool TestLegalDirections(string doorName)
        {
            switch (doorName.Substring(0, 3))
            {
                case "doo":
                case "rig":
                    if (leftTransitions.Any()) return true;
                    break;
                case "lef":
                    if (rightTransitions.Any()) return true;
                    break;
                case "top":
                    if (botTransitions.Any()) return true;
                    break;
                case "bot":
                    if (topTransitions.Any()) return true;
                    break;
            }
            return false;
        }

        private static string GetNextTransition(string doorName)
        {
            string transitionSource = string.Empty;
            switch (doorName.Substring(0, 3))
            {
                case "doo":
                case "rig":
                    transitionSource = leftTransitions[rand.Next(leftTransitions.Count)];
                    break;
                case "lef":
                    transitionSource = rightTransitions[rand.Next(rightTransitions.Count)];
                    break;
                case "top":
                    transitionSource = botTransitions[rand.Next(botTransitions.Count)];
                    break;
                case "bot":
                    transitionSource = topTransitions[rand.Next(topTransitions.Count)];
                    break;
            }
            return transitionSource;
        }

        private static List<string> GetProgressionTransitions(int[] externalObtained)
        {
            int[] obtained = new List<int>(externalObtained).ToArray();

            List<string> reachableTransitions = GetReachableTransitions(obtained);
            List<string> progression = new List<string>();

            List<string> candidateTransitions = unplacedTransitions.Except(reachableTransitions).ToList();

            foreach (string transition in candidateTransitions)
            {
                obtained = LogicManager.AddObtainedProgression(obtained, transition);
                List<string> newTransitions = GetReachableTransitions(obtained).Except(reachableTransitions).ToList();
                newTransitions.Remove(transition);
                obtained = LogicManager.RemoveObtainedProgression(obtained, transition);

                if (newTransitions.Intersect(candidateTransitions).Any())
                {
                    progression.Add(transition);
                }
            }
            return progression;
        }

        private static List<string> GetProgressionItems(int[] externalObtained)
        {
            int[] obtained = new List<int>(externalObtained).ToArray();
            int reachableCount = GetReachableLocations(obtained).Count;
            List<string> progression = new List<string>();
            foreach (string str in unobtainedItems)
            {
                if (LogicManager.GetItemDef(str).progression)
                {
                    obtained = new List<int>(externalObtained).ToArray();
                    obtained = LogicManager.AddObtainedProgression(obtained, str);
                    obtained = AddReachableTransitions(obtained);

                    if (GetReachableLocations(obtained).Count > reachableCount) progression.Add(str);
                }
            }
            return progression;
        }

        private static List<string> GetDeepProgression(int[] externalObtained)
        {
            int[] obtained = new List<int>(externalObtained).ToArray();
            int reachableCount = GetReachableLocations(obtained).Count;
            List<string> progression = new List<string>();
            List<string> candidateItems = GetAreaCandidateItems();
            deepProgressionTransitions = new Dictionary<string, string>();

            foreach (string str in candidateItems)
            {
                obtained = new List<int>(externalObtained).ToArray();
                obtained = LogicManager.AddObtainedProgression(obtained, str);
                obtained = AddReachableTransitions(obtained);

                ClearDirectedTransitions();
                AddToDirectedTransitions(GetReachableTransitions(obtained).Intersect(unplacedTransitions).ToList());

                if (GetReachableLocations(obtained).Count > reachableCount) progression.Add(str);
                else
                {
                    List<string> candidateTransitions = GetProgressionTransitions(obtained).Where(transition => TestLegalDirections(LogicManager.GetTransitionDef(transition).doorName)).ToList();
                    List<string> foundTransitions = new List<string>();

                    foreach (string transition in candidateTransitions)
                    {
                        obtained = new List<int>(externalObtained).ToArray();
                        obtained = LogicManager.AddObtainedProgression(obtained, str);
                        obtained = LogicManager.AddObtainedProgression(obtained, transition);
                        if (GetReachableLocations(obtained).Count > reachableCount)
                        {
                            foundTransitions.Add(transition);
                        }
                    }
                    if (foundTransitions.Count > 0)
                    {
                        progression.Add(str);
                        deepProgressionTransitions.Add(str, foundTransitions[rand.Next(foundTransitions.Count)]);
                    }
                }
            }
            ClearDirectedTransitions();
            AddToDirectedTransitions(unplacedTransitions);

            return progression;
        }

        private static List<string> GetItemCandidateItems()
        {
            List<string> progression = new List<string>();

            foreach (string str in unobtainedItems)
            {
                if (LogicManager.GetItemDef(str).itemCandidate) progression.Add(str);
            }

            return progression;
        }

        private static List<string> GetAreaCandidateItems()
        {
            List<string> progression = new List<string>();

            foreach (string str in unobtainedItems)
            {
                if (LogicManager.GetItemDef(str).areaCandidate) progression.Add(str);
            }

            return progression;
        }

        private static void PlaceNextItem()
        {
            string placeItem;
            string placeLocation;

            // Acquire unweighted accessible locations
            List<string> reachableLocations = GetReachableLocations(obtainedProgression);
            int reachableCount = reachableLocations.Count;
            //We place items randomly while there are many reachable spaces
            if (reachableCount > 1 && unobtainedItems.Count > 0)
            {
                placeItem = unobtainedItems[rand.Next(unobtainedItems.Count)];
                placeLocation = reachableLocations[rand.Next(reachableLocations.Count)];
            }
            // This path handles forcing progression items when few random locations are left
            else if (reachableCount == 1)
            {
                List<string> progressionItems = GetProgressionItems(obtainedProgression); // Progression items which open new locations
                List<string> areaCandidateItems = GetAreaCandidateItems();
                List<string> itemCandidateItems = GetItemCandidateItems();
                if (progressionItems.Count > 0)
                {
                    placeItem = progressionItems[rand.Next(progressionItems.Count)];
                    placeLocation = reachableLocations[0];
                    if (placeLocation == "Fury_of_the_Fallen" && LogicManager.GetItemDef(placeItem).isGoodItem) placeItem = progressionItems[rand.Next(progressionItems.Count)];
                    if (placeLocation == "Fury_of_the_Fallen" && placeItem == "Mantis_Claw") placeItem = progressionItems[rand.Next(progressionItems.Count)];
                }
                else if (unobtainedLocations.Count > 1 && areaCandidateItems.Count > 0)
                {
                    overflow = true;
                    placeItem = areaCandidateItems[rand.Next(areaCandidateItems.Count)];
                    progressionStandby.Add(placeItem); // Note that we don't have enough locations to place candidate items here, so they go onto a standby list until the second pass
                    unobtainedItems.Remove(placeItem);
                    obtainedProgression = LogicManager.AddObtainedProgression(obtainedProgression, placeItem);
                    obtainedProgression = AddReachableTransitions(obtainedProgression);
                    return;
                }
                else if (unobtainedLocations.Count > 1 && itemCandidateItems.Count > 0)
                {
                    overflow = true;
                    placeItem = itemCandidateItems[rand.Next(itemCandidateItems.Count)];
                    progressionStandby.Add(placeItem); // Note that we don't have enough locations to place candidate items here, so they go onto a standby list until the second pass
                    unobtainedItems.Remove(placeItem);
                    obtainedProgression = LogicManager.AddObtainedProgression(obtainedProgression, placeItem);
                    obtainedProgression = AddReachableTransitions(obtainedProgression);
                    return;
                }
                else // This is how the last reachable location is filled
                {
                    placeItem = unobtainedItems[rand.Next(unobtainedItems.Count)];
                    placeLocation = reachableLocations[0];
                }
            }
            else // No reachable locations, ready to proceed to next stage
            {
                firstPassDone = true;
                return;
            }
            if (!overflow && !LogicManager.GetItemDef(placeItem).progression)
            {
                junkStandby.Add(placeItem);
                locationStandby.Add(placeLocation);
                reachableLocations.Remove(placeLocation);
                unobtainedLocations.Remove(placeLocation);
                unobtainedItems.Remove(placeItem);
            }
            else
            {
                reachableLocations.Remove(placeLocation);
                unobtainedLocations.Remove(placeLocation);
                unobtainedItems.Remove(placeItem);
                if (LogicManager.GetItemDef(placeItem).progression)
                {
                    obtainedProgression = LogicManager.AddObtainedProgression(obtainedProgression, placeItem);
                    obtainedProgression = AddReachableTransitions(obtainedProgression);
                }
                if (shopItems.ContainsKey(placeLocation))
                {
                    shopItems[placeLocation].Add(placeItem);
                }
                else
                {
                    nonShopItems.Add(placeLocation, placeItem);
                }
                return;
            }
        }

        private static void PlaceRemainingItems()
        {
            foreach (string placeItem in junkStandby) unobtainedItems.Add(placeItem);

            // First, we have to guarantee that items used in the logic chain are accessible
            foreach (string placeItem in progressionStandby)
            {
                if (locationStandby.Count > 0)
                {
                    string placeLocation = locationStandby[rand.Next(locationStandby.Count)];
                    locationStandby.Remove(placeLocation);
                    if (shopItems.ContainsKey(placeLocation))
                    {
                        shopItems[placeLocation].Add(placeItem);
                    }
                    else
                    {
                        nonShopItems.Add(placeLocation, placeItem);
                    }
                }
                else
                {
                    string placeLocation = shopNames[rand.Next(5)];
                    shopItems[placeLocation].Add(placeItem);
                }
            }

            // We fill the remaining locations and shops with the leftover junk
            while (unobtainedItems.Count > 0)
            {
                string placeItem = unobtainedItems[rand.Next(unobtainedItems.Count)];
                unobtainedItems.Remove(placeItem);
                if (unobtainedLocations.Count > 0)
                {
                    string placeLocation = unobtainedLocations[rand.Next(unobtainedLocations.Count)];
                    unobtainedLocations.Remove(placeLocation);
                    if (shopItems.ContainsKey(placeLocation))
                    {
                        shopItems[placeLocation].Add(placeItem);
                    }
                    else
                    {
                        nonShopItems.Add(placeLocation, placeItem);
                    }
                }
                else if (locationStandby.Count > 0)
                {
                    string placeLocation = locationStandby[rand.Next(locationStandby.Count)];
                    locationStandby.Remove(placeLocation);
                    if (shopItems.ContainsKey(placeLocation))
                    {
                        shopItems[placeLocation].Add(placeItem);
                    }
                    else
                    {
                        nonShopItems.Add(placeLocation, placeItem);
                    }
                }
                else
                {
                    string placeLocation = shopNames[rand.Next(5)];
                    shopItems[placeLocation].Add(placeItem);
                }
            }
        }

        private static bool CheckPlacementValidity()
        {
            Stopwatch validationWatch = new Stopwatch();
            validationWatch.Start();
            AreaRando.Instance.Log("Beginning seed validation...");

            List<string> floorItems = nonShopItems.Keys.ToList();

            List<string> everything = new List<string>();
            everything.AddRange(floorItems);
            everything.AddRange(LogicManager.ShopNames);
            everything.AddRange(LogicManager.TransitionNames);

            int[] obtained = new int[LogicManager.bitMaskMax + 1];
            int passes = 0;
            while (everything.Any())
            {
                obtained = AddReachableTransitions(obtained);
                everything = everything.Except(GetReachableTransitions(obtained)).ToList();
                foreach (string itemName in floorItems)
                {
                    if (everything.Contains(itemName) && LogicManager.ParseProcessedLogic(itemName, obtained))
                    {
                        everything.Remove(itemName);
                        if (LogicManager.GetItemDef(nonShopItems[itemName]).progression) obtained = LogicManager.AddObtainedProgression(obtained, nonShopItems[itemName]);
                    }
                }
                foreach (string shopName in shopNames)
                {
                    if (everything.Contains(shopName) && LogicManager.ParseProcessedLogic(shopName, obtained))
                    {
                        everything.Remove(shopName);
                        foreach (string newItem in shopItems[shopName])
                        {
                            if (LogicManager.GetItemDef(newItem).progression) obtained = LogicManager.AddObtainedProgression(obtained, newItem);
                        }
                    }
                }
                
                passes++;
                if (passes > 400)
                {
                    Log("Unable to validate!");
                    Log("Able to get: " + LogicManager.ListObtainedProgression(obtained));
                    string m = string.Empty;
                    foreach (string s in everything) m += s + ", ";
                    Log("Unable to get: " + m);
                    return false;
                }
            }
            Log("Validation successful.");
            return true;
        }

        private static void LogAllPlacements()
        {
            Log("Logging transition placements");
            foreach (KeyValuePair<string, string> kvp in transitionPlacements)
            {
                LogTransitionPlacement(kvp.Key, kvp.Value);
            }


            Log("Logging progression item placements:");
            foreach (KeyValuePair<string, List<string>> kvp in shopItems)
            {
                foreach (string item in kvp.Value)
                {
                    if (LogicManager.GetItemDef(item).progression) LogItemPlacement(item, kvp.Key);
                }
            }

            foreach (KeyValuePair<string, string> kvp in nonShopItems)
            {
                if (LogicManager.GetItemDef(kvp.Value).progression) LogItemPlacement(kvp.Value, kvp.Key);
            }

            Log("Logging ordinary item placements:");
            foreach (KeyValuePair<string, List<string>> kvp in shopItems)
            {
                foreach (string item in kvp.Value)
                {
                    if (!LogicManager.GetItemDef(item).progression) LogItemPlacement(item, kvp.Key);
                }
            }

            foreach (KeyValuePair<string, string> kvp in nonShopItems)
            {
                if (!LogicManager.GetItemDef(kvp.Value).progression) LogItemPlacement(kvp.Value, kvp.Key);
            }
        }
        private static void LogTransitionPlacement(string entrance, string exit)
        {
            AreaRando.Instance.Settings.AddTransitionPlacement(entrance, exit);
            Log("Entrance " + entrance + " linked to exit " + exit);
        }
        private static void LogItemPlacement(string item, string location)
        {
            AreaRando.Instance.Settings.AddItemPlacement(item, location);
            Log(
                $"Putting item \"{item.Replace('_', ' ')}\" at \"{location.Replace('_', ' ')}\"");
        }
    }
}