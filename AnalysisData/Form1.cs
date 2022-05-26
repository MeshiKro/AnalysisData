
using Sylvan.Data.Csv;
using System.Data;

using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using ClosedXML.Excel;
using Newtonsoft.Json.Linq;

namespace AnalysisData
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            DataTable dt = ImportData();

            filterData(dt);

        }

        private void filterData(DataTable dt)
        {

            DataTable finaldt = createDataTableOfHTML(dt);

             finaldt = addUsernameToDataTable(finaldt);

            string[] cols = new string[] { "username" };
            finaldt = RemoveDuplicateRows(finaldt, cols);

            finaldt = RemoveRows(1000, finaldt);


            finaldt = addNumberOfFollowing(finaldt);


            saveToExcel(finaldt);
           
        }

        private DataTable RemoveRows(int len, DataTable finaldt)
        {
            DataTable dt = finaldt.Clone();
            foreach (DataRow dr in finaldt.Rows)
            {
                if (dt.Rows.Count >= 1000)
                    break;

                dt.ImportRow(dr);
                dt.AcceptChanges();
                

            }
            return dt;
        }

        private DataTable addNumberOfFollowing(DataTable finaldt)
        {
            foreach (DataRow dr in finaldt.Rows)
            {

                string following = getNumberOfFollowing(dr["username"].ToString());
                if (following == "FALSE")
                    break;
                dr["following"] = following;

            }
            return finaldt;
        }

        private DataTable addUsernameToDataTable(DataTable finaldt)
        {

            foreach (DataRow dr in finaldt.Rows)
            {

                string username = dr["url"].ToString().Split('/')[4].ToString();
                dr["username"] = username;

            

            }
            return finaldt;
        }

        private DataTable createDataTableOfHTML(DataTable dt)
        {
            DataTable finaldt = dt.Clone();

            foreach (DataRow dr in dt.Rows)
            {
                if (dr["language"].ToString() == "HTML")
                    finaldt.ImportRow(dr);
                finaldt.AcceptChanges();
            }
            finaldt.Columns.Add("following", typeof(string));
            finaldt.Columns.Add("username", typeof(string));
            return finaldt;
        }

        public DataTable RemoveDuplicateRows(DataTable dTable, String[] colNames)
        {
            var hTable = new Dictionary<object[], DataRow>(new ObjectArrayComparer());

            foreach (DataRow drow in dTable.Rows)
            {
                Object[] objects = new Object[colNames.Length];
                for (int c = 0; c < colNames.Length; c++)
                    objects[c] = drow[colNames[c]];
                if (!hTable.ContainsKey(objects))
                    hTable.Add(objects, drow);
            }

            // create a clone with the same columns and import all distinct rows
            DataTable clone = dTable.Clone();
            foreach (var kv in hTable)
                clone.ImportRow(kv.Value);

            return clone;
        }
        private void saveToExcel(DataTable finaldt)
        {
            var workbook = new XLWorkbook();
            finaldt.TableName = "dt";
            var worksheet = workbook.Worksheets.Add(finaldt);
            workbook.SaveAs("finaldatatset.xlsx");
            workbook.Dispose();
        }

       
        private string getNumberOfFollowing(string? username)
        {
            string api = "https://api.github.com/users/" + username;

            try
            {
                string data = getData(api);

                JObject jObj = JObject.Parse(data);
                //                System.Diagnostics.Debug.WriteLine(jObj.ToString());
                if (jObj != null && !jObj.ToString().Contains("API rate limit exceeded"))
                    return jObj["following"].ToString();
                if (jObj.ToString().Contains("API rate limit exceeded"))
                {
                    System.Diagnostics.Debug.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>API rate limit exceeded");
                    return "FALSE";                }


            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message + " " + e.StackTrace);


            }
            return "0";
        }
        public String getData(string api)
        {
            var client = new RestClient(api);
            var request = new RestRequest();
            request.Method = Method.Get;
            Task<RestResponse> response = client.ExecuteAsync(request);
            String content = response.Result.Content.ToString();

            return content;
        }
        private DataTable createFinalDataSet()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("username", typeof(string));
            dt.Columns.Add("following", typeof(string));
            dt.Columns.Add("total_fork_count", typeof(string));


            return dt;

        }

        private DataTable ImportData()
        {
            using var dr = CsvDataReader.Create("github_software_forks.csv");
            DataTable dt = new DataTable();
            dt.Load(dr);
            return dt;
        }
    }
}