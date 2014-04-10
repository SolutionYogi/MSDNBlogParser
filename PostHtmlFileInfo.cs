using System.Collections.Generic;

namespace MSDNBlogParser
{
    public class PostHtmlFileInfo
    {
        private readonly List<string> _postFilePaths = new List<string>();

        public string PostUrl { get; private set; }

        public IEnumerable<string> PostFilePaths
        {
            get { return _postFilePaths; }
        }

        public PostHtmlFileInfo(string postUrl, IEnumerable<string> filePaths)
        {
            PostUrl = postUrl;
            _postFilePaths.AddRange(filePaths);
        }
    }
}