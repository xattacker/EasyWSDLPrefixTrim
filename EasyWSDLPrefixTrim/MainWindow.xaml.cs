using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace EasyWSDLPrefixTrim
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string GetSelectedLanguage()
        {
            return this.javaRadioButton.IsChecked.Value ? "java" : this.swiftRadioButton.IsChecked.Value ? "swift" : null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string lang = this.GetSelectedLanguage();
            if (string.IsNullOrEmpty(lang))
            {
                this.ShowMessage("請選擇程式語言!!");

                return;
            }


            // 按下 path
            this.OpenFolderDialog
            (
            (string path) =>
            {
                if (this.SetTrimPrefix(path, lang))
                {
                    this.pathTextBox.Text = path;
                }
                else
                {
                    this.ShowMessage("此目錄下無任何java 檔存在");
                }
            }
            );
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // 按下 trim 
            string lang = this.GetSelectedLanguage();
            if (string.IsNullOrEmpty(lang))
            {
                this.ShowMessage("請選擇程式語言!!");

                return;
            }

            if (string.IsNullOrEmpty(this.pathTextBox.Text) || string.IsNullOrEmpty(this.prefixTextBox.Text))
            {
                this.ShowMessage("路徑與Prefix不可為空!!");

                return;
            }

            string path = this.pathTextBox.Text;
            if (!Directory.Exists(path))
            {
                this.ShowMessage("選擇路徑不存在!!");

                return;
            }


            DialogResult result = System.Windows.Forms.MessageBox.Show
                                    (
                                    "確定進行去掉程式碼前綴字?",
                                    "確認",
                                    MessageBoxButtons.YesNo
                                    );
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                this.TrimCode(path, this.prefixTextBox.Text.Trim(), lang);
            }
        }

        private bool SetTrimPrefix(string path, string lang)
        {
            bool succeed = true;

            DirectoryInfo dir = new DirectoryInfo(path);
            List<FileInfo> files = dir.GetFilesByExtensions("." + lang);
            if (files.Count > 0)
            {
                FileInfo file = files[0];
                this.prefixTextBox.Text = file.Name.Substring(0, 3); // 取檔名前三個字
            }
            else
            {
                succeed = false;
            }

            return succeed;
        }

        private void TrimCode(string path, string prefix, string lang)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                List<FileInfo> files = dir.GetFilesByExtensions("." + lang);
                List<FileInfo> deleted = new List<FileInfo>();

                prefix = prefix.ToUpper();

                foreach (FileInfo file in files)
                {
                    if (file.Name.StartsWith(prefix))
                    {
                        string new_path = System.IO.Path.Combine(file.DirectoryName, file.Name.Remove(0, prefix.Length));
                        string content = File.ReadAllText(file.FullName);

                        switch (lang)
                        {
                            case "java":
                                {
                                    content = content.Replace(" " + prefix, " ");
                                    content = content.Replace("(" + prefix, "(");
                                    content = content.Replace(")" + prefix, ")");
                                    content = content.Replace("," + prefix, ",");
                                    content = content.Replace("=" + prefix, "=");
                                }
                                break;

                            case "swift":
                                {
                                    content = content.Replace(" " + prefix, " ");
                                    content = content.Replace(":" + prefix, ":");
                                    content = content.Replace("(" + prefix, "(");
                                    content = content.Replace("," + prefix, ",");
                                    content = content.Replace("!" + prefix, "!");
                                    content = content.Replace("=" + prefix, "=");
                                }
                                break;
                        }

                        File.WriteAllText(new_path, content);

                        deleted.Add(file);
                    }
                }

                DialogResult result = System.Windows.Forms.MessageBox.Show
                            (
                            "執行完成, 請到原目錄下確認新檔案, 是否要清除原檔",
                            "確認",
                            MessageBoxButtons.YesNo
                            );
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    // 砍掉原本的檔案
                    deleted.ForEach(file => file.Delete());
                }
            }
            catch (Exception ex)
            {
                this.ShowMessage("執行出現錯誤:\n" + ex.Message);
            }
        }

        private void OpenFolderDialog(Action<string> callback)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "選擇source code 目錄";
                dialog.ShowNewFolderButton = false;

                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    callback(dialog.SelectedPath);
                }
            }
        }

        private string GetAppPath()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

            // some IO classes does not support URI format (like StreamWriter)
            if (path.StartsWith("file:\\"))
            {
                path = path.Substring(6, path.Length - 6);
            }

            return path;
        }
    }


    // Windows extension function
    public static class WindowExtension
    {
        public static System.Windows.MessageBoxResult ShowMessage(this Window win, string message)
        {
            return System.Windows.MessageBox.Show(message);
        }
    }


    // DirectoryInfo extension function
    public static class DirectoryInfoExtension
    {
        public static List<FileInfo> GetFilesByExtensions(this DirectoryInfo dir, params string[] extensions)
        {
            if (extensions == null)
                throw new ArgumentNullException("extensions");
            IEnumerable<FileInfo> files = dir.EnumerateFiles();
            return new List<FileInfo>(files.Where(f => extensions.Contains(f.Extension)));
        }
    }
}
