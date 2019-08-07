using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PacketDotNet;
using SharpPcap;
using System.Windows.Forms.DataVisualization.Charting;

namespace MyPacketCapturer
{
    public partial class frmCapture : Form
    {
        CaptureDeviceList devices; // List of devices for this computer
        public static ICaptureDevice device; // The device we will be using
        public static string stringPackets = ""; // Data that is captured
        static int numPackets = 0;
        static int numOfARPPackets = 0;
        static int numOfIPV6Packets = 0;
        static int numOfUDPPackets = 0;
        static int numOfTCPPackets = 0;
        static int numOfOtherPackets = 0;
        static bool isClassified = false;
        static bool isIPPacket = false;
        public static string seriesname = "MySeriesName";
        public static string be = "";
        frmSend fSend; // send form

        public frmCapture()
        {
            InitializeComponent();
           

            // Get the list of devices
            devices = CaptureDeviceList.Instance;

            // Make sure there is atleast on device
            if(devices.Count < 1)
            {
                MessageBox.Show("No Capture Devices Found!!!");
                Application.Exit();
            }

            // Add the devices to the combo box
            foreach (ICaptureDevice dev in devices)
            {
                cmbDevices.Items.Add(dev.Description);
                
            }

            // Get the chosen device and display in combo box
            device = devices[0];
            cmbDevices.Text = device.Description;

            // Register our handler function to the 'packet arrival' event
            device.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(device_OnPacketArrival);

            //OPen the device for capturing
            int readTimeoutMilliseconds = 1000;
            device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);

            drawChart();

        }

        private static void device_OnPacketArrival (object sender, CaptureEventArgs packet)
        {
            // reset boolean
            isClassified = false;
            isIPPacket = false;

            // increment the number of packets captured
            numPackets++;

            //Put the packet number in the caputure window
            stringPackets += "Packet Number: " + Convert.ToString(numPackets);
            stringPackets += Environment.NewLine;

            //Array to store our data
            byte[] data = packet.Packet.Data;

            //Keep track of the number of bytes displayed per line
            int byteCounter = 0;

            stringPackets += "Destination MAC Address: ";
            // Parsing the packets
            foreach (byte b in data)
            {
                if(byteCounter < 14)
                {
                    stringPackets += b.ToString("X2") + " ";
                }
                byteCounter++;

                switch (byteCounter)
                {
                    case 6:
                        stringPackets += Environment.NewLine;
                        stringPackets += "Source Mac Address: ";
                        break;
                    case 12:
                        stringPackets += Environment.NewLine;
                        stringPackets += "EtherType: ";
                        break;
                    case 14:
                        if (data[12] == 8)
                        {
                            if (data[13] == 0)
                            {
                                stringPackets += "(IP)";
                                isIPPacket = true;
             
                            }
                            if (data[13] == 6)
                            {
                                stringPackets += "(ARP)";
                                numOfARPPackets++;
                                isClassified = true;
                            }
                        }

                        if (data[12] == 134)
                        {
                            stringPackets += "(IPV6)";
                            isClassified = true;
                            numOfIPV6Packets++;
                        }


                        if (isClassified == false && isIPPacket == false)
                        {
                            stringPackets += "(Other)";
                            numOfOtherPackets++;
                        }

                        break;
                    case 23:
                        if (data[23] == 17)
                        {
                            stringPackets += "(UDP)";
                            numOfUDPPackets++;
                            isClassified = true;

                        }
                        if (data[23] == 6)
                        {
                            stringPackets += "(TCP)";
                            numOfTCPPackets++;
                            isClassified = true;

                        }
                        break;

                }

            }

            stringPackets += Environment.NewLine + Environment.NewLine;
            byteCounter = 0;
            stringPackets += "Raw Data" + Environment.NewLine;
            // process each byte in our captured packet
            foreach (byte b in data)
            {
                // add the byte to our string (in hexadecimal)
                stringPackets += b.ToString("X2") + " ";
                byteCounter++;

                // start new line after 16 bytes
                if(byteCounter == 16)
                {
                    byteCounter = 0;
                    stringPackets += Environment.NewLine;
                }

            }

            stringPackets += Environment.NewLine;
            stringPackets += Environment.NewLine;
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            try
            {
                if(btnStartStop.Text == "Start")
                {
                    device.StartCapture();
                    timer1.Enabled = true;
                    btnStartStop.Text = "Stop";
                }
                else
                {
                    device.StopCapture();
                    timer1.Enabled = false;
                    btnStartStop.Text = "Start";
                }
            } catch (Exception ex)
            {

            }
        }

