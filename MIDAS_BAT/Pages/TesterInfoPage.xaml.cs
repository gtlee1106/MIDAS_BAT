using System;
using System.Collections.Generic;
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
    public sealed partial class TesterInfoPage : Page
    {
        public TesterInfoPage()
        {
            this.InitializeComponent();
        }

        private void startTest_Click(object sender, RoutedEventArgs e)
        {
            DatabaseManager dbManager = DatabaseManager.Instance;

            string gender = "";
            if (maleRadioBtn.IsChecked == true)
                gender = "남";
            else if (femaleRadioBtn.IsChecked == true)
                gender = "여";
            else
                gender = "모름";

            string education = educationCmb.SelectedValue + " ";
            if( graduateRadioBtn.IsChecked == true )
                education += graduateRadioBtn.Content;
            else if( dropRadioBtn.IsChecked == true )
                education += dropRadioBtn.Content;

            Tester tester = new Tester()
            {
                Name = name.Text,
                Gender = gender,
                birthday = year.Text + month.Text + day.Text,
                Education = education,
            };
            dbManager.InsertTester(tester);

            TestSet testSet = dbManager.GetActiveTestSet();
            if( testSet == null )
            {
                var dialog = new MessageDialog("활성화된 실험셋이 없습니다. 새로 만들어주세요.");
                dialog.ShowAsync();
                return;
            }

            TestExec testExec = new TestExec()
            {
                TesterId = tester.Id,
                TestSetId = testSet.Id,
                Datetime = System.DateTime.Now.ToString("yyyyMMdd_hhmmss"),
                ShowBorder = showBoxChk.IsChecked == true ? true : false,
                ScreenWidth = Int32.Parse(widthBox.Text),
                ScreenHeight = Int32.Parse(heightBox.Text)
            };
            dbManager.InsertTestExec(testExec);

            this.Frame.Navigate(typeof(TestPage), testExec);
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private void gender_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void graduate_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void showBoxChk_Click(object sender, RoutedEventArgs e)
        {
            if( showBoxChk.IsChecked == true )
            {
                if (widthBox != null)
                { 
                    widthBox.IsEnabled= true;
                }
                if (heightBox != null)
                {
                    heightBox.IsEnabled = true;
                }

            }
            else
            {
                if( widthBox != null )
                {
                    widthBox.IsEnabled = false;
                }
                if (heightBox != null)
                {
                    heightBox.IsEnabled = false;
                }
            }


        }

    }
}
