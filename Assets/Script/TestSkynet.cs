using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Skynet.DotNetClient;

public class TestSkynet : MonoBehaviour {

	private SkynetClient skynetClient = null;

	// Use this for initialization
	void Start () {
		skynetClient = new SkynetClient ();
		skynetClient.initClient ("127.0.0.1", 8888);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
