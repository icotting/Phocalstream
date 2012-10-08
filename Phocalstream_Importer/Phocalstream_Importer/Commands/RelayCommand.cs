using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Phocalstream_Importer.Commands
{
    class RelayCommand : ICommand
    {
        readonly Func<Boolean> _canExecute;
        readonly Action _execute;

        public RelayCommand(Action execute): this(execute, null)
        {
        }

        public RelayCommand(Action execute, Func<Boolean> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {

                if (_canExecute != null)
                    CommandManager.RequerySuggested += value;
            }
            remove
            {

                if (_canExecute != null)
                    CommandManager.RequerySuggested -= value;
            }
        }

        public Boolean CanExecute(Object parameter)
        {
            return _canExecute == null ? true : _canExecute();
        }

        public void Execute(Object parameter)
        {
            _execute();
        }
    }
}
