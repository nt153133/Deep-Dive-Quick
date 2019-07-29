﻿/*
DeepDungeon2 is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.

Orginal work done by zzi, contibutions by Omninewb, Freiheit, and mastahg
                                                                                 */
using Buddy.Coroutines;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deep2.Helpers.Logging;

namespace Deep2.TaskManager.Actions
{
    class GetToCaptiain : ITask
    {
        public string Name => "GetToCaptain";

        public async Task<bool> Run()
        {
            //we are inside POTD
            if (Constants.InDeepDungeon || Constants.InExitLevel) return false;

            if (WorldManager.ZoneId != Constants.SouthShroudZoneId ||
               GameObjectManager.GetObjectByNPCId(Constants.CaptainNpcId) == null ||
               GameObjectManager.GetObjectByNPCId(Constants.CaptainNpcId).Distance2D(Core.Me.Location) > 35)
            {
                if (Core.Me.IsCasting)
                {
                    await Coroutine.Sleep(500);
                    return true;
                }

                if (!WorldManager.TeleportById(5))
                {
                    Logger.Error("We can't get to Quarrymill. something is very wrong...");
                    TreeRoot.Stop();
                    return false;
                }
                await Coroutine.Sleep(1000);
                return true;

            }
            if (GameObjectManager.GetObjectByNPCId(Constants.CaptainNpcId) == null || GameObjectManager.GetObjectByNPCId(Constants.CaptainNpcId).Distance2D(Core.Me.Location) > 4f)
            {
                return await CommonTasks.MoveAndStop(new MoveToParameters(Constants.CaptainNpcPosition, "Moving toward NPC"), 4f, true);
            }
            return false;
        }

        public void Tick()
        {
            
        }
    }
}
