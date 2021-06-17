﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Utilities
{
	public class StepTracker : INotifyPropertyChanged
	{
		public static StepTracker Default = new StepTracker();

		public event PropertyChangedEventHandler PropertyChanged;
		public string Name { get; set; }

		public Step CurrentStep
		{
			get => currentStep;
			set { currentStep = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentStep))); }
		}
		Step currentStep;

		public Stack<Step> Stack { get; set; }
		public TimeSpan LastElapsed { get; set; }

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
        public override string ToString()
        {
            return Description;
        }
    }

	public class Step : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public string Description
		{
			get => description;
			set { description = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description))); }
		}
		string description;

		public DateTime StartTime
		{
			get => startTime;
			set { startTime = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartTime))); }
		}
		DateTime startTime;

		public Step() { }

		public Step(string desc)
		{
			Description = desc;
			StartTime = DateTime.Now;
		}
	}
}
