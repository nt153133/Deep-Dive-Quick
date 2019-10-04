/*
DeepDungeon2 is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.

Orginal work done by zzi, contibutions by Omninewb, Freiheit, and mastahg
                                                                                 */

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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Deep2.TaskManager.Actions;
using ff14bot.Navigation;
using TreeSharp;

namespace Deep2.Providers
{
    internal class DDTargetingProvider
    {
        private static DDTargetingProvider _instance;
        private int _count;

        private int _floor;
        private Vector3 _lastLoc;
        private DateTime _lastPulse = DateTime.MinValue;

        public DDTargetingProvider()
        {
            LastEntities = new ReadOnlyCollection<GameObject>(new List<GameObject>());
        }

        internal static DDTargetingProvider Instance => _instance ?? (_instance = new DDTargetingProvider());

       // public ReadOnlyCollection<GameObject> LastEntities => new ReadOnlyCollection<GameObject>(GetObjectsByWeight());

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

                if (Settings.Instance.GoExit)
                {
                    if (Settings.Instance.GoForTheHoard)
                        return !LastEntities.Any(i =>
                            (i.NpcId == EntityNames.Hidden || i.NpcId == EntityNames.BandedCoffer) &&
                            !Blacklist.Contains(i.ObjectId, (BlacklistFlags) DeepDungeonManager.Level));

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
            
            if (DirectorManager.ActiveDirector is InstanceContentDirector activeAsInstance)
            {
                if (activeAsInstance.TimeLeftInDungeon == TimeSpan.Zero)
                {
                    return;
                }
            }

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

            //using (new PerformanceLogger("Targeting Pulse"))
            // {
           // GameObjectManager.Update();
            LastEntities = new ReadOnlyCollection<GameObject>(GetObjectsByWeight());

            if (_lastPulse + TimeSpan.FromSeconds(5) >= DateTime.Now) return;
            Logger.Verbose($"Found {LastEntities.Count} Targets");

            if (_lastLoc == Core.Me.Location && !Core.Me.HasTarget)
            {
                Logger.Verbose($"Stuck but found {LastEntities.Count} Targets");

                if (_count > 3)
                {
                    Logger.Verbose($"[Stuck] COUNTER TRIGGERED... Do Something but found {LastEntities.Count} Targets");
                    _count = 0;
                    _lastLoc = Core.Me.Location;
                    _lastPulse = DateTime.Now;

                    GameObjectManager.Update();

                    DDNavigationProvider.trapList.RemoveWhere(r =>
                        r.Center.Distance2D(Core.Me.Location) < 15 || r.Center.Distance2D(Poi.Current.Location) < 15);
                    
                    Logger.Debug("Going to starting room and clearing traps");
                    Navigator.PlayerMover.MoveTowards(Poi.Current.Location);
                    Thread.Sleep(500);
                    Navigator.Stop();
                    if (Poi.Current.Unit != null)
                        DDTargetingProvider.Instance.AddToBlackList(Poi.Current.Unit, TimeSpan.FromSeconds(20),
                            "Navigation Error");
                    Poi.Clear("Stuck?");
                    Navigator.Clear();
                    
                    
                    //Poi.Current = new Poi(DDNavigationProvider.startingLoc, (PoiType) PoiTypes.ExplorePOI);
                   // Navigator.MoveTo(
                   //     POTDNavigation.SafeSpots.OrderByDescending(i => i.Distance2D(Core.Me.Location)).First(),
                  //      "SafeSpot)");
                    //DDNavigationProvider.trapList.Clear();
                }

                _count++;
            }

            _lastLoc = Core.Me.Location;
            _lastPulse = DateTime.Now;
            // }
        }

        //{
        //    get
        //    {
        //        var badGuys = (CombatTargeting.Instance.Provider as DDCombatTargetingProvider)?.GetObjectsByWeight();

        // var anyBadGuysAround = badGuys != null && badGuys.Any();

        // //if (Beta.Target != null && Beta.Target.IsValid &&
        // !Blacklist.Contains(Beta.Target.ObjectId, (BlacklistFlags)DeepDungeonManager.Level) &&
        // Beta.Target.Type != GameObjectType.GatheringPoint) // return null;

