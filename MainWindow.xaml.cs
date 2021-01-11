using System;
using System.Collections.Generic;
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
using System.Threading;

namespace GAlbumSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        private Auth auth = new Auth();

        public MainWindow(){
            InitializeComponent();
            FileNameTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            updateConnectedStatus();
        }

        public void updateConnectedStatus()
        {
            Dispatcher.Invoke(new Action(() => {
                if(isConnected()){
                    ConnexionStatus.Text = "You are connected to Google";
                    ConnexionStatus.Foreground = Brushes.Green;
                }else{
                    ConnexionStatus.Text = "You are not connected to Google";
                    ConnexionStatus.Foreground = Brushes.Red;
                }
            }));
            
        }
        public bool isConnected(){
            return auth.credential != null;
        }

        async void connect_Click(object sender, RoutedEventArgs e){
            Console.WriteLine("Connecting to Google Photos...");

            await auth.auth();

            updateConnectedStatus();

        }

        void resetConnexion_Click(object sender, RoutedEventArgs e){
            auth.resetConexion();
            auth.credential = null;
            updateConnectedStatus();
        }
        void browseSource_Click(object sender, RoutedEventArgs e){
            Console.WriteLine("Browsing Source...");

            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog dlg = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            dlg.RootFolder = Environment.SpecialFolder.MyPictures;

            Nullable<bool> result = dlg.ShowDialog();
            if(result == true){
                FileNameTextBox.Text = dlg.SelectedPath;
            }
        }

        async void sync_Click(object sender, RoutedEventArgs e){
            if(isConnected()){
                Console.WriteLine("Syncing albums...");
                await new Sync().sync(FileNameTextBox.Text, auth);

            }else{
                connect_Click(this, new RoutedEventArgs());
            }
        }

    }
}
