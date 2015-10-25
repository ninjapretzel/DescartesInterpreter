using System;
using System.Collections.Generic;

public class DescartesParseTree {

	public Tokenizer source;
	public Prog root;

	//Constructor.
	public DescartesParseTree(Tokenizer s) {
		source = s;
	}

	//Attempts to build the parse tree
	public void Build() {
		root = new Prog();
		root.Parse(source);
	}

	//Base node type
	//All nodes inherit from this type
	public class Node {
		//Debugging hook
		public static bool debugMode = false;
		//Debugging utility function
		public static void Log(object o) {
			if (debugMode) { Console.WriteLine("\t<Debug: " + o.ToString() + ">"); }
		}

		//I really never even use these two things from any node class... Oh well.
		public virtual int index { get { return -1; } }
		public virtual string type { get { return "Undefined"; } }

		//Required function for all nodes- all nodes must be able to parse.
		//Really no reason to return a reference to the current node.
		public virtual Node Parse(Tokenizer source) { return this; }

		//Helper evaluation functions to avoid duplication.
		public static bool Eval(BoolTerm term, BoolTermTail tail, RuntimeState state) {
			bool termVal = term.Evaluate(state);
			if (termVal == true) { return true; } // Short Circuit if we have a true value
			if (tail != null) {
				bool tailVal = tail.Evaluate(state);
				return termVal || tailVal;
			}
			return termVal;
		}

		public static bool Eval(BoolFactor factor, BoolFactorTail tail, RuntimeState state) {
			bool factorVal = factor.Evaluate(state);
			if (factorVal == false) { return false; } // Short Circuit if we have a false value
			if (tail != null) {
				bool tailVal = tail.Evaluate(state);
				return factorVal && tailVal;
			}
			return factorVal;
		}


		public static double Eval(ArithTerm term, ArithTermTail tail, RuntimeState state) {
			double termVal = term.Evaluate(state);
			if (tail != null) {
				double tailVal = tail.Evaluate(state);
				if (tail.op == "+") {
					return termVal + tailVal;
				} else {
					return termVal - tailVal;
				}
			}
			return termVal;
		}
		public static double Eval(ArithFactor factor, ArithFactorTail tail, RuntimeState state) {
			double factorVal = factor.Evaluate(state);
			if (tail != null) {
				double tailVal = tail.Evaluate(state);
				if (tail.op == "*") {
					return factorVal * tailVal;
				} else {
					if (tailVal == 0) {
						Log("Error, Divide by zero");
						throw new ArithmeticException();
					}
					return factorVal / tailVal;
				}
			}
			return factorVal;
		}
	}

	//Statement Node.
	//All executable statements derive from this node.
	public class Stmt : Node {
		public override int index { get { return 32; } }
		public override string type { get { return "stmt"; } }
		//Overridable execution function.
		//Gets passed the current state.
		//Idea is to use this state information and make a decision on it, or mutate it
		//Then move to the next statement...
		public virtual void Execute(RuntimeState state) {}
	}

	//Program Class
	//Holds information for the 'program' root node.
	//Makes more sense for this to inherit from stmt than be its own unique class.
	public class Prog : Stmt {
		public StmtList stmtList;
		public override int index { get { return 30; } }
		public override string type { get { return "prog"; } }
		//Blank Execute() function calls Execute(RuntimeState) with a new RuntimeState object
		public void Execute() { Execute(new RuntimeState()); }
		//Program Execution logic.
		public override void Execute(RuntimeState state) { 
			stmtList.Execute(state); 

			//If the program ends and the program is trying to break, 
			//that means either a break statement was at the 'root' statement list
			//Or there is a break statement with a non-existant label
			if (state.breaking) {
				Console.WriteLine("Error! - Attempting to break, and reached the end of program");
				if (state.breakTo != "") {
					Console.WriteLine("Label " + state.breakTo + ": Not Found");
				}
			}
		}
		public override Node Parse(Tokenizer source) {
			stmtList = new StmtList();
			stmtList.Parse(source);

			Token token = source.lastRealToken;
			if (token.actualToken != ".") {
				Console.WriteLine("Error!\nExpected: '.'\nFound: " + token.actualToken);

			}

			return this;
		}
	}

