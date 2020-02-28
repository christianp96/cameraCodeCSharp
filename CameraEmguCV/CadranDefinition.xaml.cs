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
using System.Windows.Shapes;

namespace CameraEmguCV
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class CadranDefinition : Window
    {
        MainWindow parent;
        public CadranDefinition()
        {
            InitializeComponent();
            this.parent = (MainWindow)Application.Current.MainWindow;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;  // cancels the window close    
            this.Hide();      // Programmatically hides the window
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

   

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

        }
    }
}
