using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Clio.Utilities;
using ff14bot.Helpers;
using Newtonsoft.Json;

namespace Deep2.Helpers
{
    public class TrapList
    {
        public static Dictionary<int, TrapList> TrapLocations;
        
        public TrapList(string trapFile)
        {
            MapId = GetMapIdFromFile(trapFile);

            if (!File.Exists(trapFile)) return;
            
            using (var trapRead = new StreamReader(trapFile))
            {
                var trapJson = trapRead.ReadToEnd();
                trapList = JsonConvert.DeserializeObject<HashSet<Vector3>>(trapJson);
            }

        }

        public TrapList(int mapId, HashSet<Vector3> trapList)
        {
            MapId = mapId;
            this.trapList = trapList;
        }

        public TrapList(int mapId)
        {
            MapId = mapId;
            this.trapList = new HashSet<Vector3>();
        }

        public HashSet<Vector3> trapList { get; private set; }

        public int MapId { get; set; }

        public string TrapFile
        {
            get => Path.Combine(GetTrapFilePath(), $@"{MapId}.json");
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
            }
        }

        public bool AddTrap(Vector3 newTrap)
        {
            var tempTrap = newTrap.ToVector2().ToVector3();
            
            if (IsKnownTrapClose(tempTrap))
                return false;
            
            return trapList.Add(tempTrap);
        }
        
        public void WriteTrapFile()
        {
            using (StreamWriter outputFile = new StreamWriter(TrapFile, false))
            {
                outputFile.Write(JsonConvert.SerializeObject(trapList));
            }
        }

        public bool IsKnownTrapClose(Vector3 trap)
        {
            return trapList.Any(t => t.Distance2D(trap) < 3);
        }
        
        public static string GetTrapFilePath()
        {
            var directory = new DirectoryInfo(JsonSettings.AssemblyPath);

            var dirsInDir = directory.GetDirectories($@"BotBases\*{Constants.ProjectName}*");

            return dirsInDir.Any() ? Path.Combine(dirsInDir.First().FullName, @"Resources\Traps") : "";
        }

        public static int GetMapIdFromFile(FileInfo filename)
        {
            return GetMapIdFromFile(filename.Name);
        }

        public static int GetMapIdFromFile(string filename)
        {
            int mapid = 0;
            var split = filename.Split('.');
            if (split[0].Length == 3) int.TryParse(split[0], out mapid);

            return mapid;
        }

        public static Dictionary<int, TrapList> LoadTrapLists()
        {
            Dictionary<int, TrapList> list = new Dictionary<int, TrapList>();

            if (GetTrapFilePath() == "")
                return list;

            var hdDirectoryInWhichToSearch = new DirectoryInfo(GetTrapFilePath());

            var filesInDir = hdDirectoryInWhichToSearch.GetFiles("*.json");

            foreach (FileInfo foundFile in filesInDir)
            {
                string fullName = foundFile.FullName;
                int mapid = GetMapIdFromFile(foundFile.Name);
                if (mapid != 0)
                {
                    list.Add(mapid, new TrapList(fullName));
                }
            }

            return list;
        }

        public static void WriteTrapList()
        {
            foreach (var trapList in TrapLocations)
            {
                trapList.Value.WriteTrapFile();
            }
        }

        public static void Initialize()
        {
            TrapLocations = LoadTrapLists();
        }

        public static HashSet<Vector3> GetMapTraps(int map)
        {
            if (TrapLocations.ContainsKey(map))
                return TrapLocations[map].trapList;
            
            TrapLocations.Add(map, new TrapList(map));

            return TrapLocations[map].trapList;
        }

        public static bool AddTrapToMap(int map, Vector3 trap)
        {
            return TrapLocations[map].AddTrap(trap);
        }

        public static void CheckForTrapList(int map)
        {
            if (!TrapLocations.ContainsKey(map))
                TrapLocations.Add(map, new TrapList(map));
        }
    }
}