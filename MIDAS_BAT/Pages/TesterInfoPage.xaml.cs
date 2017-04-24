using System;
using System.Collections.Generic;
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
    public sealed partial class TesterInfoPage : Page
    {
        public TesterInfoPage()
        {
            this.InitializeComponent();
        }

        private void startTest_Click(object sender, RoutedEventArgs e)
        {
            DatabaseManager dbManager = DatabaseManager.Instance;
            
            Tester tester = new Tester()
            {
                Name = name.Text,
                birthday = year.Text + month.Text + day.Text,
                Education = Int32.Parse( education.Text)
            };
            dbManager.InsertTester(tester);

            TestSet testSet = dbManager.GetActiveTestSet();
            if( testSet == null )
            {
                // 메시지를 띄워야되려나?
                return;
            }

            TestExec testExec = new TestExec()
            {
                TesterId = tester.Id,
                TestSetId = testSet.Id,
                Datetime = System.DateTime.Now.ToString("yyyyMMdd_hhmmss")
            };
            dbManager.InsertTestExec(testExec);

            this.Frame.Navigate(typeof(TestPage), testExec);
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}
