using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumitech.Helpers;
using System.Threading;
using System.Collections.Concurrent;

namespace Lumitech.Interfaces
{
    public enum PILEDMode
    {
        PILED_SET_BRIGHTNESS = 1,
        PILED_SET_CCT = 2,
        PILED_SET_XY = 3,
        PILED_SET_RGB = 4,
        PILED_SET_LOCKED = 99,
    };

    public enum LLMsgType
    {
        LL_SET_LIGHTS = 10,
        LL_CALL_SCENE = 20,
        LL_START_TESTSEQUENCE = 30,
        LL_STOP_TESTSEQUENCE = 31,
        LL_PAUSE_TESTSEQUENCE = 32,
        LL_NEXT_TESTSEQUENCE_STEP = 33,
        LL_PREV_TESTSEQUENCE_STEP = 34
    };

    public class PILEDData
    {
        public PILEDMode mode;
        public int groupid;
        public int cct;
        public int brightness;
        public double[] xy = new double[2];
        public int[] rgb = new int[3];
        public string sender;
        public string receiver;
        public LLMsgType msgtype;

        public PILEDData() { }
    }

    public class LightLifeData
    {
        public int roomid;
        public int userid;
        public int vlid;
        public int sceneid;
        public int sequenceid;
        public int stepid;
        public string remark;

        public PILEDData piled;

        public LightLifeData()  { }
    }

    internal class UnsubscriberPILEDData<PILEDData> : IDisposable
    {
        private List<IObserver<PILEDData>> _observers;
        private IObserver<PILEDData> _observer;

        internal UnsubscriberPILEDData(List<IObserver<PILEDData>> observers, IObserver<PILEDData> observer)
        {
            this._observers = observers;
            this._observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }

    internal class UnsubscriberLightLifeData<LightLifeData> : IDisposable
    {
        private List<IObserver<LightLifeData>> _observers;
        private IObserver<LightLifeData> _observer;

        internal UnsubscriberLightLifeData(List<IObserver<LightLifeData>> observers, IObserver<LightLifeData> observer)
        {
            this._observers = observers;
            this._observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }
}
