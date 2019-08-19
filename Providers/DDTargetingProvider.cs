/*
DeepDungeon2 is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.

Orginal work done by zzi, contibutions by Omninewb, Freiheit, and mastahg
                                                                                 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Clio.Utilities;
using Deep2.Helpers;
using Deep2.Helpers.Logging;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Directors;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;

namespace Deep2.Providers
{
    internal class DDTargetingProvider
    {
        private static DDTargetingProvider _instance;

        private int _floor;
        private DateTime _lastPulse = DateTime.MinValue;
        private Vector3 _lastLoc;
        private int _count;
        public DDTargetingProvider()
        {
            LastEntities = new ReadOnlyCollection<GameObject>(new List<GameObject>());
        }

        internal static DDTargetingProvider Instance => _instance ?? (_instance = new DDTargetingProvider());


        public ReadOnlyCollection<GameObject> LastEntities { get; set; }

        internal bool LevelComplete
        {
            get
            {
                if (!DeepDungeonManager.PortalActive)
                    return false;

                if (Settings.Instance.GoExit && PartyManager.IsInParty)
                {
                    if (PartyManager.AllMembers.Any(i => i.CurrentHealth == 0))
                        return false;

                    if (Settings.Instance.GoForTheHoard)
                        return !LastEntities.Any(i =>
                            (i.NpcId == EntityNames.Hidden || i.NpcId == EntityNames.BandedCoffer) &&
                            !Blacklist.Contains(i.ObjectId, (BlacklistFlags) DeepDungeonManager.Level));

                    //Logger.Instance.Verbose("Full Explore : {0} {1}", _levelComplete, !NotMobs().Any());
                    return true;
                }

                return !LastEntities.Any();
            }
        }

        /// <summary>
        ///     decide what we need to do
        /// </summary>
        public GameObject FirstEntity => LastEntities.FirstOrDefault();

        internal void Reset()
        {
            Blacklist.Clear(i => true);
        }

        internal void Pulse()
        {
            if (CommonBehaviors.IsLoading)
                return;

            if (!Constants.InDeepDungeon)
                return;

            if (_floor != DeepDungeonManager.Level)
            {
                Logger.Info("Level has Changed. Clearing Targets");
                _floor = DeepDungeonManager.Level;
                Blacklist.Clear(i => i.Flags == (BlacklistFlags) DeepDungeonManager.Level);
            }

            //if (CairnOfReturn != null && !CairnOfReturn.IsValid)
            //    CairnOfReturn = null;

            //if (Portal != null && !Portal.IsValid)
            //    Portal = null;

            using (new PerformanceLogger("Targeting Pulse"))
            {
                LastEntities = new ReadOnlyCollection<GameObject>(GetObjectsByWeight());

                if (_lastPulse + TimeSpan.FromSeconds(5) < DateTime.Now)
                {
                    Logger.Verbose($"Found {LastEntities.Count} Targets");

                    if (_lastLoc == Core.Me.Location && !Core.Me.HasTarget)
                    {
                        Logger.Verbose($"Stuck but found {LastEntities.Count} Targets");

                        if (_count > 5 )
                        {
                            Logger.Verbose($"[Stuck] COUNTER TRIGGERED... Do Something but found {LastEntities.Count} Targets");
                            _count = 0;
                            _lastLoc = Core.Me.Location;
                            _lastPulse = DateTime.Now;

                            if (DirectorManager.ActiveDirector == null)
                                DirectorManager.Update();
                            DDTargetingProvider.Instance.Pulse();
                            
                        }

                        _count++;
                    }

                    _lastLoc = Core.Me.Location;
                    _lastPulse = DateTime.Now;
                }
            }
        }
        //{
        //    get
        //    {
        //        var badGuys = (CombatTargeting.Instance.Provider as DDCombatTargetingProvider)?.GetObjectsByWeight();

        //        var anyBadGuysAround = badGuys != null && badGuys.Any();

        //        //if (Beta.Target != null && Beta.Target.IsValid && !Blacklist.Contains(Beta.Target.ObjectId, (BlacklistFlags)DeepDungeonManager.Level) && Beta.Target.Type != GameObjectType.GatheringPoint)
        //        //    return null;

        //        // Party member is dead
        //        if (PartyManager.AllMembers.Any(member => member.CurrentHealth == 0))
        //        {
        //            // Select Cairn of Return as highest priority if it is known and can be used.
        //            if (CairnOfReturn != null && DeepDungeonManager.ReturnActive)
        //                return CairnOfReturn;

        //            // If the Cairn of Return is not yet active and there are any mobs around: Kill the mobs.
        //            if (anyBadGuysAround)
        //                return new Poi(badGuys.First(), PoiType.Kill);
        //        }

        //        // Cairn of Passage
        //        if (LevelComplete && Portal != null)
        //            return Portal;

        //        // Bosses or Pomander of Rage / Pomander of Lust
        //        if ((DeepDungeonManager.BossFloor || Core.Me.HasAura(Auras.Lust)) && anyBadGuysAround)
        //            return new Poi(badGuys.First(), PoiType.Kill);

        //        // Chests
        //        if (LastEntities != null && LastEntities.Any())
        //            return LastEntities.First();

        //        // Kill something
        //        if (anyBadGuysAround)
        //            return new Poi(badGuys.First(), PoiType.Kill);

        //        return new Poi(
        //            SafeSpots.OrderByDescending(i => i.Distance2D(Core.Me.Location)).First(),
        //            PoiType.Hotspot
        //        );


        //    }
        //}

        internal void AddToBlackList(GameObject obj, string reason)
        {
            AddToBlackList(obj, TimeSpan.FromMinutes(3), reason);
        }

        internal void AddToBlackList(GameObject obj, TimeSpan time, string reason)
        {
            Blacklist.Add(obj, (BlacklistFlags) _floor, time, reason);
            Poi.Clear(reason);
        }

        private List<GameObject> GetObjectsByWeight()
        {
            return GameObjectManager.GameObjects
                .Where(Filter)
                .OrderByDescending(Sort)
                .ToList();
        }

        private float Sort(GameObject obj)
        {
            var weight = 100f;

            weight -= obj.Distance2D();

            if (obj.Type == GameObjectType.BattleNpc) return weight / 2;

            if (obj.NpcId == EntityNames.BandedCoffer)
                weight += 500;

            if (DeepDungeonManager.PortalActive && Settings.Instance.GoForTheHoard && obj.NpcId == EntityNames.Hidden)
                weight += 5;
            else if (DeepDungeonManager.PortalActive && Settings.Instance.GoExit &&
                     obj.NpcId != EntityNames.FloorExit && PartyManager.IsInParty)
                weight -= 10;

            return weight;
        }

        private bool Filter(GameObject obj)
        {
            if (obj.NpcId == 5042) //script object
                return false;
            //Don't pick up the pheonix downs	
/* 			if (obj.Name == "treasure coffer")
				return false; */

            if (obj.Location == Vector3.Zero)
                return false;

            //Blacklists
            if (Blacklist.Contains(obj) || Constants.TrapIds.Contains(obj.NpcId) ||
                Constants.IgnoreEntity.Contains(obj.NpcId))
                return false;

            //Check for Party Chest setting
            if (obj.NpcId == EntityNames.GoldCoffer && Settings.Instance.OpenNone && PartyManager.IsInParty)
                return false;

            var data = DeepDungeonManager.GetInventoryItem(Pomander.Lust);
            var data1 = DeepDungeonManager.GetInventoryItem(Pomander.Strength);
            var data2 = DeepDungeonManager.GetInventoryItem(Pomander.Steel);

            //If there is more than 1 of Str,Lust,Steel then skip gold chest
            if (data.Count > 0 && data1.Count > 0 && data2.Count > 0 && obj.NpcId == EntityNames.GoldCoffer &&
                !Settings.Instance.OpenGold)
                return false;

            if (obj.Type == GameObjectType.BattleNpc)
            {
                if (DeepDungeonManager.PortalActive)
                    return false;

                var battleCharacter = (BattleCharacter) obj;
                return !battleCharacter.IsDead;
            }

            return obj.Type == GameObjectType.EventObject || obj.Type == GameObjectType.Treasure ||
                   obj.Type == GameObjectType.BattleNpc;
        }
    }
}