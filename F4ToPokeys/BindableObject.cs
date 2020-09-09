using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace F4ToPokeys
{
    /// <summary>
    /// Implémente l'interface INotifyPropertyChanged et expose la méthode
    /// RaisePropertyChanged pour que les classes dérivées puissent lever
    /// l'événement PropertyChanged.
    /// </summary>
    [Serializable]
    public abstract class BindableObject : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        [field:NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion // INotifyPropertyChanged

        /// <summary>
        /// Lève l'événement PropertyChanged indiquant qu'une propriété à changé.
        /// </summary>
        /// <param name="propertyName">Nom de la propriété qui a changé.</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            VerifyProperty(propertyName);

            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                if (String.IsNullOrEmpty(propertyName))
                    throw new ArgumentException("propertyName must not be null or empty.");

                // Lever l'événement
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region Membres privés

        /// <summary>
        /// Vérifie que la propriété existe dans la classe dérivée.
        /// </summary>
        /// <param name="propertyName"></param>
        [Conditional("DEBUG")]
        private void VerifyProperty(string propertyName)
        {
            Type type = this.GetType();
            PropertyInfo propInfo = type.GetProperty(propertyName);
            if (propInfo == null)
            {
                // La propriété n'existe pas.
                string msg = string.Format(
                    "{0} is not a public property of {1}",
                    propertyName,
                    type.FullName);

                Debug.Fail(msg);
            }
        }

        #endregion //Membres privés
    }
}
