using AsyncIO;
using NetMQ;
using NetMQ.Sockets;

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
These dll files should be under `DEPTH/Assets/Plugins/NetMQDlls/netstandard2.0`
	NetMQ.dll (4.0.1.12)
	AsyncIO.dll (0.1.69)
	NaCl.dll (0.1.13)
from the nuget files.
*/
/*
Using
	https://github.com/off99555/Unity3D-Python-Communication/blob/master/UnityProject/Assets/NetMQExample/Scripts/HelloRequester.cs
as reference.
*/

using Mdict = System.Collections.Generic.Dictionary<string, string>;

public class ZmqDepthModel : DepthModel {
	public string ModelType {get; private set;}

	private string _addr;
	private RequestSocket _socket;

	private const float _timeout = 3;

	private Mdict _handshakeMdict;

	public ZmqDepthModel(string addr="tcp://localhost:5555") {
		Debug.Log($"ZmqDepthModel(): addr: {addr}");
		_addr = addr;

		//"this line is needed to prevent unity freeze after one use, not sure why yet" It's of `AsyncIO`.
		ForceDotNet.Force();
		_socket = new RequestSocket();
		Debug.Log($"Connecting to {_addr}");
		_socket.Connect(_addr);
		Handshake();
	}

	private bool Send(string tosend, out byte[] output) {
		byte[] bytes = Encoding.ASCII.GetBytes(tosend);
		return Send(bytes, out output);
	}

	private bool Send(byte[] tosend, out byte[] output) {
		_socket.SendFrame(tosend);

		byte[] message = null;
		bool gotMessage = false;
		float startTime = Time.realtimeSinceStartup; //Time.time does not update in a single frame
		while (Time.realtimeSinceStartup - startTime < _timeout) {
			gotMessage = _socket.TryReceiveFrameBytes(out message);
			if (gotMessage) break;
		}

		if (!gotMessage)
			Debug.LogWarning("Timeout.");

		output = message;
		return gotMessage;
	}

	private void Parse(byte[] bytes, out Mdict mdict, out byte[] data) {
		int idx;
		List<byte> lineList = new List<byte>();

		mdict = new Mdict();
		data = null;

		for (idx = 0; idx < bytes.Length; idx++) {
			if (bytes[idx] != '\n') {
				lineList.Add(bytes[idx]);
				
				//If it's not the end the bytestring, continue
				if (idx != bytes.Length - 1)
					continue;
			}

			//parse lineList
			byte[] lineArray = lineList.ToArray();
			lineList.Clear();
			string line = Encoding.ASCII.GetString(lineArray);
			line = line.Trim();

			//Skip empty lines
			if (line == "")
				continue;

			//Does the line start with '!'?
			if (line.StartsWith('!')) {
				if (line == "!HEADEREND") {
					//The rest it the data
					if (idx != bytes.Length - 1) {
						int dataSize = bytes.Length - 1 - idx; //currently at the newline
						data = new byte[dataSize];
						Array.Copy(bytes, idx + 1, data, 0, dataSize);
					}

					break;
				}
				else {
					Debug.LogWarning($"Unknown line: {line}");
					continue;
				}
			}

			//If it doesn't it's a `key=value` line
			if (line.IndexOf('=') < 0) {
				Debug.LogWarning($"Illegal key-value line: {line}");
				continue;
			}
			
			string[] tokens = line.Split('=', 2); //Max 2
			string key = tokens[0].Trim();
			string value = tokens[1].Trim();
			mdict.Add(key, value);
		}
	}

	private string ReconstructHeader(Mdict mdict) {
		string header = "";
		foreach (KeyValuePair<string, string> item in mdict)
			header += $"{item.Key}={item.Value}\n";

		return header;
	}

	private void GetPtypePname(Mdict mdict, out string ptype, out string pname) {
		ptype = mdict["ptype"];
		pname = mdict["pname"];
	}

	private void OnUnknownPtypePname(Mdict mdict) {
		string ptype, pname;
		GetPtypePname(mdict, out ptype, out pname);
		Debug.LogWarning($"Got unknown (pname, ptype): ({pname}, {ptype})");
	}

	private void OnResError(Mdict mdict, byte[] data) {
		string errorMsg = Encoding.ASCII.GetString(data);
		Debug.LogWarning($"The server responded with the error message: {errorMsg}");
	}

	private void Handshake() {
		Debug.Log("Handshaking...");

		bool success;
		byte[] output;

		success = Send(
			@$"
			ptype=REQ
			pname=HANDSHAKE_DEPTH
			
			pversion=1
			client_program=DepthViewer
			client_program_version={DepthFileUtils.Version}
			!HEADEREND",
			out output
		);

		byte[] data;

		if (success) {
			//Check errors... and if there's any problem set success to false
			try {
				Parse(output, out _handshakeMdict, out data);
				Debug.Log(ReconstructHeader(_handshakeMdict));

				string ptype, pname;
				GetPtypePname(_handshakeMdict, out ptype, out pname);

				//Wrong ptype/pname
				if (!(ptype == "RES" && pname == "HANDSHAKE_DEPTH")) {
					if (ptype == "RES" && pname == "ERROR")
						OnResError(_handshakeMdict, data);
					else
						OnUnknownPtypePname(_handshakeMdict);

					success = false;
				}
			}
			catch (Exception exc) {
				Debug.LogError($"Failed to parse: {exc}");
			}
		}
		else
			Debug.Log("The server did not respond.");

		if (success) {
			ModelType = _handshakeMdict["model_type"];
			Debug.Log($"Handshake success. ModelType: {ModelType}");
		}
		else {
			Debug.LogWarning("Handshake failure.");
			Dispose();
		}

	}

	public Depth Run(Texture inputTexture) {
		bool success;
		byte[] output;

		success = Send(
			@"
			ptype=REQ
			pname=DEPTH
			input_format=jpg
			!HEADEREND
			asdfasdfajiofds ofiajfn asdfahsi", 
			out output
		);

		if (success) Debug.Log("Received: " + output);
		else Debug.LogWarning("Failed.");

		return new Depth(new float[] {0, 0, 1, 1}, 2, 2);
	}

	public void Dispose() {
		_socket.Dispose();
		NetMQConfig.Cleanup(); //"this line is needed to prevent unity freeze after one use, not sure why yet"
	}
}

