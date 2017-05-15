using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

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

        private static async Task<Windows.Storage.StorageFolder> GetSaveFolder()
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
        public static async Task<bool> SaveResult(StorageFolder folder, int testExecId)
        {
            if (folder == null)
                return false;

            DatabaseManager dbManager = DatabaseManager.Instance;
            TestExec testExec = dbManager.GetTestExec(testExecId);
            Tester tester = dbManager.GetTester(testExec.TesterId);

            string testerName = tester.Name + "(" + tester.birthday + " " + tester.Gender + ")";

            StorageFile resultFile = await folder.CreateFileAsync(testerName + "_결과.csv", CreationCollisionOption.ReplaceExisting);

            
            List<TestSetItem> testSetItems = dbManager.GetTestSetItems(testExec.TestSetId);

            StringBuilder builder = new StringBuilder();
            builder.Append(tester.Name + "(" + tester.Gender + "),");
            builder.Append(tester.birthday + ",");
            builder.Append(tester.Education.ToString() + "," );
            builder.AppendLine("검사일 : " + ParsePrettyDateTimeForm(testExec.Datetime));

            builder.AppendLine("단어, 한글자, 초성시간(ms), 간격(ms), 중성시간(ms), 간격(ms), 종성시간(ms), 간격(ms), "+
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

            StorageFolder savedFolder = ApplicationData.Current.LocalFolder;
            
            foreach (var item in testSetItems)
            {
                string orgGifName = tester.Id + "_char_" + item.Number + ".gif";

                if (await savedFolder.TryGetItemAsync(orgGifName) != null)
                {
                    string newGifName = testerName + "_" + item.Number + "_" + item.Word + ".gif";

                    StorageFile charGifFile = await savedFolder.GetFileAsync(orgGifName);
                    await charGifFile.CopyAsync(folder, newGifName, NameCollisionOption.ReplaceExisting);
                }


                string orgPngName = tester.Id + "_char_" + item.Number + "_last.png";
                if (await savedFolder.TryGetItemAsync(orgPngName) != null)
                {
                    string newPngName = testerName + "_" + item.Number + "_" + item.Word + "_last.png";

                    StorageFile charPngFile = await savedFolder.GetFileAsync(orgPngName);
                    await charPngFile.CopyAsync(folder, newPngName, NameCollisionOption.ReplaceExisting);
                }

                // pressure
                string orgPressureName = tester.Id + "_raw_pressure_" + item.Number + ".txt";
                if (await savedFolder.TryGetItemAsync(orgPngName) != null)
                {
                    string newPressureName = testerName + "_" + item.Number + "_" + item.Word + "_pressure.txt";

                    StorageFile pressureFile = await savedFolder.GetFileAsync(orgPressureName);
                    await pressureFile.CopyAsync(folder, newPressureName, NameCollisionOption.ReplaceExisting);
                }


            }

            return true;
        }

        public static async Task<bool> SaveResult(int testExecId)
        {
            StorageFolder folder = await GetSaveFolder();
            return await SaveResult(folder, testExecId);
        }

        public static async Task<bool> CaptureInkCanvas(InkCanvas inkCanvas, string tester_id, int curIdx)
        {
            // 음.............. ㅋㅋㅋㅋㅋㅋㅋㅋ
            string file_name = tester_id + "_char_" + curIdx.ToString() + "_last.png";
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
                (uint)inkCanvas.ActualWidth,
                displayInformation.RawDpiX,
                displayInformation.RawDpiY,
                pixels);

            await encoder.FlushAsync();
            stream.Dispose();

            return true;
        }

        public static async Task<bool> CaptureInkCanvasForStroke(InkCanvas inkCanvas, string tester_id, int curIdx)
        {
            // 음.............. ㅋㅋㅋㅋㅋㅋㅋㅋ
            string file_name = tester_id + "_char_" + curIdx.ToString() + ".gif";
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
            }

            var pixelBuffer = rtb.GetPixelBytes();
            var pixels = pixelBuffer.ToArray();

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                 BitmapAlphaMode.Premultiplied,
                                 (uint)inkCanvas.ActualWidth,
                                 (uint)inkCanvas.ActualWidth,
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
                                     (uint)inkCanvas.ActualWidth,
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
                                (uint)inkCanvas.ActualWidth,
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

        public static string ParsePrettyDateTimeForm( string datetime )
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
