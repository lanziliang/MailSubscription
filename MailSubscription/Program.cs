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
        static List<PostData> dataList = new List<PostData>();

        static void Main(string[] args)
        {
            DataTable dt = ReadCsv("top-1m.csv");
            //Console.WriteLine(dt.Rows[101][1]);

            for (int i = 8070; i < 8100; i++)
            {
                Console.WriteLine(dt.Rows[i][1]);

                string url = "http://www." + dt.Rows[i][1];
                Console.WriteLine(url);

                SetPostData(url);
            }

            


            //bool re1 = SetPostData("http://www.vitaminstore.nl");
            //bool re2 = SetPostData("http://www.notonthehighstreet.com");
            //bool re3 = SetPostData("http://www.hongkiat.com");
            //bool re4 = SetPostData("http://www.vitaminedz.com");

            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.Default;


            foreach (var data in dataList)
            {
                Console.WriteLine("Request Url:{0}", data.requestUrl);
                Console.WriteLine("Input Name:{0}", data.inputName);

                //string s = client.OpenRead("http://www.notonthehighstreet.com/communication-preference", "newsletter_subscription[user_email]=lanziliang11@163.com");

            }

            Console.WriteLine("end");

            


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
            string htmlStr = GetHtmlStr(url).ToLower();
            //Console.WriteLine(htmlStr);

            //访问网站超时或出错
            if (htmlStr == null)
                return false;

            HtmlDocument doc = new HtmlDocument();
            HtmlNode.ElementsFlags.Remove("form");
            doc.LoadHtml(htmlStr);
            HtmlNode rootNode = doc.DocumentNode;

            //1.先查找所有form节点
            //2.再查找包含input节点（name属性含有email）并且不包含password的input框 的form节点
            //3.获取action和input的name值

            HtmlNodeCollection formNodes = rootNode.SelectNodes("//form[@method='post']");

            if (formNodes == null)
            {
                Console.WriteLine("没有找到form");
                return false;
            }

            //遍历form节点
            foreach (var formNode in formNodes)
            {
                HtmlNodeCollection emailInput;
                //表单包含email输入框并且不包含密码输入框
                if (((emailInput = formNode.SelectNodes(".//input[(@type='email' or @type='text') and contains(@name,'email')]")) != null) && (formNode.SelectNodes(".//input[@type='password']") == null))
                {

                    Console.WriteLine("111");
                    string _requestUrl;
                    string _inputName;

                    //获取action 并组装post请求地址
                    string action = formNode.Attributes["action"].Value;
                    if (string.IsNullOrEmpty(action))
                    {
                        _requestUrl = url;
                    }
                    else if (action.Contains("http:"))
                    {
                        _requestUrl = action;
                    }
                    else
                    {
                        _requestUrl = url + action;
                    }

                    //获取input属性name的值
                    _inputName = emailInput.First().Attributes["name"].Value;

                    //添加到dataList中
                    dataList.Add(new PostData { requestUrl = _requestUrl, inputName = _inputName });

                    return true;
                }
                else
                {
                    continue;
                }
            }

            Console.WriteLine("没有找到email subscribe");
            return false;
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
