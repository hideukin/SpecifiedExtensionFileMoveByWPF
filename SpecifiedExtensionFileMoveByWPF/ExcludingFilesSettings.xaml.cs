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
using System.Windows.Shapes;

namespace SpecifiedExtensionFileMoveByWPF
{
    /// <summary>
    /// ExcludingFilesSettings.xaml の相互作用ロジック
    /// </summary>
    public partial class ExcludingFilesSettings : Window
    {
        public ExcludingFilesSettings()
        {
            InitializeComponent();
            ExcludingFilesTextBox.Text = Properties.Settings.Default.ExcludingFilesList;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ExcludingFilesList = ExcludingFilesTextBox.Text;
            // 設定値の保存
            Properties.Settings.Default.Save();
            Close();
        }
    }
}
