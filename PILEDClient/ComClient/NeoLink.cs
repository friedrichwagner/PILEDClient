using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using Lumitech.Helpers;
using System.Net.Sockets;
using System.Net;
using Lumitech.Interfaces;

namespace Lumitech.Interfaces
{
    public enum NEOLINK_INTERFACE : byte
    {
        NONE,
        USB,
        UDP
    }

    enum NeoLinkGroups : byte
    {
	    NL_GROUP_BROADCAST = 0,
	    NL_GROUP_1 = 201,
	    NL_GROUP_2 = 202,
	    NL_GROUP_3 = 203,
	    NL_GROUP_4 = 204,
	    NL_GROUP_5 = 205,
	    NL_GROUP_6 = 206,
	    NL_GROUP_7 = 207,
	    NL_GROUP_8 = 208,
	    NL_GROUP_9 = 209,
	    NL_GROUP_10 = 210,
	    NL_GROUP_11 = 211,
	    NL_GROUP_12 = 212,
	    NL_GROUP_13 = 213,
	    NL_GROUP_14 = 214,
	    NL_GROUP_15 = 215,
	    NL_GROUP_16 = 216
    };

    public enum NeoLinkMode : byte
    {
	    //ZLL
	    NL_STARTUP = 1,
	    NL_NETWORK_STATUS = 2,
	    NL_NETWORK = 3,
	    NL_IDENTIFY = 4,
	    NL_GROUPCONFIG = 5,
	    NL_GROUP_REFRESH_ADDRESS = 6,
	    NL_GROUP_REFRESH_NAME = 7,
	    NL_BRIGHTNESS = 10,
	    NL_CCT = 11,
	    NL_RGB = 12,
	    NL_SCENES = 13,
	    NL_SEQUENCES_CALL = 14,
	    NL_SEQUENCES_SET = 15,
	    NL_XY = 16			//tbd
    };

    enum NeoLinkSubMode_NETWORK : byte
    {
	    //ZLL
	    NL_NETWORK_TOUCHLINK = 0,
	    NL_NETWORK_RESETNEW = 1,
	    NL_NETWORK_CLASSICAL = 2,
	    NL_NETWORK_RELEASELAMP = 3
    };

    enum NeoLinkSubMode_SCENES : byte
    {
	    //ZLL
	    NL_SCENES_ADD = 0,
	    NL_SCENES_DELETE = 1,
	    NL_SCENES_DELETE_ALL = 2,
	    NL_SCENES_CALL = 3
    };

    struct NeoLinkData
    {
        private const int NL_BUFFER_SIZE=30;
        private const int DATA_SIZE= 24;

        public byte byStart;
        public byte byMode;
        public byte byAddress;

        public byte[] data;

	    public byte byGroupUpdate;
	    public byte byCRC;
	    public byte byStop;        

        public byte[] byArrBuffer;

        public static NeoLinkData NewFrame()
        {
            NeoLinkData nlFrame = new NeoLinkData();

            nlFrame.byStart = 0x02;
            nlFrame.byMode = 0x00;
            nlFrame.byAddress = 0x00;

            nlFrame.byGroupUpdate = 0x00;
            nlFrame.byCRC = 0x00;
            nlFrame.byStop = 0x03;
            
            nlFrame.data = new Byte[DATA_SIZE]; 
            nlFrame.byArrBuffer = new Byte[NL_BUFFER_SIZE];

            return nlFrame;
        }

        public byte[] ToByteArray()
        {
            int i = 0;

            byArrBuffer[i++] = byStart;
            byArrBuffer[i++] = byMode;
            byArrBuffer[i++] = byAddress;

            for (int k = 0; k < DATA_SIZE; k++) byArrBuffer[i++] = data[k];

            byArrBuffer[i++] = byGroupUpdate;
            byArrBuffer[i++] = calcCRC();
            byArrBuffer[i++] = byStop;

            return byArrBuffer;
        }

