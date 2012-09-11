using System;
using System.Windows;
using System.Windows.Media;
using RepairControl;
using RepairControlPanel.Properties;
using System.Text.RegularExpressions;

namespace RepairControlPanel
{
    /// <summary>
    /// Interaction logic for UnitStatus.xaml
    /// </summary>
    public partial class UnitStatus
    {
        public UnitStatus()
        {
            InitializeComponent();
        }

        public Unit MyUnit;

        public void SetUnit(Unit unit)
        {
            if (MyUnit != null)
            {
                MyUnit.Dispose();
            }
            DataContext = MyUnit = unit;
        }

        private void RefreshNow_Click(object sender, RoutedEventArgs e)
        {
            MyUnit.RefreshNow();
        }

        private void BreakJumper_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var jumperValue = GetTargetJumperValue();
                MyUnit.BreakJumper(jumperValue);
                UpdateStatus();
            }
            catch (InvalidCastException)
            {
                MessageBox.Show("Неверный формат строки!");
            }
        }

        private byte? GetTargetJumperValue()
        {
            var targetJumperValue = JumperTargetValueTextBox.Text.Trim();

            Regex _reg = new Regex("^[0-1]{8}", RegexOptions.None);
            Match _mbin = _reg.Match(targetJumperValue);

            if (_mbin.Success)
                return targetJumperValue.Length == 0
                    ? (byte?)null
                    : Convert.ToByte(targetJumperValue, 2);
            else
                JumperTargetValueTextBox.Text = "ERROR";
                return (byte?)null;
        }

        private void BreakResistor_Click(object sender, RoutedEventArgs e)
        {
            MyUnit.BreakResistor();
            UpdateStatus();
        }


        public void UpdateStatus()
        {
            StatusLabel.Content = MyUnit.UpdatedString;
            Visibility = MyUnit.IsOnline? Visibility.Visible : Visibility.Collapsed;
            JumperValueLabel.Content = MyUnit.JumperValueString;
            ResistorValueLabel.Content = MyUnit.ResistorValueString;
            Background = new SolidColorBrush(MyUnit.IsWorking ? Color.FromRgb(0,255,0) : Color.FromRgb(255,0,0));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var showButtons = !Settings.Default.EnableAutoRepair ? Visibility.Visible : Visibility.Hidden;
            BreakJamper.Visibility = BreakResistor.Visibility = showButtons;
        }

        public double GetResistorVal()
        {
            return MyUnit.GetConsumeValue();
        }
    }
}
