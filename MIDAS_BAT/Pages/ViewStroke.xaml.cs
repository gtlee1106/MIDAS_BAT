using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Core;
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
    public sealed partial class ViewStroke: Page
    {
        public ViewStroke()
        {
            this.InitializeComponent();

            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse 
                                                    | Windows.UI.Core.CoreInputDeviceTypes.Pen
                                                    | Windows.UI.Core.CoreInputDeviceTypes.Touch;


            /*
            CoreInkIndependentInputSource core = CoreInkIndependentInputSource.Create(inkCanvas.InkPresenter);
            core.PointerPressing += Core_PointerPressing;
            core.PointerReleasing += Core_PointerReleasing;
            */
        }

        public async void LoadAndDrawStork()
        {
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile storageFile = await storageFolder.GetFileAsync("7_0.gif");

            if (storageFile == null)
                return;

            /*
            IRandomAccessStream stream = await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
            using (var inputStream = stream.GetInputStreamAt(0))
            {
                await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
            }
            stream.Dispose();
            */

            InkStrokeContainer container = new InkStrokeContainer();

            using (var outputSream = await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                await container.LoadAsync(outputSream);
            }

            List<InkStroke> inkStrokes = new List<InkStroke>();
            foreach (var inkStroke in inkCanvas.InkPresenter.StrokeContainer.GetStrokes())
                inkStrokes.Add(inkStroke);
                 
            foreach( var stroke in inkStrokes)
            {
                bool firstStroke = true;
                foreach(InkStrokeRenderingSegment segment in stroke.GetRenderingSegments())
                {
                    if(firstStroke)
                    {
                        inkCanvas.InkPresenter.StrokeContainer.MoveSelected(segment.Position);
                        firstStroke = false;
                    }
                }
            }
        }

        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadAndDrawStork();

        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
