using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

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
            if( folder != null )
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                return folder;
            }
            return null;
        }

        public static async Task<bool> SaveResults( List<int> testExecList)
        {
            // 파일 위치 picker 필요
            Windows.Storage.StorageFolder rootFolder = await GetSaveFolder();
            if (rootFolder == null)
                return false;


            DatabaseManager dbManager = DatabaseManager.Instance;
            foreach( int testExecId in testExecList)
            {
                TestExec testExec = dbManager.GetTestExec(testExecId);
                Tester tester = dbManager.GetTester(testExec.TesterId);

                string newFolderName = tester.Name + "_" + tester.birthday;

                Windows.Storage.StorageFolder subFolder = await rootFolder.CreateFolderAsync(newFolderName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                await SaveResult(subFolder, testExecId);
            }

            return true;
        }
        public static async Task<bool> SaveResult(Windows.Storage.StorageFolder folder, int testExecId)
        {
            if (folder == null)
                return false;

            DatabaseManager dbManager = DatabaseManager.Instance;

            Windows.Storage.StorageFile resultFile = await folder.CreateFileAsync("result.csv", Windows.Storage.CreationCollisionOption.ReplaceExisting);


            TestExec testExec = dbManager.GetTestExec(testExecId);
            Tester tester = dbManager.GetTester(testExec.TesterId);
            List<TestSetItem> testSetItems = dbManager.GetTestSetItems(testExec.TestSetId);


            // tester 정보
            await Windows.Storage.FileIO.WriteTextAsync(resultFile, tester.Name + "(" + tester.Gender + "),");
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, tester.birthday + "," );
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, tester.Education.ToString() + "\r\n");

            // 각 항목별 헤더
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, "단어,");

            await Windows.Storage.FileIO.AppendTextAsync(resultFile, "한글자,");
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, "초성시간(ms),");
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, "간격(ms),");
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, "중성시간(ms),");
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, "간격(ms),");
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, "종성시간(ms),");
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, "초성평균압력(0~1),");
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, "중성평균압력(0~1),");
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, "종성평균압력(0~1)"); 
            await Windows.Storage.FileIO.AppendTextAsync(resultFile, "\r\n");

            foreach (var item in testSetItems)
            {
                List<TestExecResult> results = dbManager.GetTextExecResults(testExec.Id, item.Id);

                for( int i = 0; i < results.Count; ++i )
                {
                    if (i == 0)
                        await Windows.Storage.FileIO.AppendTextAsync(resultFile, item.Word);
                    await Windows.Storage.FileIO.AppendTextAsync(resultFile, ",");

                    await Windows.Storage.FileIO.AppendTextAsync(resultFile, item.Word.ElementAt(results[i].TestSetItemCharIdx).ToString() + "," );
                    await Windows.Storage.FileIO.AppendTextAsync(resultFile, results[i].ChosungTime.ToString("F3") + "," );
                    await Windows.Storage.FileIO.AppendTextAsync(resultFile, results[i].FirstIdleTIme.ToString("F3") + "," );
                    await Windows.Storage.FileIO.AppendTextAsync(resultFile, results[i].JoongsungTime.ToString("F3") + "," );
                    await Windows.Storage.FileIO.AppendTextAsync(resultFile, results[i].SecondIdelTime.ToString("F3") + "," );
                    await Windows.Storage.FileIO.AppendTextAsync(resultFile, results[i].JongsungTime.ToString("F3") + "," );
                    await Windows.Storage.FileIO.AppendTextAsync(resultFile, results[i].ChosungAvgPressure.ToString("F6") + "," );
                    await Windows.Storage.FileIO.AppendTextAsync(resultFile, results[i].JoongsungAvgPressure.ToString("F6") + "," );
                    await Windows.Storage.FileIO.AppendTextAsync(resultFile, results[i].JongsungAvgPressure.ToString("F6") );
                    await Windows.Storage.FileIO.AppendTextAsync(resultFile, "\r\n");
                }
            }
            
            // 애니메이션 gif 저장

            // 또 다른거 있나...? 

            return true;
        }
 
        public static async Task<bool> SaveResult(int testExecId)
        {
            Windows.Storage.StorageFolder folder = await GetSaveFolder();
            return await SaveResult(folder, testExecId); 
        }
    }
}
