﻿using CefSharp;
using CefSharp.WinForms;
using CefSharp.WinForms.Internals;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Korot
{
    public partial class frmCEF : Form
    {
        TabPage parentTabPage;
        bool isLoading = false;
        string loaduri = null;
        bool _Incognito = false;
        string userName;
        string userCache;
        frmMain anaform;
        ChromiumWebBrowser chromiumWebBrowser1;
        string defaultproxyaddress;
        public frmCEF(TabPage pranetPage, frmMain rmmain, bool isIncognito, string loadurl, string profileName)
        {
            InitializeComponent();
            parentTabPage = pranetPage;
            loaduri = loadurl;
            anaform = rmmain;
            _Incognito = isIncognito;
            userName = profileName;
            userCache = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Korot\\Users\\" + profileName + "\\cache\\";
            WebProxy proxy = (WebProxy)WebProxy.GetDefaultProxy();
            Uri resource = new Uri("http://localhost");
            Uri resourceProxy = proxy.GetProxy(resource);
           if (resourceProxy == resource)
            {
                defaultproxyaddress = null;
                Output.WriteLine("[INFO] No proxy detected.");
            } else {
                defaultproxyaddress = resourceProxy.AbsoluteUri.ToString();
            Output.WriteLine("[INFO] Listening on proxy : " + defaultproxyaddress);
            SetProxy(chromiumWebBrowser1, defaultproxyaddress);
            }

            InitializeChromium();
        }
        async private void SetProxy(ChromiumWebBrowser cwb, string Address)
        {
            if (Address == null) { }else {
            await Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var rc = cwb.GetBrowser().GetHost().RequestContext;
                var v = new Dictionary<string, object>();
                v["mode"] = "fixed_servers";
                v["server"] = Address;
                string error;
                bool success = rc.SetPreference("proxy", v, out error);
            });
            }
        }
        private static ManagementObject GetMngObj(string className)
        {
            var wmi = new ManagementClass(className);

            foreach (var o in wmi.GetInstances())
            {
                var mo = (ManagementObject)o;
                if (mo != null) return mo;
            }

            return null;
        }

        public static string GetOsVer()
        {
            try
            {
                ManagementObject mo = GetMngObj("Win32_OperatingSystem");

                if (null == mo)
                    return string.Empty;

                return mo["Version"] as string;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int Brightness(System.Drawing.Color c)
        {
            return (int)Math.Sqrt(
               c.R * c.R * .241 +
               c.G * c.G * .691 +
               c.B * c.B * .068);
        }
        public void InitializeChromium()
        {
            CefSettings settings = new CefSettings();
            string ProductName =
Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "").ToString();
            settings.UserAgent = "Mozilla/5.0 ( Windows NT " + GetOsVer() + "; " + Environment.OSVersion.Platform + ") AppleWebKit/537.36 (KHTML, like Gecko) Chrome/" + Cef.ChromiumVersion + " Safari/537.36 Korot/" + Application.ProductVersion.ToString();
            if (_Incognito) { settings.CachePath = null; settings.PersistSessionCookies = false; }
            else { settings.CachePath = userCache; }
            // Initialize cef with the provided settings
            if (Cef.IsInitialized == false) { Cef.Initialize(settings); }
            chromiumWebBrowser1 = new ChromiumWebBrowser(loaduri); 
            panel1.Controls.Add(chromiumWebBrowser1);
            chromiumWebBrowser1.DisplayHandler = new DisplayHandler(this,anaform);
            chromiumWebBrowser1.LoadingStateChanged += loadingstatechanged;
            chromiumWebBrowser1.TitleChanged += cef_TitleChanged;
            chromiumWebBrowser1.AddressChanged += cef_AddressChanged;
            chromiumWebBrowser1.LoadError += cef_onLoadError;
            chromiumWebBrowser1.KeyDown += tabform_KeyDown;
            chromiumWebBrowser1.MenuHandler = new ContextMenuHandler(this, anaform);
            chromiumWebBrowser1.LifeSpanHandler = new BrowserLifeSpanHandler(this);
            chromiumWebBrowser1.DownloadHandler = new DownloadHandler(this, anaform);
            chromiumWebBrowser1.JsDialogHandler = new JsHandler(anaform);
            chromiumWebBrowser1.DialogHandler = new MyDialogHandler();
            chromiumWebBrowser1.Dock = DockStyle.Fill;
            chromiumWebBrowser1.Show();
        }
        public void executeStartupExtensions()
        {
            foreach (String x in Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Korot\\Users\\" + userName + "\\Extensions\\", "*.*", SearchOption.AllDirectories))
            {
                if (x.EndsWith("startup.js", StringComparison.CurrentCultureIgnoreCase))
                {
                    try
                    {
                        Output.WriteLine("[Korot] Script Execute : " + x);
                        chromiumWebBrowser1.ExecuteScriptAsync(File.ReadAllText(x));
                        Output.WriteLine("[Korot] Script Execute Completed: " + x);
                    }
                    catch (Exception ex)
                    {
                        Output.WriteLine("[Korot] Script Execute Error : {Script:" + x + ",ErrorMessage:" + ex.Message + "}");
                        continue;
                    }
                }
            }
        }
        public void ChangeStatus(string status)
        {
            label2.Text = status;
        }
        public void loadingstatechanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (e.IsLoading)
            {
                if (Brightness(Properties.Settings.Default.BackColor) > 130)
                {
                    button2.Image = Korot.Properties.Resources.cancel;
                }
                else { button2.Image = Korot.Properties.Resources.cancel_w; }
            }
            else
            {
                if (_Incognito) { }
                else
                {
                    this.InvokeOnUiThreadIfRequired(() => Korot.Properties.Settings.Default.History += DateTime.Now.ToString("dd/MM/yy hh:mm:ss") + ";" + this.Text + ";" + (textBox1.Text) + ";");

                }
                executeStartupExtensions();
                if (Brightness(Properties.Settings.Default.BackColor) > 130)
                {
                    button2.Image = Korot.Properties.Resources.refresh;
                }
                else
                { button2.Image = Korot.Properties.Resources.refresh_w; }
            }
            try
            {
                button1.Invoke(new Action(() => button1.Enabled = e.CanGoBack));
                button3.Invoke(new Action(() => button3.Enabled = e.CanGoForward));
            }catch
            {
                button1.Invoke(new Action(() => button1.Enabled = false));
                button3.Invoke(new Action(() => button3.Enabled = false));
            }
            isLoading = e.IsLoading;
        }

        public void NewTab(string url)
        {
            anaform.Invoke(new Action(() => anaform.NewTab(url)));
        }
        public void RefreshFavorites()
        {
            mFavorites.Items.Clear();
            string Playlist = Properties.Settings.Default.Favorites;
            string[] SplittedFase = Playlist.Split(';');
            int Count = SplittedFase.Length - 1; ; int i = 0;
            while (!(i == Count))
            {
                ToolStripMenuItem miFavorite = new ToolStripMenuItem();
                miFavorite.Tag = SplittedFase[i].Replace(Environment.NewLine, "");
                i += 1;
                miFavorite.Text = SplittedFase[i].Replace(Environment.NewLine, "");
                i += 1;
                miFavorite.Click += TestToolStripMenuItem_Click;
                mFavorites.Items.Add(miFavorite);
                i += 1;
            }
        }
        private void tabform_Load(object sender, EventArgs e)
        {
            if (_Incognito) { } else { pictureBox1.Visible = false; textBox1.Size = new Size(textBox1.Size.Width + pictureBox1.Size.Width, textBox1.Size.Height); }

            RefreshFavorites();
            LoadProxies();
            LoadExt();
            RefreshProfiles();
            profilenameToolStripMenuItem.Text = userName;
            label3.Text = anaform.SearchOnPage;
            label6.Text = anaform.CaseSensitive;
        }


        public static bool ValidHttpURL(string s, out Uri resultURI)
        {
            if (!Regex.IsMatch(s, @"^https?:\/\/", RegexOptions.IgnoreCase))
            {
                if (s.Contains(".") || s.Contains(":") || !(s.EndsWith(".")) || !(s.EndsWith(":")) || !(s.StartsWith(".")) || !(s.StartsWith(":"))) { s = "http://" + s; Output.WriteLine(s); } else { resultURI = null; return false; }
            }

            if (Uri.TryCreate(s, UriKind.Absolute, out resultURI))
            { return (resultURI.Scheme == Uri.UriSchemeHttp || resultURI.Scheme == Uri.UriSchemeHttps); }

            else { return false; }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.ToLower() == "korot.settings:searchpage")
            {
                chromiumWebBrowser1.Load(Properties.Settings.Default.SearchURL);
            }
            else if (textBox1.Text.ToLower() == "korot.settings:homepage")
            {
                chromiumWebBrowser1.Load(Properties.Settings.Default.Homepage);
            }
            else if (textBox1.Text.ToLower() == "korot.settings:savedFormResolutions")
            {
                chromiumWebBrowser1.LoadHtml("x: " + Properties.Settings.Default.WindowPosX + Environment.NewLine +
                    "y: " + Properties.Settings.Default.WindowPosY + Environment.NewLine +
                    "Width: " + Properties.Settings.Default.WindowSizeW + Environment.NewLine +
                    "Height: " + Properties.Settings.Default.WindowSizeH);
            }
            else if (textBox1.Text.ToLower() == "korot.settings:downloadSettings")
            {
                chromiumWebBrowser1.LoadHtml("openafterfinished: " + Properties.Settings.Default.downloadOpen + Environment.NewLine +
                    "closeafterfinished: " + Properties.Settings.Default.downloadClose);
            }
            else if (textBox1.Text.ToLower() == "korot.settings:languageFile")
            {
                chromiumWebBrowser1.LoadHtml(Properties.Settings.Default.LangFile);
            }
            else if (textBox1.Text.ToLower() == "korot.settings:theme")
            {
                chromiumWebBrowser1.LoadHtml("themeFile: " + Properties.Settings.Default.ThemeFile + Environment.NewLine + 
                    "BackColor: rgb(" + Properties.Settings.Default.BackColor.ToArgb().ToString() + ")" + Environment.NewLine +
                    "OverlayColor: rgb(" + Properties.Settings.Default.OverlayColor.ToArgb().ToString() + ")");
            }
            else if (textBox1.Text.ToLower() == "korot.settings:lastUser")
            {
                chromiumWebBrowser1.LoadHtml(Properties.Settings.Default.LastUser);
            } //defaults
            else if (textBox1.Text.ToLower() == "korot.settings.default:searchpage")
            {
                chromiumWebBrowser1.Load("");
            }
            else if (textBox1.Text.ToLower() == "korot.settings.default:homepage")
            {
                chromiumWebBrowser1.Load("about:blank");
            }
            else if (textBox1.Text.ToLower() == "korot.settings.default:savedFormResolutions")
            {
                chromiumWebBrowser1.LoadHtml("x: 0" + Environment.NewLine +
                    "y: 25"  + Environment.NewLine +
                    "Width: 0"  + Environment.NewLine +
                    "Height: 0" );
            }
            else if (textBox1.Text.ToLower() == "korot.settings.default:downloadSettings")
            {
                chromiumWebBrowser1.LoadHtml("openafterfinished: false"  + Environment.NewLine +
                    "closeafterfinished: false");
            }
            else if (textBox1.Text.ToLower() == "korot.settings.default:languageFile")
            {
                chromiumWebBrowser1.LoadHtml("English.lang");
            }
            else if (textBox1.Text.ToLower() == "korot.settings.default:theme")
            {
                chromiumWebBrowser1.LoadHtml("themeFile: " +  Environment.NewLine +
                    "BackColor: rgb(255, 255, 255, 255)"  + Environment.NewLine +
                    "OverlayColor: rgb(255, 30, 144, 55)");
            }
            else if (textBox1.Text.ToLower() == "korot.settings.default:lastUser")
            {
                chromiumWebBrowser1.LoadHtml("user0");
            }
            else { 
            string urlLower = textBox1.Text.ToLower();

            Uri newUri = null;
            if (ValidHttpURL(urlLower, out newUri))
            {
                Output.WriteLine("Valid URL");
                if (urlLower.StartsWith("http://korot://error"))
                {
                    cef_onLoadError(null, null);
                }
                else
                {
                    chromiumWebBrowser1.Load(urlLower);
                }
            }

            else
            {
                Output.WriteLine("Not Valid URL");
                chromiumWebBrowser1.Load(Properties.Settings.Default.SearchURL + urlLower);
                button1.Enabled = true;
            }
        }
        }
                


        private void button1_Click(object sender, EventArgs e)
        {
            chromiumWebBrowser1.Back(); 
        }

        private void button3_Click(object sender, EventArgs e)
        {
            chromiumWebBrowser1.Forward();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (isLoading)
            {
                chromiumWebBrowser1.Stop();
            }
            else { chromiumWebBrowser1.Reload(); }
        }
        private void cef_AddressChanged(object sender, AddressChangedEventArgs e)
        {
            this.InvokeOnUiThreadIfRequired(() => textBox1.Text = e.Address); 
            if (e.Address == "http://korot://error") { cef_onLoadError(null, null); }
            if (Properties.Settings.Default.Favorites.Contains(e.Address))
            {
                isLoadedPageFavroited = true;
                button7.Image = Properties.Resources.star_on;
            }
            else
            {
                if (Brightness(Properties.Settings.Default.BackColor) > 130) { button7.Image = Properties.Resources.star; }
                else { button7.Image = Properties.Resources.star_w; }
                isLoadedPageFavroited = false;
            }
        }
        private void cef_onLoadError(object sender, LoadErrorEventArgs e)
        {
            if (e == null) //User Asked
            {
                    chromiumWebBrowser1.LoadHtml(anaform.ErrorHTML
                        + "<a style=\"font - family: Modern, Arial; \"> TEST_ERROR </a></body>", "http://korot://error");
            }
            else
            {
                if (e.Frame.IsMain)
                {
                    chromiumWebBrowser1.LoadHtml(anaform.ErrorHTML +
                    "<a style=\"font - family: Modern, Arial; \">" + e.ErrorText + "</a></body>", "http://korot://error");
                }
                else
                {
                    e.Frame.LoadHtml(anaform.ErrorHTML +
                        "<a style=\"font - family: Modern, Arial; \">" + e.ErrorText + "</a></body>");
                }
            }
        }
      
        
        private void cef_TitleChanged(object sender, TitleChangedEventArgs e)
        {
            if (e.Title.Length < 101)
            {
                this.InvokeOnUiThreadIfRequired(() => this.Text = e.Title);
            }else
            {
                this.InvokeOnUiThreadIfRequired(() => this.Text = e.Title.Substring(0,100));
            }
            this.Parent.Invoke(new Action(() => this.Parent.Text = this.Text));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            chromiumWebBrowser1.Load(Korot.Properties.Settings.Default.Homepage);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                button4_Click(null, null);
            }
        }

        private void tabform_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.BrowserBack)
            {
                button1_Click(null, null);
            }
            else if (e.KeyData == Keys.BrowserForward)
            {
                button3_Click(null, null);
            }
            else if (e.KeyData == Keys.BrowserRefresh)
            {
                button2_Click(null, null);
            }
            else if (e.KeyData == Keys.BrowserStop)
            {
                button2_Click(null, null);
            }
            else if (e.KeyData == Keys.BrowserHome)
            {
                button5_Click(null, null);
            }
            else if (e.KeyCode == Keys.F && e.Control)
            {
                panel3.Visible = !panel3.Visible;
                findTextBox.Text = "";
                chromiumWebBrowser1.StopFinding(true);
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private static int GerekiyorsaAzalt(int defaultint, int azaltma)
        {
            if (defaultint > azaltma) { return defaultint - 20; } else { return defaultint; }
        }
        private static int GerekiyorsaArttır(int defaultint, int arttırma, int sınır)
        {
            if (defaultint + arttırma > sınır) { return defaultint; } else { return defaultint + arttırma; }
        }
        void ChnageTheme()
        {
            if (Brightness(Properties.Settings.Default.BackColor) > 130) //Light
            {
                findTextBox.BackColor = Color.FromArgb(GerekiyorsaAzalt(Properties.Settings.Default.BackColor.R, 10), GerekiyorsaAzalt(Properties.Settings.Default.BackColor.G, 10), GerekiyorsaAzalt(Properties.Settings.Default.BackColor.B, 10));
                findTextBox.ForeColor = Color.Black;
                panel3.BackColor = Properties.Settings.Default.BackColor;
                panel3.ForeColor = Color.Black;
                pbProgress.BackColor = Properties.Settings.Default.OverlayColor;
                button9.Image = Properties.Resources.profiles;
                cmsProfiles.BackColor = Properties.Settings.Default.BackColor;
                cmsProfiles.ForeColor = Color.Black;
                foreach (ToolStripMenuItem x in cmsProfiles.Items) { x.BackColor = Properties.Settings.Default.BackColor; x.ForeColor = Color.Black; }
                button1.Image = Properties.Resources.leftarrow;
                button2.Image = Properties.Resources.refresh;
                button3.Image = Properties.Resources.rightarrow;
                button4.Image = Properties.Resources.go;
                button5.Image = Properties.Resources.home;
                textBox1.BackColor = Color.FromArgb(GerekiyorsaAzalt(Properties.Settings.Default.BackColor.R, 10), GerekiyorsaAzalt(Properties.Settings.Default.BackColor.G, 10), GerekiyorsaAzalt(Properties.Settings.Default.BackColor.B, 10));
                textBox1.ForeColor = Color.Black;
                this.BackColor = Properties.Settings.Default.BackColor;
                this.ForeColor = Color.Black;
                label2.BackColor = Properties.Settings.Default.BackColor;
                label2.ForeColor = Color.Black;

                textBox1.BackColor = Color.FromArgb(GerekiyorsaAzalt(Properties.Settings.Default.BackColor.R, 10), GerekiyorsaAzalt(Properties.Settings.Default.BackColor.G, 10), GerekiyorsaAzalt(Properties.Settings.Default.BackColor.B, 10));
                textBox1.ForeColor = Color.Black;
                button6.Image = Properties.Resources.prxy;
                contextMenuStrip2.BackColor = Properties.Settings.Default.BackColor;
                contextMenuStrip2.ForeColor = Color.Black;
                if (isLoadedPageFavroited) { button7.Image = Properties.Resources.star_on; } else { button7.Image = Properties.Resources.star; }
                mFavorites.BackColor = Color.FromArgb(GerekiyorsaAzalt(Properties.Settings.Default.BackColor.R, 10), GerekiyorsaAzalt(Properties.Settings.Default.BackColor.G, 10), GerekiyorsaAzalt(Properties.Settings.Default.BackColor.B, 10));
                mFavorites.ForeColor = Color.Black;

                button8.Image = Properties.Resources.ext;
                contextMenuStrip1.BackColor = Properties.Settings.Default.BackColor;
                contextMenuStrip1.ForeColor = Color.Black;
                foreach (ToolStripMenuItem x in contextMenuStrip1.Items)
                {
                    x.BackColor = Properties.Settings.Default.BackColor;
                    x.ForeColor = Color.Black;
                }

            }
            else //Dark
            {
                pbProgress.BackColor = Properties.Settings.Default.OverlayColor;
                contextMenuStrip1.BackColor = Properties.Settings.Default.BackColor;
                contextMenuStrip1.ForeColor = Color.White;
                foreach (ToolStripMenuItem x in contextMenuStrip1.Items)
                {
                    x.BackColor = Properties.Settings.Default.BackColor;
                    x.ForeColor = Color.White;
                }
                button8.Image = Properties.Resources.ext_w;
                button9.Image = Properties.Resources.profiles_w;
                cmsProfiles.BackColor = Properties.Settings.Default.BackColor;
                cmsProfiles.ForeColor = Color.White;
                foreach (ToolStripMenuItem x in cmsProfiles.Items) { x.BackColor = Properties.Settings.Default.BackColor; x.ForeColor = Color.White; }
                findTextBox.BackColor = Color.FromArgb(GerekiyorsaArttır(Properties.Settings.Default.BackColor.R, 10, 255), GerekiyorsaArttır(Properties.Settings.Default.BackColor.G, 10, 255), GerekiyorsaArttır(Properties.Settings.Default.BackColor.B, 10, 255));
                findTextBox.ForeColor = Color.White;
                panel3.BackColor = Properties.Settings.Default.BackColor;
                panel3.ForeColor = Color.White;
                button1.Image = Properties.Resources.leftarrow_w;
                button2.Image = Properties.Resources.refresh_w;
                button3.Image = Properties.Resources.rightarrow_w;
                button4.Image = Properties.Resources.go_w;
                button5.Image = Properties.Resources.home_w;
                textBox1.BackColor = Color.FromArgb(GerekiyorsaArttır(Properties.Settings.Default.BackColor.R, 10, 255), GerekiyorsaArttır(Properties.Settings.Default.BackColor.G, 10, 255), GerekiyorsaArttır(Properties.Settings.Default.BackColor.B, 10, 255));
                textBox1.ForeColor = Color.White;
                this.BackColor = Properties.Settings.Default.BackColor;
                this.ForeColor = Color.White;
                label2.BackColor = Properties.Settings.Default.BackColor;
                label2.ForeColor = Color.White;
                button6.Image = Properties.Resources.prxy_w;
                contextMenuStrip2.BackColor = Properties.Settings.Default.BackColor;
                contextMenuStrip2.ForeColor = Color.White;
                textBox1.BackColor = Color.FromArgb(GerekiyorsaArttır(Properties.Settings.Default.BackColor.R, 10, 255), GerekiyorsaArttır(Properties.Settings.Default.BackColor.G, 10, 255), GerekiyorsaArttır(Properties.Settings.Default.BackColor.B, 10, 255));
                textBox1.ForeColor = Color.White;

                if (isLoadedPageFavroited) { button7.Image = Properties.Resources.star_on; } else { button7.Image = Properties.Resources.star_w; }
                mFavorites.BackColor = Color.FromArgb(GerekiyorsaArttır(Properties.Settings.Default.BackColor.R, 10, 255), GerekiyorsaArttır(Properties.Settings.Default.BackColor.G, 10, 255), GerekiyorsaArttır(Properties.Settings.Default.BackColor.B, 10, 255));
                mFavorites.ForeColor = Color.White;
                
            }
        }
        int websiteprogress;
        public void ChangeProgress(int value)
        {
            websiteprogress = value;
            pbProgress.Width = (this.Width / 100) * websiteprogress;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            ChnageTheme();
            this.Parent.Text = this.Text;
            try
            {
                if (((TabControl)parentTabPage.Parent).TabPages.Contains(parentTabPage)) { } else { this.Close(); }
            }catch { this.Close(); }
        }

        private void TestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            chromiumWebBrowser1.Load(((ToolStripMenuItem)sender).Tag.ToString());
        }
        bool isLoadedPageFavroited = false;

        private void Button7_Click(object sender, EventArgs e)
        {
            if (isLoadedPageFavroited)
            {
                Properties.Settings.Default.Favorites = Properties.Settings.Default.Favorites.Replace(chromiumWebBrowser1.Address + ";", "");
                Properties.Settings.Default.Favorites = Properties.Settings.Default.Favorites.Replace(this.Text + ";", "");
                button7.Image = Brightness(Properties.Settings.Default.BackColor) > 130 ? Properties.Resources.star : Properties.Resources.star_w;
                isLoadedPageFavroited = false;
            }
            else
            {
                Properties.Settings.Default.Favorites += (chromiumWebBrowser1.Address + ";");
                Properties.Settings.Default.Favorites += (this.Text + ";");
                button7.Image = Properties.Resources.star_on;
                isLoadedPageFavroited = true;
            }
            RefreshFavorites();
        }
        void RefreshProfiles()
        {
            switchToToolStripMenuItem.DropDownItems.Clear();
            foreach (string x in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Korot\\Users\\"))
            {
                ToolStripMenuItem profileItem = new ToolStripMenuItem();
                profileItem.Text = new DirectoryInfo(x).Name;
                profileItem.Click += ProfilesToolStripMenuItem_Click;
                switchToToolStripMenuItem.DropDownItems.Add(profileItem);
            }
        }


        private void ProfilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            anaform.Invoke(new Action(() => anaform.SwitchProfile(((ToolStripMenuItem)sender).Text)));
        }

        private void NewProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            anaform.Invoke(new Action(() => anaform.NewProfile()));
        }

        private void DeleteThisProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            anaform.Invoke(new Action(() => anaform.DeleteProfile(userName)));
        }

        private void Button9_Click(object sender, EventArgs e)
        {
            cmsProfiles.Show(MousePosition);
            this.Text = "Test";
        }

        private void ExtensionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmExt formext = new frmExt(this, anaform, userName, ((ToolStripMenuItem)sender).Tag.ToString());
            formext.Show();
        }
        public void LoadExt()
        {
            contextMenuStrip1.Items.Clear();
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Korot\\Users\\" + userName + "\\Extensions\\")) { Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Korot\\Users\\" + userName + "\\Extensions\\"); }

            foreach (string x in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Korot\\Users\\" + userName + "\\Extensions\\"))
            {
                if (File.Exists(x + "\\ext.kem"))
                {
                    ToolStripMenuItem extItem = new ToolStripMenuItem();
                    extItem.Text = new DirectoryInfo(x).Name;
                    if (!File.Exists(x + "\\icon.png"))
                    {
                        if (Brightness(Properties.Settings.Default.BackColor) > 130) { extItem.Image = Properties.Resources.ext; }
                        else { extItem.Image = Properties.Resources.ext_w; }
                    }
                    else
                    {
                        extItem.Image = Image.FromFile(x + "\\icon.png");
                    }
                    ToolStripMenuItem extRunItem = new ToolStripMenuItem();
                    extItem.Click += ExtensionToolStripMenuItem_Click;
                    extItem.Tag = x + "\\ext.kem";
                    contextMenuStrip1.Items.Add(extItem);

                }
            }
            if (contextMenuStrip1.Items.Count == 0)
            {
                ToolStripMenuItem emptylol = new ToolStripMenuItem();
                emptylol.Text = anaform.empty;
                emptylol.Enabled = false;
                contextMenuStrip1.Items.Add(emptylol);
            }
        }
        public void LoadProxies()
        {
            contextMenuStrip2.Items.Clear();
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Korot\\Users\\" + userName + "\\Proxy\\")) { Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Korot\\Users\\" + userName + "\\Proxy\\"); }
            // add default proxy
            ToolStripMenuItem defaultProxyItem = new ToolStripMenuItem();
            defaultProxyItem.Text = anaform.defaultproxytext;
            defaultProxyItem.Click += ExampleProxyToolStripMenuItem_Click;
            defaultProxyItem.Tag = defaultproxyaddress;
            contextMenuStrip2.Items.Add(defaultProxyItem);
            foreach (string x in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Korot\\Users\\" + userName + "\\Proxy\\"))
            {
                if (File.Exists(x + "\\proxy.kem"))
                {
                    ToolStripMenuItem extItem = new ToolStripMenuItem();
                    extItem.Text = new DirectoryInfo(x).Name;             
                    extItem.Click += ExampleProxyToolStripMenuItem_Click;
                    extItem.Tag = File.ReadAllText(x + "\\proxy.kem");
                    contextMenuStrip2.Items.Add(extItem);

                }
            }
        }
        private void Button8_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show(MousePosition);
        }

        private void TmrSlower_Tick(object sender, EventArgs e)
        {
            RefreshFavorites();
        }


        private void FrmCEF_SizeChanged(object sender, EventArgs e)
        {
            pbProgress.Width = (this.Width / 100) * websiteprogress;
        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {
            if ((!string.IsNullOrEmpty(findTextBox.Text)) & panel3.Visible)
            {
                chromiumWebBrowser1.Find(0, findTextBox.Text, false, haltroySwitch1.Checked, false);
            }
        }

        private void Label4_Click(object sender, EventArgs e)
        {
            panel3.Visible = false;
            findTextBox.Text = "";
            chromiumWebBrowser1.StopFinding(true);
        }

        private void Panel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Panel1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            tabform_KeyDown(panel1, new KeyEventArgs(e.KeyData));
        }

        private void ExampleProxyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                SetProxy(chromiumWebBrowser1, ((ToolStripMenuItem)sender).Tag.ToString());
            }
            catch { }
            chromiumWebBrowser1.Reload();
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            contextMenuStrip2.Show(MousePosition);
        }
    }
}
