using System;
using System.Collections.Generic;

namespace MSDNBlogParser
{
    public class Post
    {
        public DateTime Date { get; set; }

        public string Contents { get; set; }

        public List<string> Tags { get; private set; }

        public List<Comment> Comments { get; private set; }

        public string Title { get; set; }

        public Post()
        {
            Tags = new List<string>();
            Comments = new List<Comment>();
        }
    }
}