/*
DeepDungeon2 is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.

Orginal work done by zzi, contibutions by Omninewb, Freiheit, and mastahg
                                                                                 */

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Utilities;
using Clio.Utilities.Helpers;
using Deep2.Helpers;
using Deep2.Helpers.Logging;
using Deep2.Providers;
using ff14bot;
using ff14bot.Helpers;
using ff14bot.Managers;

namespace Deep2.TaskManager.Actions
{
    public class SideStepTask : ITask
    {
        private static FieldInfo MoveTo;
        internal readonly WaitTimer MoveTimer = new WaitTimer(TimeSpan.FromSeconds(5));

        static SideStepTask()
        {
            MoveTo = typeof(AvoidanceManager).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                .First(i => !i.IsInitOnly && i.FieldType == typeof(Vector3));
        }

        public string Name => nameof(SideStepTask);

        public async Task<bool> Run()
        {
            var supportsCapabilities = RoutineManager.Current.SupportedCapabilities != CapabilityFlags.None;

            // if (DeepTracker._debug != null) DeepTracker._debug.progB.Value = DeepDungeonManager.PortalStatus;
            /*
            if (MoveTimer.IsFinished)
            {
                if (DDTargetingProvider.Instance.LastEntities != null)
                {
                    DeepTracker._debug.listBox1.Items.Clear();
                    //DeepTracker._debug.
                    foreach (var i in DDTargetingProvider.Instance.LastEntities)
                    {
                        DeepTracker._debug.listBox1.Items.Add(i);
                    }
                    DeepTracker._debug.Update();
                    DeepTracker._debug.Refresh();

                    Logger.Debug($"LastEntities count: {DDTargetingProvider.Instance.LastEntities.Count}");

                    await Coroutine.Sleep(500);
                }
                MoveTimer.Reset();
            }

            */
            //      else
            //           DeepTracker._debug.listBox1.Items.Add("Null");
            //DoEvents();

            if (AvoidanceManager.IsRunningOutOfAvoid && Core.Me.IsCasting)
            {
                ActionManager.StopCasting();
                return true;
            }

            if (AvoidanceManager.IsRunningOutOfAvoid && !supportsCapabilities)
                return true;
            var poiType = Poi.Current.Type;

            // taken from HB Special case: Bot will do a lot of fast stop n go when avoiding a mob
            // that moves slowly and trying to do something near the mob. To fix, a delay is added to
            // slow down the 'Stop n go' behavior
            if (poiType == PoiType.Collect || poiType == PoiType.Gather || poiType == PoiType.Hotspot)
                if (Core.Me.InCombat && AvoidanceManager.Avoids.Any(o => o.IsPointInAvoid(Poi.Current.Location)))
                {
                    TreeRoot.StatusText = "Waiting for 'avoid' to move before attempting to interact " +
                                          Poi.Current.Name;
                    var randomWaitTime = new Random().Next(3000, 8000);
                    await Coroutine.Wait(randomWaitTime,
                        () => Core.Me.InCombat ||
                              !AvoidanceManager.Avoids.Any(o => o.IsPointInAvoid(Poi.Current.Location)));
                }

            return false;
        }

        public void Tick()
        {
        }

        public void Start()
        {
            MoveTimer.Reset();
        }

        public void Stop()
        {
        }
    }
}