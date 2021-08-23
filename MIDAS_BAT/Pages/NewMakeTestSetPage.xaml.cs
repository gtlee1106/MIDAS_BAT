using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 http://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace MIDAS_BAT
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class NewMakeTestSetPage : Page
    {
        private const int MAX_ITEM_NUM = 100;
        private bool m_updateMode = false;
        private TestSet m_targetTestSet = new TestSet();
        private ObservableCollection<TestSetItem> m_testSetItemList = new ObservableCollection<TestSetItem>();
        private ObservableCollection<TestSetItem> TestSetItemList
        {
            get
            {
                return m_testSetItemList;
            }
            set
            {
                m_testSetItemList = value;
            }
        }

        public NewMakeTestSetPage()
        {
            this.InitializeComponent();
            for (int i = 1; i <= MAX_ITEM_NUM; ++i)
            {
                m_testSetItemList.Add(new TestSetItem()
                {
                    Number = i,
                    Word = ""
                });
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is TestSet)
            {
                m_targetTestSet = e.Parameter as TestSet;

                m_updateMode = true;

                DatabaseManager dbManager = DatabaseManager.Instance;
                List<TestSetItem> items = dbManager.GetTestSetItems(m_targetTestSet.Id);

                testSetName.Text = m_targetTestSet.SetName;
                foreach(var item in items)
                    TestSetItemList.ElementAt(item.Number - 1).Word = item.Word;

                add.Content = "수정하기";

                checkHorizontalLineTest.IsChecked = m_targetTestSet.HorizontalLineTest;
                checkVerticalLineTest.IsChecked = m_targetTestSet.VerticalLineTest;
                checkCounterClockwiseSpiralTest.IsChecked = m_targetTestSet.CounterClockwiseSpiralTest;
                checkClockwiseSpiralTest.IsChecked = m_targetTestSet.ClockwiseSpiralTest;
                checkCounterClockwiseFreeSpiralTest.IsChecked = m_targetTestSet.CounterClockwiseFreeSpiralTest;
                checkClockwiseFreeSpiralTest.IsChecked = m_targetTestSet.ClockwiseFreeSpiralTest;
                checkTextWriting.IsChecked = m_targetTestSet.TextWritingTest;
            }
            base.OnNavigatedTo(e);
        }

        private bool IsAllHangul()
        {
            foreach (var item in TestSetItemList)
            {
                string str = item.Word.Trim();
                if (item.Word.Length != 0 && !CharacterUtil.IsHangul(str)) 
                {
                    return false;
                }
            }
            return true;
        }

        private async void add_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAllHangul())
            {
                var dialog = new MessageDialog("단어에 한글이 아닌 것이 포함되어있습니다. 확인해주십시오.");
                var res = await dialog.ShowAsync();
                return;
            }

            int writingItemCount = 0;
            foreach (var item in TestSetItemList)
            {
                if (item.Word.Length == 0)
                    continue;
                writingItemCount += 1;
            }
            if ( checkTextWriting.IsChecked == true && writingItemCount == 0 )
            {
                var dialog = new MessageDialog("글자 쓰기 테스트를 활성화했으나, 단어가 없습니다. 확인해주십시오.");
                var res = await dialog.ShowAsync();
                return;
            }

            itemList.CompleteViewChange();
            itemList.UpdateLayout();

            // data 추가
            DatabaseManager databaseManager = DatabaseManager.Instance;

            TestSet curSet = null;
            if (m_updateMode)
            {
                curSet = m_targetTestSet;

                List<TestSetItem> list = databaseManager.GetTestSetItems(m_targetTestSet.Id);
                foreach (var item in list)
                {
                    databaseManager.DeleteTestSetItem(item);
                }

                curSet.SetName = testSetName.Text.Trim();
                databaseManager.UpdateTestSet(curSet);
            }
            else
            {
                m_targetTestSet.SetName = testSetName.Text.Trim();
                m_targetTestSet.Active = false;

                m_targetTestSet.HorizontalLineTest = (bool)checkHorizontalLineTest.IsChecked;
                m_targetTestSet.VerticalLineTest = (bool)checkVerticalLineTest.IsChecked;
                m_targetTestSet.CounterClockwiseSpiralTest = (bool)checkCounterClockwiseSpiralTest.IsChecked;
                m_targetTestSet.ClockwiseSpiralTest = (bool)checkClockwiseSpiralTest.IsChecked;
                m_targetTestSet.CounterClockwiseFreeSpiralTest = (bool)checkCounterClockwiseFreeSpiralTest.IsChecked;
                m_targetTestSet.ClockwiseFreeSpiralTest = (bool)checkClockwiseFreeSpiralTest.IsChecked;
                m_targetTestSet.TextWritingTest = (bool)checkTextWriting.IsChecked;

                curSet = m_targetTestSet;
                
                databaseManager.InsertTestSet(curSet);
            }

            foreach (var item in TestSetItemList)
            {
                if (item.Word.Length == 0)
                    continue;

                databaseManager.InsertTestSetItem(
                    new TestSetItem()
                    {
                        TestSetId = curSet.Id,
                        Number = item.Number,
                        Word = item.Word.Trim()
                    }
                );
            }

            var test = itemList.Items;

            // Active 된 TestSet이 없다면 이 set을 active 시킴
            TestSet activeSet = databaseManager.GetActiveTestSet();
            if (activeSet == null)
                databaseManager.SetActive(curSet);

            this.Frame.GoBack();
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private void checkHorizontalLineTest_Checked(object sender, RoutedEventArgs e)
        {
            if (checkHorizontalLineTest == null)
                return;

            m_targetTestSet.HorizontalLineTest = (bool)checkHorizontalLineTest.IsChecked;
        }

        private void checkVerticalLineTest_Checked(object sender, RoutedEventArgs e)
        {
            if (checkVerticalLineTest == null)
                return;

            m_targetTestSet.VerticalLineTest = (bool)checkVerticalLineTest.IsChecked;
        }

        private void checkCounterClockwiseSpiralTest_Checked(object sender, RoutedEventArgs e)
        {
            if (checkCounterClockwiseSpiralTest == null)
                return;

            m_targetTestSet.CounterClockwiseSpiralTest = (bool)checkCounterClockwiseSpiralTest.IsChecked;
        }

        private void checkClockwiseSpiralTest_Checked(object sender, RoutedEventArgs e)
        {
            if (checkClockwiseSpiralTest == null)
                return;

            m_targetTestSet.ClockwiseSpiralTest = (bool)checkClockwiseSpiralTest.IsChecked;
        }

        private void checkCounterClockwiseFreeSpiralTest_Checked(object sender, RoutedEventArgs e)
        {
            if (checkCounterClockwiseFreeSpiralTest == null )
                return;

            m_targetTestSet.CounterClockwiseFreeSpiralTest = (bool)checkCounterClockwiseFreeSpiralTest.IsChecked;
        }

        private void checkClockwiseFreeSpiralTest_Checked(object sender, RoutedEventArgs e)
        {
            if ( checkClockwiseFreeSpiralTest == null )
                return;

            m_targetTestSet.ClockwiseFreeSpiralTest = (bool)checkClockwiseFreeSpiralTest.IsChecked;
        }

        private void checkTextWriting_Checked(object sender, RoutedEventArgs e)
        {
            if (checkTextWriting == null || itemList == null )
                return;

            m_targetTestSet.TextWritingTest = (bool)checkTextWriting.IsChecked;
            itemList.Visibility = m_targetTestSet.TextWritingTest ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
