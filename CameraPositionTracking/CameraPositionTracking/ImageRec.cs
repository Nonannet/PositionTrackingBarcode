using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraPositionTracking
{
    class ImageRec
    {
        private struct DigFilter
        {
            public int[,] filterMatrix;
            public int xLen;
            public int yLen;
        }

        public struct IntPoint
        {
            public int X;
            public int Y;
            public int[] Neighbors;
            public int[] Dists;
        }

        private int[] PrimarryBuffer;
        private int[] workingBuffer;
        private int[] SecondaryBuffer;
        private int[] tempBuff;

        private IntPoint[] PointList;
        private int PointCount;

        public int Width;
        public int Height;
        public int PixelCount;
        public bool LoopMode = false;

        private int diagonalLen;

        private static DigFilter ScharrOperatorX;
        private static DigFilter ScharrOperatorY;
        private static DigFilter GausFilterX;
        private static DigFilter GausFilterY;

        private const int SlopeTrashould = (256 * 256 * 16);

        public IntPoint CentrePoint;

        public ImageRec(int ImageWidth, int ImageHeight)
        {
            PrimarryBuffer = new int[ImageHeight * ImageWidth];
            workingBuffer = new int[ImageHeight * ImageWidth];
            SecondaryBuffer = new int[ImageHeight * ImageWidth];
            IntSetImageData(ImageWidth, ImageHeight);

            PointList = new IntPoint[256];

            CentrePoint.X = ImageWidth / 2;
            CentrePoint.Y = ImageHeight / 2;
        }

        public void SetImageData(byte[] Buffer, int Scale)
        {
            SetImageData(Buffer, Scale, Width, Height);
        }

        public void SetImageData(byte[] Buffer, int Scale, int SrcWidth, int SrcHeight)
        {
            int i = 0;
            int i2 = 0;
            int xdiff = (SrcWidth - Width * Scale) / 2;
            int ydiff = (SrcHeight - Height * Scale) / 2;

            for (int y = 0; y < Height * Scale; y += Scale)
            {
                for (int x = 0; x < Width * Scale; x += Scale)
                {
                    i = (x + xdiff + (y + ydiff) * SrcWidth) * 4;

                    PrimarryBuffer[i2] = Buffer[i];
                    PrimarryBuffer[i2] += Buffer[i + 1];
                    PrimarryBuffer[i2] += Buffer[i + 2];

                    i2++;
                }
            }
        }

        private void IntSetImageData(int ImageWidth, int ImageHeight)
        {
            Height = ImageHeight;
            Width = ImageWidth;
            PixelCount = Height * Width;
            diagonalLen = Isqrt(Width * Width + Height * Height);

            prepareFilters();
        }

        private void prepareFilters()
        {
            ScharrOperatorX.filterMatrix = new int[,] { { 3, 0, -3 }, { 10, 0, -10 }, { 3, 0, -3 } };
            ScharrOperatorX.xLen = 3;
            ScharrOperatorX.yLen = 3;

            ScharrOperatorY.filterMatrix = new int[,] { { 3, 10, 3 }, { 0, 0, 0 }, { -3, -10, -3 } };
            ScharrOperatorY.xLen = 3;
            ScharrOperatorY.yLen = 3;

            GausFilterX.filterMatrix = new int[,] { { 25 }, { 61 }, { 83 }, { 61 }, { 25 } };
            GausFilterX.xLen = 5;
            GausFilterX.yLen = 1;

            GausFilterY.filterMatrix = new int[,] { { 25, 61, 83, 61, 25 } };
            GausFilterY.xLen = 1;
            GausFilterY.yLen = 5;

            //private int[,] filterGausY = { { 15, 32, 51, 59, 51, 32, 15 } };
            //private int[,] filterGausX = { {15}, {32}, {51}, {59}, {51}, {32}, {15} };
        }

        public void GausFilter()
        {
            int fVal;
            int yW;

            for (int y = 0; y < Height; y++)
            {
                yW = y * Width;

                for (int x = 2; x < Width - 2; x++)
                {
                    fVal = 0;

                    for (int i = 0; i < 5; i++)
                    {
                        fVal += PrimarryBuffer[x + i - 2 + yW] * GausFilterX.filterMatrix[i, 0];
                    }

                    SecondaryBuffer[x + yW] = fVal / 256;
                }

                SecondaryBuffer[0 + yW] = PrimarryBuffer[0 + yW];
                SecondaryBuffer[1 + yW] = PrimarryBuffer[1 + yW];
                SecondaryBuffer[Width - 2 + yW] = PrimarryBuffer[Width - 2 + yW];
                SecondaryBuffer[Width - 1 + yW] = PrimarryBuffer[Width - 1 + yW];
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 2; y < Height - 2; y++)
                {
                    fVal = 0;

                    for (int i = 0; i < 5; i++)
                    {
                        fVal += SecondaryBuffer[x + (y + i - 2) * Width] * GausFilterY.filterMatrix[0, i];
                    }

                    workingBuffer[x + y * Width] = fVal / 256;
                }

                workingBuffer[x + 0 * Width] = SecondaryBuffer[x + 0 * Width];
                workingBuffer[x + 1 * Width] = SecondaryBuffer[x + 1 * Width];
                workingBuffer[x + (Height - 2) * Width] = SecondaryBuffer[x + (Height - 2) * Width];
                workingBuffer[x + (Height - 1) * Width] = SecondaryBuffer[x + (Height - 1) * Width];
            }

            tempBuff = PrimarryBuffer;
            PrimarryBuffer = workingBuffer;
            workingBuffer = tempBuff;
        }

        public void Singulate(int FrameSizeX, int FrameSizeY, int Threshold)
        {
            int bigestValue = 0;
            int bigestPos = 0;
            int bigestPosX = 0;
            int bigestPosY = 0;
            int i;

            PointCount = 0;
            PointList.Initialize();

            for (i = 0; i < PixelCount; i++)
            {
                workingBuffer[i] = 0;
            }

            for (int x = 0; x < Width; x += FrameSizeX)
            {
                for (int y = 0; y < Height; y += FrameSizeY)
                {
                    bigestValue = 0;
                    bigestPos = -1;

                    for (int ix = -FrameSizeX; ix < FrameSizeX * 2; ix++)
                    {
                        for (int iy = -FrameSizeY; iy < FrameSizeY * 2; iy++)
                        {
                            i = GetIndex(x + ix, y + iy);
                            if (bigestValue < PrimarryBuffer[i])
                            {
                                bigestValue = PrimarryBuffer[i];
                                if (ix >= 0 && ix < FrameSizeX && iy >= 0 && iy < FrameSizeY)
                                {
                                    bigestPos = i;
                                    bigestPosX = ix;
                                    bigestPosY = iy;
                                }
                                else
                                {
                                    bigestPos = -1;
                                }
                            }
                        }
                    }

                    if (bigestPos >= 0 && bigestValue > Threshold && PointCount < PointList.Length)
                    {
                        workingBuffer[bigestPos] = 255; // bigestValue;
                        workingBuffer[bigestPos + 1] = 255; // bigestValue;
                        workingBuffer[bigestPos - 1] = 255; // bigestValue;
                        PointList[PointCount].X = bigestPosX + x;
                        PointList[PointCount].Y = bigestPosY + y;
                        PointList[PointCount].Neighbors = new int[8];
                        PointList[PointCount].Dists = new int[8];
                        PointCount++;
                    }
                }
            }

            Array.Sort(PointList, delegate(IntPoint X, IntPoint Y)
            {
                return IntPointDist(X, CentrePoint) - IntPointDist(Y, CentrePoint);
            });

            tempBuff = PrimarryBuffer;
            PrimarryBuffer = workingBuffer;
            workingBuffer = tempBuff;
        }


        public void ClearImage()
        {
            for (int i = 0; i < PixelCount; i++)
            {
                PrimarryBuffer[i] = 0;
            }
        }

        public void NormelizeImage()
        {
            int bigestValue = 0;
            int smalestValue = 255;

            for (int i = 0; i < PixelCount; i++)
            {
                if (bigestValue < PrimarryBuffer[i]) bigestValue = PrimarryBuffer[i];
                if (smalestValue > PrimarryBuffer[i]) smalestValue = PrimarryBuffer[i];
            }

            if (bigestValue > smalestValue)
            {
                for (int i = 0; i < PixelCount; i++)
                {
                    PrimarryBuffer[i] = PrimarryBuffer[i] * 255 / (bigestValue - smalestValue);
                }
            }
        }

        public void AmplifyImage(int Faktor)
        {
            for (int i = 0; i < PixelCount; i++)
            {
                PrimarryBuffer[i] = PrimarryBuffer[i] * Faktor;
            }
        }

        private static int Isqrt(int num)
        {
            if (0 == num) { return 0; }  // Avoid zero divide   
            int n = (num / 2) + 1;       // Initial estimate, never low   
            int n1 = (n + (num / n)) / 2;

            while (n1 < n)
            {
                n = n1;
                n1 = (n + (num / n)) / 2;
            }
            return n;
        }

        private int GetFilteredPixel(int inpX, int inpY, int[] buffer, DigFilter filter)
        {
            int pixelValue = 0;

            for (int x = 0; x < filter.xLen; x++)
            {
                for (int y = 0; y < filter.yLen; y++)
                {
                    pixelValue += buffer[GetIndex(inpX + x - filter.xLen / 2, inpY + y - filter.yLen / 2)] * filter.filterMatrix[x, y];
                }
            }

            return pixelValue;
        }

        private int GetIndex(int x, int y)
        {
            if (LoopMode)
            {
                if (x > Width - 1) x -= Width - 1;
                if (y > Height - 1) y -= Height - 1;
                if (x < 0) x += Width;
                if (y < 0) y += Height;
            }
            else
            {
                if (x > Width - 1) x = Width - 1;
                if (y > Height - 1) y = Height - 1;
                if (x < 0) x = 0;
                if (y < 0) y = 0;
            }
            return x + y * Width;
        }

        public void Convolute()
        {
            int pValX;
            int pValY;

            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    pValX = 0;
                    pValY = 0;

                    for (int xf = 0; xf < 3; xf++)
                    {
                        for (int yf = 0; yf < 3; yf++)
                        {
                            pValX += PrimarryBuffer[x + xf - 1 + (y + yf - 1) * Width] * ScharrOperatorX.filterMatrix[xf, yf];
                            pValY += PrimarryBuffer[x + xf - 1 + (y + yf - 1) * Width] * ScharrOperatorY.filterMatrix[xf, yf];
                        }
                    }

                    workingBuffer[GetIndex(x, y)] = pValX;
                    SecondaryBuffer[GetIndex(x, y)] = pValY;
                }
            }

            tempBuff = PrimarryBuffer;
            PrimarryBuffer = workingBuffer;
            workingBuffer = tempBuff;
        }

        public void CombineBuffers()
        {
            int dx;
            int dy;

            for (int i = 0; i < PixelCount; i++)
            {
                dx = PrimarryBuffer[i];
                dy = SecondaryBuffer[i];

                PrimarryBuffer[i] = Isqrt(dx * dx + dy * dy);
            }
        }

        public void SuppressNonMaximum()
        {
            int dx;
            int dy;

            for (int x = 0; x < Width; x++)
            {
                workingBuffer[GetIndex(x, 0)] = 0;
                workingBuffer[GetIndex(x, Height - 1)] = 0;
            }
            for (int y = 0; y < Height; y++)
            {
                workingBuffer[GetIndex(0, y)] = 0;
                workingBuffer[GetIndex(Width - 1, y)] = 0;
            }

            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    dx = PrimarryBuffer[GetIndex(x, y)];
                    dy = SecondaryBuffer[GetIndex(x, y)];

                    if (bComp(dy, dx)) //Wagerechte Kante
                    {
                        if (bComp(PrimarryBuffer[GetIndex(x + 1, y)], dx) || bComp(PrimarryBuffer[GetIndex(x - 1, y)], dx))
                            workingBuffer[GetIndex(x, y)] = 0;
                        else
                            workingBuffer[GetIndex(x, y)] = 1;
                    }
                    else //Senkrechte Kante
                    {
                        if (bComp(SecondaryBuffer[GetIndex(x, y + 1)], dy) || bComp(SecondaryBuffer[GetIndex(x, y - 1)], dy))
                            workingBuffer[GetIndex(x, y)] = 0;
                        else
                            workingBuffer[GetIndex(x, y)] = 1;
                    }
                }
            }

            for (int i = 0; i < PixelCount; i++)
            {
                PrimarryBuffer[i] *= workingBuffer[i];
                SecondaryBuffer[i] *= workingBuffer[i];
            }
        }

        private bool bComp(int Val1, int Val2)
        {
            int v1 = Val1;
            int v2 = Val2;

            if (v1 < 0) v1 *= -1;
            if (v2 < 0) v2 *= -1;

            return v1 > v2;
        }

        public void FastHoughTransformCircle(ImageRec DestImageData)
        {
            int slope2;
            int dx;
            int dy;

            int iy = 0;
            int ix = 0;
            int i;

            int MaxDi = (Width + Height) / 4;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    i = x + y * Width;

                    dx = PrimarryBuffer[i];
                    dy = SecondaryBuffer[i];

                    slope2 = (dx * dx) + (dy * dy);

                    if (slope2 > SlopeTrashould)
                    {
                        if (bComp(dy, dx))
                        {
                            for (ix = 0; ix < Width; ix++)
                            {
                                iy = (int)((ix - x) * dx / dy + y); //Create orthogonal line
                                if (iy >= 0 && iy < DestImageData.Height)
                                    DestImageData.PrimarryBuffer[ix + iy * DestImageData.Width] += 1;
                            }
                        }
                        else
                        {
                            for (iy = 0; iy < Height; iy++)
                            {
                                ix = (int)((iy - y) * dy / dx + x); //Create orthogonal line
                                if (ix >= 0 && ix < DestImageData.Width)
                                    DestImageData.PrimarryBuffer[ix + iy * DestImageData.Width] += 1;
                            }
                        }
                    }
                }
            }
        }

        static private Double PointDist(IntPoint P1, IntPoint P2)
        {
            int dx = P1.X - P2.X;
            int dy = P1.Y - P2.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        static private int IntPointDist(IntPoint P1, IntPoint P2)
        {
            int dx = P1.X - P2.X;
            int dy = P1.Y - P2.Y;

            return Isqrt(dx * dx + dy * dy);
        }

        public void FindNeighbors()
        {
            int dist;
            int tmp1 = 0;
            int tmp2 = 0;
            int tmp3 = 0;
            int tmp4 = 0;
            bool mFlag;

            for (int i = 0; i < PointCount; i++)
            {
                IntPoint pPoint = PointList[i];

                for (int j = 0; j < PointCount; j++)
                {
                    if (i != j)
                    {
                        mFlag = false;

                        dist = IntPointDist(pPoint, PointList[j]);

                        for (int k = 0; k < pPoint.Dists.Length; k++)
                        {
                            if (mFlag)
                            {
                                tmp3 = pPoint.Dists[k];
                                tmp4 = pPoint.Neighbors[k];
                                pPoint.Dists[k] = tmp1;
                                pPoint.Neighbors[k] = tmp2;
                                tmp1 = tmp3;
                                tmp2 = tmp4;
                            }
                            else if (dist < pPoint.Dists[k] || pPoint.Dists[k] == 0)
                            {
                                tmp1 = pPoint.Dists[k];
                                tmp2 = pPoint.Neighbors[k];
                                pPoint.Dists[k] = dist;
                                pPoint.Neighbors[k] = j;
                                mFlag = true;
                            }
                        }
                    }
                }

                for (int j = 0; j < pPoint.Dists.Length; j++)
                {
                    if (pPoint.Dists[j] * 1.2 < pPoint.Dists[2] || pPoint.Dists[j] / 1.2 > pPoint.Dists[2])
                    {
                        pPoint.Dists[j] = 0;
                    }
                }
            }
        }

        public class PatternPoint
        {
            public IntPoint[] Points = new IntPoint[7];
            public double AverageDistance = 0;

            public Double GetAngleTo(int index)
            {
                double ret = 0;
                const double circ3 = Math.PI / 3;
                int offsIndex = 0;
                double angle = 0;

                for (int i = 1; i < 7; i++)
                {
                    angle = Math.Atan2(Points[i].X - Points[0].X, Points[i].Y - Points[0].Y);


                    if (angle >= 0 && angle < circ3) offsIndex = i;

                    if (angle < 0)
                        ret += circ3 - (Math.PI - angle) % circ3;
                    else
                        ret += angle % circ3;

                    //System.Diagnostics.Debug.WriteLine(angle + "  " + ret + "  " + (Points[i].X - Points[0].X) + "  " + (Points[i].Y - Points[0].Y));
                }

                return ret / 6 + circ3 * (offsIndex - index);
            }
        }

        public PatternPoint FindPattern()
        {
            PatternPoint ret = new PatternPoint();

            for (int i = 0; i < PointCount; i++)
            {
                IntPoint cPoint = PointList[i];
                int hindex = 1;
                int k1 = 0;
                Double Distance = 0;

                while (k1 < cPoint.Dists.Length && cPoint.Dists[k1] == 0) k1++;

                IntPoint sPoint = PointList[cPoint.Neighbors[k1]];

                for (int j = 0; j < 6; j++)
                {
                    for (int k2 = 0; k2 < cPoint.Dists.Length; k2++)
                    {
                        if (cPoint.Dists[k2] > 0)
                        {
                            for (int k3 = 0; k3 < sPoint.Dists.Length; k3++)
                            {
                                if (sPoint.Dists[k3] > 0 && (sPoint.Neighbors[k3] == cPoint.Neighbors[k2]))
                                {
                                    IntPoint s2Point = PointList[cPoint.Neighbors[k2]];

                                    int kp = (sPoint.X - cPoint.X) * (s2Point.Y - cPoint.Y) - (s2Point.X - cPoint.X) * (sPoint.Y - cPoint.Y);

                                    if (kp > 0)
                                    {
                                        //System.Diagnostics.Debug.WriteLine(i + ": " + IntPointDist(cPoint, s2Point) + " -- " + IntPointDist(sPoint, s2Point) + " -- " + kp);
                                        Distance += PointDist(cPoint, s2Point);
                                        ret.Points[hindex] = sPoint;
                                        hindex++;
                                        sPoint = s2Point;
                                    }

                                    if (hindex > 6)
                                    {
                                        ret.AverageDistance = Distance / 6;
                                        ret.Points[0] = cPoint;
                                        return ret;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public int GetPatternData(PatternPoint Item)
        {
            int dist;
            int dy = 0;
            int dx = 0;
            int iy = 0;
            int ix = 0;
            int slope2 = 0;
            int ret = 0;

            const int bitCount = 4;

            int SquareRadius = (int)Item.AverageDistance / 2;

            for (int i = 0; i < Item.Points.Length; i++)
            {
                int CountSum = 0;
                int[] LineBuffer = new int[bitCount];
                bool lastBit = false;

                for (int x = -SquareRadius; x < SquareRadius; x++)
                {
                    ix = Item.Points[i].X + x;

                    for (int y = -SquareRadius; y < SquareRadius; y++)
                    {
                        iy = Item.Points[i].Y + y;

                        if (iy > 0 && ix > 0 && iy < Height - 1 && ix < Width - 1)
                        {
                            dx = PrimarryBuffer[GetIndex(ix, iy)];
                            dy = SecondaryBuffer[GetIndex(ix, iy)];

                            slope2 = (dx * dx) + (dy * dy);

                            if (slope2 > (256 * 256 * 16))
                            {
                                dist = Isqrt(x * x + y * y);

                                if (dist < SquareRadius)
                                {
                                    int SumVal = (128 / (dist + 1));
                                    LineBuffer[dist * bitCount / SquareRadius] += SumVal;
                                    CountSum += SumVal;
                                }
                            }
                        }
                    }
                }

                CountSum /= bitCount * 2;

                for (int j = 3; j >= 0; j--)
                {
                    if (LineBuffer[j] > CountSum != lastBit)
                    {
                        ret |= (1 << (24 - i * 4 + j));
                        lastBit = true;
                    }
                    else
                    {
                        lastBit = false;
                    }
                }
            }

            return ret;
        }

        public void CircleDiameterStat(ImageRec DestImageData, PatternPoint Item)
        {
            int dist;
            int dy = 0;
            int dx = 0;
            int iy = 0;
            int ix = 0;
            int slope2 = 0;
            int[] LineBuffer = new int[48];

            int SquareRadius = (int)Item.AverageDistance / 2;

            for (int i = 0; i < Item.Points.Length; i++)
            {
                int CountSum = 0;

                for (int x = -SquareRadius; x < SquareRadius; x++)
                {
                    ix = Item.Points[i].X + x;

                    for (int y = -SquareRadius; y < SquareRadius; y++)
                    {
                        iy = Item.Points[i].Y + y;

                        if (iy > 0 && ix > 0 && iy < Height - 1 && ix < Width - 1)
                        {
                            dx = PrimarryBuffer[GetIndex(ix, iy)];
                            dy = SecondaryBuffer[GetIndex(ix, iy)];

                            slope2 = (dx * dx) + (dy * dy);

                            if (slope2 > (256 * 256 * 16))
                            {
                                dist = Isqrt(x * x + y * y);

                                if (dist < SquareRadius)
                                {
                                    DestImageData.PrimarryBuffer[DestImageData.GetIndex(dist * 16 / SquareRadius, i * 2)] += (128 / (dist + 1));
                                }
                            }
                        }

                    }
                }
            }
        }


        public void RelativeThreshold(double Value)
        {
            int maxVal;
            int cval;


            for (int y = 0; y < Height; y++)
            {
                maxVal = 0;

                for (int x = 0; x < Width; x++)
                {
                    cval = PrimarryBuffer[GetIndex(x, y)];

                    if (cval > maxVal) maxVal = cval;
                }

                for (int x = 0; x < Width; x++)
                {
                    cval = PrimarryBuffer[GetIndex(x, y)];

                    //PrimarryBuffer[GetIndex(x, y)] = (cval > maxVal * Value) ? 1 : 0;
                    PrimarryBuffer[GetIndex(x, y)] = cval * 256 / (maxVal + 1);
                }
            }
        }

        public byte[] Get32BitArray()
        {
            //PrepareImageArray();

            byte[] destArray = new byte[PixelCount * 4];

            int pixVal;

            for (int i = 0; i < PixelCount; i++)
            {
                pixVal = PrimarryBuffer[i];
                if (pixVal > 255) pixVal = 255;

                destArray[i * 4 + 0] = (byte)pixVal;
                destArray[i * 4 + 1] = (byte)pixVal;
                destArray[i * 4 + 2] = (byte)pixVal;
                destArray[i * 4 + 3] = 0xFF;
            }

            return destArray;
        }
    }
}
