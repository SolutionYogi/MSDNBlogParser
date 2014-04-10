using System;
using System.IO;
using System.Linq;
using System.Net;

namespace MSDNBlogParser
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var post = GetPost("http://blogs.msdn.com/b/ericlippert/archive/2012/11/29/a-new-fabulous-adventure.aspx");
            Console.WriteLine(post.Date);
            Console.WriteLine(post.Title);
            Console.WriteLine(post.Contents);
            Console.ReadLine();
        }

        private static Post GetPost(string url)
        {
            var filePath = GetHtmlPage(url);
            var post = Post.Create(filePath, Enumerable.Empty<string>());
            return post;
        }

        private static string GetHtmlPage(string url)
        {
            var tempFileName = Path.GetTempFileName();
            var client = new WebClient();
            client.DownloadFile(url, tempFileName);
            return tempFileName;
        }
    }
}