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
using System.Collections.Generic;

namespace CameraEmguCV
{
    public partial class MainWindow : Window
    {
        private Capture capture;
        DispatcherTimer timer;
        private Point first_corner = new Point();
        //private Point second_corner = new Point();
        private bool add_markers = false;
        private bool set_markers_done = false;
        private int num_of_clicks = 0;
        List<Point> markers = new List<Point>();
        Mat roi = null;
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
                

               if (set_markers_done)
                {
                    int x = (int)markers[0].X;
                    int y = (int)markers[0].Y;

                    int w = (int)Math.Abs(markers[0].X - markers[2].X);
                    int h = (int)Math.Abs(markers[0].Y - markers[2].Y);
                    //currentFrame.ROI = new System.Drawing.Rectangle(x,y,w,h);
                    if (roi == null)
                        roi = currentFrame.Mat;
                }
                    
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

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            first_corner = Mouse.GetPosition(mainCanvas);
            if (add_markers)
            {
               if (num_of_clicks <4)
                {
                    System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                    ellipse.Stroke = new SolidColorBrush(Colors.Orange);
                    ellipse.StrokeThickness = 2;
                    ellipse.Width = 5;
                    ellipse.Fill = new SolidColorBrush(Colors.Orange);
                    mainCanvas.Children.Add(ellipse);
                    Point point = new Point(Mouse.GetPosition(mainCanvas).X, Mouse.GetPosition(mainCanvas).Y);
                    Canvas.SetLeft(ellipse, point.X);
                    Canvas.SetTop(ellipse, point.Y);
                    markers.Add(point);
                    num_of_clicks++;
                    
                }
                if (num_of_clicks == 4)
                    set_markers_done = true;
            }
        }

        private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            /*second_corner = Mouse.GetPosition(mainCanvas);
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
            second_corner = new Point();*/
            

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
            Mat screen = new Mat(M.Size,Emgu.CV.CvEnum.DepthType.Cv8U,3);
            CvInvoke.WarpPerspective(image, screen, M, new System.Drawing.Size(200, (int)(200 / r)));

            return screen;
        }

        private Mat ApplyBlur(Mat img)
        {
            int stdev = 0, kernelSize = 3;
            Mat blurred = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            CvInvoke.GaussianBlur(img, blurred, new System.Drawing.Size(kernelSize, kernelSize), stdev);

            return blurred;
        }

        private Mat CannyEdgeDetection(Mat img)
        {
            int lowThreshold = 30, highThreshold = 200;
            Mat canny = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            
            CvInvoke.Canny(img, canny, lowThreshold, highThreshold);
           
            return canny;
        }


        private LineSegment2D[] HoughLines(Mat img)
        {
            LineSegment2D[] houghLines = new LineSegment2D[100];
         
            int threshold = 30, minLineLength = 15, maxLineGap = 3; 
            double rho = 0.1, theta = Math.PI / 180;
            houghLines = CvInvoke.HoughLinesP(img, rho, theta, threshold, minLineLength, maxLineGap);
            return houghLines;
        }

        private Mat AddLines(LineSegment2D[] lines)
        {
            double r = 1.298;
            Mat output = new Mat(new System.Drawing.Size(200,(int)(200/r)),Emgu.CV.CvEnum.DepthType.Cv8U,3);
            output.SetTo(new MCvScalar(0));

            //output.
            MCvScalar color = new MCvScalar(0, 0, 255);
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


            //MessageBox.Show(roi.Bitmap.GetPixel((int)markers[0].X,(int) markers[0].Y).ToString());

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
           LineSegment2D[] lines = HoughLines(img);
            Mat output = AddLines(lines);
            image2.Source = ToBitmapSource(output.ToImage<Bgr, byte>());
        }

        private void BtnAddMarkers_Click(object sender, RoutedEventArgs e)
        {
            add_markers = true;
        }
    }
}
