// Version: 0.1.1.86
using System;
using System.Windows.Input;

namespace Thmd.Controls;

public class RelayCommand<T> : ICommand
{
	private readonly Action<T> _execute;

	private readonly Func<T, bool> _canExecute;

	public event EventHandler CanExecuteChanged
	{
		add
		{
			CommandManager.RequerySuggested += value;
		}
		remove
		{
			CommandManager.RequerySuggested -= value;
		}
	}

	public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
	{
		_execute = execute ?? throw new ArgumentNullException("execute");
		_canExecute = canExecute;
	}

	public bool CanExecute(object parameter)
	{
		return _canExecute?.Invoke((T)parameter) ?? true;
	}

	public void Execute(object parameter)
	{
		_execute((T)parameter);
	}
}
