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

// 빈 페이지 항목 템플릿은 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 에 문서화되어 있습니다.

namespace MIDAS_BAT
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

        }

        private void startTest_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(TesterInfoPage));
            //this.Frame.Navigate(typeof(ViewStroke));
        }
        private void viewResult_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ViewResultPage));
        }
        private void configTest_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ConfigPage));
        }
        private void configAppBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ConfigAppPage));
        }
    }
}
