using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace MIDAS_BAT
{
    class Util
    {
        public static async Task<bool> ShowDeleteAlertDlg()
        {
            var dialog = new MessageDialog("정말로 삭제하시겠습니까?");
            dialog.Title = "삭제";
            dialog.Commands.Add(new UICommand { Label = "예", Id = 0 });
            dialog.Commands.Add(new UICommand { Label = "아니오", Id = 1 });

            var res = await dialog.ShowAsync();
            if ((int)res.Id != 0)
                return false;

            return true;
        }
        public static async Task<bool> ShowStopExamAlertDlg()
        {
            var dialog = new MessageDialog("저장하지 않고 끝내시겠습니까?");
            dialog.Title = "중단";
            dialog.Commands.Add(new UICommand { Label = "예", Id = 0 });
            dialog.Commands.Add(new UICommand { Label = "아니오", Id = 1 });

            var res = await dialog.ShowAsync();
            if ((int)res.Id != 0)
                return false;

            return true;
        }


        public static async Task<bool> ShowWrongWritingAlertDlg()
        {
            var dialog = new MessageDialog("인식할 수 없습니다. 정자체로 다시 써주시기바랍니다.");
            var res = await dialog.ShowAsync();

            return true;
        }

        private static async Task<StorageFolder> GetSaveFolder()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                return folder;
            }
            return null;
        }

        public static async Task<bool> SaveResults(List<int> testExecList)
        {
            // 파일 위치 picker 필요
            Windows.Storage.StorageFolder rootFolder = await GetSaveFolder();
            if (rootFolder == null)
                return false;

            DatabaseManager dbManager = DatabaseManager.Instance;
            foreach (int testExecId in testExecList)
            {
                TestExec testExec = dbManager.GetTestExec(testExecId);
                Tester tester = dbManager.GetTester(testExec.TesterId);

                string newFolderName = tester.Name + "_" + tester.birthday + "_" + tester.Gender;

                StorageFolder subFolder = await rootFolder.CreateFolderAsync(newFolderName, CreationCollisionOption.ReplaceExisting);
                await SaveResult(subFolder, testExecId);
            }

            return true;
        }

        private static async Task<bool> exportDBResult(StorageFolder folder, int testExecId)
        {
            DatabaseManager dbManager = DatabaseManager.Instance;

            TestExec testExec = dbManager.GetTestExec(testExecId);
            Tester tester = dbManager.GetTester(testExec.TesterId);

            string testerName = tester.GetTesterName(true, true, true);
            StorageFile resultFile = await folder.CreateFileAsync(testerName + "_결과.csv", CreationCollisionOption.ReplaceExisting);

            List<TestSetItem> testSetItems = dbManager.GetTestSetItems(testExec.TestSetId);

            StringBuilder builder = new StringBuilder();
            builder.Append(tester.GetTesterName(true, true, true) + ",");
            builder.AppendLine("검사일 : " + ParsePrettyDateTimeForm(testExec.Datetime));
            builder.AppendLine("단어, 한글자, 초성시간(ms), 간격(ms), 중성시간(ms), 간격(ms), 종성시간(ms), 간격(ms), " +
                "초성평균압력(0~1), 중성평균압력(0~1), 종성평균압력(0~1)");

            foreach (var item in testSetItems)
            {
                List<TestExecResult> results = dbManager.GetTextExecResults(testExec.Id, item.Id);

                for (int i = 0; i < results.Count; ++i)
                {
                    if (i == 0)
                        builder.Append(item.Word);

                    builder.Append(",");
                    builder.Append(item.Word.ElementAt(results[i].TestSetItemCharIdx).ToString() + ",");
                    builder.Append(results[i].ChosungTime.ToString("F3") + ",");
                    builder.Append(results[i].FirstIdleTIme.ToString("F3") + ",");
                    builder.Append(results[i].JoongsungTime.ToString("F3") + ",");
                    builder.Append(results[i].SecondIdelTime.ToString("F3") + ",");
                    builder.Append(results[i].JongsungTime.ToString("F3") + ",");
                    builder.Append(results[i].ThirdIdleTime.ToString("F3") + ",");
                    builder.Append(results[i].ChosungAvgPressure.ToString("F6") + ",");
                    builder.Append(results[i].JoongsungAvgPressure.ToString("F6") + ",");
                    builder.AppendLine(results[i].JongsungAvgPressure.ToString("F6"));
                }
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = Encoding.GetEncoding("euc-kr");

            byte[] fileBytes = encoding.GetBytes(builder.ToString().ToCharArray());

            await FileIO.WriteBytesAsync(resultFile, fileBytes);

            return true;
        }

        private static async void ExportRawResultItem(StorageFolder folder, string testerId, string testerName, string itemNumber, string itemWord)
        {
            StorageFolder savedFolder = ApplicationData.Current.LocalFolder;

            string orgGifName = testerId + "_char_" + itemNumber + ".gif";

            if (await savedFolder.TryGetItemAsync(orgGifName) != null)
            {
                string newGifName = testerName + "_" + itemNumber + "_" + itemWord + ".gif";

                StorageFile charGifFile = await savedFolder.GetFileAsync(orgGifName);
                await charGifFile.CopyAsync(folder, newGifName, NameCollisionOption.ReplaceExisting);
            }

            string orgPngName = testerId + "_char_" + itemNumber + "_last.png";
            if (await savedFolder.TryGetItemAsync(orgPngName) != null)
            {
                string newPngName = testerName + "_" + itemNumber + "_" + itemWord + "_last.png";

                StorageFile charPngFile = await savedFolder.GetFileAsync(orgPngName);
                await charPngFile.CopyAsync(folder, newPngName, NameCollisionOption.ReplaceExisting);
            }

            // time
            string orgTimeName = testerId + "_raw_time_" + itemNumber + ".csv";
            if (await savedFolder.TryGetItemAsync(orgPngName) != null)
            {
                string newTimeName = testerName + "_" + itemNumber + "_" + itemWord + "_time.csv";

                StorageFile tiimeFile = await savedFolder.GetFileAsync(orgTimeName);
                await tiimeFile.CopyAsync(folder, newTimeName, NameCollisionOption.ReplaceExisting);
            }

            // pressure
            string orgPressureName = testerId + "_raw_pressure_" + itemNumber + ".csv";
            if (await savedFolder.TryGetItemAsync(orgPngName) != null)
            {
                string newPressureName = testerName + "_" + itemNumber + "_" + itemWord + "_pressure.csv";

                StorageFile pressureFile = await savedFolder.GetFileAsync(orgPressureName);
                await pressureFile.CopyAsync(folder, newPressureName, NameCollisionOption.ReplaceExisting);
            }
        }

        public static bool ExportRawResult(StorageFolder folder, int testExecId)
        {
            DatabaseManager dbManager = DatabaseManager.Instance;

            TestExec testExec = dbManager.GetTestExec(testExecId);
            Tester tester = dbManager.GetTester(testExec.TesterId);

            List<TestSetItem> testSetItems = dbManager.GetTestSetItems(testExec.TestSetId);
            string testerName = tester.GetTesterName(true, true, true);

            StorageFolder savedFolder = ApplicationData.Current.LocalFolder;

            // 사전 테스트 결과
            ExportRawResultItem(folder, tester.Id.ToString(), testerName, 0.ToString(), "사전테스트");

            foreach (var item in testSetItems)
            {
                ExportRawResultItem(folder, tester.Id.ToString(), testerName, item.Number.ToString(), item.Word);
            }

            return true;
        }


        public static async Task<bool> SaveResult(StorageFolder folder, int testExecId)
        {
            if (folder == null)
                return false;

            if (AppConfig.Instance.UseJamoSeperation == true)
                await exportDBResult(folder, testExecId);

            ExportRawResult(folder, testExecId);

            return true;
        }

        public static async Task<bool> SaveResult(int testExecId)
        {
            StorageFolder folder = await GetSaveFolder();
            return await SaveResult(folder, testExecId);
        }

        public static async Task<bool> CaptureInkCanva_PreTest(InkCanvas inkCanvas, TestExec testExec)
        {
            // 음.............. ㅋㅋㅋㅋㅋㅋㅋㅋ
            string file_name = testExec.TesterId + "_char_0_last.png";
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            var displayInformation = DisplayInformation.GetForCurrentView();
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget rtb = new CanvasRenderTarget(device, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96); // 96 쓰는게 맞나? or dpi 받아서 써야되나?

            using (var ds = rtb.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                ds.DrawInk(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            }

            var pixelBuffer = rtb.GetPixelBytes();
            var pixels = pixelBuffer.ToArray();

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)inkCanvas.ActualWidth,
                (uint)inkCanvas.ActualHeight,
                displayInformation.RawDpiX,
                displayInformation.RawDpiY,
                pixels);

            await encoder.FlushAsync();
            stream.Dispose();

            return true;
        }


        public static async Task<bool> CaptureInkCanvas(InkCanvas inkCanvas, Border borderUI, TestExec testExec, TestSetItem setItem)
        {
            // 음.............. ㅋㅋㅋㅋㅋㅋㅋㅋ
            string file_name = testExec.TesterId + "_char_" + setItem.Number + "_last.png";
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            var displayInformation = DisplayInformation.GetForCurrentView();
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget rtb = new CanvasRenderTarget(device, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96); // 96 쓰는게 맞나? or dpi 받아서 써야되나?

            using (var ds = rtb.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                ds.DrawInk(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());

                DrawGuideLineInImage(borderUI, ds);
            }

            var pixelBuffer = rtb.GetPixelBytes();
            var pixels = pixelBuffer.ToArray();

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)inkCanvas.ActualWidth,
                (uint)inkCanvas.ActualHeight,
                displayInformation.RawDpiX,
                displayInformation.RawDpiY,
                pixels);

            await encoder.FlushAsync();
            stream.Dispose();

            return true;
        }

        private static void DrawGuideLineInImage(Border borderUI, CanvasDrawingSession ds)
        {
            // 흠... 직접 그린다...
            RelativePanel parent = borderUI.Parent as RelativePanel;
            var transform = borderUI.TransformToVisual(parent);
            var point = transform.TransformPoint(new Point(0, 0));

            float[] range = { (float)point.X, (float)point.Y, (float)(point.X + borderUI.ActualWidth), (float)(point.Y + borderUI.ActualHeight) };

            ds.DrawLine(range[0], range[1], range[2], range[1], Colors.Black);
            ds.DrawLine(range[2], range[1], range[2], range[3], Colors.Black);
            ds.DrawLine(range[2], range[3], range[0], range[3], Colors.Black);
            ds.DrawLine(range[0], range[3], range[0], range[1], Colors.Black);

            Canvas guideLineCanvas = borderUI.Child as Canvas;
            CanvasStrokeStyle style = new CanvasStrokeStyle();
            style.DashStyle = CanvasDashStyle.Dash;
            foreach (var guideLine in guideLineCanvas.Children)
            {
                Line l = guideLine as Line;

                float[] linePt = { (float)(range[0] + l.X1), (float)(range[1] + l.Y1),
                        (float)(range[0] + l.X2), (float)(range[1] + l.Y2) };
                ds.DrawLine(linePt[0], linePt[1], linePt[2], linePt[3], Colors.Black, 2, style);
            }
        }

        public static async Task<bool> CaptureInkCanvasForStroke_PreTest(InkCanvas inkCanvas, TestExec testExec)
        {
            // 음.............. ㅋㅋㅋㅋㅋㅋㅋㅋ
            string file_name = testExec.TesterId + "_char_0.gif";
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

            var displayInformation = DisplayInformation.GetForCurrentView();
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.GifEncoderId, stream);

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget rtb = new CanvasRenderTarget(device, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96); // 96 쓰는게 맞나? or dpi 받아서 써야되나?
            IReadOnlyList<InkStroke> strokeList = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            List<InkStroke> newStrokeList = new List<InkStroke>();

            var propertySet = new BitmapPropertySet();
            var propertyValue = new BitmapTypedValue(
                100, // multiple of 10ms
                Windows.Foundation.PropertyType.UInt16
                );
            propertySet.Add("/grctlext/Delay", propertyValue);

            // 아오 복붙 ㅋㅋㅋ... ㅠㅠ

            // 초기화면
            // 한프레임...?
            using (var ds = rtb.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                ds.DrawInk(newStrokeList);
            }

            var pixelBuffer = rtb.GetPixelBytes();
            var pixels = pixelBuffer.ToArray();

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                 BitmapAlphaMode.Premultiplied,
                                 (uint)inkCanvas.ActualWidth,
                                 (uint)inkCanvas.ActualHeight,
                                 displayInformation.RawDpiX,
                                 displayInformation.RawDpiY,
                                 pixels);
            await encoder.BitmapProperties.SetPropertiesAsync(propertySet);
            await encoder.GoToNextFrameAsync();

            // 중간
            foreach (var stroke in strokeList)
            {
                newStrokeList.Add(stroke);

                // 한프레임...?
                using (var ds = rtb.CreateDrawingSession())
                {
                    ds.Clear(Colors.White);
                    ds.DrawInk(newStrokeList);
                }

                pixelBuffer = rtb.GetPixelBytes();
                pixels = pixelBuffer.ToArray();

                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                     BitmapAlphaMode.Premultiplied,
                                     (uint)inkCanvas.ActualWidth,
                                     (uint)inkCanvas.ActualHeight,
                                     displayInformation.RawDpiX,
                                     displayInformation.RawDpiY,
                                     pixels);
                await encoder.BitmapProperties.SetPropertiesAsync(propertySet);

                await encoder.GoToNextFrameAsync();
            }

            // 마지막 샷 
            using (var ds = rtb.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                ds.DrawInk(newStrokeList);
            }

            pixelBuffer = rtb.GetPixelBytes();
            pixels = pixelBuffer.ToArray();

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                BitmapAlphaMode.Premultiplied,
                                (uint)inkCanvas.ActualWidth,
                                (uint)inkCanvas.ActualHeight,
                                displayInformation.RawDpiX,
                                displayInformation.RawDpiY,
                                pixels);
            var lastPropertySet = new BitmapPropertySet();
            var lastPropertyValue = new BitmapTypedValue(
                500, // multiple of 10ms
                Windows.Foundation.PropertyType.UInt16
                );
            lastPropertySet.Add("/grctlext/Delay", lastPropertyValue);
            await encoder.BitmapProperties.SetPropertiesAsync(lastPropertySet);

            await encoder.FlushAsync();
            stream.Dispose();

            return true;
        }


        public static async Task<bool> CaptureInkCanvasForStroke(InkCanvas inkCanvas, Border borderUI, TestExec testExec, TestSetItem setItem)
        {
            // 음.............. ㅋㅋㅋㅋㅋㅋㅋㅋ
            string file_name = testExec.TesterId + "_char_" + setItem.Number.ToString() + ".gif";
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            var displayInformation = DisplayInformation.GetForCurrentView();
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.GifEncoderId, stream);

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget rtb = new CanvasRenderTarget(device, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96); // 96 쓰는게 맞나? or dpi 받아서 써야되나?
            IReadOnlyList<InkStroke> strokeList = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            List<InkStroke> newStrokeList = new List<InkStroke>();

            var propertySet = new BitmapPropertySet();
            var propertyValue = new BitmapTypedValue(
                100, // multiple of 10ms
                Windows.Foundation.PropertyType.UInt16
                );
            propertySet.Add("/grctlext/Delay", propertyValue);

            // 아오 복붙 ㅋㅋㅋ... ㅠㅠ

            // 초기화면
            // 한프레임...?
            using (var ds = rtb.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                ds.DrawInk(newStrokeList);

                DrawGuideLineInImage(borderUI, ds);
            }

            var pixelBuffer = rtb.GetPixelBytes();
            var pixels = pixelBuffer.ToArray();

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                 BitmapAlphaMode.Premultiplied,
                                 (uint)inkCanvas.ActualWidth,
                                 (uint)inkCanvas.ActualHeight,
                                 displayInformation.RawDpiX,
                                 displayInformation.RawDpiY,
                                 pixels);
            await encoder.BitmapProperties.SetPropertiesAsync(propertySet);
            await encoder.GoToNextFrameAsync();

            // 중간
            foreach (var stroke in strokeList)
            {
                newStrokeList.Add(stroke);

                // 한프레임...?
                using (var ds = rtb.CreateDrawingSession())
                {
                    ds.Clear(Colors.White);
                    ds.DrawInk(newStrokeList);

                    DrawGuideLineInImage(borderUI, ds);
                }

                pixelBuffer = rtb.GetPixelBytes();
                pixels = pixelBuffer.ToArray();

                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                     BitmapAlphaMode.Premultiplied,
                                     (uint)inkCanvas.ActualWidth,
                                     (uint)inkCanvas.ActualHeight,
                                     displayInformation.RawDpiX,
                                     displayInformation.RawDpiY,
                                     pixels);
                await encoder.BitmapProperties.SetPropertiesAsync(propertySet);

                await encoder.GoToNextFrameAsync();
            }

            // 마지막 샷 
            using (var ds = rtb.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                ds.DrawInk(newStrokeList);

                DrawGuideLineInImage(borderUI, ds);
            }

            pixelBuffer = rtb.GetPixelBytes();
            pixels = pixelBuffer.ToArray();

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                BitmapAlphaMode.Premultiplied,
                                (uint)inkCanvas.ActualWidth,
                                (uint)inkCanvas.ActualHeight,
                                displayInformation.RawDpiX,
                                displayInformation.RawDpiY,
                                pixels);
            var lastPropertySet = new BitmapPropertySet();
            var lastPropertyValue = new BitmapTypedValue(
                500, // multiple of 10ms
                Windows.Foundation.PropertyType.UInt16
                );
            lastPropertySet.Add("/grctlext/Delay", lastPropertyValue);
            await encoder.BitmapProperties.SetPropertiesAsync(lastPropertySet);

            await encoder.FlushAsync();
            stream.Dispose();

            return true;
        }

        public static string ParsePrettyDateTimeForm(string datetime)
        {
            // YYYYMMDD_hhmmss 폼으로 온 것들 파싱해서 예쁘게...? ㅋㅋ
            string result = datetime.Substring(0, 4) + "." +
                            datetime.Substring(4, 2) + "." +
                            datetime.Substring(6, 2) + " " +
                            datetime.Substring(9, 2) + ":" +
                            datetime.Substring(11, 2) + ":" +
                            datetime.Substring(13, 2);
            return result;
        }
    }
}
