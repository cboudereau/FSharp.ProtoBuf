﻿namespace FSharp.RegexTypeProvider

open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open System.Text.RegularExpressions
open ProviderImplementation.ProvidedTypes

[<TypeProvider>]
type public RegexProvider() as this =
    inherit TypeProviderForNamespaces()

    // Get the assembly and namespace used to house the provided types.
    let thisAssembly = Assembly.GetExecutingAssembly()
    let rootNamespace = "FSharp.RegexTypeProvider"
    let baseTy = typeof<obj>
    let staticParams = [ProvidedStaticParameter("pattern", typeof<string>)]

    let regexTy = ProvidedTypeDefinition(thisAssembly, rootNamespace, "RegexProvider", Some baseTy)

    do regexTy.DefineStaticParameters(
        staticParameters=staticParams, 
        apply=(fun typeName parameterValues ->

          match parameterValues with 
          | [| :? string as pattern|] -> 
            let r = System.Text.RegularExpressions.Regex(pattern)            
            let ty = ProvidedTypeDefinition(
                        thisAssembly, 
                        rootNamespace, 
                        typeName, 
                        baseType = Some baseTy)

            ty.AddXmlDoc "A strongly typed interface to the regular expression '%s'"

            // Provide strongly typed version of Regex.IsMatch static method.
            let isMatch = ProvidedMethod(
                            methodName = "IsMatch", 
                            parameters = [ProvidedParameter("input", typeof<string>)], 
                            returnType = typeof<bool>, 
                            IsStaticMethod = true,
                            InvokeCode = fun args -> <@@ Regex.IsMatch(%%args.[0], pattern) @@>) 

            isMatch.AddXmlDoc "Indicates whether the regular expression finds a match in the specified input string"

            ty.AddMember isMatch

            // Provided type for matches
            // Again, erase to obj even though the representation will always be a Match
            let matchTy = ProvidedTypeDefinition(
                            "MatchType", 
                            baseType = Some baseTy, 
                            HideObjectMethods = true)

            // Nest the match type within parameterized Regex type.
            ty.AddMember matchTy
        
            // Add group properties to match type
            for group in r.GetGroupNames() do
                // Ignore the group named 0, which represents all input.
                if group <> "0" then
                    let prop = ProvidedProperty(
                                propertyName = group, 
                                propertyType = typeof<Group>, 
                                GetterCode = fun args -> <@@ ((%%args.[0]:obj) :?> Match).Groups.[group] @@>)
                    prop.AddXmlDoc(sprintf @"Gets the ""%s"" group from this match" group)
                    matchTy.AddMember(prop)

            // Provide strongly typed version of Regex.Match instance method.
            let matchMeth = ProvidedMethod(
                                methodName = "Match", 
                                parameters = [ProvidedParameter("input", typeof<string>)], 
                                returnType = matchTy, 
                                InvokeCode = fun args -> <@@ ((%%args.[0]:obj) :?> Regex).Match(%%args.[1]) :> obj @@>)
            matchMeth.AddXmlDoc "Searches the specified input string for the first occurence of this regular expression"
            
            ty.AddMember matchMeth
            
            // Declare a constructor.
            let ctor = ProvidedConstructor(
                        parameters = [], 
                        InvokeCode = fun args -> <@@ Regex(pattern) :> obj @@>)

            // Add documentation to the constructor.
            ctor.AddXmlDoc "Initializes a regular expression instance"

            ty.AddMember ctor
            
            ty
          | _ -> failwith "unexpected parameter values")) 

    do this.AddNamespace(rootNamespace, [regexTy])

[<TypeProviderAssembly>]
do ()