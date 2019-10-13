/*
DeepDungeon2 is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.

Orginal work done by zzi, contibutions by Omninewb, Freiheit, and mastahg
                                                                                 */

using System.Threading.Tasks;
using Buddy.Coroutines;
using Deep2.Helpers.Logging;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Pathing;

namespace Deep2.TaskManager.Actions
{
    internal class GetToCaptiain : ITask
    {
        public string Name => "GetToCaptain";

        public async Task<bool> Run()
        {
            //we are inside POTD
            if (Constants.InDeepDungeon || Constants.InExitLevel) return false;

            if (WorldManager.ZoneId != Constants.EntranceZone.ZoneId ||
                GameObjectManager.GetObjectByNPCId(Constants.CaptainNpcId).Distance2D(Core.Me.Location) > 110)
            {
                if (Core.Me.IsCasting)
                {
                    await Coroutine.Sleep(1000);
                    return true;
                }

                if (!WorldManager.TeleportById(Constants.EntranceZone.Id))
                {
                    Logger.Error($"We can't get to {Constants.EntranceZone.CurrentLocaleAethernetName}. something is very wrong...");
                    TreeRoot.Stop();
                    return false;
                }

                await Coroutine.Sleep(5000);
                return true;
            }

            if (GameObjectManager.GetObjectByNPCId(Constants.CaptainNpcId) != null &&
                !(Constants.CaptainNpcPosition.Distance2D(Core.Me.Location) > 5f)) return false;
            Logger.Verbose("at Move");

            if (GameObjectManager.GetObjectByNPCId(Constants.CaptainNpcId) != null)
                return await CommonTasks.MoveAndStop(
                    new MoveToParameters(GameObjectManager.GetObjectByNPCId(Constants.CaptainNpcId).Location,
                        "Moving toward NPC"), 5f, true);

            return await CommonTasks.MoveAndStop(
                new MoveToParameters(Constants.CaptainNpcPosition, "Moving toward NPC"), 5f, true);
        }

        public void Tick()
        {
        }
    }
}