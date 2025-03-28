using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp.Models
{
    public class ChatMessage : INotifyPropertyChanged
    {
        private string message;
        private string timestamp;

        public string Message
        {
            get => message;
            set
            {
                if (message != value)
                {
                    message = value;
                    OnPropertyChanged(nameof(Message)); // PropertyChanged 이벤트 발생
                }
            }
        }

        public string Timestamp
        {
            get => timestamp;
            set
            {
                if (timestamp != value)
                {
                    timestamp = value;
                    OnPropertyChanged(nameof(Timestamp)); // PropertyChanged 이벤트 발생
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{Timestamp}: {Message}";
        }
    }
}
