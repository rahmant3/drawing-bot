// ************************************************************************************************
// DRAWING BOT
//
// Repository:
//  Github: https://github.com/rahmant3/drawing-bot
//
// Description:
//  View model for the main window of the DrawingBot tool.
//
// History:
//  2019-04-04 by Tamkin Rahman
//  - Created.
// ************************************************************************************************


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DrawingBotController.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DrawingBotController.ViewModels
{
    public class MainWindowViewModel: INotifyPropertyChanged, IDisposable
    {
        private const int MONITOR_DELAY_ms = 10;
        
        private SerialMonitor monitor = new SerialMonitor();
        private DrawingCanvas canvas = null;
        private StringBuilder monitorText = new StringBuilder();

        #region Properties
        public String MonitorText
        {
            get
            {
                return this.monitorText.ToString();
            }
            set
            {
                this.monitorText.Append(value);
                NotifyPropertyChanged("MonitorText");
            }
        }

        public string[] SerialPorts
        {
            get
            {
                return this.monitor.SerialPorts;
            }
        }

        public int SelectedPortIndex { get; set; } = 0;

        public List<int> BaudRates
        {
            get
            {
                return this.monitor.BaudRates;
            }
        }

        public int SelectedBaudRate { get; set; } = 9600;

        public bool Connected { get; set; } = false;
        #endregion

        public MainWindowViewModel()
        {
            this.MonitorWorker_init();
        }

        public void Init(System.Windows.Controls.Canvas canvasObj)
        {
            this.canvas = new DrawingCanvas(canvasObj, this.monitor);
        }

        public void AboutCommand()
        {
            Windows.AboutWindow dash = new Windows.AboutWindow();
            dash.ShowInTaskbar = false;

            dash.ShowDialog();
        }

        public void StartRecordingCommand()
        {
            this.canvas.StartRecording();
        }

        public void StopRecordingCommand()
        {
            this.canvas.StopRecording();
        }

        public void ConnectCommand()
        {
            if (this.SerialPorts.Count() > 0)
            {
                this.monitor.InitPort(this.SerialPorts[this.SelectedPortIndex], this.SelectedBaudRate);
                this.Connected = this.monitor.Connect();
                if (this.Connected)
                {
                    this.monitorWorker.RunWorkerAsync();
                }
            }
        }

        public void ClearMonitorCommand()
        {
            this.monitorText.Clear();
            NotifyPropertyChanged("MonitorText");
        }

        #region SerialMonitorBackgroundWorker
        private BackgroundWorker monitorWorker;

        private void MonitorWorker_init()
        {
            this.monitorWorker = new BackgroundWorker();
            this.monitorWorker.DoWork += this.MonitorWorker_DoWork;
        }

        private void MonitorWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (true)
            {
                if (this.monitor.ReadAvailable() > 0)
                {
                    string existing = this.monitor.ReadExisting();
                    if (existing.Contains(DrawingCanvas.ACK_MESSAGE))
                    {
                        this.canvas.ReceivedAck();
                    }
                    this.MonitorText = existing;
                }
                
                System.Threading.Thread.Sleep(MONITOR_DELAY_ms);
            }
        }

        #endregion

        #region Commands
        private ICommand about;
        public ICommand About
        {
            get
            {
                if (this.about == null)
                {
                    this.about = new RelayCommand(param => this.AboutCommand(), null);
                }
                return this.about;
            }
        }

        private ICommand startRecording;
        public ICommand StartRecording
        {
            get
            {
                if (this.startRecording == null)
                {
                    this.startRecording = new RelayCommand(param => this.StartRecordingCommand(), null);
                }
                return this.startRecording;
            }
        }

        private ICommand stopRecording;
        public ICommand StopRecording
        {
            get
            {
                if (this.stopRecording == null)
                {
                    this.stopRecording = new RelayCommand(param => this.StopRecordingCommand(), null);
                }
                return this.stopRecording;
            }
        }

        private ICommand clearCanvas;
        public ICommand ClearCanvas
        {
            get
            {
                if (this.clearCanvas == null)
                {
                    this.clearCanvas = new RelayCommand(param => this.canvas.Clear(), null);
                }
                return this.clearCanvas;
            }
        }

        private ICommand clearMonitor;
        public ICommand ClearMonitor
        {
            get
            {
                if (this.clearMonitor == null)
                {
                    this.clearMonitor = new RelayCommand(param => this.ClearMonitorCommand(), null);
                }
                return this.clearMonitor;
            }
        }

        private ICommand connect;
        public ICommand Connect
        {
            get
            {
                if (this.connect == null)
                {
                    this.connect = new RelayCommand(param => this.ConnectCommand(), null);
                }
                return this.connect;
            }
        }

        #endregion

        #region INotifyPropertyChanged
        // See: https://docs.microsoft.com/en-us/dotnet/framework/winforms/how-to-implement-the-inotifypropertychanged-interface
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            this.monitor.Disconnect();
        }
        #endregion
    }
}
