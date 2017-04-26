using SQLite.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDAS_BAT
{
    public sealed class DatabaseManager
    {
        private static readonly DatabaseManager instance = new DatabaseManager();

        SQLite.Net.SQLiteConnection conn;
        string path;
        private DatabaseManager()
        {
            path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "bat_db.sqlite");
            conn = new SQLite.Net.SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), path);

            conn.CreateTable<Tester>();
            conn.CreateTable<TestSet>();
            conn.CreateTable<TestSetItem>();
            conn.CreateTable<TestExec>();
            conn.CreateTable<TestExecResult>();

        } 
        public static DatabaseManager Instance
        {
            get
            {
                return instance;
            }
            
        }

        internal List<TestSet> GetTestSet()
        {
            TableQuery<TestSet> tb = conn.Table<TestSet>();
            List<TestSet> list = tb.ToList<TestSet>();
            return list;
        }

        internal Tester GetTester(int testerId)
        {
            IEnumerable<Tester> results = conn.Query<Tester>("SELECT * FROM Tester WHERE Id= '" + testerId + "'");
            if (results.Count() != 1)
                return null;

            return results.ElementAt(0);
        }

        internal List<TestExecResult> GetTextExecResults(int id)
        {
            string query = "SELECT * FROM TestExecResult WHERE TestExecId = '" + id + "'";
            IEnumerable<TestExecResult> results = conn.Query<TestExecResult>(query);
            List<TestExecResult> list = new List<TestExecResult>(results);
            return list;
        }

        internal TestExec GetTestExec(int id)
        {
            IEnumerable<TestExec> results = conn.Query<TestExec>("SELECT * FROM TestExec WHERE Id= '" + id + "'");
            if (results.Count() != 1)
                return null;

            return results.ElementAt(0);
        }

        internal List<TestExec> GetTextExecs( bool reverseOrder)
        {
            TableQuery<TestExec> tb = conn.Table<TestExec>();
            List<TestExec> list = tb.ToList<TestExec>();
            if( reverseOrder )
                list.Reverse();
            return list;
        }

        internal TestSetItem GetTestSetItem(int testSetItemId)
        {
            IEnumerable<TestSetItem> results = conn.Query<TestSetItem>("SELECT * FROM TestSetItem WHERE Id = '" + testSetItemId +  "'");
            if (results.Count() != 1)
                return null;

            return results.ElementAt(0);
        }

        internal void DeleteTestExec(int id)
        {
            TestExec testExec = GetTestExec(id);
            conn.Delete(testExec);
        }

        internal void InsertTestExec(TestExec testExec)
        {
            conn.Insert(testExec);
        }

        internal TestSet GetActiveTestSet()
        {
            IEnumerable<TestSet> results = conn.Query<TestSet>("SELECT * FROM TestSet WHERE Active = '1'");
            if (results.Count() != 1)
                return null;

            return results.ElementAt(0);
        }

        internal void InsertTester(Tester tester)
        {
            conn.Insert(tester);
        }

        public void InsertTestSet( TestSet testSet )
        {
            conn.Insert(testSet);
        }

        internal void DeleteTestExec(TestExec testExec)
        {
            List<TestExecResult> results = GetTestExecResults(testExec.Id);
            foreach (var item in results)
                DeleteTestExecResult(item);

            conn.Delete(testExec);
        }

        private List<TestExecResult> GetTestExecResults(int testExecId )
        {
            string query = "SELECT * FROM TestExecResult WHERE TestExecId = '" + testExecId + "' ORDER BY Id";
            IEnumerable<TestExecResult> results = conn.Query<TestExecResult>(query);
            List<TestExecResult> list = new List<TestExecResult>(results);
            
            return list;
        }

        private void DeleteTestExecResult(TestExecResult item)
        {
            conn.Delete(item);
        }

        internal void InsertTestSetItem(TestSetItem testSetItem)
        {
            conn.Insert(testSetItem);
        }

        internal TestSet GetTestSet( int testSetId)
        {
            IEnumerable<TestSet> results = conn.Query<TestSet>("SELECT * FROM TestSetI WHERE Id = '" + testSetId + "'");
            if (results.Count() != 1)
                return null;

            return results.ElementAt(0);
        }

        internal List<TestSetItem> GetTestSetItems(int testSetId)
        {
            string query = "SELECT * FROM TestSetItem WHERE TestSetId = '" + testSetId + "' ORDER BY Number";
            IEnumerable<TestSetItem> results = conn.Query<TestSetItem>(query);
            List<TestSetItem> list = new List<TestSetItem>(results);
            
            return list;
        }

        internal void DeleteTestSet(TestSet selectedTestSet)
        {
            List<TestSetItem> results = GetTestSetItems(selectedTestSet.Id);
            foreach (var item in results)
                DeleteTestSetItem(item);

            conn.Delete(selectedTestSet);
        }

        internal void DeleteTestSetItem(TestSetItem item)
        {
            conn.Delete(item);
        }

        internal void SetActive(TestSet selectedTestSet)
        {
            TableQuery<TestSet> tb = conn.Table<TestSet>();
            foreach (var item in tb)
            { 
                item.Active = false;
                conn.Update(item);
            }
            selectedTestSet.Active = true;
            conn.Update(selectedTestSet);
        }

        internal void InsertTestExecResult(TestExecResult result)
        {
            conn.Insert(result);
        }
    }
}