	//Other statement type nodes are in this region
	#region Statements
	public class StmtList : Stmt {
		public Stmt stmt;
		public StmtList next;
		public override int index { get { return 31; } }
		public override string type { get { return "stmt-list"; } }
		//Execute current statement, and then pass execution to the next statement.
		//Does not handle breaking - the statement contained in stmt handles 
		public override void Execute(RuntimeState state) {
			stmt.Execute(state); 
			if (next != null) {
				next.Execute(state); 
			}
		}
		public override Node Parse(Tokenizer source) {
			source.Next();
			Token token = source.lastRealToken;

			if (token.actualToken == "IF") {
				stmt = new IfStmt();
			} else if (token.actualToken == "LOOP") {
				stmt = new LoopStmt();
			} else if (token.actualToken == "BREAK") {
				stmt = new BreakStmt();
			} else if (token.actualToken == "READ") {
				stmt = new ReadStmt();
			} else if (token.actualToken == "PRINT") {
				stmt = new PrintStmt();
			} else if (token.actualToken != "") {
				stmt = new AssignStmt();
			} else {
				stmt = null;
			}

			stmt.Parse(source);


			token = source.peekToken;
			if (token.actualToken == ";") { 
				Log(">Next statement");
				source.Next();
				next = new StmtList();
				next.Parse(source);
			} else if (token.actualToken == ".") { 
				Log("Found '.', terminating");
				source.Next();
				return this;
			}


			return this;
		}
	}



	public class IfStmt : Stmt {
		public BoolExpr expr;
		public StmtList thenList;
		public ElsePart elsePart;
		public override int index { get { return 34; } }
		public override string type { get { return "if-stmt"; } }
		//An if statement evaluates its expression, and executes one of two instruction lists.
		public override void Execute(RuntimeState state) {
			if (state.breaking) { return; }
			bool val = expr.Evaluate(state);
			Log("If Statement reached - Condition: " + val);

			if (val) {
				thenList.Execute(state);
			} else {
				elsePart.Execute(state);
			}
		}
		public override Node Parse(Tokenizer source) {
			Log("========>(IfStmt)");
			expr = new BoolExpr();
			expr.Parse(source);

			Token token = source.Next();
			if (token.actualToken != "THEN") {
				Console.WriteLine("ERROR - 'THEN' expected, found: " + token.actualToken);
				throw new Exception();
			}

			thenList = new StmtList();
			thenList.Parse(source);
			elsePart = new ElsePart();
			elsePart.Parse(source);


			return this;
		}
	}

	public class ElsePart : Stmt {
		public StmtList list;
		public override int index { get { return 41; } }
		public override string type { get { return "else-part"; } }
		//An else statement executes its list if it is not null.
		//It is only executed if the if statement it resides in evaluates to false.
		public override void Execute(RuntimeState state) {
			if (state.breaking) { return; }
			if (list != null) { list.Execute(state); }
		}
		public override Node Parse(Tokenizer source) {
			Log("========>(ElsePart)");
			Token token = source.Next();

			//Log(""+token.actualToken);
			if (token.actualToken == "FI") {
				Log("End If");
				list = null;
			}

			if (token.actualToken == "ELSE") {
				Log("\t>>>Else");

				list = new StmtList();
				list.Parse(source);

				token = source.Next();
				if (token.actualToken == "FI") {
					Log("\t>>>End If");
				} else {
					Console.WriteLine("Expected : 'FI', Found: " + token.actualToken);
					throw new Exception();
				}

			}

			return this;
		}
	}

	public class LoopStmt : Stmt {
		public string label;
		public StmtList body;
		public override int index { get { return 35; } }
		public override string type { get { return "loop-stmt"; } }
		//Loop statements continue to run their loop body until a break statement is reached.
		public override void Execute(RuntimeState state) {
			if (state.breaking) { return; }
			while (true) {
				body.Execute(state);
				if (state.breaking) {
					Log("Attempting to break [Loop: " + label + "] [Target: " + state.breakTo + "]");
					if (state.breakTo == label || state.breakTo == "") {
						state.FinishBreak();
					} 
					return;
				}
			}

		}
		public override Node Parse(Tokenizer source) {
			Token token = source.Next();
			if (!token.isIdentifier) {
				Console.WriteLine("Identifier Expected next to LOOP statement, found: " + token.actualToken);
				throw new Exception();
			}

			label = token.actualToken;

			token = source.Next();
			if (token.actualToken != ":") {
				Console.WriteLine("COLON (:) expected after Identifier after LOOP label, found: " + token.actualToken);
				throw new Exception();
			}

			body = new StmtList();
			body.Parse(source);

			token = source.Next();
			if (token.actualToken != "REPEAT") {
				Console.WriteLine("REPEAT expected to end LOOP block, found: " + token.actualToken);
				throw new Exception();
			}
			
			return this;
		}

	}

