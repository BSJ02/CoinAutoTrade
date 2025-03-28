using CoinAutoTradingApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinAutoTradingApp;

partial class TradePage : ContentPage
{
    // BindableProperty를 사용하여 ChatMessages 속성 정의
    public static readonly BindableProperty ChatMessagesProperty =
        BindableProperty.Create(nameof(ChatMessages), typeof(ObservableCollection<ChatMessage>), typeof(TradePage), new ObservableCollection<ChatMessage>());

    public ObservableCollection<ChatMessage> ChatMessages
    {
        get => (ObservableCollection<ChatMessage>)GetValue(ChatMessagesProperty);
        set => SetValue(ChatMessagesProperty, value);
    }


    // DebugMessages도 동일한 방식으로 설정
    public static readonly BindableProperty DebugMessagesProperty =
        BindableProperty.Create(nameof(DebugMessages), typeof(ObservableCollection<ChatMessage>), typeof(TradePage), new ObservableCollection<ChatMessage>());

    public ObservableCollection<ChatMessage> DebugMessages
    {
        get => (ObservableCollection<ChatMessage>)GetValue(DebugMessagesProperty);
        set => SetValue(DebugMessagesProperty, value);
    }


#pragma warning disable CS0618
    public void AddChatMessage(string message)
    {
        // 새로운 메시지를 생성하여 ObservableCollection에 추가
        Device.BeginInvokeOnMainThread(() =>
        {
            Console.WriteLine(message);
            ChatMessages.Insert(0, new ChatMessage
            {
                Timestamp = DateTime.Now.ToString("G"),
                Message = message
            });
        });       
    }

    public void AddDebugMessage(string message)
    {
        // 새로운 메시지를 생성하여 ObservableCollection에 추가
        Device.BeginInvokeOnMainThread(() =>
        {
            Console.WriteLine(message);
            DebugMessages.Insert(0, new ChatMessage
            {
                Timestamp = DateTime.Now.ToString("G"),
                Message = message
            });
        });
    }
#pragma warning restore CS0618
}

