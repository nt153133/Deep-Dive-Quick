using Deep2.Helpers;
using Deep2.Providers;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Deep2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            Timer timer1 = new Timer();
            timer1.Interval = 3000; //5 seconds
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            listBox4.Items.Clear();
            var ProjectName = "Deep-Dive-Quick";
            string TrapsPath = "crap";
            
            DirectoryInfo hdDirectoryInWhichToSearch = new DirectoryInfo(Environment.CurrentDirectory);
            
            DirectoryInfo[] dirsInDir = hdDirectoryInWhichToSearch.GetDirectories($@"BotBases\*{ProjectName}*");

            if (dirsInDir.Any())
            {
                TrapsPath = Path.Combine(dirsInDir.FirstOrDefault().FullName, @"Resources\Traps");
            }

            listBox4.Items.Add(TrapsPath);

            hdDirectoryInWhichToSearch = new DirectoryInfo(TrapsPath);
            FileInfo[] filesInDir = hdDirectoryInWhichToSearch.GetFiles("*.json");

            foreach (FileInfo foundFile in filesInDir)
            {
                string fullName = foundFile.FullName;
                int mapid;
                if (foundFile.Name.Split('.')[0].Length == 3)
                {
                    mapid = int.Parse(foundFile.Name.Split('.')[0]);
                }
                listBox4.Items.Add(fullName );
                // Console.WriteLine(fullName);
            }


            //listBox4.Items.Add((TrapsPath));
            Update();
            Refresh();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //do whatever you want
            RefreshControls();
        }

        private void RefreshControls()
        {
            if (!Constants.InDeepDungeon)
                return;

            try
            {
                if (DDTargetingProvider.Instance.LastEntities != null)
                {
                    listBox1.Items.Clear();

                    foreach (var i in DDTargetingProvider.Instance.LastEntities)
                        listBox1.Items.Add(
                            $"{i.EnglishName} - Weight: {DDTargetingProvider.Sort(i)}({DDTargetingProvider.SortComplete(i)}) - Distance: {i.Location.Distance2D(Core.Me.Location)}");

                    TargetLbl.Text = DDTargetingProvider.Instance.FirstEntity.EnglishName;
                }
            }
            catch (Exception e)
            {
            }

            try
            {
                listBox2.Items.Clear();

                if (CombatTargeting.Instance.LastEntities != null)
                {
                    foreach (var i in CombatTargeting.Instance.LastEntities) listBox2.Items.Add(i);

                    CombatLbl.Text = CombatTargeting.Instance.FirstEntity.EnglishName;
                }
            }
            catch (Exception e)
            {
            }

            try
            {
                progressBar1.Value = DeepDungeonManager.PortalStatus;

                listBox3.Items.Clear();

                listBox3.Items.Add($"Progress: {Constants.Percent[DeepDungeonManager.PortalStatus]}");
                listBox3.Items.Add($"Portal Active: {DeepDungeonManager.PortalActive}");
                listBox3.Items.Add($"Portal Status: {DeepDungeonManager.PortalStatus}");
/*
                //var units = GameObjectManager.GameObjects;
                listBox4.Items.Clear();
                GameObjectManager.Update();
                //var units = GameObjectManager.GameObjects;
                //label6.Text = $"Count: {units.Count()}";
                foreach (var unit in GameObjectManager.GameObjects.OrderBy(r => r.Distance()))
                    listBox4.Items.Add($"Name:{unit} - Dist:{unit.Location.Distance2D(Core.Me.Location)}");
*/
                Update();
                Refresh();
            }
            catch (Exception e)
            {
            }

            Update();
            Refresh();
        }

        private void Label1_Click(object sender, EventArgs e)
        {
        }

        private float Sort(GameObject obj)
        {
            var weight = 100f;

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
                case GameObjectType.BattleNpc when !DeepDungeonManager.PortalActive && !PartyManager.IsInParty:
                    return weight / 2;

                case GameObjectType.BattleNpc:
                    weight /= 2;
                    break;
            }

            if (obj.NpcId == EntityNames.BandedCoffer)
                weight += 500;

            if (DeepDungeonManager.PortalActive && Settings.Instance.GoForTheHoard && obj.NpcId == EntityNames.Hidden)
                weight += 5;
            //else if (DeepDungeonManager.PortalActive && Settings.Instance.GoExit &&
            //         obj.NpcId != EntityNames.FloorExit && PartyManager.IsInParty)
            //    weight -= 10;

            if (obj.NpcId == EntityNames.BandedCoffer) weight += 200;

            //if (DeepDungeonManager.PortalActive && obj.NpcId == EntityNames.FloorExit &&
            // (Core.Me.HasAura(Auras.NoAutoHeal) || Core.Me.HasAura(Auras.Amnesia))) weight += 500;

            if (DeepDungeonManager.PortalActive && Settings.Instance.GoExit) weight -= 10;

            if (DeepDungeonManager.PortalActive && Settings.Instance.GoForTheHoard && obj.NpcId == EntityNames.Hidden)
                weight += 5;
            else if (DeepDungeonManager.PortalActive && Settings.Instance.GoExit &&
                     obj.NpcId != EntityNames.FloorExit && PartyManager.IsInParty)
                weight -= 10;

            return weight;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            var units = GameObjectManager.GameObjects;
            

            //Log("Name:{0}, Type:{3}, ID:{1}, Obj:{2}", unit, unit.NpcId, unit.ObjectId, unit.GetType());

            Update();
            Refresh();
            //OrderByDescending(Sort).ToList();
        }
    }
}