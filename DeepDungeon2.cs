/*
DeepDungeon2 is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.

Orginal work done by zzi, contibutions by Omninewb, Freiheit, and mastahg
                                                                                 */

using Buddy.Coroutines;
using Deep2.Forms;
using Deep2.Helpers;
using Deep2.Helpers.Logging;
using Deep2.Providers;
using Deep2.TaskManager;
using Deep2.TaskManager.Actions;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.NeoProfiles;
using ff14bot.Overlay3D;
using ff14bot.Pathing.Service_Navigation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TreeSharp;

namespace Deep2
{
    public class DeepDungeon2 : BotBase
    {
        public override string EnglishName => "Deep Dungeon Quick";
#if RB_CN
        public override string Name => "深层迷宫宫";
#else
        public override string Name => "Deep Dungeon Quick";
#endif
        public override PulseFlags PulseFlags => PulseFlags.All;
        public override bool IsAutonomous => true;
        public override bool RequiresProfile => false;
        public override bool WantButton => true;
        public static bool ShowDebug = false;

        public DeepDungeon2()
        {
            //Captain = new GetToCaptain();

            if (Settings.Instance.FloorSettings == null || !Settings.Instance.FloorSettings.Any())
                Logger.Warn("Settings are empty?");

            Task.Factory.StartNew(() =>
            {
                Constants.INIT();
                Overlay3D.Drawing += DDNavigationProvider.Render;

                _init = true;
                Logger.Info("INIT DONE");
            });
        }

        private volatile bool _init;

        private TaskManagerProvider _tasks;

        #region BotBase Stuff

        private SettingsForm _settings;
        public Form1 _debug;
        private static readonly Version v = new Version(2, 3, 3);

        public override void OnButtonPress()
        {
            if (_settings == null)
            {
#if RB_CN
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
                Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("zh-CN");
                CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("zh-CN");
#endif

                _settings = new SettingsForm
                {
                    Text = "DeepDive2 v" + v //title
                };

                _settings.Closed += (o, e) => { _settings = null; };
            }

            try
            {
                _settings.Show();
            }
            catch (Exception)
            {
            }
        }

        #endregion BotBase Stuff

        /// <summary>
        ///     = true when we stop gets pushed
        /// </summary>
        internal static bool StopPlz;

        public override void Stop()
        {
            _root = null;
            StopPlz = true;

            Navigator.NavigationProvider = new NullProvider();
            DDTargetingProvider.Instance.Reset();
            Navigator.Clear();
            Poi.Current = null;
            _debug?.Close();
            _debug = null;
        }

        public override Composite Root => _root;

        private Composite _root;

        //private DDServiceNavigationProvider serviceProvider = new DDServiceNavigationProvider();
        public override void Pulse()
        {
            if (Constants.InDeepDungeon)
            {
                //force a pulse on the director if we are hitting "start" inside of the dungeon
                if (DirectorManager.ActiveDirector == null)
                    DirectorManager.Update();
                DDTargetingProvider.Instance.Pulse();
            }

            if (_tasks != null)
                _tasks.Tick();
        }