	public class BreakStmt : Stmt {
		public string breakTarget;
		public override int index { get { return 36; } }
		public override string type { get { return "break-stmt"; } }
		//Break statements change the runtime state to 'breaking'.
		//They set their target to the state's break target.
		//If this target is empty, it just breaks from one loop layer
		//If this target is some other identifier, 
		//it breaks from all loop layers until it reaches that label
		//Or until it crashes.
		public override void Execute(RuntimeState state) {
			if (state.breaking) { return; }
			state.Break(breakTarget);
		}
		public override Node Parse(Tokenizer source) {
			if (source.peekToken.isIdentifier) {
				Token token = source.Next();
				breakTarget = token.actualToken;
			} else {
				breakTarget = "";
			}
			return this;
		}

	}

	public class AssignStmt: Stmt {
		public string varName;
		public ArithExpr expr;
		public override int index { get { return 37; } }
		public override string type { get { return "assign-stmt"; } }
		//Assignment statements mutate the runtime state 
		//they evaluate an arithmatic expression 
		//and assign the result into a variable
		public override void Execute(RuntimeState state) {
			if (state.breaking) { return; }
			state[varName] = expr.Evaluate(state);
		}
		public override Node Parse(Tokenizer source) {
			Log("========>(Assignment Statement)");
			Token token = source.lastRealToken;
			Log(token.actualToken + " := ");

			varName = token.actualToken;

			token = source.Next();
			if (token.actualToken != ":=") {
				Console.WriteLine("Error!\nExpected: ':='\nFound: " + token.actualToken);
				throw new Exception();
			}

			expr = new ArithExpr();
			expr.Parse(source);

			return this;
		}
	}

	public class ReadStmt: Stmt {
		public List<string> stuffToRead;
		public override int index { get { return 38; } }
		public override string type { get { return "read-stmt"; } }
		//Reat statements read input from the user 
		//and store it into one or more variables
		public override void Execute(RuntimeState state) {
			if (state.breaking) { return; }
			foreach (string varName in stuffToRead) {
				Console.Write("IN (" + varName + ") <== : ");
				string input = Console.ReadLine();
				double val = 0;
				if (double.TryParse(input, out val)) {
				} else {
					Console.WriteLine("Error, incorrect value");
					throw new Exception();
				}
				state[varName] = val;
			}
		}
		public override Node Parse(Tokenizer source) {
			Log("========>(ReadStmt)");
			Token token = source.Next();
			stuffToRead = new List<string>();
			stuffToRead.Add(token.actualToken);

			while (source.peekToken.actualToken == ",") {
				//Skip over ','
				source.Next();
				token = source.Next();
				stuffToRead.Add(token.actualToken);
			}

			return this;
		}
	}

	public class PrintStmt: Stmt {
		public List<string> stuffToPrint;
		public override int index { get { return 39; } }
		public override string type { get { return "print-stmt"; } }
		//Print statements print one or more variables to the screen.
		public override void Execute(RuntimeState state) {
			if (state.breaking) { return; }
			foreach (string varName in stuffToPrint) {
				Console.WriteLine("OUT==> " + varName + "=" + state[varName]);
			}

		}
		public override Node Parse(Tokenizer source) {
			Log("========>(PrintStmt)");
			Token token = source.Next();
			stuffToPrint = new List<string>();
			stuffToPrint.Add(token.actualToken);

			while (source.peekToken.actualToken == ",") {
				//Skip over ','
				source.Next();
				token = source.Next();
				stuffToPrint.Add(token.actualToken);
			}

			return this;
		}

	}

	#endregion

	//Boolean Logic nodes are in thius region
	#region Boolean Logic
	//All nodes that reduce into a boolean value extend from this class
	//BoolExpr, BoolTerm, BoolFactor, BoolTermTail, BoolFactorTail
	public class BoolNode : Node {
		//Overridable function to provide the value of a node during any runtime state.
		//Most of the time, this involves calling one of the 'bool Eval' helper functions from Node
		public virtual bool Evaluate(RuntimeState state) { return false; }
	}

	public class BoolExpr : BoolNode {
		public BoolTerm term;
		public BoolTermTail tail;
		public override int index { get { return 40; } }
		public override string type { get { return "bool-expr"; } }
		public override bool Evaluate(RuntimeState state) { return Eval(term, tail, state);}
		public override Node Parse(Tokenizer source) {
			Log("========>(BoolExpr)");
			term = new BoolTerm();
			term.Parse(source);

			Token token = source.peekToken;
			if (token.actualToken == "OR") {
				tail = new BoolTermTail();
				tail.Parse(source);
			}

			return this;
		}
	}

