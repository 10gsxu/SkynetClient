using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Skynet.DotNetClient;

public class TestSkynet : MonoBehaviour {

	private SkynetClient skynetClient = null;

	// Use this for initialization
	void Start () {
		skynetClient = new SkynetClient ();
		skynetClient.initClient ("127.0.0.1", 8888, () => {
			print("TestSkynet");
			sendMsg();
		});
		skynetClient.on("heartbeat", (SpObject obj) => {
			print("heartbeat");
		});
	}

	void sendMsg() {
		skynetClient.request ("handshake", (SpObject obj) => {
			print(obj["msg"].AsString());
		});
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
