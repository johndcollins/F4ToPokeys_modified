using System;
using System.Diagnostics;
using System.Windows.Input;

namespace F4ToPokeys
{
    /// <summary>
    /// Relaye <see cref="Execute"/> et <see cref="CanExecute"/> à un autre objet via des délégués.
    /// Par défaut, <see cref="CanExecute"/> retourne true.
    /// </summary>
    public class RelayCommand : ICommand
    {
        /// <summary>
        /// Délégué chargé de l'execution de la commande.
        /// </summary>
        private readonly Action<object> execute;

        /// <summary>
        /// Délégué chargé d'autoriser l'execution de la commande.
        /// </summary>
        private readonly Func<object, bool> canExecute;

        /// <summary>
        /// Construit un objet RelayCommand.
        /// </summary>
        /// <param name="execute">Délégué chargé de l'execution de la commande.</param>
        /// <param name="canExecute">Délégué chargé d'autoriser l'execution de la commande.</param>
        /// <exception cref="ArgumentNullException">Si <paramref name="execute"/> est null.</exception>
        public RelayCommand(Action<object> execute = null, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        #region ICommand Membres

        /// <summary>
        /// Se produit lorsque des modifications influent sur l'exécution de la commande.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (canExecute != null)
                    CommandManager.RequerySuggested += value;
            }

            remove
            {
                if (canExecute != null)
                    CommandManager.RequerySuggested -= value;
            }
        }

        /// <summary>
        /// Définit la méthode qui détermine si la commande peut s'exécuter dans son état actuel.
        /// </summary>
        /// <param name="parameter">
        /// Données utilisées par la commande.
        /// Si la commande ne requiert pas que les données soient passées, cet objet peut avoir la valeur null.
        /// </param>
        /// <returns>true si cette commande peut être exécutée ; sinon false.</returns>
        public bool CanExecute(object parameter)
        {
            return execute == null ? false : (canExecute == null ? true : canExecute(parameter));
        }

        /// <summary>
        /// Définit la méthode à appeler lorsque la commande est appelée.
        /// </summary>
        /// <param name="parameter">
        /// Données utilisées par la commande.
        /// Si la commande ne requiert pas que les données soient passées, cet objet peut avoir la valeur null.
        /// </param>
        public void Execute(object parameter)
        {
            if (execute != null)
                execute(parameter);
        }

        #endregion // ICommand Membres
    }
}
