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
using System.Windows.Forms;
using System.Text;
using System.Runtime.Serialization.Json;

namespace CameraEmguCV
{
    public partial class MainWindow : Window
    {
        private Capture capture;
        DispatcherTimer timer;
        private bool add_markers = false;
        private int num_of_clicks = 0;
        public  List<Point> markers = new List<Point>();
        private Point lastPoint = new Point();
        private System.Windows.Shapes.Ellipse lastEllipse = new System.Windows.Shapes.Ellipse();
        private List<System.Windows.Shapes.Ellipse> allEllipses = new List<System.Windows.Shapes.Ellipse>();
        private bool wasClick = false;
        private int num_of_clicks_second_markers = 0;
        private List<Point> second_markers = new List<Point>();
        private bool selection = false;
        public Mat selectedScreen = null;
        DebugWindow debugWindow = null;
        CadranDefinition cadranDefinition = null;
        Screen currentScreen = new Screen("defaultScreenName");

     
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Camera Capture Functions
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateDevicesComboBox();    
            capture = new Capture(0);
            SetImageAndCanvasSize(capture.Height, capture.Width);
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
                //CvInvoke.Imwrite("warp_frame.jpg", frame);
                //frame = CvInvoke.Imread("warp_frame.jpg", LoadImageType.Grayscale);
                //Mat template = null, mask = null;
                //bool found = false;

                //try
                //{
                //    template = CvInvoke.Imread("template.jpg", LoadImageType.Grayscale);
                //    mask = CvInvoke.Imread("template_mask.jpg", LoadImageType.Grayscale);

                //} catch(Exception ex)
                //{
                //    MessageBox.Show("A handled exception just occurred: " + ex.Message, "Exception Sample", MessageBoxButton.OK, MessageBoxImage.Warning);
                //}

                //if (mask != null && template != null)
                //    found = ImageProcessor.MatchTemplate(frame, template, mask);
                //lblFound.Content = found.ToString();
                SetImageAndCanvasSize(frame.Height, frame.Width);
                //image1.Width = frame.Width;
                //image1.Height = frame.Height;
                //mainCanvas.Height = frame.Height;
                //mainCanvas.Width = frame.Width;

                currentFrame = frame.ToImage<Bgr, byte>(); 
            }
            
