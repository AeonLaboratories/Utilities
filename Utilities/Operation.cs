﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Utilities
{
	public class Operation
	{
		[XmlAttribute]
		public string Name { get; set; }

		public virtual double Execute(double input)
		{
			return input;
		}
	}

	public class OperationSet : Operation
	{
		[XmlElement(typeof(OperationSet))]
		[XmlElement(typeof(Arithmetic))]
		[XmlElement(typeof(Lookup))]
		public List<Operation> Operations { get; set; }

		public OperationSet() { Operations = new List<Operation>(); }

		public OperationSet(params Operation[] operations)
		{
			Operations = new List<Operation>();
			Operations.AddRange(operations);
		}

		public override double Execute(double input)
		{
			double value = input;

			foreach (Operation o in Operations)
			{
				value = o.Execute(value);
			}

			return value;
		}
	}

	public class Lookup : Operation
	{
		[XmlText]
		public string Argument { get { return getArgument(); } set { setArgument(value); } }

		LookupTable table;

		public Lookup() { }

		public override double Execute(double input)
		{
			try { return table.Interpolate(input); }
			catch { return input; }
		}

		string getArgument()
		{
			return table?.filename;
		}

		void setArgument(string value)
		{
			table = new LookupTable(value);
		}
	}

	public class Arithmetic : Operation
	{
		public enum Operators { None, Add, Subtract, Multiply, Divide, Power, Ln, Log }

		private static Dictionary<string, Operators> SymbolOperatorDictionary = new Dictionary<string, Operators>
		{
			{ "+", Operators.Add },
			{ "-", Operators.Subtract },
			{ "*", Operators.Multiply },
			{ "/", Operators.Divide },
			{ "^", Operators.Power },
			{ "ln", Operators.Ln },
			{ "log", Operators.Log }
		};

		private static Dictionary<Operators, string> OperatorSymbolDictionary = new Dictionary<Operators, string>
		{
			{ Operators.Add, "+" },
			{ Operators.Subtract, "-" },
			{ Operators.Multiply, "*" },
			{ Operators.Divide, "/" },
			{ Operators.Power, "^" },
			{ Operators.Ln, "Ln" },
			{ Operators.Log, "Log" }
		};

		public static Operators ParseOperator(string operatorSymbol)
		{
			try { return SymbolOperatorDictionary[operatorSymbol.ToLower()]; }
			catch { return Operators.None; }
		}

		public static string OperatorSymbol(Operators operatorType)
		{
			try { return OperatorSymbolDictionary[operatorType]; }
			catch { return ""; }
		}

		private string inputSymbol = "x";

		string _formula = "";
		[XmlText]
		public string Argument { get { return getArgument(); } set { setArgument(value); } }

		bool _inputFirst = true;
		[XmlIgnore]
		public bool InputFirst
		{
			get { return _inputFirst; }
			set { _inputFirst = value; updateFormula(); compile(); }
		}

		Operators _operator = Operators.None;
		[XmlIgnore]
		public Operators Operator
		{
			get { return _operator; }
			set { _operator = value; updateFormula(); compile(); }
		}

		double _operand;
		[XmlIgnore]
		public double Operand
		{
			get { return _operand; }
			set { _operand = value; updateFormula(); }
		}

		Func<double, double, double> exec;

		public Arithmetic() { }

		public Arithmetic(string argument)
		{
			Argument = argument;
		}

		public override double Execute(double input)
		{
			try { return exec(input, _operand); }
			catch { return input; }
		}

		string getArgument()
		{
			return _formula;
		}

		void setArgument(string value)
		{
			_formula = value;
			parseProperties();
			compile();
			updateFormula();
		}

		void updateFormula()
		{
			if (_operator == Operators.Ln)
				_formula = Arithmetic.OperatorSymbol(_operator) + "(" + inputSymbol + ")";
			else if (_operator == Operators.Log)
			{
				_formula = Arithmetic.OperatorSymbol(_operator) + (_inputFirst ? _operand.ToString() : inputSymbol) + "(" + (_inputFirst ? inputSymbol : _operand.ToString()) + ")";
			}
			else if (_operator == Operators.Add && _operand < 0)
			{
				_formula = inputSymbol + OperatorSymbol(Operators.Subtract) + Math.Abs(_operand);
			}
			else
			{
				_formula = (_inputFirst ? inputSymbol : _operand.ToString()) + OperatorSymbol(_operator) + (_inputFirst ? _operand.ToString() : inputSymbol);
			}
		}

		void parseProperties()
		{
			try
			{
				string toParse = _formula.ToLower().Replace("(", "").Replace(")", "");

				if (toParse.StartsWith("ln"))
				{
					_operator = Operators.Ln;
				}
				else if (toParse.StartsWith("log"))
				{
					_operator = Operators.Log;
					toParse = toParse.Replace("log", "");

					if (toParse.StartsWith(inputSymbol))
						_inputFirst = false;
					toParse = toParse.Replace(inputSymbol, "");
				}
				else
				{
					toParse = toParse.Replace(inputSymbol, "");

					char testChar = toParse.First();
					if (!Char.IsNumber(testChar))
					{
						_operator = ParseOperator(testChar.ToString());
						toParse = toParse.Remove(0, 1);
					}
					else
					{
						_inputFirst = false;
						_operator = ParseOperator(toParse.Last().ToString());
						toParse = toParse.Remove(toParse.Length - 1);
					}
				}

				try
				{
					_operand = double.Parse(toParse);
					if (_operator == Operators.Subtract && _inputFirst)
					{
						_operator = Operators.Add;
						_operand = -_operand;
					}
				}
				catch { }
			}
			catch { }
		}

		void compile()
		{
			if (_operator == Operators.None)
				return;

			ParameterExpression input = Expression.Parameter(typeof(double), "input");
			ParameterExpression operand = Expression.Parameter(typeof(double), "operand");
			ParameterExpression left = _inputFirst ? input : operand;
			ParameterExpression right = _inputFirst ? operand : input;

			Expression execute = null;

			switch (_operator)
			{
				case Operators.None:
					return;
				case Operators.Add:
					execute = Expression.Add(left, right);
					break;
				case Operators.Subtract:
					execute = Expression.Subtract(left, right);
					break;
				case Operators.Multiply:
					execute = Expression.Multiply(left, right);
					break;
				case Operators.Divide:
					execute = Expression.Divide(left, right);
					break;
				case Operators.Power:
					execute = Expression.Power(left, right);
					break;
				case Operators.Ln:
					execute = Expression.Call(typeof(Math).GetMethod("Log", new[] { typeof(double) }), input);
					break;
				case Operators.Log:
					execute = Expression.Call(typeof(Math).GetMethod("Log", new[] { typeof(double), typeof(double) }), left, right);
					break;
				default:
					break;
			}
			
			exec = Expression.Lambda<Func<double, double, double>>
			(
				execute,
				input,
				operand
			).Compile();
		}
	}
}