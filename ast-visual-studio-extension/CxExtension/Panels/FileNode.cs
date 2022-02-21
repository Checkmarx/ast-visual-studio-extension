
namespace ast_visual_studio_extension.CxExtension.Panels
{
    /// <summary>
    /// This class represents a File with a name, line and column
    /// </summary>
    internal class FileNode
    {
        public string FileName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public static FileNode Builder()
        {
            return new FileNode();
        }

        public FileNode WithFileName(string fileName)
        {
            FileName = fileName;
            return this;
        }

        public FileNode WithLine(int line)
        {
            Line = line;
            return this;
        }

        public FileNode WithColumn(int column)
        {
            Column = column;
            return this;
        }

        public FileNode Build()
        {
            return this;
        }
    }
}
