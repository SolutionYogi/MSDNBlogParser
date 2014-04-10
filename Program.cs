using System;

namespace MSDNBlogParser
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var exporter = BlogExporter.Create("ericlippert");

            exporter.ExportWordpressXml();

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

    }
}