        // // Party member is dead if (PartyManager.AllMembers.Any(member => member.CurrentHealth ==
        // 0)) { // Select Cairn of Return as highest priority if it is known and can be used. if
        // (CairnOfReturn != null && DeepDungeonManager.ReturnActive) return CairnOfReturn;

        // // If the Cairn of Return is not yet active and there are any mobs around: Kill the mobs.
        // if (anyBadGuysAround) return new Poi(badGuys.First(), PoiType.Kill); }

        // // Cairn of Passage if (LevelComplete && Portal != null) return Portal;

        // // Bosses or Pomander of Rage / Pomander of Lust if ((DeepDungeonManager.BossFloor ||
        // Core.Me.HasAura(Auras.Lust)) && anyBadGuysAround) return new Poi(badGuys.First(), PoiType.Kill);

        // // Chests if (LastEntities != null && LastEntities.Any()) return LastEntities.First();

        // // Kill something if (anyBadGuysAround) return new Poi(badGuys.First(), PoiType.Kill);

        // return new Poi( SafeSpots.OrderByDescending(i => i.Distance2D(Core.Me.Location)).First(),
        // PoiType.Hotspot );

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
            if (DeepDungeonManager.PortalActive)
            {
                return GameObjectManager.GameObjects
                    .Where(Filter)
                    .OrderByDescending(SortComplete)
                    .ToList();
            }

            return GameObjectManager.GameObjects
                .Where(Filter)
                .OrderByDescending(Sort)
                .ToList();

        }

        public static float Sort(GameObject obj)
        {
            var weight = 150f;

            if (PartyManager.IsInParty && !PartyManager.IsPartyLeader && !DeepDungeonManager.BossFloor)
            {
                if (PartyManager.PartyLeader.IsInObjectManager && PartyManager.PartyLeader.CurrentHealth > 0)
                {
                    if (PartyManager.PartyLeader.BattleCharacter.HasTarget)
                        if (obj.ObjectId == PartyManager.PartyLeader.BattleCharacter.TargetGameObject.ObjectId)
                            weight += 600;
                    weight -= obj.Distance2D(PartyManager.PartyLeader.GameObject);
                }
                else
                {
                    weight -= obj.Distance2D();
                }
            }
            else
            {
                weight -= obj.Distance2D();
            }

            switch (obj.Type)
            {
                case GameObjectType.BattleNpc:
                    weight /= 2;
                    break;
                case GameObjectType.Treasure:
                    //weight += 10;
                    break;
            }

            /*
            if (obj.NpcId == EntityNames.BandedCoffer)
                weight += 500;

            if (obj.NpcId == EntityNames.BandedCoffer && !Blacklist.Contains(obj.ObjectId)) weight += 200;
            */
            
            return weight;
        }
        
