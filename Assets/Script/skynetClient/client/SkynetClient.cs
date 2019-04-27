using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Skynet.DotNetClient
{
	/// <summary>
	/// network state enum
	/// </summary>
	public enum NetWorkState
	{
		[Description("initial state")]
		CLOSED,

		[Description("connecting server")]
		CONNECTING,

		[Description("server connected")]
		CONNECTED,

		[Description("disconnected with server")]
		DISCONNECTED,

		[Description("connect timeout")]
		TIMEOUT,

		[Description("netwrok error")]
		ERROR
	}

	public class SkynetClient : IDisposable {

		/// <summary>
		/// netwrok changed event
		/// </summary>
		public event Action<NetWorkState> NetWorkStateChangedEvent;


		private NetWorkState netWorkState = NetWorkState.CLOSED;   //current network state

		private EventManager eventManager;
		private Socket socket;
		private Protocol protocol;
		private bool disposed = false;

		private ManualResetEvent timeoutEvent = new ManualResetEvent(false);
		private int timeoutMSec = 8000;    //connect timeout count in millisecond

		private HeartBeatService heartBeatService;

		//不能使用0，使用0默认没有Response
		private int mSession = 1;

		public SkynetClient()
		{
		}

		/// <summary>
		/// initialize pomelo client
		/// </summary>
		/// <param name="host">server name or server ip (www.xxx.com/127.0.0.1/::1/localhost etc.)</param>
		/// <param name="port">server port</param>
		public void initClient(string host, int port)
		{
			timeoutEvent.Reset();
			eventManager = new EventManager();
			NetWorkChanged(NetWorkState.CONNECTING);
			
			IPAddress ipAddress = null;

			try
			{
				IPAddress[] addresses = Dns.GetHostEntry(host).AddressList;
				foreach (var item in addresses)
				{
					if (item.AddressFamily == AddressFamily.InterNetwork)
					{
						ipAddress = item;
						break;
					}
				}
			}
			catch (Exception e)
			{
				NetWorkChanged(NetWorkState.ERROR);
				return;
			}

			if (ipAddress == null)
			{
				throw new Exception("can not parse host : " + host);
			}

			this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint ie = new IPEndPoint(ipAddress, port);

			socket.BeginConnect(ie, new AsyncCallback((result) =>
				{
					try
					{
						this.socket.EndConnect(result);
						this.protocol = new Protocol(this, this.socket);
						NetWorkChanged(NetWorkState.CONNECTED);

						this.request ("handshake", (SpObject obj) => {
							Debug.Log(obj["msg"].AsString());
							//开始心跳，检测网络断开
							heartBeatService = new HeartBeatService(5, this);
							heartBeatService.start();
						});
					}
					catch (SocketException e)
					{
						if (netWorkState != NetWorkState.TIMEOUT)
						{
							NetWorkChanged(NetWorkState.ERROR);
						}
						Dispose();
					}
					finally
					{
						timeoutEvent.Set();
					}
				}), this.socket);

			if (timeoutEvent.WaitOne(timeoutMSec, false))
			{
				if (netWorkState != NetWorkState.CONNECTED && netWorkState != NetWorkState.ERROR)
				{
					NetWorkChanged(NetWorkState.TIMEOUT);
					Dispose();
				}
			}
		}

		public void request(string proto, Action<SpObject> action)
		{
			this.request(proto, null, action);
		}

		public void request(string proto, SpObject msg, Action<SpObject> action)
		{
			this.eventManager.AddCallBack(mSession, action);
			protocol.send(proto, mSession, msg);

			++mSession;
		}

		public void on(string eventName, Action<SpObject> action)
		{
			eventManager.AddOnEvent(eventName, action);
		}

		/// <summary>
		/// 网络状态变化
		/// </summary>
		/// <param name="state"></param>
		private void NetWorkChanged(NetWorkState state)
		{
			netWorkState = state;

			if (NetWorkStateChangedEvent != null)
			{
				NetWorkStateChangedEvent(state);
			}
		}

		internal void processMessage(SpRpcResult result)
		{
			switch (result.Op) {
			case SpRpcOp.Request:
				Debug.Log ("Recv Request : " + result.Protocol.Name + ", session : " + result.Session);
				/*
				if (result.Arg != null)
					Debug.Log (result.Arg.ToString());
				*/
				eventManager.InvokeOnEvent(result.Protocol.Name, result.Arg);
				break;
			case SpRpcOp.Response:
				Debug.Log ("Recv Response : " + result.Protocol.Name + ", session : " + result.Session);
				/*
				if (result.Arg != null)
					Debug.Log (result.Arg.ToString());
				*/
				eventManager.InvokeCallBack(result.Session, result.Arg);
				break;
			}
		}

		public void disconnect()
		{
			Dispose();
			NetWorkChanged(NetWorkState.DISCONNECTED);
		}

		public void Dispose() {
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		// The bulk of the clean-up code
		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
				return;

			if (disposing)
			{
				// free managed resources
				if (this.protocol != null)
				{
					this.protocol.close();
				}

				if (heartBeatService != null) {
					heartBeatService.stop ();
				}

				if (this.eventManager != null)
				{
					this.eventManager.Dispose();
				}

				try
				{
					this.socket.Shutdown(SocketShutdown.Both);
					this.socket.Close();
					this.socket = null;
				}
				catch (Exception)
				{
					//todo : 有待确定这里是否会出现异常，这里是参考之前官方github上pull request。emptyMsg
				}

				this.disposed = true;
			}
		}
	}
}