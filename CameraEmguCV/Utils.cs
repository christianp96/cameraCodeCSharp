﻿using System;
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

            return SortPoints(pointFs);

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

        public static System.Drawing.PointF[] SortPoints(System.Drawing.PointF[] pts)
        {
            for (int i = 0; i < pts.Length - 1; i++)
                for (int j = i + 1; j < pts.Length; j++)

                    if (pts[i].Y > pts[j].Y)
                    {
                        System.Drawing.PointF aux = pts[i];
                        pts[i] = pts[j];
                        pts[j] = aux;
                    }
            for (int i = 0; i < pts.Length; i += 2)
            {
                if (pts[i].X > pts[i + 1].X)
                {
                    System.Drawing.PointF aux = pts[i];
                    pts[i] = pts[i + 1];
                    pts[i + 1] = aux;
                }
            }

            return pts;
        }

    }
}