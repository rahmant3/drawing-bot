// ************************************************************************************************
// DRAWING BOT
//
// Repository:
//  None
//
// Description:
//  Code behind for the Main window.
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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using DrawingBotController.ViewModels;

namespace DrawingBotController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel model = new MainWindowViewModel();
        public MainWindow()
        {
            this.DataContext = this.model;
            InitializeComponent();

            this.model.Init(MyCanvas); // Make sure to call the constructor AFTER the Window is initialized.

        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            this.model.Dispose();
            base.OnClosing(e);
        }

        // Cannot bind data to methods. Wasn't successful in finding a way to bind a MouseDown/MouseUp command.

        private void MyCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.model.StartRecordingCommand();
        }

        private void MyCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.model.StopRecordingCommand();
        }
    }
}
