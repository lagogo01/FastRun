using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FastRun
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            loadConfig();
            loadPowerStart();
        }

        void loadConfig()
        {
            string path = Application.StartupPath + "\\启动目录.ini";
            if (File.Exists(path))
            {
                StreamReader sr = new StreamReader(path);
                string txt = "";
                while (!sr.EndOfStream)
                {
                    string str = sr.ReadLine();
                    txt += str ;
                }
                sr.Close();

                if (txt.Trim().Length>0)
                {
                    GetFiles(new DirectoryInfo(txt), null);
                }
                else
                {
                    DefaultPath(path);
                }
            }
            else
            {
                DefaultPath(path);
            }
        }


        void DefaultPath(string path)
        {
            RegistryKey folders = OpenRegistryPath(Registry.CurrentUser, @"/software/microsoft/windows/currentversion/explorer/shell folders");

            GetFiles(new DirectoryInfo(folders.GetValue("Programs").ToString()), null);
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.WriteLine(folders.GetValue("Programs").ToString());
            }
        }

        private RegistryKey OpenRegistryPath(RegistryKey root, string s)
        {
            s = s.Remove(0, 1) + @"/";
            while (s.IndexOf(@"/") != -1)
            {
                root = root.OpenSubKey(s.Substring(0, s.IndexOf(@"/")));
                s = s.Remove(0, s.IndexOf(@"/") + 1);
            }
            return root;
        }

        void loadPowerStart()
        {
            RegistryKey rk = Registry.LocalMachine;
            RegistryKey rk2 = rk.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            try
            {
                var sta = rk2.GetValue("JcShutdown");
                if (sta!=null)
                {
                    this.设置ToolStripMenuItem.Checked = true;
                }
                else
                {
                    this.设置ToolStripMenuItem.Checked = false;
                }
            }
            catch (Exception)
            {
                this.设置ToolStripMenuItem.Checked = false;
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        public void GetFiles(DirectoryInfo directory,ToolStripMenuItem tm)
        {
            if (directory.Exists)
            {
                try
                {
                    foreach (FileInfo info in directory.GetFiles())
                    {
                       ToolStripMenuItem tsm = new ToolStripMenuItem();

                        Image img = Image.FromHbitmap(GetIconByFileName(info.FullName).ToBitmap().GetHbitmap());
                        Graphics g = Graphics.FromImage(img);
                        g.DrawImage(img, 0, 0, img.Width, img.Height);

                        tsm.Tag = info.FullName;
                        tsm.Image = img;
                        tsm.Click += Tsm_Click;
                        tsm.Text =info.Name.Replace(info.Extension,"");

                        if (tm != null) {
                            tm.DropDownItems.Add(tsm);
                        }
                        else {
                            this.contextMenuStrip1.Items.Add(tsm);
                        }         
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                foreach (DirectoryInfo info in directory.GetDirectories())//获取文件夹下的子文件夹
                {
                    ToolStripMenuItem tsm = new ToolStripMenuItem();
                    Image img = Image.FromHbitmap(GetIconByFileName(new DirectoryInfo(Environment.SystemDirectory).Parent.FullName+ "\\explorer.exe").ToBitmap().GetHbitmap());
                    Graphics g = Graphics.FromImage(img);
                    g.DrawImage(img, 0, 0, img.Width, img.Height);

                    tsm.Image = img;
                    tsm.Text = info.Name;
                    this.contextMenuStrip1.Items.Add(tsm);
                    GetFiles(info,tsm);//递归调用该函数，获取子文件夹下的文件
                }
            }
        }

        private void Tsm_Click(object sender, EventArgs e)
        {
            var text = sender as ToolStripMenuItem;
            Process myPro = new Process();
            myPro.StartInfo.FileName = "cmd.exe";
            myPro.StartInfo.UseShellExecute = false;
            myPro.StartInfo.RedirectStandardInput = true;
            myPro.StartInfo.RedirectStandardOutput = true;
            myPro.StartInfo.RedirectStandardError = true;
            myPro.StartInfo.CreateNoWindow = true;
            myPro.Start();
            //如果调用程序路径中有空格时，cmd命令执行失败，可以用双引号括起来 ，在这里两个引号表示一个引号（转义）

            myPro.StandardInput.WriteLine(text.Tag.ToString());
            myPro.StandardInput.AutoFlush = true;
        }

        private void 设置ToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            var tsm = sender as ToolStripMenuItem;
            if (tsm.Checked) //设置开机自启动  
            {
                string path = Process.GetCurrentProcess().MainModule.FileName;
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                rk2.SetValue("JcShutdown", path);
                rk2.Close();
                rk.Close();
            }
            else //取消开机自启动  
            {
                string path = Process.GetCurrentProcess().MainModule.FileName; //Application.ExecutablePath;
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                rk2.DeleteValue("JcShutdown", false);
                rk2.Close();
                rk.Close();
            }
        }

        private void 设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tsm = sender as ToolStripMenuItem;
            tsm.Checked = !tsm.Checked;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        /// <summary>
        /// 定义调用的API方法
        /// </summary>
        class Win32
        {
            public const uint SHGFI_ICON = 0x100;
            public const uint SHGFI_LARGEICON = 0x0; // 'Large icon
            public const uint SHGFI_SMALLICON = 0x1; // 'Small icon

            [DllImport("shell32.dll")]
            public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
            [DllImport("shell32.dll")]
            public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, int[] phiconLarge, int[] phiconSmall, uint nIcons);
        }

        public Icon GetIconByFileName(string fileName)
        {
            if (fileName == null || fileName.Equals(string.Empty)) return null;
            if (!File.Exists(fileName)) return null;

            SHFILEINFO shinfo = new SHFILEINFO();
            //Use this to get the small Icon
            Win32.SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), Win32.SHGFI_ICON | Win32.SHGFI_SMALLICON);
            //The icon is returned in the hIcon member of the shinfo struct
            Icon myIcon = Icon.FromHandle(shinfo.hIcon);
            return myIcon;
        }
    }
}
