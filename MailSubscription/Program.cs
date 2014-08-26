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
        public int isSend;
    }

    class Program
    {
        static List<PostData> dataList = new List<PostData>();
        static int startSiteID;

        static void Main(string[] args)
        {
            DataTable dt = ReadCsv("top-1m.csv");
            CreateFile();
            SetStartSiteID();

            for (int i = startSiteID - 1; i < 1000000; i++)
            {
                Console.WriteLine(i + 1 + "  " + dt.Rows[i][1]);

                string url = "http://www." + dt.Rows[i][1];

                SetPostData(url);
                HttpPost();
                UpdateFile(i + 2);
            }





            //bool re1 = SetPostData("http://www.vitaminstore.nl");
            //bool re2 = SetPostData("http://www.notonthehighstreet.com");
            //bool re3 = SetPostData("http://www.hongkiat.com");
            //bool re4 = SetPostData("http://www.vitaminedz.com");
        }

        /// <summary>
        /// 创建保存 程序运行时第一个查找的网站ID 的txt文件 
        /// </summary>
        public static void CreateFile()
        {
            //文件不存在
            if (!File.Exists("StartSiteID.txt"))
            {

                FileStream fs1 = new FileStream("StartSiteID.txt", FileMode.Create, FileAccess.Write);//创建写入文件 
                StreamWriter sw = new StreamWriter(fs1);
                sw.WriteLine("1");//开始写入值

                sw.Close();
                fs1.Close();
            }
        }

        /// <summary>
        /// 读取文件设置StartSiteID的值
        /// </summary>
        public static void SetStartSiteID()
        {
            FileStream fs = new FileStream("StartSiteID.txt", FileMode.Open);
            StreamReader sr = new StreamReader(fs);

            sr.BaseStream.Seek(0, SeekOrigin.Begin);
            string strLine = sr.ReadLine();
            startSiteID = Convert.ToInt32(strLine);


            sr.Close();
            fs.Close();
        }

        /// <summary>
        /// 更新文件，以便于下次运行程序时接下去查找
        /// </summary>
        /// <param name="id">当前查找的网站id</param>
        public static void UpdateFile(int id)
        {
            FileStream fs = new FileStream("StartSiteID.txt", FileMode.Open, FileAccess.Write);
            StreamWriter sr = new StreamWriter(fs);
            sr.WriteLine(id);//开始写入值
            sr.Close();
            fs.Close();
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        public static void HttpPost()
        {
            List<PostData> noSendData = (from data in dataList
                                         where data.isSend == 0
                                         select data).ToList();
            if (noSendData.Count > 0)
            {
                foreach (var data in noSendData.ToArray())
                {
                    if (data.isSend == 0)
                    {
                        WebClient client = new WebClient();
                        client.Encoding = System.Text.Encoding.UTF8;

                        Console.WriteLine("Request Url:{0}", data.requestUrl);
                        Console.WriteLine("Input Name:{0}", data.inputName);

                        if (data.requestUrl.Contains("https:"))
                        {
                            client.OpenReadWithHttps(data.requestUrl, data.inputName + "=lanziliang11@163.com");
                            data.isSend = 1;
                            dataList.Find(c => c.requestUrl == data.requestUrl).isSend = 1;
                        }
                        else
                        {
                            client.OpenRead(data.requestUrl, data.inputName + "=lanziliang11@163.com");
                            data.isSend = 1;
                            dataList.Find(c => c.requestUrl == data.requestUrl).isSend = 1;
                        }
                        Console.WriteLine("StatusCode:{0}\n", client.StatusCode);

                    }
                }
            }


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
                Console.WriteLine("出错了：{0}\n", e.Message);
                return null;
            }
        }

        /// <summary>
        /// 获取POST数据
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
            HtmlNode.ElementsFlags.Remove("form");
            doc.LoadHtml(htmlStr.ToLower());
            HtmlNode rootNode = doc.DocumentNode;

            //1.先查找所有form节点
            //2.再查找包含input节点（name属性含有email）并且不包含password的input框 的form节点
            //3.获取action和input的name值

            HtmlNodeCollection formNodes = rootNode.SelectNodes("//form[@method='post']");

            if (formNodes == null)
            {
                Console.WriteLine("没有找到form\n");
                return false;
            }

            //遍历form节点
            foreach (var formNode in formNodes)
            {
                HtmlNodeCollection emailInput;
                //表单包含email输入框并且不包含密码输入框
                if (((emailInput = formNode.SelectNodes(".//input[(@type='email' or @type='text') and contains(@name,'email')]")) != null) && (formNode.SelectNodes(".//input[@type='password']") == null))
                {

                    Console.WriteLine("成功找到\n");
                    string _requestUrl;
                    string _inputName;

                    //获取action 并组装post请求地址
                    string action = formNode.Attributes["action"].Value;
                    if (string.IsNullOrEmpty(action))
                    {
                        _requestUrl = url;
                    }
                    else if (action.Contains("http:") || action.Contains("https:"))
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
                    dataList.Add(new PostData { requestUrl = _requestUrl, inputName = _inputName, isSend = 0 });

                    return true;
                }
                else
                {
                    continue;
                }
            }

            Console.WriteLine("没有找到email subscribe \n");
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
