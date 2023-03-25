using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using BigeumTalkClient.Network;
using System.Collections.Specialized;

namespace BigeumTalkClient.Pages
{
    /// <summary>
    /// ChatPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChatPage : Page
    {
        private SocketNetwork sn;
        internal static ChatPage chatPage;
        public readonly LimitedSizeObservableCollection<Chat> chats;
        public readonly ObservableCollection<User> usersList;

        public ChatPage()
        {
            InitializeComponent();
            InitWindowLocation();

            sn = new SocketNetwork();
            chats = new LimitedSizeObservableCollection<Chat>(300);
            usersList = new ObservableCollection<User>();

            chatPage = this;
            ChatListView.ItemsSource = chats._collection;
            UserList.ItemsSource = usersList;

            Binding b = new Binding();
            b.Source = usersList;
            b.Path = new PropertyPath("Count");
            b.Mode = BindingMode.OneWay;
            BindingOperations.SetBinding(RoomUsersCountBlock, TextBlock.TextProperty, b);

            sn.Send_C_ENTER_ROOM();
        }

        private void InitWindowLocation()
        {
            MainWindow main = (MainWindow)Application.Current.MainWindow;

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            main.Left = (screenWidth / 2) - (windowWidth / 2);
            main.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        private void BindingElements(DependencyObject target, DependencyProperty dp, string name)
        {

        }

        private void SendText_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void SendText_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if (String.IsNullOrWhiteSpace(SendText.Text) == true)
                {
                    return;
                }

                sn.Send_C_CHAT(SendText.Text.Trim());
                SendText.Text = "";
            }
        }
    }

    public static class AutoScrollBehavior
    {
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(AutoScrollBehavior), new PropertyMetadata(false, AutoScrollPropertyChanged));


        public static void AutoScrollPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var scrollViewer = obj as ScrollViewer;
            if (scrollViewer != null && (bool)args.NewValue)
            {
                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
                scrollViewer.ScrollToEnd();
            }
            else
            {
                scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
            }
        }

        private static void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Only scroll to bottom when the extent changed. Otherwise you can't scroll up
            if (e.ExtentHeightChange != 0)
            {
                var scrollViewer = sender as ScrollViewer;
                scrollViewer?.ScrollToBottom();
            }
        }

        public static bool GetAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollProperty, value);
        }

    }

    public class LimitedSizeObservableCollection<T> : INotifyCollectionChanged
    {
        public ObservableCollection<T> _collection;
        private bool _ignoreChange;

        public LimitedSizeObservableCollection(int capacity)
        {
            Capacity = capacity;
            _ignoreChange = false;
            _collection = new ObservableCollection<T>();
            _collection.CollectionChanged += _collection_CollectionChanged;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Capacity { get; }

        public void Add(T item)
        {
            if (_collection.Count == Capacity)
            {
                _ignoreChange = true;
                _collection.RemoveAt(0);
                _ignoreChange = false;
            }
            _collection.Add(item);

        }

        private void _collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_ignoreChange)
            {
                CollectionChanged?.Invoke(this, e);
            }
        }
    }
}
