using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;

namespace CameraEmguCV
{
    class ImageProcessor
    {
        public static Mat WarpPerspective(Mat image, System.Drawing.PointF[] points)
        {
            double r = Utils.GetRatioOfSelectedArea(points);//1.298;
            //MessageBox.Show("r = " + r.ToString());
            System.Drawing.PointF[] squared_points = new System.Drawing.PointF[4];
            squared_points[0] = new System.Drawing.PointF(0, 0);
            squared_points[1] = new System.Drawing.PointF(400, 0);
            squared_points[2] = new System.Drawing.PointF(0, (int)(400 / r));
            squared_points[3] = new System.Drawing.PointF(400, (int)(400 / r));

            Mat M = CvInvoke.GetPerspectiveTransform(points, squared_points);
            Mat screen = new Mat(M.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            screen.SetTo(new MCvScalar(0));
            CvInvoke.WarpPerspective(image, screen, M, new System.Drawing.Size(400, (int)(400 / r)));

            return screen;
        }

        public static Mat ApplyBlur(Mat img, int stdev, int kernelSize)
        {
            Mat blurred = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            CvInvoke.GaussianBlur(img, blurred, new System.Drawing.Size(kernelSize, kernelSize), stdev);

            return blurred;
        }


        public static Mat CannyEdgeDetection(Mat img, int lowThreshold, int highThreshold)
        {
            Mat canny = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            CvInvoke.Canny(img, canny, lowThreshold, highThreshold, 3);

            return canny;
        }

        public static LineSegment2D[] HoughLines(Mat img, double rho, int threshold, int minLineLength, int maxLineGap)
        {
            LineSegment2D[] houghLines = new LineSegment2D[100];

            //int threshold = 30, minLineLength = 15, maxLineGap = 3;
            double theta = Math.PI / 180;//rho = 0.1;
            houghLines = CvInvoke.HoughLinesP(img, rho, theta, threshold, minLineLength, maxLineGap);
            //CvInvoke.Imwrite("hough.jpg", img);
            return houghLines;
        }

        public static Mat AddLines(LineSegment2D[] lines, double ratio)
        {
            //double r = 1.298;
            Mat output = new Mat(new System.Drawing.Size(400, (int)(400 / ratio)), Emgu.CV.CvEnum.DepthType.Cv8U, 3);
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

        public static Mat ApplyDilation(Mat img, int iterations)
        {
            Mat output = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            var kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Cross, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
            CvInvoke.Dilate(img, output, kernel, new System.Drawing.Point(-1, -1), iterations, Emgu.CV.CvEnum.BorderType.Default, default(MCvScalar));

            return output;
        }

        public static Mat ApplyMask(Mat img, Mat mask)
        {
            Mat output = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            output.SetTo(new MCvScalar(0));
            if (img.Height < mask.Height)
                CvInvoke.Resize(img, img, mask.Size);
            CvInvoke.Threshold(mask, mask, 127, 255, ThresholdType.Binary);

            CvInvoke.BitwiseAnd(img, img, output, mask);
            return output;
        }

        public static Mat ApplyErosion(Mat img, int iterations)
        {
            Mat output = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            var kernel = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
            CvInvoke.Erode(img, output, kernel, new System.Drawing.Point(-1, -1), iterations, Emgu.CV.CvEnum.BorderType.Default, default(MCvScalar));

            return output;
        }

        public static bool MatchTemplate(Mat img, Mat template, Mat mask)
        {
            bool found = false;
            double min_val = 0, max_val = 0, threshold = 10;
            System.Drawing.Point min_loc = new System.Drawing.Point(), max_loc = new System.Drawing.Point();
            Mat output = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            output.SetTo(new MCvScalar(0));

            CvInvoke.Threshold(mask, mask, 127, 255, ThresholdType.Binary);
            CvInvoke.Resize(mask, mask, template.Size);

            if (template.Height > img.Height)
                CvInvoke.Resize(img, img, template.Size);
            CvInvoke.MatchTemplate(img, template, output, TemplateMatchingType.Sqdiff, mask: mask);
            CvInvoke.MinMaxLoc(output, ref min_val, ref max_val, ref min_loc, ref max_loc);

            if (max_val < threshold)
                found = true;


            return found;
        }


    }
}
