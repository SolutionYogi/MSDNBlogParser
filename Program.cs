using System;
using System.IO;
using System.Linq;
using System.Net;
using HtmlAgilityPack;

namespace MSDNBlogParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var post = GetPost("http://blogs.msdn.com/b/ericlippert/archive/2012/11/29/a-new-fabulous-adventure.aspx");
            Console.WriteLine(post.Date);
            Console.WriteLine(post.Title);
            Console.WriteLine(post.Contents);
            Console.ReadLine();
        }

        private static Post GetPost(string url)
        {
            var filePath = GetPageContents(url);
            var document = new HtmlDocument();
            document.Load(filePath);

            var fullPostNode = document.DocumentNode.SelectSingleNode("//div[@class='full-post']");

            var result = new Post();

            var dateNode = fullPostNode.SelectSingleNode("div[@class='post-date']");
            var dateAsText = dateNode.SelectSingleNode("span").InnerText;

            result.Date = DateTime.Parse(dateAsText);
            result.Title = fullPostNode.SelectSingleNode("h3[@class='post-name']").InnerText;
            result.Contents = fullPostNode.SelectSingleNode(".//div[@class='mine']").InnerText;

            return result;
        }

        private static string GetPageContents(string url)
        {
            var tempFileName = Path.GetTempFileName();
            var client = new WebClient();
            client.DownloadFile(url, tempFileName);
            return tempFileName;
        }
    }
}
