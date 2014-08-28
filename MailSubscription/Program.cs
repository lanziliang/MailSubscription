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
using System.Threading;
using System.Xml;


namespace MailSubscription
{
    class Program
    {

        static int flag = 0;
        //线程数
        static int threadNum = 20;
        static int startNum = 15000;
        static int[] startSiteID = new int[threadNum];

        static DataTable dt;
        static string Email = "mailsubscribe@163.com";
        //static string Email = "lanziliang11@163.com";

        static void Main(string[] args)
        {
            //读取csv文件内容
            dt = ReadCsv("top-1m.csv");

            //设置开始查询网站ID
            SetStartSiteID();


            //多线程执行查询
            Thread product;
            ParameterizedThreadStart pts_product = new ParameterizedThreadStart(SetPostData);

            for (int i = 0; i < threadNum; i++)
            {
                product = new Thread(pts_product);
                product.Name = "Thread" + (i + 1).ToString();
                product.Start(startSiteID[i]);
            }



            //定时更新XML
            while (true)
            {
                if (flag > 20)
                {
                    UpdateXML();
                    flag = 0;
                }
                //Thread.Sleep(10000);
            }

        }

        /// <summary>
        /// 从xml文档中获取数据  初始化 StartSiteID[]
        /// </summary>
        public static void SetStartSiteID()
        {
            bool bl = File.Exists("StartSiteID.xml");
            if (bl == false && startNum >= 0)
            {
                //创建新的XML
                XmlDocument doc = new XmlDocument();
                XmlDeclaration Declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                XmlNode root = doc.CreateElement("root");
                XmlNode item = doc.CreateElement("item");
                XmlElement th;
                for (int i = 0; i < threadNum; i++)
                {
                    startSiteID[i] = i + startNum;
                    th = doc.CreateElement("Thread" + (i + 1).ToString());
                    th.InnerText = (i + startNum).ToString();
                    item.AppendChild(th);
                }

                root.AppendChild(item);
                doc.AppendChild(root);
                doc.InsertBefore(Declaration, doc.DocumentElement);
                doc.Save("StartSiteID.xml");
            }
            else
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("StartSiteID.xml");
                XmlElement x;
                for (int i = 0; i < threadNum; i++)
                {
                    x = (XmlElement)doc.GetElementsByTagName("Thread" + (i + 1).ToString())[0];
                    startSiteID[i] = int.Parse(x.InnerText);
                }
            }
        }


        /// <summary>
        /// 更新xml文件，以便于下次运行程序时接下去查找
        /// </summary>
        public static void UpdateXML()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement x;
            doc.Load("StartSiteID.xml");

            for (int i = 0; i < threadNum; i++)
            {
                x = (XmlElement)doc.GetElementsByTagName("Thread" + (i + 1).ToString())[0];
                x.InnerText = startSiteID[i].ToString();
            }

            doc.Save("StartSiteID.xml");
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        public static void HttpPost(int id, string requestUrl, string sendData)
        {

            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.UTF8;

            if (requestUrl.Contains("https:"))
            {
                client.OpenReadWithHttps(requestUrl, sendData);
            }
            else
            {
                client.OpenRead(requestUrl, sendData);
            }
            Console.WriteLine("Site ID:{0}", id);
            Console.WriteLine("Request Url:{0}", requestUrl);
            Console.WriteLine("Form Data:{0}", sendData);
            Console.WriteLine("Status Code:{0}\n", client.StatusCode);
        }

