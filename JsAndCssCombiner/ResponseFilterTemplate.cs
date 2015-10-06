using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace JsAndCssCombiner
{
    /// <summary>
    /// This is a base for custom Stream implementations that can be used to set the Response.Filter
    /// property in order to manipulate the html before rendering it to the response.
    /// </summary>
    public abstract class ResponseFilterTemplate : Stream
    {
        private readonly Stream _responseStream;
        private readonly StringBuilder _responseHtml;

        protected ResponseFilterTemplate(Stream responseStream)
        {
            _responseStream = responseStream;
            _responseHtml = new StringBuilder();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _responseStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            string html = Encoding.UTF8.GetString(buffer);

            _responseHtml.Append(html);

            if (IsEndOfFile(html))
            {
                string result = ProcessHtml(_responseHtml.ToString());

                buffer = Encoding.UTF8.GetBytes(result);

                _responseStream.Write(buffer, offset, buffer.Length);
            }
        }

        public override void Flush()
        {
            _responseStream.Flush();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position { get; set; }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _responseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _responseStream.SetLength(value);
        }

        private static bool IsEndOfFile(string currentHtml)
        {
            var endOfFileTag =
                new Regex("</html>", RegexOptions.IgnoreCase);

            if (!endOfFileTag.IsMatch(currentHtml))
                return false;
            return true;
        }

        public abstract string ProcessHtml(string htmlToProcess);
    }
}