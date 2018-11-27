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
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.IO;

namespace PositionPatternCreator
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        static double mmw = 96 / 24.5;
        static double ratio = 0.866025403784438;
        static double MarkerStrokeSize = 5;
        static double Dist = MarkerStrokeSize * 4.5 * 2;

        bool swCount = true;

        private struct DataListEntr
        {
            private int[] data;
            private int centPositionX;
            private int centPositionY;
            private int rot;

            public int GetData(int i)
            {
                return data[i];
            }

            public void SetData(int i, int Data)
            {
                if (data == null) data = new int[7];

                data[i] = Data;
            }

            public int GetArrayEntr(int[,] DataArray)
            {
                return DataArray[centPositionX, centPositionY];
            }

            public void SetArrayEntr(ref int[,] DataArray, int Data)
            {
                DataArray[centPositionX, centPositionY] = Data;
                //System.Diagnostics.Debug.WriteLine(centPositionX + "  " + centPositionY + "    -- >  " + Data);
            }

            public void SetPos(int PosX, int PosY)
            {
                centPositionX = PosX;
                centPositionY = PosY;
            }

            public int X { get { return centPositionX; } }
            public int Y { get { return centPositionY; } }

            public int Rotation {
                get { return rot; }
                set { rot = value; }
            }

            public int CompareTo(DataListEntr Entr)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (this.data[i] > Entr.data[i]) return 1;
                    if (this.data[i] < Entr.data[i]) return -1;
                }

                return 0;
            }
        }

        private double AddOffs(double xVal, int index)
        {
            if ((index & 1) > 0)
                return xVal;
            else
                return xVal + Dist / 2;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string containerName = @"C:\Users\Nicolas\Documents\Visual Studio 2013\Projects\CameraPositionTrackingCreatorHex\PositionPatternCreator\bin\Debug\output.xps";
            string binListName = @"C:\Users\Nicolas\Documents\Visual Studio 2013\Projects\CameraPositionTrackingCreatorHex\PositionPatternCreator\bin\Debug\output.dat";

            File.Delete(containerName);

            /*LocalPrintServer ps = new LocalPrintServer();
            PrintQueue pq = ps.DefaultPrintQueue;
            XpsDocumentWriter xpsdw = PrintQueue.CreateXpsDocumentWriter(pq);
            PrintTicket pt = pq.UserPrintTicket;

            pt.PageOrientation = PageOrientation.Portrait;
            PageMediaSize pageMediaSize = new PageMediaSize(DrawCanvas.ActualWidth, DrawCanvas.ActualHeight);
            pt.PageMediaSize = pageMediaSize;
            xpsdw.Write(DrawCanvas);*/
           
            FixedPage fp = new FixedPage();

            //Canvas fp = DrawCanvas;

            fp.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            
            //Din A4
            fp.Height = 297 * mmw;
            fp.Width = 210 * mmw;

            //Din A3
            //fp.Height = 594 * mmw;
            //fp.Width = 297 * mmw;

            int sizex = (int)((fp.Width - 30) / Dist);
            int sizey = (int)((fp.Height - 30) / Dist / ratio);


            int[,] dat = new int[sizex+2, sizey+2];

            int[,] xm = { { -1, -1, 0, 1, 0, -1 }, { -1, 0, 1, 0, -1, -1 }, { 0, 1, 0, -1, -1, -1 }, { 1, 0, -1, -1, -1, 0 }, { 0, -1, -1, -1, 0, 1 }, { -1, -1, -1, 0, 1, 0 } };
            int[,] ym = { { 0, -1, -1, 0, 1, 1 }, { -1, -1, 0, 1, 1, 0 }, { -1, 0, 1, 1, 0, -1 }, { 0, 1, 1, 0, -1, -1 }, { 1, 1, 0, -1, -1, 0 }, { 1, 0, -1, -1, 0, 1 } };

            List<DataListEntr> sList = new List<DataListEntr>();

            int SymCount = 0;
            int dubCount = 1;

            Random rnd = new Random(SymCount);

            for (int ix = 1; ix < sizex + 1; ix++)
            {
                for (int iy = 1; iy < sizey + 1; iy++)
                {
                    dat[ix, iy] = (rnd.Next() & 0x7) | 0x8; // (rnd.Next() & 0x7) | 0x8;
                }
            }

            int lc = 0;
            int mx = 0;

            while (dubCount > 0)
            {
                lc++;
                sList.Clear();

                for (int ix = 1; ix < sizex + 1; ix++)
                {
                    for (int iy = 1; iy < sizey + 1; iy++)
                    {
                        if (ix == 15 && iy == 4 && lc == 136)
                        {
                            iy = iy;
                        }


                        for (int i = 0; i < 6; i++)
                        {
                            DataListEntr sl = new DataListEntr();

                            sl.SetPos(ix, iy);
                            sl.SetData(0, dat[ix, iy]);
                            sl.Rotation = i;

                            bool nonNull = true;

                            for (int j = 0; j < 6; j++)
                            {
                                if (ym[j, i] != 0 && iy % 2 == 0)
                                    mx = 1;
                                else
                                    mx = 0;

                                int tmpd = dat[ix + xm[j, i] + mx, iy + ym[j, i]];
                                sl.SetData(j+1, tmpd);
                                if (tmpd == 0) nonNull = false;
                            }

                            if (nonNull) sList.Add(sl);
                        }
                    }
                }

                sList.Sort(delegate(DataListEntr X, DataListEntr Y)
                {
                    return X.CompareTo(Y);
                });

                dubCount = 0;

                for (int i = 1; i < sList.Count; i++)
                {
                    if (sList[i].CompareTo(sList[i - 1]) == 0)
                    {
                        //System.Diagnostics.Debug.WriteLine("XY: " + sList[i+1].X + " " + sList[i+1].Y);
                        dubCount++;
                    }
                }

                if (dubCount > 0)
                {
                    for (int i = 1; i < sList.Count; i++)
                    {
                        if (sList[i].CompareTo(sList[i - 1]) == 0)
                        {
                            sList[i].SetArrayEntr(ref dat, (rnd.Next() & 0x7) | 0x8);
                        }
                    }

                    int iz = rnd.Next() % sList.Count;

                    sList[iz].SetArrayEntr(ref dat, (rnd.Next() & 0x7) | 0x8);
                }


                System.Diagnostics.Debug.WriteLine(dubCount +"  " + lc);
            }

            sList.Sort(delegate(DataListEntr X, DataListEntr Y)
            {
                return X.CompareTo(Y);
            });

            var ListOutFile = File.OpenWrite(binListName);

            byte[] wBytes = new byte[sList.Count * 9];

            for (int i = 0; i < sList.Count; i++)
            {
                int i2 = i * 9;

                int xpos = (int)((sList[i].X * Dist + AddOffs(0, sList[i].Y)) / mmw * 10);    //in 1/10 mm
                int ypos = (int)(sList[i].Y * Dist * ratio / mmw) * 10;                       //in 1/10 mm

                wBytes[i2 + 0] = (byte)(sList[i].GetData(0));
                wBytes[i2 + 1] = (byte)((sList[i].GetData(1) << 4) | sList[i].GetData(2));
                wBytes[i2 + 2] = (byte)((sList[i].GetData(3) << 4) | sList[i].GetData(4));
                wBytes[i2 + 3] = (byte)((sList[i].GetData(5) << 4) | sList[i].GetData(6));
                wBytes[i2 + 4] = (byte)sList[i].Rotation;
                wBytes[i2 + 5] = (byte)(xpos >> 8);
                wBytes[i2 + 6] = (byte)xpos;
                wBytes[i2 + 7] = (byte)(ypos >> 8);
                wBytes[i2 + 8] = (byte)ypos;

            }

            ListOutFile.Write(wBytes, 0, wBytes.Length);

            ListOutFile.Close();


            XpsDocument xpsDoc = new XpsDocument(containerName, FileAccess.ReadWrite);

            for (int iy = 1; iy < sizey; iy++)
            {
                for (int ix = 1; ix < sizex; ix++)
                {
                    int data = dat[ix, iy];

                    for (int n = 0; n < 4; n++)
                    {
                        bool bit = ((data >> n) & 1) == 1;

                        if (bit) fp.Children.Add(GetCircle(ix * Dist + AddOffs(12, iy), iy * Dist * ratio + 20, MarkerStrokeSize * (n + 1)));
                    }

                    //fp.Children.Add(GetCircle(ix, iy, MarkerStrokeSize * 5));

                    SymCount++;
                }
            }
            

            

            XpsDocumentWriter xpsdw = XpsDocument.CreateXpsDocumentWriter(xpsDoc);


            xpsdw.Write(fp);

            xpsDoc.Close();
        }

        private Ellipse GetCircle(double xPosition, double yPosition, double Radius)
        {
            Ellipse el = new Ellipse();
            el.Margin = new Thickness(xPosition - Radius, yPosition - Radius, 0, 0);
            el.Height = 2 * Radius;
            el.Width = 2 * Radius;
            el.StrokeThickness = MarkerStrokeSize * 1.1;
            el.Stroke = new SolidColorBrush(Colors.Black);
            if (Radius <= MarkerStrokeSize) el.Fill = new SolidColorBrush(Colors.Black);
            return el;
        }
    }
}
