using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 http://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace MIDAS_BAT
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class ViewResultPage : Page, INotifyPropertyChanged
    {
        ObservableCollection<TestExecData> testExecList = new ObservableCollection<TestExecData>();
        public ObservableCollection<TestExecData> TestExecList
        {
            get { return testExecList; }
            set { testExecList = value;  }
        }
        public ViewResultPage()
        {
            this.InitializeComponent();

            DatabaseManager dbManager = DatabaseManager.Instance;
            List<TestExec> list = dbManager.GetTextExecs();
            foreach( var item in list)
            {
                Tester tester = dbManager.GetTester(item.TesterId);
                string testerStr = tester.Name + "(" + tester.Gender+ ", " + tester.birthday+ ")";
                TestExecData data = new TestExecData()
                {
                    Id = item.Id,
                    TesterName = testerStr,
                    TesterId = item.TesterId,
                    TestSetId = item.TestSetId,
                    Datetime = item.Datetime
                };
            }
        }

        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {

        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class TestExecData
    {
        public int Id { get; set; }
        public int TesterId { get; set; }
        public string TesterName { get; set; }
        public int TestSetId { get; set; }
        public string Datetime { get; set; }
    }
}
