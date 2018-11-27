using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CameraPositionTracking
{
    class FastLookUpTable
    {
        private UInt32[] keyList;
        private Byte[] rotationList;
        private UInt16[] valueListX;
        private UInt16[] valueListY;

        private int xPos = 0;
        private int yPos = 0;
        private int rotVal = 0;

        public int X
        {
            get { return xPos; }
        }

        public int Y
        {
            get { return yPos; }
        }

        public int Rotation
        {
            get { return rotVal; }
        }


        public int LoadFromFile(string Path)
        {
            FileStream lfile = File.OpenRead(Path);
            byte[] buffer = new byte[lfile.Length];

            lfile.Read(buffer, 0, buffer.Length);

            int lines = LoadFromByteArray(buffer);

            lfile.Close();

            return lines;
        }

        public async Task<int> LoadAsyncFromFile(string Path)
        {
            FileStream lfile = File.OpenRead(Path);
            byte[] buffer = new byte[lfile.Length];

            await lfile.ReadAsync(buffer, 0, buffer.Length);

            int lines = LoadFromByteArray(buffer);

            lfile.Close();

            return lines;
        }

        private int LoadFromByteArray(byte[] buffer)
        {
            int lines = buffer.Length / 9;

            keyList = new uint[lines];
            rotationList = new byte[lines];
            valueListX = new UInt16[lines];
            valueListY = new UInt16[lines];

            for (int i = 0; i < lines; i++)
            {
                int j = i * 9;

                keyList[i] = ((uint)buffer[j] << 24) | ((uint)buffer[j + 1] << 16) | ((uint)buffer[j + 2] << 8) | buffer[j + 3];
                rotationList[i] = buffer[j + 4];
                valueListX[i] = (UInt16)(((UInt16)buffer[j + 5] << 8) | buffer[j + 6]);
                valueListY[i] = (UInt16)(((UInt16)buffer[j + 7] << 8) | buffer[j + 8]);
            }

            return lines;
        }

        public bool Select(int Value)
        {
            int i = keyList.Length / 2;
            int di = i;

            while (di > 0)
            {
                di = di / 2;

                if (Value > keyList[i])
                {
                    i += di;
                }
                else if (Value < keyList[i])
                {
                    i -= di;
                }
                else
                {
                    xPos = valueListX[i];
                    yPos = valueListY[i];
                    rotVal = rotationList[i];

                    return true;
                }
            }

            return false;
        }
    }
}
