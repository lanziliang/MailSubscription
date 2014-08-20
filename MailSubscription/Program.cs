using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using HtmlAgilityPack;
using System.IO;
using System.Xml.XPath;
using System.Data;


namespace MailSubscription
{
    class PostData
    {
        public string requestUrl;
        public string inputName;
    }

    class Program
    {

        static List<HtmlNode> formNodes = new List<HtmlNode>();
        static List<PostData> dataList = new List<PostData>();

        static void Main(string[] args)
        {
            //DataTable dt = ReadCsv("top-1m.csv");
            //Console.WriteLine(dt.Rows[101][1]);

            //string action = GetAction("http://" + dt.Rows[101][1]);
            //string url = "http://www." + dt.Rows[101][1] + action;
            //Console.WriteLine(url);


            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.Default;
            string s = client.OpenRead("http://www.notonthehighstreet.com/communication-preference", "newsletter_subscription[user_email]=lanziliang11@163.com");

            Console.WriteLine(s);


            //for (int i = 100; i < 130; i++)
            //{
            //    Console.WriteLine(dt.Rows[i][1]);

            //    string action = GetAction("http://" + dt.Rows[i][1]);
            //    if (action == null)
            //    {
            //        continue;
            //    }
            //    else
            //    {
            //        string url = "http://" + dt.Rows[i][1] + action;
            //        actionUrl.Add(url);
            //    }
            //}


            //foreach(var act in actionUrl)
            //{
            //    Console.WriteLine(act);
            //}
        }

        /// <summary>
        /// 获取html页面数据
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetHtmlStr(string url)
        {
            try
            {
                WebRequest rGet = WebRequest.Create(url);
                rGet.Timeout = 10000;
                WebResponse rSet = rGet.GetResponse();
                Stream s = rSet.GetResponseStream();
                StreamReader reader = new StreamReader(s, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (WebException e)
            {
                Console.WriteLine("出错了：{0}", e.Message);
                return null;
            }
        }

        /// <summary>
        /// 获取表单的action值
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool SetPostData(string url)
        {
            string htmlStr = GetHtmlStr(url);
            //Console.WriteLine(htmlStr);

            //访问网站超时或出错
            if (htmlStr == null)
                return false;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlStr);

            HtmlNode rootNode = doc.DocumentNode;
            string xpathStr = "//input[contains(@name,'Email') and @type='text']";
            HtmlNodeCollection inputNodes = rootNode.SelectNodes(xpathStr);

            //没有找到email输入框
            if (inputNodes == null)
            {
                return false;
            }

            foreach (var item in inputNodes)
            {
                foreach (var anc in item.Ancestors())
                {
                    HtmlNode formNode = anc.SelectSingleNode("child::form");
                    if (formNode != null)
                    {
                        formNodes.Add(formNode);
                    }
                }
            }


            return formNodes.First().GetAttributeValue("action", "");
        }

        /// <summary>
        /// 读取csv文件到DataTable
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static DataTable ReadCsv(string filePath)
        {
            DataTable dt = new DataTable("NewTable");
            DataRow row;

            dt.Columns.Add("num");
            dt.Columns.Add("site");
            dt.Columns.Add("flag");

            string strline;
            string[] aryline;
            using (StreamReader mysr = new StreamReader(filePath))
            {
                while ((strline = mysr.ReadLine()) != null)
                {
                    aryline = strline.Split(',');

                    row = dt.NewRow();
                    row.ItemArray = aryline;
                    dt.Rows.Add(row);
                }
            }

            return dt;
        }
    }
}
