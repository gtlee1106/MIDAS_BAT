using MIDAS_BAT.Pages;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// 빈 페이지 항목 템플릿에 대한 설명은 http://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace MIDAS_BAT
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class TesterInfoPage : Page
    {
        private string m_dropYear = ""; // 있는 경우 1~6문자열 입력됨. 
        public TesterInfoPage()
        {
            this.InitializeComponent();

            initBtn();
            initBoxSize();
        }

        private void initBoxSize()
        {
            if (widthBox != null)
                widthBox.Text = AppConfig.Instance.BoxWidth.ToString();
            if (heightBox != null)
                heightBox.Text = AppConfig.Instance.BoxHeight.ToString();
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
            if (graduateRadioBtn.IsChecked == true)
                education += graduateRadioBtn.Content;
            else if (dropRadioBtn.IsChecked == true)
            {
                education += dropRadioBtn.Content;
                education += "(" + m_dropYear + "년 재학)";
            }

            Tester tester = new Tester()
            {
                Name = name.Text,
                Gender = gender,
                birthday = year.Text + month.Text + day.Text,
                Education = education,
            };
            dbManager.InsertTester(tester);

            TestSet testSet = dbManager.GetActiveTestSet();
            if (testSet == null)
            {
                var dialog = new MessageDialog("활성화된 실험셋이 없습니다. 새로 만들어주세요.");
                dialog.ShowAsync();
                return;
            }

            TestExec testExec = new TestExec()
            {
                TesterId = tester.Id,
                TestSetId = testSet.Id,
                Datetime = DateTime.Now.ToString("yyyyMMdd_hhmmss"),
                UseJamoSepartaion = (bool)AppConfig.Instance.UseJamoSeperation,
                ShowBorder = showBoxChk.IsChecked == true ? true : false,
                ScreenWidth = Int32.Parse(widthBox.Text),
                ScreenHeight = Int32.Parse(heightBox.Text)
            };
            dbManager.InsertTestExec(testExec);

            //this.Frame.Navigate(typeof(TestPage), testExec);
            //this.Frame.Navigate(typeof(PreTestPage), testExec);
            this.Frame.Navigate(typeof(HorizontalLineTestPage), testExec, new SuppressNavigationTransitionInfo());
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
            if (graduateRadioBtn == null ||
                dropRadioBtn == null ||
                dropUISet == null)
                return;

            if (graduateRadioBtn.IsChecked == true)
            {
                dropUISet.Visibility = Visibility.Collapsed;
            }
            else if (dropRadioBtn.IsChecked == true)
            {
                dropUISet.Visibility = Visibility.Visible;

                onChangedEducationComb();
            }
        }

        private void showBoxChk_Click(object sender, RoutedEventArgs e)
        {
            if (showBoxChk.IsChecked == true)
            {
                if (widthBox != null)
                {
                    widthBox.IsEnabled = true;
                }
                if (heightBox != null)
                {
                    heightBox.IsEnabled = true;
                }

            }
            else
            {
                if (widthBox != null)
                {
                    widthBox.IsEnabled = false;
                }
                if (heightBox != null)
                {
                    heightBox.IsEnabled = false;
                }
            }


        }

        private void dropYear_btn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            m_dropYear = btn.Content.ToString();

            initBtn();
            btn.Background = new SolidColorBrush(Windows.UI.Colors.DarkGray);
        }

        private void initBtn()
        {
            dropYear_1.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
            dropYear_2.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
            dropYear_3.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
            dropYear_4.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
            dropYear_5.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
            dropYear_6.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
        }

        private void onChangedEducationComb()
        {
            if( dropRadioBtn == null)
                return;

            if (dropRadioBtn.IsChecked != true )
                return;

            int eduIdx = educationCmb.SelectedIndex;
            switch (eduIdx)
            {
                case 0:
                    {
                        dropYear_1.Visibility = Visibility.Visible;
                        dropYear_2.Visibility = Visibility.Visible;
                        dropYear_3.Visibility = Visibility.Visible;
                        dropYear_4.Visibility = Visibility.Visible;
                        dropYear_5.Visibility = Visibility.Visible;
                        dropYear_6.Visibility = Visibility.Visible;
                    }
                    break;
                case 1:
                case 2:
                    {
                        dropYear_1.Visibility = Visibility.Visible;
                        dropYear_2.Visibility = Visibility.Visible;
                        dropYear_3.Visibility = Visibility.Visible;
                        dropYear_4.Visibility = Visibility.Collapsed;
                        dropYear_5.Visibility = Visibility.Collapsed;
                        dropYear_6.Visibility = Visibility.Collapsed;
                    }
                    break;
                case 3:
                    {
                        dropYear_1.Visibility = Visibility.Visible;
                        dropYear_2.Visibility = Visibility.Visible;
                        dropYear_3.Visibility = Visibility.Visible;
                        dropYear_4.Visibility = Visibility.Visible;
                        dropYear_5.Visibility = Visibility.Collapsed;
                        dropYear_6.Visibility = Visibility.Collapsed;
                    }

                    break;
            }
        }

        private void educationCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            onChangedEducationComb();
        }

        private string getDropYear()
        {
            if (dropRadioBtn.IsChecked != true)
                return "";

            return m_dropYear;
        }
    }
}
