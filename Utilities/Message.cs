using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
	public class Message
	{
		// Request and Tell are both Ok only, but Tell is modeless
		public enum Type { Request, OkCancel, Warn, Tell }

		public string Caption;
		public string Text;
		public Type MessageType;
		
		public Message(string text, string caption = null, Type type = Type.Request)
		{
			Caption = caption;
			Text = text;
			MessageType = type;
		}
	}

	public class MessageSender
	{
		public delegate Message Handler(Message message);
		public Handler OnMessage;

		public Message Send(string text, Message.Type type = Message.Type.Request)
		{
			return Send(null, text, type);
		}

		public Message Send(string caption, string text, Message.Type type = Message.Type.Request)
		{
			return OnMessage?.Invoke(new Message(text, caption, type));
		}
	}
}
