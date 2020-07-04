using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CursovaSPZ
{
   // Програма,яка рахує коефіцієнт фрагментації файлу
    unsafe public partial class Form1 : System.Windows.Forms.Form
    {
        const uint FSCTL_GET_RETRIEVAL_POINTERS = 0x00090073;
        static string path ="";
        static long totalClusters = 0;

        [StructLayout(LayoutKind.Sequential)]
        struct STARTING_VCN_INPUT_BUFFER
        {
            public static readonly int Size;

            static STARTING_VCN_INPUT_BUFFER()
            {
                Size = Marshal.SizeOf(typeof(STARTING_VCN_INPUT_BUFFER));
            }

            public long StartingVcn;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RETRIEVAL_POINTERS_BUFFER
        {
            public static readonly int Size;

            static RETRIEVAL_POINTERS_BUFFER()
            {
                Size = Marshal.SizeOf(typeof(RETRIEVAL_POINTERS_BUFFER));
            }

            public int ExtentCount;
            public long StartingVcn;
            // Extents
            public long NextVcn;
            public long Lcn;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeviceIoControl(
            SafeFileHandle hFile,
            uint ioctl,
            void* In,
            int InSize,
            void* Out,
            int OutSize,
            int* BytesReturned,
            void* zero
            );
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            totalClusters = 0;
            int extentNumber = 1;
            label4.Text = "File fragmentation factor = ";


            if (path.Length == 0 || !File.Exists(path))
            {
                MessageBox.Show("Select a file!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            textBox1.Text += "Analysis...";

            using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var vcnIn = new STARTING_VCN_INPUT_BUFFER();
                var rpb = new RETRIEVAL_POINTERS_BUFFER();
                int bytesReturned = 0;
                int err = 0;
                vcnIn.StartingVcn = 0L;
                do
                {
                    DeviceIoControl(
                        file.SafeFileHandle, FSCTL_GET_RETRIEVAL_POINTERS,
                        &vcnIn, STARTING_VCN_INPUT_BUFFER.Size,
                        &rpb, RETRIEVAL_POINTERS_BUFFER.Size,
                        &bytesReturned, null
                        );

                    err = Marshal.GetLastWin32Error();

                    switch (err)
                    {
                        case 38: // ERROR_HANDLE_EOF

                            if (extentNumber == 1)
                                textBox1.Text += "MFT Table...";
                            else
                                textBox1.Text += $"\r\nTotal clusters: {totalClusters} \r\nFragments: {(extentNumber - 1)}";
                            break;
                        case 0: // NO_ERROR
                            textBox1.Text += $"\r\nFragment #{extentNumber}";
                            textBox1.Text += $"\r\n\tStart cluster: {rpb.Lcn}\r\n\tLength: {rpb.NextVcn - rpb.StartingVcn} clusters";
                            totalClusters += rpb.NextVcn - rpb.StartingVcn;
                            textBox1.Text += $"\r\nTotal clusters: {totalClusters}\r\nFragments:{extentNumber}";
                            break;
                        case 234: // ERROR_MORE_DATA
                            textBox1.Text += $"\r\nFragment #{extentNumber++}";
                            textBox1.Text += $"\r\n\tStart cluster: {rpb.Lcn}\r\n\tLength: {rpb.NextVcn - rpb.StartingVcn} clusters";
                            totalClusters += rpb.NextVcn - rpb.StartingVcn;
                            vcnIn.StartingVcn = rpb.NextVcn;
                            break;
                    }
                } while (err == 234);
            }
            if (extentNumber - 1 == 0)
            {
                textBox1.Text += $"\r\nFile fragmentation factor = 0%";
                label4.Text += $"0%";
            }
            else
            {
                double coeffragment = (double)(extentNumber - 1) / totalClusters * 100;
                textBox1.Text += $"\r\nFile fragmentation factor = {coeffragment:0.####} %";
                label4.Text += $"{coeffragment:0.######}%";
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            path = openFileDialog.FileName;
            label1.Text = Path.GetFileName(path);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            label1.Text = "";
            textBox1.Text = "";
            label4.Text = "File fragmentation factor = ";

        }
    }
}
