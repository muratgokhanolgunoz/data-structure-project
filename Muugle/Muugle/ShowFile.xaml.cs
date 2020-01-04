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

namespace Muugle
{
    /// <summary>
    /// Interaction logic for ShowFile.xaml
    /// </summary>
    public partial class ShowFile : UserControl
    {
        public static ShowFile usercontrol_show_file = new ShowFile();

        public ShowFile()
        {
            InitializeComponent();
            usercontrol_show_file = this;
        }
    }
}
