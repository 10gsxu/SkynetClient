using System;
using System.Timers;
using UnityEngine;

namespace Skynet.DotNetClient
{
	public class HeartBeatService
	{
		int interval;
		public int timeout;
		Timer timer;
		DateTime lastTime;

		SkynetClient sc;

		public HeartBeatService(int interval, SkynetClient sc)
		{
			this.interval = interval * 1000;
			this.sc = sc;

			this.sc.on("heartbeat", (SpObject obj) => {
				Debug.Log("heartbeat");
				this.resetTimeout();
			});
		}

		internal void resetTimeout()
		{
			this.timeout = 0;
			lastTime = DateTime.Now;
		}

		public void sendHeartBeat(object source, ElapsedEventArgs e)
		{
			TimeSpan span = DateTime.Now - lastTime;
			timeout = (int)span.TotalMilliseconds;

			//check timeout
			if (timeout > interval * 2)
			{
				Debug.Log ("timeout disconnect");
				sc.disconnect();
				//stop();
				return;
			}
		}

		public void start()
		{
			if (interval < 1000) return;

			//start hearbeat
			this.timer = new Timer();
			timer.Interval = interval;
			timer.Elapsed += new ElapsedEventHandler(sendHeartBeat);
			timer.Enabled = true;

			//Set timeout
			timeout = 0;
			lastTime = DateTime.Now;
		}

		public void stop()
		{
			if (this.timer != null)
			{
				this.timer.Enabled = false;
				this.timer.Dispose();
			}
		}
	}
}