using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using NLog;

namespace MSDNBlogParser
{
    public class Post
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public DateTime Date { get; set; }

        public string Contents { get; set; }

        public List<string> Tags { get; private set; }

        public List<Comment> Comments { get; private set; }

        public string Title { get; set; }

        private HtmlDocument MainHtmlDocument { get; set; }

        private IEnumerable<string> CommentHtmlFilePathList { get; set; }

        private string MainHtmlFilePath { get; set; }

        private HtmlNode MainPostNode { get; set; }

        public Post()
        {
            Tags = new List<string>();
            Comments = new List<Comment>();
        }

        public static Post Create(string mainHtmlFilePath, IEnumerable<string> commentHtmlFilePathList)
        {
            if(string.IsNullOrWhiteSpace(mainHtmlFilePath))
                throw new ArgumentNullException("mainHtmlFilePath");

            if(commentHtmlFilePathList == null)
                throw new ArgumentNullException("commentHtmlFilePathList");

            if(! File.Exists(mainHtmlFilePath))
                throw new InvalidOperationException(string.Format("File specified by path {0} doesn't exist.", mainHtmlFilePath));

            var post = new Post
                       {
                           MainHtmlFilePath = mainHtmlFilePath,
                           CommentHtmlFilePathList = commentHtmlFilePathList
                       };

            post.ParseHtmlDocument();
            return post;
        }

        private void ParseHtmlDocument()
        {
            MainHtmlDocument = GetHtmlDocument(MainHtmlFilePath);

            MainPostNode = MainHtmlDocument.DocumentNode.SelectSingleNode("//div[@class='full-post']");

            ParseDate();

            ParseTitle();

            ParseContents();

            ParseTags();

            ParseComments();
        }

        private IEnumerable<HtmlDocument> HtmlDocumentList
        {
            get
            {
                yield return MainHtmlDocument;

                foreach(var htmlFilePath in CommentHtmlFilePathList)
                {
                    yield return GetHtmlDocument(htmlFilePath);
                }
            }
        }

        private void ParseComments()
        {
            Comments.AddRange(HtmlDocumentList.SelectMany(ParseComments));
        }

        private static HtmlDocument GetHtmlDocument(string htmlFilePath)
        {
            var document = new HtmlDocument();
            document.Load(htmlFilePath);
            return document;
        }

        private static IEnumerable<Comment> ParseComments(HtmlDocument document)
        {
            var mainCommentsNode = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'blog-feedback-list')]");
            var unorderedListNode = mainCommentsNode.SelectSingleNode(".//ul[@class='content-list']");
            var listItemNodes = unorderedListNode.SelectNodes("li");
            return listItemNodes.Select(listItemNode => new Comment
                                                        {
                                                            Author =
                                                                listItemNode.SelectSingleNode(".//div[@class='post-author']/*/span[@class='user-name']")
                                                                .InnerText,
                                                            Date = DateTime.Parse(listItemNode.SelectSingleNode(".//div[@class='post-date']/span").InnerText),
                                                            Contents = listItemNode.SelectSingleNode(".//div[contains(@class, 'post-content')]").InnerText
                                                        });
        }

        private void ParseTags()
        {
            var tagsNode = MainPostNode.SelectSingleNode("div[@class='post-tags']");

            var tagLinks = tagsNode.SelectNodes(".//a[@rel='tag']");

            foreach(var tagLink in tagLinks)
            {
                Tags.Add(tagLink.InnerText);
            }
        }

        private void ParseContents()
        {
            Contents = MainPostNode.SelectSingleNode(".//div[@class='mine']").InnerText;
        }

        private void ParseTitle()
        {
            Title = MainPostNode.SelectSingleNode("h3[@class='post-name']").InnerText;
        }

        private void ParseDate()
        {
            var dateNode = MainPostNode.SelectSingleNode("div[@class='post-date']");
            var dateAsText = dateNode.SelectSingleNode("span").InnerText;

            Date = DateTime.Parse(dateAsText);
        }
    }
}