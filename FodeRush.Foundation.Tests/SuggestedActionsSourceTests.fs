namespace FodeRush.Foundation.Tests


open FodeRush.Foundation
open System.Linq
open System.Threading
open System.Collections
open System.Collections.Generic
open Microsoft.VisualStudio.Language.Intellisense
open Microsoft.VisualStudio.Text

type MockCategorySet() =
    interface ISuggestedActionCategorySet with
        member __.Contains name = true
        member __.GetEnumerator() = null :> IEnumerator<string>
        member __.GetEnumerator() = null :> IEnumerator

(*
[<TestFixture>]
type SuggestedActionsSourceTests() = 
    [<Test>]
    member this.ShouldReturnTwoSuggestedActions() = 
        let categorySet = new MockCategorySet()
        let span = new SnapshotSpan()
        let token = CancellationToken.None
        let sut = new SuggestedActionsSource() :> ISuggestedActionsSource
        let actions = sut.GetSuggestedActions(categorySet, span, token)
        Assert.That(actions, Is.Not.Null)
        Assert.That(actions.Count(), Is.EqualTo(2))
*)