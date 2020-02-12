using Microsoft.Expression.Encoder.Devices;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CameraApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        public Collection<EncoderDevice> VideoDevices { get; set; }
        public Collection<EncoderDevice> AudioDevices { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            VideoDevices = EncoderDevices.FindDevices(EncoderDeviceType.Video);
            AudioDevices = EncoderDevices.FindDevices(EncoderDeviceType.Audio);
        }

        private void StartCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Display webcam video
                WebcamViewer.StartPreview();
            }
            catch (Microsoft.Expression.Encoder.SystemErrorException ex)
            {
                MessageBox.Show("Device is in use by another application");
            }
        }

        private void StopCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop the display of webcam video.
            WebcamViewer.StopPreview();
        }

        private void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            // Start recording of webcam video to harddisk.
            WebcamViewer.StartRecording();
        }

        private void StopRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop recording of webcam video to harddisk.
            WebcamViewer.StopRecording();
        }

        private void TakeSnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            // Take snapshot of webcam video.
            WebcamViewer.TakeSnapshot();
        }

        private void AddMarkerButton_Click(object sender, RoutedEventArgs e)
        {
            Panel.SetZIndex(WebcamViewer,1);
            Rectangle rect = new Rectangle();
            rect.Width = 240;
            rect.Height = 140;
            rect.Stroke = new SolidColorBrush(Colors.AliceBlue);
            rect.StrokeThickness = 2;
            rect.Fill = new SolidColorBrush(Colors.Brown);
            mainCanvas.Children.Add(rect);
            Panel.SetZIndex(mainCanvas, 2);
            MessageBox.Show("Hi!");

            
            
        }

        private void MainCanvas_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MessageBox.Show("hooola!");
        }
    }
}
