using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using DirectShowLib;




namespace CameraEmguCV
{
    public partial class MainWindow : Window
    {
        private Capture capture;
        DispatcherTimer timer;
        private bool add_markers = false;
        private int num_of_clicks = 0;
        private  double rho = 0.5;
        private int lowThreshold = 30, highThreshold = 210, minLineLength=90, maxLineGap = 50, houghThreshold =50;
        private List<Point> markers = new List<Point>();
        private Point lastPoint = new Point();
        private System.Windows.Shapes.Ellipse lastEllipse = new System.Windows.Shapes.Ellipse();
        private List<System.Windows.Shapes.Ellipse> allEllipses = new List<System.Windows.Shapes.Ellipse>();
        private bool wasClick = false;
        private bool test_markers = false;
        private int num_of_clicks_test = 0;
        private List<Point> markers_test = new List<Point>();
        private bool selection = false;
       

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Camera Capture Functions
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DsDevice[] captureDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            
            for (int i = 0; i < captureDevices.Length; i++)
            {               
                cbxCameraDevices.Items.Add(captureDevices[i].Name.ToString());
            }

            capture = new Capture(0);
            capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, image1.Width);
            capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, image1.Height);
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Image<Bgr, byte> currentFrame = capture.QueryFrame().ToImage<Bgr, byte>();

            if(num_of_clicks == 4)
            {
                Mat frame = currentFrame.Mat;
                frame = ImageProcessor.WarpPerspective(frame, Utils.GetPoints(markers));
                CvInvoke.Imwrite("warp_frame.jpg", frame);
                frame = CvInvoke.Imread("warp_frame.jpg", LoadImageType.Grayscale);
                
                
                Mat template = CvInvoke.Imread("template.jpg", LoadImageType.Grayscale);
                Mat mask = CvInvoke.Imread("template_mask.jpg", LoadImageType.Grayscale);
                bool found = ImageProcessor.MatchTemplate(frame, template, mask);
                lblFound.Content = found.ToString();
                //currentFrame = frame.ToImage<Bgr, byte>();
            }
            
            if (currentFrame != null)
                image1.Source = ToBitmapSource(currentFrame);
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap
                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap
                (
                  ptr,
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  BitmapSizeOptions.FromEmptyOptions()
                );

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }
        #endregion

        #region GUI Events
        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (add_markers == true && selection == true)
            {
                if (num_of_clicks_test < 4)
                {
                    System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                    ellipse.Stroke = new SolidColorBrush(Colors.Red);
                    ellipse.StrokeThickness = 3;
                    ellipse.Width = 5;
                    ellipse.Fill = new SolidColorBrush(Colors.Blue);
                   mainCanvas.Children.Add(ellipse);
                   // mainCanvas.Width = image1.ActualWidth;
                   // mainCanvas.Height = image1.ActualHeight;
                    //mainCanvas.Margin = image1.Margin;
                  //  mainCanvas.VerticalAlignment = image1.VerticalAlignment;
                    Point point = new Point(Mouse.GetPosition(this).X, Mouse.GetPosition(this).Y);
                    Canvas.SetLeft(ellipse, point.X);
                    Canvas.SetTop(ellipse, point.Y);
                    markers_test.Add(point);
                    num_of_clicks_test++;

                }

                if (num_of_clicks_test == 4)
                {
                     Image<Bgr, byte> currentFrame = capture.QueryFrame().ToImage<Bgr, byte>();
                    

                   // Image<Bgr, byte> currentFrame = new Image<Bgr, byte>();
                    currentFrame.Save("test_save_1.jpg");
                    Mat img = CvInvoke.Imread("test_save_1.jpg", Emgu.CV.CvEnum.LoadImageType.Color);
                    Mat warp = ImageProcessor.WarpPerspective(img, Utils.GetPoints(markers_test));
                    CvInvoke.Imwrite("warp_save_1.jpg", warp);
                    image3.Source = ToBitmapSource(warp.ToImage<Bgr, byte>());

                }

            }
                else if(add_markers)
            {
                if (num_of_clicks < 4)
                {
                    System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                    ellipse.Stroke = new SolidColorBrush(Colors.Orange);
                    ellipse.StrokeThickness = 2;
                    ellipse.Width = 5;
                    ellipse.Fill = new SolidColorBrush(Colors.Orange);
                    mainCanvas.Children.Add(ellipse);
                    
                    allEllipses.Add(ellipse);
                    lastEllipse = ellipse;

                    Point point = new Point(Mouse.GetPosition(image1).X, Mouse.GetPosition(image1).Y);
                    Canvas.SetLeft(ellipse, point.X);
                    Canvas.SetTop(ellipse, point.Y);
                    markers.Add(point);
                    lastPoint = point;
                    num_of_clicks++;
                    wasClick = false;
                }

                if (num_of_clicks == 4)
                {
                    Image<Bgr, byte> currentFrame = capture.QueryFrame().ToImage<Bgr, byte>();
                    currentFrame.Save("test_save.jpg");
                    Mat img = CvInvoke.Imread("test_save.jpg", Emgu.CV.CvEnum.LoadImageType.Color);
                    Mat warp = ImageProcessor.WarpPerspective(img, Utils.GetPoints(markers));
                    CvInvoke.Imwrite("warp_save.jpg", warp);
                    image2.Source = ToBitmapSource(warp.ToImage<Bgr, byte>());
                    selection = true;
                }
            }
        }

        private void SecondaryCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (test_markers == true && selection == true)
            {
                


            }

        }

        private void MainCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (add_markers)
            {

                if (wasClick == false)
                {
                        mainCanvas.Children.Remove(lastEllipse);
                    markers.Remove(lastPoint);

                    num_of_clicks--;
                    wasClick = true;
                }
            }

        }

        private void ResetAll()
        {
                foreach (System.Windows.Shapes.Ellipse c in allEllipses)
                {
                    mainCanvas.Children.Remove(c);
                }
                markers.Clear();
                num_of_clicks = 0;
                wasClick = false;
        }



            private void BtnShowImage_Click(object sender, RoutedEventArgs e)
        {
            //Image<Bgr, byte> currentFrame = capture.QueryFrame().ToImage<Bgr, byte>();
            //currentFrame.Save("frame.jpg");
            //Mat img = CvInvoke.Imread("frame.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            //Mat warp = WarpPerspective(img, Utils.GetPoints(markers));
            //CvInvoke.Imwrite("warp_frame.jpg", warp);
            //Mat template = CvInvoke.Imread("template.jpg", LoadImageType.Grayscale);
            //Mat mask = CvInvoke.Imread("template_mask.jpg", LoadImageType.Grayscale);
            //bool found = MatchTemplate(warp, template, mask);
            //MessageBox.Show("Template found = " + found.ToString());


            Mat img = CvInvoke.Imread("warp_save.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            Mat mask = CvInvoke.Imread("template_mask.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            Mat result = ImageProcessor.ApplyMask(img, mask);
            CvInvoke.Imwrite("template.jpg", result);
            image2.Source = ToBitmapSource(result.ToImage<Bgr, byte>());
        }

        private void BtnWarp_Click(object sender, RoutedEventArgs e)
        {
            Mat img = CvInvoke.Imread("test_save.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);

            if (markers.Count == 4)
                img = ImageProcessor.WarpPerspective(img, Utils.GetPoints(markers));
            img = ImageProcessor.ApplyBlur(img, 0, 3);
            img = ImageProcessor.CannyEdgeDetection(img, lowThreshold, highThreshold);
            image2.Source = ToBitmapSource(img.ToImage<Bgr, byte>());

        }

        private void BtnHough_Click(object sender, RoutedEventArgs e)
        {
            Mat img = CvInvoke.Imread("test_save.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            if (markers.Count == 4)
                img = ImageProcessor.WarpPerspective(img, Utils.GetPoints(markers));
            img = ImageProcessor.ApplyBlur(img, 0, 3);

            img = ImageProcessor.CannyEdgeDetection(img, lowThreshold, highThreshold);
            img = ImageProcessor.ApplyDilation(img, 3);
            img = ImageProcessor.ApplyErosion(img, 3);

            LineSegment2D[] lines = ImageProcessor.HoughLines(img, rho, houghThreshold, minLineLength, maxLineGap);
            
            Mat output = ImageProcessor.AddLines(lines, Utils.GetRatioOfSelectedArea(Utils.GetPoints(markers)));
            CvInvoke.Imwrite("template_mask.jpg", output);
            image2.Source = ToBitmapSource(output.ToImage<Bgr, byte>());
            
        }

        private void BtnAddMarkers_Click(object sender, RoutedEventArgs e)
        {
            if (add_markers == false)
            {
                add_markers = true;
                btnAddMarkers.Background = Brushes.Pink;
            }
            else
            {
               // ResetAll();
                add_markers = false;
                btnAddMarkers.Background = Brushes.LightGray;


            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetAll();
        }

        private void BtnSingleLines_Click(object sender, RoutedEventArgs e)
        {
            Mat img = CvInvoke.Imread("test_save.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            if (markers.Count == 4)
                img = ImageProcessor.WarpPerspective(img, Utils.GetPoints(markers));
            img = ImageProcessor.ApplyBlur(img, 0, 3);
            img = ImageProcessor.CannyEdgeDetection(img, lowThreshold, highThreshold);
            img = ImageProcessor.ApplyDilation(img, 3);
            img = ImageProcessor.ApplyErosion(img, 3);

            LineSegment2D[] lines = ImageProcessor.HoughLines(img, rho, houghThreshold, minLineLength, maxLineGap);

            LineSegment2D[] singleLines = Utils.GetSingleLinesFromHoughLines(lines, 20);//KMeans(lines, 5);
            Mat output = ImageProcessor.AddLines(singleLines, Utils.GetRatioOfSelectedArea(Utils.GetPoints(markers)));
            CvInvoke.Imwrite("template_mask.jpg", output);
            image2.Source = ToBitmapSource(output.ToImage<Bgr, byte>());
        }

        private void SlRho_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            rho = slRho.Value;
        }

        private void SlLowThresh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lowThreshold = (int)slLowThresh.Value;
        }

        private void SlHighThresh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            highThreshold = (int)slHighThresh.Value;
        }

        private void CbxCameraDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            capture = new Capture(cbxCameraDevices.SelectedIndex);
        }

        private void SlMinLineLength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            minLineLength = (int)slMinLineLength.Value;
        }

        private void SlMaxLineGap_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            maxLineGap = (int)slMaxLineGap.Value;
        }

        private void SlHoughThresh_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            houghThreshold = (int)slHoughThresh.Value;
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            if (test_markers == false)
            {
                test_markers = true;
                btnTest.Background = Brushes.Pink;
            }
            else
            {
                test_markers = false;
                btnTest.Background = Brushes.LightGray;
            }
        }
    }
    #endregion

}