using System;

namespace Oxtail.Utils
{
    public class ReactiveProperty<T>
    {
        public Action<T> OnPropertyChanged;
        public Action<string> OnPropertyChangedAsString;

        private T m_Value;

        public ReactiveProperty(T initialValue = default)
        {
            m_Value = initialValue;
        }

        public T Value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                OnPropertyChanged?.Invoke(m_Value);
                OnPropertyChangedAsString?.Invoke(m_Value.ToString());
            }
        }

        public void RemoveListeners()
        {
            OnPropertyChanged = null;
            OnPropertyChangedAsString = null;
        }
    }
}
