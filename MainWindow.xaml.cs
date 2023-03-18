using System;
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

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Net.Http;
using System.Windows.Markup;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading;

namespace SeaBattle
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int[,] p1Map = new int[10,10];
        int p1ShipCounter;
        int[,] p2Map = new int[10,10];
        int p2ShipCounter;
        bool turn;

        string JSON;

        NetworkStream stream;

        public MainWindow()
        {
            InitializeComponent();
            createButtonField();
            ConnectToServer();
        }

        void createButtonField()
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j=0; j < 10; j++)
                {
                    Button button = new Button();
                    button.Click += SetShips;
                    Button button2 = new Button();
                    button2.Click += Shoot;
                    button2.IsEnabled = false;
                    Grid.SetColumn(button, i);
                    Grid.SetColumn(button2, i);
                    Grid.SetRow(button, j);
                    Grid.SetRow(button2, j);
                    p1Field.Children.Add(button);
                    p2Field.Children.Add(button2);
                }
            }
            Button button3 = new Button();
            Grid.SetRow(button3, 1);
            button3.Content = "Ready";
            button3.Click += StartPlay;
            button3.HorizontalAlignment = HorizontalAlignment.Center;
            button3.VerticalAlignment = VerticalAlignment.Center;
            mainGrid.Children.Add(button3);
        }

        void SetShips(object sender, RoutedEventArgs e)
        {
            var currentButton = sender as Button;
            if (p1ShipCounter < 20 && p1Map[Grid.GetColumn(currentButton), Grid.GetRow(currentButton)] != 1)
            {
                p1Map[Grid.GetColumn(currentButton), Grid.GetRow(currentButton)] = 1;
                p1ShipCounter++;
                currentButton.Background = Brushes.Blue;
            }
        }

        void StartPlay(object sender, RoutedEventArgs e)
        {
            
            if (p1ShipCounter == 20)
            {
                (sender as Button).IsEnabled = false;
                turn = false;
                SendData(turn);
                var task = Task.Run(() => RecieveData());
                foreach (var button in p1Field.Children)
                {
                    (button as Button).Click -= SetShips;
                }
                foreach (var button in p2Field.Children)
                {
                    (button as Button).IsEnabled = true;
                }
            }
        }

        void Shoot(object sender, RoutedEventArgs e)
        {
            var currentButton = sender as Button;
            if (turn)
            {
                if (p1ShipCounter != 0 && p2ShipCounter != 0)
                {
                    if (p2Map[Grid.GetColumn(currentButton), Grid.GetRow(currentButton)] == 1)
                    {
                        currentButton.Background = Brushes.Red;
                        p2Map[Grid.GetColumn(currentButton), Grid.GetRow(currentButton)] = -1;
                        SendData(true);
                    }
                    else if (p2Map[Grid.GetColumn(currentButton), Grid.GetRow(currentButton)] == 0)
                    {
                        p2Map[Grid.GetColumn(currentButton), Grid.GetRow(currentButton)] = -2;
                        currentButton.Background = Brushes.Gray;
                        SendData(false);
                    }
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        foreach (var button in p2Field.Children)
                        {
                            (button as Button).Click -= Shoot;
                        }
                        EndGame();
                    });
                    SendData(false);
                }
            }
        }

        void EndGame()
        {
            TextBlock textBlock = new TextBlock();
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.FontSize = 42;
            textBlock.Background = Brushes.White;
            Grid.SetColumn(textBlock, 1);
            Grid.SetRow(textBlock, 0);
            if (p1ShipCounter <= 0)
                textBlock.Text = "Потрачено";
            else
                textBlock.Text = "Победа";
            mainGrid.Children.Add(textBlock);
            Button button = new Button();
            Grid.SetRow(button, 1);
            Grid.SetColumn(button, 1);
            button.Content = "Restart";
            button.Click += RestartGame;
            button.HorizontalAlignment = HorizontalAlignment.Center;
            button.VerticalAlignment = VerticalAlignment.Center;
            mainGrid.Children.Add(button);
        }

        void RestartGame(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                p1Field.Children.Clear();
                p2Field.Children.Clear();
                p1Map = new int[10, 10];
                p1ShipCounter = 0;
                p2Map = new int[10, 10];
                p2ShipCounter = 0;
                turn = false;
                JSON = "";
                for (int i = 0; i < mainGrid.Children.Count; i++)
                {
                    if (mainGrid.Children[i] is Button)
                    {
                        mainGrid.Children.Remove(mainGrid.Children[i]);
                        i--;
                    }
                    else if (mainGrid.Children[i] is TextBlock)
                    {
                        mainGrid.Children.Remove(mainGrid.Children[i]);
                        i--;
                    }
                }
            });
            createButtonField();
        }

        void ConnectToServer()
        {
            
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.99"), 8888);
            TcpClient client = new TcpClient();
            client.Connect(iPEndPoint);
            if (client.Connected)
            {
                stream = client.GetStream();
            }
        }

        void SendData(bool isHit, [CallerMemberName] string prop = "")
        {
            SeaBattleJson json = new SeaBattleJson()
            {
                p2ShipCounter = p1ShipCounter
            };

            json.p2Map = new int[100];
            json.p1Map = new int[100];

            for (int i = 0, n = 0 , k=0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    json.p2Map[n++] = p1Map[i, j];
                    json.p1Map[k++] = p2Map[i, j];
                }
            }

            if (prop == "Shoot" && !isHit)
                turn = !turn;

            json.turn = turn;

            var send = JsonSerializer.Serialize(json);

            JSON = send;

            //File.WriteAllText(@"C:\Users\99max\Desktop\Новый текстовый документ (2).txt", send);

            var sendArray = Encoding.UTF8.GetBytes(send);
            stream.Write(sendArray, 0, sendArray.Length);
        }

        void RecieveData()
        {
            while (true)
            {
                byte[] dataRecieve = new byte[1024];
                stream.Read(dataRecieve, 0, dataRecieve.Length);
                ParsedRecieveData(dataRecieve);
            }
        }

        void ParsedRecieveData(byte[] dataRecieve)
        {
            string str = Encoding.UTF8.GetString(dataRecieve);
            str = str.Substring(0, str.LastIndexOf('}')+1);
            var parsedJson = JsonSerializer.Deserialize<SeaBattleJson>(str);


            if (JSON != str && parsedJson.turn == false)
            {
                turn = true;

                int[,] bufMap = new int[10, 10];

                for (int i = 0, n = 0, k=0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        p2Map[i, j] = parsedJson.p2Map[n++];
                        bufMap[i, j] = parsedJson.p1Map[k++];
                    }
                }
                p2ShipCounter = parsedJson.p2ShipCounter;

                Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            if (bufMap[i, j] == -1)
                            {
                                foreach (var e in p1Field.Children)
                                {
                                    if (Grid.GetColumn(e as Button) == i && Grid.GetRow(e as Button) == j)
                                    {
                                        p1Map[Grid.GetColumn(e as Button), Grid.GetRow(e as Button)] = -1;
                                        (e as Button).Content = "X";
                                        (e as Button).Background = Brushes.Red;
                                        p1ShipCounter--;
                                    }
                                }
                            }
                            else if (bufMap[i, j] == -2)
                            {
                                foreach (var e in p1Field.Children)
                                {
                                    if (Grid.GetColumn(e as Button) == i && Grid.GetRow(e as Button) == j)
                                    {
                                        p1Map[Grid.GetColumn(e as Button), Grid.GetRow(e as Button)] = -2;
                                        (e as Button).Content = ".";
                                        (e as Button).Background = Brushes.Gray;
                                    }
                                }
                            }
                        }
                    }
                });
            }
        }
    }

    class SeaBattleJson
    {
        public int[] p1Map { get; set; }
        public int[] p2Map { get; set; }
        public int p2ShipCounter { get; set; }
        public bool turn { get; set; }
    }
}
