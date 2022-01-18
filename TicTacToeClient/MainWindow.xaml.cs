using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using TicTacToeClient.Helpers;

namespace TicTacToeClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Socket ClientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private const int PORT = 27001;
        public MainWindow()
        {
            InitializeComponent();
            getAllCameraList();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ConnectToServer();
            RequestLoop();
        }
        private void ConnectToServer()
        {
            int attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    // Change IPAddress.Loopback to a remote IP to connect to a remote host.
                    ClientSocket.Connect(IPAddress.Loopback, PORT);
                }
                catch (SocketException)
                {
                }
            }

            MessageBox.Show("Connected");

            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            this.Title = "Player : " + text;
            player.Text = text;

        }

        private void RequestLoop()
        {


            var receiver = Task.Run(() =>
            {

                while (true)
                {
                    ReceiveResponse();
                }
            });

        }





        /// <summary>
        /// Sends a string to the server with ASCII encoding.
        /// </summary>
        private void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private void SendImage(byte[] src)
        {
            ClientSocket.Send(src, 0, src.Length, SocketFlags.None);
        }

        private void ReceiveResponse()
        {
            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            IntegrateToView(text);
        }
        public void IntegrateToView(string text)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var data = text.Split('\n');
                var row1 = data[0].Split('\t');
                var row2 = data[1].Split('\t');
                var row3 = data[2].Split('\t');
                b1.Content = row1[0];
                b2.Content = row1[1];
                b3.Content = row1[2];
                b4.Content = row2[0];
                b5.Content = row2[1];
                b6.Content = row2[2];
                b7.Content = row3[0];
                b8.Content = row3[1];
                b9.Content = row3[2];
            });
        }
        private void b1_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {

                    var bt = sender as Button;
                    string request = bt.Content.ToString() + player.Text;
                    SendString(request);
                });
            });
        }
        public static byte[] imgToByteConverter(System.Windows.Controls.Image inImg)
        {
            ImageConverter imgCon = new ImageConverter();
            return (byte[])imgCon.ConvertTo(inImg, typeof(byte[]));
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                _videoDevices.Stop();
                App.Current.Dispatcher.Invoke(() =>
                {
                    SendImage(ImageBytes);
                });

            });
        }
        FilterInfoCollection _captureDevice;
        VideoCaptureDevice _videoDevices;
        private void getAllCameraList()
        {
            _captureDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            try
            {
                _videoDevices = new VideoCaptureDevice(_captureDevice[0].MonikerString);
                _videoDevices.NewFrame += _videoDevices_NewFrame;
                _videoDevices.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        public  byte[] ImageToByte(System.Drawing.Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
        public byte[] ImageBytes { get; set; }
        private void _videoDevices_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var _source = (Bitmap)eventArgs.Frame.Clone();
                img.Source = ImageSourceFromBitmap(_source);
                ImageBytes = ImageToByte(_source);
            });
        }
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);
        public static ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
    }
}
