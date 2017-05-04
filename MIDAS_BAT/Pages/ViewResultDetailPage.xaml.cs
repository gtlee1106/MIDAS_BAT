using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
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
    public sealed partial class ViewResultDetailPage : Page, INotifyPropertyChanged
    {
        ObservableCollection<TestExecResultDetailData> testExecResultList = new ObservableCollection<TestExecResultDetailData>();
        public ObservableCollection<TestExecResultDetailData> TestExecResultList
        {
            get { return testExecResultList; }
            set { testExecResultList = value; }
        }

        TestExec m_testExec;
        
        public ViewResultDetailPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            DatabaseManager dbManager = DatabaseManager.Instance;
            if (e.Parameter is TestExecData)
            {
                TestExecData data = e.Parameter as TestExecData;
                if( data != null )
                    m_testExec = dbManager.GetTestExec(data.Id);
            }

            if (m_testExec != null)
            {
                List<TestSetItem> testSetItemList = dbManager.GetTestSetItems(m_testExec.TestSetId);
                foreach (var testSetItem in testSetItemList)
                {

                    TestExecResultDetailData data = new TestExecResultDetailData()
                    {
                        TargetWord = testSetItem.Word,
                    };
                    data.DetailSubData = new ObservableCollection<TestExecResultDetailSubData>();
                    
                    List<TestExecResult> list = dbManager.GetTextExecResults(m_testExec.Id, testSetItem.Id);
                    foreach(var result in list)
                    {
                        TestExecResultDetailSubData subData = new TestExecResultDetailSubData()
                        {
                            Char = data.TargetWord.ElementAt(result.TestSetItemCharIdx).ToString(),
                            ChosungTime = result.ChosungTime.ToString("F3"),
                            JoongsungTime = result.JoongsungTime.ToString("F3"),
                            JongsungTime = result.JongsungTime.ToString("F3"),
                            FirstIdleTime = result.FirstIdleTIme.ToString("F3"),
                            SecondIdleTime = result.SecondIdelTime.ToString("F3"),
                            ChosungAvgPressure = result.ChosungAvgPressure.ToString("F6"),
                            JoongsungAvgPressure = result.JoongsungAvgPressure.ToString("F6"),
                            JongsungAvgPressure = result.JongsungAvgPressure.ToString("F6"),
                        };
                        data.DetailSubData.Add(subData);
                    }

                    TestExecResultList.Add(data);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private async void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            await Util.SaveResult(m_testExec.Id);
        }

        private async void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            bool delete = await Util.ShowDeleteAlertDlg();
            if (!delete )
                return;

            DatabaseManager dbManager = DatabaseManager.Instance;
            dbManager.DeleteTestExec(m_testExec);

            this.Frame.GoBack();
        }
    }

    public class TestExecResultDetailData
    {
        public string TargetWord { get; set; }

        public ObservableCollection<TestExecResultDetailSubData> DetailSubData { get; set; }
        
    }

    public class TestExecResultDetailSubData
    {
        public string Char { get; set; }
        public string ChosungTime { get; set; }
        public string FirstIdleTime { get; set; }
        public string JoongsungTime { get; set; }
        public string SecondIdleTime { get; set; }
        public string JongsungTime { get; set; }
        public string ChosungAvgPressure { get; set; }
        public string JoongsungAvgPressure { get; set; }
        public string JongsungAvgPressure { get; set; }
    }

}
