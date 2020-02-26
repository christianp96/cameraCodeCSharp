using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using Emgu.CV.Structure;
using Emgu.CV;
using Emgu.CV.CvEnum;



namespace CameraEmguCV
{
    class Utils
    {
        public static System.Drawing.PointF[] GetPoints(List<Point> points)
        {
            System.Drawing.PointF[] pointFs = new System.Drawing.PointF[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                pointFs[i] = new System.Drawing.PointF((float)points[i].X, (float)points[i].Y);
            }

            return pointFs;

        }

        public static LineSegment2D[] GetSingleLinesFromHoughLines(LineSegment2D[] lines, double threshold)
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

        public static double GetMaxWidthOfSelectedArea(System.Drawing.PointF[] points)
        {
            return Math.Min(Math.Abs(points[0].X - points[1].X), Math.Abs(points[2].X - points[3].X));
        }

        public static double GetMaxHeightOfSelectedArea(System.Drawing.PointF[] points)
        {
            return Math.Min(Math.Abs(points[0].Y - points[2].Y), Math.Abs(points[1].Y - points[3].Y));
        }

        public static double GetRatioOfSelectedArea(System.Drawing.PointF[] points)
        {
            return GetMaxWidthOfSelectedArea(points) / GetMaxHeightOfSelectedArea(points);
        }

        public static Matrix<float> ConvertLineSegmentArrayToMatrix(LineSegment2D[] lines)
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


        public static LineSegment2D[] ConvertMatrixOfLineSegmentsToArray(Matrix<float> lines)
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



        public static LineSegment2D[] GetOneLineFromDuplicate(LineSegment2D[] lines, Matrix<int> labels, int k)
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

        public static LineSegment2D[] ReverseArray(LineSegment2D[] lines)
        {
            for (int i = 0; i < lines.Length / 2; i++)
            {
                LineSegment2D aux = lines[i];
                lines[i] = lines[lines.Length - i - 1];
                lines[lines.Length - i - 1] = aux;
            }

            return lines;
        }

        public static LineSegment2D[] KMeans(LineSegment2D[] lines, int k)
        {

            Matrix<float> lines_matrix = ConvertLineSegmentArrayToMatrix(lines);
            Matrix<int> retLabels = new Matrix<int>(lines.Length, 1);

            MCvTermCriteria criteria = new MCvTermCriteria(10, 1.0);
            criteria.Type = Emgu.CV.CvEnum.TermCritType.Eps | Emgu.CV.CvEnum.TermCritType.Iter;


            if (lines.Length >= k)
            {
                CvInvoke.Kmeans(lines_matrix, k, retLabels, criteria, 10, Emgu.CV.CvEnum.KMeansInitType.RandomCenters);
                return GetOneLineFromDuplicate(lines, retLabels, k);
            }

            else
                return lines;
        }

        public static LineSegment2D[] SortLineSegmentsByLength(LineSegment2D[] lines)
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


        public static double GetYIntercept(LineSegment2D line)
        {
            double a = line.P2.Y - line.P1.Y;
            double b = line.P1.X - line.P2.X;
            double c = a * line.P1.X + b * line.P1.Y;

            if (b == 0)
                return double.PositiveInfinity;
            else
                return c / b;
        }

        public static LineSegment2D[] SortSegmentsByYIntercept(LineSegment2D[] lines)
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
