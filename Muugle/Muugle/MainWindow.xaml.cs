using System;
using System.Collections;
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
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.Win32;
using Word = Microsoft.Office.Interop.Word;
using System.Globalization;
using System.Diagnostics;

namespace Muugle
{
    public class node
    {
        public int front_data;      // Aranılan kelimenin belirli bir uzaklık öncesinde bulunan bir karakter indisini tutabilmek için 
        public int rear_data;       // Aranılan kelimenin belirli bir uzaklık sonrasında bulunan bir karakter indisini tutabilmek için 
        public node next;           // Bir sonraki düğüm referansı için
    }       

    public partial class MainWindow : Window
    {
        public static MainWindow mainwindow = new MainWindow();

        public MainWindow()
        {
            InitializeComponent();
            mainwindow = this;

            // Program ilk açıldığında textbox nesnesine otomatik imleç kontrolü için
            textbox_search.Focus();
        }

        #region Variables

        // Tüm dosya işlemleri için ortak dosya konumu
        public string file_location = @"C:\Users\HP\Desktop\Muugle\Muugle\";  

        // Dosya işlemleri için tanımlanan değişkenler
        public FileStream file_stream, file_stream_2, file_stream_levenshtein_distance;   
        public StreamReader stream_reader;
        public StreamWriter stream_writer, stream_writer_levenshtein_distance;
        StringBuilder text;
        OpenFileDialog file_dialog = new OpenFileDialog();
        string search_text_all_to_lower,
               search_text_first_letter_to_upper,
               search_text_all_to_upper,
               file_content,
               temporary_content,
               closest_result,
               html_data;       
        
        public int list_count;                  // Arama işlemi sonunda ne kadar sonuç bulunduğunu 
        int distance = 999,                     // Mesafe algoritmasında aranan kelimeye olan max uzaklık ( Başlangıç değeri rastgele verilmiştir )
            estimated_index_range = 70;         // Tahmini olarak belirlenen , aranan kelimenin öncesi ve sonrasındaki kullanılacak olan index aralığı için tanımlana değişken
        bool window_status = false;             // Yüklenen dosyanın önizlemesini görebilmek için kurgulanan pencere geçişleri için tanımlandı

        // Arama sonuçlarının ne kadar zamanda gerçekleştiğini ölçebilmek için tanımlanan değişkenler
        public string timer_result; 
        Stopwatch timer;
        TimeSpan calculate;

        #endregion

        private void Button_load_pdf_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            close_opened_file();
            text = null;
            PdfReader pdf_reader;
            ITextExtractionStrategy strategy;
            string current_text;         // Okunan dosyayı geçici olarak hafızada tutmak için kullanılıyor
            Nullable<bool> result;      // open_File_dialog nesnesinin açılıp açılmadığını kontrol etmek için kullanılıyor

            File.Delete(file_location + "text.txt");
            File.Delete(file_location + "result.txt");

            file_stream = new FileStream(file_location + "text.txt", FileMode.OpenOrCreate, FileAccess.Write);
            stream_writer = new StreamWriter(file_stream);
            text = new StringBuilder();

            // Seçilecek dosyalar için filtre uygulanıyor
            file_dialog.DefaultExt = ".pdf";
            file_dialog.Filter = "PDF Files (*.pdf)|*.pdf";
            result = file_dialog.ShowDialog();

            if (result == true)
            {
                pdf_reader = new PdfReader(file_dialog.FileName);

                for (int i = 1; i <= pdf_reader.NumberOfPages; i++)
                {
                    strategy = new SimpleTextExtractionStrategy();
                    current_text = PdfTextExtractor.GetTextFromPage(pdf_reader, i, strategy);

                    // Pdf ten alınan içerik değişkene atanıyor
                    current_text = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(current_text)));
                    text.Append(current_text);
                }

                pdf_reader.Close();
                user_control_show_file.label_content.Text = text.ToString();
                stream_writer.WriteLine(text);

