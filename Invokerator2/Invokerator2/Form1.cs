using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Invokerator2
{
    public partial class Form1 : Form
    {
        private bool isCmdModified = false;
        private string tempCmdDirectory;

        public Form1()
        {
            InitializeComponent();
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop; 
            this.Start.AllowDrop = true;
            this.Start.DragEnter += Form1_DragEnter;
            this.Start.DragDrop += Form1_DragDrop;
            this.FormClosing += Form1_FormClosing;
        }

        private async void Start_Click(object sender, EventArgs e)
        {
            if (!isCmdModified)
            {
                ModifyCmd();
                isCmdModified = true;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string command = $"{GetModifiedCmdPath()} /c \"cd /d \"{Directory.GetCurrentDirectory()}\" & set __COMPAT_LAYER=RUNASINVOKER & start \"\" \"{openFileDialog.FileName}\" && exit\"";

                await RunCommandAsync(command);
            }
        }

        private void ModifyCmd()
        {
            string windir = Environment.GetEnvironmentVariable("windir");
            string cmdPath = Path.Combine(windir, "System32\\cmd.exe");

            tempCmdDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempCmdDirectory);

            string copyPath = Path.Combine(tempCmdDirectory, "cmd.exe");
            File.Copy(cmdPath, copyPath, true);

            byte[] bytes = File.ReadAllBytes(copyPath);
            for (int i = 0; i < bytes.Length - 2; i++)
            {
                if (bytes[i] == 0x44 && bytes[i + 1] == 0x4F && bytes[i + 2] == 0x53)
                {
                    bytes[i + 2] = 0x73;
                    break;
                }
            }
            File.WriteAllBytes(copyPath, bytes);
        }

        private string GetModifiedCmdPath()
        {
            return Path.Combine(tempCmdDirectory, "cmd.exe");
        }

        private Task<int> RunCommandAsync(string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = GetModifiedCmdPath(),
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            var process = new Process { StartInfo = processStartInfo };
            var tcs = new TaskCompletionSource<int>();

            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (!isCmdModified)
            {
                ModifyCmd();
                isCmdModified = true;
            }

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                string filePath = files[0];

                string command = $" /c \"cd /d \"{Directory.GetCurrentDirectory()}\" & set __COMPAT_LAYER=RUNASINVOKER & start \"\" \"{filePath}\" && exit\"";

                RunCommandAsync(command);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isCmdModified == true)
            {
                string modifiedCmdPath = GetModifiedCmdPath();
                if (File.Exists(modifiedCmdPath))
                {
                    try
                    {
                        File.Delete(modifiedCmdPath);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting modified cmd.exe: {ex.Message}");
                    }
                }
                else
                {
                    Application.Exit();
                }
            }
            else
            {
                Application.Exit();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
