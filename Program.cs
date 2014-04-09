using System;
using System.Collections.Generic;
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

            var tagsNode = fullPostNode.SelectSingleNode("div[@class='post-tags']");

            var tagLinks = tagsNode.SelectNodes(".//a[@rel='tag']");

            foreach(var tagLink in tagLinks)
            {
                result.Tags.Add(tagLink.InnerText);
            }

            //Comments on the main page.
            result.Comments.AddRange(ParseComments(document));

            var pagerNode = document.DocumentNode.SelectSingleNode("//div[@class='pager']");

            var links = pagerNode.SelectNodes(".//a");

            foreach(var link in links)
            {
                const string baseUrl = "http://blogs.msdn.com";
                var fullUrl = baseUrl + link.Attributes["href"].Value;
                Console.WriteLine("Parsing comment page {0}", fullUrl);
                var pageDocument = GetHtmlDocument(fullUrl);
                result.Comments.AddRange(ParseComments(pageDocument));
            }

            return result;
        }

        private static HtmlDocument GetHtmlDocument(string fullUrl)
        {
            var filePath = GetPageContents(fullUrl);
            var document = new HtmlDocument();
            document.Load(filePath);
            return document;
        }

        private static IEnumerable<Comment> ParseComments(HtmlDocument document)
        {
            var mainCommentsNode = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'blog-feedback-list')]");
            var unorderedListNode = mainCommentsNode.SelectSingleNode(".//ul[@class='content-list']");
            var listItemNodes = unorderedListNode.SelectNodes("li");
            return listItemNodes.Select(listItemNode => new Comment
                                                        {
                                                            Author = listItemNode.SelectSingleNode(".//div[@class='post-author']/*/span[@class='user-name']").InnerText,
                                                            Date = DateTime.Parse(listItemNode.SelectSingleNode(".//div[@class='post-date']/span").InnerText),
                                                            Contents = listItemNode.SelectSingleNode(".//div[contains(@class, 'post-content')]").InnerText
                                                        });
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