	public class BoolTerm : BoolNode {
		public BoolFactor factor;
		public BoolFactorTail tail;
		public override int index { get { return 44; } }
		public override string type { get { return "bool-term"; } }
		public override bool Evaluate(RuntimeState state) { return Eval(factor, tail, state); }
		public override Node Parse(Tokenizer source) {
			Log("========>(BoolTerm)");
			factor = new BoolFactor();
			factor.Parse(source);

			Token token = source.peekToken;
			if (token.actualToken == "AND") {
				tail = new BoolFactorTail();
				tail.Parse(source);
			}

			return this;
		}
	}

	public class BoolTermTail : BoolNode {
		public BoolTerm term;
		public string op;
		public BoolTermTail tail;
		public override int index { get { return 45; } }
		public override string type { get { return "bool-term-tail"; } }
		public override bool Evaluate(RuntimeState state) { return Eval(term, tail, state); }
		public override Node Parse(Tokenizer source) {
			Log("========>(BoolTermTail)");
			Token token = source.Next();
			op = token.actualToken;

			term = new BoolTerm();
			term.Parse(source);

			token = source.peekToken;
			if (token.actualToken == "OR") {
				tail = new BoolTermTail();
				tail.Parse(source);
			}


			return this;
		}
	}


	public class BoolFactor : BoolNode {
		public ArithExpr left;
		public string op;
		public ArithExpr right;
		public override int index { get { return 46; } }
		public override string type { get { return "bool-factor"; } }
		//Boolean factor evaluation logic
		//This will grab the values of a left value
		//If there is a comparison and a right value, 
			//it evaluates the comparison with the two values
		//Otherwise, tests if the leftVal is not 0
		public override bool Evaluate(RuntimeState state) {
			double leftVal = left.Evaluate(state);

			if (right != null) {
				double rightVal = right.Evaluate(state);
				if (op == ">") { return leftVal > rightVal; }
				if (op == ">=") { return leftVal >= rightVal; }
				if (op == "<") { return leftVal < rightVal; }
				if (op == "<=") { return leftVal <= rightVal; }
				if (op == "=") { return leftVal == rightVal; }
				if (op == "<>") { return leftVal != rightVal; }
				Console.WriteLine("Unknown comparison: " + op);
				throw new Exception();
			}

			return leftVal != 0;
		}
		public override Node Parse(Tokenizer source) {
			Log("========>(BoolFactor)");
			left = new ArithExpr();
			left.Parse(source);

			Token token = source.peekToken;
			if (token.isRelation) {
				op = token.actualToken;
				source.Next();
				right = new ArithExpr();
				right.Parse(source);
			}

			return this;
		}
	}

	public class BoolFactorTail : BoolNode {
		public BoolFactor factor;
		public string op;
		public BoolFactorTail tail;

		public override int index { get { return 45; } }
		public override string type { get { return "bool-factor-tail"; } }
		public override bool Evaluate(RuntimeState state) { return Eval(factor, tail, state); }
		public override Node Parse(Tokenizer source) {
			Log("========>(BoolFactorTail)");
			Token token = source.Next();
			op = token.actualToken;

			factor = new BoolFactor();
			factor.Parse(source);

			token = source.peekToken;
			if (token.actualToken == "AND") {
				tail = new BoolFactorTail();
				tail.Parse(source);
			}

			return this;
		}
	}

	#endregion


	//Arithmatic Logic nodes are in this region
	#region Arithmatic Logic
	//All nodes that reduce into a number extend from this class
	//ArithExpr, ArithTerm, ArithFactor, ArithTermTail, ArithFactorTail
	public class ArithNode : Node {
		//Overridable function that reduces the current node to a single number.
		//Typically, this is calling one of the 'double Eval' helper functions from Node.
		public virtual double Evaluate(RuntimeState state) { return 0; }
	}

	public class ArithExpr : ArithNode {
		public ArithTerm term;
		public ArithTermTail tail;
		public override int index { get { return 40; } }
		public override string type { get { return "arith-expr"; } }
		public override double Evaluate(RuntimeState state) { return Eval(term, tail, state); }
		public override Node Parse(Tokenizer source) {
			Log("========>(ArithExpr)");
			term = new ArithTerm();
			term.Parse(source);

			Token token = source.peekToken;
			if (token.isAddSub) {
				tail = new ArithTermTail();
				tail.Parse(source);
			}

			return this;
		}
	}



