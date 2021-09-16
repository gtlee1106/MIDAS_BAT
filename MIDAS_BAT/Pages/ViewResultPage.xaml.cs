using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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

            try
            {
                DatabaseManager dbManager = DatabaseManager.Instance;
                List<TestExec> list = dbManager.GetTextExecs(true);
                foreach (var item in list)
                {
                    Tester tester = dbManager.GetTester(item.TesterId);
                    string execDatetime = item.Datetime.Substring(0, 4) + "." +
                                          item.Datetime.Substring(4, 2) + "." +
                                          item.Datetime.Substring(6, 2) + " " +
                                          item.Datetime.Substring(9, 2) + ":" +
                                          item.Datetime.Substring(11, 2) + ":" +
                                          item.Datetime.Substring(13, 2);

                    string testerStr = String.Format("{0}({1}, {2}, 만 {3}세, 교육년수 {4}년)", tester.Name, tester.Gender, tester.birthday,
                        Util.calculateAge(tester.birthday, item.Datetime), Util.calculateEducation(tester.Education));

                    TestExecData data = new TestExecData()
                    {
                        Id = item.Id,
                        TesterName = testerStr,
                        TesterId = item.TesterId,
                        TestSetId = item.TestSetId,
                        ExecDatetime = execDatetime,
                        Selected = false
                    };

                    testExecList.Add(data);
                }

                NotifyPropertyChanged();
            }
            catch (Exception e)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(e.ToString());
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Encoding encoding = Encoding.GetEncoding("euc-kr");

                string logFileName = String.Format("log_{0}_{1}.txt", "ViewResultPage", DateTime.Now.ToString());

                byte[] fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());
                StorageFolder orgFolder = ApplicationData.Current.LocalFolder;
                IAsyncOperation<StorageFile> resultFile = orgFolder.CreateFileAsync(logFileName, CreationCollisionOption.ReplaceExisting);

                FileIO.WriteBytesAsync(resultFile.GetResults(), fileBytes);
            }
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

        private async void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedTestExecData = (sender as FrameworkElement).Tag as TestExecData;

            await Util.SaveResult(selectedTestExecData.Id);
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

        private async void saveSelectedBtn_Click(object sender, RoutedEventArgs e)
        {
            List<int> selectedTestExecList = new List<int>();
            foreach (var item in testExecList)
            {
                if (item.Selected == true)
                    selectedTestExecList.Add(item.Id);
            }
            await Util.SaveResults(selectedTestExecList);
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

        private void selectAllChk_Click(object sender, RoutedEventArgs e)
        {
            bool? allCheck = selectAllChk.IsChecked;
            for (int i = 0; i < testExecList.Count; ++i)
            {
                testExecList[i].Selected = allCheck;
            }

            NotifyPropertyChanged();
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
        public string ExecDatetime { get; set; }

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
