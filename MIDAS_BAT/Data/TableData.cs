using System;
using SQLite.Net.Attributes;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
    }

    public class TestSet : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string SetName { get; set; }

        private bool? active;
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
        public bool ShowBorder { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight{ get; set; }
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
