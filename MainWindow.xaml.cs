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

namespace GAlbumSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(){
            InitializeComponent();
            FileNameTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            
        }

        void connect_Click(object sender, RoutedEventArgs e){
            Console.WriteLine("Connecting to Google Photos...");

            Auth.auth();

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

        void sync_Click(object sender, RoutedEventArgs e){
            Console.WriteLine("Syncing albums...");

            Sync.sync();
        }

    }
}