        private byte calcCRC()
        {
            int num = 24; // CRC Berechnung von Byte 4(=Brightness) bis 27 (=vor CRC Byte)
            int j, k, crc8, m, x;

            crc8 = 0;
            //Achtung: m startet mit 4 (Brightness), nicht 0
            for (m = 4; m < num; m++)
            {
                x = byArrBuffer[m];
                for (k = 0; k < 8; k++)
                {
                    j = 1 & (x ^ crc8);
                    crc8 = (crc8 / 2) & 0xFF;
                    x = (x / 2) & 0xFF;
                    if (j != 0)
                        crc8 = crc8 ^ 0x8C;
                }
            }
            byCRC = (byte)crc8;

            return byCRC;
        }

    }

    class NeoLink : IPILed//, IObserver<PILEDData>
    {
        private NeoLinkData nlFrame = NeoLinkData.NewFrame();
        private Object thisLock = new Object();

        UdpClient udpclient = new UdpClient();
        private const int UDPPort = 1025; // PortNr of NeoLinkBox = 1025
        NEOLINK_INTERFACE enumIF;
        private IDisposable cancellation;

        private SerialPort serial;
        private const int BAUDRATE = 19200;
        private const int DATABITS = 8;
        private const int RECVARRAYSIZE = 30;

        private byte[] recvDataArray = new byte[RECVARRAYSIZE];

        private byte lastBrightness;
        private Single lastCCT = 3000;
        private Single[] lastXy = new Single[2];
        private byte[] lastRGB = new byte[3];
        private int fadetime = 0;
        public int FadingTime       //hier in ms
        {
            get { return fadetime*100; }    
            set { 
                fadetime = value;
                byFadetime = BitConverter.GetBytes((UInt16)(fadetime / 100));
            }
        }

        private byte[] byFadetime = new byte[2] {0,0};

        public NeoLink()
        {
            Settings ini = Settings.GetInstance();
            fadetime = ini.Read<int>("NEOLINK", "Fadetime", 0);
            enumIF = NEOLINK_INTERFACE.NONE;

            string udpaddressString = ini.Read<string>("NEOLINK", "UDP-Address", "");
            IPAddress udpaddress;
            if (IPAddress.TryParse(udpaddressString, out udpaddress))
            {
                int portnr= ini.Read<int>("NEOLINK", "UDP-Port", UDPPort);
                Connect(udpaddressString, portnr.ToString());
            }                        
        }

        #region IPILed-Interface
        public bool Connect(string portname)
        {
            serial = new SerialPort();

            serial.PortName = portname;
            serial.BaudRate = BAUDRATE;
            serial.Parity = Parity.None;
            serial.DataBits = DATABITS;
            serial.StopBits = StopBits.One;
            serial.Open();

            serial.WriteTimeout = 2000;
            serial.ReadTimeout = 2000;

            enumIF = NEOLINK_INTERFACE.USB;

            return serial.IsOpen;
        }

        public bool Connect(string IPAddress, string PortNr)
        {
            enumIF = NEOLINK_INTERFACE.UDP;

            udpclient.Connect(IPAddress, Int16.Parse(PortNr));

            return true;
        }

        public bool Disconnect()
        {
            if (enumIF == NEOLINK_INTERFACE.USB)
            {
                serial.Close();
                return serial.IsOpen;
            }

            return true;            
        }

        public CommInterface Interface
        {
            get { return CommInterface.NEOLINK; }
        }

        public TuneableWhiteType TWType
        {
            get { return TuneableWhiteType.PILED_MM2; }
        }

        public bool isConnected
        {
            get { return serial.IsOpen; }
        }


        public void Flash(byte[] b, byte turns)
        {
            byte localBrightness = lastBrightness;
            for (int i = 0; i < turns; i++)
            {
                setBrightness(0);
                Thread.Sleep(200);
                setBrightness(255);
                Thread.Sleep(200);
            }

            setBrightness(localBrightness);
        }

        public void setBrightness(byte val)
        {
            nlFrame.byMode = (byte)NeoLinkMode.NL_BRIGHTNESS;

            //brightness
            nlFrame.data[0] = val;

            //fadetime            
            nlFrame.data[1] = byFadetime[0];
            nlFrame.data[2] = byFadetime[1];

            Send();
            lastBrightness = val;
        }

