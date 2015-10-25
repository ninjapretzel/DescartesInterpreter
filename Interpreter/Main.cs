using System;
using System.IO;

class Program {

	//Entry Point.
	//Similar to java in the way that it is detected/used.
	static void Main(string[] args)	{
		OldMain(args);
	}

	public static Func<int> MakeCounter() {
		int i = 0;
		return ( () => { 
			i++;
			return i;
		} );
	}

	public static void OldMain(string[] args) {
		string workingDirectory = Directory.GetCurrentDirectory();
		string file = workingDirectory + "\\src.txt";

		Console.WriteLine("Reading file from: ");
		Console.WriteLine(file);
		Console.WriteLine("\nPress ENTER to read File.");
		Console.ReadLine();
		string content = "";

		//Attempt to grab source code from the file
		//try/catch/finally are similar to java
		try {
			content = File.ReadAllText(file);

			Console.WriteLine("File opened successfully\n\nFile Content:\n");
			Console.WriteLine(content);

		} catch (Exception e) {
			Console.WriteLine("Could not find " + file);
			Console.WriteLine("Make sure that there is a file called \"src.txt\"");
			Console.WriteLine("Located in: " + workingDirectory);
			Console.ReadLine();
			Console.WriteLine(e);
			return;
		}

		Console.WriteLine("Done Reading File...");
		Console.WriteLine("\nReady to Tokenize...\nPress ENTER ");
		Console.ReadLine();

		//Tokenizer is the tokenizer we worked on, modified for this assignment.
		//Convert file to uppercase
		Tokenizer t = new Tokenizer(content.ToUpper());
		while (!t.done) {
			Console.WriteLine(t.peekToken);
			t.Next();
		}

		Console.WriteLine("Done Tokenizing");
		Console.WriteLine("next: " + t.peekToken);
		Console.WriteLine("\nReady to build Parse Tree...\nPress ENTER.");
		Console.ReadLine();

		//Move 'head' of tokenizer back to the begining of the file.
		t.Reset();

		//Debug hooks. Set these to true to see additional debug information.
		t.debugMode = false;
		DescartesParseTree.Node.debugMode = false;

		//Build a parse tree from the tokenizer
		DescartesParseTree tree = new DescartesParseTree(t);
		tree.Build();

		Console.WriteLine("\nParse Tree Built\nReady to execute program...\nPress ENTER.");
		Console.ReadLine();


		Console.WriteLine("\nExecution Begins");
		Console.WriteLine("--------------------------------");
		//RuntimeState holds runtime information for the interpreter.
		RuntimeState state = new RuntimeState();
		tree.root.Execute(state);
		Console.WriteLine("--------------------------------");
		Console.WriteLine("Execution Ends");

		Console.WriteLine("\nExecution finished. Final State:");
		state.Dump();

		Console.WriteLine("\n\nPress ENTER to exit");
		Console.ReadLine();

	}


}
