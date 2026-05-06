using OmniDown.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniDown.Services.Downloads;

public static class TorrentMetadataReader
{
    public static TorrentMetadata Read(byte[] bytes)
    {
        BencodeReader reader = new(bytes);
        object root = reader.ReadValue();
        if (root is not Dictionary<string, object> rootDictionary ||
            !rootDictionary.TryGetValue("info", out object? infoValue) ||
            infoValue is not Dictionary<string, object> info)
        {
            throw new InvalidOperationException("The torrent file does not contain a valid info dictionary.");
        }

        string name = ReadString(info, "name.utf-8");
        if (string.IsNullOrWhiteSpace(name))
        {
            name = ReadString(info, "name");
        }

        if (info.TryGetValue("files", out object? filesValue) &&
            filesValue is List<object> fileItems)
        {
            List<TorrentFileEntry> files = [];
            int index = 1;
            foreach (object fileValue in fileItems)
            {
                if (fileValue is not Dictionary<string, object> file)
                {
                    continue;
                }

                long length = ReadLong(file, "length");
                string path = ReadPath(file, "path.utf-8");
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = ReadPath(file, "path");
                }

                files.Add(new TorrentFileEntry
                {
                    Index = index++,
                    Path = path,
                    Length = length,
                    IsSelected = true
                });
            }

            return new TorrentMetadata(name, files);
        }

        long singleLength = ReadLong(info, "length");
        return new TorrentMetadata(
            name,
            [
                new TorrentFileEntry
                {
                    Index = 1,
                    Path = name,
                    Length = singleLength,
                    IsSelected = true
                }
            ]);
    }

    private static string ReadString(Dictionary<string, object> dictionary, string key)
    {
        return dictionary.TryGetValue(key, out object? value) && value is byte[] bytes
            ? DecodeText(bytes)
            : string.Empty;
    }

    private static long ReadLong(Dictionary<string, object> dictionary, string key)
    {
        return dictionary.TryGetValue(key, out object? value) && value is long number
            ? number
            : 0;
    }

    private static string ReadPath(Dictionary<string, object> dictionary, string key)
    {
        if (!dictionary.TryGetValue(key, out object? value) ||
            value is not List<object> parts)
        {
            return string.Empty;
        }

        return string.Join("\\", parts
            .OfType<byte[]>()
            .Select(DecodeText)
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string DecodeText(byte[] bytes)
    {
        try
        {
            return Encoding.UTF8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private sealed class BencodeReader(byte[] bytes)
    {
        private int _position;

        public object ReadValue()
        {
            if (_position >= bytes.Length)
            {
                throw new InvalidOperationException("Unexpected end of torrent data.");
            }

            byte token = bytes[_position];
            return token switch
            {
                (byte)'d' => ReadDictionary(),
                (byte)'l' => ReadList(),
                (byte)'i' => ReadInteger(),
                >= (byte)'0' and <= (byte)'9' => ReadBytes(),
                _ => throw new InvalidOperationException("Invalid bencoded torrent data.")
            };
        }

        private Dictionary<string, object> ReadDictionary()
        {
            _position++;
            Dictionary<string, object> dictionary = new(StringComparer.Ordinal);
            while (ReadToken() != (byte)'e')
            {
                string key = DecodeText(ReadBytes());
                dictionary[key] = ReadValue();
            }

            _position++;
            return dictionary;
        }

        private List<object> ReadList()
        {
            _position++;
            List<object> list = [];
            while (ReadToken() != (byte)'e')
            {
                list.Add(ReadValue());
            }

            _position++;
            return list;
        }

        private long ReadInteger()
        {
            _position++;
            int start = _position;
            while (ReadToken() != (byte)'e')
            {
                _position++;
            }

            string text = Encoding.ASCII.GetString(bytes, start, _position - start);
            _position++;
            return long.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        }

        private byte[] ReadBytes()
        {
            int length = 0;
            while (ReadToken() != (byte)':')
            {
                length = checked(length * 10 + bytes[_position] - (byte)'0');
                _position++;
            }

            _position++;
            byte[] value = bytes.AsSpan(_position, length).ToArray();
            _position += length;
            return value;
        }

        private byte ReadToken()
        {
            if (_position >= bytes.Length)
            {
                throw new InvalidOperationException("Unexpected end of torrent data.");
            }

            return bytes[_position];
        }
    }
}
