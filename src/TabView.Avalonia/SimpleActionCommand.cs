using System;
using System.Windows.Input;

namespace TabView.Avalonia;

internal class SimpleActionCommand(Action action) : ICommand
{
    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => action.Invoke();

    public event EventHandler? CanExecuteChanged;
}

internal class SimpleParamActionCommand(Action<object?> action) : ICommand
{
    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => action.Invoke(parameter);

    public event EventHandler? CanExecuteChanged;
}
