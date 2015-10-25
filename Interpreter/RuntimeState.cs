using System;
using System.Collections.Generic;

public class RuntimeState {
	//[Holds values of variables]//
	public Dictionary<string, double> variables;

	//C# Automatically Generates a 'property' when a property is declared as follows.
	private bool breakOnce { get; set; }
	//It will automatically create a field, and hook it up with a get/set function.
	//The property get/set functions can be used as if the property was a variable.
	//Then, different scopes can be applied to the get/set functions.
	public string breakTo { get; private set; }

	//Read-Only property.
	//[Is the program currently in a break-state?]//
	public bool breaking { get { return breakOnce || breakTo != ""; } }
	//Indexer property. Allows this type to be indexed by strings as if it was an array.
	public double this[string key] { 
		get {
			if (variables.ContainsKey(key)) { return variables[key]; }
			return 0;
		}
		set {
			variables[key] = value;
		}
	}
	public RuntimeState() {
		variables = new Dictionary<string, double>();
		breakTo = "";
	}
	public void FinishBreak() {
		breakTo = "";
		breakOnce = false;
	}

	public void Break() { if (!breaking) { breakOnce = true; } }
	public void Break(string target) { 
		if (!breaking) { 
			if (target == "") {
				breakOnce = true;
			} else {
				breakTo = target;
			}
		}
	}

	public void Dump() {
		foreach (var pair in variables) {
			Console.WriteLine(pair.Key.PadRight(12) + " : " + pair.Value);
		}
	}

}