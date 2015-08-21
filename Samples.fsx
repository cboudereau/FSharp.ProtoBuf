#r @"FSharp.ProtoBuf\bin\Debug\FSharp.ProtoBuf.dll"

open FSharp.RegexTypeProvider
open System.Text.RegularExpressions

type test = RegexProvider< "(?<Name>.*)" >

test.IsMatch("clement")

let t = test()

let r = t.Match("clement")

r.Name

