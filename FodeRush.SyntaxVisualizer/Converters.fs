namespace Converters

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Data
open ViewModels
open FsXaml

module Convert = 
    let selectedItemChangedConverter (args : RoutedPropertyChangedEventArgs<Object>) =
        match args.NewValue with
        | :? NodeViewModel as v -> SelectNode(Some v)
        | _ -> SelectNode None

type EmptyConverter() =
    interface IValueConverter with
        override this.Convert(value, targetType, parameter, culture) =
            value
        override this.ConvertBack(value, targetType, parameter, culture) =
            null
type BoolToCollapseVisibleConverter() =
    interface IValueConverter with
        override this.Convert(value, targetType, parameter, culture) =
            let boolValue:bool = unbox value
            match boolValue with
            | true -> Visibility.Visible :> _
            | false -> Visibility.Collapsed :> _
        override this.ConvertBack(value, targetType, parameter, culture) =
            null
type SelectedItemChangedConverter() =
    inherit EventArgsConverter<RoutedPropertyChangedEventArgs<Object>, Event>(Convert.selectedItemChangedConverter, Event.Unknown)