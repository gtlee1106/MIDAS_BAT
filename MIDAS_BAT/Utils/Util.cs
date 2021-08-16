﻿using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using MIDAS_BAT.Data;
using MIDAS_BAT.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
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

        public static async Task<bool> ShowEndOfTestDlg()
        {
            var dialog = new MessageDialog("검사가 끝났습니다. 수고하셨습니다.");
            await dialog.ShowAsync();
            return true;
        }




        public static async Task<bool> ShowWrongWritingAlertDlg()
        {
            var dialog = new MessageDialog("인식할 수 없습니다. 정자체로 다시 써주시기바랍니다.");
            var res = await dialog.ShowAsync();

            return true;
        }

        public static async Task<bool> ShowCannotGoBackAlertDlg()
        {
            var dialog = new MessageDialog("뒤로 돌아갈 수 없습니다.");
            var res = await dialog.ShowAsync();

            return true;
        }

        public static async Task<bool> ShowEraseAlertDlg()
        {
            var dialog = new MessageDialog("정말로 화면을 지우시겠습니까?");
            dialog.Title = "지우기";
            dialog.Commands.Add(new UICommand { Label = "예", Id = 0 });
            dialog.Commands.Add(new UICommand { Label = "아니오", Id = 1 });

            var res = await dialog.ShowAsync();
            if ((int)res.Id != 0)
                return false;

            return true;
        }

        public static async Task<bool> ShowGoBackAlertDlg()
        {
            var dialog = new MessageDialog("현재 번호 및 이전 번호에 저장된 반응이 삭제됩니다. 이전번호로 돌아가시겠습니까?");
            dialog.Title = "지우기";
            dialog.Commands.Add(new UICommand { Label = "예", Id = 0 });
            dialog.Commands.Add(new UICommand { Label = "아니오", Id = 1 });

            var res = await dialog.ShowAsync();
            if ((int)res.Id != 0)
                return false;

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

        public static Type getNextTest(TestSet testSet, int curTestOrder)
        {
            bool[] testOrder =
            {
                testSet.HorizontalLineTest,
                testSet.VerticalLineTest,
                testSet.CounterClockwiseSpiralTest,
                testSet.ClockwiseSpiralTest,
                testSet.CounterClockwiseFreeSpiralTest,
                testSet.ClockwiseFreeSpiralTest,
                testSet.TextWritingTest
            };

            Type[] testType =
            {
                typeof(HorizontalLineTestPage),
                typeof(VerticalLineTestPage),
                typeof(CounterClockWiseSpiralTestPage),
                typeof(ClockWiseSpiralTestPage),
                typeof(CounterClockWiseFreeSpiralTestPage),
                typeof(ClockWiseFreeSpiralTestPage),
                typeof(TestPage)
            };

            for(int i = curTestOrder+1; i < testOrder.Length; i++)
            {
                if (testOrder[i])
                    return testType[i];
            }

            return null;
        }

        public static Type getPrevTest(TestSet testSet, int curTestOrder)
        {
            bool[] testOrder =
            {
                testSet.HorizontalLineTest,
                testSet.VerticalLineTest,
                testSet.CounterClockwiseSpiralTest,
                testSet.ClockwiseSpiralTest,
                testSet.CounterClockwiseFreeSpiralTest,
                testSet.ClockwiseFreeSpiralTest,
                testSet.TextWritingTest
            };

            Type[] testType =
            {
                typeof(HorizontalLineTestPage),
                typeof(VerticalLineTestPage),
                typeof(CounterClockWiseSpiralTestPage),
                typeof(ClockWiseSpiralTestPage),
                typeof(CounterClockWiseFreeSpiralTestPage),
                typeof(ClockWiseFreeSpiralTestPage),
                typeof(TestPage)
            };

            for (int i = curTestOrder - 1; i >= 0; i--)
            {
                if (testOrder[i])
                    return testType[i];
            }

            return null;
        }


        public static async Task<bool> SaveResults(List<int> testExecList)
        {
            // 파일 위치 picker 필요
            StorageFolder rootFolder = await GetSaveFolder();
            if (rootFolder == null)
                return false;

            DatabaseManager dbManager = DatabaseManager.Instance;
            foreach (int testExecId in testExecList)
            {
                TestExec testExec = dbManager.GetTestExec(testExecId);
                Tester tester = dbManager.GetTester(testExec.TesterId);

                StorageFolder orgSourceFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(tester.Id.ToString(), CreationCollisionOption.OpenIfExists);

                string newFolderName = tester.Name + "_" + tester.birthday + "_" + tester.Gender;

                StorageFolder subFolder = await rootFolder.CreateFolderAsync(newFolderName, CreationCollisionOption.OpenIfExists);
                await SaveResult(orgSourceFolder, subFolder, testExecId);
            }

            return true;
        }

        private static async Task<bool> exportDBResult(StorageFolder orgFolder, StorageFolder targetFolder, int testExecId)
        {
            DatabaseManager dbManager = DatabaseManager.Instance;

            TestExec testExec = dbManager.GetTestExec(testExecId);
            Tester tester = dbManager.GetTester(testExec.TesterId);

            string testerName = tester.GetTesterName(true, true, true);
            StorageFile resultFile = await targetFolder.CreateFileAsync(testerName + "_결과.csv", CreationCollisionOption.ReplaceExisting);

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

        private static async void ExportRawResultItem(StorageFolder orgFolder, StorageFolder targetFolder, string testerId, string testerName, List<TestSetItem> testSetItems)
        {
            string[,] testNames = new string[,] {
                { String.Format("{0}_{1}", HorizontalLineTestPage.TEST_ORDER, HorizontalLineTestPage.TEST_NAME) ,
                    String.Format("{0}_{1}", HorizontalLineTestPage.TEST_ORDER, HorizontalLineTestPage.TEST_NAME_KR) },
                { String.Format("{0}_{1}", VerticalLineTestPage.TEST_ORDER, VerticalLineTestPage.TEST_NAME) ,
                    String.Format("{0}_{1}", VerticalLineTestPage.TEST_ORDER, VerticalLineTestPage.TEST_NAME_KR) },
                { String.Format("{0}_{1}", CounterClockWiseSpiralTestPage.TEST_ORDER, CounterClockWiseSpiralTestPage.TEST_NAME) ,
                    String.Format("{0}_{1}", CounterClockWiseSpiralTestPage.TEST_ORDER, CounterClockWiseSpiralTestPage.TEST_NAME_KR) },
                { String.Format("{0}_{1}", ClockWiseSpiralTestPage.TEST_ORDER, ClockWiseSpiralTestPage.TEST_NAME) ,
                    String.Format("{0}_{1}", ClockWiseSpiralTestPage.TEST_ORDER, ClockWiseSpiralTestPage.TEST_NAME_KR) },
                { String.Format("{0}_{1}", CounterClockWiseFreeSpiralTestPage.TEST_ORDER, CounterClockWiseFreeSpiralTestPage.TEST_NAME) ,
                    String.Format("{0}_{1}", CounterClockWiseFreeSpiralTestPage.TEST_ORDER, CounterClockWiseFreeSpiralTestPage.TEST_NAME_KR) },
                { String.Format("{0}_{1}", ClockWiseFreeSpiralTestPage.TEST_ORDER, ClockWiseFreeSpiralTestPage.TEST_NAME) , 
                    String.Format("{0}_{1}", ClockWiseFreeSpiralTestPage.TEST_ORDER, ClockWiseFreeSpiralTestPage.TEST_NAME_KR) }
            };

            for( int i = 0; i < testNames.GetLength(0); i++ )
            {
                string gifFileName = String.Format("{0}_{1}_{2}.gif", testerId, testNames[i, 0], 0);
                if (await orgFolder.TryGetItemAsync(gifFileName) != null)
                {
                    string newGifName = String.Format("{0}_{1}.gif", testerName, testNames[i, 1]);
                    StorageFile charGifFile = await orgFolder.GetFileAsync(gifFileName);
                    await charGifFile.CopyAsync(targetFolder, newGifName, NameCollisionOption.ReplaceExisting);
                }

                string orgPngName = String.Format("{0}_{1}_{2}_last.png", testerId, testNames[i, 0], 0); 
                if (await orgFolder.TryGetItemAsync(orgPngName) != null)
                {
                    string newPngName = String.Format("{0}_{1}_최종.png", testerName, testNames[i, 1]);
                    StorageFile charPngFile = await orgFolder.GetFileAsync(orgPngName);
                    await charPngFile.CopyAsync(targetFolder, newPngName, NameCollisionOption.ReplaceExisting);
                }

                string diffPngName = String.Format("{0}_{1}_{2}_diff.png", testerId, testNames[i, 0], 0);
                if (await orgFolder.TryGetItemAsync(diffPngName) != null)
                {
                    string newDiffPngName = String.Format("{0}_{1}_차이.png", testerName, testNames[i, 1]);
                    StorageFile charPngFile = await orgFolder.GetFileAsync(diffPngName);
                    await charPngFile.CopyAsync(targetFolder, newDiffPngName, NameCollisionOption.ReplaceExisting);
                }

                // time
                string orgTimeName = String.Format("{0}_{1}_raw_time_{2}.csv", testerId, testNames[i, 0], 0);
                if (await orgFolder.TryGetItemAsync(orgTimeName) != null)
                {
                    string newTimeName = String.Format("{0}_{1}_time.csv", testerName, testNames[i, 1]);

                    StorageFile tiimeFile = await orgFolder.GetFileAsync(orgTimeName);
                    await tiimeFile.CopyAsync(targetFolder, newTimeName, NameCollisionOption.ReplaceExisting);
                }

                // pressure
                string orgPressureName = String.Format("{0}_{1}_raw_pressure_{2}.csv", testerId, testNames[i, 0], 0);
                if (await orgFolder.TryGetItemAsync(orgPressureName) != null)
                {
                    string newPressureName = String.Format("{0}_{1}_pressure.csv", testerName, testNames[i, 1]);

                    StorageFile pressureFile = await orgFolder.GetFileAsync(orgPressureName);
                    await pressureFile.CopyAsync(targetFolder, newPressureName, NameCollisionOption.ReplaceExisting);
                }

                // minmax
                string orgMinmaxName = String.Format("{0}_{1}_minmax_{2}.csv", testerId, testNames[i, 0], 0);
                if (await orgFolder.TryGetItemAsync(orgMinmaxName) != null)
                {
                    string newMinmaxName = String.Format("{0}_{1}_MinMax.csv", testerName, testNames[i, 1]);

                    StorageFile pressureFile = await orgFolder.GetFileAsync(orgMinmaxName);
                    await pressureFile.CopyAsync(targetFolder, newMinmaxName, NameCollisionOption.ReplaceExisting);
                }

                // difference
                string orgDiffName = String.Format("{0}_{1}_diff_{2}.csv", testerId, testNames[i, 0], 0);
                if (await orgFolder.TryGetItemAsync(orgDiffName) != null)
                {
                    string newDiffName = String.Format("{0}_{1}_차이.csv", testerName, testNames[i, 1]);

                    StorageFile pressureFile = await orgFolder.GetFileAsync(orgDiffName);
                    await pressureFile.CopyAsync(targetFolder, newDiffName, NameCollisionOption.ReplaceExisting);
                }
            }

            string[] charTestNames = { String.Format("{0}_{1}", TestPage.TEST_ORDER, TestPage.TEST_NAME),
                    String.Format("{0}_{1}", TestPage.TEST_ORDER, TestPage.TEST_NAME_KR) };

            foreach( var item in testSetItems)
            {
                string gifFileName = String.Format("{0}_{1}_{2}.gif", testerId, charTestNames[0], item.Number);
                if (await orgFolder.TryGetItemAsync(gifFileName) != null)
                {
                    string newGifName = String.Format("{0}_{1}_{2}_{3}.gif", testerName, charTestNames[1], item.Number, item.Word);
                    StorageFile charGifFile = await orgFolder.GetFileAsync(gifFileName);
                    await charGifFile.CopyAsync(targetFolder, newGifName, NameCollisionOption.ReplaceExisting);
                }

                string orgPngName = String.Format("{0}_{1}_{2}_last.png", testerId, charTestNames[0], item.Number);
                if (await orgFolder.TryGetItemAsync(orgPngName) != null)
                {
                    string newPngName = String.Format("{0}_{1}_{2}_{3}_최종.png", testerName, charTestNames[1], item.Number, item.Word);
                    StorageFile charPngFile = await orgFolder.GetFileAsync(orgPngName);
                    await charPngFile.CopyAsync(targetFolder, newPngName, NameCollisionOption.ReplaceExisting);
                }

                // time
                string orgTimeName = String.Format("{0}_{1}_raw_time_{2}.csv", testerId, charTestNames[0], item.Number);
                if (await orgFolder.TryGetItemAsync(orgTimeName) != null)
                {
                    string newTimeName = String.Format("{0}_{1}_{2}_{3}_time.csv", testerName, charTestNames[1], item.Number, item.Word);

                    StorageFile tiimeFile = await orgFolder.GetFileAsync(orgTimeName);
                    await tiimeFile.CopyAsync(targetFolder, newTimeName, NameCollisionOption.ReplaceExisting);
                }

                // pressure
                string orgPressureName = String.Format("{0}_{1}_raw_pressure_{2}.csv", testerId, charTestNames[0], item.Number);
                if (await orgFolder.TryGetItemAsync(orgPressureName) != null)
                {
                    string newPressureName = String.Format("{0}_{1}_{2}_{3}_pressure.csv", testerName, charTestNames[1], item.Number, item.Word);

                    StorageFile pressureFile = await orgFolder.GetFileAsync(orgPressureName);
                    await pressureFile.CopyAsync(targetFolder, newPressureName, NameCollisionOption.ReplaceExisting);
                }

                // minmax
                string orgMinmaxName = String.Format("{0}_{1}_minmax_{2}.csv", testerId, charTestNames[0], item.Number);
                if (await orgFolder.TryGetItemAsync(orgMinmaxName) != null)
                {
                    string newMinmaxName = String.Format("{0}_{1}_{2}_{3}_MinMax.csv", testerName, charTestNames[1], item.Number, item.Word);

                    StorageFile minmaxFile = await orgFolder.GetFileAsync(orgMinmaxName);
                    await minmaxFile.CopyAsync(targetFolder, newMinmaxName, NameCollisionOption.ReplaceExisting);
                }

                // pngs
                // 이건 몇 장이 될 지 알 수가 없어서...
                List<string> fileTypeFilter = new List<string>();
                fileTypeFilter.Add(".png");
                var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);

                // Create query and retrieve files
                var query = orgFolder.CreateFileQueryWithOptions(queryOptions);
                IReadOnlyList<StorageFile> fileList = await query.GetFilesAsync();
                string pngFileFormat = String.Format("{0}_{1}_{2}_stroke_", testerId, charTestNames[0], item.Number);
                foreach (StorageFile file in fileList)
                {
                    if (!file.Name.StartsWith(pngFileFormat))
                        continue;

                    string newPngName = file.Name.Replace(pngFileFormat, String.Format("{0}_{1}_{2}_{3}_획순_", testerName, charTestNames[1], item.Number, item.Word));
                    await file.CopyAsync(targetFolder, newPngName, NameCollisionOption.ReplaceExisting);
                }
            }
        }

        public static async Task deleteFiles(int testerId, int testOrder, string testName)
        {
            StorageFolder orgSourceFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(testerId.ToString(), CreationCollisionOption.OpenIfExists);

            List<string> fileTypeFilter = new List<string>();
            fileTypeFilter.Add(".png");
            fileTypeFilter.Add(".csv");
            fileTypeFilter.Add(".gif");
            fileTypeFilter.Add(".xlsx");
            fileTypeFilter.Add(".txt");
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);

            // Create query and retrieve files
            var query = orgSourceFolder.CreateFileQueryWithOptions(queryOptions);
            IReadOnlyList<StorageFile> fileList = await query.GetFilesAsync();
            string fileFormat = String.Format("{0}_{1}_{2}_", testerId, testOrder, testName);
            foreach (StorageFile file in fileList)
            {
                if (!file.Name.StartsWith(fileFormat))
                    continue;

                await file.DeleteAsync();
            }
        }

        public static bool ExportRawResult(StorageFolder orgFolder, StorageFolder targetFolder, int testExecId)
        {
            DatabaseManager dbManager = DatabaseManager.Instance;

            TestExec testExec = dbManager.GetTestExec(testExecId);
            Tester tester = dbManager.GetTester(testExec.TesterId);

            List<TestSetItem> testSetItems = dbManager.GetTestSetItems(testExec.TestSetId);
            string testerName = tester.GetTesterName(true, true, true);

            ExportRawResultItem(orgFolder, targetFolder, tester.Id.ToString(), testerName, testSetItems);

            return true;
        }


        public static async Task<bool> SaveResult(StorageFolder orgFolder, StorageFolder targetFolder, int testExecId)
        {
            if (targetFolder == null)
                return false;

            if (AppConfig.Instance.UseJamoSeperation == true)
                await exportDBResult(orgFolder, targetFolder, testExecId);

            ExportRawResult(orgFolder, targetFolder, testExecId);

            return true;
        }

        public static async Task<bool> SaveResult(int testExecId)
        {
            StorageFolder targetFolder = await GetSaveFolder();
            DatabaseManager dbManager = DatabaseManager.Instance;
            TestExec testExec = dbManager.GetTestExec(testExecId);

            StorageFolder orgFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(testExec.TesterId.ToString(), CreationCollisionOption.OpenIfExists);

            return await SaveResult(orgFolder, targetFolder, testExecId);
        }

        public static async Task<bool> CaptureInkCanva_PreTest(InkCanvas inkCanvas, TestExec testExec)
        {
            // 음.............. ㅋㅋㅋㅋㅋㅋㅋㅋ
            string file_name = testExec.TesterId + "_char_0_last.png";
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(testExec.TesterId.ToString(), CreationCollisionOption.OpenIfExists);
            StorageFile file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

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

        public static async Task<bool> CaptureInkCanvas(int testOrder, string testName, InkCanvas inkCanvas, Border borderUI, List<Point> orgLines, List<DiffData> diffResults, TestExec testExec, TestSetItem setItem)
        {
            string file_name = String.Format("{0}_{1}_{2}_{3}_last.png", testExec.TesterId, testOrder, testName, setItem.Number);
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(testExec.TesterId.ToString(), CreationCollisionOption.OpenIfExists);
            StorageFile file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

            var displayInformation = DisplayInformation.GetForCurrentView();
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget rtb = new CanvasRenderTarget(device, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96); // 96 쓰는게 맞나? or dpi 받아서 써야되나?

            using (var ds = rtb.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                if (orgLines != null)
                {
                    for (int i = 0; i < orgLines.Count - 1; i++)
                        ds.DrawLine((float)orgLines[i].X, (float)orgLines[i].Y, (float)orgLines[i + 1].X, (float)orgLines[i + 1].Y, Colors.Black, 2.0f);
                }

                ds.DrawInk(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
                if (borderUI != null && testExec.ShowBorder)
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


            file_name = String.Format("{0}_{1}_{2}_{3}_diff.png", testExec.TesterId, testOrder, testName, setItem.Number);
            StorageFile calcFile = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

            var calcStream = await calcFile.OpenAsync(FileAccessMode.ReadWrite);
            var calcEncoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, calcStream);

            using (var ds = rtb.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                if (orgLines != null)
                {
                    for (int i = 0; i < orgLines.Count - 1; i++)
                        ds.DrawLine(toVector(orgLines[i]), toVector(orgLines[i + 1]), Colors.Blue, 2.0f);
                }

                ds.DrawInk(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());

                if (diffResults != null)
                {
                    foreach (var diffResult in diffResults)
                    {
                        if (diffResult.hasValue)
                            ds.DrawLine(toVector(diffResult.org), toVector(diffResult.drawn), Colors.Red);
                        else
                            ds.DrawCircle(toVector(diffResult.org), 2, Colors.Red);
                    }
                }

                if (borderUI != null && testExec.ShowBorder)
                    DrawGuideLineInImage(borderUI, ds);
            }

            pixelBuffer = rtb.GetPixelBytes();
            pixels = pixelBuffer.ToArray();

            calcEncoder.SetPixelData(BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)inkCanvas.ActualWidth,
                (uint)inkCanvas.ActualHeight,
                displayInformation.RawDpiX,
                displayInformation.RawDpiY,
                pixels);

            await calcEncoder.FlushAsync();
            calcStream.Dispose();

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

        public static Vector2 toVector(Point p)
        {
            return new Vector2((float)p.X, (float)p.Y);
        }

        public static async Task<bool> CaptureInkCanvasForStroke(int testOrder, string testName, InkCanvas inkCanvas, Border borderUI, List<Point> orgLines, TestExec testExec, TestSetItem setItem)
        {
            // 음.............. ㅋㅋㅋㅋㅋㅋㅋㅋ
            string file_name = String.Format("{0}_{1}_{2}_{3}.gif", testExec.TesterId, testOrder, testName, setItem.Number);
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(testExec.TesterId.ToString(), CreationCollisionOption.OpenIfExists);
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
                PropertyType.UInt16
                );
            propertySet.Add("/grctlext/Delay", propertyValue);

            // 아오 복붙 ㅋㅋㅋ... ㅠㅠ

            // 초기화면
            // 한프레임...?
            using (var ds = rtb.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                if( orgLines != null)
                {
                    for( int i = 0; i < orgLines.Count - 1; i++)
                        ds.DrawLine((float)orgLines[i].X, (float)orgLines[i].Y, (float)orgLines[i+1].X, (float)orgLines[i + 1].Y, Colors.Black, 2.0f);
                }

                
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
                    if (orgLines != null)
                    {
                        for (int i = 0; i < orgLines.Count - 1; i++)
                            ds.DrawLine((float)orgLines[i].X, (float)orgLines[i].Y, (float)orgLines[i + 1].X, (float)orgLines[i + 1].Y, Colors.Black, 2.0f);
                    }
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
                if (orgLines != null)
                {
                    for (int i = 0; i < orgLines.Count - 1; i++)
                        ds.DrawLine((float)orgLines[i].X, (float)orgLines[i].Y, (float)orgLines[i + 1].X, (float)orgLines[i + 1].Y, Colors.Black, 2.0f);
                }
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


            // 개별 stroke들은 한번 넘어갈 때 마다 png 이미지로 생성을 한다. 
            var format = getTextFormat();
            newStrokeList.Clear();
            foreach (var stroke in strokeList)
            {
                newStrokeList.Add(stroke);
                // png 이미지 생성
                using (var ds = rtb.CreateDrawingSession())
                {
                    ds.Clear(Colors.White);
                    if (orgLines != null)
                    {
                        for (int i = 0; i < orgLines.Count - 1; i++)
                            ds.DrawLine((float)orgLines[i].X, (float)orgLines[i].Y, (float)orgLines[i + 1].X, (float)orgLines[i + 1].Y, Colors.Black, 2.0f);
                    }

                    ds.DrawInk(newStrokeList);

                    var point = new Vector2((float)inkCanvas.ActualWidth - 100, 30);
                    ds.DrawText(newStrokeList.Count().ToString(), point, Colors.Black, format);

                    ds.DrawRectangle(stroke.BoundingRect, Colors.Red);

                    if (borderUI != null && testExec.ShowBorder)
                        DrawGuideLineInImage(borderUI, ds);
                }
                pixelBuffer = rtb.GetPixelBytes();
                string filename = String.Format("{0}_{1}_{2}_{3}_stroke_{4}.png", testExec.TesterId, testOrder, testName, setItem.Number.ToString(), newStrokeList.Count());

                await SaveStrokeAsImage(storageFolder, filename, pixelBuffer, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight);
            }

            return true;
        }

        // p1->p2, p3->p4
        public static void FindIntersection(Point p1, Point p2, Point p3, Point p4, out bool isIntersected, out Point intersectedPt)
        {
            // Get the segments' parameters.
            double dx12 = p2.X - p1.X;
            double dy12 = p2.Y - p1.Y;
            double dx34 = p4.X - p3.X;
            double dy34 = p4.Y - p3.Y;

            // Solve for t1 and t2
            double denominator = (dy12 * dx34 - dx12 * dy34);

            double t1 = ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34) / denominator;
            if (double.IsInfinity(t1))
            {
                // The lines are parallel (or close enough to it).
                isIntersected = false;
                intersectedPt = new Point(float.NaN, float.NaN);
                return;
            }

            double t2 = ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12) / -denominator;

            // Find the point of intersection.
            intersectedPt = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            isIntersected = ((t1 >= 0) && (t1 <= 1) && (t2 >= 0) && (t2 <= 1));
        }


        private static CanvasTextFormat getTextFormat()
        {
            var format = new CanvasTextFormat();
            format.FontSize = 40;
            format.WordWrapping = CanvasWordWrapping.NoWrap;
            format.TrimmingGranularity = CanvasTextTrimmingGranularity.None;
            format.HorizontalAlignment = CanvasHorizontalAlignment.Left;
            return format;
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

        private static async Task SaveStrokeAsImage(StorageFolder folder, string filename, byte[] bytes, int width, int height)
        {
            var bmp = new WriteableBitmap(width, height);
            using (var stream = bmp.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }

            StorageFile file = await folder.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);
            using (IRandomAccessStream outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (MemoryStream imageStream = new MemoryStream())
                {
                    using (Stream pixelBufferStream = bmp.PixelBuffer.AsStream())
                    {
                        pixelBufferStream.CopyTo(imageStream);
                    }

                    BitmapEncoder encoder = await BitmapEncoder
                        .CreateAsync(BitmapEncoder.PngEncoderId, outputStream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                        (uint)bmp.PixelWidth,
                        (uint)bmp.PixelHeight,
                        dpiX: 96,
                        dpiY: 96,
                        pixels: imageStream.ToArray());
                    await encoder.FlushAsync();
                }
            }
        }

        public static List<Point> generateClockWiseSpiralPoints(Point startPt, double totalRadius, bool counterClockWise)
        {
            List<Point> points = new List<Point>();

            double radius2 = totalRadius / 8;
            double radius1 = radius2 / 2;
            double radiusStep = radius2;

            if (counterClockWise)
            {
                Point start2Pt = new Point(startPt.X - radius1, startPt.Y);
                for (int i = 0; i < 4; i++)
                {
                    for (double angle = 0; angle >= -180; angle -= 0.5)
                    {
                        double radian = Math.PI * angle / 180;
                        double x = start2Pt.X + radius1 * Math.Cos(radian);
                        double y = start2Pt.Y + radius1 * Math.Sin(radian);
                        points.Add(new Point(x, y));
                    }
                    radius1 += radiusStep;

                    for (double angle = -180; angle >= -360; angle -= 0.5)
                    {
                        double radian = Math.PI * angle / 180;
                        double x = startPt.X + radius2 * Math.Cos(radian);
                        double y = startPt.Y + radius2 * Math.Sin(radian);
                        points.Add(new Point(x, y));
                    }
                    radius2 += radiusStep;
                }
            }
            else
            {
                Point start2Pt = new Point(startPt.X + radius1, startPt.Y);
                for (int i = 0; i < 4; i++)
                {
                    for (double angle = 180; angle <= 360; angle += 0.5)
                    {
                        double radian = Math.PI * angle / 180;
                        double x = start2Pt.X + radius1 * Math.Cos(radian);
                        double y = start2Pt.Y + radius1 * Math.Sin(radian);
                        points.Add(new Point(x, y));
                    }
                    radius1 += radiusStep;

                    for (double angle = 0; angle <= 180; angle += 0.5)
                    {
                        double radian = Math.PI * angle / 180;
                        double x = startPt.X + radius2 * Math.Cos(radian);
                        double y = startPt.Y + radius2 * Math.Sin(radian);
                        points.Add(new Point(x, y));
                    }
                    radius2 += radiusStep;

                }
            }
            

            return points;
        }

        public static double getDistance(Point a, Point b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        public static double mmToPixels(double mm)
        {
            DisplayInformation di = DisplayInformation.GetForCurrentView();
            // 1 inch == 25.4 mm
            return di.RawDpiX * (mm / 25.4f) / di.RawPixelsPerViewPixel;
        }

        public static int calculateAge(string birthDate, string execDate)
        {
            var birthYear = Int32.Parse(birthDate.Substring(0, 4));
            var birthMonth = Int32.Parse(birthDate.Substring(4, 2));
            var birthDay = Int32.Parse(birthDate.Substring(6, 2));

            var execYear = Int32.Parse(execDate.Substring(0, 4));
            var execMonth = Int32.Parse(execDate.Substring(4, 2));
            var execDay = Int32.Parse(execDate.Substring(6, 2));


            int age = execYear - birthYear;
            if ((execMonth < birthMonth)
                || (execMonth == birthMonth && execDay < birthDay))
                age -= 1;

            return age;
        }

        public static int calculateEducation(string education)
        {
            int edu = 0;
            if( education.StartsWith("초등학교 졸업"))
            {
                edu = 6;
            }
            else if(education.StartsWith("중학교 졸업"))
            {
                edu = 9;
            }
            else if (education.StartsWith("고등학교 졸업"))
            {
                edu = 12;
            }
            else if (education.StartsWith("대학교 이상 졸업"))
            {
                edu = 16;
            }
            else if (education.StartsWith("초등학교 중퇴"))
            {
                edu = Int32.Parse(education.Substring(8, 1));
            }
            else if (education.StartsWith("중학교 중퇴"))
            {
                edu = 6 + Int32.Parse(education.Substring(7, 1));
            }
            else if (education.StartsWith("고등학교 중퇴"))
            {
                edu = 9 + Int32.Parse(education.Substring(8, 1));
            }
            else if (education.StartsWith("대학교 이상 중퇴"))
            {
                edu = 12 + Int32.Parse(education.Substring(10, 1));
            }

            return edu;
        }
    }

}
