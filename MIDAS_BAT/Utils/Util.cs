using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using MIDAS_BAT.Data;
using MIDAS_BAT.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Windows.UI.ViewManagement;
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

            for (int i = curTestOrder + 1; i < testOrder.Length; i++)
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

                string newFolderName = tester.GetTesterName(testExec.Datetime);

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

            string testerName = tester.GetTesterName(testExec.Datetime);
            StorageFile resultFile = await targetFolder.CreateFileAsync(testerName + "_결과.csv", CreationCollisionOption.ReplaceExisting);

            List<TestSetItem> testSetItems = dbManager.GetTestSetItems(testExec.TestSetId);

            StringBuilder builder = new StringBuilder();
            builder.Append(tester.GetTesterName(testExec.Datetime) + ",");
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
            IReadOnlyList<StorageFile> allFiles = await orgFolder.GetFilesAsync();
            foreach (var file in allFiles)
            {
                if (file.Name.EndsWith("_canvas.gif"))
                    continue;

                await file.CopyAsync(targetFolder, file.Name, NameCollisionOption.ReplaceExisting);
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
            foreach (StorageFile file in fileList)
            {
                if (!file.Name.Contains(testName))
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
            string testerName = tester.GetTesterName(testExec.Datetime);

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

        public static async Task<bool> CaptureInkCanvas(int testOrder, string testName, InkCanvas inkCanvas, Border borderUI, List<List<Point>> orgLines, List<List<BATPoint>> drawLines, List<List<DiffData>> diffResults, TestExec testExec, TestSetItem setItem)
        {
            await SaveInkPng(testOrder, testName, inkCanvas, borderUI, orgLines, testExec, setItem);
            await SaveLastDrawLinePng(testOrder, testName, inkCanvas, borderUI, orgLines, drawLines, testExec, setItem);
            await SaveDiffResult(testOrder, testName, inkCanvas, borderUI, orgLines, drawLines, diffResults, testExec, setItem);

            return true;
        }

        public static async Task SaveInkPng(int testOrder, string testName, InkCanvas inkCanvas, Border borderUI, List<List<Point>> orgLines, TestExec testExec, TestSetItem setItem)
        {
            DatabaseManager databaseManager = DatabaseManager.Instance;
            Tester tester = databaseManager.GetTester(testExec.TesterId);

            string file_name = String.Format("{0}_{1}_잉크.png", tester.GetTesterName(testExec.Datetime), testName);
            if(testOrder.Equals(TestPage.TEST_ORDER))
                file_name = String.Format("{0}_{1}_{2}_{3}_잉크.png", tester.GetTesterName(testExec.Datetime), testName, setItem.Number, setItem.Word);

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
                drawOrgLine(ds, orgLines);

                var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
                double radiusStep = Util.mmToPixels(15.0f);

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
        }

        public static async Task SaveLastDrawLinePng(int testOrder, string testName, InkCanvas inkCanvas, Border borderUI, List<List<Point>> orgLines, List<List<BATPoint>> drawLines, TestExec testExec, TestSetItem setItem)
        {
            DatabaseManager databaseManager = DatabaseManager.Instance;
            Tester tester = databaseManager.GetTester(testExec.TesterId);

            string file_name = String.Format("{0}_{1}_최종.png", tester.GetTesterName(testExec.Datetime), testName);
            if (testOrder.Equals(TestPage.TEST_ORDER))
                file_name = String.Format("{0}_{1}_{2}_{3}_최종.png", tester.GetTesterName(testExec.Datetime), testName, setItem.Number, setItem.Word);

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
                drawOrgLine(ds, orgLines);
                //ds.DrawInk(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
                drawDrawLine(ds, drawLines);
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
        }

        public static async Task SaveLastDrawLinePngForSpiral(int testOrder, string testName, InkCanvas inkCanvas, Border borderUI, List<List<Point>> orgLines, List<List<BATPoint>> drawLines, TestExec testExec, TestSetItem setItem, bool counterClockWise)
        {
            DatabaseManager databaseManager = DatabaseManager.Instance;
            Tester tester = databaseManager.GetTester(testExec.TesterId);

            string file_name = String.Format("{0}_{1}_최종.png", tester.GetTesterName(testExec.Datetime), testName);
            if(testOrder.Equals(TestPage.TEST_ORDER))
                file_name = String.Format("{0}_{1}_{2}_{3}_최종.png", tester.GetTesterName(testExec.Datetime), testName, setItem.Number, setItem.Word);

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
                drawOrgLine(ds, orgLines);

                var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
                double radiusStep = Util.mmToPixels(15.0f);

                List<List<BATPoint>> splitDrawLines = null;
                Point orgCenter = new Point(bounds.Width / 2, bounds.Height / 2);
                if (counterClockWise)
                {
                    splitDrawLines = Util.splitDrawing(orgCenter, drawLines, true, true);
                }
                else
                {
                    splitDrawLines = Util.splitDrawing(orgCenter, drawLines, true, false);
                }

                Color[] colors = { Color.FromArgb(255, 255, 0, 0), Color.FromArgb(255, 255, 255, 73), Color.FromArgb(255, 73, 73, 255), Color.FromArgb(255, 0, 255, 0), Color.FromArgb(255, 255, 0, 255) };
                int colorIndex = 0;
                foreach (var splitDrawLine in splitDrawLines)
                {
                    for (int i = 0; i < splitDrawLine.Count - 1; i++)
                    {
                        if (splitDrawLine[i].isEnd)
                            continue;

                        ds.DrawLine(toVector(splitDrawLine[i].point), toVector(splitDrawLine[i + 1].point), colors[colorIndex]);
                    }
                    colorIndex += 1;
                    if (colorIndex >= colors.Length)
                        colorIndex = 0;
                }
                //ds.DrawInk(strokes);
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
        }

        public static async Task SaveDiffResult(int testOrder, string testName, InkCanvas inkCanvas, Border borderUI, List<List<Point>> orgLines, List<List<BATPoint>> drawLines, List<List<DiffData>> diffResults, TestExec testExec, TestSetItem setItem)
        {
            DatabaseManager databaseManager = DatabaseManager.Instance;
            Tester tester = databaseManager.GetTester(testExec.TesterId);

            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(testExec.TesterId.ToString(), CreationCollisionOption.OpenIfExists);
            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget rtb = new CanvasRenderTarget(device, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96); // 96 쓰는게 맞나? or dpi 받아서 써야되나?
            var displayInformation = DisplayInformation.GetForCurrentView();
            for (int i = 0; i < diffResults.Count; i++)
            {
                string file_name = String.Format("{0}_{1}_차이_{2}.png", tester.GetTesterName(testExec.Datetime), testName, i);
                //String.Format("{0}_{1}_{2}_{3}_diff_{4}.png", testExec.TesterId, testOrder, testName, setItem.Number, i);
                StorageFile calcFile = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

                var calcStream = await calcFile.OpenAsync(FileAccessMode.ReadWrite);
                var calcEncoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, calcStream);

                using (var ds = rtb.CreateDrawingSession())
                {
                    ds.Clear(Colors.White);
                    drawOrgLine(ds, orgLines);

                    //ds.DrawInk(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
                    drawDrawLine(ds, drawLines);

                    if (diffResults != null)
                    {
                        foreach (var item in diffResults[i])
                        {
                            if (item.hasValue)
                                ds.DrawLine(toVector(item.org), toVector(item.drawn), Colors.Red);
                            else
                                ds.DrawCircle(toVector(item.org), 2, Colors.Red);
                        }
                    }

                    if (borderUI != null && testExec.ShowBorder)
                        DrawGuideLineInImage(borderUI, ds);
                }

                var pixelBuffer = rtb.GetPixelBytes();
                var pixels = pixelBuffer.ToArray();

                calcEncoder.SetPixelData(BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    (uint)inkCanvas.ActualWidth,
                    (uint)inkCanvas.ActualHeight,
                    displayInformation.RawDpiX,
                    displayInformation.RawDpiY,
                    pixels);

                await calcEncoder.FlushAsync();
                calcStream.Dispose();
            }
        }

        public static async Task<bool> CaptureInkCanvasForSpiral(int testOrder, string testName, InkCanvas inkCanvas, Border borderUI, List<List<Point>> orgLines, List<List<BATPoint>> drawLines, List<List<DiffData>> diffResults, TestExec testExec, TestSetItem setItem, bool counterClockWise)
        {
            await SaveInkPng(testOrder, testName, inkCanvas, borderUI, orgLines, testExec, setItem);
            await SaveLastDrawLinePngForSpiral(testOrder, testName, inkCanvas, borderUI, orgLines, drawLines, testExec, setItem, counterClockWise);
            await SaveDiffResult(testOrder, testName, inkCanvas, borderUI, orgLines, drawLines, diffResults, testExec, setItem);

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


        public static async Task<bool> CaptureInkCanvasForStroke(int testOrder, string testName, InkCanvas inkCanvas, Border borderUI, List<List<Point>> orgLines, TestExec testExec, TestSetItem setItem)
        {
            DatabaseManager databaseManager = DatabaseManager.Instance;
            Tester tester = databaseManager.GetTester(testExec.TesterId);

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
                drawOrgLine(ds, orgLines);
                if (orgLines != null)
                {
                    foreach (var points in orgLines)
                    {
                        for (int i = 0; i < points.Count - 1; i++)
                            ds.DrawLine((float)points[i].X, (float)points[i].Y, (float)points[i + 1].X, (float)points[i + 1].Y, Color.FromArgb(255, 73, 73, 73), 2.0f);
                    }

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
                    drawOrgLine(ds, orgLines);
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
                drawOrgLine(ds, orgLines);
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
                    drawOrgLine(ds, orgLines);

                    ds.DrawInk(newStrokeList);

                    var point = new Vector2((float)inkCanvas.ActualWidth - 100, 30);
                    ds.DrawText(newStrokeList.Count().ToString(), point, Colors.Black, format);

                    ds.DrawRectangle(stroke.BoundingRect, Colors.Red);

                    if (borderUI != null && testExec.ShowBorder)
                        DrawGuideLineInImage(borderUI, ds);
                }
                pixelBuffer = rtb.GetPixelBytes();
                string filename = String.Format("{0}_{1}_{2}_획순_{3}.png", tester.GetTesterName(testExec.Datetime), testName, setItem.Number.ToString(), newStrokeList.Count());

                await SaveStrokeAsImage(storageFolder, filename, pixelBuffer, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight);
            }

            return true;
        }

        private static void drawOrgLine(CanvasDrawingSession ds, List<List<Point>> orgLines)
        {
            if (orgLines == null)
                return;

            foreach (var points in orgLines)
            {
                for (int i = 0; i < points.Count - 1; i++)
                    ds.DrawLine((float)points[i].X, (float)points[i].Y, (float)points[i + 1].X, (float)points[i + 1].Y, Color.FromArgb(255, 73, 73, 73), 2.0f);
            }
        }


        private static void drawDrawLine(CanvasDrawingSession ds, List<List<BATPoint>> drawLines)
        {
            foreach (var drawLine in drawLines)
            {
                for (int i = 0; i < drawLine.Count - 1; i++)
                {
                    if (drawLine[i].isEnd)
                        continue;

                    ds.DrawLine(toVector(drawLine[i].point), toVector(drawLine[i + 1].point), Colors.Black);
                }
            }
        }

        public static async Task<bool> CaptureInkCanvasForStroke2(int testOrder, string testName, InkCanvas inkCanvas, Border borderUI, List<List<Point>> orgLines, List<List<BATPoint>> drawPoints, TestExec testExec, TestSetItem setItem)
        {
            DatabaseManager databaseManager = DatabaseManager.Instance;
            Tester tester = databaseManager.GetTester(testExec.TesterId);

            string file_name = String.Format("{0}_{1}_{2}.gif", tester.GetTesterName(testExec.Datetime), testName, setItem.Number);
            StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(testExec.TesterId.ToString(), CreationCollisionOption.OpenIfExists);
            StorageFile file = await storageFolder.CreateFileAsync(file_name, CreationCollisionOption.ReplaceExisting);

            var displayInformation = DisplayInformation.GetForCurrentView();
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.GifEncoderId, stream);

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget rtb = new CanvasRenderTarget(device, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96); // 96 쓰는게 맞나? or dpi 받아서 써야되나?
            //IReadOnlyList<InkStroke> strokeList = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            List<List<BATPoint>> newStrokeList = new List<List<BATPoint>>();

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
                drawOrgLine(ds, orgLines);
                // ds.DrawInk(newStrokeList);
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
            foreach (var drawPoint in drawPoints)
            {
                newStrokeList.Add(drawPoint);

                // 한프레임...?
                using (var ds = rtb.CreateDrawingSession())
                {
                    ds.Clear(Colors.White);
                    drawOrgLine(ds, orgLines);
                    drawDrawLine(ds, newStrokeList);
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
                drawOrgLine(ds, orgLines);
                drawDrawLine(ds, newStrokeList);
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
            foreach (var drawPoint in drawPoints)
            {
                newStrokeList.Add(drawPoint);
                // png 이미지 생성
                using (var ds = rtb.CreateDrawingSession())
                {
                    ds.Clear(Colors.White);
                    drawOrgLine(ds, orgLines);
                    drawDrawLine(ds, newStrokeList);

                    var point = new Vector2((float)inkCanvas.ActualWidth - 100, 30);
                    ds.DrawText(newStrokeList.Count().ToString(), point, Colors.Black, format);

                    Rect? rect = getBoundingBox(drawPoint);
                    if( rect.HasValue )
                        ds.DrawRectangle(rect.Value, Colors.Red);

                    if (borderUI != null && testExec.ShowBorder)
                        DrawGuideLineInImage(borderUI, ds);
                }
                pixelBuffer = rtb.GetPixelBytes();
                string filename = String.Format("{0}_{1}_{2}_획순_{3}.png", tester.GetTesterName(testExec.Datetime), testName, setItem.Number.ToString(), newStrokeList.Count());

                await SaveStrokeAsImage(storageFolder, filename, pixelBuffer, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight);
            }

            return true;
        }

        public static Rect? getBoundingBox(List<Point> points)
        {
            if (points.Count == 0 )
            {
                return null;
            }

            double minX = points.Min(p => p.X);
            double minY = points.Min(p => p.Y);
            double maxX = points.Max(p => p.X);
            double maxY = points.Max(p => p.Y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
        public static Rect? getBoundingBox(List<BATPoint> points)
        {
            if (points.Count == 0)
            {
                return null;
            }

            double minX = points.Min(p => p.point.X);
            double minY = points.Min(p => p.point.Y);
            double maxX = points.Max(p => p.point.X);
            double maxY = points.Max(p => p.point.Y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public static Rect? getBoundingBox2(List<List<BATPoint>> points)
        {
            List<BATPoint> flattenDrawLines = new List<BATPoint>();
            foreach (var drawLine in points)
            {
                foreach (var drawPoint in drawLine)
                    flattenDrawLines.Add(drawPoint);
            }

            if (flattenDrawLines.Count == 0)
            {
                return null;
            }

            double minX = flattenDrawLines.Min(p => p.point.X);
            double minY = flattenDrawLines.Min(p => p.point.Y);
            double maxX = flattenDrawLines.Max(p => p.point.X);
            double maxY = flattenDrawLines.Max(p => p.point.Y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
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

            StorageFile file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
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

        public static List<List<Point>> generateClockWiseSpiralPoints(Point startPt, double totalRadius, int cycleCount, bool counterClockWise)
        {
            List<List<Point>> totalPoints = new List<List<Point>>();

            double radius2 = totalRadius / ( cycleCount * 2 );
            double radius1 = radius2 / 2;
            double radiusStep = radius2;

            if (counterClockWise)
            {
                Point start2Pt = new Point(startPt.X - radius1, startPt.Y);
                for (int i = 0; i < cycleCount; i++)
                {
                    List<Point> points = new List<Point>();
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

                    totalPoints.Add(points);
                }
            }
            else
            {
                Point start2Pt = new Point(startPt.X + radius1, startPt.Y);
                for (int i = 0; i < cycleCount; i++)
                {
                    List<Point> points = new List<Point>();
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

                    totalPoints.Add(points);
                }
            }

            return totalPoints;
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

        public static List<List<BATPoint>> splitDrawingToHalfSpiral(Point orgCenter, List<BATPoint> drawLine, bool counterClockwise)
        {
            List<List<BATPoint>> splitDrawLines = new List<List<BATPoint>>();
            Point pt1 = new Point(0, orgCenter.Y);
            Point pt2 = new Point(100000, orgCenter.Y);

            int targetIdx = -1;
            for(int i = 0; i < drawLine.Count - 1; i++)
            {
                Vector2 vec = Util.toVector(drawLine[i + 1].point) - Util.toVector(drawLine[i].point);
                Util.FindIntersection(pt1, pt2, drawLine[i].point, drawLine[i + 1].point, out bool isIntersected, out Point intersectedPt);
                if (isIntersected && vec.Y > 0)
                {
                    targetIdx = i + 1;
                    break;
                }
            }

            if( targetIdx != -1)
            {
                splitDrawLines.Add(drawLine.GetRange(0, targetIdx + 1));
                splitDrawLines.Add(drawLine.GetRange(targetIdx, drawLine.Count - targetIdx));
            }
            else
            {
                splitDrawLines.Add(drawLine);
            }

            return splitDrawLines;
        }

        /*
        public static List<List<BATPoint>> splitDrawing(Point orgCenter, List<List<BATPoint>> drawLines, bool cutSmallFirst, bool counterClockwise)
        {
            List<List<BATPoint>> splitDrawLines = new List<List<BATPoint>>();

            List<BATPoint> splitDrawLine = new List<BATPoint>();

            Point endPoint = orgCenter;
            Point pt1 = new Point(0, orgCenter.Y);
            Point pt2 = new Point(100000, orgCenter.Y);
            if (counterClockwise)
                endPoint.X += 100000;
            else
                endPoint.X -= 100000;

            int cycleCnt = 0;
            bool hasSmallFirst = false;
            foreach (var drawLine in drawLines)
            {
                for( int i = 0; i < drawLine.Count - 1; i++)
                {
                    // 초반 조금 무시할 것인지 말것인지
                    if ( cycleCnt == 0
                        && splitDrawLine.Count == 0
                        && drawLine[i].point.Y > orgCenter.Y)
                    {
                        hasSmallFirst = true;
                        if (cutSmallFirst)
                            continue;
                    }

                    // cutSmallHead인 경우, 첫번째 바퀴에서 y가 0 과 겹치는 line을 만들 수 없기에 바로 전 점은 붙여준다.
                    if(cutSmallFirst
                        && cycleCnt == 0
                        && splitDrawLine.Count == 0
                        && hasSmallFirst 
                        && i > 0)
                    {
                        splitDrawLine.Add(drawLine[i - 1]);
                    }


                    splitDrawLine.Add(drawLine[i]);

                    Util.FindIntersection(orgCenter, endPoint, drawLine[i].point, drawLine[i+1].point, out bool isIntersected, out Point intersectedPt);
                    if( isIntersected )
                    {
                        BATPoint point = new BATPoint(intersectedPt, (drawLine[i].pressure + drawLine[i + 1].pressure) / 2, (drawLine[i].timestamp + drawLine[i + 1].timestamp) / 2);
                        splitDrawLine.Add(point);
                        splitDrawLines.Add(splitDrawLine);

                        splitDrawLine = new List<BATPoint>();
                        splitDrawLine.Add(point);
                    }
                }
                splitDrawLine.Add(drawLine.Last() );

                cycleCnt += 1;
            }

            if( !cutSmallFirst && hasSmallFirst && splitDrawLines.Count >= 2)
            {
                splitDrawLines[0].AddRange(splitDrawLines[1]);
                splitDrawLines.RemoveAt(1);
            }

            splitDrawLines.Add(splitDrawLine);

            return splitDrawLines;
        }
        */

        public static List<List<BATPoint>> splitDrawing(Point orgCenter, List<List<BATPoint>> drawLines, bool cutSmallFirst, bool counterClockwise)
        {
            List<List<BATPoint>> splitDrawLines = new List<List<BATPoint>>();

            List<BATPoint> splitDrawLine = new List<BATPoint>();

            Point pt1 = new Point(0, orgCenter.Y);
            Point pt2 = new Point(100000, orgCenter.Y);

            int cycleCnt = 0;
            bool firstFound = false;
            double prevVec = 1.0;
            foreach (var drawLine in drawLines)
            {
                for (int i = 0; i < drawLine.Count - 1; i++)
                {
                    splitDrawLine.Add(drawLine[i]);

                    Vector2 vec = Util.toVector(drawLine[i+1].point) - Util.toVector(drawLine[i].point);
                    Util.FindIntersection(pt1, pt2, drawLine[i].point, drawLine[i + 1].point, out bool isIntersected, out Point intersectedPt);
                    if (isIntersected)
                    {
                        if ( vec.Y < 0.0 && prevVec > 0.0)
                        {
                            BATPoint point = new BATPoint(intersectedPt, (drawLine[i].pressure + drawLine[i + 1].pressure) / 2, (drawLine[i].timestamp + drawLine[i + 1].timestamp) / 2);
                            // 여기 들어왔는데 firstFound == false인 경우, 화면 기준 위에서 선이 위에서 아래로 내려가는 지점을 지나지 않고 들어온 케이스(최소 반바퀴 이상은 안 돈 케이스)
                            // cutSmallFirst == true 인 경우, 이전 점들은 다 날리도록 함. cutSmallFirst == false인 경우에는 그냥 놔둠. 
                            if ( !firstFound ) 
                            {
                                if(cutSmallFirst)
                                    splitDrawLine.Clear();
                            }
                            else
                            {
                                splitDrawLine.Add(point);
                                splitDrawLines.Add(splitDrawLine);

                                splitDrawLine = new List<BATPoint>();
                            }
                            splitDrawLine.Add(point);
                        }

                        firstFound = true;
                        prevVec = vec.Y;
                    }
                }
                splitDrawLine.Add(drawLine.Last());

                cycleCnt += 1;
            }

            /*
            if (!cutSmallFirst && hasSmallFirst && splitDrawLines.Count >= 2)
            {
                splitDrawLines[0].AddRange(splitDrawLines[1]);
                splitDrawLines.RemoveAt(1);
            }
            */

            splitDrawLines.Add(splitDrawLine);

            return splitDrawLines;
        }

        public static double getLength(List<BATPoint> drawLine)
        {
            double dist = 0.0;
            for( int i = 0; i < drawLine.Count - 1; i++)
            {
               dist += Util.getDistance(drawLine[i].point, drawLine[i + 1].point);
            }
            return dist;
        }

        public static double getAngle(Point org, Point target)
        {
            double dist = Util.getDistance(org, target);
            double x = target.X - org.X;  // 역으로 계산함
            double angle = Math.Acos(x / dist) * 180 / Math.PI;
            if (org.Y - target.Y < 0)
                angle = 360 - angle;

            return angle;
        }

        public static double calculateStdev(IEnumerable<double> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }

    }
}