            if (currentFrame != null)
                image1.Source = Utils.ToBitmapSource(currentFrame);
        }

        private void PopulateDevicesComboBox()
        {
            DsDevice[] captureDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            for (int i = 0; i < captureDevices.Length; i++)
            {
                cbxCameraDevices.Items.Add(captureDevices[i].Name.ToString());
            }
        }
        #endregion

        #region GUI Events

        private void SetImageAndCanvasSize(double height, double width)
        {
            image1.Height = height;
            image1.Width = width;
            mainCanvas.Height = height;
            mainCanvas.Width = width;
        }
        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (add_markers == true && selection == true)
            {
                if (num_of_clicks_second_markers < 4)
                {
                    System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                    AddEllipse(ellipse);

                    Point point = new Point(Mouse.GetPosition(image1).X, Mouse.GetPosition(image1).Y);

                    Canvas.SetLeft(ellipse, point.X);
                    Canvas.SetTop(ellipse, point.Y);
                    
                    second_markers.Add(point);
                    num_of_clicks_second_markers++;

                }

                if (num_of_clicks_second_markers == 4)
                {

                    num_of_clicks_second_markers = 0;
                    TreeViewItem treeItemTest = new TreeViewItem();
                    Image<Bgr, byte> currentFrame = capture.QueryFrame().ToImage<Bgr, byte>();
                    Mat frame = currentFrame.Mat;
                    frame = ImageProcessor.WarpPerspective(frame, Utils.GetPoints(markers));
                    currentFrame = frame.ToImage<Bgr, byte>();
                    Mat img = currentFrame.Mat; 
                    CvInvoke.Resize(img, img, new System.Drawing.Size((int)image1.Width,(int) image1.Height));
                    Mat warp = ImageProcessor.WarpPerspective(img, Utils.GetPoints(second_markers));
                    //image3.Source = Utils.ToBitmapSource(warp.ToImage<Bgr, byte>());
                    ResetMarkers();
                    try { cadranDefinition = new CadranDefinition(); cadranDefinition.Owner = GetWindow(this); cadranDefinition.ShowDialog(); }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("A handled exception just occurred: " + ex.Message, "Exception Sample", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    //Doar de test, voi sterge dupa gasirea unei solutii mai bune
                    var item = (ComboBoxItem)cadranDefinition.CadranType.SelectedItem;
                    var content = (string)item.Content;
                   
                    //Adaugare in treeview
                    treeItemTest.Header = cadranDefinition.CadranName.Text;
                    treeItemTest.Items.Add(content);
                    tree.Items.Add(treeItemTest);

                    second_markers.Clear();
                }

            }
                else if(add_markers)
            {
                if (num_of_clicks < 4)
                {
                    System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();

                    AddEllipse(ellipse);

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
                    Mat img = currentFrame.Mat;
                    Mat warp = ImageProcessor.WarpPerspective(img, Utils.GetPoints(markers));
                    selectedScreen = warp;
                    //image2.Source = Utils.ToBitmapSource(warp.ToImage<Bgr, byte>());
                    ResetMarkers();
                    
                    selection = true;
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

        private void ResetMarkers()
        {
            foreach (System.Windows.Shapes.Ellipse c in allEllipses)
            {
                mainCanvas.Children.Remove(c);
            }
        }
        //Generarea elipselor pe canvas si adaugarea lor intr-o lista de elipse pentru stergerea lor ulterioara
        private void AddEllipse(System.Windows.Shapes.Ellipse ellipse)
        {
            ellipse.Stroke = new SolidColorBrush(Colors.Orange);
            ellipse.StrokeThickness = 2;
            ellipse.Width = 5;
            ellipse.Fill = new SolidColorBrush(Colors.Orange);
            mainCanvas.Children.Add(ellipse);

            allEllipses.Add(ellipse);
            lastEllipse = ellipse;
        }

        private void ResetAll()
        {
            ResetMarkers();
            markers.Clear();
            second_markers.Clear();
            num_of_clicks = 0;
            num_of_clicks_second_markers = 0;
            add_markers = false;
            selection = false;
            wasClick = false;
            btnAddMarkers.Background = Brushes.LightGray;
            SetImageAndCanvasSize(capture.Height, capture.Width);
            selectedScreen = null;
            tree.Items.Clear();
            
        }

        private void BtnShowImage_Click(object sender, RoutedEventArgs e)
        {
    
            //Mat img = null;
            //Mat mask = null;
            //Mat result = null;
            //try
            //{
            //    img = CvInvoke.Imread("warp_save.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            //    mask = CvInvoke.Imread("template_mask.jpg", Emgu.CV.CvEnum.LoadImageType.Grayscale);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("A handled exception just occurred: " + ex.Message, "Exception Sample", MessageBoxButton.OK, MessageBoxImage.Warning);
            //}

            //if (mask != null && img != null)
            //{
            //    result = ImageProcessor.ApplyMask(img, mask);
            //    CvInvoke.Imwrite("template.jpg", result);
            //    image2.Source = Utils.ToBitmapSource(result.ToImage<Bgr, byte>());
            //}
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
                add_markers = false;
                btnAddMarkers.Background = Brushes.LightGray;
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetAll();
        }

        private void btnDebugWindow_Click(object sender, RoutedEventArgs e)
        {
            try { debugWindow = new DebugWindow(); debugWindow.Owner = GetWindow(this); debugWindow.Show(); }
            catch (Exception ex) 
            {
                System.Windows.MessageBox.Show("A handled exception just occurred: " + ex.Message, "Exception Sample", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
        }

        private void CbxCameraDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            capture = new Capture(cbxCameraDevices.SelectedIndex);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Json|*.json";
            saveDialog.Title = "Save the Display Definition";
            saveDialog.ShowDialog();

            if(saveDialog.FileName !=  "")
            {
                System.IO.FileStream fs = (System.IO.FileStream)saveDialog.OpenFile();

                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(Screen));
                js.WriteObject(fs, currentScreen);

            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Title = "Load the Display Definition";
            openDialog.ShowDialog();
            openDialog.Filter = "Json|*.json";

            if (openDialog.FileName != "")
            {
                System.IO.FileStream fs = (System.IO.FileStream)openDialog.OpenFile();

                DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(Screen));
                currentScreen = (Screen)js.ReadObject(fs);
            }
        }


    }
    #endregion
}