using System;
using System.Text.RegularExpressions;

//Tokenizer is a port of the previous program we wrote to tokenize a file.
//This is an adaptation of that, where it stores the original file, and also keeps track
//of the last token that was read, as well as the previous token.
public class Tokenizer {

	//Basic tokens in the Descartes language.
	public static string[] basicTokens = {
		"IF", "THEN", "ELSE", "FI", "LOOP", "BREAK", "READ", "PRINT", "AND", "OR",
		".", ")", "(", "/", "*", "-", "+", "<>", ">=", "<=", ">", "=", "<", ":=", ";"
	};

	//Sometimes these get added onto numbers if they are right next to them
	//I have no idea why this happens.
	public static string[] weridStuff = {
		"/", "+", "-", "*", ";"
	};

	public static Regex number = new Regex(@"[+-]?[0-9]+.?[0-9]*");
	public static Regex identifier = new Regex(@"[A-Z_][A-Z_0-9]*");

	//State information
	public string originalInput;
	public string remainingInput;
	public Token peekToken;
	public Token lastToken;
	public Token lastRealToken;

	public bool debugMode = false;


	public bool done { get { return !peekToken.isValid; } }


	public Tokenizer(string input) {
		originalInput = input;
		Reset();
	}

	public Tokenizer Clone() {
		Tokenizer t = new Tokenizer(originalInput);
		t.remainingInput = remainingInput;
		t.peekToken = peekToken;
		t.lastToken = lastToken;
		t.lastRealToken = lastRealToken;
		return t;
	}

	public void Reset() {
		remainingInput = originalInput;
		lastToken = null;
		lastRealToken = null;
		peekToken = Peek();
	}

	//Move the cursor to the next token.
	public void Move() {
		if (done) { return; }

		lastToken = peekToken;
		if (lastToken.isValid && !lastToken.isWhitespace) {
			lastRealToken = lastToken;
		}

		remainingInput = remainingInput.Substring(lastToken.length);
		peekToken = Peek();

	}

	//Move the cursor to the next real (non whitespace/newline) token.
	//Returns the last real token it was peeking at.
	//Tokenizer's peekToken is the next non-whitespace token.
	public Token Next() {
		Move();

		while (peekToken.isWhitespace) {
			Move();
			if (!peekToken.isValid) { break; }
		}
		if (debugMode) {

			Console.WriteLine("Next(): (" + lastRealToken.actualToken 
				+ ") -> (" + peekToken.actualToken + ")");
		}
		return lastRealToken;
	}

	//Peek at the next real (non whitespace/newline) token.
	public Token PeekNextRealToken() {
		Tokenizer copy = Clone();
		copy.Next();
		return copy.peekToken;
	}

	//Get the immediately next token
	public Token Peek() {
		//alias remaining input to make the code easier to type/read.
		string input = remainingInput;
		if (input.Length == 0) { return Token.emptyToken; }

		//Check for fixed keywords
		for (int i = 0; i < basicTokens.Length; i++) {
			if (input.IndexOf(basicTokens[i]) == 0) {
				return new Token(basicTokens[i], i + 1);
			}
		}

		//Whitespace characters
		if (input.IndexOf(" ") == 0) { return new Token(" ", 26); }
		if (input.IndexOf("\t") == 0) { return new Token("\t", 26); }

		//Newline Characters
		if (input.IndexOf("\r\n") == 0) { return new Token("\r\n", 27); }
		if (input.IndexOf("\n\r") == 0) { return new Token("\n\r", 27); }
		if (input.IndexOf("\n") == 0) { return new Token("\n", 27); }
		if (input.IndexOf("\r") == 0) { return new Token("\r", 27); }

		//Check for END fixed keyword
		if (input.IndexOf("END") == 0) { return new Token("END", 31); }

		//Check for COLON 
		if (input.IndexOf(":") == 0) { return new Token(":", 32); }

		//Check for string
		if (input.IndexOf("\"") == 0) {
			int endIndex = input.IndexOf("\"", 1);
			int nextLine = nextNewline(input);

			if (endIndex < 0) { return Token.mismatchedQuote; }
			if (nextLine < endIndex) { return Token.newlineInLiteral; }

			string t = input.Substring(0, endIndex + 1);


			return new Token(t, 30);
		}

		//Check for number using the number regex.
		Match numberCheck = number.Match(input);
		if (numberCheck.Length > 0 && numberCheck.Index == 0) {
			string check = numberCheck.Value;
			foreach (string s in weridStuff) {
				if (check.IndexOf(s) == check.Length - s.Length) {
					check = check.Substring(0, check.Length - s.Length);
				}
			}	
			return new Token(check, 29);
		}

		//Check for a identifier using the indentifier regex.
		Match identifierCheck = identifier.Match(input);
		if (identifierCheck.Length > 0 && identifierCheck.Index == 0) {
			return new Token(identifierCheck.Value, 28);
		}


		//if nothing matches, there's an unknown character.
		return Token.unknownCharacters;

	}

