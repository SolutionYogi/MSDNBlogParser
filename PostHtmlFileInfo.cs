using System.Collections.Generic;

namespace MSDNBlogParser
{
    public class PostHtmlFileInfo
    {
        public string PostUrl { get; private set; }

        public List<string> PostFilePaths { get; set; }

        public PostHtmlFileInfo()
        {
            PostFilePaths = new List<string>();
        }

        public PostHtmlFileInfo(string postUrl, IEnumerable<string> filePaths)
        {
            PostUrl = postUrl;
            PostFilePaths.AddRange(filePaths);
        }
    }
}