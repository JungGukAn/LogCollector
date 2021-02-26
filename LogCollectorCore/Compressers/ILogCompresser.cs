using System;
namespace LogCollectorCore.Compresser
{
    public interface ILogCompresser
    {
        string Identifier { get; }

        byte[] Decompress(byte[] content);
    }
}