	//Helper function to get the position of the next real newline character in the string.
	public static int nextNewline(string str) {
		int unix = str.IndexOf("\n");
		int mac = str.IndexOf("\r");
		int odd = str.IndexOf("\n\r");
		int win = str.IndexOf("\r\n");

		if (unix == -1) { unix = str.Length + 1; }
		if (mac == -1) { mac = str.Length + 1; }
		if (odd == -1) { odd = str.Length + 1; }
		if (win == -1) { win = str.Length + 1; }

		int num = Min(unix, mac, odd, win);
		if (num == str.Length + 1) { num = -1; }

		return num;
	}

	public static int Min(params int[] nums) {
		int num = int.MaxValue;
		for (int i = 0; i < nums.Length; i++) {
			if (nums[i] < num) { num = nums[i]; }
		}
		return num;
	}


}
	
//In C#, public classes don't need to be listed in their own file.
//So here, I also define the Token class
public class Token {
	public string actualToken;
	public string fullToken;
	public int index;
	public int length;

	public const int NUMBER = 29;
	public const int IDENTIFIER = 28;
	//This is a property, which acts like a function, but is used like a variable.
	//C# also 'inlines' small properties, making them similar to C++ preprocessor macros.
	public bool isValid { get { return index > 0; } }

	public bool isWhitespace { get { return index == 26 || index == 27; } }

	public bool isNumber { get { return index == NUMBER; } }
	public bool isIdentifier { get { return index == IDENTIFIER; } }

	public bool isAddSub { get { return actualToken == "+" || actualToken == "-"; } }
	public bool isMulDiv { get { return actualToken == "*" || actualToken == "/"; } }
	public bool isRelation { get { return actualToken == "<" ||
									actualToken == "<=" ||
									actualToken == "=" ||
									actualToken == ">" ||
									actualToken == ">=" ||
									actualToken == "<>";
		} 
	}


	//Some more properties for errors
	public static Token newlineInLiteral { get { return new Token("[ERROR, STRING LITERAL CONTAINS NEWLINES.]", -3); } }
	public static Token mismatchedQuote { get { return new Token("[ERROR, MISMATCHED \".]", -2); } }
	public static Token emptyToken { get { return new Token("[Empty String, done, or nothing left to process]", -400); } }
	public static Token unknownCharacters { get { return new Token("[ERROR - UNKNOWN CHARACTERS OR SYMBOLS.]", -1); } }

	public Token(string s, int i) {
		length = s.Length;
		actualToken = s;
		fullToken = s;

		if (length >= 2 && actualToken[0] == '\"' && actualToken[actualToken.Length - 1] == '\"') {
			actualToken = s.Substring(1, length-2);
		}

		index = i;
	}

	public override string ToString() {
		if (index == 26) { return "SPACE\t: 26"; }
		if (index == 27) { return "EOLN\t: 27"; }
		return "" + actualToken + "\t: " + index;
	}


}

