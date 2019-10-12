using System;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Common;
using Clio.Utilities;
using Deep2.Helpers.Logging;
using Deep2.Providers;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.Pathing;

namespace Deep2.Helpers
{
    public static class ScriptHelpers
    {
        private static float Variance = GetRandomFloat(-0.0872665, 0.0872665);
        
        public static float GetRandomFloat(double minimum, double maximum)
        {
            Random random = new Random();
            return (float)(random.NextDouble() * (maximum - minimum) + minimum);
        }
        /// <summary>
        /// Object Interaction
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="interactRange"></param>
        /// <returns></returns>
        public static async Task<bool> ObjectInteraction(GameObject obj, float interactRange = 4.5f)
        {
            return await ObjectInteraction(obj, interactRange, () => true);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="interactRange"></param>
        /// <param name="canInteract">Return False if we should not interact with the object</param>
        /// <returns></returns>
        public static async Task<bool> ObjectInteraction(GameObject obj, float interactRange, Func<bool> canInteract)
        {
            if (!canInteract())
                return false;

            if (!obj.IsValid || !obj.IsVisible)
                return false;

            if (Core.Me.IsCasting)
                return true;

            if (obj.Distance2D() > interactRange)
            {
                var mr = await CommonTasks.MoveTo(new MoveToParameters(obj.Location));
                if (mr == MoveResult.PathGenerationFailed && obj.InLineOfSight())
                {
                    Navigator.PlayerMover.MoveTowards(obj.Location);
                    return true;
                }
                else if (mr == MoveResult.PathGenerationFailed)
                {
                    Logger.Debug($"Unable to move toward {obj.Name} [{obj.NpcId}] (It appears to be out of line of sight and off the mesh)");
                }
                return mr.IsSuccessful();
            }
            if (MovementManager.IsMoving)
            {
                await CommonTasks.StopMoving();
                return true;
            }
            if (Core.Target == null || Core.Target.ObjectId != obj.ObjectId)
            {
                obj.Target();
                return true;
            }
            obj.Interact();
            await Coroutine.Sleep(500);
            return true;
        }
        
        public static void LookAway(this GameObject OBJ)
        {
            MovementManager.SetFacing(MathHelper.CalculateHeading(Core.Player.Location, OBJ.Location));
        }

        /// <summary>
        /// detects if we (or our party members) are in combat
        /// </summary>
        /// <returns></returns>
        public static bool InCombat()
        {
            
            return Core.Me.InCombat || GameObjectManager.NumberOfAttackers > 0 ||
                   (!PartyManager.IsPartyLeader && (CombatTargeting.Instance.FirstEntity != null && CombatTargeting.Instance.FirstEntity.InCombat));
        // return true;
        }

        public static Vector3 CalculatePointInFront(this GameObject obj, float distanceToTarget)
        {
            var targetFacingRadians = obj.Heading; //heading is normalized Radians
            Vector3 left = new Vector3((float)Math.Cos((double)targetFacingRadians), (float)Math.Sin((double)targetFacingRadians), 0f);
            return obj.Location + left * (distanceToTarget + Variance);
        }

        public static Vector3 CalculatePointBehind(this GameObject obj, float distanceToTarget)
        {
            return CalculatePointBehind(obj.Location, obj.Heading, distanceToTarget);
        }

        private static Vector3 CalculatePointBehind(Vector3 target, float targetFacingRadians, float distanceToTarget)
        {
            targetFacingRadians = MathEx.NormalizeRadian(targetFacingRadians);
            Vector3 left = new Vector3((float)Math.Cos((double)targetFacingRadians), 0f, (float)Math.Sin((double)targetFacingRadians));
            return target - left * (distanceToTarget + Variance);
        }

        public static Vector3 CalculatePointAtSide(this GameObject obj, float distanceToTarget, bool rightSide)
        {
            var targetFacingRadians = obj.Heading;
            if (rightSide)
            {
                targetFacingRadians += 1.5708f + Variance;
            }
            else
            {
                targetFacingRadians += 4.71239f + Variance;
            }
            return CalculatePointBehind(obj.Location, targetFacingRadians, distanceToTarget);
        }

    }
}