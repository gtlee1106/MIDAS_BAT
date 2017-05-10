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

                string newFolderName = tester.Name + "_" + tester.birthday;

                Windows.Storage.StorageFolder subFolder = await rootFolder.CreateFolderAsync(newFolderName, CreationCollisionOption.ReplaceExisting);
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


            // tester 정보
            await FileIO.WriteTextAsync(resultFile, tester.Name + "(" + tester.Gender + "),");
            await FileIO.AppendTextAsync(resultFile, tester.birthday + ",");
            await FileIO.AppendTextAsync(resultFile, tester.Education.ToString() + "\r\n");

            // 각 항목별 헤더
            await FileIO.AppendTextAsync(resultFile, "단어,");

            await FileIO.AppendTextAsync(resultFile, "한글자,");
            await FileIO.AppendTextAsync(resultFile, "초성시간(ms),");
            await FileIO.AppendTextAsync(resultFile, "간격(ms),");
            await FileIO.AppendTextAsync(resultFile, "중성시간(ms),");
            await FileIO.AppendTextAsync(resultFile, "간격(ms),");
            await FileIO.AppendTextAsync(resultFile, "종성시간(ms),");
            await FileIO.AppendTextAsync(resultFile, "초성평균압력(0~1),");
            await FileIO.AppendTextAsync(resultFile, "중성평균압력(0~1),");
            await FileIO.AppendTextAsync(resultFile, "종성평균압력(0~1)");
            await FileIO.AppendTextAsync(resultFile, "\r\n");

            foreach (var item in testSetItems)
            {
                List<TestExecResult> results = dbManager.GetTextExecResults(testExec.Id, item.Id);

                for (int i = 0; i < results.Count; ++i)
                {
                    if (i == 0)
                        await FileIO.AppendTextAsync(resultFile, item.Word);
                    await FileIO.AppendTextAsync(resultFile, ",");

                    await FileIO.AppendTextAsync(resultFile, item.Word.ElementAt(results[i].TestSetItemCharIdx).ToString() + ",");
                    await FileIO.AppendTextAsync(resultFile, results[i].ChosungTime.ToString("F3") + ",");
                    await FileIO.AppendTextAsync(resultFile, results[i].FirstIdleTIme.ToString("F3") + ",");
                    await FileIO.AppendTextAsync(resultFile, results[i].JoongsungTime.ToString("F3") + ",");
                    await FileIO.AppendTextAsync(resultFile, results[i].SecondIdelTime.ToString("F3") + ",");
                    await FileIO.AppendTextAsync(resultFile, results[i].JongsungTime.ToString("F3") + ",");
                    await FileIO.AppendTextAsync(resultFile, results[i].ChosungAvgPressure.ToString("F6") + ",");
                    await FileIO.AppendTextAsync(resultFile, results[i].JoongsungAvgPressure.ToString("F6") + ",");
                    await FileIO.AppendTextAsync(resultFile, results[i].JongsungAvgPressure.ToString("F6"));
                    await FileIO.AppendTextAsync(resultFile, "\r\n");
                }
            }

            StorageFolder gifSavedFolder = ApplicationData.Current.LocalFolder;
            
            foreach (var item in testSetItems)
            {
                string orgGifName = tester.Id + "_char_" + item.Number + ".gif";

                if( await gifSavedFolder.TryGetItemAsync(orgGifName) == null)
                    continue;

                string newGifName = testerName + "_" + item.Number + "_" + item.Word + ".gif";

                StorageFile charGifFile = await gifSavedFolder.GetFileAsync(orgGifName);
                await charGifFile.CopyAsync(folder, newGifName);
            }
            
            // 또 다른거 있나...? 

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
            string file_name = tester_id + "_char_" + curIdx.ToString() + ".gif";
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await storageFolder.CreateFileAsync(file_name, Windows.Storage.CreationCollisionOption.ReplaceExisting);

            var displayInformation = DisplayInformation.GetForCurrentView();
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.GifEncoderId, stream);

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
    }
}
