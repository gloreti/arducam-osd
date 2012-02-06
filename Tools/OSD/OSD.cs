﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.IO;
using ArdupilotMega;
using System.Xml;

namespace OSD
{
    public partial class OSD : Form
    {
        //max 7456 datasheet pg 10
        //pal  = 16r 30 char
        //ntsc = 13r 30 char
        Size basesize = new Size(30, 16);
        /// <summary>
        /// the un-scaled font render image
        /// </summary>
        Bitmap screen = new Bitmap(30 * 12, 16 * 18);
        /// <summary>
        /// the scaled to size background control
        /// </summary>
        Bitmap image = new Bitmap(30 * 12, 16 * 18);
        /// <summary>
        /// Bitmaps of all the chars created from the mcm
        /// </summary>
        Bitmap[] chars;
        /// <summary>
        /// record of what panel is using what squares
        /// </summary>
        string[][] used = new string[30][];
        /// <summary>
        /// used to track currently selected panel across calls
        /// </summary>
        string currentlyselected = "";
        /// <summary>
        /// used to track current processing panel across calls (because i maintained the original code for panel drawing)
        /// </summary>
        string processingpanel = "";
        /// <summary>
        /// use to draw the red outline box is currentlyselected matchs
        /// </summary>
        bool selectedrectangle = false;
        /// <summary>
        /// use to as a invalidator
        /// </summary>
        bool startup = false;
        /// <summary>
        /// 328 eeprom memory
        /// </summary>
        byte[] eeprom = new byte[1024];
        /// <summary>
        /// background image
        /// </summary>
        Image bgpicture;

        SerialPort comPort = new SerialPort();

        Panels pan;

        Tuple<string, Func<int, int, int>, int, int, int, int, int>[] items = new Tuple<string, Func<int, int, int>, int, int, int, int, int>[30];

        Graphics gr;

        // in pixels
        int x = 0, y = 0;

        public OSD()
        {
            InitializeComponent();

            // load default font
            chars = mcm.readMCM("OSD_SA_v5.mcm");
            // load default bg picture
            try
            {
                bgpicture = Image.FromFile("vlcsnap-2012-01-28-07h46m04s95.png");
            }
            catch { }

            gr = Graphics.FromImage(screen);

            pan = new Panels(this);

            // setup all panel options
            setupFunctions();
        }

        void changeToPal(bool pal)
        {
            if (pal)
            {
                basesize = new Size(30, 16);

                screen = new Bitmap(30 * 12, 16 * 18);
                image = new Bitmap(30 * 12, 16 * 18);

                numericUpDown1.Maximum = 29;
                numericUpDown2.Maximum = 15;
            }
            else
            {
                basesize = new Size(30, 13);

                screen = new Bitmap(30 * 12, 13 * 18);
                image = new Bitmap(30 * 12, 13 * 18);

                numericUpDown1.Maximum = 29;
                numericUpDown2.Maximum = 15;
            }

            
        }

        void setupFunctions()
        {
            currentlyselected = "";
            processingpanel = "";

            int a = 0;

            for (a = 0; a < used.Length; a++)
            {
                used[a] = new string[16];
            }

            a = 0;

            // first 8
            // Display name,printfunction,X,Y,ENaddress,Xaddress,Yaddress
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Center", pan.panCenter, 13, 8, panCenter_en_ADDR, panCenter_x_ADDR, panCenter_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Pitch", pan.panPitch, 22, 9, panPitch_en_ADDR, panPitch_x_ADDR, panPitch_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Roll", pan.panRoll, 11, 1, panRoll_en_ADDR, panRoll_x_ADDR, panRoll_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Battery A", pan.panBatt_A, 22, 1, panBatt_A_en_ADDR, panBatt_A_x_ADDR, panBatt_A_y_ADDR);
            //items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Battery B", pan.panBatt_B, 22, 1, panBatt_B_en_ADDR, panBatt_B_x_ADDR, panBatt_B_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Visible Sats", pan.panGPSats, 2, 13, panGPSats_en_ADDR, panGPSats_x_ADDR, panGPSats_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("GPS Lock", pan.panGPL, 7, 13, panGPL_en_ADDR, panGPL_x_ADDR, panGPL_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("GPS Coord", pan.panGPS, 2, 14, panGPS_en_ADDR, panGPS_x_ADDR, panGPS_y_ADDR);

