
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
using Sylvan.Data.Csv;
using System.Data;
using Newtonsoft.Json;
using ClosedXML.Excel;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace AnalysisData
{
    public partial class Form1 : Form
    {
        public Form1()
        {


        InitializeComponent();
            System.Diagnostics.Debug.WriteLine("START ImportData");

            DataTable dt = ImportData();

              filterData(dt);

            addCommitToDataset(dt);

        }

        private void addCommitToDataset(DataTable dt)
        {
            int i = 0;
            foreach (DataRow dr in dt.Rows)
            {
                System.Diagnostics.Debug.WriteLine(" i ="+ i);
                string commits = dr["commits_count"].ToString().Trim();
                if(commits=="")
                {
                    string user = dr["username"].ToString().Trim();
                    string [] parts = dr["url"].ToString().Split("//");
                    string[] parts2 = parts[1].Split("/");

                    string repo = parts2[parts2.Length - 1].Trim();
                    string coomitCount = getCommitNumbers(user, repo, dt); ;
                    dr["commits_count"] = coomitCount;
                    System.Diagnostics.Debug.WriteLine(" user =" + user + " repo =" + repo + "commits_count =" + coomitCount);

                    dt.AcceptChanges();

                }
                i++;
                dt.AcceptChanges();
                if (i%100 ==0)
                {
                    saveToExcel(dt);

                }
            }

            saveToExcel(dt);


        }

        private string getCommitNumbers(string owner, string repo,DataTable dt)
        {


            string result = "";
            //string base_url = "https://api.github.com";
            //string url = string.Format("{0}/repos/{1}/{2}/commits", base_url, owner, repo);

            //string json = getData(url);
            //json= json.Substring(1, json.Length - 2);
            //System.Diagnostics.Debug.WriteLine(" json =" + json );

            //JObject jsonObject = JObject.Parse(json);
            //string link = jsonObject["Link"].ToString();
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.WorkingDirectory = @"C:\Test";

                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/c python get_commit_count1.py " + owner + " " + repo + " main";
                // "python get_commit_count1.py spills fw1 master"; ;
                process.StartInfo = startInfo;
                process.Start();
                Thread.Sleep(5000);
                result = File.ReadAllText(@"C:\Test\number.txt");
                File.Delete(@"C:\Test\number.txt");
                Thread.Sleep(10000);

            }
            catch (Exception e)
            {
                saveToExcel(dt);

               // Thread.Sleep(30 * 60 * 1000);

            }
            return result;
        }

        private DataTable RemoveFirstLastName(DataTable finaldt)
        {
            DataTable dt = finaldt.Clone();
            foreach (DataRow dr in finaldt.Rows)
            {

                dt.ImportRow(dr);
                dt.AcceptChanges();

            }
            return dt;
        }

        private void filterData(DataTable dt)
        {
            System.Diagnostics.Debug.WriteLine("START createDataTableWithoutZeroForks");

            DataTable finaldt = createDataTableWithoutZeroForks(dt);

            finaldt = addUsernameToDataTable(finaldt);

            string[] cols = new string[] { "username" };
            System.Diagnostics.Debug.WriteLine("START RemoveDuplicateRows");

            finaldt = RemoveDuplicateRows(finaldt, cols);

            //    finaldt = RemoveRows(0, finaldt);
            System.Diagnostics.Debug.WriteLine("START addNumberOfFollowing");

            finaldt = addNumberOfFollowing(finaldt);
            System.Diagnostics.Debug.WriteLine("START saveToExcel");

            saveToExcel(finaldt);
            System.Diagnostics.Debug.WriteLine("DONE");

        }

        private DataTable RemoveRows(int len, DataTable finaldt)
        {
            DataTable dt = finaldt.Clone();
            foreach (DataRow dr in finaldt.Rows)
            {
                if (!dr["forks_count"].ToString().Equals("0"))
                {

                    dt.ImportRow(dr);
                    dt.AcceptChanges();
                }
                if (dt.Rows.Count > 5000)
                    break;

            }
            return dt;
        }

        private DataTable addNumberOfFollowing(DataTable finaldt)
        {
            int i = 0;
            foreach (DataRow dr in finaldt.Rows)
            {
                
                string following = "";
                string company = "";

                string commits_count = "";
                string followers = getNumberOfFollowing(dr["username"].ToString(), out commits_count, out following, out company);
                if (followers == "FALSE")
                    break;
                if (followers.Equals(""))
                    followers = "0";
                dr["followers"] = followers;
                //            dr["commits_count"] = commits_count;

                if (i > 400)
                    break;
                else
                    i++;
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

        private DataTable createDataTableWithoutZeroForks(DataTable dt)
        {
            DataTable finaldt = dt.Clone();
            try
            {
                finaldt.Columns.Add("username", typeof(string));
                finaldt.Columns.Add("followers", typeof(int));
                finaldt.Columns.Add("commits_count", typeof(int));
                finaldt.Columns.Add("total_fork_count", typeof(int));
                finaldt.AcceptChanges();
                foreach (DataRow dr in dt.Rows)
                {


                    finaldt.ImportRow(dr);
                    finaldt.AcceptChanges();
                }
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine("e " + e.Message + e.StackTrace);

            }

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
            workbook.SaveAs(DateTime.Now.ToString("yyyyMMddHHmm") + "_finaldatatset.xlsx");
            workbook.Dispose();
        }


        private string getNumberOfFollowing(string? username, out string commits_count, out string following, out string company)
        {
            string api = "https://api.github.com/users/" + username;
            commits_count = "";
            company = "";
            following = "";
            try
            {
                string data = getData(api);

                JObject jObj = JObject.Parse(data);
                if (jObj != null && !jObj.ToString().Contains("API rate limit exceeded") && !jObj.ToString().Contains("Not Found"))
                {
                    string followers = jObj["followers"].ToString();
                  //  System.Diagnostics.Debug.WriteLine("Objet " + jObj.ToString());

                    System.Diagnostics.Debug.WriteLine("followers " + followers);

                //   commits_count = jObj["commits_count"].ToString();
                    following = jObj["following"].ToString();
                    company = jObj["company"].ToString();
                    return followers;
                }
                if (jObj.ToString().Contains("API rate limit exceeded"))
                {
                    return "FALSE";
                }


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
            request.AddHeader("Authorization", "token " + "");
            request.Method = Method.Get;
            Task<RestResponse> response = client.ExecuteAsync(request);
            String content = response.Result.Content.ToString();

            return content;
        }

        private DataTable ImportData()
        {
            using var dr = CsvDataReader.Create("202207291418_finaldatatset.csv");
            DataTable dt = new DataTable();
            dt.Load(dr);
            return dt;
        }
    }
}