        public void setBrightness(byte[] b)
        {
            setRGB(b);
        }

        public void setBrightnessOneModule(byte[] b, byte notused)
        {
            setBrightness(b);
        }

        public void setCCT(Single CCT, byte brightness)
        {
            nlFrame.byMode = (byte)NeoLinkMode.NL_CCT;

	        //cct in mirek
	        int mirek = (int)(1e6 / CCT);

            byte[] b2 = BitConverter.GetBytes((UInt16)(mirek));

            nlFrame.data[0] = b2[0];
            nlFrame.data[1] = b2[1];

            //fadetime            
            nlFrame.data[2] = byFadetime[0];
            nlFrame.data[3] = byFadetime[1];

            Send();

            lastCCT = CCT;
        }

        public void setXy(Single[] cie, byte brightness)
        {
            nlFrame.byMode = (byte)NeoLinkMode.NL_XY;

            byte[] b2 = BitConverter.GetBytes((UInt16)(65536 * cie[0]));
            nlFrame.data[0] = b2[0];
            nlFrame.data[1] = b2[1];

            b2 = BitConverter.GetBytes((UInt16)(65536 * cie[1]));
            nlFrame.data[2] = b2[0];
            nlFrame.data[3] = b2[1];

            Send();

            lastXy = cie;
        }


        public void setRGB(byte[] b)
        {
            nlFrame.byMode = (byte)NeoLinkMode.NL_RGB;           

            nlFrame.data[0] = b[0];
            nlFrame.data[1] = b[1];
            nlFrame.data[2] = b[2];

            Send();

            lastRGB = b;

        }
        #endregion

        private void Send(bool waitReceive = false)
        {
            lock (thisLock)
            {
                nlFrame.ToByteArray();

                if (enumIF == NEOLINK_INTERFACE.USB)
                    serial.Write(nlFrame.byArrBuffer, 0, nlFrame.byArrBuffer.Length);
                else if (enumIF == NEOLINK_INTERFACE.UDP)
                {
                    udpclient.Send(nlFrame.byArrBuffer, nlFrame.byArrBuffer.Length);
                    Thread.Sleep(200);
                }

                if (waitReceive)
                    Receive();
            }
        }

        private void Receive()
        {
            //tbd
        }

#region Observer Pattern
        /*
        public virtual void Subscribe(Object provider)
        {
            Settings ini = Settings.GetInstance();

            cancellation = provider.Subscribe(this);

            //First see, if we have a NeoLink Box
            string strUDPAddress = ini.Read<string>("NOELINK", "UDP_Address", "");
            if (strUDPAddress.Length>0)
                Connect(strUDPAddress, UDPPort.ToString());

            string[] strComport = ini.Read<string>("NOELINK", "USBCom", "").Split(',');
            if (strComport.Length > 0)
                Connect(strComport[0]);

        }

        public virtual void Unsubscribe()
        {
            cancellation.Dispose();
        }

        //Called from UDP Server when closing application
        public virtual void OnCompleted()
        {
            Disconnect();
        }

        public virtual void OnError(Exception e)
        {

        }

        //Called from UDP Server when new data arrive
        public virtual void OnNext(PILEDData info)
        {
            nlFrame.byAddress = (byte)info.groupid;

            switch (info.mode)
            {
                case PILEDMode.PILED_SET_BRIGHTNESS:
                    this.setBrightness((byte)info.brightness);
                    break;
                case PILEDMode.PILED_SET_CCT:
                    this.setCCT(info.cct, (byte)info.brightness);
                    break;
                case PILEDMode.PILED_SET_XY:
                    float[] f = new float[2];
                    f[0] = (float)info.xy[0]; f[1] = (float)info.xy[1];
                    this.setXy(f, (byte)info.brightness);
                    break;
                case PILEDMode.PILED_SET_RGB:
                    byte[] b = new byte[3];
                    b[0] = (byte)info.rgb[0]; b[1] = (byte)info.rgb[1]; b[2] = (byte)info.rgb[2];
                    this.setRGB(b);
                    break;
            }
        }
        */
 #endregion
    }
}
