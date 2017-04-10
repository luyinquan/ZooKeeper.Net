using System;
using System.Collections.Generic;
using ZKClientNET.Client;
using ZKClientNET.Exceptions;
using ZKClientNET.Util;

namespace ZKClientNET.Queue
{
    /// <summary>
    /// 分布式队列
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ZKDistributedQueue<T>
    {
        #region Element
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
        #endregion

        private ZKClient _zkClient;
        private string _root;

        private static string ELEMENT_NAME = "element";

        /// <summary>
        /// 创建分布式队列
        /// </summary>
        /// <param name="zkClient">ZKClientNET客户端</param>
        /// <param name="root">分布式队列根路径</param>
        public ZKDistributedQueue(ZKClient zkClient, string root)
        {
            _zkClient = zkClient;
            _root = root;
        }

        /// <summary>
        /// 添加一个元素
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public bool Offer(T element)
        {
            try
            {
                _zkClient.CreatePersistentSequential(_root + "/" + ELEMENT_NAME + "-", element);
            }
            catch (Exception e)
            {
                throw ExceptionUtil.ConvertToException(e);
            }
            return true;
        }

        /// <summary>
        /// 删除并返回顶部元素
        /// </summary>
        /// <returns></returns>
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
                    bool flag = _zkClient.Delete(element.name);
                    if (flag)
                    {
                        return element.data;
                    }
                    else
                    {
                        //如果删除失败，证明已被其他线程获取
                        //重新获取最新的元素，直到获取成功为止。
                    }
                }
                catch (ZKNoNodeException e)
                {
                }
                catch (Exception e)
                {
                    throw ExceptionUtil.ConvertToException(e);
                }
            }
        }

        /// <summary>
        /// 获取顶部元素
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            Element element = GetFirstElement();
            if (element == null)
            {
                return default(T);
            }
            return element.data;
        }

        /// <summary>
        /// 获得最小节点
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 判断队列是否为空
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return _zkClient.GetChildren(_root).Count == 0;
        }

        /// <summary>
        /// 获取队列顶部元素
        /// </summary>
        /// <returns></returns>
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
                    catch (ZKNoNodeException e)
                    {
                        // somebody else picked up the element first, so we have to
                        // retry with the new first element
                    }
                }
            }
            catch (Exception e)
            {
                throw ExceptionUtil.ConvertToException(e);
            }
        }
    
    }
}
