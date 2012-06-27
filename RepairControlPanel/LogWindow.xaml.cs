
using System;
using System.Timers;
using System.Windows;
using RepairControl;

namespace RepairControlPanel
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        public LogWindow()
        {
            InitializeComponent();
        }

        private Timer UpdateLog;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLog = new Timer(1000) {AutoReset = true};
            UpdateLog.Elapsed += new ElapsedEventHandler(UpdateLog_Elapsed);
            UpdateLog.Start();
        }

        void UpdateLog_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new UpdateDelegate(Update));
        }

        public delegate void UpdateDelegate();

        private void Update()
        {
            lock (Log.Items)
            {
                textBox1.Text += "\n" + string.Join("\n", Log.Items.ToArray());
                Log.Items.Clear();
            }
        }
    }
}