        /// <summary>
        /// 获取html页面数据
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string GetStringByUrl(int id, string Url, System.Text.Encoding encoding)
        {
            StreamReader sreader = null;
            string result = string.Empty;
            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(Url);
                httpWebRequest.Timeout = 10000;

                //关键参数，否则会取不到内容
                httpWebRequest.UserAgent = " User-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;) ";
                httpWebRequest.Accept = " */* ";
                httpWebRequest.KeepAlive = true;
                //httpWebRequest.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8");

                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    sreader = new StreamReader(httpWebResponse.GetResponseStream(), encoding);
                    char[] cCont = new char[256];
                    int count = sreader.Read(cCont, 0, 256);
                    while (count > 0)
                    {  //  Dumps the 256 characters on a string and displays the string to the console.   
                        String str = new String(cCont, 0, count);
                        result += str;
                        count = sreader.Read(cCont, 0, 256);
                    }
                }
                if (null != httpWebResponse)
                {
                    httpWebResponse.Close();
                }
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(id + "  " + Url);
                Console.WriteLine("出错了：{0}\n", e.Message);
                return null;
            }
        }

        /// <summary>
        /// 获取POST数据
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static void SetPostData(object obj)
        {
            int startID = (int)obj;
            int startIDIndex = startID % threadNum;

            for (int i = 0 + startID; i < dt.Rows.Count; i += threadNum, flag++)
            {
                startSiteID[startIDIndex] = i;



                string url = "http://www." + dt.Rows[i][1];


                string htmlStr = GetStringByUrl(i + 1, url, Encoding.UTF8);
                //Console.WriteLine(htmlStr);

                //访问网站超时或出错
                if (htmlStr == null)
                    continue;

                HtmlDocument doc = new HtmlDocument();
                HtmlNode.ElementsFlags.Remove("form");
                doc.LoadHtml(htmlStr);
                HtmlNode rootNode = doc.DocumentNode;

                //1.先查找所有form节点
                //2.再查找包含input节点（name属性含有email）并且不包含password的input框 的form节点
                //3.获取action和input的name值，以及隐藏表单的name和value

                HtmlNodeCollection formNodes = rootNode.SelectNodes("//form[@method='post' or @method='POST' or @method='Post']");

                if (formNodes == null)
                {
                    Console.WriteLine(i + 1 + "  " + url);
                    Console.WriteLine("没有找到form\n");
                    continue;
                }

                //遍历form节点
                foreach (var formNode in formNodes)
                {
                    HtmlNodeCollection emailInput;
                    //表单包含email输入框并且不包含密码输入框
                    if (((emailInput = formNode.SelectNodes(".//input[(@type='email' or @type='text') and (contains(@name,'email') or contains(@name,'Email') or contains(@name,'EMAIL'))]")) != null) && ((formNode.SelectNodes(".//input[@type='password']") == null) || formNode.SelectNodes(".//input[@type='password']").First().Attributes["name"] == null))
                    {
                        if (formNode.Attributes["action"] == null)
                        {
                            Console.WriteLine(i + 1 + "  " + url);
                            Console.WriteLine("没有找到form的action属性\n");
                            continue;
                        }

                        Console.WriteLine(i + 1 + "  " + url);
                        Console.WriteLine("成功找到\n");

                        //保存POST地址和发送的数据
                        string _requestUrl;
                        string _sendData;

                        //获取action 并组装post请求地址
                        string action = formNode.Attributes["action"].Value;
                        if (string.IsNullOrEmpty(action))
                        {
                            _requestUrl = url;
                        }
                        else if (action.ToLower().Contains("http:") || action.ToLower().Contains("https:"))
                        {
                            _requestUrl = action;
                        }
                        else
                        {
                            _requestUrl = url + action;
                        }


                        string hiddenStr = "";
                        string submitStr = "";
                        string emailStr = "";

                        //获取隐藏的表单数据
                        HtmlNodeCollection hiddenInputs = formNode.SelectNodes(".//input[@type='hidden']");
                        if (hiddenInputs != null)
                        {
                            foreach (var hiddenInput in hiddenInputs)
                            {
                                if (hiddenInput.Attributes["value"] == null && hiddenInput.Attributes["name"] != null)
                                {
                                    hiddenStr += hiddenInput.Attributes["name"].Value + "=&";
                                }
                                else if (hiddenInput.Attributes["name"] != null)
                                {
                                    hiddenStr += hiddenInput.Attributes["name"].Value + "=" + hiddenInput.Attributes["value"].Value + "&";
                                }
                            }
                        }


                        //获取提交按钮数据
                        HtmlNodeCollection submitInput = formNode.SelectNodes(".//input[@type='submit']");

                        if (submitInput != null && submitInput.First().Attributes["value"] != null && submitInput.First().Attributes["name"] != null)
                        {
                            submitStr = submitInput.First().Attributes["name"].Value + "=" + submitInput.First().Attributes["value"].Value + "&";
                        }

                        //获取email input属性name的值   
                        emailStr = emailInput.First().Attributes["name"].Value + "=" + Email;


                        //拼凑要发送的数据
                        _sendData = hiddenStr + submitStr + emailStr;

                        //成功找到后直接POST
                        HttpPost(i + 1, _requestUrl, _sendData);

                        continue;
                    }
                    else
                    {
                        continue;
                    }
                }

                Console.WriteLine(i + 1 + "  " + url);
                Console.WriteLine("没有找到email subscribe \n");
                continue;

            }
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
