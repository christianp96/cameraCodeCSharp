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
        public List<Point> markers = null;
        private Point lastPoint = new Point();
        private System.Windows.Shapes.Ellipse lastEllipse = new System.Windows.Shapes.Ellipse();
        private List<System.Windows.Shapes.Ellipse> allEllipses = new List<System.Windows.Shapes.Ellipse>();
        private bool wasClick = false;
        public Mat selectedScreen = null;
        private Mat loadedImage = null;
        DebugWindow debugWindow = null;
        CadranDefinition cadranDefinition = null;
        Screen currentScreen = null;//new Screen("defaultScreenName");

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
            Image<Bgr, byte> currentFrame = null;
            if (capture != null)
             currentFrame = capture.QueryFrame().ToImage<Bgr, byte>();
            else if (capture == null)
            {
                currentFrame = loadedImage.ToImage<Bgr, byte>();
            }
            if(currentScreen != null)
            {
                
                Mat frame = currentFrame.Mat;
                frame = ImageProcessor.WarpPerspective(frame, Utils.GetPoints(currentScreen.coordinates));

                    SetImageAndCanvasSize(frame.Height, frame.Width);
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

            if (add_markers == true && currentScreen == null)
            {
                
                if (num_of_clicks < 4)
                {
                    System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                    AddEllipse(ellipse);
                    Point point = new Point(Mouse.GetPosition(image1).X, Mouse.GetPosition(image1).Y);
                    Canvas.SetLeft(ellipse, point.X);
                    Canvas.SetTop(ellipse, point.Y);
                    markers.Add(point);
                    num_of_clicks++;
                }

                if (num_of_clicks == 4)
                {
                    currentScreen = new Screen("selectedScreen");
                    currentScreen.coordinates = markers;
                    Image<Bgr, byte> currentFrame = GetCurrentImage();
                    Mat img = currentFrame.Mat;
                    Mat warp = ImageProcessor.WarpPerspective(img, Utils.GetPoints(currentScreen.coordinates));
                    btnAddDialMarkers.IsEnabled = true;
                    btn_AddMarkers.IsEnabled = false;
                    add_markers = false;
                    ResetMarkers();
                    markers = null;
                    num_of_clicks = 0;
                }
            }
            else if (add_markers)
            {
                if (num_of_clicks < 4)
                {
                    System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                    AddEllipse(ellipse);
                    Point point = new Point(Mouse.GetPosition(image1).X, Mouse.GetPosition(image1).Y);
                    Canvas.SetLeft(ellipse, point.X);
                    Canvas.SetTop(ellipse, point.Y);
                    markers.Add(point);
                    num_of_clicks++;
                }
                if (num_of_clicks == 4)
                {
                    TreeViewItem treeItemTest = new TreeViewItem();
                    Image<Bgr, byte> currentFrame = GetCurrentImage();
                    Mat img = currentFrame.Mat;
                    Mat warp = ImageProcessor.WarpPerspective(img, Utils.GetPoints(markers));      
                    ResetMarkers();
                    try { cadranDefinition = new CadranDefinition(); cadranDefinition.Owner = GetWindow(this); cadranDefinition.ShowDialog(); }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("A handled exception just occurred: " + ex.Message, "Exception Sample", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    //Doar de test, voi sterge dupa gasirea unei solutii mai bune
                    var item = (ComboBoxItem)cadranDefinition.CadranType.SelectedItem;
                    var content = (string)item.Content;
                    currentScreen.dials.Add(new Dial(cadranDefinition.CadranName.Text, cadranDefinition.CadranType.Text));
                    num_of_clicks = 0;
                    markers.Clear();
                    //Adaugare in treeview
                    treeItemTest.Header = cadranDefinition.CadranName.Text;
                    treeItemTest.Items.Add(content);
                    tree.Items.Add(treeItemTest);
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
            num_of_clicks = 0;
            add_markers = false;
            btn_AddMarkers.IsEnabled = true;
            currentScreen = null;
            btnAddDialMarkers.IsEnabled = false;
            btnAddMarkers.Background = Brushes.LightGray;
            if (capture == null)
                capture = new Capture(0);
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

        private Image<Bgr,byte> GetCurrentImage()
        {
            ImageSource image = image1.Source;
            BitmapSource bmp = (BitmapSource)image;
            System.Drawing.Bitmap imgg = Utils.BitmapFromSource(bmp);
            return new Image<Bgr, byte>(imgg);
        }

        private void BtnAddMarkers_Click(object sender, RoutedEventArgs e)
        {
            if (add_markers == false)
            {
                add_markers = true;
                markers = new List<Point>();
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

        private void BtnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "JPG Files(*.jpg)|*.jpg| PNG Files(*.png)|*.png";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                string imagePath = openFileDialog.FileName;
                capture = null;
                loadedImage = CvInvoke.Imread(imagePath, 0);
                CvInvoke.Resize(loadedImage, loadedImage, new System.Drawing.Size(640, 360),interpolation:Inter.Linear);
                image1.Source = Utils.ToBitmapSource(loadedImage);
                SetImageAndCanvasSize(loadedImage.Height, loadedImage.Width);
                //CvInvoke.Imshow("bla", new Image<Bgr,byte>(imgg));
                //Utils.ToBitmapSource(imgg);
                    
            }

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