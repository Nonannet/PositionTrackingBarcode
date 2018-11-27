using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CameraPositionTracking
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            byte[] imageBuffer;
            int imageWidth;
            int imageHeight;
            string filePath = "..\\..\\test\\WP_20141125_004.png";
            double dpi = 96;

            System.Diagnostics.Debug.WriteLine(Environment.CurrentDirectory);

            FastLookUpTable PatternTable = new FastLookUpTable();

            PatternTable.LoadFromFile("..\\..\\test\\output.dat");

            ImageArrayFromFile(filePath, out imageBuffer, out imageWidth, out imageHeight);

            ImageRec MyIR = new ImageRec(imageWidth, imageHeight);
            ImageRec MyHT = new ImageRec(imageWidth, imageHeight);
            ImageRec MyDi = new ImageRec(imageWidth, imageHeight);


            //MyHT.LoopMode = true;

            MyIR.SetImageData(imageBuffer, 1);

            DateTime StartTime = DateTime.UtcNow;


            MyIR.GausFilter();

            ElipsedTime(StartTime, "GausFilter");

            MyIR.Convolute();

            ElipsedTime(StartTime, "Convolute");

            MyIR.SuppressNonMaximum();

            ElipsedTime(StartTime, "SuppressNonMaximum");
            
            MyIR.FastHoughTransformCircle(MyDi);

            ElipsedTime(StartTime, "HoughTransformCircle");

            MyDi.GausFilter();

            ElipsedTime(StartTime, "GausFilter");

            MyDi.Singulate(16, 16, 10);

            ElipsedTime(StartTime, "Singulate");

            MyDi.FindNeighbors();

            var PointTrip = MyDi.FindPattern();


            /*for (int i = 0; i < 7; i++ )
            {
                System.Diagnostics.Debug.WriteLine(PointTrip.Points[i].X + "; " + PointTrip.Points[i].Y);
            }*/

            ElipsedTime(StartTime, "FindHexPoint");

            if (PointTrip != null)
            {
                var data = MyIR.GetPatternData(PointTrip);

                PointTrip.GetAngleTo(0);


                MyIR.CircleDiameterStat(MyDi, PointTrip);

                ElipsedTime(StartTime, "GetPatternData");
                PatternTable.Select(data);
            }

            

            System.Diagnostics.Debug.WriteLine(PatternTable.X + "; " + PatternTable.Y + "; " + PatternTable.Rotation);


            ElipsedTime(StartTime, "PatternTable.Select");

            MyIR.CombineBuffers();

            //MyDi.RelativeThreshold(0.2);

            MyHT.SetImageData(imageBuffer, 1);

            MyIR.NormelizeImage();
            MyHT.NormelizeImage();
            MyDi.NormelizeImage();

            


            var bmp1 = BitmapSource.Create(MyHT.Width, MyHT.Height, dpi, dpi, PixelFormats.Bgra32, null, MyHT.Get32BitArray(), MyHT.Width * 4);
            TestImage.Source = bmp1;

            var bmp2 = BitmapSource.Create(MyIR.Width, MyIR.Height, dpi, dpi, PixelFormats.Bgra32, null, MyIR.Get32BitArray(), MyIR.Width * 4);
            OutPutImage.Source = bmp2;

            var bmp3 = BitmapSource.Create(MyDi.Width, MyDi.Height, dpi, dpi, PixelFormats.Bgra32, null, MyDi.Get32BitArray(), MyDi.Width * 4);
            StattImage.Source = bmp3;

            TestImage.Width = bmp1.Width;
            TestImage.Height = bmp1.Height;

            OutPutImage.Width = bmp2.Width;
            OutPutImage.Height = bmp2.Height;

            StattImage.Width = bmp3.Width;
            StattImage.Height = bmp3.Height;

        }

        private void ElipsedTime(DateTime StartTime, string Lable)
        {
            double tdiff = (double)DateTime.UtcNow.Subtract(StartTime).Ticks / TimeSpan.TicksPerMillisecond;
            
            System.Diagnostics.Debug.WriteLine(tdiff.ToString() + "      " +   Lable);
        }


        public void ImageArrayFromFile(string FilePath, out byte[] dataBufferIn, out int Width, out int Height)
        {
            BitmapImage Bmp = new BitmapImage(new Uri(FilePath, UriKind.Relative));

            Width = Bmp.PixelWidth;
            Height = Bmp.PixelHeight;

            int nStride = (Bmp.PixelWidth * Bmp.Format.BitsPerPixel+7) / 8;


            dataBufferIn = new byte[Bmp.PixelHeight * nStride];

            Bmp.CopyPixels(dataBufferIn, nStride, 0);
        }
    }
}
