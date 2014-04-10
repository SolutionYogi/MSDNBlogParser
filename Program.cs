using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using HtmlAgilityPack;

namespace MSDNBlogParser
{
    internal class Program
    {
        private const string MsdnBlogRootUrl = "http://blogs.msdn.com/";

        private const string Username = "ericlippert";

        private const string FolderPath = @"c:\Temp\Blog";

        private static void Main(string[] args)
        {
            var mainBlogUrl = string.Format("{0}/b/{1}", MsdnBlogRootUrl, Username);

            var mainPageDocument = GetHtmlDocument(mainBlogUrl);

            var archivePageLinks = mainPageDocument.DocumentNode.SelectNodes("//a[contains(@class,'view-post-archive-list')]");

            foreach(var archivePageLink in archivePageLinks)
            {
                Console.WriteLine(archivePageLink.Attributes["href"].Value);
            }

            foreach (var postLink in GetPostLinks("http://blogs.msdn.com/b/ericlippert/archive/2003/10.aspx"))
            {
                Console.WriteLine(postLink);
            }

            var post = GetPost("http://blogs.msdn.com/b/ericlippert/archive/2012/11/29/a-new-fabulous-adventure.aspx");

            Console.WriteLine(post.Title);
            Console.WriteLine(post.Date);
            Console.WriteLine(post.Comments.Count);

            Console.ReadLine();
        }

        private static IEnumerable<string> GetPostPages(string postUrl)
        {
            var mainPostFilePath = GetHtmlPage(postUrl);

            yield return mainPostFilePath;
            
            var document = GetHtmlDocument(postUrl);

            var otherPages = document.DocumentNode.SelectNodes("//div[@class='pager']//a");

            if(otherPages == null)
                yield break;

            foreach(var otherPageLink in otherPages.Skip(1))
            {
                var otherPageUrl = string.Format("{0}{1}", MsdnBlogRootUrl, otherPageLink.Attributes["href"].Value);
                yield return GetHtmlPage(otherPageUrl);
            }
        }

        private static IEnumerable<string> GetPostLinks(string archivePageLink)
        {
            var document = GetHtmlDocument(archivePageLink);

            var pageLinks = document.DocumentNode.SelectNodes("//div[@class='pager']//a");

            if(pageLinks == null)
                return GetPostLinks(document);
            
            var result = new List<string>();
            
            foreach(var pageUrl in pageLinks.Select(pageLink => string.Format("{0}{1}", MsdnBlogRootUrl, pageLink.Attributes["href"].Value)))
            {
                result.AddRange(GetPostLinks(GetHtmlDocument(pageUrl)));
            }
            
            return result;
        }

        private static IEnumerable<string> GetPostLinks(HtmlDocument document)
        {
            return
                document.DocumentNode.SelectNodes("//h4/a[contains(@class, 'view-post')]")
                    .Select(articleLink => string.Format("{0}{1}", MsdnBlogRootUrl, articleLink.Attributes["href"].Value));
        }

        protected static Post GetPost(string url)
        {
            var files = GetPostPages(url);
            var post = Post.Create(files);
            return post;
        }

        private static HtmlDocument GetHtmlDocument(string url)
        {
            var document = new HtmlDocument();
            document.Load(GetHtmlPage(url));
            return document;
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