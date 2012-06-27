using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using RepairControlDb;
using RepairControl;
using RepairControlPanel.Properties;

namespace RepairControlPanel
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1
    {
        private Timer Timer;
      
        private readonly Random rnd = new Random();
        
        private int energyLevel;
        public Window1()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectToCom(Settings.Default.ComPortName);
            portLabel.Content = "Порт: " + Settings.Default.ComPortName;
            energyLevel = Settings.Default.EnergyLevel;
            UpdateEnergyLabel();

            Timer = new Timer(Settings.Default.PingDelayMs) { AutoReset = true };
            Timer.Elapsed += (sender1, e1) => Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<string>(UpdateStatus), null);
            Timer.Start();

            var log = new LogWindow();
           // log.Show();
        }

        private void MakeKill()
        {
            var diff = (int)Math.Floor(Math.Abs(energyLevel - _consume));

            double prob = 1.0 * diff * diff / Settings.Default.KillCoef;
            var nextDouble = rnd.NextDouble();

            Log.Write(string.Format("Diff: {0} Prob: {1} ND: {2}", diff, prob, nextDouble));
            if (nextDouble > prob) return;
            var units = StatusPanels.Select(sp => sp.MyUnit).Where(u => u.GetConsumeValue() > 0).ToArray();
            if (units.Length == 0)
            {
                return;
            }
            var unitNum = rnd.Next(0, units.Length - 1);
            units[unitNum].BreakJumper();
        }

        private void UpdateEnergyLabel()
        {
            energyLabel.Content = string.Format("Потребление: {0} Производство: {1}", _consume * 1000.0, energyLevel * 1000.0);
        }

        private int interval = 0;

        private void UpdateStatus(object ignore)
        {
            foreach (var cnt in (IEnumerable<UnitStatus>) StatusPanels)
            {
                cnt.UpdateStatus();
            }
            if (Connector != null)
            {
                Connector.SendPing();
            }
            _consume = ((IEnumerable<UnitStatus>) StatusPanels).Sum(cnt => cnt.GetResistorVal());
            UpdateEnergyLabel();
            interval++;
            if (interval > 2 * 60 * 5)
            {
                interval = 0;
                MakeKill();
            }
        }

        private ComPortConnector Connector;

        private void ConnectToCom(string comPortName)
        {
            Connector = new ComPortConnector(comPortName, Settings.Default.DefaultDifficulty, Settings.Default.EnableDataSave ? (IUnitSavedData)new UnitSavedData() : new NullUnitSavedData(), Settings.Default.EnableAutoRepair,  Settings.Default.AutoRepairTreshold);
            for (byte i = 1; i < Settings.Default.MaxAddress; i++)
            {
                AddUnit(i);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Timer.Stop();
        }

        public List<UnitStatus> StatusPanels = new List<UnitStatus>();
        private double _consume;

        private void AddUnit(byte address)
        {
            var unitStatus = new UnitStatus();
            unitStatus.SetUnit(new Unit(address, Connector));
            StatusPanels.Add(unitStatus);
            unitsStackPanel.Children.Add(unitStatus);
        }
    }
}
