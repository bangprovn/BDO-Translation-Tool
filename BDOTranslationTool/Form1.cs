using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Web.UI;
using System.Web.Script.Serialization;

namespace BDOTranslationTool
{
    public partial class BDOTranslationTool : Form
    {
        string _AppPath = AppDomain.CurrentDomain.BaseDirectory;
        string _GamePath;
        bool _Installing = false, _Uninstalling = false, _Decompressing = false, _Downloading = false, _Merging = false;
        Dictionary<string, string[]> translator = new Dictionary<string, string[]>();
        string jsonUrl = "https://lehieugch68.github.io/BDO/translator.json";
        public BDOTranslationTool()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.Icon;
        }

        public void ReportProgress(int percent)
        {
            this.progressBar.BeginInvoke((MethodInvoker)delegate ()
            {
                int p = (percent > 100) ? 100 : percent;
                progressBar.Value = p;
            });
        }

        public void ReportStatus(string status)
        {
            this.Status.BeginInvoke((MethodInvoker)delegate ()
            {
                Status.Text = status;
            });
        }

        public void Write_Log(string text)
        {
            this.Log.BeginInvoke((MethodInvoker)delegate ()
            {
                Log.Items.Add(text);
                Log.SelectedIndex = Log.Items.Count - 1;
            });
        }

        private void BDOTranslationTool_Load(object sender, EventArgs e)
        {
            string registryPath = @"SOFTWARE\Wow6432Node\BlackDesert_ID";
            try
            {
                _GamePath = Registry.LocalMachine.OpenSubKey(registryPath).GetValue("Path").ToString();
                GamePath.Text = _GamePath;
            }
            catch
            {
                MessageBox.Show("Kh??ng t??m th???y th?? m???c c??i ?????t Black Desert Online!\nVui l??ng ch???n ???????ng d???n th??? c??ng.", "Th??ng b??o");
            }
            string jsonFile = $"{_AppPath}\\translator\\translator.json";
            if (File.Exists(jsonFile))
            {
                translator = new JavaScriptSerializer().Deserialize<Dictionary<string, string[]>>(File.ReadAllText(jsonFile));
                
            }
            else
            {
                translator.Add("S??", new string[] { "", "BDO_Translation_Su.zip", "S??", "https://www.facebook.com/visaosang2305" });
                translator.Add("L?? Hi???u", new string[] { "", "BDO_Translation_LeHieu.zip", "L?? Hi???u", "https://www.facebook.com/le.anh.hieu.68" });
            }
            foreach (string key in translator.Keys)
            {
                selectTranslator.Items.Add(key);
            }
            selectTranslator.SelectedIndex = 0;
        }