	public class ArithTerm : ArithNode {
		public ArithFactor factor;
		public ArithFactorTail tail;
		public override int index { get { return 50; } }
		public override string type { get { return "arith-term"; } }
		public override double Evaluate(RuntimeState state) { return Eval(factor, tail, state); }
		public override Node Parse(Tokenizer source) {
			Log("========>(ArithTerm)");
			factor = new ArithFactor();
			factor.Parse(source);

			tail = null;
			Token token = source.peekToken;
			if (token.isMulDiv) {
				tail = new ArithFactorTail();
				tail.Parse(source);
			}

			return this;
		}
	}

	public class ArithTermTail : ArithNode {
		public string op;
		public ArithTerm term;
		public ArithTermTail tail;
		public override int index { get { return 51; } }
		public override string type { get { return "arith-term-tail"; } }
		public override double Evaluate(RuntimeState state) { return Eval(term, tail, state); }
		public override Node Parse(Tokenizer source) {
			Log("========>(ArithTermTail)");
			Token token = source.Next();
			if (!token.isAddSub) {
				Console.WriteLine("Expected : '+' or '-' - Found: " + token.actualToken);
				throw new Exception();
			}

			op = token.actualToken;
			Log("Operator: " + op);

			term = new ArithTerm();
			term.Parse(source);

			token = source.peekToken;
			if (token.isAddSub) {
				tail = new ArithTermTail();
				tail.Parse(source);
			}

			return this;
		}
	}

	public class ArithFactor : ArithNode {
		public Atom atom;
		public ArithFactor factor;
		public ArithExpr expr;
		public override int index { get { return 52; } }
		public override string type { get { return "arith-factor"; } }
		//Based on what is contained by the factor, it does a different application of evaluation logic.
		public override double Evaluate(RuntimeState state) {
			if (atom != null) { return atom.Evaluate(state); }
			if (factor != null) { return -1 * factor.Evaluate(state); }
			if (expr != null) { return expr.Evaluate(state); }
			return 0;
		}

		public override Node Parse(Tokenizer source) {
			Log("========>(ArithFactor)");
			Token token = source.peekToken;
			Log("Token: " + token.actualToken);

			if (token.actualToken == "-") {
				//Log("ArithFactor => - ArithFactor");
				source.Next();
				factor = new ArithFactor();
				factor.Parse(source);

			} else if (token.actualToken == "(") {
				//Log("ArithFactor => ( ArithExpr )");

				//Consume the '('
				token = source.Next();
				expr = new ArithExpr();
				expr.Parse(source);

				//Attemt to match to the ')'
				token = source.Next();
				if (token.actualToken != ")") {
					Console.WriteLine("Expecting ')', found " + token.actualToken);
					throw new Exception();
				}

			} else {
				//Log("ArithFactor => Atom");
				atom = new Atom();
				atom.Parse(source);
			}

			return this;
		}
	}

	public class ArithFactorTail : ArithNode {
		public string op;
		public ArithFactor factor;
		public ArithFactorTail tail;
		public override int index { get { return 53; } }
		public override string type { get { return "arith-factor-tail"; } }
		public override double Evaluate(RuntimeState state) { return Eval(factor, tail, state); }
		public override Node Parse(Tokenizer source) {
			Log("========>(ArithFactorTail)");
			Token token = source.Next();
			if (!token.isMulDiv) {
				Console.WriteLine("Expected : '*' or '/' - Found: " + token.actualToken);
				throw new Exception();
			}

			op = token.actualToken;
			Log("Operator: " + op);

			token = source.peekToken;
			Log("Next Token: " + token.actualToken);


			factor = new ArithFactor();
			factor.Parse(source);

			token = source.peekToken;
			if (token.isMulDiv) {
				tail = new ArithFactorTail();
				tail.Parse(source);
			}


			return this;
		}
	}

	public class Atom : ArithNode {
		public double val;
		public string id;
		public override int index { get { return 53; } }
		public override string type { get { return "arith-factor-tail"; } }
		//An atom is either a constant value, or a variable identifier
		//Evaluating an atom checks if it is an id, 
		//if it is, returns the value of that variable during the current state
		//otherwise returns the constant value it represents
		public override double Evaluate(RuntimeState state) {
			if (id != null) { return state[id]; } 
			return val;
		}
		public override Node Parse(Tokenizer source) {
			Log("========>(Atom)");
			Token token = source.Next();
			//Log(token.actualToken);

			if (token.index == Token.NUMBER) {
				val = Double.Parse(token.actualToken);
				id = null;
				Log("Atom => Const Value: " + val);
			} else if (token.index == Token.IDENTIFIER) {
				id = token.actualToken;
				Log("Atom => Identifier: " + id);
			}

			return this;
		}
	}


	#endregion




}