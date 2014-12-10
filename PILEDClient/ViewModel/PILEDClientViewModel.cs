using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MvvmFoundation.Wpf;
using PILEDClient;
using Lumitech.Helpers;

namespace PILEDClient.ViewModel
{
    class PILEDClientViewModel : ObservableObject
    {
        private DaytimeCCT dtCircle;
        private int _CCT;
        public int CCT { 
            get { 
                _CCT = dtCircle.getCCT();
                RaisePropertyChanged("CCT");
                return _CCT; 
            } 
        } 

        private bool _DTCircleEnabled;
        public bool DTCircleEnabled { get { return _DTCircleEnabled; } set { _DTCircleEnabled = value; RaisePropertyChanged("DTCircleEnabled"); } } 
        public bool DTCircleNotEnabled { get { return !_DTCircleEnabled; } }

        private bool _ExpertModeEnabled;
        public bool ExpertModeEnabled { get { return _ExpertModeEnabled;  }    
                                        set {   _ExpertModeEnabled = value;  
                                                RaisePropertyChanged("ExpertModeEnabled");                                                     
                                            }
                                      }

        public PILEDClientViewModel()
        {
            _DTCircleEnabled = true;
            _ExpertModeEnabled = false;
            dtCircle = new DaytimeCCT();
        }

        /*public ICommand DaytimeCircleCommand
        {
            get
            {
                return new RelayCommand(() => { _bDTCircleEnabled = !_bDTCircleEnabled;});
            }
        }

        public ICommand ExpertModeCommand
        {
            get
            {
                return new RelayCommand(() => { _bExpertModeEnabled = !_bExpertModeEnabled; });
            }
        }*/

        private void ShowControlWindow()
        {
            if (Application.Current.MainWindow == null)
            {
                Application.Current.MainWindow = new MainWindow();
                Application.Current.MainWindow.DataContext = this;                
            }

            Application.Current.MainWindow.Left = 0;

            if (Application.Current.MainWindow.Visibility == Visibility.Visible)
                Application.Current.MainWindow.Hide();

            Application.Current.MainWindow.Show();

            //TEST
            //List<int> cct = new List<int>();
            //for (int i = 0; i < 24; i++)
           //     cct.Add(dtCircle.getCCT(DateTime.Parse("9.12.2014 " + i +":30")));
        }


        #region commands
        public ICommand ShowWindowCommand
        {
            get
            {
                return new RelayCommand(()=> this.ShowControlWindow(),
                                        () => Application.Current.MainWindow == null);
            }
        }

        public RelayCommand HideWindowCommand
        {
            get
            {
                return new RelayCommand(() => Application.Current.MainWindow.Close(), () => Application.Current.MainWindow != null);
            }
        }

        public ICommand ExitApplicationCommand
        {
            get
            {
                return new RelayCommand( () => Application.Current.Shutdown());
            }
        }

        #endregion
    }
}