            //second 8
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Heading Rose", pan.panRose, 16, 14, panRose_en_ADDR, panRose_x_ADDR, panRose_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Heading", pan.panHeading, 24, 13, panHeading_en_ADDR, panHeading_x_ADDR, panHeading_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Heart Beat", pan.panMavBeat, 2, 9, panMavBeat_en_ADDR, panMavBeat_x_ADDR, panMavBeat_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Home Direction", pan.panHomeDir, 14, 3, panHomeDir_en_ADDR, panHomeDir_x_ADDR, panHomeDir_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Home Distance", pan.panHomeDis, 2, 1, panHomeDis_en_ADDR, panHomeDis_x_ADDR, panHomeDis_y_ADDR);
            //items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("WP Dir", pan.panWPDir, 14, 4, panWPDir_en_ADDR, panWPDir_x_ADDR, panWPDir_y_ADDR);
            //items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("WP Dir", pan.panWPDis, 14, 4, panWPDis_en_ADDR, panWPDis_x_ADDR, panWPDis_y_ADDR);
            // rssi

            // third 8
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Altitude", pan.panAlt, 2, 2, panAlt_en_ADDR, panAlt_x_ADDR, panAlt_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Velocity", pan.panVel, 2, 3, panVel_en_ADDR, panVel_x_ADDR, panVel_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Throttle", pan.panThr, 2, 4, panThr_en_ADDR, panThr_x_ADDR, panThr_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Flight Mode", pan.panFlightMode, 17, 13, panFMod_en_ADDR, panFMod_x_ADDR, panFMod_y_ADDR);
            items[a++] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>("Horizon", pan.panHorizon, 8, 7, panHorizon_en_ADDR, panHorizon_x_ADDR, panHorizon_y_ADDR);

            LIST_items.Items.Clear();

            startup = true;

            foreach (var thing in items)
            {
                if (thing != null)
                {
                    if (thing.Item1 == "Center")
                    {
                        LIST_items.Items.Add(thing.Item1, false);
                    }
                    else
                    {
                        LIST_items.Items.Add(thing.Item1, true);
                    }
                }
            }

            startup = false;

