using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Net;

namespace Lumitech.Interfaces
{
    //FW 9.2.2014
    public enum TuneableWhiteType { None, PILED_MM1, PILED_MM2, CWWW};
    public enum CommInterface { None, Zigbee, ZLL, DMX, DALI, NEOLINK }

    public interface IPILed2
    {
        bool Connect();
        bool Connect(string comport);
        bool Connect(IPAddress startAddress, int UDPPort);
        bool Disconnect();
        bool isConnected { get; }
        void Flash(byte[] b, byte cnt);
        void setBrightness(byte b);
        void setFadeTime(int f);
        void setCCT(Single CCT);
        void setXy(Single[] cie);
        void setRGB(byte[] b);
        CommInterface Interface { get; }
        TuneableWhiteType TWType { get; }
    }

    //FW 10.2.2014 - CW-WW Zigbee Interface to LTEF,LMU ohne Memory Map
    public interface ILTEF: IPILed2
    {
        Single[,] getEckCoords();
        Single[] getCurrGains();
        string getVersion();
        Single getTemperature();
    }

    public interface IMemoryMap: ILTEF
    {
        int MMVersion { get; }
        UInt32 BatchNr { get; }
        Int32 SerialNr { get; }
        bool isMMLoaded { get; }
        void LoadMM(byte ModuleNr);
        void LoadMMbin(ref byte[] buf, byte ModuleNr);
        void LoadAtmelbin(ref byte[] buf);
        //void SaveMM(byte ModuleNr); --> never implemented
        void SaveMMbin(byte[] buf, byte ModuleNr);
        UInt16 CalcChecksum(byte[] buf);
        void DelMM();
        ArrayList PrintMM();
        void Test123();

        //FW 10.4.2013
        /*Single[,] getEckCoords();
        Single[] getCurrGains();
        Single getTemperature();*/
        Single[] calcCurrGains(Single Temperature, Single[] coords);
    }

    struct MMProperties
    {
        public string Name;
        public string[] Value;

        public MMProperties(int dummy)
        {
            Value = new String[4];
            Name = "";
        }
    }
}
