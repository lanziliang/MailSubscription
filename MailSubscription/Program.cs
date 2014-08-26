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
        public string sendData;
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

                if (SetPostData(url))
                {
                    HttpPost();
                }
                UpdateFile(i + 2);
            }


            //WebClient client = new WebClient();
            //client.Encoding = System.Text.Encoding.UTF8;
            //client.OpenRead("http://www.notonthehighstreet.com/communication-preference", "utf8=✓&authenticity_token=jrnpYJUC00m8zPDcUx4RdMKEq9BofUmfiwa5T5Ey/AM=&newsletter_subscription[user_email]=lanziliang11@163.com&commit=subscribe");
            //client.OpenRead("http://www.vitaminstore.nl/", "__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE=/wEPDwUJOTkyNDIyMjA4ZGTnduirsFs23eouAY8DK32LKtjOwNAmt0imFpZROy3NcQ==&ctl00$cpContent$NewsletterSignup1$btnSignup=AANMELDEN&ctl00$cpContent$NewsletterSignup1$txtEmail=lanziliang11@163.com");
            //client.OpenRead("http://www.vitaminstore.nl/", "__eventtarget=&__eventargument=&__viewstate=/wepdwujotkyndiymja4zgtnduirsfs23eouay8dk32lktjownamt0imfpzroy3ncq==&ctl00$cpcontent$newslettersignup1$btnsignup=aanmelden&ctl00$cpcontent$newslettersignup1$txtemail=lanziliang11@163.com");
            //Console.WriteLine("StatusCode:{0}\n", client.StatusCode);

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
                        Console.WriteLine("Form Data:{0}", data.sendData);

                        if (data.requestUrl.Contains("https:"))
                        {
                            client.OpenReadWithHttps(data.requestUrl, data.sendData);
                            data.isSend = 1;
                            dataList.Find(c => c.requestUrl == data.requestUrl).isSend = 1;
                        }
                        else
                        {
                            client.OpenRead(data.requestUrl, data.sendData);
                            data.isSend = 1;
                            dataList.Find(c => c.requestUrl == data.requestUrl).isSend = 1;
                        }
                        Console.WriteLine("Status Code:{0}\n", client.StatusCode);

                    }
                }
            }


        }


        /// <summary>
        /// 获取html页面数据
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        //public static string GetHtmlStr(string url)
        //{
        //    try
        //    {
        //        WebRequest rGet = WebRequest.Create(url);
        //        rGet.Timeout = 10000;
        //        WebResponse rSet = rGet.GetResponse();
        //        Stream s = rSet.GetResponseStream();
        //        StreamReader reader = new StreamReader(s, Encoding.UTF8);
        //        return reader.ReadToEnd();
        //    }
        //    catch (WebException e)
        //    {
        //        Console.WriteLine("出错了：{0}\n", e.Message);
        //        return null;
        //    }
        //}


        /// <summary>
        /// 获取html页面数据
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string GetStringByUrl(string Url, System.Text.Encoding encoding)
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
            string htmlStr = GetStringByUrl(url, Encoding.UTF8);
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
            //3.获取action和input的name值，以及隐藏表单的name和value

            HtmlNodeCollection formNodes = rootNode.SelectNodes("//form[@method='post' or @method='POST' or @method='Post']");

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
                if (((emailInput = formNode.SelectNodes(".//input[(@type='email' or @type='text') and (contains(@name,'email') or contains(@name,'Email') or contains(@name,'EMAIL'))]")) != null) && ((formNode.SelectNodes(".//input[@type='password']") == null) || formNode.SelectNodes(".//input[@type='password']").First().Attributes["name"] == null))
                {
                    if (formNode.Attributes["action"] == null)
                    {
                        Console.WriteLine("没有找到form的action属性\n");
                        return false;
                    }
                    Console.WriteLine("成功找到\n");

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
                            if (hiddenInput.Attributes["value"] == null)
                            {
                                hiddenStr += hiddenInput.Attributes["name"].Value + "=&";
                            }
                            else if (hiddenInput.Attributes["name"] != null)
                            {
                                hiddenStr += hiddenInput.Attributes["name"].Value + "=" + hiddenInput.Attributes["value"].Value + "&";
                            }
                        }
                    }

                    HtmlNodeCollection submitInput = formNode.SelectNodes(".//input[@type='submit']");

                    if (submitInput != null && submitInput.First().Attributes["value"] != null && submitInput.First().Attributes["name"] != null)
                    {
                        submitStr = submitInput.First().Attributes["name"].Value + "=" + submitInput.First().Attributes["value"].Value + "&";
                    }

                    //获取input属性name的值
                    emailStr = emailInput.First().Attributes["name"].Value + "=mailsubscribe@163.com";

                    _sendData = hiddenStr + submitStr + emailStr;

                    //添加到dataList中
                    dataList.Add(new PostData { requestUrl = _requestUrl, sendData = _sendData, isSend = 0 });

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
