﻿/*
DeepDungeon2 is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.

Orginal work done by zzi, contibutions by Omninewb, Freiheit, and mastahg
                                                                                 */

using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Utilities;
using Deep2.Helpers;
using Deep2.Providers;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Pathing;

namespace Deep2.TaskManager.Actions
{
    internal class FloorExit : ITask
    {
        private int Level;
        private Vector3 location = Vector3.Zero;

        private Poi Target => Poi.Current;
        public string Name => "Floor Exit";

        public async Task<bool> Run()
        {
            if (Target.Type != PoiType.Wait)
                return false;

            //let the navigator handle movement if we are far away
            if (Target.Location.Distance2D(Core.Me.Location) > 3)
                return false;

            // move closer plz
            if (Target.Location.Distance2D(Core.Me.Location) >= 2)
            {
                await CommonTasks.MoveAndStop(new MoveToParameters(Target.Location, "Floor Exit"), 0.5f, true);
                return true;
            }

            await CommonTasks.StopMoving();

            var _level = DeepDungeonManager.Level;
            await Coroutine.Wait(-1,
                () => Core.Me.InCombat || _level != DeepDungeonManager.Level || CommonBehaviors.IsLoading ||
                      QuestLogManager.InCutscene);
            Poi.Clear("Floor has changed or we have entered combat");
            Navigator.Clear();
            return true;
        }


        public void Tick()
        {
            if (!Constants.InDeepDungeon || CommonBehaviors.IsLoading || QuestLogManager.InCutscene)
                return;

            if (location == Vector3.Zero || Level != DeepDungeonManager.Level)
            {
                var ret = GameObjectManager.GetObjectByNPCId(EntityNames.FloorExit);
                if (ret != null)
                {
                    Level = DeepDungeonManager.Level;
                    location = ret.Location;
                }
                else if (Level != DeepDungeonManager.Level)
                {
                    Level = DeepDungeonManager.Level;
                    location = Vector3.Zero;
                }
            }

            //if we are in combat don't move toward the carn of return
            if (Poi.Current != null && (Poi.Current.Type == PoiType.Kill || Poi.Current.Type == PoiType.Wait ||
                                        Poi.Current.Type == PoiType.Collect))
                return;

            if (DDTargetingProvider.Instance.LevelComplete && !DeepDungeonManager.BossFloor && location != Vector3.Zero)
                Poi.Current = new Poi(location, PoiType.Wait);
        }
    }
}