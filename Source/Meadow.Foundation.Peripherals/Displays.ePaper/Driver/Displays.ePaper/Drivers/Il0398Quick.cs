using System.Threading;
using Meadow.Hardware;

namespace Meadow.Foundation.Displays.ePaper
{
    /// aka WaveShare 4.2" B tri color
    /// <summary>
    ///     Represents an Il0398 ePaper color display
    ///     400x300, 4.2inch e-Ink three-color display, SPI interface 
    /// </summary>
    public class Il0398Quick : EpdColorBase
    {
        #region Lookup Tables
        static readonly byte[] lut_vcom0_quick =
        {
            0x00, 0x0E, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        static readonly byte[] lut_ww_quick =
        {
            0xA0, 0x0E, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        static readonly byte[] lut_bw_quick =
        {
            0xA0, 0x0E, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        static readonly byte[] lut_bb_quick =
        {
            0x50, 0x0E, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };

        static readonly byte[] lut_wb_quick =
         {
            0x50, 0x0E, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        };
        #endregion

        protected override bool IsBlackInverted => false;
        protected override bool IsColorInverted => false;

        public Il0398Quick(IIODevice device, ISpiBus spiBus, IPin chipSelectPin, IPin dcPin, IPin resetPin, IPin busyPin,
            uint width, uint height) :
            base(device, spiBus, chipSelectPin, dcPin, resetPin, busyPin, width, height)
        {
        }

        #region Override from base class
        protected override void Initialize()
        {
            Reset();

            SendCommand(POWER_SETTING);
            SendData(0x03);                  // VDS_EN, VDG_EN
            SendData(0x00);                  // VCOM_HV, VGHL_LV[1], VGHL_LV[0]
            SendData(0x2b);                  // VDH
            SendData(0x2b);                  // VDL
            SendData(0xff);                  // VDHR

            SendCommand(BOOSTER_SOFT_START);
            SendData(0x17);
            SendData(0x17);
            SendData(0x17);
            SendCommand(POWER_ON);

            WaitUntilIdle();
            SendCommand(PANEL_SETTING);

            // SendData(0xbf);              // KW-BF   KWR-AF  BWROTP 0f
            // SendData(0x0b);
            // SendData(0x0F);              //300x400 Red mode, LUT from OTP
            // SendData(0x1F);              //300x400 B/W mode, LUT from OTP
            SendData(0x3F);                 //300x400 B/W mode, LUT set by register
            // SendData(0x2F);              //300x400 Red mode, LUT set by register

            SendCommand(PLL_CONTROL);
            SendData(0x3C);        // 3A 100Hz   29 150Hz   39 200Hz    31 171Hz       3C 50Hz (default)    0B 10Hz
        }

        protected override void Refresh()
        {
            xRefreshStart = -1;
            if (xRefreshStart == -1)
            {
                DisplayFrame(blackImageBuffer, colorImageBuffer);
            }
            else
            {
                SetPartialWindow(blackImageBuffer, colorImageBuffer,
                        xRefreshStart, yRefreshStart, xRefreshEnd - xRefreshStart, yRefreshEnd - yRefreshStart);

                DisplayFrame();
            }

            xRefreshStart = yRefreshStart = xRefreshEnd = yRefreshEnd = -1;
        }
        #endregion



        void DisplayFrame(byte[] blackBuffer, byte[] colorBuffer)
        {
            SendCommand(RESOLUTION_SETTING);
            SendData((byte)(Height >> 8) & 0xFF);
            SendData((byte)(Height & 0xFF));
            SendData((byte)(Width >> 8) & 0xFF);
            SendData((byte)(Width & 0xFF));

            SendCommand(VCM_DC_SETTING);
            SendData(0x12);

            SendCommand(VCOM_AND_DATA_INTERVAL_SETTING);
            SendCommand(0x97);          //VBDF 17|D7 VBDW 97  VBDB 57  VBDF F7  VBDW 77  VBDB 37  VBDR B7


            SendCommand(DATA_START_TRANSMISSION_1);
            Thread.Sleep(2);

            for (int i = 0; i < Width * Height / 8; i++)
            {
                SendData(blackBuffer[i]);
            }
            Thread.Sleep(2);

            SendCommand(DATA_START_TRANSMISSION_2);
            Thread.Sleep(2);
            for (int i = 0; i < Width * Height / 8; i++)
            {
                SendData(colorBuffer[i]);
            }
            Thread.Sleep(2);

            DisplayFrame();
        }

        public void DisplayFrame()
        {
            SetLut();

            SendCommand(DISPLAY_REFRESH);

            //DelayMs(100);
            //WaitUntilIdle();
        }

        protected void SetPartialWindow(byte[] bufferBlack, byte[] bufferColor, int x, int y, int width, int height)
        {
            SendCommand(PARTIAL_IN);
            SendCommand(PARTIAL_WINDOW);
            SendData(x >> 8);
            SendData(x & 0xf8);     // x should be the multiple of 8, the last 3 bit will always be ignored
            SendData(((x & 0x1f8) + width - 1) >> 8);
            SendData(((x & 0x1f8) + width - 1) | 0x07);
            SendData(y >> 8);
            SendData(y & 0xff);
            SendData((y + height - 1) >> 8);
            SendData((y + height - 1) & 0xff);
            SendData(0x01);         // Gates scan both inside and outside of the partial window. (default) 

            //DelayMs(2);
            SendCommand(DATA_START_TRANSMISSION_1);

            if (bufferBlack != null)
            {
                for (int i = 0; i < width / 8 * height; i++)
                {
                    SendData(bufferBlack[i]);
                }
            }

            //DelayMs(2);
            SendCommand(DATA_START_TRANSMISSION_2);

            if (bufferColor != null)
            {
                for (int i = 0; i < width / 8 * height; i++)
                {
                    SendData(bufferColor[i]);
                }
            }

            //DelayMs(2);
            SendCommand(PARTIAL_OUT);
        }

        ///<summary>method
        ///  <c>SetLutQuick</c>
        ///  set the look-up table for quick display (partial refresh).
        ///</summary>
        protected void SetLut()
        {
            uint count;
            SendCommand(LUT_FOR_VCOM);                            //vcom
            for (count = 0; count < 44; count++)
            {
                SendData(lut_vcom0_quick[count]);
            }

            SendCommand(LUT_WHITE_TO_WHITE);                      //ww --
            for (count = 0; count < 42; count++)
            {
                SendData(lut_ww_quick[count]);
            }

            SendCommand(LUT_BLACK_TO_WHITE);                      //bw r
            for (count = 0; count < 42; count++)
            {
                SendData(lut_bw_quick[count]);
            }

            SendCommand(LUT_WHITE_TO_BLACK);                      //wb w
            for (count = 0; count < 42; count++)
            {
                SendData(lut_wb_quick[count]);
            }

            SendCommand(LUT_BLACK_TO_BLACK);                      //bb b
            for (count = 0; count < 42; count++)
            {
                SendData(lut_bb_quick[count]);
            }
        }
    }
}