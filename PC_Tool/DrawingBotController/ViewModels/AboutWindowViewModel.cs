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
    public class AboutWindowViewModel : INotifyPropertyChanged
    {
        public string Description { get; set; } = "This application is to be used for testing and demonstrating the Drawing Bot Project by Tamkin Rahman and Patrick Sarmiento, for ECE 4180 (Winter 2019). Unauthorized commercial use is strictly prohibited.\n\nIcon is by artist Oxygen Team, under the license LGPL: http://www.iconarchive.com/show/oxygen-icons-by-oxygen-icons.org/Actions-document-edit-icon.html";
        public string Version
        {
            get
            {
                return "V0.01";
            }
        }
        public AboutWindowViewModel() { }

        public void CloseWindow(object obj)
        {
            System.Windows.Window window = obj as System.Windows.Window;
            window.Close();
        }

        private ICommand exitCommand;
        public ICommand ExitCommand
        {
            get
            {
                if (exitCommand == null)
                {
                    exitCommand = new RelayCommand(param => this.CloseWindow(param), null);
                }
                return exitCommand;
            }
        }

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
    }
}
