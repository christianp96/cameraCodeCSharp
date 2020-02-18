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
        private bool add_markers = false;
        private int num_of_clicks = 0;
        List<Point> markers = new List<Point>();
        Point lastPoint = new Point();
        System.Windows.Shapes.Ellipse lastEllipse = new System.Windows.Shapes.Ellipse();
        List<System.Windows.Shapes.Ellipse> allEllipses = new List<System.Windows.Shapes.Ellipse>();
        bool wasClick = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Camera Capture Functions
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            capture = new Capture();
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

        #region Image Processing Functions
        private Mat WarpPerspective(Mat image, System.Drawing.PointF[] points)
        {
            double r = GetRatioOfSelectedArea(GetPoints(markers));//1.298;
            //MessageBox.Show("r = " + r.ToString());
            System.Drawing.PointF[] squared_points = new System.Drawing.PointF[4];
            squared_points[0] = new System.Drawing.PointF(0, 0);
            squared_points[1] = new System.Drawing.PointF(200, 0);
            squared_points[2] = new System.Drawing.PointF(0, (int)(200 / r));
            squared_points[3] = new System.Drawing.PointF(200, (int)(200 / r));

            Mat M = CvInvoke.GetPerspectiveTransform(points, squared_points);
            Mat screen = new Mat(M.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            CvInvoke.WarpPerspective(image, screen, M, new System.Drawing.Size(200, (int)(200 / r)));

            return screen;
        }

        private Mat ApplyBlur(Mat img, int stdev, int kernelSize)
        {
            Mat blurred = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            CvInvoke.GaussianBlur(img, blurred, new System.Drawing.Size(kernelSize, kernelSize), stdev);

            return blurred;
        }

        private Mat CannyEdgeDetection(Mat img, int lowThreshold, int highThreshold)
        {
            Mat canny = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);

            CvInvoke.Canny(img, canny, lowThreshold, highThreshold);

            return canny;
        }

        private LineSegment2D[] HoughLines(Mat img, double rho, int threshold, int minLineLength, int maxLineGap)
        {
            LineSegment2D[] houghLines = new LineSegment2D[100];

            //int threshold = 30, minLineLength = 15, maxLineGap = 3;
            double theta = Math.PI / 180;//rho = 0.1;
            houghLines = CvInvoke.HoughLinesP(img, rho, theta, threshold, minLineLength, maxLineGap);
            return houghLines;
        }

        private Mat AddLines(LineSegment2D[] lines, double ratio)
        {
            //double r = 1.298;
            Mat output = new Mat(new System.Drawing.Size(200, (int)(200 / ratio)), Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            output.SetTo(new MCvScalar(0));

            MCvScalar color = new MCvScalar(0, 0, 255);
            int thickness = 2;
            foreach (LineSegment2D line in lines)
            {
                System.Drawing.Point p1 = line.P1;
                System.Drawing.Point p2 = line.P2;
                CvInvoke.Line(output, p1, p2, color, thickness);
            }
            return output;
        }

        private Mat ApplyDilation(Mat img, int iterations)
        {
            Mat output = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            var kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Cross, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
            CvInvoke.Erode(img, output, kernel, new System.Drawing.Point(-1, -1), iterations, Emgu.CV.CvEnum.BorderType.Default, default(MCvScalar));

            return output;
        }

        private Mat ApplyErosion(Mat img, int iterations)
        {
            Mat output = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            var kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
            CvInvoke.Dilate(img, output, kernel, new System.Drawing.Point(-1, -1), iterations, Emgu.CV.CvEnum.BorderType.Default, default(MCvScalar));

            return output;
        }
        #endregion

        #region GUI Events
        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (add_markers)
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
                    Mat warp = WarpPerspective(img, GetPoints(markers));
                    CvInvoke.Imwrite("warp_save.jpg", warp);
                    image2.Source = ToBitmapSource(warp.ToImage<Bgr, byte>());
                }
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
            if (add_markers)
            {

                foreach (System.Windows.Shapes.Ellipse c in allEllipses)
                {
                    mainCanvas.Children.Remove(c);
                }
                markers.Clear();
                num_of_clicks = 0;
                wasClick = false;
            }

        }



            private void BtnShowImage_Click(object sender, RoutedEventArgs e)
        {
            Mat img = CvInvoke.Imread("4.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            image2.Source = ToBitmapSource(img.ToImage<Bgr, byte>());
        }

        private void BtnWarp_Click(object sender, RoutedEventArgs e)
        {
            Mat img = CvInvoke.Imread("test_save.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);

            if (markers.Count == 4)
            {
                image2.Source = ToBitmapSource(WarpPerspective(img, GetPoints(markers)).ToImage<Bgr, byte>());

            }
        }

        private void BtnHough_Click(object sender, RoutedEventArgs e)
        {
            Mat img = CvInvoke.Imread("test_save.jpg", Emgu.CV.CvEnum.LoadImageType.Color);
            if (markers.Count == 4)
                img = WarpPerspective(img, GetPoints(markers));
            img = ApplyBlur(img, 0, 3);
            img = CannyEdgeDetection(img, 30, 250);
            LineSegment2D[] lines = HoughLines(img, 1, 20, 15, 3);

            LineSegment2D[] singleLines = KMeans(lines, 5);
            Mat output = AddLines(lines, GetRatioOfSelectedArea(GetPoints(markers)));
            image2.Source = ToBitmapSource(output.ToImage<Bgr, byte>());
        }

        private void BtnAddMarkers_Click(object sender, RoutedEventArgs e)
        {
            if (add_markers == false)
            {
                add_markers = true;
                btnAddMarkers.Background = Brushes.Red;
            }
            else
            {
                ResetAll();
                add_markers = false;
                btnAddMarkers.Background = Brushes.LightGray;


            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetAll();
        }
        #endregion

        #region Helpers
        private System.Drawing.PointF[] GetPoints(List<Point> points)
        {
            System.Drawing.PointF[] pointFs = new System.Drawing.PointF[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                pointFs[i] = new System.Drawing.PointF((float)points[i].X, (float)points[i].Y);
            }

            return pointFs;

        }

        private double GetMaxWidthOfSelectedArea(System.Drawing.PointF[] points)
        {
            return Math.Min(Math.Abs(points[0].X - points[1].X), Math.Abs(points[2].X - points[3].X));
        }

        private double GetMaxHeightOfSelectedArea(System.Drawing.PointF[] points)
        {
            return Math.Min(Math.Abs(points[0].Y - points[2].Y), Math.Abs(points[1].Y - points[3].Y));
        }

        private double GetRatioOfSelectedArea(System.Drawing.PointF[] points)
        {
            return GetMaxWidthOfSelectedArea(points) / GetMaxHeightOfSelectedArea(points);
        }

        private Matrix<float> ConvertLineSegmentArrayToMatrix(LineSegment2D[] lines)
        {
            Matrix<float> convertedLines = new Matrix<float>(lines.Length, 4, 3);

            for (int i = 0; i < lines.Length; i++)
            {
                convertedLines[i, 0] = lines[i].P1.X;
                convertedLines[i, 1] = lines[i].P1.Y;
                convertedLines[i, 2] = lines[i].P2.X;
                convertedLines[i, 3] = lines[i].P2.Y;
            }

            return convertedLines;
        }

        private LineSegment2D[] ConvertMatrixOfLineSegmentsToArray(Matrix<float> lines)
        {
            LineSegment2D[] convertedLines = new LineSegment2D[lines.Rows];

            for (int i = 0; i < lines.Rows; i++)
            {
                System.Drawing.Point p1 = new System.Drawing.Point((int)lines[i, 0], (int)lines[i, 1]);
                System.Drawing.Point p2 = new System.Drawing.Point((int)lines[i, 2], (int)lines[i, 3]);
                convertedLines[i] = new LineSegment2D(p1, p2);
            }

            return convertedLines;
        }

        private LineSegment2D[] GetOneLineFromDuplicate(LineSegment2D[] lines, Matrix<int> labels, int k)
        {
            LineSegment2D[] singleLines = new LineSegment2D[k];
            List<int> existingLabels = new List<int>();
            int count = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (!existingLabels.Contains(labels[i, 0]))
                {
                    existingLabels.Add(labels[i, 0]);
                    singleLines[count++] = lines[i];
                }

            }

            return singleLines;
        }


        private LineSegment2D[] KMeans(LineSegment2D[] lines, int k)
        {

            Matrix<float> lines_matrix = ConvertLineSegmentArrayToMatrix(lines);
            Matrix<int> retLabels = new Matrix<int>(lines.Length, 1);
            MCvTermCriteria criteria = new MCvTermCriteria(1, 2);
            CvInvoke.Kmeans(lines_matrix, k, retLabels, criteria, 10, Emgu.CV.CvEnum.KMeansInitType.RandomCenters);

            return GetOneLineFromDuplicate(lines, retLabels, k);
        }





        #endregion

       
    }
}