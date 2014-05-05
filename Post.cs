using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace MSDNBlogParser
{
    public class Post
    {
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

        public static Post Create(IEnumerable<string> htmlFilePaths)
        {
            if(htmlFilePaths == null)
                throw new ArgumentNullException("htmlFilePaths");

            var list = htmlFilePaths.ToList();

            if(list.Count == 0)
                throw new InvalidOperationException("There must be at least one html page for the post.");

            var post = new Post
                       {
                           MainHtmlFilePath = list.First(),
                           CommentHtmlFilePathList = list.Skip(1)
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
            
            if(unorderedListNode == null)
                return Enumerable.Empty<Comment>();

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

            if(tagsNode == null)
                return;

            var tagLinks = tagsNode.SelectNodes(".//a[@rel='tag']");

            foreach(var tagLink in tagLinks)
            {
                Tags.Add(tagLink.InnerText);
            }
        }

        private void ParseContents()
        {
            Contents = MainPostNode.SelectSingleNode(".//div[contains(@class, 'post-content')]").InnerText;
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