            osdDraw();
        }

        private string[] GetPortNames()
        {
            string[] devs = new string[0];

            if (Directory.Exists("/dev/"))
                devs = Directory.GetFiles("/dev/", "*ACM*");

            string[] ports = SerialPort.GetPortNames();

            string[] all = new string[devs.Length + ports.Length];

            devs.CopyTo(all, 0);
            ports.CopyTo(all, devs.Length);

            return all;
        }

        public void setPanel(int x, int y)
        {
            this.x = x * 12;
            this.y = y * 18;
        }

        public void openPanel()
        {
            d = 0;
            r = 0;
        }

        public void openSingle(int x, int y)
        {
            setPanel(x, y);
            openPanel();
        }

        public int getCenter()
        {
            if (CHK_pal.Checked)
                return 8;
            return 6;
        }

        // used for printf tracking line and row
        int d = 0, r = 0;

        public void printf(string format, params object[] args)
        {
            StringBuilder sb = new StringBuilder();

            sb = new StringBuilder(AT.MIN.Tools.sprintf(format, args));

            //sprintf(sb, format, __arglist(args));

            //Console.WriteLine(sb.ToString());

            foreach (char ch in sb.ToString().ToCharArray())
            {
                if (ch == '|')
                {
                    d += 1;
                    r = 0;
                    continue;
                }

                try
                {
                    // draw red boxs
                    if (selectedrectangle)
                    {
                        gr.DrawRectangle(Pens.Red, (this.x + r * 12), (this.y + d * 18), 12, 18);
                    }

                    int w1 = this.x / 12 + r;
                    int h1 = this.y / 18 + d;

                    if (w1 < basesize.Width && h1 < basesize.Height)
                    {
                        // check if this box has bene used
                        if (used[w1][h1] != null)
                        {
                            //System.Diagnostics.Debug.WriteLine("'" + used[this.x / 12 + r * 12 / 12][this.y / 18 + d * 18 / 18] + "'");
                        }
                        else
                        {
                            gr.DrawImage(chars[ch], (this.x + r * 12), (this.y + d * 18), 12, 18);
                        }

                        used[w1][h1] = processingpanel;
                    }
                }
                catch { System.Diagnostics.Debug.WriteLine("printf exception"); }
                r++;
            }
        }

        string getMouseOverItem(int x, int y)
        {
            int ansW,ansH;

            getCharLoc(x, y, out ansW, out ansH);

            if (used[ansW][ansH] != null && used[ansW][ansH] != "")
            {
                LIST_items.SelectedIndex = LIST_items.Items.IndexOf(used[ansW][ansH]);
                return used[ansW][ansH];
            }

            return "";
        }

        void getCharLoc(int x, int y,out int xpos, out int ypos)
        {

            x = Constrain(x, 0, pictureBox1.Width - 1);
            y = Constrain(y, 0, pictureBox1.Height - 1);

            float scaleW = pictureBox1.Width / (float)screen.Width;
            float scaleH = pictureBox1.Height / (float)screen.Height;

            int ansW = (int)((x / scaleW / 12) % 30);
            int ansH = 0;
            if (CHK_pal.Checked)
            {
                ansH = (int)((y / scaleH / 18) % 16);
            }
            else
            {
                ansH = (int)((y / scaleH / 18) % 13);
            }

            xpos = Constrain(ansW,0,30 -1);
            ypos = Constrain(ansH,0,16 - 1);
        }

        public void printf_P(string format, params object[] args)
        {
            printf(format, args);
        }

        public void closePanel()
        {
            x = 0;
            y = 0;
        }

        void osdDraw()
        {
            if (startup)
                return;

            for (int b = 0; b < used.Length; b++)
            {
                used[b] = new string[16];
            }

            image = new Bitmap(pictureBox1.Width, pictureBox1.Height);

            float scaleW = pictureBox1.Width / (float)screen.Width;
            float scaleH = pictureBox1.Height / (float)screen.Height;

            screen = new Bitmap(screen.Width, screen.Height);

            gr = Graphics.FromImage(screen);

            image = new Bitmap(image.Width, image.Height);

            Graphics grfull = Graphics.FromImage(image);

            try
            {
                grfull.DrawImage(bgpicture, 0, 0, pictureBox1.Width, pictureBox1.Height);
            }
            catch { }

            if (checkBox1.Checked)
            {
                for (int b = 1; b < 16; b++)
                {
                    for (int a = 1; a < 30; a++)
                    {
                        grfull.DrawLine(new Pen(Color.Gray, 1), a * 12 * scaleW, 0, a * 12 * scaleW, pictureBox1.Height);
                        grfull.DrawLine(new Pen(Color.Gray, 1), 0, b * 18 * scaleH, pictureBox1.Width, b * 18 * scaleH);
                    }
                }
            }

            pan.setHeadingPatern();
            pan.setBatteryPic();

            List<string> list = new List<string>();

            foreach (string it in LIST_items.CheckedItems)
            {
                list.Add(it);
            }

            list.Reverse();

            foreach (string it in list)
            {
                foreach (var thing in items)
                {
                    selectedrectangle = false;
                    if (thing != null)
                    {
                        if (thing.Item1 == it)
                        {
                            if (thing.Item1 == currentlyselected)
                            {
                                selectedrectangle = true;
                            }

                            processingpanel = thing.Item1;

                            // ntsc and below the middle line
                            if (thing.Item4 >= getCenter() && !CHK_pal.Checked)
                            {
                                thing.Item2(thing.Item3, thing.Item4 - 3);
                            }
                            else // pal and no change
                            {
                                thing.Item2(thing.Item3, thing.Item4);
                            }

                        }
                    }
                }
            }

            grfull.DrawImage(screen, 0, 0, image.Width, image.Height);

            pictureBox1.Image = image;
        }

        int Constrain(double value, double min, double max)
        {
            if (value < min)
                return (int)min;
            if (value > max)
                return (int)max;

            return (int)value;
        }

        private void OSD_Load(object sender, EventArgs e)
        {

            string strVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.Text = this.Text + " " + strVersion;

            comboBox1.Items.AddRange(GetPortNames());

            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;

            xmlconfig(false);

            osdDraw();
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string item = ((CheckedListBox)sender).SelectedItem.ToString();

            currentlyselected = item;

            osdDraw();

            foreach (var thing in items)
            {
                if (thing != null && thing.Item1 == item)
                {
                        numericUpDown1.Value = Constrain(thing.Item3,0,basesize.Width -1);
                        numericUpDown2.Value = Constrain(thing.Item4,0,16 -1);
                }
            }
        }

        private void checkedListBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (((CheckedListBox)sender).Text == "Horizon")
            {
                //groupBox1.Enabled = false;
            }
            else
            {
                //groupBox1.Enabled = true;
            }
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // if (((CheckedListBox)sender).SelectedItem != null && ((CheckedListBox)sender).SelectedItem.ToString() == "Horizon")
            if (((CheckedListBox)sender).SelectedItem != null)
            {
                if (((CheckedListBox)sender).SelectedItem.ToString() == "Horizon" && e.NewValue == CheckState.Checked)
                {
                    int index = LIST_items.Items.IndexOf("Center");
                    LIST_items.SetItemChecked(index, false);
                }
                else if (((CheckedListBox)sender).SelectedItem.ToString() == "Center" && e.NewValue == CheckState.Checked)
                {
                    int index = LIST_items.Items.IndexOf("Horizon");
                    LIST_items.SetItemChecked(index, false);
                }
            }

            // add a delay to this so it runs after the control value has been defined.
                if (this.IsHandleCreated)
                    this.BeginInvoke((MethodInvoker)delegate { osdDraw(); });
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            string item;
            try
            {
                item = LIST_items.SelectedItem.ToString();
            }
            catch { return; }

            for (int a = 0; a < items.Length; a++)
            {
                if (items[a] != null && items[a].Item1 == item)
                {
                    items[a] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>(items[a].Item1, items[a].Item2, (int)numericUpDown1.Value, items[a].Item4, items[a].Item5, items[a].Item6, items[a].Item7);
                }
            }

            osdDraw();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            string item;
            try
            {
                item = LIST_items.SelectedItem.ToString();
            }
            catch { return; }

            for (int a = 0; a < items.Length; a++)
            {
                if (items[a] != null && items[a].Item1 == item)
                {
                    items[a] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>(items[a].Item1, items[a].Item2, items[a].Item3, (int)numericUpDown2.Value, items[a].Item5, items[a].Item6, items[a].Item7);

                }
            }

            osdDraw();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (string str in this.LIST_items.Items)
            {
                foreach (var tuple in this.items)
                {
                    if ((tuple != null) && ((tuple.Item1 == str)) && tuple.Item5 != -1)
                    {
                        eeprom[tuple.Item5] = (byte)(this.LIST_items.CheckedItems.Contains(str) ? 1 : 0);
                        eeprom[tuple.Item6] = (byte)tuple.Item3; // x
                        eeprom[tuple.Item7] = (byte)tuple.Item4; // y

                        Console.WriteLine(str);
                    }
                }
            }


            ArduinoSTK sp;

            try
            {
                if (comPort.IsOpen)
                    comPort.Close();

                sp = new ArduinoSTK();
                sp.PortName = comboBox1.Text;
                sp.BaudRate = 57600;
                sp.DtrEnable = true;

                sp.Open();
            }
            catch { MessageBox.Show("Error opening com port"); return; }

            if (sp.connectAP())
            {
                if (sp.upload(eeprom, 0, 200, 0))
                {
                    MessageBox.Show("Done!");
                }
                else
                {
                    MessageBox.Show("Failed to upload new settings");
                }
            }
            else
            {
                MessageBox.Show("Failed to talk to bootloader");
            }

            sp.Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(GetPortNames());
        }



        /* *********************************************** */
        // EEPROM Storage addresses

        // First of 8 panels
        const int panCenter_en_ADDR = 0;
        const int panCenter_x_ADDR = 2;
        const int panCenter_y_ADDR = 4;
        const int panPitch_en_ADDR = 6;
        const int panPitch_x_ADDR = 8;
        const int panPitch_y_ADDR = 10;
        const int panRoll_en_ADDR = 12;
        const int panRoll_x_ADDR = 14;
        const int panRoll_y_ADDR = 16;
        const int panBatt_A_en_ADDR = 18;
        const int panBatt_A_x_ADDR = 20;
        const int panBatt_A_y_ADDR = 22;
        const int panBatt_B_en_ADDR = 24;
        const int panBatt_B_x_ADDR = 26;
        const int panBatt_B_y_ADDR = 28;
        const int panGPSats_en_ADDR = 30;
        const int panGPSats_x_ADDR = 32;
        const int panGPSats_y_ADDR = 34;
        const int panGPL_en_ADDR = 36;
        const int panGPL_x_ADDR = 38;
        const int panGPL_y_ADDR = 40;
        const int panGPS_en_ADDR = 42;
        const int panGPS_x_ADDR = 44;
        const int panGPS_y_ADDR = 46;

        // Second set of 8 panels
        const int panRose_en_ADDR = 48;
        const int panRose_x_ADDR = 50;
        const int panRose_y_ADDR = 52;
        const int panHeading_en_ADDR = 54;
        const int panHeading_x_ADDR = 56;
        const int panHeading_y_ADDR = 58;
        const int panMavBeat_en_ADDR = 60;
        const int panMavBeat_x_ADDR = 62;
        const int panMavBeat_y_ADDR = 64;
        const int panHomeDir_en_ADDR = 66;
        const int panHomeDir_x_ADDR = 68;
        const int panHomeDir_y_ADDR = 70;
        const int panHomeDis_en_ADDR = 72;
        const int panHomeDis_x_ADDR = 74;
        const int panHomeDis_y_ADDR = 76;
        const int panWPDir_en_ADDR = 80;
        const int panWPDir_x_ADDR = 82;
        const int panWPDir_y_ADDR = 84;
        const int panWPDis_en_ADDR = 86;
        const int panWPDis_x_ADDR = 88;
        const int panWPDis_y_ADDR = 90;
        const int panRSSI_en_ADDR = 92;
        const int panRSSI_x_ADDR = 94;
        const int panRSSI_y_ADDR = 96;


        // Third set of 8 panels
        const int panCurA_en_ADDR = 98;
        const int panCurA_x_ADDR = 100;
        const int panCurA_y_ADDR = 102;
        const int panCurB_en_ADDR = 104;
        const int panCurB_x_ADDR = 106;
        const int panCurB_y_ADDR = 108;
        const int panAlt_en_ADDR = 110;
        const int panAlt_x_ADDR = 112;
        const int panAlt_y_ADDR = 114;
        const int panVel_en_ADDR = 116;
        const int panVel_x_ADDR = 118;
        const int panVel_y_ADDR = 120;
        const int panThr_en_ADDR = 122;
        const int panThr_x_ADDR = 124;
        const int panThr_y_ADDR = 126;
        const int panFMod_en_ADDR = 128;
        const int panFMod_x_ADDR = 130;
        const int panFMod_y_ADDR = 132;
        const int panHorizon_en_ADDR = 134;
        const int panHorizon_x_ADDR = 136;
        const int panHorizon_y_ADDR = 138;

        const int CHK1 = 1000;
        const int CHK2 = 1006;

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            osdDraw();
        }

        private void OSD_Resize(object sender, EventArgs e)
        {
            try
            {
                osdDraw();
            }
            catch { }
        }

        private void button2_Click(object sender, EventArgs e)
        {

            ArduinoSTK sp;

            try
            {
                if (comPort.IsOpen)
                    comPort.Close();

                sp = new ArduinoSTK();
                sp.PortName = comboBox1.Text;
                sp.BaudRate = 57600;
                sp.DtrEnable = true;

                sp.Open();
            }
            catch { MessageBox.Show("Error opening com port"); return; }

            if (sp.connectAP())
            {
                eeprom = sp.download(1024);
            }
            else
            {
                MessageBox.Show("Failed to talk to bootloader");
            }

            sp.Close();

            for (int a = 0; a < items.Length; a++)
            {
                if (items[a] != null)
                {
                    if (items[a].Item5 >= 0)
                        LIST_items.SetItemCheckState(a, eeprom[items[a].Item5] == 0 ? CheckState.Unchecked : CheckState.Checked);

                    if (items[a].Item7 >= 0 || items[a].Item6 >= 0)
                        items[a] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>(items[a].Item1, items[a].Item2, eeprom[items[a].Item6], eeprom[items[a].Item7], items[a].Item5, items[a].Item6, items[a].Item7);
                }
            }

            MessageBox.Show("Done!");
        }


        byte[] readIntelHEXv2(StreamReader sr)
        {
            byte[] FLASH = new byte[1024 * 1024];

            int optionoffset = 0;
            int total = 0;
            bool hitend = false;

            while (!sr.EndOfStream)
            {
                toolStripProgressBar1.Value = (int)(((float)sr.BaseStream.Position / (float)sr.BaseStream.Length) * 100);

                string line = sr.ReadLine();

                if (line.StartsWith(":"))
                {
                    int length = Convert.ToInt32(line.Substring(1, 2), 16);
                    int address = Convert.ToInt32(line.Substring(3, 4), 16);
                    int option = Convert.ToInt32(line.Substring(7, 2), 16);
                    Console.WriteLine("len {0} add {1} opt {2}", length, address, option);

                    if (option == 0)
                    {
                        string data = line.Substring(9, length * 2);
                        for (int i = 0; i < length; i++)
                        {
                            byte byte1 = Convert.ToByte(data.Substring(i * 2, 2), 16);
                            FLASH[optionoffset + address] = byte1;
                            address++;
                            if ((optionoffset + address) > total)
                                total = optionoffset + address;
                        }
                    }
                    else if (option == 2)
                    {
                        optionoffset = (int)Convert.ToUInt16(line.Substring(9, 4), 16) << 4;
                    }
                    else if (option == 1)
                    {
                        hitend = true;
                    }
                    int checksum = Convert.ToInt32(line.Substring(line.Length - 2, 2), 16);

                    byte checksumact = 0;
                    for (int z = 0; z < ((line.Length - 1 - 2) / 2); z++) // minus 1 for : then mins 2 for checksum itself
                    {
                        checksumact += Convert.ToByte(line.Substring(z * 2 + 1, 2), 16);
                    }
                    checksumact = (byte)(0x100 - checksumact);

                    if (checksumact != checksum)
                    {
                        MessageBox.Show("The hex file loaded is invalid, please try again.");
                        throw new Exception("Checksum Failed - Invalid Hex");
                    }
                }
                //Regex regex = new Regex(@"^:(..)(....)(..)(.*)(..)$"); // length - address - option - data - checksum
            }

            if (!hitend)
            {
                MessageBox.Show("The hex file did no contain an end flag. aborting");
                throw new Exception("No end flag in file");
            }

            Array.Resize<byte>(ref FLASH, total);

            return FLASH;
        }

        void sp_Progress(int progress)
        {
            toolStripProgressBar1.Value = progress;
        }

        private void CHK_pal_CheckedChanged(object sender, EventArgs e)
        {
            changeToPal(CHK_pal.Checked);

            osdDraw();
        }

        private void pALToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            nTSCToolStripMenuItem.Checked = !CHK_pal.Checked;
        }

        private void nTSCToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            CHK_pal.Checked = !nTSCToolStripMenuItem.Checked;
        }

        private void saveToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog() { Filter = "*.osd|*.osd" };

            sfd.ShowDialog();

            if (sfd.FileName != "")
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(sfd.OpenFile()))
                    {

                        foreach (var item in items)
                        {
                            if (item != null)
                                sw.WriteLine("{0}\t{1}\t{2}", item.Item1, item.Item3, item.Item4);
                        }
                        sw.Close();
                    }
                }
                catch
                {
                    MessageBox.Show("Error writing file");
                }
            }
        }

        private void loadFromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog() { Filter = "*.osd|*.osd" };

            ofd.ShowDialog();

            if (ofd.FileName != "")
            {
                try
                {
                    using (StreamReader sr = new StreamReader(ofd.OpenFile()))
                    {
                        while (!sr.EndOfStream)
                        {
                            string[] strings = sr.ReadLine().Split(new char[] {'\t'},StringSplitOptions.RemoveEmptyEntries);

                            for (int a = 0; a < items.Length; a++)
                            {
                                if (items[a] != null && items[a].Item1 == strings[0])
                                {
                                    // incase there is an invalid line number or to shore
                                    try
                                    {
                                        items[a] = new Tuple<string, Func<int, int, int>, int, int, int, int, int>(items[a].Item1, items[a].Item2, int.Parse(strings[1]), int.Parse(strings[2]), items[a].Item5, items[a].Item6, items[a].Item7);
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("Error Reading file");
                }
            }

            osdDraw();
        }

        private void loadDefaultsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setupFunctions();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            getMouseOverItem(e.X, e.Y);
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                int ansW,ansH;
                getCharLoc(e.X, e.Y, out ansW, out ansH);
                if (ansH >= getCenter() && !CHK_pal.Checked)
                {
                    ansH += 3;
                }

                numericUpDown1.Value = Constrain(ansW, 0, basesize.Width -1);
                numericUpDown2.Value = Constrain(ansH, 0, 16 -1);

                pictureBox1.Focus();
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            currentlyselected = getMouseOverItem(e.X, e.Y);
        }

        private void updateFirmwareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "*.hex|*.hex";

            ofd.ShowDialog();

            if (ofd.FileName != "")
            {
                byte[] FLASH;
                try
                {
                    FLASH = readIntelHEXv2(new StreamReader(ofd.FileName));
                }
                catch { MessageBox.Show("Bad Hex File"); return; }

                ArduinoSTK sp;

                try
                {
                    if (comPort.IsOpen)
                        comPort.Close();

                    sp = new ArduinoSTK();
                    sp.PortName = comboBox1.Text;
                    sp.BaudRate = 57600;
                    sp.DtrEnable = true;

                    sp.Open();
                }
                catch { MessageBox.Show("Error opening com port"); return; }

                if (sp.connectAP())
                {
                    sp.Progress += new ProgressEventHandler(sp_Progress);

                    if (!sp.uploadflash(FLASH, 0, FLASH.Length, 0))
                    {
                        if (sp.IsOpen)
                            sp.Close();
                        MessageBox.Show("Upload failed. Lost sync. Try Arduino!!");
                    }
                }
                else
                {
                    MessageBox.Show("Failed to talk to bootloader");
                }

                sp.Close();

                MessageBox.Show("Done!");
            }
        }

        private void customBGPictureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "jpg or bmp|*.jpg;*.bmp";

            ofd.ShowDialog();

            if (ofd.FileName != "")
            {
                try
                {
                    bgpicture = Image.FromFile(ofd.FileName);

                }
                catch { MessageBox.Show("Bad Image"); }

                osdDraw();
            }
        }

        private void sendTLogToolStripMenuItem_Click(object sender, EventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Tlog|*.tlog";

            ofd.ShowDialog();

            if (ofd.FileName != "")
            {
                if (comPort.IsOpen)
                    comPort.Close();

                try
                {

                    comPort.PortName = comboBox1.Text;
                    comPort.BaudRate = 57600;

                    comPort.Open();

                }
                catch { MessageBox.Show("Error opening com port"); return; }

                BinaryReader br = new BinaryReader(ofd.OpenFile());

                while (br.BaseStream.Position < br.BaseStream.Length && !this.IsDisposed)
                {
                    try
                    {
                        byte[] bytes = br.ReadBytes(20);

                        comPort.Write(bytes, 0, bytes.Length);

                        System.Threading.Thread.Sleep(5);

                    }
                    catch { break; }

                    Application.DoEvents();
                }
            }
        }

        private void OSD_FormClosed(object sender, FormClosedEventArgs e)
        {
            xmlconfig(true);

        }

        private void xmlconfig(bool write)
        {
            if (write || !File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + @"config.xml"))
            {
                try
                {
                    XmlTextWriter xmlwriter = new XmlTextWriter(Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + @"config.xml", Encoding.ASCII);
                    xmlwriter.Formatting = Formatting.Indented;

                    xmlwriter.WriteStartDocument();

                    xmlwriter.WriteStartElement("Config");

                    xmlwriter.WriteElementString("comport", comboBox1.Text);


                    xmlwriter.WriteEndElement();

                    xmlwriter.WriteEndDocument();
                    xmlwriter.Close();

                    //appconfig.Save();
                }
                catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            }
            else
            {
                try
                {
                    using (XmlTextReader xmlreader = new XmlTextReader(Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + @"config.xml"))
                    {
                        while (xmlreader.Read())
                        {
                            xmlreader.MoveToElement();
                            try
                            {
                                switch (xmlreader.Name)
                                {
                                    case "comport":
                                        string temp = xmlreader.ReadString();
                                        comboBox1.Text = temp;
                                        break;
                                    case "Config":
                                        break;
                                    case "xml":
                                        break;
                                    default:
                                        if (xmlreader.Name == "") // line feeds
                                            break;
                                        break;
                                }
                            }
                            catch (Exception ee) { Console.WriteLine(ee.Message); } // silent fail on bad entry
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine("Bad Config File: " + ex.ToString()); } // bad config file
            }
        } 
    }
}