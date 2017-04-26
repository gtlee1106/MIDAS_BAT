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
        ObservableCollection<ViewResultDataDetailData> testExecResultList = new ObservableCollection<ViewResultDataDetailData>();
        public ObservableCollection<ViewResultDataDetailData> TestExecResultList
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
                List<TestExecResult> list = dbManager.GetTextExecResults(m_testExec.Id);
                foreach (var result in list)
                {
                    TestSetItem setItem = dbManager.GetTestSetItem(result.TestSetItemId);

                    ViewResultDataDetailData data = new ViewResultDataDetailData()
                    {
                        Word = setItem.Word,
                        ChosungTime = result.ChosungTime,
                        FirstIdleTIme = result.FirstIdleTIme,
                        JoongsungTime = result.JoongsungTime,
                        SecondIdelTime = result.SecondIdelTime,
                        JongSungTime = result.JongsungTime
                    };

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

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
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

    public class ViewResultDataDetailData
    {
        public string Word { get; set; }
        public double ChosungTime { get; set; }
        public double FirstIdleTIme { get; set; }
        public double JoongsungTime { get; set; }
        public double SecondIdelTime { get; set; }
        public double JongSungTime { get; set; }
    }

}
