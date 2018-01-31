namespace ViewModels

open System
open System.Windows.Media
open FodeRush.SyntaxVisualizer.Models
open FodeRush.SyntaxVisualizer
open FodeRush.Platform.Interfaces

type NodeViewModel (model : SyntaxNodeOrSyntaxTokenModel) =

    let displayName, span, children, foreground, original = 
        match model with
        | Node(name, span, children, original) ->
            String.Format("{0} {1}", name, span), span, children |> List.map NodeViewModel, Brushes.Blue, original
        | Token(name, span, original) ->
            String.Format("{0} {1}", name, span), span, [], Brushes.Red, original

    member x.DisplayName = displayName
    member x.Span = span
    member x.Foreground = foreground
    member x.Children = children
    member x.Original = original

type Event =
    | SelectNode of NodeViewModel option
    | ChangeView of ITextView option
    | Unknown