                // Dosya bağlantıları kapatılıyor
                stream_writer.Flush();
                stream_writer.Close();
                file_stream.Close();
            }
        }

        private void Button_load_docx_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            close_opened_file();
            text = null;
            Word.Application word;
            Word.Document docs;
            object missing_value, file_path, readOnly;

            // Seçilecek dosya için filtre uygulanıyor
            file_dialog.DefaultExt = ".docx";
            file_dialog.Filter = "Word Files (*.docx)|*.docx";
            Nullable<bool> result = file_dialog.ShowDialog();

            if (result == true)
            {
                // Geçici olarak tanımlanmış dosyalar siliniyor
                File.Delete(file_location + "text.txt");
                File.Delete(file_location + "result.txt");

                // text.txt adlı dosya tekrar oluşturuluyor ve okunan docx dosya geçici dosta içerisine yazılıyor
                file_stream = new FileStream(file_location + "text.txt", FileMode.OpenOrCreate, FileAccess.Write);
                stream_writer = new StreamWriter(file_stream);

                text = new StringBuilder();
                word = new Word.Application();

                missing_value = System.Reflection.Missing.Value;
                file_path = file_dialog.FileName;
                readOnly = true;

                // docx dosyası okunuyor
                docs = word.Documents.Open(ref file_path, ref missing_value, ref readOnly);

                for (int i = 0; i < docs.Paragraphs.Count; i++)
                {
                    text.Append(docs.Paragraphs[i + 1].Range.Text.ToString());
                }

                user_control_show_file.label_content.Text = text.ToString();
                stream_writer.WriteLine(text);

                // Dosya bağlantıları kapatılıyor
                stream_writer.Flush();
                stream_writer.Close();
                file_stream.Close();
            }
        }

        private void Button_load_txt_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            close_opened_file();
            text = null;

            // Seçilecek dosya için filtre uygulanıyor
            file_dialog.DefaultExt = ".txt";
            file_dialog.Filter = "Text Files (*.txt)|*.txt";
            Nullable<bool> result = file_dialog.ShowDialog();

            if (result == true)
            {
                File.Delete(file_location + "text.txt");
                File.Delete(file_location + "result.txt");

                file_stream = new FileStream(file_location + "text.txt", FileMode.OpenOrCreate, FileAccess.Write);
                stream_writer = new StreamWriter(file_stream);

                user_control_show_file.label_content.Text = System.IO.File.ReadAllText(file_dialog.FileName);
                stream_writer.WriteLine(System.IO.File.ReadAllText(file_dialog.FileName));

                // Dosya bağlantıları kapatılıyor
                stream_writer.Flush();
                stream_writer.Close();
                file_stream.Close();
            }
        }

        private void Button_load_html_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            close_opened_file();
            string text_html, content;

            // Seçilecek dosya için filtre uygulanıyor
            file_dialog.DefaultExt = ".html";
            file_dialog.Filter = "HTML Files (*.html)|*.html";
            Nullable<bool> result = file_dialog.ShowDialog();

            // File dialog açıldıysa
            if (result == true)
            {
                user_control_show_file.label_content.Text = null;

                File.Delete(file_location + "text.txt");
                File.Delete(file_location + "result.txt");

                file_stream = new FileStream(file_location + "text.txt", FileMode.OpenOrCreate, FileAccess.Write);
                stream_writer = new StreamWriter(file_stream);

                text_html = System.IO.File.ReadAllText(file_dialog.FileName);

                for (int i = 0; i < text_html.Length; i++)
                {
                    html_data = null;

                    if (text_html[i] == '>')
                    {
                        while (text_html[i] != '<')
                        {
                            i++;
                            if (text_html.Length == i)
                            {
                                break;
                            }

                            if (text_html[i] != '<')
                            {
                                if (text_html[i] != '\n' && text_html[i] != '\t' && text_html[i] != '\r')
                                {
                                    html_data += text_html[i].ToString();
                                }
                            }
                        }
                        stream_writer.WriteLine(html_data);
                    }
                }

                user_control_show_file.label_content.Text = text_html;

                // Dosya bağlantıları kapatılıyor
                stream_writer.Flush();
                stream_writer.Close();
                file_stream.Close();
            }

            // Geçici dosyalar oluşturuluyor ( Bunun sebebi html okuma sonucunda boşluklar oluşuyor. Boşluklardan kurtulmak için geçici değişkenler tanmlanıyor )
            file_stream = new FileStream(file_location + "temporary_text.txt", FileMode.OpenOrCreate, FileAccess.Write);
            stream_writer = new StreamWriter(file_stream);

            file_stream_2 = new FileStream(file_location + "text.txt", FileMode.Open, FileAccess.Read);
            stream_reader = new StreamReader(file_stream_2);

            content = stream_reader.ReadLine();

            while (content != null)
            {
                if (content != "")
                {
                    stream_writer.WriteLine(content);
                }
                content = stream_reader.ReadLine();
            }

            file_stream.Close();
            file_stream_2.Close();
            content = null;

            // Esas dosya silinip tekrar oluşturuluyor
            File.Delete(file_location + "text.txt");

            // text.txt dosyası tekar oluşturuluyor ve yazılmak üzere açılıyor
            file_stream = new FileStream(file_location + "text.txt", FileMode.OpenOrCreate, FileAccess.Write);
            stream_writer = new StreamWriter(file_stream);

            // temporary.txt dosyası okunmak üzere tekrar açılıyor
            file_stream_2 = new FileStream(file_location + "temporary_text.txt", FileMode.Open, FileAccess.Read);
            stream_reader = new StreamReader(file_stream_2);

            content = stream_reader.ReadLine();

            while (content != null)
            {
                content = stream_reader.ReadLine();
                stream_writer.WriteLine(content);
            }

            file_stream.Close();
            file_stream_2.Close();

            // Aktarma işlemleri tamamlandı geçici dosya siliniyor
            File.Delete(file_location + "temporary_text.txt");
        }

        private void LeftMenu_MouseEnter(object sender, MouseEventArgs e)
        {
            Label label = (Label)sender;
            label.Margin = new Thickness(20, 10, 10, 10);
        }

        private void LeftMenu_MouseLeave(object sender, MouseEventArgs e)
        {
            Label label = (Label)sender;
            label.Margin = new Thickness(10, 10, 10, 10);
        }

        private void Textbox_search_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            textbox_search.Text = null;
        }

        private void Button_view_file_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (user_control_show_file.label_content.Text != "")
            {
                if (window_status == false)
                {
                    stackdpanel_main.Visibility = Visibility.Collapsed;
                    user_control_show_file.Visibility = Visibility.Visible;
                    window_status = true;
                }

                else
                {
                    stackdpanel_main.Visibility = Visibility.Visible;
                    user_control_show_file.Visibility = Visibility.Collapsed;
                    window_status = false;
                }
            }

            else
            {
                MessageBox.Show("Görüntülenecek bir dosya bulunamadı", "Muugle", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Textbox_search_KeyDown(object sender, KeyEventArgs e)
        {
            // Arama işlemi için enter tuşu özelliği 
            if (e.Key == Key.Enter && button_search.IsEnabled == true)
            {
                Button_search_Click(button_search, e);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Pencerenin kapanma olayında açık kalan dosya bağlantıları kapatılıyor ve geçici dosyalar siliniyor
            if (file_stream != null)
            {
                file_stream.Close();
            }

            else if (stream_reader != null)
            {
                stream_reader.Close();
            }

            else if (stream_writer != null)
            {
                stream_writer.Close();
            }

            File.Delete(file_location + "text.txt");
            File.Delete(file_location + "result.txt");

            Application.Current.Shutdown();
        }

        private void Textbox_search_TextChanged(object sender, TextChangedEventArgs e)
        {           
            if (textbox_search.Text.Length > 1)
            {
                button_search.IsEnabled = true;
            }

            else
            {
                button_search.IsEnabled = false;
            }
        }

        // Arama işleminin gerçekleşmesi için gerekli fonksiyonları tetikleyen button click fonksiyonu
        private void Button_search_Click(object sender, RoutedEventArgs e)
        {
            if (user_control_show_file.label_content.Text != "")
            {
                // Parametresiz tam eşleşme fonksiyonu
                exact_matching(textbox_search.Text);

                if (list_count == 0)
                {
                    // Yakın eşleşme fonksiyonu 
                    convergent_matching(false);

                    // Kullanıcıdan bir cevap bekleniyor ( tam eşleşmeli sonuçlar mı yoksa yakın eşleşmeli sonuçlar mı gösterilsin)
                    MessageBoxResult question = MessageBox.Show("Bunu mu demek istediniz ?          " + closest_result, "Muugle", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    // Yakın eşleşme fonksiyonunun sonucunda en yakın sonuç kullanıcıya soruluyor
                    if (question == MessageBoxResult.Yes)
                    {
                        // En yakın sonuç seçilirse tekrar tam eşleşma algoritması çağırılıyor ve yakın sonuç parametre olarak gönderiliyor
                        exact_matching(closest_result);
                    }

                    else
                    {
                        // Kullanıcı en yakın olarak bulunan kelimeyi seçti. Bu yüzden de kontrol parametresi olarak "TRUE" değer gönderiyoruz.
                        convergent_matching(true);
                    }

                    // Kullanılan geçici değişkenler varsayılan hale getiriliyor.
                    clear_items();
                }

                // Sonuç olarak bulunan çıktı için ilgili pencere açılıyor.
                Result new_result = new Result();
                new_result.ShowDialog();
            }

            else
            {
                MessageBox.Show("Arama yapılacak bir dosya bulunamadı", "Muugle", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Bu algoritmada ki amaç, aranılan kelimenin indisleyi indisleri tek tek kullanarak kontrol edip kelimenin ilk indisini bulabilmektir.
        private node brute_force_matching(node root, string pattern, string text)
        {
            int i = 0, front_i = 0, rear_i = 0;

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
                    // Parametre olarak gönderilen metin içerisinden aranılan kelimenin ilk indisi bulunuyor ve ekranda bir cümle olarak gösterileceğinden 
                    // tahmini olarak 60 karakter öncesi ve sonrasında ki indis hesaplanarak listeye kayıt ediliyor
                    front_i = i - estimated_index_range;
                    rear_i  = i + estimated_index_range;

                    // Eğer tahmini 60 karakter geri gidildiğinde indis 0' dan küçük oluyor ise indis 0 olacak şekilde ayarlanıyor
                    if (front_i < 0)
                    {
                        front_i = 0;
                    }

                    // Aynı şekilde tahmini 60 karakter ileri gidildiğinde indis içeriğin toplam karakter uzunluğundan fazla oluyor ise liste değişkenine içeriğin son indisi gönderiliyor
                    if (rear_i > text.Length)
                    {
                        rear_i = text.Length;
                    }

                    // Bir önceki adımda ayarlanan önceki ve sonraki indis değerleri liste' ye kayıt edilmek üzere ilgili fonksiyona parametre olarak gönderiliyor
                    root = insert_in_list(root, front_i, rear_i);

                    if (i >= text.Length)
                    {
                        return root;
                    }
                }
            }
            return root;
        }

        private node insert_in_list(node root, int _front_data, int _rear_data)
        {
            // Parametre olarak gönderilen bağlı liste boş ise
            if (root == null)
            {
                // Yeni bir node tanımlanır
                root = new node();

                // Aranılan kelimenin ilk indisinden belirli uzaklıktaki indis ( kelimenin öncesi için )
                root.front_data = _front_data;

                // Aranılan kelimenin ilk indisinden belirli uzaklıktaki indis ( kelimenin sonrası için )
                root.rear_data = _rear_data;

                // root' un bir sonraki tutacağı referans null olarak belirleniyor
                root.next = null;
            }

            // Herhangi bir veri var ise
            else
            {
                // Liste boş olmadığı için kökü kaybetmek istemeyiz bu nedenle bir geçici değişken tanımlanıyor
                node temporary = new node();

                // Başa ekleme yapılıyor ve bu yüzden yardımcı değişken en başta olacağından next özelliği root olmak zorunda
                temporary.next = root;

                // Aranılan kelimenin ilk indisinden belirli uzaklıktaki indis ( kelimenin öncesi için )
                temporary.front_data = _front_data;

                // Aranılan kelimenin ilk indisinden belirli uzaklıktaki indis ( kelimenin sonrası için )
                temporary.rear_data = _rear_data;
                root = temporary;

                // Geçici olarak tanımlanan değişken Garbage Collector kullanılarak bellekten siliniyor
                System.GC.SuppressFinalize(temporary);
            }

            return root;
        }

        // Tam eşlesme algoritmasının kullanıldığı fonksiyon
        private void exact_matching(string _search_text)
        {
            // Zamanlayıcı başlatılıyor
            timer = new Stopwatch();
            timer.Start();

            textbox_search.Text = _search_text;

            // Kullanılan liste ve liste uzunluğu değişkenleri varsayılan hale getiriliyor
            node root = null;
            list_count = 0;

            // Alınan kelimenin tamamı küçük karakterlerden oluşacak şekilde ayarlanıyor
            search_text_all_to_lower = _search_text.ToLower();

            // Alınan kelimenin ilk harfi büyük karakterlerden oluşacak şekilde ayarlanıyor
            search_text_first_letter_to_upper = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(search_text_all_to_lower);

            // Alınan kelimenin tamamı büyük karakterlerden oluşacak şekilde ayarlanıyor
            search_text_all_to_upper = search_text_all_to_lower.ToUpper();            

            // Yüklenen dosya geçici olarak kayıt edildiği text.txt adlı dosyadan okunuyor ve tanımlanan 'file_content' adlı string değişkene aktarılıyor
            stream_reader = new StreamReader(file_location + "text.txt");
            file_content = stream_reader.ReadToEnd();

            // Okunan içerik tam eşleşme algoritmasına, aranılan kelime sırasıyla küçük, ilk harf büyük ve hepsi büyük karakter olacak şelikde gönderiliyor
            root = brute_force_matching(root, search_text_all_to_lower, file_content);
            find_words(root, file_content, search_text_all_to_lower.Length);

            root = brute_force_matching(root, search_text_first_letter_to_upper, file_content);
            find_words(root, file_content, search_text_first_letter_to_upper.Length);

            root = brute_force_matching(root, search_text_all_to_upper, file_content);
            find_words(root, file_content, search_text_all_to_upper.Length);

            // Dödürülen sonuçlara göre verileri tutan listenin uzunluğu alınıyor
            while (root != null)
            {
                list_count++;
                root = root.next;
            }

            // Zamanlayıcı durduruluyor ve ekranda gösterilmek üzere ilgili nesnenin content özelliğine gönderiliyor
            timer.Stop();

            calculate = timer.Elapsed;
            timer_result = string.Format("{0:00}:{1:00}", calculate.Seconds, calculate.Milliseconds / 10);
        }

        // Yakın eşleşme algoritmasının kullanıldığı fonksiyon
        private void convergent_matching(bool search_status)
        {
            // Geçici olarak kullanılan text.txt adlı dosyadan arama yapılacak dosya içeriği okunuyor
            file_stream = new FileStream(file_location + "text.txt", FileMode.Open, FileAccess.Read);
            stream_reader = new StreamReader(file_stream);
            string content = stream_reader.ReadLine();

            // Dosya while döngüsü kullanılarak en son satır' a kadar okunuyor
            while (content != null)
            {
                for (int i = 0; i < content.Length; i++)
                {
                    if (content[i] != ' ')
                    {
                        temporary_content += content[i];
                    }

                    else
                    {
                        // "FALSE" paramatre göndermemizin sebebi kullanıcıya soru sorabilmek için ilk olarak aranan kelimeye en yakın sonucu bulmak gerekiyor. Bu durumu kontrol amaçlı olarak kullanılmıştır.
                        result_find_levenshtein_distance(search_text_all_to_lower, temporary_content, search_status);
                        temporary_content = null;
                    }
                }

                content = stream_reader.ReadLine();
            }

            stream_reader.Close();
            file_stream.Close();            
        }        

        // Tam eşleşme algoritmasında doldurulan liste bu fonksiyon ile ekrana yazılıyor
        private void find_words(node root, string file_content, int search_text_length)
        {
            string content;         // result.txt adlı dosyadan okunan içeriğin tutulacağı değişken
            int distance_i = 0;     // Aranan kelimenin tahmini 68. satırda belirtilen index aralığı kadar karakter öncesi ve sonrasını bulmak için tanımlanan indis aralık değişkeni ( içerik içindeki esas indislerin farkını tutar )

            // result.txt adlı geçici dosya okunuyor
            File.Delete(file_location + "result.txt");
            file_stream = new FileStream(file_location + "result.txt", FileMode.OpenOrCreate, FileAccess.Write);
            stream_writer = new StreamWriter(file_stream);

            while (root != null)
            {
                // Önceki ve sonraki indis değerlerinin farkları hesaplanyor
                distance_i = root.rear_data - root.front_data;
                content = null;

                content += "... ";

                // Önceki inditen başlayarak sonraki indise kadar dosyaya yazılıyor
                for (int i = 0; i < distance_i; i++)
                {
                    if (file_content[root.front_data + i] != '\n')
                    {
                        content += file_content[root.front_data + i];
                    }
                }

                content += " ...";
                stream_writer.WriteLine(content);
                root = root.next;
            }


            // Dosya bağlantıları kapatılıyor
            stream_writer.Flush();
            stream_writer.Close();
            file_stream.Close();
        }

        // Açılan dosya bağlantıları kapatılıyor
        public void close_opened_file()
        {
            if (file_stream != null)
            {
                file_stream.Close();
            }

            else if (stream_reader != null)
            {
                stream_reader.Close();
            }

            else if (stream_writer != null)
            {
                stream_writer.Close();
            }
        }     

        // Yakın eşleşme algoritmasının yönetildiği fonksiyon
        private void result_find_levenshtein_distance(string source, string target, bool status)
        {           
            if (target != null)
            {
                int[,] matrix = new int[source.Length, target.Length];
                int result_distance = source.find_levenshtein_distance(target, out matrix);

                // Eğer en yakın değer istenirse bunun için status paramatresi false değerli gelirse bu blok çalışacaktır
                if (status == false)
                {
                    if (result_distance < distance)
                    {
                        distance = result_distance;
                        closest_result = target;
                    }
                }

                // Aksi halde status true değerli gelirse yakın olan tüm sonuçlar MessageBox nesnesi ile ekranda gösterilcektir
                else
                {
                    if (result_distance <= 3)
                    {
                        MessageBox.Show(" Yakın Kelime : " + target + " \n Yakınlık Derecesi : " + result_distance);                       
                    }
                }
            }
        }

        // Kullanılan değişkenler varsayılan hale getiriliyor
        private void clear_items()
        {
            distance = 9999;
            temporary_content = null;
            textbox_search.Focus();
        }
    }

    // Yakın eşleşme algoritması 
    public static class StringExtensions
    {
        public static int find_levenshtein_distance(this string source, string target, out int[,] matrix)
        {
            int n = source.Length;
            int m = target.Length;

            matrix = new int[n + 1, m + 1];

            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            for (int i = 0; i <= n; i++)
            {
                matrix[i, 0] = i;
            }

            for (int j = 0; j <= m; j++)
            {
                matrix[0, j] = j;
            }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[n, m];
        }
    }
}