        private void Browser_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    _GamePath = fbd.SelectedPath;
                    GamePath.Text = _GamePath;
                }
            }
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(_AppPath, "translator");
            if (!_Downloading && !_Merging)
            {
                _Downloading = true;
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        Write_Log($"T???o th?? m???c: {path}");
                    }
                    Write_Log($"??ang t???i t???p JSON...");
                    using (WebClient JsonDownload = new WebClient())
                    {
                        JsonDownload.DownloadFile(jsonUrl, Path.Combine(path, "translator.json"));
                    }
                    translator = new JavaScriptSerializer().Deserialize<Dictionary<string, string[]>>(File.ReadAllText(Path.Combine(path, "translator.json")));
                    string[] value;
                    if (translator.TryGetValue(selectTranslator.GetItemText(selectTranslator.SelectedItem), out value))
                    {
                        using (WebClient download = new WebClient())
                        {
                            download.DownloadProgressChanged += download_ProgressChanged;
                            download.DownloadFileCompleted += download_Completed;
                            string zipFile = Path.Combine(path, value[1]);
                            download.QueryString.Add("path", zipFile);
                            Write_Log($"??ang t???i xu???ng b???n d???ch c???a {value[2]}...");
                            download.DownloadFileAsync(new Uri(value[0]), zipFile);
                        }
                    }
                }
                catch (Exception err)
                {
                    Write_Log("X???y ra l???i khi t???i xu???ng b???n d???ch!");
                    MessageBox.Show("???? x???y ra l???i!\n\n" + err, "Th??ng b??o");
                    _Downloading = false;
                }
            }
            else
            {
                string msg = _Downloading ? "??ang t???i b???n d???ch!" : "??ang g???p b???n d???ch!";
                MessageBox.Show(msg, "Th??ng b??o");
            }
        }

        private void download_ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBarDownload.Value = e.ProgressPercentage;
        }

        private void download_Completed(object sender, AsyncCompletedEventArgs e)
        {
            string path = ((WebClient)(sender)).QueryString["path"];
            FileInfo zipFile = new FileInfo(path);
            if (zipFile.Length > 0)
            {
                Write_Log($"??ang gi???i n??n b???n d???ch...");
                string extractPath = Path.Combine(_AppPath, "translator");
                try
                {
                    Task.Run(() =>
                    {
                        using (var zip = ZipFile.OpenRead(path))
                        {
                            foreach (var entry in zip.Entries)
                            {
                                string destinationPath = Path.Combine(extractPath, entry.FullName);
                                entry.ExtractToFile(destinationPath, true);
                            }
                        }
                    }).GetAwaiter().OnCompleted(() =>
                    {
                        _Downloading = false;
                        File.Delete(path);
                        Write_Log("Gi???i n??n b???n d???ch th??nh c??ng.");
                    });
                }
                catch (Exception err)
                {
                    Write_Log("X???y ra l???i khi gi???i n??n b???n d???ch!");
                    _Downloading = false;
                    MessageBox.Show("???? x???y ra l???i!\n\n" + err, "Th??ng b??o");
                }
            }
            else
            {
                _Downloading = false;
                File.Delete(path);
                Write_Log($"T???i th???t b???i!");
            }
        }

        private void buttonContact_Click(object sender, EventArgs e)
        {
            string[] value;
            if (translator.TryGetValue(selectTranslator.GetItemText(selectTranslator.SelectedItem), out value))
            {
                try
                {
                    Process.Start(value[3]);
                }
                catch { }
            }
        }

        private void Install_Click(object sender, EventArgs e)
        {
            if (!_Installing && !_Uninstalling && !_Decompressing && !_Merging)
            {
                string backupFile = $"{_GamePath}\\ads\\backup\\languagedata_en.loc";
                string sourceFile = $"{_GamePath}\\ads\\languagedata_en.loc";
                string encryptFile = $"{_AppPath}\\languagedata_en.loc";
                string decryptFile = $"{_AppPath}\\languagedata_en.tsv";
                string translationFile = $"{_AppPath}\\BDO_Translation.tsv";
                if (File.Exists(sourceFile))
                {
                    _Installing = true;
                    ReportProgress(0);
                    if (!File.Exists(backupFile))
                    {
                        CopyFile(sourceFile, backupFile);
                    }
                    Task.Run(() => decrypt(decompress(backupFile), decryptFile)).GetAwaiter().OnCompleted(() =>
                    {
                        if (!_Installing) return;
                        ReportProgress(25);
                        ReportStatus("??ang sao ch??p b???n d???ch");
                        Task.Run(() => Replace_Text(decryptFile, translationFile)).GetAwaiter().OnCompleted(() =>
                        {
                            if (!_Installing) return;
                            ReportProgress(50);
                            Task.Run(() => compress(encrypt(decryptFile), decryptFile)).GetAwaiter().OnCompleted(() =>
                            {
                                if (!_Installing) return;
                                ReportStatus("??ang sao ch??p");
                                ReportProgress(75);
                                Task.Run(() => CopyFile(encryptFile, sourceFile)).GetAwaiter().OnCompleted(() =>
                                {
                                    if (!_Installing) return;
                                    _Installing = false;
                                    ReportStatus("C??i ?????t th??nh c??ng!");
                                    ReportProgress(100);
                                });
                            });
                        });
                    });
                }
                else
                {
                    MessageBox.Show("Kh??ng t??m th???y t???p languagedata_en.loc!", "Th??ng b??o");
                }
            }

        }

        private void buttonDecompress_Click(object sender, EventArgs e)
        {
            if (!_Installing && !_Uninstalling && !_Decompressing && !_Merging)
            {
                string backupFile = $"{_GamePath}\\ads\\backup\\languagedata_en.loc";
                string sourceFile = File.Exists(backupFile) ? backupFile : $"{_GamePath}\\ads\\languagedata_en.loc";
                string decryptFile = $"{_AppPath}\\languagedata_en.tsv";
                string translationFile = $"{_AppPath}\\BDO_Translation.tsv";
                bool _Overwrite = true;
                if (File.Exists(translationFile))
                {
                    DialogResult dialogResult = MessageBox.Show("Ph??t hi???n t???p BDO_Translation.tsv\nB???n c?? mu???n ghi ???? t???p n??y?", "Th??ng b??o", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes) { }
                    else if (dialogResult == DialogResult.No)
                    {
                        _Overwrite = false;
                    }
                }
                if (!File.Exists(sourceFile))
                {
                    MessageBox.Show("Kh??ng t??m th???y t???p languagedata_en.loc!", "Th??ng b??o");
                }
                else
                {
                    _Decompressing = true;
                    ReportProgress(0);
                    Task.Run(() => { decrypt(decompress(sourceFile), decryptFile); }).GetAwaiter().OnCompleted(() =>
                    {
                        if (!_Decompressing) return;
                        if (_Overwrite)
                        {
                            ReportProgress(50);
                            Task.Run(() => Remove_Duplicate(decryptFile, translationFile)).GetAwaiter().OnCompleted(() =>
                            {
                                if (_Decompressing)
                                {
                                    _Decompressing = false;
                                    ReportStatus("Gi???i n??n th??nh c??ng!");
                                }
                            });
                        }
                        else
                        {
                            if (_Decompressing)
                            {
                                _Decompressing = false;
                                ReportProgress(100);
                                ReportStatus("Gi???i n??n th??nh c??ng!");
                            }
                        }
                    });
                }
            }
        }

        private void buttonMerge_Click(object sender, EventArgs e)
        {
            string[] value;
            if (!_Installing && !_Uninstalling && !_Decompressing && !_Merging && !_Downloading && translator.TryGetValue(selectTranslator.GetItemText(selectTranslator.SelectedItem), out value))
            {
                string transFile = Path.Combine(Path.Combine(_AppPath, "translator"), $"{Path.GetFileNameWithoutExtension(value[1])}.tsv");
                string destFile = Path.Combine(_AppPath, "BDO_Translation.tsv");
                if (File.Exists(transFile))
                {
                    _Merging = true;
                    ReportStatus($"??ang g???p b???n d???ch ({value[2]})");
                    Write_Log($"B???t ?????u g???p b???n d???ch ({value[2]})...");
                    ReportProgress(0);
                    Task.Run(() => Replace_Text(destFile, transFile)).GetAwaiter().OnCompleted(() =>
                    {
                        if (_Merging)
                        {
                            ReportProgress(100);
                            ReportStatus($"G???p b???n d???ch th??nh c??ng ({value[2]})");
                            Write_Log($"G???p b???n d???ch th??nh c??ng ({value[2]})!");
                            _Merging = false;
                        }

                    });
                }
                else
                {
                    MessageBox.Show($"Kh??ng t??m th???y t???p:\n{transFile}", "Th??ng b??o");
                }
            }
        }

        private void buttonBackup_Click(object sender, EventArgs e)
        {
            if (!_Installing && !_Decompressing)
            {
                string backupFile = $"{_GamePath}\\ads\\backup\\languagedata_en.loc";
                string sourceFile = $"{_GamePath}\\ads\\languagedata_en.loc";
                if (File.Exists(sourceFile))
                {
                    Write_Log("??ang sao l??u...");
                    Task.Run(() => CopyFile(sourceFile, backupFile)).GetAwaiter().OnCompleted(() =>
                    {
                        Write_Log("Sao l??u t???p g???c th??nh c??ng!");
                    });
                }
                else
                {
                    MessageBox.Show("Kh??ng t??m th???y t???p languagedata_en.loc!", "Th??ng b??o");
                }
            }
        }

        private void buttonRestore_Click(object sender, EventArgs e)
        {
            if (!_Installing && !_Decompressing)
            {
                string backupFile = $"{_GamePath}\\ads\\backup\\languagedata_en.loc";
                string sourceFile = $"{_GamePath}\\ads\\languagedata_en.loc";
                if (File.Exists(backupFile))
                {
                    Write_Log("??ang kh??i ph???c t???p g???c...");
                    Task.Run(() => CopyFile(backupFile, sourceFile)).GetAwaiter().OnCompleted(() =>
                    {
                        Write_Log("Kh??i ph???c t???p g???c th??nh c??ng!");
                    });
                }
                else
                {
                    MessageBox.Show("Kh??ng t??m th???y t???p sao l??u!", "Th??ng b??o");
                }
            }
        }

        private void CopyFile(string sourceFile, string destinationFile)
        {
            try
            {
                if (File.Exists(sourceFile))
                {
                    string directory = Path.GetDirectoryName(destinationFile);
                    if (!Directory.Exists(directory))
                    {
                        Write_Log($"T???o th?? m???c: {directory}");
                        Directory.CreateDirectory(directory);
                    }
                    File.Copy(sourceFile, destinationFile, true);
                }
            }
            catch (Exception e)
            {
                if (_Installing) _Installing = false;
                if (_Decompressing) _Decompressing = false;
                if (_Uninstalling) _Uninstalling = false;
                MessageBox.Show("???? x???y ra l???i!\n\n" + e, "Th??ng b??o");
                ReportStatus("Ch??a r??");
            }
        }

        private void Replace_Text(string sourceFile, string translationFile)
        {
            if (!_Installing && !_Decompressing && !_Merging) return;
            try
            {
                Write_Log($"??ang th??m nh???ng c??u ???????c d???ch...");
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                string[] allLines = File.ReadAllLines(translationFile);
                foreach (string line in allLines)
                {
                    string[] content = line.Split(new string[] { "\t" }, StringSplitOptions.None);
                    if (content.Length > 1)
                    {
                        content[0] = content[0].TrimStart((char)34).TrimEnd((char)34);
                        content[1] = content[1].TrimStart((char)34).TrimEnd((char)34).Replace($"{(char)34}", "<quot>");
                        if (content[0] != "<null>" && !content[0].StartsWith("http") && !string.IsNullOrWhiteSpace(content[0]) && !string.IsNullOrWhiteSpace(content[1]) && !dictionary.ContainsKey(content[0]))
                        {
                            dictionary.Add(content[0], content[1]);
                        }
                    }
                }
                Write_Log($"Th??m th??nh c??ng {dictionary.Count()} d??ng.");
                Write_Log("B???t ?????u d???ch...");
                allLines = File.ReadAllLines(sourceFile);
                int a = _Merging ? 1 : 5;
                int b = _Merging ? 0 : 5;
                int count = 0;
                using (StreamWriter temp = new StreamWriter(sourceFile, false, Encoding.Unicode))
                {
                    foreach (string line in allLines)
                    {
                        string[] content = line.Split(new string[] { "\t" }, StringSplitOptions.None);
                        if (content.Length > 1)
                        {
                            string value;
                            bool isInstalling = _Merging ? string.IsNullOrWhiteSpace(content[a]) : true;
                            if (!string.IsNullOrWhiteSpace(content[b]) && dictionary.TryGetValue(content[b], out value) && isInstalling)
                            {
                                count++;
                                content[a] = value;
                            }
                        }
                        temp.WriteLine(string.Join("\t", content));
                    }
                }
                Write_Log($"D???ch th??nh c??ng {count} d??ng!");
            }
            catch (Exception e)
            {
                ReportStatus("Ch??a r??");
                Write_Log("???? x???y ra l???i!");
                if (_Installing) _Installing = false;
                if (_Decompressing) _Decompressing = false;
                if (_Merging) _Merging = false;
                MessageBox.Show("???? x???y ra l???i!\n\n" + e, "Th??ng b??o");
            }
        }

        private void Remove_Duplicate(string sourceFile, string transFile)
        {
            if (!_Installing && !_Decompressing) return;
            try
            {
                string[] allLines = File.ReadAllLines(sourceFile);
                Write_Log($"??ang l???c nh???ng c??u b??? tr??ng (t???ng {allLines.Length} d??ng)...");
                double total = allLines.Length;
                double count = 0;
                int remove = 0;
                List<string> lines = new List<string>();
                using (var writer = new StreamWriter(transFile, false, Encoding.Unicode))
                {
                    foreach (string line in allLines)
                    {
                        count++;
                        ReportStatus($"??ang l???c nh???ng c??u b??? tr??ng ({count}/{total} d??ng)");
                        ReportProgress((int)(count * 50 / total) + 50);
                        string[] content = line.Split(new string[] { "\t" }, StringSplitOptions.None);
                        if (content.Length > 1 && !string.IsNullOrWhiteSpace(content[5]) && content[5] != "<null>" && !content[5].StartsWith("http") && !lines.Contains(content[5]))
                        {
                            writer.WriteLine($"{content[5]}\t");
                            lines.Add(content[5]);
                        }
                        else
                        {
                            remove++;
                        }
                    }
                }
                Write_Log($"???? lo???i b??? {remove} c??u tr??ng.");
            }
            catch (Exception e)
            {
                if (_Installing) _Installing = false;
                if (_Decompressing) _Decompressing = false;
                ReportStatus("Ch??a r??");
                MessageBox.Show("???? x???y ra l???i!\n\n" + e, "Th??ng b??o");
            }
        }
        private MemoryStream decompress(string file)
        {
            ReportStatus("??ang gi???i n??n");
            Write_Log("B???t ?????u gi???i n??n...");
            MemoryStream stream = new MemoryStream();
            try
            {
                using (var input = File.OpenRead(file))
                {
                    input.Seek(6, SeekOrigin.Current);
                    using (var deflateStream = new DeflateStream(input, CompressionMode.Decompress, true))
                    {
                        deflateStream.CopyTo(stream);
                    }
                }
                Write_Log($"Gi???i n??n th??nh c??ng {stream.Length} byte.");
            }
            catch (Exception e)
            {
                ReportStatus("Ch??a r??");
                Write_Log("X???y ra l???i khi gi???i n??n t???p!");
                if (_Installing) _Installing = false;
                if (_Decompressing) _Decompressing = false;
                MessageBox.Show("???? x???y ra l???i!\n\n" + e, "Th??ng b??o");
            }
            return stream;
        }

        private void compress(MemoryStream stream, string file)
        {
            if (!_Installing) return;
            try
            {
                stream.Position = 0;
                byte[] input = stream.ToArray();
                byte[] size = BitConverter.GetBytes(Convert.ToUInt32(input.Length));
                ReportStatus($"??ang n??n");
                Write_Log($"B???t ?????u n??n {input.Length} byte...");
                Deflater compressor = new Deflater();
                compressor.SetLevel(Deflater.BEST_SPEED);
                compressor.SetInput(input);
                compressor.Finish();
                MemoryStream bos = new MemoryStream(input.Length);
                byte[] buf = new byte[1024];
                while (!compressor.IsFinished)
                {
                    int count = compressor.Deflate(buf);
                    bos.Write(buf, 0, count);
                }
                byte[] output = bos.ToArray();
                string directory = Path.GetDirectoryName(file);
                string filename = Path.GetFileNameWithoutExtension(file);
                FileStream writeStream = new FileStream($"{directory}\\{filename}.loc", FileMode.Create);
                BinaryWriter writeBinary = new BinaryWriter(writeStream);
                writeBinary.Write(size);
                writeBinary.Write(output);
                Write_Log($"N??n th??nh c??ng {input.Length} byte => {output.Length} byte.");
                writeBinary.Close();
            }
            catch (Exception e)
            {
                if (_Installing) _Installing = false;
                ReportStatus("Ch??a r??");
                Write_Log("X???y ra l???i khi n??n t???p!");
                MessageBox.Show("???? x???y ra l???i!\n\n" + e, "Th??ng b??o");
            }
        }

        private void decrypt(MemoryStream stream, string decryptFile)
        {
            if (!_Installing && !_Decompressing) return;
            try
            {
                stream.Position = 0;
                using (var reader = new BinaryReader(stream))
                {
                    long total = reader.BaseStream.Length;
                    ReportStatus($"??ang gi???i m??");
                    Write_Log($"B???t ?????u gi???i m?? {total} byte...");
                    using (var output = new StreamWriter(decryptFile, false, Encoding.Unicode))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            UInt32 strSize = reader.ReadUInt32();
                            UInt32 strType = reader.ReadUInt32();
                            UInt32 strID1 = reader.ReadUInt32();
                            UInt16 strID2 = reader.ReadUInt16();
                            byte strID3 = reader.ReadByte();
                            byte strID4 = reader.ReadByte();
                            string str = Encoding.Unicode.GetString(reader.ReadBytes(Convert.ToInt32(strSize * 2))).Replace("\n", "<lf>").Replace($"{(char)34}", "<quot>");
                            if (str.StartsWith("=") || str.StartsWith("+") || str.StartsWith("-")) str = $"'{str}";
                            reader.ReadBytes(4);
                            output.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", strType, strID1, strID2, strID3, strID4, str);
                        }
                        Write_Log($"Gi???i m?? th??nh c??ng {total} byte => {output.BaseStream.Length} byte.");
                    }
                }
            }
            catch (Exception e)
            {
                if (_Installing) _Installing = false;
                if (_Decompressing) _Decompressing = false;
                ReportStatus("Ch??a r??");
                Write_Log("X???y ra l???i khi gi???i m?? t???p!");
                MessageBox.Show("???? x???y ra l???i!\n\n" + e, "Th??ng b??o");
            }
        }

        private MemoryStream encrypt(string file)
        {
            MemoryStream stream = new MemoryStream();
            try
            {
                string[] allLines = File.ReadAllLines(file);
                ReportStatus($"??ang m?? h??a");
                Write_Log($"B???t ?????u m?? h??a {allLines.Length} d??ng...");
                BinaryWriter writeBinary = new BinaryWriter(stream);
                byte[] zeroes = { (byte)0, (byte)0, (byte)0, (byte)0 };
                string[] excel_cal_char = { "'+", "'=", "'-" };
                foreach (string line in allLines)
                {
                    string[] content = line.Split(new string[] { "\t" }, StringSplitOptions.None);
                    byte[] strType = BitConverter.GetBytes(Convert.ToUInt32(content[0]));
                    byte[] strID1 = BitConverter.GetBytes(Convert.ToUInt32(content[1]));
                    byte[] strID2 = BitConverter.GetBytes(Convert.ToUInt16(content[2]));
                    byte strID3 = Convert.ToByte(content[3]);
                    byte strID4 = Convert.ToByte(content[4]);
                    string str = content[5].Replace("<lf>", "\n").Replace("<quot>", $"{(char)34}");
                    if (excel_cal_char.Any(character => str.StartsWith(character)))
                    {
                        str = str.TrimStart((char)39);
                    }
                    byte[] strBytes = Encoding.Unicode.GetBytes(str);
                    byte[] strSize = BitConverter.GetBytes(str.Length);
                    writeBinary.Write(strSize);
                    writeBinary.Write(strType);
                    writeBinary.Write(strID1);
                    writeBinary.Write(strID2);
                    writeBinary.Write(strID3);
                    writeBinary.Write(strID4);
                    writeBinary.Write(strBytes);
                    writeBinary.Write(zeroes);
                }
                Write_Log($"M?? h??a th??nh c??ng {allLines.Length} d??ng => {stream.Length} byte.");
            }
            catch (Exception e)
            {
                if (_Installing) _Installing = false;
                if (_Decompressing) _Decompressing = false;
                ReportStatus("???? x???y ra l???i");
                Write_Log("???? x???y ra l???i khi m?? h??a t???p!");
                MessageBox.Show("???? x???y ra l???i!\n\n" + e, "Th??ng b??o");
            }
            return stream;
        }
        private void linkGithub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/lehieugch68/BDO-Translation-Tool");
        }
        private void linkVHG_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://viethoagame.com/threads/pc-black-desert-online-viet-hoa.222/");
        }
    }
}
