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
using Emgu.CV.CvEnum;

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
            capture = new Capture(1);
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

            //if(num_of_clicks == 4)
            //{
            //    Mat frame = currentFrame.Mat;
            //    frame = WarpPerspective(frame, GetPoints(markers));
            //    currentFrame = frame.ToImage<Bgr, byte>();
            //}
            
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
            double r = GetRatioOfSelectedArea(points);//1.298;
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
            CvInvoke.Canny(img, canny, lowThreshold, highThreshold,3);
  
            return canny;
        }

        private LineSegment2D[] HoughLines(Mat img, double rho, int threshold, int minLineLength, int maxLineGap)
        {
            LineSegment2D[] houghLines = new LineSegment2D[100];

            //int threshold = 30, minLineLength = 15, maxLineGap = 3;
            double theta = Math.PI / 180;//rho = 0.1;
            houghLines = CvInvoke.HoughLinesP(img, rho, theta, threshold, minLineLength, maxLineGap);
            //CvInvoke.Imwrite("hough.jpg", img);
            return houghLines;
        }

        private Mat AddLines(LineSegment2D[] lines, double ratio)
        {
            //double r = 1.298;
            Mat output = new Mat(new System.Drawing.Size(200, (int)(200 / ratio)), Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            output.SetTo(new MCvScalar(0));

            MCvScalar color = new MCvScalar(255, 255, 255);
                int thickness = 1;
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
            Mat output = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            var kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Cross, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
            CvInvoke.Dilate(img, output, kernel, new System.Drawing.Point(-1, -1), iterations, Emgu.CV.CvEnum.BorderType.Default, default(MCvScalar));

            return output;
        }

        private Mat ApplyMask(Mat img, Mat mask)
        {
            Mat output = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            output.SetTo(new MCvScalar(0));
            CvInvoke.Threshold(mask, mask, 127, 255, ThresholdType.Binary);
            
            CvInvoke.BitwiseAnd(img, img, output, mask);
            return output;
        }

        private Mat ApplyErosion(Mat img, int iterations)
        {
            Mat output = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            var kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
            CvInvoke.Erode(img, output, kernel, new System.Drawing.Point(-1, -1), iterations, Emgu.CV.CvEnum.BorderType.Default, default(MCvScalar));

            return output;
        }

        private bool MatchTemplate(Mat img, Mat template, Mat mask)
        {
            bool found = false;
            double min_val = 0 , max_val = 0,threshold = 10;
            System.Drawing.Point min_loc = new System.Drawing.Point(), max_loc = new System.Drawing.Point();
            Mat output = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            output.SetTo(new MCvScalar(0));

            CvInvoke.Threshold(mask, mask, 127, 255, ThresholdType.Binary);
            CvInvoke.Resize(mask, mask, template.Size);
            CvInvoke.MatchTemplate(img, template, output, TemplateMatchingType.Sqdiff, mask: mask);
            CvInvoke.MinMaxLoc(output,ref min_val, ref max_val, ref min_loc,ref max_loc);

            if (max_val < threshold)
                found = true; 


            return found;
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
                    Mat warp = WarpPerspective(img, GetPoints(markers_test));
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
                    Mat warp = WarpPerspective(img, GetPoints(markers));
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
            Image<Bgr, byte> currentFrame = capture.QueryFrame().ToImage<Bgr, byte>();
            currentFrame.Save("frame.jpg");
            Mat img = CvInvoke.Imread("frame.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            Mat warp = WarpPerspective(img, GetPoints(markers));
            CvInvoke.Imwrite("warp_frame.jpg", warp);
            Mat template = CvInvoke.Imread("template.jpg", LoadImageType.Grayscale);
            Mat mask = CvInvoke.Imread("template_mask.jpg", LoadImageType.Grayscale);
            bool found = MatchTemplate(warp, template, mask);
            MessageBox.Show("Template found = " + found.ToString());


            //Mat img = CvInvoke.Imread("warp_save.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            //Mat mask = CvInvoke.Imread("template_mask.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            //Mat result = ApplyMask(img, mask);
            //CvInvoke.Imwrite("template.jpg", result);
            //image2.Source = ToBitmapSource(result.ToImage<Bgr, byte>());
        }

        private void BtnWarp_Click(object sender, RoutedEventArgs e)
        {
            Mat img = CvInvoke.Imread("test_save.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);

            if (markers.Count == 4)
                img = WarpPerspective(img, GetPoints(markers));
            img = ApplyBlur(img, 0, 3);
            img = CannyEdgeDetection(img, lowThreshold, highThreshold);
            image2.Source = ToBitmapSource(img.ToImage<Bgr, byte>());

        }

        private void BtnHough_Click(object sender, RoutedEventArgs e)
        {
            Mat img = CvInvoke.Imread("test_save.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            if (markers.Count == 4)
                img = WarpPerspective(img, GetPoints(markers));
            img = ApplyBlur(img, 0, 3);

            img = CannyEdgeDetection(img, lowThreshold, highThreshold);
            img = ApplyDilation(img, 3);
            img = ApplyErosion(img, 3);

            LineSegment2D[] lines = HoughLines(img, rho, houghThreshold, minLineLength, maxLineGap);
            
            Mat output = AddLines(lines, GetRatioOfSelectedArea(GetPoints(markers)));
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
                img = WarpPerspective(img, GetPoints(markers));
            img = ApplyBlur(img, 0, 3);
            img = CannyEdgeDetection(img, lowThreshold, highThreshold);
            img = ApplyDilation(img, 3);
            img = ApplyErosion(img, 3);

            LineSegment2D[] lines = HoughLines(img, rho, houghThreshold, minLineLength, maxLineGap);

            LineSegment2D[] singleLines = GetSingleLinesFromHoughLines(lines, 20);//KMeans(lines, 5);
            Mat output = AddLines(singleLines, GetRatioOfSelectedArea(GetPoints(markers)));
            CvInvoke.Imwrite("tempalte_mask.jpg", output);
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

        private LineSegment2D[] GetSingleLinesFromHoughLines(LineSegment2D[] lines, double threshold)
        {
            LineSegment2D[] singleLines = new LineSegment2D[lines.Length];

            lines = SortSegmentsByYIntercept(lines);
            int count = 0;
            singleLines[0] = lines[0];

            for (int i = 0; i < lines.Length; i++)
            {
                double y_intercept = GetYIntercept(lines[i]);
                if (Math.Abs(GetYIntercept(singleLines[count]) - y_intercept) >= threshold)
                    singleLines[++count] = lines[i];
                else if (lines[i].Length >= singleLines[count].Length)
                    singleLines[count] = lines[i];
            }

            return singleLines;
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
            lines = SortLineSegmentsByLength(lines);
            lines = ReverseArray(lines);
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

        private LineSegment2D[] ReverseArray(LineSegment2D[] lines)
        {
            for(int i=0; i<lines.Length/2;i++)
            {
                LineSegment2D aux = lines[i];
                lines[i] = lines[lines.Length - i-1];
                lines[lines.Length - i-1] = aux;
            }

            return lines;
        }

        private LineSegment2D[] KMeans(LineSegment2D[] lines, int k)
        {

            Matrix<float> lines_matrix = ConvertLineSegmentArrayToMatrix(lines);
            Matrix<int> retLabels = new Matrix<int>(lines.Length,1);

            MCvTermCriteria criteria = new MCvTermCriteria(10,1.0);
            criteria.Type = Emgu.CV.CvEnum.TermCritType.Eps | Emgu.CV.CvEnum.TermCritType.Iter;
            
           
            if (lines.Length >= k)
            {
                CvInvoke.Kmeans(lines_matrix, k, retLabels, criteria, 10, Emgu.CV.CvEnum.KMeansInitType.RandomCenters);
                return GetOneLineFromDuplicate(lines, retLabels, k);
            }
                
            else
                return lines;
        }
        
        private LineSegment2D[] SortLineSegmentsByLength(LineSegment2D[] lines)
        {
            
            int i, j;
            LineSegment2D key;

            for (i = 1; i < lines.Length; i++)
            {
                key = lines[i];
                j = i - 1;

                while (j >= 0 && lines[j].Length > key.Length)
                {
                    lines[j + 1] = lines[j];
                    j = j - 1;
                }
                lines[j + 1] = key;
            }

            return lines;

            
        }


        private double GetYIntercept(LineSegment2D line)
        {
            double a = line.P2.Y - line.P1.Y;
            double b = line.P1.X - line.P2.X;
            double c = a * line.P1.X + b * line.P1.Y;

            if (b == 0)
                return double.PositiveInfinity;
            else
                return c / b;
        }

        private LineSegment2D[] SortSegmentsByYIntercept(LineSegment2D[] lines)
        {
            int i, j;
            LineSegment2D key;

            for (i = 1; i < lines.Length; i++)
            {
                key = lines[i];
                j = i - 1;

                while (j >= 0 && GetYIntercept(lines[j]) > GetYIntercept(key))
                {
                    lines[j + 1] = lines[j];
                    j = j - 1;
                }
                lines[j + 1] = key;
            }

            return lines;

        }


        #endregion

       

        public static Mat ToMat(BitmapSource source)
        {
            if (source.Format == PixelFormats.Bgr32)
            {
                Mat result = new Mat();
                result.Create(source.PixelHeight, source.PixelWidth, DepthType.Cv8U, 4);
                source.CopyPixels(Int32Rect.Empty, result.DataPointer, result.Step * result.Rows, result.Step);
                return result;
            }
            else
                return new Mat();
        }
    }
}