        // Dump the packet data from stringPackets to the text box
        private void timer1_Tick(object sender, EventArgs e)
        {
            txtCapturedData.AppendText(stringPackets);
            stringPackets = "";
            txtNumPackets.Text = Convert.ToString(numPackets);
            txtArpPackets.Text = Convert.ToString(numOfARPPackets);
            txtIPV6Packets.Text = Convert.ToString(numOfIPV6Packets);
            txtTCPPackets.Text = Convert.ToString(numOfTCPPackets);
            txtUDPPackets.Text = Convert.ToString(numOfUDPPackets);
            txtOtherPackets.Text = Convert.ToString(numOfOtherPackets);
            updateChart();
            //txtNumPackets.Text = numPackets + "";
        }

        private void cmbDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            device = devices[cmbDevices.SelectedIndex];
            cmbDevices.Text = device.Description;

            // Register our handler function to the 'packet arrival' event
            device.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(device_OnPacketArrival);

            //OPen the device for capturing
            int readTimeoutMilliseconds = 1000;
            device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Text Files|*.txt|ALL Files|*.*";
            saveFileDialog1.Title = "Save the Caputured Packets";
            saveFileDialog1.ShowDialog();

            // Check to see if a filename was given
            if (saveFileDialog1.FileName != "")
            {
                System.IO.File.WriteAllText(saveFileDialog1.FileName, txtCapturedData.Text);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Text Files|*.txt|ALL Files|*.*";
            openFileDialog1.Title = "Open Caputured Packets";
            openFileDialog1.ShowDialog();

            // Check to see if a filename was given
            if (openFileDialog1.FileName != "")
            {
                txtCapturedData.Text = System.IO.File.ReadAllText(openFileDialog1.FileName);
            }
        }

        private void sendWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (frmSend.instantiations == 0)
            {
                fSend = new frmSend(); // creates new send form
                fSend.Show();
            }

        }

        private void updateChart()
        {
            chart1.Series[seriesname].Points.Clear();
            if(numOfARPPackets > 0)
                chart1.Series[seriesname].Points.AddXY("ARP Packets", numOfARPPackets);

            if (numOfIPV6Packets > 0)
                chart1.Series[seriesname].Points.AddXY("IPV6 Packets", numOfIPV6Packets);

            if (numOfTCPPackets > 0)
                chart1.Series[seriesname].Points.AddXY("TCP Packets", numOfTCPPackets);

            if (numOfUDPPackets > 0)
                chart1.Series[seriesname].Points.AddXY("UDP Packets", numOfUDPPackets);

            if (numOfOtherPackets > 0)
                chart1.Series[seriesname].Points.AddXY("Other", numOfOtherPackets);
        }

        private void drawChart()
        {
            chart1.Series.Clear();
            chart1.Legends.Clear();

            chart1.Legends.Add("MyLegend");
            chart1.Legends[0].LegendStyle = LegendStyle.Table;
            chart1.Legends[0].Docking = Docking.Bottom;
            chart1.Legends[0].Alignment = StringAlignment.Center;
            chart1.Legends[0].Title = "Packet Breakdown";
            chart1.Legends[0].BorderColor = Color.Black;

            //Add a new chart-series
            chart1.Series.Add(seriesname);
            //set the chart-type to "Pie"
            chart1.Series[seriesname].ChartType = SeriesChartType.Pie;


        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtCapturedData.Clear();
            stringPackets = "";
            numPackets = 0;
            numOfARPPackets = 0;
            numOfIPV6Packets = 0;
            numOfUDPPackets = 0;
            numOfTCPPackets = 0;
            numOfOtherPackets = 0;
            chart1.Series[seriesname].Points.Clear();

        }


    }
}
