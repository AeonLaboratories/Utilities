namespace Utilities
{
	public class Notice
	{
        public static NoticeSender DefaultSender = new NoticeSender();
        public static Notice Send(string text, Notice.Type type = Notice.Type.Request)
        {
            return Send("", text, type);
        }
        public static Notice Send(string caption, string text, Notice.Type type = Notice.Type.Request)
        {
            if (DefaultSender.OnNotice != null)
                return DefaultSender.Send(caption, text, type);
            else
                return null;
        }

        // If the type is Tell, the notice is processed in a newly
        // spawned background thread, and control returns to the caller
        // immediately without waiting for it to complete.
        public enum Type { Request, OkCancel, Warn, Tell}

		public string Caption;
		public string Text;
		public Type NoticeType;
		
		public Notice(string text, string caption = null, Type type = Type.Request)
		{
			Caption = caption;
			Text = text;
			NoticeType = type;
		}
	}

	public class NoticeSender
	{
		public delegate Notice Handler(Notice notice);
		public Handler OnNotice;

		public Notice Send(string text, Notice.Type type = Notice.Type.Request)
		{
			return Send("", text, type);
		}

		public Notice Send(string caption, string text, Notice.Type type = Notice.Type.Request)
		{
			return OnNotice?.Invoke(new Notice(text, caption, type));
		}
	}
}
