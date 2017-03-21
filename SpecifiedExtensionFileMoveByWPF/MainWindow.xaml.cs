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
//using System.Windows.Shapes;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace SpecifiedExtensionFileMoveByWPF
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 設定値の反映
            SetSettings();
        }

        /// <summary>
        /// 選択ボタンによるフォルダ選択および画面表示
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
            dlg.Title = "作業フォルダの選択";
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = this.SavedFolderPathLabel.Text;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = this.SavedFolderPathLabel.Text;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                this.SavedFolderPathLabel.Text = dlg.FileName;
            }
        }

        /// <summary>
        /// ドラッグされた際の事前処理
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private void FoldersListView_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// フォルダドロップ時のファイルパス取得と表示
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private void FoldersListView_Drop(object sender, DragEventArgs e)
        {
            var folderPaths = new List<string>();
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                foreach (var s in files)
                    if (Directory.Exists(s))
                    {
                        folderPaths.Add(s);
                    }
            }
            FoldersListView.ItemsSource = folderPaths;
            SetPickupListView(folderPaths);
        }

        /// <summary>
        /// PickupListViewにファイルパスをセットする
        /// </summary>
        /// <param name="folderPaths">フォルダパスリスト</param>
        private void SetPickupListView(List<string> folderPaths)
        {
            // フォルダパスリストが存在するかチェック
            if (folderPaths == null) { return; }

            var fileList = new List<string>();
            List<string> patterns = GetPatternFromExtensions();

            // searchOption オプション [ AllDirectories…サブフォルダーも検索する / TopDirectoryOnly…直下のフォルダのみ検索する ]
            var searchOption = (bool)SubFolderCheckBox.IsChecked ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly;

            // フォルダを展開する
            foreach (string path in folderPaths)
            {
                try
                {
                    var files = Directory.EnumerateFiles(path, "*.*", searchOption);
                    var filteringFile = files.Where(file => patterns.Any(pattern => file.ToLower().EndsWith(pattern))).ToArray();

                    foreach (string filePath in filteringFile)
                    {
                        fileList.Add(filePath);
                    }
                }
                catch
                {
                    MessageBox.Show("フォルダ一覧に指定されている一部のフォルダが見つかりません。\nフォルダ一覧をクリアします。", "保存先フォルダ存在確認", MessageBoxButton.OK, MessageBoxImage.Error);
                    FoldersListView.ItemsSource = null;
                    return;
                }
            }
            // ファイル一覧を格納
            PickupListView.ItemsSource = fileList;
        }

        /// <summary>
        /// 拡張子パターンの取得
        /// </summary>
        /// <returns>パターンリスト</returns>
        private List<string> GetPatternFromExtensions()
        {
            // チェックする拡張子の格納
            List<string> patterns = new List<string>();

            // 拡張子チェックボックスの格納
            CheckBox[] extensionCheckBoxes = { AviCheckBox, MkvCheckBox, Mp4CheckBox, WmvCheckBox, JpgCheckBox, PngCheckBox, ZipCheckBox };

            var pattern = string.Empty;
            foreach (CheckBox checkBox in extensionCheckBoxes)
            {
                pattern = OnOffCheckExtensions(checkBox);
                if (pattern != string.Empty) { patterns.Add(pattern); }
            }

            // 拡張子テキストボックスに設定されている拡張子の格納
            if (SpecifiedTextBox.Text != string.Empty)
            {
                string[] lines = SpecifiedTextBox.Text.Split(',');
                foreach (string data in lines)
                {
                    patterns.Add("." + data.Trim());
                }
            }
            return patterns;
        }

        /// <summary>
        /// チェックされた拡張子の格納
        /// </summary>
        /// <param name="checkBox">チェックボックスオブジェクト</param>
        /// <returns>.拡張子</returns>
        private string OnOffCheckExtensions(CheckBox checkBox)
        {
            var extensionString = string.Empty;
            if ((bool)checkBox.IsChecked) { extensionString = "." + checkBox.Content.ToString(); }
            return extensionString;
        }

        /// <summary>
        /// 拡張子テキストボックスの変更イベントによる拡張子再取得
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private void ExtensionCheckBoxes_Click(object sender, RoutedEventArgs e)
        {
            SetPickupListView((List<string>)FoldersListView.ItemsSource);
        }

        /// <summary>
        /// 拡張子テキストボックスからカーソルが離脱した際の拡張子取得
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private void SpecifiedTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // HACK: 変更の有無をチェックして処理させることが望ましい
            SetPickupListView((List<string>)FoldersListView.ItemsSource);
        }

        /// <summary>
        /// 実行ボタンクリックイベントによる、ピックアップファイルの指定フォルダへの移動もしくはコピー
        /// </summary>
        /// <param name="sender">イベント発生元オブジェクト</param>
        /// <param name="e">イベントルーティング情報</param>
        private void ExecButton_Click(object sender, RoutedEventArgs e)
        {
            // 保存先フォルダ指定確認
            if (SavedFolderPathLabel.Text == string.Empty)
            {
                MessageBox.Show("保存先フォルダが指定されていません。", "保存先フォルダ存在確認", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 保存先フォルダ存在確認
            if (!Directory.Exists(SavedFolderPathLabel.Text))
            {
                MessageBox.Show("指定された保存先フォルダが存在しません。", "保存先フォルダ存在確認", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ピックアップファイル指定確認
            if (PickupListView.Items.Count < 1)
            {
                MessageBox.Show("ピックアップファイルが指定されていません。", "ファイル指定確認", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ファイル存在確認
            foreach (object item in PickupListView.Items)
            {
                if (!File.Exists(item.ToString()))
                {
                    MessageBox.Show("存在しないファイルがあります。", "ファイル存在確認", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                // 確認ダイアログ
                if (MessageBox.Show("処理を継続します。", "継続確認", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                {
                    return;
                }
                else
                {
                    try
                    {
                        var copyMode = (bool)CopyCheckBox.IsChecked;
                        var overWriteMode = (bool)OverWriteCheckBox.IsChecked;
                        foreach (object item in PickupListView.Items)
                        {
                            var targetPath = SavedFolderPathLabel.Text + "\\" + Path.GetFileName(item.ToString());
                            if (copyMode)
                            {
                                if (overWriteMode)
                                {
                                    FileSystem.CopyFile(item.ToString(), targetPath, true);
                                }
                                else
                                {
                                    if (FileSystem.FileExists(item.ToString()))
                                    {
                                        if (MessageBox.Show(item.ToString() + "\nは保存先フォルダに同名のファイルが存在します。上書きしますか。", "上書き確認", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                                        {
                                            MessageBox.Show("処理を中断しました。", "実行結果", MessageBoxButton.OK, MessageBoxImage.Warning);
                                            return;
                                        }
                                        else
                                        {
                                            FileSystem.CopyFile(item.ToString(), targetPath, true);
                                        }
                                    }

                                }
                            }
                            else
                            {
                                if (overWriteMode)
                                {
                                    FileSystem.MoveFile(item.ToString(), targetPath, true);
                                }
                                else
                                {
                                    if (FileSystem.FileExists(item.ToString()))
                                    {
                                        if (MessageBox.Show(item.ToString() + "\nは保存先フォルダに同名のファイルが存在します。上書きしますか。", "上書き確認", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                                        {
                                            MessageBox.Show("処理を中断しました。", "実行結果", MessageBoxButton.OK, MessageBoxImage.Warning);
                                            return;
                                        }
                                        else
                                        {
                                            FileSystem.MoveFile(item.ToString(), targetPath, true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("ファイル移動に失敗しました。\n既にファイルが存在していないか、ファイルがロックされていないか確認してください。", "例外", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if ((bool)DeleteFolderCheckBox.IsChecked)
                    {
                        try
                        {
                            foreach (object item in FoldersListView.Items)
                            {
                                Directory.Delete(item.ToString(), true);
                            }
                        }
                        catch
                        {
                            MessageBox.Show("フォルダ削除に失敗しました。\nファイルがロックされていないか確認してください。", "例外", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    SaveSettings();
                    MessageBox.Show("完了しました。", "実行結果", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                MessageBox.Show("システムエラーが発生しました。", "例外", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        /// <summary>
        /// 設定値の反映
        /// </summary>
        private void SetSettings()
        {
            SavedFolderPathLabel.Text = Properties.Settings.Default.SavedFolderPath;
            SubFolderCheckBox.IsChecked = Properties.Settings.Default.SubFolderFlag;
            SubFolderCheckBox.IsChecked = Properties.Settings.Default.SubFolderFlag;
            AviCheckBox.IsChecked = Properties.Settings.Default.AviFlag;
            MkvCheckBox.IsChecked = Properties.Settings.Default.MkvFlag;
            Mp4CheckBox.IsChecked = Properties.Settings.Default.Mp4Flag;
            WmvCheckBox.IsChecked = Properties.Settings.Default.WmvFlag;
            JpgCheckBox.IsChecked = Properties.Settings.Default.JpgFlag;
            PngCheckBox.IsChecked = Properties.Settings.Default.PngFlag;
            ZipCheckBox.IsChecked = Properties.Settings.Default.ZipFlag;
            SpecifiedTextBox.Text = Properties.Settings.Default.SpecifiedList;
            DeleteFolderCheckBox.IsChecked = Properties.Settings.Default.DeleteFolderFlag;
            CopyCheckBox.IsChecked = Properties.Settings.Default.CopyFlag;
            OverWriteCheckBox.IsChecked = Properties.Settings.Default.OverWriteFlag;
        }

        /// <summary>
        /// 設定値の保存
        /// </summary>
        private void SaveSettings()
        {
            Properties.Settings.Default.SavedFolderPath = SavedFolderPathLabel.Text;
            Properties.Settings.Default.SubFolderFlag = (bool)SubFolderCheckBox.IsChecked;
            Properties.Settings.Default.SubFolderFlag = (bool)SubFolderCheckBox.IsChecked;
            Properties.Settings.Default.AviFlag = (bool)AviCheckBox.IsChecked;
            Properties.Settings.Default.MkvFlag = (bool)MkvCheckBox.IsChecked;
            Properties.Settings.Default.Mp4Flag = (bool)Mp4CheckBox.IsChecked;
            Properties.Settings.Default.WmvFlag = (bool)WmvCheckBox.IsChecked;
            Properties.Settings.Default.JpgFlag = (bool)JpgCheckBox.IsChecked;
            Properties.Settings.Default.PngFlag = (bool)PngCheckBox.IsChecked;
            Properties.Settings.Default.ZipFlag = (bool)ZipCheckBox.IsChecked;
            Properties.Settings.Default.SpecifiedList = SpecifiedTextBox.Text;
            Properties.Settings.Default.DeleteFolderFlag = (bool)DeleteFolderCheckBox.IsChecked;
            Properties.Settings.Default.CopyFlag = (bool)CopyCheckBox.IsChecked;
            Properties.Settings.Default.OverWriteFlag = (bool)OverWriteCheckBox.IsChecked;
            // 設定値の保存
            Properties.Settings.Default.Save();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }
    }
}
