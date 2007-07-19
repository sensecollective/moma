using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO;

namespace MoMA.Analyzer
{
	public class MethodExtractor
	{
		// Parse the assemblies looking for various sticky points
		// Leave any of the SortedList parameters null that you aren't interested in
		public static void ExtractFromAssembly (string assembly, SortedList<string, Method> allMethods, SortedList<string, Method> throwsNotImplementedMethods, SortedList<string, Method> monoTodoMethods)
		{
			AssemblyDefinition ad = AssemblyFactory.GetAssembly (assembly);
			
			//Gets all types of the MainModule of the assembly
			foreach (TypeDefinition type in ad.MainModule.Types) {
				if (type.Name != "<Module>") {
				
					// Is the type part of the public API?
					if (!IsTypeVisible (type))
						continue;
						
					//Check for [MonoTODO]s on a Property, but not actually on the Getter/Setter
					if (monoTodoMethods != null) {
						foreach (PropertyDefinition property in type.Properties) {
							foreach (CustomAttribute ca in property.CustomAttributes) {
								if (IsReportableMonoTODO (ca.Constructor.DeclaringType.ToString ())) {
									if (property.GetMethod != null && IsMethodVisible (property.GetMethod))
										monoTodoMethods[property.GetMethod.ToString ()] = new Method (property.GetMethod.ToString (), ca.ConstructorParameters.Count > 0 ? ca.ConstructorParameters[0].ToString ().Replace ('\n', ' ') : string.Empty);
									if (property.SetMethod != null && IsMethodVisible (property.SetMethod))
										monoTodoMethods[property.SetMethod.ToString ()] = new Method (property.SetMethod.ToString (), ca.ConstructorParameters.Count > 0 ? ca.ConstructorParameters[0].ToString ().Replace ('\n', ' ') : string.Empty);
								}
							}
						}
					}
					
					//Gets all methods of the current type
					foreach (MethodDefinition method in type.Methods) {
						if (!IsMethodVisible (method))
							continue;
							
						// If adding all methods, add this method
						if (allMethods != null)
							allMethods[method.ToString ()] = new Method (method.ToString ());

						// If adding MonoTODO methods, check this method
						if (monoTodoMethods != null)
							foreach (CustomAttribute ca in method.CustomAttributes)
								if (IsReportableMonoTODO (ca.Constructor.DeclaringType.ToString ()))
									monoTodoMethods[method.ToString ()] = new Method (method.ToString (), ca.ConstructorParameters.Count > 0 ? ca.ConstructorParameters[0].ToString ().Replace ('\n', ' ') : string.Empty);

						// If adding methods that throw NotImplementedException, look for those
						if (throwsNotImplementedMethods != null && ThrowsNotImplementedException (method))
							throwsNotImplementedMethods[method.ToString ()] = new Method (method.ToString ());
					}

					//Gets all constructors of the current type
					foreach (MethodDefinition method in type.Constructors) {
						// We only want Public and Protected methods
						if (!IsMethodVisible (method))
							continue;

						// If adding all methods, add this method
						if (allMethods != null)
							allMethods[method.ToString ()] = new Method (method.ToString ());

						// If adding MonoTODO methods, check this method
						if (monoTodoMethods != null)
							foreach (CustomAttribute ca in method.CustomAttributes)
								if (IsReportableMonoTODO (ca.Constructor.DeclaringType.ToString ()))
									monoTodoMethods[method.ToString ()] = new Method (method.ToString (), ca.ConstructorParameters.Count > 0 ? ca.ConstructorParameters[0].ToString ().Replace ('\n', ' ') : string.Empty);

						// If adding methods that throw NotImplementedException, look for those
						if (throwsNotImplementedMethods != null && ThrowsNotImplementedException (method))
							throwsNotImplementedMethods[method.ToString ()] = new Method (method.ToString ());
					}
				}
			}
		}

		public static void ComputeMethodDifference (SortedList<string, Method> master, SortedList<string, Method> subset, SortedList<string, Method> output)
		{
			// If it's in the master but not the subset, add it to the output
			foreach (string s in master.Keys)
				if (!(subset.ContainsKey (s)))
					output[s] = new Method (s);
		}
		
		// Is method part of the public API?  (Public or Protected)
		private static bool IsMethodVisible (MethodDefinition method)
		{
			if (!((method.Attributes & MethodAttributes.Family) == MethodAttributes.Family || (method.Attributes & MethodAttributes.Public) == MethodAttributes.Public))
				return false;

			return true;
		}
		
		// Is type part of the public API?  (Public or Protected)
		private static bool IsTypeVisible (TypeDefinition type)
		{
			if (((type.Attributes & TypeAttributes.Public) == 0 && (type.Attributes & TypeAttributes.NestedPublic) == 0) || (((type.Attributes & TypeAttributes.NestedPrivate) == TypeAttributes.NestedPrivate)) || (((type.Attributes & TypeAttributes.NestedFamily) == TypeAttributes.NestedFamily)))
				return false;
			
			// Recurse to make sure all parents are visible
			if (type.DeclaringType != null)
				return IsTypeVisible (TypeReferenceToDefinition (type.DeclaringType));
				
			return true;
		}
		
		private static TypeDefinition TypeReferenceToDefinition (TypeReference type)
		{
			return type.Module.Types[type.FullName];
		}
		
		// Is this attribute a MonoTODO that we want to report in MoMA?
		private static bool IsReportableMonoTODO (string attributeString)
		{
			if (attributeString.Equals ("System.MonoTODOAttribute"))
				return true;

			if (attributeString.Equals ("System.MonoLimitationAttribute"))
				return true;

			if (attributeString.Equals ("System.MonoNotSupportedAttribute"))
				return true;
		
			return false;
		}
		
		// Does the method throw a NotImplementedException?
		private static bool ThrowsNotImplementedException (MethodDefinition method)
		{
			if (method.Body != null)
				foreach (Instruction i in method.Body.Instructions)
					if (i.OpCode == OpCodes.Throw)
						if (i.Previous.Operand != null && i.Previous.Operand.ToString ().StartsWith ("System.Void System.NotImplementedException"))
							return true;
							
			return false;
		}
	}
}