        public static float SortComplete(GameObject obj)
        {
            var weight = 150f;

            if (PartyManager.IsInParty && !PartyManager.IsPartyLeader && !DeepDungeonManager.BossFloor)
            {
                if (PartyManager.PartyLeader.IsInObjectManager && PartyManager.PartyLeader.CurrentHealth > 0)
                {
                    if (PartyManager.PartyLeader.BattleCharacter.HasTarget)
                        if (obj.ObjectId == PartyManager.PartyLeader.BattleCharacter.TargetGameObject.ObjectId)
                            weight += 600;
                    weight -= obj.Distance2D(PartyManager.PartyLeader.GameObject);
                }
                else
                {
                    weight -= obj.Distance2D();
                }
            }
            else
            {
                if (FloorExit.location != Vector3.Zero)
                {
                    weight -= Core.Me.Distance2D(Vector3.Lerp(obj.Location, FloorExit.location, 0.25f) ); 
                }
                else
                {
                    weight -= obj.Distance2D();    
                }
            }

            switch (obj.Type)
            {
                case GameObjectType.BattleNpc when !PartyManager.IsInParty:
                    return weight / 2;

                case GameObjectType.BattleNpc:
                    weight /= 2;
                    break;
                case GameObjectType.Treasure:
                    break;
            }
/*
            if (obj.NpcId == EntityNames.BandedCoffer)
                weight += 500;

            if (DeepDungeonManager.PortalActive && Settings.Instance.GoForTheHoard && obj.NpcId == EntityNames.Hidden)
                weight += 5;
                */
            //else if (DeepDungeonManager.PortalActive && Settings.Instance.GoExit &&
            //         obj.NpcId != EntityNames.FloorExit && PartyManager.IsInParty)
            //    weight -= 10;

            //if (obj.NpcId == EntityNames.BandedCoffer && !Blacklist.Contains(obj.ObjectId)) weight += 200;

            //if (DeepDungeonManager.PortalActive && obj.NpcId == EntityNames.FloorExit &&
            // (Core.Me.HasAura(Auras.NoAutoHeal) || Core.Me.HasAura(Auras.Amnesia))) weight += 500;

            //if (DeepDungeonManager.PortalActive && Settings.Instance.GoExit) weight -= 10;

            if (DeepDungeonManager.PortalActive && Settings.Instance.GoForTheHoard && obj.NpcId == EntityNames.Hidden)
                weight += 5;
            //else if (DeepDungeonManager.PortalActive && Settings.Instance.GoExit &&
            //         obj.NpcId != EntityNames.FloorExit && PartyManager.IsInParty)
            //    weight -= 10;

            return weight;
        }

        public static bool Filter(GameObject obj)
        {
            //Blacklists
            if (Blacklist.Contains(obj) || Constants.TrapIds.Contains(obj.NpcId) ||
                Constants.IgnoreEntity.Contains(obj.NpcId))
                return false;

            if (obj.Location == Vector3.Zero)
                return false;

            //If there is more than 1 of Str,Lust,Steel then skip gold chest
            /*           
            if (DeepDungeonManager.HaveMainPomander && obj.NpcId == EntityNames.GoldCoffer &&
                (!Settings.Instance.OpenGold && DeepDungeonManager.PortalActive && FloorExit.location != Vector3.Zero))
                return false;
            */
            
            switch (obj.Type)
            {
                case GameObjectType.Treasure:
                    return !(!Settings.Instance.OpenGold && DeepDungeonManager.HaveMainPomander &&
                             DeepDungeonManager.PortalActive && FloorExit.location != Vector3.Zero);
                case GameObjectType.EventObject:
                    return true;
                case GameObjectType.BattleNpc:
                    return !((BattleCharacter) obj).IsDead;
                default:
                    return false;
            }

            /*
            if (obj.Type != GameObjectType.BattleNpc)
                return obj.Type == GameObjectType.EventObject || obj.Type == GameObjectType.Treasure ||
                       obj.Type == GameObjectType.BattleNpc;

            BattleCharacter battleCharacter = (BattleCharacter) obj;
            return !battleCharacter.IsDead;
            */
        }
        
        public static bool FilterKnown(GameObject obj)
        {
            if (obj.Location == Vector3.Zero)
                return false;
            //Blacklists
            if (Blacklist.Contains(obj) || Constants.TrapIds.Contains(obj.NpcId) ||
                Constants.IgnoreEntity.Contains(obj.NpcId))
                return false;

            

            //If there is more than 1 of Str,Lust,Steel then skip gold chest
            /*           
            if (DeepDungeonManager.HaveMainPomander && obj.NpcId == EntityNames.GoldCoffer &&
                (!Settings.Instance.OpenGold && DeepDungeonManager.PortalActive && FloorExit.location != Vector3.Zero))
                return false;
            */
            
            switch (obj.Type)
            {
                case GameObjectType.Treasure:
                    return true;
                case GameObjectType.EventObject:
                    return true;
                case GameObjectType.BattleNpc:
                    return true;
                default:
                    return false;
            }

            /*
            if (obj.Type != GameObjectType.BattleNpc)
                return obj.Type == GameObjectType.EventObject || obj.Type == GameObjectType.Treasure ||
                       obj.Type == GameObjectType.BattleNpc;

            BattleCharacter battleCharacter = (BattleCharacter) obj;
            return !battleCharacter.IsDead;
            */
        }
    }
}