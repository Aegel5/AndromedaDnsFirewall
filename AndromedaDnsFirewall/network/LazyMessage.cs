using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Text;

namespace AndromedaDnsFirewall; 
public class LazyMessage {

	public LazyMessage(byte[] buf) {
		this.buf = buf;
	}
	public LazyMessage(Message msg) {
		this.msg = msg;
	}

	Message? msg;
	byte[]? buf;


	public Message Msg {
		get {
			if (msg != null)
				return msg;
			if (buf != null) {
				msg = new Message();
				msg.Read(buf);
				return msg;
			}
			throw new Exception("no data");
		}
		set {
			msg = value;
		}
	}

	public void ClearBuf() {
		buf = null;
	}

	public byte[] Buf {
		get {
			if (buf != null)
				return buf;
			if (msg != null) {
				buf = msg.ToByteArray();
				return buf;
			}
			throw new Exception("no data");
		}
		set {
			buf = value;
		}
	}
}
