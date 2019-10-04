using System;
using System.Collections.Generic;
using System.IO;
using Clio.Utilities;
using Deep2.Forms;
using Deep2.Helpers.Logging;
using ff14bot.RemoteWindows;
using Newtonsoft.Json;

namespace Deep2.Helpers
{
    internal static class DeepTracker
    {
        private static int _startingLevel;
        private static int _currentLevel;
        private static DateTime _currentRunStarTime;
        private static DateTime _starTime;
        private static TimeSpan _lastRunTime;
        private static bool _isMeasuring;
        private static int _deaths;
        private static int _successfulRuns;
        private static int _failedRuns;
        private static float _xpPerHour;
        private static float _deathsPerHour;
        private static uint _startingXP;
        private static uint _runEndXP;
        private static uint _toLevelXP;
        private static uint _totalXPGain;
        private static TimeSpan _elapsedSpan;
        private static DateTime _currentRunEndTime;
        private static uint _xpNeeded;

        public static HashSet<Vector3> Traps = new HashSet<Vector3>();
        public static Form1 _debug;

        //public static object ProjectName = "Deep-Dive-Quick";
        //public static readonly string TrapsPath = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}\Resources");
        //DeepDungeon2
        private static TimeSpan CurrentRunTime()
        {
            return DateTime.Now.Subtract(_currentRunStarTime);
        }

        public static void InitializeTracker(int currentLevel)
        {
            if (File.Exists(Constants.TrapsFile))
            {
                using (StreamReader trapRead = new StreamReader(Constants.TrapsFile))
                {
                    string trapJson = trapRead.ReadToEnd();
                    Traps = JsonConvert.DeserializeObject<HashSet<Vector3>>(trapJson);
                }

                Logger.Info("Read trap file:");
                foreach (var i in Traps) Logger.Verbose($"##TRAP_Loaded## {i.X} , {i.Y} , {i.Z}");
            }
            else
            {
                Logger.Info($"Couldn't read trap file {Constants.TrapsFile}");
            }

            _startingLevel = currentLevel;
            _starTime = DateTime.Now;
            _deaths = 0;
            _startingXP = Experience.CurrentExperience;
            _toLevelXP = Experience.ExperienceRequired;
        }

        public static void Reset()
        {
            _isMeasuring = false;
            // GameStatsManager.uint_2 = Experience.CurrentExperience; GameStatsManager.uint_1 = Experience.ExperienceRequired;

            _deaths = 0;
            _xpPerHour = 0.0f;
            _deathsPerHour = 0.0f;
        }

        private static void StartMeasuring()
        {
            _currentRunStarTime = DateTime.Now;
            _isMeasuring = true;
        }

        private static void StopMeasuring()
        {
            _currentRunEndTime = DateTime.Now;
            _isMeasuring = false;
        }

        public static void StartRun(int currentLevel)
        {
            if (_isMeasuring == true)
                EndRun(true);

            StartMeasuring();

            if (_startingLevel == 0)
                _startingLevel = currentLevel;

            _currentLevel = currentLevel;
        }

        public static void EndRun(bool failed)
        {
            if (_isMeasuring == false)
                return;

            StopMeasuring();

            if (failed)
                _failedRuns++;
            else
                _successfulRuns++;

            _lastRunTime = _currentRunEndTime.Subtract(_currentRunStarTime);
            _runEndXP = Experience.CurrentExperience;

            RunReport();
        }

        public static void Died()
        {
            _deaths++;
        }

        public static void AddTrap(Vector3 trap)
        {
            Traps.Add(trap.ToVector2().ToVector3());
        }

        private static void UpdateXP(int realLevel)
        {
            if (realLevel > _currentLevel) _totalXPGain += _xpNeeded;
        }

        public static void RunReport()
        {
            _elapsedSpan = DateTime.Now.Subtract(_starTime);
            Logger.Info(@"

================Status   Report==============
=======================================
Starting Level   : {0}
Current Level    : {1}
Deaths             : {2}
Failed Runs       : {3}
Successful Runs  : {4}
Pre-Run XP       : {9}
Post-Run XP       : {10}
Total XP Gain       : {11}
Last Run Time     : {5} Min  {6} Sec
Total Run Time     : {7} Hours  {8} Min
=======================================

", _startingLevel, _currentLevel, _deaths, _failedRuns, _successfulRuns, _lastRunTime.Minutes, _lastRunTime.Seconds,
                _elapsedSpan.Hours, _elapsedSpan.Minutes, 0, 0, 0);

            Logger.Info("================TRAPS=============");

            foreach (var i in Traps) Logger.Verbose($"##TRAP_RUN## {i.X} , {i.Y} , {i.Z}");
            //TrapsPath
            //Path.Combine(TrapsPath, "Traps1.json")

            using (StreamWriter outputFile = new StreamWriter(Constants.TrapsFile, false))
            {
                outputFile.Write(JsonConvert.SerializeObject(Traps));
            }

            //JsonConvert.SerializeObject(Traps);
        }
    }
}