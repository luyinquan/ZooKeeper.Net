using System;
using System.Collections.Generic;
using System.Linq;
using ZkClientNET.Exceptions;

namespace ZkClientNET
{
    public class DistributedQueue<T>
    {
        private class Element
        {
            public string name { set; get; }

            public T data { set; get; }

            public Element(string name, T data)
            {
                this.name = name;
                this.data = data;
            }
        }

        private ZkClient _zkClient;
        private string _root;

        private static string ELEMENT_NAME = "element";

        public DistributedQueue(ZkClient zkClient, string root)
        {
            _zkClient = zkClient;
            _root = root;
        }

        public bool Offer(T element)
        {
            try
            {
                _zkClient.CreatePersistentSequential(_root + "/" + ELEMENT_NAME + "-", element);
            }
            catch (Exception e)
            {
                throw ExceptionUtil.ConvertToRuntimeException(e);
            }
            return true;
        }

        public T Poll()
        {
            while (true)
            {
                Element element = GetFirstElement();
                if (element == null)
                {
                    return default(T);
                }
                try
                {
                    _zkClient.Delete(element.name);
                    return element.data;
                }
                catch (ZkNoNodeException e)
                {
                    // somebody else picked up the element first, so we have to
                    // retry with the new first element
                }
                catch (Exception e)
                {
                    throw ExceptionUtil.ConvertToRuntimeException(e);
                }
            }
        }

        private string GetSmallestElement(List<string> list)
        {
            string smallestElement = list[0];
            foreach (string element in list)
            {
                if (element.CompareTo(smallestElement) < 0)
                {
                    smallestElement = element;
                }
            }
            return smallestElement;
        }

        public bool IsEmpty()
        {
            return _zkClient.GetChildren(_root).Count == 0;
        }

        private Element GetFirstElement()
        {
            try
            {
                while (true)
                {
                    List<string> list = _zkClient.GetChildren(_root);
                    if (list.Count == 0)
                    {
                        return null;
                    }
                    string elementName = GetSmallestElement(list);

                    try
                    {
                        return new Element(_root + "/" + elementName, _zkClient.ReadData<T>(_root + "/" + elementName));
                    }
                    catch (ZkNoNodeException e)
                    {
                        // somebody else picked up the element first, so we have to
                        // retry with the new first element
                    }
                }
            }
            catch (Exception e)
            {
                throw ExceptionUtil.ConvertToRuntimeException(e);
            }
        }

        public T Peek()
        {
            Element element = GetFirstElement();
            if (element == null)
            {
                return default(T);
            }
            return element.data;
        }

    }
}
