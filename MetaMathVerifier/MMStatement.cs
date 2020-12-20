using System.IO;

namespace MetaMathVerifier
{
    public abstract class MMStatement
    {
        public FileInfo File;
        public int Line;
        public int Column;
        public int ByteOffset;
        public string Name;
    }
}