        public override void Start()
        {
            Poi.Current = null;
            //setup navigation manager
            Navigator.NavigationProvider = new DDNavigationProvider(new ServiceNavigationProvider());
            Navigator.PlayerMover = new SlideMover();

            TreeHooks.Instance.ClearAll();

            _tasks = new TaskManagerProvider();

            DeepTracker.InitializeTracker(Core.Me.ClassLevel);

            _tasks.Add(new LoadingHandler());
            _tasks.Add(new DeathWindowHandler());
            _tasks.Add(new SideStepTask());
            //not sure if i want the trap handler to be above combat or not
            _tasks.Add(new TrapHandler());

            //pomanders for sure need to happen before combat so that we can correctly apply Lust for bosses
            _tasks.Add(new Pomanders());

            _tasks.Add(new CombatHandler());
            _tasks.Add(new LobbyHandler());
            
            _tasks.Add(new GetToCaptiain());
            _tasks.Add(new POTDEntrance());

            _tasks.Add(new CarnOfReturn());
            _tasks.Add(new FloorExit());
            _tasks.Add(new Loot());

            _tasks.Add(new StuckDetection());
            _tasks.Add(new POTDNavigation());

            _tasks.Add(new BaseLogicHandler());

            Settings.Instance.Stop = false;
            if (!Core.Me.IsDow())
            {
                Logger.Error("Please change to a DOW class");
                _root = new ActionAlwaysFail();
                return;
            }

            //setup combat manager
            CombatTargeting.Instance.Provider = new DDCombatTargetingProvider();

            GameSettingsManager.FaceTargetOnAction = true;

            if (Constants.Lang == Language.Chn)
                //回避 - sidestep
                //Zekken
                if (PluginManager.Plugins.Any(i =>
                    (i.Plugin.Name.Contains("Zekken") || i.Plugin.Name.Contains("技能躲避")) && i.Enabled))
                {
                    Logger.Error("禁用 AOE技能躲避插件 - Zekken");
                    _root = new ActionAlwaysFail();
                    return;
                }

            if (PluginManager.Plugins.Any(i => i.Plugin.Name == "Zekken" && i.Enabled))
            {
                Logger.Error(
                    "Zekken is currently turned on, It will interfere with DeepDive & SideStep. Please Turn it off and restart the bot.");
                _root = new ActionAlwaysFail();
                return;
            }

            if (!ConditionParser.IsQuestCompleted(67092))
            {
                Logger.Error("You must complete \"The House That Death Built\" to run this base.");
                Logger.Error(
                    "Please switch to \"Order Bot\" and run the profile: \\BotBases\\DeepDive\\Profiles\\PotD_Unlock.xml");
                _root = new ActionAlwaysFail();
                return;
            }

            StopPlz = false;

            SetupSettings();

            if (ShowDebug)
            {
                if (_debug == null)
                    // _debug = new Debug
                    // {
                    //Text = "DeepDive2 v" + v //title
                    // };
                    try
                    {
                        //_debug = new Form1();

                        Thread Messagethread = new Thread(new ThreadStart(delegate()
                        {
                            _debug = new Form1();
                            /* DispatcherOperation DispacherOP =
                             _debug.Dispatcher.BeginInvoke(
                                 DispatcherPriority.Normal,
                                 new System.Action(delegate ()
                                 {
                                     _debug.Show();
                                     _debug.Closed += (o, e) => { _debug = null; };
                                 }));

                        _debug.BeginInvoke(
                        new System.Action(delegate ()
                        {
                            // _debug.Show();
                            _debug.ShowDialog();
                            _debug.Closed += (o, e) => { _debug = null; };
                        }));
                        // _debug.Show();
                        */
                            _debug.ShowDialog();

                            //_debug.listBox1.DataSource = DDTargetingProvider.Instance.LastEntities;
                        }));
                        Messagethread.SetApartmentState(ApartmentState.STA);
                        Messagethread.Start();

                        // _debug.Show(); _debug.listBox1.DataSource = DDTargetingProvider.Instance.LastEntities;
                        //_debug.ShowDialog();
                        DeepTracker._debug = _debug;
                    }
                    catch (Exception)
                    { }
            }

            _root =
                new ActionRunCoroutine(async x =>
                {
                    if (StopPlz)
                        return false;
                    if (!_init)
                    {
                        Logging.Write("DeepDive is waiting on Initialization to finish");
                        return true;
                    }

                    if (await _tasks.Run())
                    {
                        await Coroutine.Yield();
                    }
                    else
                    {
                        Logger.Warn("No tasks ran");
                        await Coroutine.Sleep(1000);
                    }

                    return true;
                });
        }

        private static void SetupSettings()
        {
            Logger.Info("UpdateTrapSettings");
            //mimic stuff
            if (Settings.Instance.OpenMimics)
            {
                //if we have mimics remove them from our ignore list
                if (Constants.IgnoreEntity.Contains(EntityNames.MimicCoffer[0]))
                    Constants.IgnoreEntity = Constants.IgnoreEntity.Except(EntityNames.MimicCoffer).ToArray();
            }
            else
            {
                //if we don't have mimics add them to our ignore list
                if (!Constants.IgnoreEntity.Contains(EntityNames.MimicCoffer[0]))
                    Constants.IgnoreEntity = Constants.IgnoreEntity.Concat(EntityNames.MimicCoffer).ToArray();
            }

            //Exploding Coffers
            if (Settings.Instance.OpenTraps)
            {
                //if we have traps remove them
                if (Constants.IgnoreEntity.Contains(EntityNames.TrapCoffer))
                    Constants.IgnoreEntity = Constants.IgnoreEntity.Except(new[] {EntityNames.TrapCoffer}).ToArray();
            }
            else
            {
                if (!Constants.IgnoreEntity.Contains(EntityNames.TrapCoffer))
                    Constants.IgnoreEntity = Constants.IgnoreEntity.Concat(new[] {EntityNames.TrapCoffer}).ToArray();
            }

            if (Settings.Instance.OpenSilver)
            {
                //if we have traps remove them
                if (Constants.IgnoreEntity.Contains(EntityNames.SilverCoffer))
                    Constants.IgnoreEntity = Constants.IgnoreEntity.Except(new[] {EntityNames.SilverCoffer}).ToArray();
            }
            else
            {
                if (!Constants.IgnoreEntity.Contains(EntityNames.SilverCoffer))
                    Constants.IgnoreEntity = Constants.IgnoreEntity.Concat(new[] {EntityNames.SilverCoffer}).ToArray();
                /* 				if (!Constants.IgnoreEntity.Contains(EntityNames.GoldCoffer))
                                    Constants.IgnoreEntity = Constants.IgnoreEntity.Concat(new[] { EntityNames.GoldCoffer }).ToArray();	 */
                //if (!Constants.IgnoreEntity.Contains(EntityNames.GoldChest))
                //     Constants.IgnoreEntity = Constants.IgnoreEntity.Concat(new[] { EntityNames.GoldChest }).ToArray();
            }

            Settings.Instance.Dump();
        }
    }
}