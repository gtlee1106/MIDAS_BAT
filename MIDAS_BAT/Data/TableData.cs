using System;
using SQLite.Net.Attributes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace MIDAS_BAT
{
    public class Tester
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string birthday { get; set; }
        public string Education { get; set; }

        public string GetTesterName(string execDateTime)
        {
            return String.Format("{0}({1}, {2}, 만 {3}세, 교육년수 {4}년)", this.Name, this.Gender, this.birthday,
                    Util.calculateAge(this.birthday, execDateTime), Util.calculateEducation(this.Education));
        }
    }

    public class TestSet : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string SetName { get; set; }

        private bool? active;

        public bool HorizontalLineTest { get; set; }
        public bool VerticalLineTest { get; set; }
        public bool RightCrossLineTest { get; set; }
        public bool LeftCrossLineTest { get; set; }
        public bool CounterClockwiseSpiralTest { get; set; }
        public bool ClockwiseSpiralTest { get; set; }
        public bool CounterClockwiseFreeSpiralTest { get; set; }
        public bool ClockwiseFreeSpiralTest { get; set; }
        public bool TextWritingTest { get; set; }

        public Boolean? Active
        {
            get
            {
                return active;
            }
            set
            {
                active = value;
                NotifyPropertyChanged("Active"); 
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged( [CallerMemberName] String propertyName = "")
        {
            if( PropertyChanged != null )
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class TestSetItem 
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TestSetId { get; set; }
        public string Word { get; set; }
        public int Number { get; set; }
    }

    public class TestExec
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TesterId { get; set; }
        public int TestSetId { get; set; }
        public string Datetime { get; set; }
        public bool UseJamoSepartaion { get; set; }
        public bool ShowBorder { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight{ get; set; }
        public string FontName { get; set; }
        public int FontSize { get; set; }
        public bool HasTimeLimit { get; set; }
        public int TimeLimit { get; set; }

        public string getExecDateTimeStr()
        {
            string execDatetime = this.Datetime.Substring(0, 4) + "." +
                                      this.Datetime.Substring(4, 2) + "." +
                                      this.Datetime.Substring(6, 2) + " " +
                                      this.Datetime.Substring(9, 2) + ":" +
                                      this.Datetime.Substring(11, 2) + ":" +
                                      this.Datetime.Substring(13, 2);
            return execDatetime;
        }
    }

    public class TestExecResult
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TestExecId { get; set; }
        public int TestSetItemId { get; set; }
        public int TestSetItemCharIdx { get; set; }
        public double ChosungTime { get; set; }
        public double ChosungAvgPressure { get; set; }
        public double FirstIdleTIme { get; set; }
        public double JoongsungTime { get; set; }
        public double JoongsungAvgPressure { get; set; }
        public double SecondIdelTime { get; set; }
        public double JongsungTime { get; set; }
        public double JongsungAvgPressure { get; set; }
        public double ThirdIdleTime { get; set; }
    }
}
