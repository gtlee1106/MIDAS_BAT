using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class ViewResultPage : Page, INotifyPropertyChanged
    {
        ObservableCollection<TestExecData> testExecList = new ObservableCollection<TestExecData>();
        public ObservableCollection<TestExecData> TestExecList
        {
            get { return testExecList; }
            set
            {
                if( testExecList != value )
                {
                    testExecList = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ViewResultPage()
        {
            this.InitializeComponent();

            DatabaseManager dbManager = DatabaseManager.Instance;
            List<TestExec> list = dbManager.GetTextExecs(true);
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
                    Datetime = item.Datetime,
                    Selected = false
                };

                testExecList.Add(data);
            }

            NotifyPropertyChanged();
        }

        private async void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            bool delete = await Util.ShowDeleteAlertDlg();
            if (!delete)
                return;

            var selectedTestExecData = (sender as FrameworkElement).Tag as TestExecData;

            DatabaseManager dbManager = DatabaseManager.Instance;
            dbManager.DeleteTestExec(selectedTestExecData.Id);

            // itemsource 갱신
            testExecList.Remove(selectedTestExecData);

            //리스트뷰 갱신이 필요함 음...            
            NotifyPropertyChanged();
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void testExecListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            TestExecData item = e.ClickedItem as TestExecData;
            this.Frame.Navigate(typeof(ViewResultDetailPage), item);
        }

        private void selectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            for( int i = 0; i < testExecList.Count; ++i )
            {
                testExecList[i].Selected = true;
            }

            NotifyPropertyChanged();
        }

        private void saveSelectedBtn_Click(object sender, RoutedEventArgs e)
        {
            List<int> selectedTestExecList = new List<int>();
            foreach (var item in testExecList)
            {
                if (item.Selected == true)
                    selectedTestExecList.Add(item.Id);
            }
            Util.SaveResults(selectedTestExecList);
        }

        private async void deleteSelectedBtn_Click(object sender, RoutedEventArgs e)
        {
            bool delete = await Util.ShowDeleteAlertDlg();
            if (!delete)
                return;

            DatabaseManager dbManager = DatabaseManager.Instance;

            List<TestExecData> delTargets = new List<TestExecData>();
            foreach(var item in testExecList)
            {
                if (item.Selected != true)
                    continue;
                dbManager.DeleteTestExec(item.Id);
                delTargets.Add(item);
            }

            foreach (var item in delTargets)
                testExecList.Remove(item);
            NotifyPropertyChanged();

            //리스트뷰 갱신이 필요함 음...            
        }

        private void selectChk_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class TestExecData : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int TesterId { get; set; }
        private bool? selected;
        public Boolean? Selected
        {
            get
            {
                return selected;
            }
            set
            {
                selected = value;
                NotifyPropertyChanged();
            }
        }
        public string TesterName { get; set; }
        public int TestSetId { get; set; }
        public string Datetime { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
