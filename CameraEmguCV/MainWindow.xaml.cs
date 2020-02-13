using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Runtime.InteropServices;

namespace CameraEmguCV
{
    public partial class MainWindow : Window
    {
        private Capture capture;
        DispatcherTimer timer;
        private bool mousePressed;
        private Point first_corner = new Point();
        private Point second_corner = new Point();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            capture = new Capture();
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Image<Bgr, byte> currentFrame = capture.QueryFrame().ToImage<Bgr, byte>();

            if (currentFrame != null)
            {
                Image<Gray, byte> grayFrame = currentFrame.Convert<Gray, byte>();

                image1.Source = ToBitmapSource(currentFrame);
            }
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

        private void Image1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle rect = new Rectangle();
            rect.Height = 50;
            rect.Width = 50;
            rect.StrokeThickness = 2;
            rect.Stroke = new SolidColorBrush(Colors.AliceBlue);
            mainCanvas.Children.Add(rect);
            Canvas.SetLeft(rect,Mouse.GetPosition(mainCanvas).X);
            Canvas.SetTop(rect, Mouse.GetPosition(mainCanvas).Y);

        }

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePressed = true;
            first_corner = Mouse.GetPosition(mainCanvas);
           
        }

        private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            second_corner = Mouse.GetPosition(mainCanvas);
            Rectangle rect = new Rectangle();
            rect.Width = Math.Abs(second_corner.X - first_corner.Y);
            rect.Height = Math.Abs(first_corner.Y - second_corner.X);
            rect.StrokeThickness = 2;
            rect.Stroke = new SolidColorBrush(Colors.AliceBlue);
            mainCanvas.Children.Add(rect);

            Canvas.SetLeft(rect, first_corner.X);
            Canvas.SetTop(rect, first_corner.Y);
            Canvas.SetRight(rect, second_corner.X);
            Canvas.SetBottom(rect, second_corner.Y);
            Image<Bgr, byte> currentFrame = capture.QueryFrame().ToImage<Bgr, byte>();
            currentFrame.ROI = new System.Drawing.Rectangle(new System.Drawing.Point(250,300),new System.Drawing.Size(50,60));
            
            currentFrame.Save("test_save.jpg");
            first_corner = new Point();
            second_corner = new Point();
            

        }


        private Mat WarpPerspective(Mat image)
        {
            double r = 1.298;
            System.Drawing.PointF[] pts1 = new System.Drawing.PointF[4];
            pts1[0] = new System.Drawing.PointF(207, 38);
            pts1[1] = new System.Drawing.PointF(478, 27);
            pts1[2] = new System.Drawing.PointF(192, 241);
            pts1[3] = new System.Drawing.PointF(486, 248);

            System.Drawing.PointF[] pts2 = new System.Drawing.PointF[4];
            pts2[0] = new System.Drawing.PointF(0, 0);
            pts2[1] = new System.Drawing.PointF(200, 0);
            pts2[2] = new System.Drawing.PointF(0, (int)(200 / r));
            pts2[3] = new System.Drawing.PointF(200, (int)(200 / r));

            Mat M = CvInvoke.GetPerspectiveTransform(pts1, pts2);
            Mat screen = new Mat();
            CvInvoke.WarpPerspective(image, screen, M, new System.Drawing.Size(200, (int)(200 / r)));

            return screen;
        }

        private Mat ApplyBlur(Mat img)
        {
            int stdev = 0, kernelSize = 3;
            Mat blurred = new Mat();
            CvInvoke.GaussianBlur(img, blurred, new System.Drawing.Size(kernelSize, kernelSize), stdev);

            return blurred;
        }

        private Mat CannyEdgeDetection(Mat img)
        {
            int lowThreshold = 30, highThreshold = 200;
            Mat canny = new Mat();
            CvInvoke.Canny(img, canny, lowThreshold, highThreshold);
           
            return canny;
        }


        private LineSegment2D[] HoughLines(Mat img)
        {
            LineSegment2D[] houghLines = new LineSegment2D[100];
         
            int rho = 2, threshold = 30, minLineLength = 15, maxLineGap = 3; 
            double theta = Math.PI / 180;
            houghLines = CvInvoke.HoughLinesP(img, rho, theta, threshold, minLineLength, maxLineGap);
            return houghLines;
        }

        private Mat AddLines(LineSegment2D[] lines)
        {
            double r = 1.298;
            Mat output = new Mat(new System.Drawing.Size((int)(200/r),200),Emgu.CV.CvEnum.DepthType.Cv8U,3);
            //Mat output = new Mat();

            //output.
            MCvScalar color = new MCvScalar(255, 0, 0);
            int thickness = 2;
            foreach( LineSegment2D line in lines )
            {
                System.Drawing.Point p1 = line.P1;
                System.Drawing.Point p2 = line.P2;
                CvInvoke.Line(output, p1, p2, color, thickness);
            }


            return output;

        }

        private void BtnShowImage_Click(object sender, RoutedEventArgs e)
        {
            Mat img = CvInvoke.Imread("4.jpg",Emgu.CV.CvEnum.LoadImageType.Grayscale);
            image2.Source = ToBitmapSource(img.ToImage< Bgr, byte >());     
        }

        private void BtnWarp_Click(object sender, RoutedEventArgs e)
        {
            Mat img = CvInvoke.Imread("4.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            
            image2.Source = ToBitmapSource(WarpPerspective(img).ToImage<Bgr, byte>());
            
           
        }

        private void BtnHough_Click(object sender, RoutedEventArgs e)
        {

            Mat img = CvInvoke.Imread("4.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            img = WarpPerspective(img);
            img = ApplyBlur(img);
            img = CannyEdgeDetection(img);
           // LineSegment2D[] lines = HoughLines(img);
            //Mat output = AddLines(lines);
            image2.Source = ToBitmapSource(img.ToImage<Bgr, byte>());
        }
    }
}
