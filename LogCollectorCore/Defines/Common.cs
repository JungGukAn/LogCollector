using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections;
using System.Net;


namespace LogCollectorCore
{
    public class ReadTaskResult
    {
        public byte[] Result;

        public HttpStatusCode StatusCode;
        public string Message;

        private ReadTaskResult() { }

        public static ReadTaskResult Fail(HttpStatusCode statusCode, string message)
        {
            return new ReadTaskResult { StatusCode = statusCode, Message = message };
        }

        public static ReadTaskResult Success(byte[] result)
        {
            return new ReadTaskResult { StatusCode = HttpStatusCode.OK, Result = result };
        }
    }

    public class Tag : IList<string>
    {
        string _tagString;
        List<string> _tagList;

        public Tag(string tagString)
        {
            if (string.IsNullOrWhiteSpace(tagString))
            {
                throw new ArgumentNullException();
            }

            _tagString = tagString;
        }

        public Tag(List<string> tagList)
        {
            if (tagList == null || tagList.Count == 0)
            {
                throw new ArgumentNullException();
            }

            _tagList = tagList;
        }

        public string this[int index] { get => tagList[index]; set { tagList[index] = value; _tagString = null; } }

        public int Count => tagList.Count;

        public bool IsReadOnly => false;

        public void Add(string item)
        {
            tagList.Add(item);
            _tagString = null;
        }

        public void Clear()
        {
            if (_tagList == null)
            {
                _tagList = new List<string>();
            }
            else
            {
                _tagList.Clear();
            }

            _tagString = null;
        }

        public bool Contains(string item)
        {
            return tagList.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            tagList.CopyTo(array, arrayIndex);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return tagList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return tagList.GetEnumerator();
        }

        public int IndexOf(string item)
        {
            return tagList.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            tagList.Insert(index, item);
            _tagString = null;
        }

        public bool Remove(string item)
        {
            bool isRemoved = tagList.Remove(item);

            if (isRemoved)
            {
                _tagString = null;
            }

            return isRemoved;
        }

        public void RemoveAt(int index)
        {
            tagList.RemoveAt(index);
            _tagList = null;
        }

        private string tagString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_tagString))
                {
                    _tagString = string.Join('.', _tagList);
                }

                return _tagString;
            }
        }

        private List<string> tagList
        {
            get
            {
                if (_tagList == null || _tagList.Count == 0)
                {
                    _tagList = _tagString.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                return _tagList;
            }
        }

        public static implicit operator string(Tag tag)
        {
            return tag.tagString;
        }

        public static implicit operator List<string>(Tag tag)
        {
            return tag.tagList;
        }

        public static implicit operator Tag(string tag)
        {
            return new Tag(tag);
        }

        public static implicit operator Tag(List<string> tag)
        {
            return new Tag(tag);
        }

        public override string ToString()
        {
            return tagString;
        }
    }
    
    public class TagLog
    {
        public Tag Tag { get; set; }
        public readonly dynamic Log;

        public TagLog(dynamic log)
        {
            Log = log;
        }
    }

    public class LogRequest
    {
        public TagLog TagLog { get; set; }
        public ImmutableList<Action<Tag, List<dynamic>>> Outputs { get; set; }
    }
}
