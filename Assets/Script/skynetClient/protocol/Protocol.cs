using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skynet.DotNetClient {
	public class Protocol {

		private string c2s = @"
		.package {
			type 0 : integer
			session 1 : integer
		}

		handshake 1 {
			response {
				msg 0  : string
			}
		}

		get 2 {
			request {
				what 0 : string
			}
			response {
				result 0 : string
			}
		}

		set 3 {
			request {
				what 0 : string
				value 1 : string
			}
		}";

		private string s2c = @"
        .package {
	        type 0 : integer
	        session 1 : integer
        }

        heartbeat 1 {}
        ";

		private SpStream mSendStream = new SpStream ();
		private SpRpc mRpc;

		private SkynetClient sc;
		private Transporter transporter;

		public Protocol(SkynetClient sc, System.Net.Sockets.Socket socket)
		{
			this.sc = sc;
			this.transporter = new Transporter(socket, this.processMessage);
			this.transporter.onDisconnect = onDisconnect;
			this.transporter.start ();

			mRpc = SpRpc.Create (s2c, "package");
			mRpc.Attach (c2s);
		}
		
		//Invoke by Transporter, process the message
		internal void processMessage(byte[] bytes)
		{
			SpStream stream = new SpStream (bytes, 0, bytes.Length, bytes.Length);
			SpRpcResult result = mRpc.Dispatch (stream);
			sc.processMessage (result);
		}

		public void send (string proto, int mSession, SpObject args) {
			mSendStream.Reset ();

			Debug.Log ("Send Request : " + proto + ", session : " + mSession);

			mSendStream.Write ((short)0);
			mRpc.Request (proto, args, mSession, mSendStream);
			int len = mSendStream.Length - 2;
			mSendStream.Buffer[0] = (byte)((len >> 8) & 0xff);
			mSendStream.Buffer[1] = (byte)(len & 0xff);
			this.transporter.send (mSendStream.Buffer, mSendStream.Length);
		}

		//The socket disconnect
		private void onDisconnect()
		{
			this.sc.disconnect();
		}

		internal void close()
		{
			transporter.close();
		}
	}
}