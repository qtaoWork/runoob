using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using HtmlAgilityPack;
using NUnit.Framework;

namespace DownRunoob
{
    [TestFixture]
    public class 下载文档
    {
        private WebClient _webClient;

        [Test]
        public void 下载菜单()
        {
            var url = "https://www.runoob.com/";
            var list = new[]
            {
                new {index = "linux-tutorial.html", tag = "linux"},
                new {index = "md-tutorial.html", tag = "markdown"},
                new {index = "docker-tutorial.html", tag = "docker"},
                new {index = "mysql-tutorial.html", tag = "mysql"},
                new {index = "sql-tutorial.html", tag = "sql"},
                new {index = "python3-tutorial.html", tag = "python3"},
                new {index = "python-tutorial.html", tag = "python"},
                new {index = "vue-tutorial.html", tag = "vue2"},
                new {index = "nodejs-tutorial.html", tag = "nodejs"},
                new {index = "regexp-tutorial.html", tag = "regexp"},
                new {index = "xpath-tutorial.html", tag = "xpath"},
                new {index = "svn-tutorial.html", tag = "svn"},
            };

            var nav = string.Join("", list.Select(x => $"<li><a href='/{x.tag}/{x.index}'>{x.tag}</a></li>"));
            _webClient = new WebClient() { Encoding = Encoding.UTF8 };
            var index = 1;
            foreach (var model in list)
            {
                if (index++ <= 10)
                {
                    continue;
                }
                var html = _webClient.DownloadString(url + model.tag + "/" + model.index);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var aList = doc.DocumentNode.SelectNodes("//div[@id='leftcolumn']/a").Select(x => x.Attributes["href"].Value.Split('/').Last());
                // Console.WriteLine(string.Join(",",aList));
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"../../../../runoobDoc", model.tag);
                Directory.CreateDirectory(dir);
                var imgDir = Path.Combine(dir, "../wp-content/images");
                Directory.CreateDirectory(imgDir);

                foreach (var s in aList)
                {
                    DownFile(url + model.tag + "/" + s, dir, nav);
                }
            }
        }
        
        private void DownFile(string fileUrl, string dir, string nav)
        {
            var html = _webClient.DownloadString(fileUrl);
            if (string.IsNullOrEmpty(html))
            {
                SpinWait.SpinUntil(() => false, TimeSpan.FromMilliseconds(200));
                DownFile(fileUrl, dir, nav);
                return;
            }
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var node = doc.DocumentNode.SelectSingleNode("//ul[@class='pc-nav']");
            if (node != null)
            {
                node.InnerHtml = nav;
            }
            else
            {
                Console.WriteLine("无导航：" + fileUrl);
            }
            // Console.WriteLine(node.InnerHtml);
            var singleNode = new[]
            {
                "//div[@id='footer']",
                "//div[@id='sidebar-right-re']",
                "//form",
                "//div[@class='col search row-search-mobile']",
                "//div[@class='cd-user-modal']",
                "//div[@class='fixed-btn']",
                "//div[@class='shang_box']",
                "//div[@class='mobile-nav']",
                "//div[@id='respond']",
                "//div[contains(@class,'col') and contains(@class,'logo')]/..",
                "//div[contains(@class,'recommend-here')]/..",
                "//div[contains(@class,'feedback-btn')]",
            };
            foreach (var nodeKey in singleNode)
            {
                var temps = doc.DocumentNode.SelectNodes(nodeKey);
                if (temps == null)
                {
                    continue;
                }

                foreach (var temp in temps)
                {
                    temp?.Remove();
                }
            }

            var nodes = doc.DocumentNode.SelectNodes("//script");
            if (nodes != null)
            {
                foreach (HtmlNode htmlNode in nodes)
                {
                    htmlNode.Remove();
                }
            }

            nodes = doc.DocumentNode.SelectNodes("//link");
            if (nodes != null)
            {
                foreach (HtmlNode htmlNode in nodes)
                {
                    if (htmlNode.Attributes["href"].Value.StartsWith("http"))
                    {
                        htmlNode.Remove();
                    }
                }
            }

            nodes = doc.DocumentNode.SelectNodes("//a");
            if (nodes != null)
            {
                foreach (HtmlNode htmlNode in nodes)
                {
                    if (htmlNode.Attributes["href"] != null && htmlNode.Attributes["href"].Value.StartsWith("http"))
                    {
                        htmlNode.Attributes["href"].Value = "###";
                    }
                }
            }

            nodes = doc.DocumentNode.SelectNodes("//img");
            if (nodes != null)
            {

                foreach (HtmlNode htmlNode in nodes)
                {
                    var value = htmlNode.Attributes["src"].Value;
                    if (!value.Contains("/wp-content/uploads/"))
                    {
                        continue;
                    }

                    if (value.StartsWith("//"))
                    {
                        value = "http:" + value;
                    }
                    else if (value.StartsWith("/wp-content"))
                    {
                        value = "https://www.runoob.com/" + value;
                    }

                    try
                    {
                        var fileName = value.Split('/').Last();
                        var imgPath = Path.Combine(dir, "../wp-content/images", fileName);
                        _webClient.DownloadFile(value, imgPath);
                        htmlNode.Attributes["src"].Value = "/wp-content/images/" + fileName;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("下载文件失败:" + value);
                    }
                }

            }
            doc.Save(Path.Combine(dir, fileUrl.Split('/').Last()));
        }
    }
}