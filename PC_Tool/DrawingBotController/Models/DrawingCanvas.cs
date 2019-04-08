// ************************************************************************************************
// DRAWING BOT
//
// Repository:
//  Github: https://github.com/rahmant3/drawing-bot
//
// Description:
//  Class used for drawing on a Canvas control.
//
// History:
//  2019-04-04 by Tamkin Rahman
//  - Created.
// ************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace DrawingBotController.Models
{
    public class DrawingCanvas
    {
        public const string ACK_MESSAGE = "+";

        private const int POINTS_TO_DRAW_CAPACITY = 1024;
        private const int SAMPLE_DELAY_ms = 10;
        private const int COMMAND_DELAY_ms = 100;
        private const int COMMAND_WAIT_DELAY_ms = 1000;

        private bool waitingForAck = false;
        private SerialMonitor monitor = null;
        private System.Windows.Controls.Canvas canvas = null;

        private List<Point> pointsToDraw = new List<Point>(POINTS_TO_DRAW_CAPACITY);
        private Queue<string> commands = new Queue<string>();

        public DrawingCanvas(System.Windows.Controls.Canvas canvas, SerialMonitor monitor)
        {
            this.canvas = canvas;
            this.monitor = monitor;
            this.DrawingWorker_init();
            this.CommandWorker_init();
        }

        public void ReceivedAck()
        {
            this.waitingForAck = false;
        }
        public void StartRecording()
        {
            if (!this.drawingWorker.IsBusy)
            {
                this.drawingWorker.RunWorkerAsync();
            }
        }

        public void StopRecording()
        {
            if (this.drawingWorker.IsBusy)
            {
                this.drawingWorker.CancelAsync();
            }
        }

        public void Clear()
        {
            this.canvas.Children.Clear();
        }

        private void DrawLine(Point start, Point end)
        {
            Line newLine = new Line()
            {
                Stroke = System.Windows.Media.Brushes.Black,
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y
            };
            this.canvas.Children.Add(newLine);
        }

        #region DrawingBackgroundWorker
        private BackgroundWorker drawingWorker;

        private void DrawingWorker_init()
        {
            this.drawingWorker = new BackgroundWorker();
            this.drawingWorker.WorkerSupportsCancellation = true;
            this.drawingWorker.DoWork += this.DrawingWorker_DoWork;
            this.drawingWorker.WorkerReportsProgress = true;
            this.drawingWorker.ProgressChanged += this.DrawingWorker_progressChanged;
            this.drawingWorker.RunWorkerCompleted += this.DrawingWorker_RunWorkerCompleted;
        }

        private void DrawingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            this.pointsToDraw.Clear();
            while (!worker.CancellationPending)
            {
                worker.ReportProgress(0); // UI elements can only be accessed on the main thread, so use this as a way to notify the main thread.
                System.Threading.Thread.Sleep(SAMPLE_DELAY_ms);
            }

            e.Cancel = true;
        }

        private void DrawingWorker_progressChanged(object sender, ProgressChangedEventArgs e)
        {
            pointsToDraw.Add(System.Windows.Input.Mouse.GetPosition(this.canvas));
        }

        private void DrawingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int len = pointsToDraw.Count;
            int ix = 1;
            Point start = new Point();
            Point end;

            if (len > 0)
            {
                start = pointsToDraw[0];
                while (ix < len)
                {
                    if ((pointsToDraw[ix].X == start.X) && (pointsToDraw[ix].Y == start.Y))
                    {
                        ix++;
                    }
                    else
                    {
                        end = pointsToDraw[ix];
                        this.DrawLine(start, end);
                        start = end;
                        ix++;
                    }
                }

                start = pointsToDraw[0];
                if (this.monitor.Connected)
                {
                    this.commands.Enqueue("PU;");
                    this.commands.Enqueue(string.Format("PA,{0},{1};", (int)start.X, (int)start.Y));
                    this.commands.Enqueue("PD;");
                    Point prev = new Point(-1, -1);
                    foreach (Point p in this.pointsToDraw)
                    {
                        if (((int)prev.X != (int)p.X) || ((int)prev.Y != (int)p.Y))
                        {
                            this.commands.Enqueue(string.Format("PA,{0},{1};", (int)p.X, (int)p.Y));
                        }
                        prev = p;
                    }
                    this.commands.Enqueue("PU;");
                }
            }

            pointsToDraw.Clear();
        }
        #endregion

        #region CommandWriterBackgroundWorker
        private BackgroundWorker commandWorker;

        private void CommandWorker_init()
        {
            this.commandWorker = new BackgroundWorker();
            this.commandWorker.DoWork += this.CommandWorker_DoWork;

            this.commandWorker.RunWorkerAsync();
        }

        private void CommandWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (true)
            {
                if (this.monitor.Connected && (this.commands.Count() > 0) && (!this.waitingForAck))
                {
                    this.monitor.WriteLine(this.commands.Dequeue());
                    this.waitingForAck = true;
                    System.Threading.Thread.Sleep(COMMAND_DELAY_ms);
                }
                else
                {
                    System.Threading.Thread.Sleep(COMMAND_WAIT_DELAY_ms);
                }

            }
        }

        #endregion

    }
}
