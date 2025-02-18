﻿using SocketModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketClient
{
    public class CallBoxTcpClientServer
    {
        #region 全局变量
        public Object LockObj = new Object();

        /// <summary>
        /// 启动服务线程
        /// </summary>
        private Thread processor;//处理线程

        /// <summary>
        /// 客户端配置
        /// </summary>
        private ClientConfig config;


        /// <summary>
        /// 客户端socket
        /// </summary>
        private Socket _clientsocket;

        /// <summary>
        /// 服务状态
        /// </summary>
        private bool keepserver;

        //private HeartBeatThread heartthread;
        #endregion

        #region 事件

        #region 属性
        public ServerStateEnum State { get; set; }

        public bool IsConnected
        {
            get
            {
                return _clientsocket.Connected;
            }
        }
        #endregion

        /// <summary>
        /// 一个命令接收成功
        /// </summary>
        public event NetEventCallBox RecvSuccess;

        #endregion

        #region 方法
        /// <summary>
        /// 初始化socket
        /// </summary>
        /// <param name="_config"></param>
        /// <returns></returns>
        public bool Setup(ClientConfig _config)
        {
            if (_clientsocket != null)
            {
                _clientsocket.Close();
            }
            config = _config;
            //初始化
            _clientsocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);
            State = ServerStateEnum.NotStarted;
            return true;
        }

        /// <summary>
        /// 启动socket
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            try
            {
                keepserver = true;
                IPAddress ip = IPAddress.Parse(config.ServerIP);
                _clientsocket.Connect(new IPEndPoint(ip, config.Port));
                processor = new Thread(RecMessage);
                processor.IsBackground = true;
                processor.Start();
                //if (heartthread != null)
                //{
                //    heartthread.Close();
                //}
                //heartthread = new HeartBeatThread(this);
                State = ServerStateEnum.Running;
            }
            catch (Exception ex)
            {
                State = ServerStateEnum.NotStarted;
                return false;
            }
            return true;
        }


        /// <summary>
        /// 测试启动socket是否成功
        /// </summary>
        /// <returns></returns>
        public bool TestConnect(ClientConfig _config)
        {
            try
            {
                //初始化
                Socket test = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Parse(_config.ServerIP);
                test.Connect(new IPEndPoint(ip, _config.Port));
                test.Close();
            }
            catch (Exception ex)
            {
                State = ServerStateEnum.NotStarted;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 暂停服务
        /// </summary>
        public void Stop()
        {
            try
            {
                this.keepserver = false;
                //this.heartthread.Close();
                _clientsocket.Shutdown(SocketShutdown.Both);
                if (processor != null)
                {
                    this.processor.Abort();
                }
                State = ServerStateEnum.NotStarted;
            }
            catch (Exception ex)
            {
                State = ServerStateEnum.NotStarted;
            }
        }
        /// <summary>
        /// 监听消息
        /// </summary>
        public void RecMessage()
        {
            try
            {
                while (keepserver)
                {
                    //处理接受包
                    //解析包头

                    int offlinecount = 0;
                    int allPackLength = 12;
                    int receivedlengh = 0;
                    byte[] bufferPack = new byte[12];
                    while (allPackLength - receivedlengh > 0)
                    {
                        byte[] buffertemp = new byte[allPackLength - receivedlengh];
                        int length = _clientsocket.Receive(buffertemp);
                        if (length <= 0)
                        {
                            if (offlinecount == 3)
                            {
                                throw new Exception("Socket  错误！");
                            }
                            offlinecount += 1;
                            Thread.Sleep(1000 * 2);
                        }
                        Buffer.BlockCopy(buffertemp, 0, bufferPack, receivedlengh, length);
                        receivedlengh += length;
                    }
                    string pack = BitConverter.ToString(bufferPack);
                    PackageInfo packageInfo = new PackageInfo()
                    {
                        PackContent = pack
                    };
                    if (RecvSuccess != null)
                    {
                            RecvSuccess(this, packageInfo);
                    }
                  
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                Stop();
            }
        }

        private void Log(string msg)
        {
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "\\log.txt", msg + "\r\n");
        }



        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="command"></param>
        public void SendMessage(SuperSocketMsg pack)
        {
            try
            {
                lock (LockObj)
                {
                    _clientsocket.Send(pack.ToBuffer());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        ///发送字符串类型的消息
        /// </summary>.
        /// 
        /// <param name="str"></param>
        public void SendMessage(string str)
        {
            try
            {
                lock (LockObj)
                {
                    _clientsocket.Send(System.Text.Encoding.UTF8.GetBytes(str.Trim()));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        ///发送字符数组
        /// </summary>.
        /// 
        /// <param name="str"></param>
        public void SendMessage(byte[] bytePack)
        {
            try
            {
                lock (LockObj)
                {
                    _clientsocket.Send(bytePack);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public void Exit()
        {
            try
            {

                //this.heartthread.Close();
                _clientsocket.Shutdown(SocketShutdown.Both);
                _clientsocket.Close();
                this.keepserver = false;
                if (processor != null)
                {
                    this.processor.Abort();
                }
                State = ServerStateEnum.NotInitialized;
            }
            catch (Exception ex)
            {
                State = ServerStateEnum.NotInitialized;
            }
        }
        #endregion
    }

    public delegate void NetEventCallBox(object sender, PackageInfo packinfo);
}
