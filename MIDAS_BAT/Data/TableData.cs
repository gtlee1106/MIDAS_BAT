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

        public string GetTesterName(bool useBirthday= false, bool useGender = false, bool useEducation = false )
        {
            StringBuilder builder = new StringBuilder(Name);
            bool useParenthesis = useBirthday || useGender || useEducation;

            if (useParenthesis)
                builder.Append("( ");

            if (useBirthday)
                builder.Append(birthday).Append(" ");

            if (useGender)
                builder.Append(Gender).Append(" ");

            if (useEducation)
                builder.Append(Education).Append(" ");

            if(useParenthesis)
                builder.Append(")");

            return builder.ToString();
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
