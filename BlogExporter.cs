using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MSDNBlogParser
{
    public class BlogExporter
    {
        private const string MsdnBlogRootUrl = "http://blogs.msdn.com/";

        private const string FolderPath = @"c:\Temp\Blog";

        public string Username { get; private set; }

        private readonly BlockingCollection<PostHtmlFileInfo> _postInfoList = new BlockingCollection<PostHtmlFileInfo>();

        private CancellationToken _token;

        private BlogExporter()
        {
        }

        public static BlogExporter Create(string userName)
        {
            if(string.IsNullOrWhiteSpace(userName))
                throw new ArgumentNullException("userName");

            var downloader = new BlogExporter
                             {
                                 Username = userName
                             };
            return downloader;
        }

        public string BlogUrl
        {
            get { return string.Format("{0}/b/{1}/", MsdnBlogRootUrl, Username); }
        }

        private string BlogFolderPath
        {
            get
            {
                var path = Path.Combine(FolderPath, Username);
                if(! Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        public void ExportWordpressXml()
        {
            ExportWordpressXml(CancellationToken.None);
        }

        public void ExportWordpressXml(CancellationToken token)
        {
            _token = token;
            Task.Factory.StartNew(DownloadPosts, token);
            var processingTask = Task.Factory.StartNew(ProcessPosts, token);
            processingTask.Wait(token);
        }

        private void ProcessPosts()
        {
            foreach(var postInfo in _postInfoList.GetConsumingEnumerable())
            {
                Task.Factory.StartNew(() => ProcessPost(postInfo), _token);
            }
        }

        private void DownloadPosts()
        {
            var mainPageDocument = GetHtmlDocument(BlogUrl);

            var archivePageLinks = mainPageDocument.DocumentNode.SelectNodes("//a[contains(@class,'view-post-archive-list')]");

            Parallel.ForEach(archivePageLinks, ParallelOptions, ProcessArchivePage);

            _postInfoList.CompleteAdding();
        }

        private ParallelOptions ParallelOptions
        {
            get
            {
                return new ParallelOptions
                       {
                           CancellationToken = _token,
                           MaxDegreeOfParallelism = Environment.ProcessorCount / 2
                       };
            }
        }

        private void ProcessArchivePage(HtmlNode linkNode)
        {
            var archiveLink = string.Format("{0}{1}", MsdnBlogRootUrl, linkNode.Attributes["href"].Value);
            var postUrls = GetPostLinksFromArchivePage(archiveLink);
            Parallel.ForEach(postUrls, ParallelOptions, ProcessPostUrls);
        }

        private void ProcessPostUrls(string postUrl)
        {
            _postInfoList.Add(GetPostHtmlFileInfo(postUrl));
            Console.WriteLine("Added: {0}", postUrl);
        }

        protected void ProcessPost(PostHtmlFileInfo info)
        {
            Console.WriteLine("Processing: {0}", info.PostUrl);
            Post.Create(info.PostFilePaths);
            Console.WriteLine("Processed: {0}", info.PostUrl);
        }

        private PostHtmlFileInfo GetPostHtmlFileInfo(string postUrl)
        {
            var pages = GetPostPages(postUrl).ToList();
            return new PostHtmlFileInfo(postUrl, pages);
        }

        private IEnumerable<string> GetPostPages(string postUrl)
        {
            var mainPostFilePath = GetHtmlPage(postUrl);

            yield return mainPostFilePath;

            var document = GetHtmlDocument(postUrl);

            var otherPages = document.DocumentNode.SelectNodes("//div[@class='pager']//a");

            if(otherPages == null)
                yield break;

            foreach(
                var otherPageUrl in otherPages.Skip(1).Select(otherPageLink => string.Format("{0}{1}", MsdnBlogRootUrl, otherPageLink.Attributes["href"].Value))
                )
            {
                yield return GetHtmlPage(otherPageUrl);
            }
        }

        private IEnumerable<string> GetPostLinksFromArchivePage(string archivePageLink)
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

        private HtmlDocument GetHtmlDocument(string url)
        {
            var document = new HtmlDocument();
            document.Load(GetHtmlPage(url));
            return document;
        }

        private string GetHtmlPage(string url)
        {
            var fileName = url.Replace(BlogUrl, string.Empty);
            fileName = fileName.Replace("?", "-");
            fileName = fileName.Replace("/", "-");

            var finalFilePath = string.Format("{0}.html", Path.Combine(BlogFolderPath, fileName));
            var client = new WebClient();
            client.DownloadFile(url, finalFilePath);
            return finalFilePath;
        }
    }
}