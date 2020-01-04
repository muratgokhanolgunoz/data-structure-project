using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Muugle
{
    /// <summary>
    /// Interaction logic for Result.xaml
    /// </summary>
    public partial class Result : Window
    {
        public static Result window_result = new Result();

        public Result()
        {
            InitializeComponent();
            window_result = this;
        }

        #region Variables

        DropShadowEffect effect = new DropShadowEffect();
        FileStream file_stream;
        StreamReader stream_reader;
        public string content,
                      front_text,
                      searched_text,
                      rear_text,
                      temporary_text;
        int counter = 0, searched_text_first_index, result;

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            effect.BlurRadius = 10;
            effect.ShadowDepth = 2;
            effect.Direction = -100;
            effect.Color = Colors.LightGray;

            border_result_counter.Effect = effect;
            border_searched_text.Effect = effect;
            border_timer.Effect = effect;

            label_timer.Content = MainWindow.mainwindow.timer_result + " saniye";
            label_result_counter.Content = "Yaklaşık " + MainWindow.mainwindow.list_count.ToString() + " sonuç bulundu";
            label_searched_text.Content = "Aranan içerik :      " + MainWindow.mainwindow.textbox_search.Text;

            // Arama sonuçları görüntülenmek üzere result.txt dosyası okunuyor. 
            file_stream = new FileStream(MainWindow.mainwindow.file_location + "result.txt", FileMode.Open, FileAccess.Read);
            stream_reader = new StreamReader(file_stream);

            // Sonuçlar ilgili değişkenden kullanılmak üzere farklı string değişkene atanıyor.
            content = stream_reader.ReadLine();

            while (content != null)
            {
                front_text = null;
                searched_text = null;
                rear_text = null;

                searched_text_first_index = searched_text_first_index = brute_force_matching(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(MainWindow.mainwindow.textbox_search.Text.ToLower()), content);

                if (searched_text_first_index == 0)
                {
                    searched_text_first_index = brute_force_matching(MainWindow.mainwindow.textbox_search.Text.ToUpper(), content);

                    if (searched_text_first_index == 0)
                    {
                        searched_text_first_index = brute_force_matching(MainWindow.mainwindow.textbox_search.Text.ToLower(), content);
                    }

                    else
                    {
                        searched_text_first_index = 0;
                    }
                }

                if (searched_text_first_index != 0)
                {
                    counter++;

                    for (int i = 0; i < content.Length; i++)
                    {
                        if (i < searched_text_first_index)
                        {
                            front_text += content[i];
                        }

                        else if (i > searched_text_first_index)
                        {
                            rear_text += content[i];
                        }

                        else
                        {
                            for (int j = 0; j < MainWindow.mainwindow.textbox_search.Text.Length; j++)
                            {
                                searched_text += content[i];
                                i++;
                            }

                            i--;
                        }
                    }

                    Border row = new Border()
                    {
                        BorderBrush = new SolidColorBrush(Colors.LightGray),
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(5),
                        Padding = new Thickness(10),
                    };
                    row.Effect = effect;

                    StackPanel stackpanel_row = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(),
                    };

                    Label index = new Label()
                    {
                        FontSize = 16,
                        Width = 60,
                    };
                    index.Content = counter;

                    Label front = new Label()
                    {
                        FontSize = 16,
                    };
                    front.Content = front_text;

                    Label searched = new Label()
                    {
                        Background = new SolidColorBrush(Colors.Yellow),
                        FontSize = 16,
                    };
                    searched.Content = searched_text;

                    Label rear = new Label()
                    {
                        FontSize = 16,
                    };
                    rear.Content = rear_text;

                    stackpanel_row.Children.Add(index);
                    stackpanel_row.Children.Add(front);
                    stackpanel_row.Children.Add(searched);
                    stackpanel_row.Children.Add(rear);
                    row.Child = stackpanel_row;
                    stackpanel_result.Children.Add(row);
                }

                // ve bir sonraki satır okunmak üzere satır değiştiriliyor.
                content = stream_reader.ReadLine();
            }

            // Okuma işlemi tamamlandı dosya bağlantıları kapatılıyor.
            stream_reader.Close();
            file_stream.Close();
        }

        private int brute_force_matching(string pattern, string text)
        {
            int i = 0;

            while (i <= text.Length - pattern.Length)
            {
                i++;
                int j = 0;

                while (j < pattern.Length && pattern[j] == text[i + j])
                {
                    j++;
                }

                if (j == pattern.Length)
                {
                    result = i;

                    if (i >= text.Length)
                    {
                        return result;
                    }
                }
            }

            return result;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MainWindow.mainwindow.file_stream != null)
            {
                MainWindow.mainwindow.file_stream.Close();
            }

            if (MainWindow.mainwindow.stream_reader != null)
            {
                MainWindow.mainwindow.stream_reader.Close();
            }

            if (MainWindow.mainwindow.stream_writer != null)
            {
                MainWindow.mainwindow.stream_writer.Close();
            }

            if (file_stream != null)
            {
                file_stream.Close();
            }

            if (stream_reader != null)
            {
                stream_reader.Close();
            }

            File.Delete(MainWindow.mainwindow.file_location + "result.txt");
        }
    }
}