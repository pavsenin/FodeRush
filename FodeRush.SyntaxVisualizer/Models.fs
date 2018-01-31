namespace FodeRush.SyntaxVisualizer.Models

open FodeRush.Platform.Utils

type SyntaxNodeOrSyntaxTokenModel =
    | Node of Name : string * Span : Span * Children : SyntaxNodeOrSyntaxTokenModel list * Original : obj
    | Token of Name : string * Span : Span * Original : obj