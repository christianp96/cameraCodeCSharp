using System.Windows;
using Emgu.CV.Structure;
using Emgu.CV;
using System.ComponentModel;
using System;
using System.IO;

namespace CameraEmguCV
{
    /// <summary>
    /// Interaction logic for DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        public double rho = 0.4;
        public int lowThreshold = 0, highThreshold = 80, minLineLength = 100, maxLineGap = 80, houghThreshold = 75;
        MainWindow parent;
        TemplateImage t = TemplateImage.Instance;
        string templateDir = "template_dir";


        public DebugWindow()
        {
            InitializeComponent();
            this.parent = (MainWindow)Application.Current.MainWindow;
            InitImages();
        }


        private void InitImages()
        {
            Mat initMat = new Mat(new System.Drawing.Size((int)tempImage.Width, (int)tempImage.Height), Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            initMat.SetTo(new MCvScalar(128,128,128));
            tempImage.Source = Utils.ToBitmapSource(initMat.ToImage<Bgr,byte>());
            templateMask.Source = Utils.ToBitmapSource(initMat.ToImage<Bgr, byte>());
            //lblTemplateName.Visibility = Visibility.Hidden;
            //txtTemplateName.Visibility = Visibility.Hidden;
            btnSaveTemplate.Visibility = Visibility.Hidden;
        }


        private void BtnPreviewTemplate_Click(object sender, RoutedEventArgs e)
        {
            if(parent.currentScreen != null)
            {
                
                Mat img = parent.selectedScreen;//CvInvoke.Imread("test_save.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
                Mat warp = new Mat(img.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
                img.CopyTo(warp);
                img = ImageProcessor.ApplyBlur(img, 0, 3);

                img = ImageProcessor.CannyEdgeDetection(img, lowThreshold, highThreshold);
                img = ImageProcessor.ApplyDilation(img, 3);
                img = ImageProcessor.ApplyErosion(img, 3);

                LineSegment2D[] lines = ImageProcessor.HoughLines(img, rho, houghThreshold, minLineLength, maxLineGap);
                LineSegment2D[] singleLines = Utils.GetSingleLinesFromHoughLines(lines, 20);
                if (singleLines != null)
                {
                    Mat mask = ImageProcessor.AddLines(singleLines, Utils.GetRatioOfSelectedArea(Utils.GetPoints(parent.currentScreen.coordinates)));
                    Mat templateImage = ImageProcessor.ApplyMask(warp, mask);
                    t.SetTemplateImageAndMask(templateImage.ToImage<Bgr, byte>(), mask.ToImage<Bgr, byte>());
                    tempImage.Source = Utils.ToBitmapSource(templateImage);
                    templateMask.Source = Utils.ToBitmapSource(mask);
                    //lblTemplateName.Visibility = Visibility.Visible;
                    //txtTemplateName.Visibility = Visibility.Visible;
                    btnSaveTemplate.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Couldn't find any lines with the specified parameters");
                    InitImages();
                }
            }
            else
            {
                MessageBox.Show("You didn't select any screen! ", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
           
        }

        private void BtnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            parent.currentScreen.TemplateImage = t.Image.Bytes;
            parent.currentScreen.TemplateHeight = t.Image.Height;
            parent.currentScreen.TemplateWidth = t.Image.Width;
            parent.currentScreen.TemplateMask = t.Mask.Bytes;    
            InitImages();
          
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

    }
}
