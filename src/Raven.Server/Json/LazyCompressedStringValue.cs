using System;
using Voron.Util;

namespace Raven.Server.Json
{
    public unsafe class LazyCompressedStringValue 
    {
        private readonly RavenOperationContext _context;
        public readonly byte* Buffer;
        public readonly int _uncompressedSize;
        private readonly int _compressedSize;
        public string String;

        public LazyCompressedStringValue(string str, byte* buffer, int uncompressedSize, int compressedSize, RavenOperationContext context)
        {
            String = str;
            _uncompressedSize = uncompressedSize;
            _compressedSize = compressedSize;
            _context = context;
            Buffer = buffer;
        }

        public static implicit operator string(LazyCompressedStringValue self)
        {
            if (self.String != null)
                return self.String;

            int bufferSize;
            var tempBuffer = self._context.GetNativeTempBuffer(self._uncompressedSize, out bufferSize);
            var uncompressedSize = LZ4.Decode64(self.Buffer, 
                self._compressedSize, 
                tempBuffer, 
                self._uncompressedSize,
                true);

            if (uncompressedSize != self._uncompressedSize)
                throw new FormatException("Wrong size detected on decompression");

            var charCount = self._context.Encoding.GetCharCount(tempBuffer, self._uncompressedSize);
            var str = new string(' ', charCount);
            fixed (char* pStr = str)
            {
                self._context.Encoding.GetChars(tempBuffer, self._uncompressedSize, pStr, charCount);
                self.String = str;
                return str;
            }
        }

        public override string ToString()
        {
            return (string) this;
        }
    }
}