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
    public sealed partial class ConfigPage : Page, INotifyPropertyChanged
    {
        ObservableCollection<TestSet> testSetList = new ObservableCollection<TestSet>();

        public ObservableCollection<TestSet> TestSetList
        {
            get { return testSetList; }
            set
            {
                if (value != testSetList)
                {
                    testSetList = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ConfigPage()
        {
            this.InitializeComponent();

            DatabaseManager dbManager = DatabaseManager.Instance;
            dbManager.GetTestSet().ForEach(testSetList.Add);

            NotifyPropertyChanged();
        }
        
        private void addTestSet_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MakeTestSetPage));
        }

        private void editBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedTestSet = (sender as FrameworkElement).Tag as TestSet;

            this.Frame.Navigate(typeof(MakeTestSetPage), selectedTestSet);
        }

        private async void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog("정말로 삭제하시겠습니까?\n(해당 실험셋을 활용한 실험결과도 같이 삭제됩니다.)");
            dialog.Title = "삭제";
            dialog.Commands.Add(new UICommand { Label = "예", Id=0 });
            dialog.Commands.Add(new UICommand { Label = "아니오", Id=1 });

            var res = await dialog.ShowAsync();
            if ((int)res.Id != 0)
                return;
            
            var selectedTestSet = (sender as FrameworkElement).Tag as TestSet;

            DatabaseManager dbManager = DatabaseManager.Instance;
            dbManager.DeleteTestSet(selectedTestSet);

            // itemsource 갱신
            testSetList.Remove(selectedTestSet);

            //리스트뷰 갱신이 필요함 음...            
            NotifyPropertyChanged();
        }
        private void activeChk_Click(object sender, RoutedEventArgs e)
        {
            CheckBox chkBox = sender as CheckBox;
            var selectedTestSet = (sender as FrameworkElement).Tag as TestSet;

            if( chkBox.IsChecked == true )
            {
                DatabaseManager dbManager = DatabaseManager.Instance;
                dbManager.SetActive(selectedTestSet);

                foreach (var item in TestSetList)
                {
                    if (!selectedTestSet.Equals(item))
                        item.Active = false;
                    else
                        item.Active = true;
                }

            }
            else if( chkBox.IsChecked == false)
            {
                chkBox.IsChecked = true;
            }

            NotifyPropertyChanged();
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged( [CallerMemberName] String propertyName = "")
        {
            if( PropertyChanged != null )
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
