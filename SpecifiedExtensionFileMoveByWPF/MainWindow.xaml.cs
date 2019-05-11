using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;

//using System.Windows.Shapes;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            LoadWindowPlacement();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!e.Cancel)
            {
                SaveWindowPlacement();
            }
        }

        void LoadWindowPlacement()
        {
            Properties.Settings.Default.Reload();

            var bounds = Properties.Settings.Default.Bounds;
            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;

            WindowState = Properties.Settings.Default.WindowState;
        }

        void SaveWindowPlacement()
        {
            Properties.Settings.Default.WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState; // 最小化は保存しない
            Properties.Settings.Default.Bounds = RestoreBounds;

            Properties.Settings.Default.Save();
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
            var folderPaths = (List<string>)FoldersListView.ItemsSource ?? new List<string>();
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                foreach (var s in files)
                    if (Directory.Exists(s) && !folderPaths.Contains(s))
                    {
                        folderPaths.Add(s);
                    }
            }
            FoldersListView.ItemsSource = folderPaths;
            FoldersListView.Items.Refresh();
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

            // 除外リストの取得
            var excludingFileList = new List<string>();
            var tempArray = Properties.Settings.Default.ExcludingFilesList.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            excludingFileList = (new List<string>(tempArray)).ConvertAll(delegate (string s) { return s.Trim(); });
            //excludingFileList.AddRange(tempArray);

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
                        var targetFileName = Path.GetFileNameWithoutExtension(filePath);
                        var isContains = excludingFileList.Any(excludingFile => targetFileName.Contains(excludingFile));

                        if (!isContains)
                        {
                            fileList.Add(filePath);
                        }
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
            CheckBox[] extensionCheckBoxes = { AviCheckBox, MkvCheckBox, Mp4CheckBox, WmvCheckBox, IsoCheckBox, JpgCheckBox, PngCheckBox, ZipCheckBox };

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
                                FileSystem.CopyFile(item.ToString(), targetPath, UIOption.AllDialogs);
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
                                FileSystem.MoveFile(item.ToString(), targetPath, UIOption.AllDialogs);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("例外が発生しました。\n" + ex.Message, "例外", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if ((bool)DeleteFolderCheckBox.IsChecked)
                {
                    try
                    {
                        // 確認ダイアログ
                        if (MessageBox.Show("ファイル操作は完了しました。\n元フォルダを削除します。本当によろしいですか。", "継続確認", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                        {
                            return;
                        }
                        else
                        {
                            foreach (object item in FoldersListView.Items)
                            {
                                Directory.Delete(item.ToString(), true);
                            }
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
                FoldersListView.ItemsSource = null;
                PickupListView.ItemsSource = null;
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
            IsoCheckBox.IsChecked = Properties.Settings.Default.IsoFlag;
            JpgCheckBox.IsChecked = Properties.Settings.Default.JpgFlag;
            PngCheckBox.IsChecked = Properties.Settings.Default.PngFlag;
            ZipCheckBox.IsChecked = Properties.Settings.Default.ZipFlag;
            SpecifiedTextBox.Text = Properties.Settings.Default.SpecifiedList;
            DeleteFolderCheckBox.IsChecked = Properties.Settings.Default.DeleteFolderFlag;
            CopyCheckBox.IsChecked = Properties.Settings.Default.CopyFlag;
            OverWriteCheckBox.IsChecked = Properties.Settings.Default.OverWriteFlag;
            CheckCopyCheckBoxStatus();
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
            Properties.Settings.Default.IsoFlag = (bool)IsoCheckBox.IsChecked;
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

        private void ExcludingFileButton_Click(object sender, RoutedEventArgs e)
        {
            ExcludingFilesSettings efs = new ExcludingFilesSettings();
            efs.Owner = this;
            efs.ShowDialog();

            SetPickupListView((List<string>)FoldersListView.ItemsSource);
        }

        private void CopyCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckCopyCheckBoxStatus();
        }

        private void CheckCopyCheckBoxStatus()
        {
            var copyMode = (bool)CopyCheckBox.IsChecked;
            if (copyMode)
            {
                DeleteFolderCheckBox.IsChecked = false;
                DeleteFolderCheckBox.IsEnabled = false;
            }
            else
            {
                DeleteFolderCheckBox.IsEnabled = true;
            }
        }

        /// <summary>
        /// クリアボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            FoldersListView.ItemsSource = null;
            PickupListView.ItemsSource = null;
        }
    }
}