using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumitech.Helpers
{
    enum DTAlgorithm { SUNHEIGHT, PARABEL, LUMITECH };
    enum Season { SPRING=0, SUMMER=1, FALL=2, WINTER=3 };

    //public delegate int CalculateCCTDelegateVoid();
    //public delegate int CalculateCCTDelegateDateTime(DateTime actualTime);
    
    //Berechnung von Sonnenhöhe und Azimuth nach Duffie, Beckman, 1990, Solar engineering of Thermal Processes
    class DaytimeCCT
    {
        

        private const int MIN_CCT = 2700;
        private const int MAX_CCT = 6500;

        public double latitude { get; set; }
        public int minCCT { get; set; }
        public int maxCCT { get; set; }

        private int _CCT;
        public int CCT {
            get { return getCCT();  } 
        }


        //For calculation with sunheight
        private int dayofYear;
        private double declination;
        private double sunheight;   //in degree
        private double rightAscension;
        private double FactorC3;
        private double FactorC1;
        private double azimut;
        private DateTime now;        


        //For parabel calculation
        private int TimeAtCCTMin;
        private Season season;

        static List<List<int>> dtCycleLumitech = new List<List<int>>()
        {
            //Hours * 100
            new List<int>() { 0,100,200,300,400,500,600,700,800,900,1000,1050,1100,1135,1150,1180,1200,1220,1250,1265,1300,1350,1400,1500,1600,1700,1800,1900,2000,2100,2200,2300,2400},
            //Spring
            new List<int>() {2700,2700,2700,2700,2700,2700,3248,3958,4663,5331,5916,6155,6340,6431,6459,6493,6500,6493,6459,6431,6340,6155,5916,5331,4663,3958,3248,2700,2700,2700,2700,2700,2700},
            //Summer
            new List<int>() {2700,2700,2700,2700,2700,2940,3522,4131,4749,5353,5904,6139,6329,6426,6455,6493,6500,6493,6455,6426,6329,6139,5904,5353,4749,4131,3522,2940,2700,2700,2700,2700,2700},
            //Fall
            new List<int>() { 2700,2700,2700,2700,2700,2700,2700,2970,4058,5024,5803,6099,6318,6423,6454,6493,6500,6493,6454,6423,6318,6099,5803,5024,4058,2970,2700,2700,2700,2700,2700,2700,2700},
            //Winter
            new List<int>() {2700,2700,2700,2700,2700,2700,2700,2700,3476,4696,5659,6018,6283,6408,6445,6491,6500,6491,6445,6408,6283,6018,5659,4696,3476,2700,2700,2700,2700,2700,2700,2700,2700}
        };


        /*Dictionary<int, int> Test = new Dictionary<int,int>()
        {
            {0,2700}, {1,2700}
        };*/

        //Delegates
        //CalculateCCTDelegateVoid _getCCT;
        //CalculateCCTDelegateDateTime _getCCT_DateTime;


        private DTAlgorithm _algorithm;
        public DTAlgorithm algorithm
        {
            get { return _algorithm;  }
            set
            {
                _algorithm = value;
            }
        }
        
        
        public DaytimeCCT()
        {
            latitude = 47.0; //ca. Jennersdorf
            minCCT = MIN_CCT;
            maxCCT = MAX_CCT;
            TimeAtCCTMin = 7;
            now = DateTime.Now;

            algorithm = DTAlgorithm.LUMITECH;

            _CCT = _getCCT();
        }


        public DaytimeCCT(double platitude) : this()
        {
            latitude = platitude;

            _CCT = _getCCT();
        }

        public DaytimeCCT(double platitude, DTAlgorithm pAlgo)
            : this()
        {
            latitude = platitude;
            algorithm = pAlgo;

            _CCT = getCCT();
        }


        public int getCCT(DateTime pnow)
        {
            now = pnow;
            _CCT = _getCCT();

            return _CCT;
        }


#region ALGORITHM_SUNHEIGHT
        
        //returns CCT at given latitude and current system time
        public int getCCT(double platitude)
        {
            latitude = platitude;
            now = DateTime.Now;

            _CCT = _getCCT();

            return _CCT;
        }

        //returns CCT at given latitude and given Date/Time
        public int getCCT(double platitude, DateTime pnow)
        {
            latitude = platitude;
            now = pnow;
            _CCT = _getCCT();
            return _CCT;
        }

        private double getDeclination()
        {
            dayofYear = DateTime.Parse(now.ToString()).DayOfYear;
            double ret = 23.45 * Math.Sin(2 * Math.PI * (284.0 + dayofYear) / 365);

            return ret;
        }

        private double getRightAscension()
        {
            double hour = DateTime.Parse(now.ToString()).Hour + DateTime.Parse(now.ToString()).Minute / 60;
            double ret = 15.0 * hour - 180.0;

            if (ret>=0)
                FactorC3 = 1;
            else
                FactorC3 = -1;

            return ret;
        }

        private double getSunheight()
        {
            double ret = Math.Asin(Math.Sin(latitude * Math.PI / 180) * Math.Sin(declination * Math.PI / 180) + Math.Cos(latitude * Math.PI / 180) * Math.Cos(declination * Math.PI / 180) * Math.Cos(rightAscension * Math.PI / 180)) * 180 / Math.PI;
            return ret;
        }

        private double getFactorC1()
        {
            double f1 = Math.Abs(Math.Tan(declination * Math.PI/180)/Math.Tan(latitude * Math.PI/180));
            double f2 = Math.Acos(Math.Tan(declination * Math.PI/180)/Math.Tan(latitude * Math.PI/180)) * 180 / Math.PI;

            double ret = 1;
            if (f1>=1)
                ret=1;
            else if (Math.Abs(rightAscension) < f2)
                ret=1;
            else
                ret=-1;
            
            return ret;
        }

        private double getAzimut()
        {
            double ret = FactorC1 * 1 * Math.Asin(Math.Cos(declination * Math.PI / 180) * Math.Sin(rightAscension * Math.PI / 180) / Math.Cos(sunheight * Math.PI / 180)) * 180 / Math.PI + FactorC3 * (1 - FactorC1 * 1) * 90;
            return ret;
        }

        #endregion

        //returns CCT at actual latitude and current system time
        public int getCCT()
        {
            now = DateTime.Now;

            //FactorC1 = getFactorC1();
            //azimut = getAzimut();
            _CCT = _getCCT();

            return _CCT;
        }


        private int _getCCT()
        {
            int ret = minCCT;
            season = getSeason(now);

            if (algorithm == DTAlgorithm.SUNHEIGHT)
            {
                declination = getDeclination();
                rightAscension = getRightAscension();
                sunheight = getSunheight();

                ret = (int)(minCCT + Math.Abs(maxCCT - minCCT) * sunheight / 90);
            }
            else if (algorithm == DTAlgorithm.PARABEL)
            {       
                
                if (season == Season.SUMMER)     TimeAtCCTMin = 4;     
                else if (season == Season.FALL)  TimeAtCCTMin = 6;     
                else if (season == Season.WINTER)TimeAtCCTMin = 7;    
                else //if (season ==Season.SPRING) 
                    TimeAtCCTMin = 5;     //Frühling              
                
                //Parabelgleichung y= a * (x-x0)^2 + y0

                double hour = DateTime.Parse(now.ToString()).Hour + DateTime.Parse(now.ToString()).Minute / 60;
                double a = (minCCT - maxCCT) / Math.Pow(TimeAtCCTMin - 12.0,2);
                ret = (int) (a *  Math.Pow(hour - 12.0,2) + maxCCT);
            }
            else //(algorithm == DTAlgorithm.LUMITECH)
            {
                ret = InterpolateCCT();
            }


            if (ret < minCCT) ret = minCCT;
            if (ret > maxCCT) ret = maxCCT;

            return ret;
        }

        private Season getSeason(DateTime date)
        {
            float value = (float)date.Month + date.Day / 100;   // <month>.<day(2 digit)>
            if (value < 3.21 || value >= 12.22) return Season.WINTER;   // Winter
            if (value < 6.21) return Season.SPRING; // Spring
            if (value < 9.23) return Season.SUMMER; // Summer
            return Season.FALL;   // Autumn
        }

        private int InterpolateCCT()
        {
            double ret = minCCT;
            double hour = (DateTime.Parse(now.ToString()).Hour + (double)DateTime.Parse(now.ToString()).Minute / 60.0) * 100;
            int ihour = (int)hour % 2400;
            int index = -1;

           for(int i=0; i<dtCycleLumitech[0].Count; i++)
           {
               if (dtCycleLumitech[0][i] >= ihour)
               {
                   index = i-1;
                   break;
               }
           }

           if ((index > -1) && (index < dtCycleLumitech[0].Count-2))
           {
               ret = linear(hour, dtCycleLumitech[0][index], dtCycleLumitech[0][index + 1], (double)dtCycleLumitech[(int)(season +1)][index], (double)dtCycleLumitech[(int)(season +1)][index + 1]);
           }

           return (int)ret;
        }

        private double linear(double x, double x0, double x1, double y0, double y1)
        {
            if ((x1 - x0) == 0)
            {
                return (y0 + y1) / 2;
            }
            return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
        }
       
    }
}
