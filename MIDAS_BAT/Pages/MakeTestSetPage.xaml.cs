using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    public sealed partial class MakeTestSetPage : Page
    {
        private FullTestSet m_testSet = new FullTestSet();
        private bool m_updateMode = false;
        private TestSet m_targetTestSet;

        public MakeTestSetPage()
        {
            this.InitializeComponent();
            this.DataContext = m_testSet;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if( e.Parameter is TestSet )
            {
                m_targetTestSet = e.Parameter as TestSet;

                m_updateMode = true;

                DatabaseManager dbManager = DatabaseManager.Instance;
                List<TestSetItem> items = dbManager.GetTestSetItems(m_targetTestSet.Id);

                m_testSet.Title = m_targetTestSet.SetName;
                m_testSet.Word1 = items[0].Word;
                m_testSet.Word2 = items[1].Word;
                m_testSet.Word3 = items[2].Word;
                m_testSet.Word4 = items[3].Word;
                m_testSet.Word5 = items[4].Word;
                m_testSet.Word6 = items[5].Word;
                m_testSet.Word7 = items[6].Word;
                m_testSet.Word8 = items[7].Word;
                m_testSet.Word9 = items[8].Word;
                m_testSet.Word10 = items[9].Word;

                testSetName.Text = m_targetTestSet.SetName;
                word1.Text = items[0].Word;
                word2.Text = items[1].Word;
                word3.Text = items[2].Word;
                word4.Text = items[3].Word;
                word5.Text = items[4].Word;
                word6.Text = items[5].Word;
                word7.Text = items[6].Word;
                word8.Text = items[7].Word;
                word9.Text = items[8].Word;
                word10.Text = items[9].Word;
            }
            base.OnNavigatedTo(e);
        }

        private async void add_Click(object sender, RoutedEventArgs e)
        {
            if ( (m_testSet.Word1 != null && !CharacterUtil.IsHangul(m_testSet.Word1))
                || (m_testSet.Word2 != null && !CharacterUtil.IsHangul(m_testSet.Word2))
                || (m_testSet.Word3 != null && !CharacterUtil.IsHangul(m_testSet.Word3))
                || (m_testSet.Word4 != null && !CharacterUtil.IsHangul(m_testSet.Word4))
                || (m_testSet.Word5 != null && !CharacterUtil.IsHangul(m_testSet.Word5))
                || (m_testSet.Word6 != null && !CharacterUtil.IsHangul(m_testSet.Word6))
                || (m_testSet.Word7 != null && !CharacterUtil.IsHangul(m_testSet.Word7))
                || (m_testSet.Word8 != null && !CharacterUtil.IsHangul(m_testSet.Word8))
                || (m_testSet.Word9 != null && !CharacterUtil.IsHangul(m_testSet.Word9))
                || (m_testSet.Word10 != null && !CharacterUtil.IsHangul(m_testSet.Word10)) )
            {
                var dialog = new MessageDialog("단어에 한글이 아닌 것이 포함되어있습니다. 확인해주십시오.");
                var res = await dialog.ShowAsync();
                return;
            }
            
            // data 추가
            DatabaseManager databaseManager = DatabaseManager.Instance;

            TestSet curSet = null;
            if ( m_updateMode )
            {
                curSet = m_targetTestSet;

                List<TestSetItem> list = databaseManager.GetTestSetItems(m_targetTestSet.Id);
                foreach (var item in list )
                {
                    databaseManager.DeleteTestSetItem(item);
                }
            }
            else
            {
                curSet = new TestSet() { SetName = m_testSet.Title, Active=false };
                databaseManager.InsertTestSet(curSet);
            }

            // 무식하지만...
            TestSetItem[] items = new TestSetItem[10];
            if( m_testSet.Word1 != null ) items[0] = new TestSetItem() { Number = 0, TestSetId = curSet.Id, Word = m_testSet.Word1.Trim() };
            if (m_testSet.Word2 != null) items[1] = new TestSetItem() { Number = 1, TestSetId = curSet.Id, Word = m_testSet.Word2.Trim() };
            if (m_testSet.Word3 != null) items[2] = new TestSetItem() { Number = 2, TestSetId = curSet.Id, Word = m_testSet.Word3.Trim() };
            if (m_testSet.Word4 != null) items[3] = new TestSetItem() { Number = 3, TestSetId = curSet.Id, Word = m_testSet.Word4.Trim() };
            if (m_testSet.Word5 != null) items[4] = new TestSetItem() { Number = 4, TestSetId = curSet.Id, Word = m_testSet.Word5.Trim() };
            if (m_testSet.Word6 != null) items[5] = new TestSetItem() { Number = 5, TestSetId = curSet.Id, Word = m_testSet.Word6.Trim() };
            if (m_testSet.Word7 != null) items[6] = new TestSetItem() { Number = 6, TestSetId = curSet.Id, Word = m_testSet.Word7.Trim() };
            if (m_testSet.Word8 != null) items[7] = new TestSetItem() { Number = 7, TestSetId = curSet.Id, Word = m_testSet.Word8.Trim() };
            if (m_testSet.Word9 != null) items[8] = new TestSetItem() { Number = 8, TestSetId = curSet.Id, Word = m_testSet.Word9.Trim() };
            if (m_testSet.Word10 != null) items[9] = new TestSetItem() { Number = 9, TestSetId = curSet.Id, Word = m_testSet.Word10.Trim() };

            for (int i = 0; i < 10; ++i)
            {
                if( items[i] != null )
                    databaseManager.InsertTestSetItem(items[i]);
            }

            // Active 된 TestSet이 없다면 이 set을 active 시킴
            TestSet activeSet = databaseManager.GetActiveTestSet();
            if( activeSet == null )
            {
                databaseManager.SetActive(curSet);
            }

            this.Frame.GoBack();
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();

        }

        private void testSetName_KeyDown(object sender, KeyRoutedEventArgs e)
        {

        }
    }

    public class FullTestSet
    {
        public string Title { get; set; }
        public string Word1 { get; set; }
        public string Word2 { get; set; }
        public string Word3 { get; set; }
        public string Word4 { get; set; }
        public string Word5 { get; set; }
        public string Word6 { get; set; }
        public string Word7 { get; set; }
        public string Word8 { get; set; }
        public string Word9 { get; set; }
        public string Word10 { get; set; }
    }
}
