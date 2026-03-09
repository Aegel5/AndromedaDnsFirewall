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

	public Message? msg;
	public byte[]? buf;


	public Message MsgGet {
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
	}

	public byte[] BuffGet {
		get {
			if (buf != null)
				return buf;
			if (msg != null) {
				buf = msg.ToByteArray();
				return buf;
			}
			throw new Exception("no data");
		}
	}
}
