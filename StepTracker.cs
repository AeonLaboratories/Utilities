using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Utilities
{
	public class StepTracker
	{
		public string Name;
		public Step CurrentStep;
		public Stack<Step> Stack;
		public TimeSpan LastElapsed;

		public StepTracker() { }

		public StepTracker(string name)
		{
			Name = name;
			Stack = new Stack<Step>();
			LastElapsed = TimeSpan.Zero;
		}

		public void Start(string desc)
		{
			if (CurrentStep != null)
				Stack.Push(CurrentStep);
			CurrentStep = new Step(desc);
		}

		public void End()
		{
			LastElapsed = Elapsed;
			if (Stack.Count > 0)
				CurrentStep = Stack.Pop();
			else if (CurrentStep == null)
				Notice.Send(Name + " Push/Pop mismatch");
			else
				CurrentStep = null;
		}

		public void Clear()
		{
			LastElapsed = Elapsed;
			CurrentStep = null;
			Stack.Clear();
		}

		public string Description
		{
			get
			{
				if (CurrentStep == null)
					return "";
				else
					return CurrentStep.Description;
			}
		}

		public TimeSpan Elapsed
		{
			get
			{
				if (CurrentStep == null)
					return LastElapsed;
				else
					return DateTime.Now.Subtract(CurrentStep.StartTime);
			}
		}
	}

	public class Step
	{
		public string Description;
		public DateTime StartTime;

		public Step() { }

		public Step(string desc)
		{
			Description = desc;
			StartTime = DateTime.Now;
		}
	}
}
