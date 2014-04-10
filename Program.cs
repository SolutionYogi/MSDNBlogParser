using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MSDNBlogParser
{
    public class PostHtmlFileInfo
    {
        private readonly List<string> _postFilePaths = new List<string>();

        public PostHtmlFileInfo(IEnumerable<string> filePaths)
        {
            _postFilePaths.AddRange(filePaths);
        }

        public List<string> PostFilePaths
        {
            get { return _postFilePaths; }
        }
    }

    internal class Program
    {
        private const string MsdnBlogRootUrl = "http://blogs.msdn.com/";

        private const string Username = "ericlippert";

        private const string FolderPath = @"c:\Temp\Blog";

        static readonly BlockingCollection<PostHtmlFileInfo> PostInfoList = new BlockingCollection<PostHtmlFileInfo>();

        private static string _mainBlogUrl;

        private static void Main(string[] args)
        {
            Task.Factory.StartNew(DownloadPosts);

            var processingTask = Task.Factory.StartNew(ProcessPosts);

            processingTask.Wait();
            
            Console.ReadLine();
        }

        private static void ProcessPosts()
        {
            foreach(var postInfo in PostInfoList.GetConsumingEnumerable())
            {
                try
                {
                    Post.Create(postInfo.PostFilePaths);
                }
                catch(Exception)
                {
                    Console.WriteLine("Could not parse " + postInfo.PostFilePaths.First());
                }
            }
        }

        public static void DownloadPosts()
        {
            _mainBlogUrl = string.Format("{0}/b/{1}/", MsdnBlogRootUrl, Username);

            var mainPageDocument = GetHtmlDocument(_mainBlogUrl);

            var archivePageLinks = mainPageDocument.DocumentNode.SelectNodes("//a[contains(@class,'view-post-archive-list')]");

            Parallel.ForEach(archivePageLinks, ProcessArchivePage);

            PostInfoList.CompleteAdding();
        }

        private static void ProcessArchivePage(HtmlNode linkNode)
        {
            var archiveLink = string.Format("{0}{1}", MsdnBlogRootUrl, linkNode.Attributes["href"].Value);
            Console.WriteLine("Processing archive page: " + archiveLink);
            var postUrls = GetPostLinksFromArchivePage(archiveLink);
            Parallel.ForEach(postUrls, ProcessPostUrls);
        }

        private static void ProcessPostUrls(string postUrl)
        {
            PostInfoList.Add(GetPostHtmlFileInfo(postUrl));
        }

        private static PostHtmlFileInfo GetPostHtmlFileInfo(string postUrl)
        {
            return new PostHtmlFileInfo(GetPostPages(postUrl));
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

        private static IEnumerable<string> GetPostLinksFromArchivePage(string archivePageLink)
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
            var mainFolderPath = Path.Combine(FolderPath, Username);
            if(! Directory.Exists(mainFolderPath))
                Directory.CreateDirectory(mainFolderPath);

            var fileName = url.Replace(_mainBlogUrl, string.Empty);
            fileName = fileName.Replace("?", "-");
            fileName = fileName.Replace("/", "-");

            var finalFilePath = string.Format("{0}.html", Path.Combine(FolderPath, Username, fileName));
            var client = new WebClient();
            client.DownloadFile(url, finalFilePath);
            return finalFilePath;
        }
    }
}