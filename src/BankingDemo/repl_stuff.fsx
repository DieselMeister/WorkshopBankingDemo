
let tree =
    String.init 9 (fun i -> sprintf "%*s/%s\\\n"(9-i)" "((String.replicate 9 "o--*-").[i..i+i*2]))+" #fsharp || #fsadvent"


printf "%s" tree
